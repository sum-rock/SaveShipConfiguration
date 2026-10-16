// Captures live sails and pending parts without invoking save hooks or changing game data.
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SaveShipConfiguration.Compatibility;
using SaveShipConfiguration.Models;
using UnityEngine;

namespace SaveShipConfiguration.ShipyardIntegration
{
    internal static class PresetCapture
    {
        internal static string Fingerprint(BoatCustomParts parts)
        {
            var text = new StringBuilder();
            foreach (var part in parts.availableParts)
            {
                text.Append(part.category).Append(':').Append(part.partOptions.Count).Append(';');
                foreach (var option in part.partOptions)
                {
                    string name = option.optionName + "|" + option.name;
                    text.Append(name.Length).Append(':').Append(name);
                }
            }
            using (var hash = SHA256.Create())
                return BitConverter
                    .ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())))
                    .Replace("-", "");
        }

        internal static Preset Capture(Shipyard shipyard, ExpansionAdapter expansion)
        {
            var boat = shipyard.GetCurrentBoat();
            var preset = new Preset
            {
                Id = Guid.NewGuid().ToString("N"),
                BoatType = PresetRules.BoatType(boat.name),
                PartsFingerprint = Fingerprint(boat.GetComponent<BoatCustomParts>()),
                PartOptions = (int[])
                    shipyard.partsInstaller.GetCurrentOrder().orderedOptions.Clone(),
            };
            foreach (var mast in boat.GetComponent<BoatRefs>().masts)
            {
                if (!mast || !mast.gameObject.activeInHierarchy)
                    continue;
                foreach (var obj in mast.sails)
                {
                    var sail = obj.GetComponent<Sail>();
                    var saved = new SailPreset
                    {
                        PrefabIndex = sail.prefabIndex,
                        PrefabName = PresetRules.BoatType(
                            PrefabsDirectory.instance.sails[sail.prefabIndex].name
                        ),
                        MastIndex = mast.orderIndex,
                        MastName = mast.name,
                        Height = sail.GetCurrentInstallHeight(),
                        ScaleY = sail.GetScaleY(),
                        ScaleZ = sail.GetScaleZ(),
                        Color = sail.activeColor,
                    };
                    expansion.Capture(sail, saved);
                    preset.Sails.Add(saved);
                }
            }
            return preset;
        }

        internal static void Validate(Preset preset, Shipyard shipyard, ExpansionAdapter expansion)
        {
            var boat = shipyard.GetCurrentBoat();
            var parts = boat.GetComponent<BoatCustomParts>();
            PresetRules.ValidateLayout(
                preset,
                PresetRules.BoatType(boat.name),
                Fingerprint(parts),
                parts.availableParts.Select(p => p.partOptions.Count).ToArray()
            );
            var masts = boat.GetComponent<BoatRefs>().masts;
            var prefabs = PrefabsDirectory.instance.sails;
            foreach (var saved in preset.Sails)
            {
                if (
                    saved.MastIndex >= masts.Length
                    || !masts[saved.MastIndex]
                    || masts[saved.MastIndex].name != saved.MastName
                )
                    throw new InvalidOperationException(
                        "A saved mast is missing or has changed: " + saved.MastName
                    );
                if (
                    saved.PrefabIndex >= prefabs.Length
                    || !prefabs[saved.PrefabIndex]
                    || PresetRules.BoatType(prefabs[saved.PrefabIndex].name) != saved.PrefabName
                    || !prefabs[saved.PrefabIndex].GetComponent<Sail>()
                )
                    throw new InvalidOperationException(
                        "A saved sail is missing or has changed: " + saved.PrefabName
                    );
                if (saved.Color >= PrefabsDirectory.instance.sailColors.Length)
                    throw new InvalidOperationException("A saved sail color is unavailable.");
                expansion.Validate(saved, prefabs[saved.PrefabIndex]);
            }
            foreach (var group in preset.Sails.GroupBy(s => s.MastIndex))
                if (group.Count() > masts[group.Key].maxSails)
                    throw new InvalidOperationException(
                        "Too many saved sails for " + masts[group.Key].name
                    );
        }
    }
}
