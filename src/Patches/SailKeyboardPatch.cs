// Prevent typing digits in a preset name from resizing the selected sail.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Prevent typing digits in a preset name from resizing the selected sail.
    [HarmonyPatch(typeof(ShipyardSailInstaller), "Update")]
    internal static class SailKeyboardPatch
    {
        private static bool Prefix() => !(Plugin.Panel?.BlocksInput ?? false);
    }
}
