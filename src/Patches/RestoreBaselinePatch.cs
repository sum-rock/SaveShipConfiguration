// Restore SE's sidecar before its native LoadData postfix can read preview state.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Restore SE's sidecar before its native LoadData postfix can read preview state.
    [HarmonyPatch(typeof(Shipyard), "CancelOrder")]
    internal static class RestoreBaselinePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Shipyard __instance, SaveBoatCustomizationData ___originalData)
        {
            OrderBaseline.BeforeCancel(__instance);
            int count = __instance.GetCurrentBoat().GetComponent<BoatRefs>().masts.Length;
            if (
                ___originalData != null
                && (___originalData.masts == null || ___originalData.masts.Length < count)
            )
                Array.Resize(ref ___originalData.masts, count);
        }
    }
}
