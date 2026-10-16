// Verifies installed API contracts and Harmony targets without executing Unity code.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;

internal static class Program
{
    private static int checks;
    private static readonly BindingFlags All =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static void Main(string[] args)
    {
        string game =
            args.Length > 0
                ? Path.GetFullPath(args[0])
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local/share/Steam/steamapps/common/Sailwind"
                );
        string[] dirs =
        {
            Path.Combine(game, "Sailwind_Data/Managed"),
            Path.Combine(game, "BepInEx/core"),
            Path.Combine(game, "BepInEx/plugins/ShipyardExpansion"),
        };
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (string dir in dirs)
            {
                string path = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(path))
                    return context.LoadFromAssemblyPath(path);
            }
            return null;
        };
        var plugin = Assembly.LoadFrom(
            Path.Combine(AppContext.BaseDirectory, "SaveShipConfiguration.dll")
        );
        Check(
            !plugin.GetReferencedAssemblies().Any(a => a.Name == "ShipyardExpansion"),
            "optional Shipyard Expansion integration must not be a hard reference"
        );
        var identity = plugin.GetType("SaveShipConfiguration.Plugin", throwOnError: true);
        Check(
            (string)identity.GetField("PluginVersion").GetRawConstantValue() == "0.1.0",
            "plugin version"
        );
        Check(
            (string)identity.GetField("PluginGuid").GetRawConstantValue()
                == "com.august.saveshipconfiguration",
            "plugin identity"
        );
        foreach (
            var patch in plugin
                .GetTypes()
                .Where(t => t.Namespace == "SaveShipConfiguration.Patches")
        )
        {
            var target = patch.GetCustomAttributes<HarmonyPatch>().Single().info;
            var method = target.declaringType.GetMethod(
                target.methodName,
                All,
                null,
                target.argumentTypes ?? Type.EmptyTypes,
                null
            );
            Check(
                method != null,
                "Harmony target: " + target.declaringType.Name + "." + target.methodName
            );
            foreach (
                var callback in patch
                    .GetMethods(All)
                    .Where(m => m.Name == "Prefix" || m.Name == "Postfix")
            )
            foreach (var parameter in callback.GetParameters())
            {
                if (parameter.Name == "__instance")
                    Check(parameter.ParameterType == target.declaringType, "instance injection");
                else if (parameter.Name.StartsWith("___"))
                    Check(
                        target.declaringType.GetField(parameter.Name.Substring(3), All)?.FieldType
                            == parameter.ParameterType,
                        "field injection: " + parameter.Name
                    );
                else
                    throw new Exception("Unchecked patch parameter: " + parameter.Name);
            }
        }
        var native = Assembly.LoadFrom(Path.Combine(dirs[0], "Assembly-CSharp.dll"));
        foreach (
            var item in new[]
            {
                ("Shipyard", "GetCurrentBoat", 0),
                ("Shipyard", "UpdateOrder", 0),
                ("ShipyardPartsInstaller", "GetCurrentOrder", 0),
                ("ShipyardPartsInstaller", "GetOrderError", 0),
                ("ShipyardSailInstaller", "AddNewSail", 1),
                ("ShipyardSailInstaller", "DestroySail", 2),
                ("ShipyardSailInstaller", "RecheckAllSailsCols", 0),
                ("ShipyardSailInstaller", "DoRecheck", 0),
                ("Sail", "LoadScale", 2),
                ("Sail", "ChangeInstallHeight", 1),
                ("Mast", "UpdateControllerAttachments", 0),
            }
        )
            Check(
                native
                    .GetType(item.Item1, true)
                    .GetMethods(All)
                    .Any(m => m.Name == item.Item2 && m.GetParameters().Length == item.Item3),
                "native API " + item
            );
        string sePath = Path.Combine(dirs[2], "ShipyardExpansion.dll");
        if (File.Exists(sePath))
        {
            var se = Assembly.LoadFrom(sePath);
            var scaler = se.GetType("ShipyardExpansion.SailScaler", true);
            Check(scaler.GetProperty("Angle")?.PropertyType == typeof(float), "SE angle");
            Check(scaler.GetProperty("Flipped")?.PropertyType == typeof(bool), "SE flipped");
            Check(scaler.GetMethod("SetAngle", new[] { typeof(float) }) != null, "SE SetAngle");
            Check(scaler.GetMethod("FlipJib", new[] { typeof(bool) }) != null, "SE FlipJib");
            var texture = se.GetType("ShipyardExpansion.Scripts.SailTextureChanger", true);
            Check(
                texture.GetField("textureIndex")?.FieldType == typeof(string),
                "SE texture index"
            );
            Check(texture.GetField("textures")?.IsStatic == true, "SE texture catalog");
            Check(
                texture.GetMethod("SetTexture", new[] { typeof(string) }) != null,
                "SE SetTexture"
            );
        }
        else
            Console.WriteLine("Shipyard Expansion not installed; optional adapter checks skipped.");
        Console.WriteLine($"Assembly checks passed: {checks}. Unity behavior was not executed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        checks++;
    }
}
