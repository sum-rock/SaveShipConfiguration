// Held native buttons can activate independently of MouseButtonPointer.
using System;
using HarmonyLib;
using SaveShipConfiguration.ShipyardIntegration;

namespace SaveShipConfiguration.Patches
{
    // Held native buttons can activate independently of MouseButtonPointer.
    [HarmonyPatch(typeof(ShipyardButton), "OnActivate", new Type[] { })]
    internal static class ButtonInputPatch
    {
        private static bool Prefix() => !(Plugin.Panel?.BlocksInput ?? false);
    }
}
