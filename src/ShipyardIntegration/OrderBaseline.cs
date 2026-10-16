// Keeps SE's per-boat sidecar aligned with the native order cancellation snapshot.
using UnityEngine;

namespace SaveShipConfiguration.ShipyardIntegration
{
    internal static class OrderBaseline
    {
        private static Shipyard owner;
        private static GameObject boat;
        private static string key;
        private static string value;
        private static bool existed;

        internal static void Capture(Shipyard shipyard)
        {
            owner = shipyard;
            boat = shipyard.GetCurrentBoat();
            var saveable = boat ? boat.GetComponent<SaveableObject>() : null;
            key = saveable ? "SEboatSails." + saveable.sceneIndex : null;
            value = null;
            existed =
                key != null
                && GameState.modData != null
                && GameState.modData.TryGetValue(key, out value);
        }

        internal static void BeforeCancel(Shipyard shipyard)
        {
            // Selection can otherwise reference a sail destroyed by native LoadData.
            shipyard.sailInstaller.SelectMast(null);
            if (
                owner != shipyard
                || boat != shipyard.GetCurrentBoat()
                || key == null
                || GameState.modData == null
            )
                return;
            if (existed)
                GameState.modData[key] = value;
            else
                GameState.modData.Remove(key);
        }
    }
}
