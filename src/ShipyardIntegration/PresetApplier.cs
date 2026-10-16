// Rebuilds a pending native order, serializing sail creation around Unity initialization.
using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using SaveShipConfiguration.Compatibility;
using SaveShipConfiguration.Models;
using UnityEngine;

namespace SaveShipConfiguration.ShipyardIntegration
{
    internal sealed class PresetApplier
    {
        private readonly ExpansionAdapter expansion;
        private readonly Action<string> status;
        private Shipyard owner;
        private GameObject boat;
        internal bool Busy { get; private set; }

        internal PresetApplier(ExpansionAdapter expansion, Action<string> status)
        {
            this.expansion = expansion;
            this.status = status;
        }

        internal IEnumerator Start(Preset preset, Shipyard shipyard)
        {
            if (Busy)
                yield break;
            // This runs before the first yield, so double clicks cannot queue another load.
            Busy = true;
            owner = shipyard;
            boat = shipyard.GetCurrentBoat();
            bool changed = false;
            var operation = Apply(preset, () => changed = true);
            while (true)
            {
                object next = null;
                bool more = false;
                Exception failure = null;
                try
                {
                    more = operation.MoveNext();
                    if (more)
                        next = operation.Current;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                if (failure != null)
                {
                    Plugin.Log.LogError(failure);
                    string message = "Could not load preset: " + failure.GetBaseException().Message;
                    if (changed)
                    {
                        try
                        {
                            Restore();
                            message += " Original order baseline restored.";
                        }
                        catch (Exception recovery)
                        {
                            Plugin.Log.LogError(recovery);
                            message += " Recovery failed; leave the shipyard without confirming.";
                        }
                    }
                    status(message);
                    break;
                }
                if (!more)
                {
                    status("Loaded " + preset.Name + ". Review and confirm the shipyard order.");
                    break;
                }
                yield return next;
            }
            (operation as IDisposable)?.Dispose();
            Busy = false;
            owner = null;
            boat = null;
        }

        private IEnumerator Apply(Preset preset, Action markChanged)
        {
            EnsureBoat();
            PresetCapture.Validate(preset, owner, expansion);
            markChanged();
            status("Loading " + preset.Name + "…");
            // Clear native position/check coroutines before they can reference removed sails.
            owner.sailInstaller.StopAllCoroutines();
            owner.CancelOrder();
            var refs = boat.GetComponent<BoatRefs>();
            foreach (var mast in refs.masts)
            {
                if (!mast)
                    continue;
                foreach (var obj in mast.sails.ToArray())
                    owner.sailInstaller.DestroySail(mast, obj.GetComponent<Sail>());
            }
            Array.Copy(
                preset.PartOptions,
                owner.partsInstaller.GetCurrentOrder().orderedOptions,
                preset.PartOptions.Length
            );
            Refresh();
            yield return null;
            EnsureBoat();
            if (owner.partsInstaller.GetOrderError())
                throw new InvalidOperationException(
                    "The saved part combination cannot be fitted here."
                );
            foreach (var saved in preset.Sails)
            {
                EnsureBoat();
                var mast = refs.masts[saved.MastIndex];
                if (!mast.gameObject.activeInHierarchy)
                    throw new InvalidOperationException(
                        "Saved parts did not enable mast " + saved.MastName
                    );
                owner.sailInstaller.SelectMast(mast);
                var obj = UnityEngine.Object.Instantiate(
                    PrefabsDirectory.instance.sails[saved.PrefabIndex]
                );
                try
                {
                    owner.sailInstaller.AddNewSail(obj);
                }
                catch
                {
                    // Unregistered instances would otherwise survive native cancellation.
                    if (!mast.sails.Contains(obj))
                        UnityEngine.Object.Destroy(obj);
                    throw;
                }
                // AddNewSail has its own deferred position fix. Keep its selected sail stable
                // until that routine and the new components' Start methods have completed.
                yield return new WaitForEndOfFrame();
                yield return new WaitForFixedUpdate();
                yield return null;
                EnsureBoat();
                var sail = obj.GetComponent<Sail>();
                sail.LoadScale(saved.ScaleY, saved.ScaleZ);
                sail.ChangeSailColor(saved.Color);
                expansion.Apply(sail, saved);
                sail.ChangeInstallHeight(saved.Height - sail.GetCurrentInstallHeight());
                sail.UpdateInstallPosition();
                if (
                    Mathf.Abs(sail.GetScaleY() - saved.ScaleY) > 0.001f
                    || Mathf.Abs(sail.GetScaleZ() - saved.ScaleZ) > 0.001f
                    || Mathf.Abs(sail.GetCurrentInstallHeight() - saved.Height) > 0.001f
                    || sail.activeColor != saved.Color
                )
                    throw new InvalidOperationException(
                        "The shipyard rejected saved settings for " + saved.PrefabName
                    );
                mast.UpdateSailOrder();
                mast.UpdateControllerAttachments();
            }
            owner.sailInstaller.SelectMast(null);
            owner.sailInstaller.StopAllCoroutines();
            // Drive the native enumerator so its completion, rather than guessed frame
            // counts, determines when every sail's collision check has been started.
            var recheck = (IEnumerator)
                AccessTools
                    .Method(typeof(ShipyardSailInstaller), "DoRecheck")
                    .Invoke(owner.sailInstaller, null);
            try
            {
                while (true)
                {
                    EnsureBoat();
                    if (!recheck.MoveNext())
                        break;
                    yield return recheck.Current;
                }
            }
            finally
            {
                (recheck as IDisposable)?.Dispose();
            }
            float deadline = Time.realtimeSinceStartup + 30f;
            while (true)
            {
                EnsureBoat();
                bool waiting = refs
                    .masts.Where(m => m)
                    .SelectMany(m => m.sails)
                    .Any(s =>
                        s.GetComponent<SailConnections>().colChecker.checking
                        || !s.GetComponent<SailConnections>().colChecker.readyToInstall
                    );
                if (!waiting)
                    break;
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Sail collision checks did not finish.");
                yield return null;
            }
            EnsureBoat();
            Refresh();
        }

        private void EnsureBoat()
        {
            if (
                !owner
                || !boat
                || GameState.currentShipyard != owner
                || owner.GetCurrentBoat() != boat
            )
                throw new InvalidOperationException("The active shipyard boat changed.");
        }

        private void Refresh()
        {
            ShipyardUI.instance.ResetPartsErrorTexts();
            // Opening rigging rebuilds labels from the pending choices, including mod pages.
            ShipyardUI.instance.ChangeMenuCategory(0);
            owner.UpdateOrder();
            ShipyardUI.instance.RefreshButtons();
        }

        private void Restore()
        {
            EnsureBoat();
            owner.sailInstaller.StopAllCoroutines();
            owner.CancelOrder();
            Refresh();
        }

        internal void Shutdown()
        {
            if (
                Busy
                && owner
                && boat
                && GameState.currentShipyard == owner
                && owner.GetCurrentBoat() == boat
            )
            {
                try
                {
                    Restore();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError(ex);
                }
            }
            Busy = false;
        }
    }
}
