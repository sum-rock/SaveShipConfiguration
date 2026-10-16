// Close naming dialogs when the native menu closes.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Close naming dialogs when the native menu closes.
    [HarmonyPatch(typeof(ShipyardUI), "HideUI")]
    internal static class HidePanelPatch
    {
        private static void Postfix() => Plugin.Panel?.SetVisible(false);
    }
}
