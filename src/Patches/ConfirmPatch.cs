// Prevent confirmation until deferred sail initialization and collision checks finish.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Prevent confirmation until deferred sail initialization and collision checks finish.
    [HarmonyPatch(typeof(Shipyard), "ConfirmOrder")]
    internal static class ConfirmPatch
    {
        private static bool Prefix() => !(Plugin.Panel?.Busy ?? false);
    }
}
