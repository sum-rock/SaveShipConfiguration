// Blocks all native/modded pointer buttons under the IMGUI panel.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Blocks all native/modded pointer buttons under the IMGUI panel.
    [HarmonyPatch(typeof(MouseButtonPointer), "LateUpdate")]
    internal static class PointerInputPatch
    {
        private static bool Prefix() => !(Plugin.Panel?.BlocksInput ?? false);
    }
}
