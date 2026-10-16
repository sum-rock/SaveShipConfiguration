// A native reset establishes the new installed baseline after admission/confirm/cancel.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // A native reset establishes the new installed baseline after admission/confirm/cancel.
    [HarmonyPatch(typeof(Shipyard), "ResetOrder")]
    internal static class CaptureBaselinePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Shipyard __instance) => OrderBaseline.Capture(__instance);
    }
}
