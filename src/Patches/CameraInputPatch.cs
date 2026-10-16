// Scroll and drag events over the panel belong to its list/window.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Scroll and drag events over the panel belong to its list/window.
    [HarmonyPatch(typeof(BoatCamera), "Update")]
    internal static class CameraInputPatch
    {
        private static bool Prefix() => !(Plugin.Panel?.BlocksInput ?? false);
    }
}
