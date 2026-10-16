// Owns plugin lifecycle, logging, and the shipyard preset UI.
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SaveShipConfiguration.Compatibility;
using SaveShipConfiguration.ShipyardIntegration;
using SaveShipConfiguration.Storage;

namespace SaveShipConfiguration
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(ExpansionAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.august.saveshipconfiguration";
        public const string PluginName = "SaveShipConfiguration";
        public const string PluginVersion = "0.1.0";
        internal static ManualLogSource Log { get; private set; }
        internal static PresetPanel Panel { get; private set; }
        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            var expansion = new ExpansionAdapter();
            Panel = gameObject.AddComponent<PresetPanel>();
            Panel.Initialize(
                new PresetStore(
                    Path.Combine(Paths.ConfigPath, PluginName),
                    message => Logger.LogWarning(message)
                ),
                expansion
            );
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded!");
        }

        private void OnDestroy()
        {
            if (Panel)
            {
                Panel.Shutdown();
                Destroy(Panel);
            }
            Panel = null;
            harmony?.UnpatchSelf();
        }
    }
}
