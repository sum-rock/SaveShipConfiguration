// Track actual menu visibility instead of displaying over the sailing view.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Track actual menu visibility instead of displaying over the sailing view.
    [HarmonyPatch(typeof(ShipyardUI), "ShowUI")]
    internal static class ShowPanelPatch
    {
        private static void Postfix() => Plugin.Panel?.SetVisible(true);
    }
}
