// Exercises portable persistence and validation without loading Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SaveShipConfiguration.Models;
using SaveShipConfiguration.Storage;

internal static class Program
{
    private static int checks;

    private static void Main()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "ship-presets-checks-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var errors = new List<string>();
            var store = new PresetStore(directory, errors.Add);
            Check(store.List("brig").Count == 0, "new directory is empty");
            var first = Example("brig", "QA α ../rig");
            store.Save(first, overwrite: false);
            var loaded = store.List("brig").Single();
            Check(
                loaded.Name == first.Name && loaded.PartOptions.SequenceEqual(first.PartOptions),
                "JSON parts and Unicode name round trip"
            );
            var sail = loaded.Sails.Single();
            Check(
                sail.MastIndex == 128
                    && sail.Height == 12.5f
                    && sail.ScaleY == 0.8f
                    && sail.ScaleZ == 1.3f
                    && sail.HasExpansion
                    && sail.Flipped
                    && sail.Angle == 350
                    && sail.Texture == "cloth",
                "extended mast and sail settings round trip"
            );
            Check(
                Directory.GetFiles(directory).All(p => Path.GetFileName(p) == first.Id + ".json"),
                "generated safe filename"
            );
            Check(store.List("junk").Count == 0, "boat filtering");
            var other = Example("junk", first.Name);
            store.Save(other, overwrite: false);
            Check(store.List("junk").Count == 1, "same name on another boat");
            var replacement = Example("brig", first.Name.ToUpperInvariant());
            Throws(
                () => store.Save(replacement, overwrite: false),
                "duplicate requires explicit replacement"
            );
            replacement.PartOptions[0] = 2;
            store.Save(replacement, overwrite: true);
            Check(
                store.List("brig").Single().PartOptions[0] == 2 && replacement.Id == first.Id,
                "replace retains ID and updates data"
            );
            Check(!Directory.GetFiles(directory, "*.tmp").Any(), "temporary files removed");
            File.WriteAllText(Path.Combine(directory, "broken.json"), "{invalid");
            Check(
                store.List("brig").Count == 1 && errors.Count > 0,
                "bad file does not hide valid presets"
            );
            Check(PresetRules.BoatType("brig(Clone)(Clone)") == "brig", "clone normalization");
            Throws(() => PresetRules.Name(" \t "), "blank name rejected");
            Throws(() => PresetRules.Name("x\ny"), "control characters rejected");
            PresetRules.ValidateLayout(first, "brig", "layout", new[] { 3, 3 });
            checks++;
            Throws(
                () => PresetRules.ValidateLayout(first, "junk", "layout", new[] { 3, 3 }),
                "wrong boat rejected"
            );
            Throws(
                () => PresetRules.ValidateLayout(first, "brig", "changed", new[] { 3, 3 }),
                "changed part layout rejected"
            );
            Throws(
                () => PresetRules.ValidateLayout(first, "brig", "layout", new[] { 1, 1 }),
                "missing part option rejected"
            );
            first.Sails[0].ScaleY = float.NaN;
            Throws(
                () => store.Save(first, overwrite: true),
                "nonfinite scale rejected before overwrite"
            );
            Check(
                store.List("brig").Single().Sails[0].ScaleY == 0.8f,
                "failed validation preserves old file"
            );
            first.Sails[0].ScaleY = 0.8f;
            first.Version = 2;
            Throws(() => PresetRules.Validate(first), "unknown schema rejected");
            first.Version = 1;
            first.Id = "../../unsafe";
            Throws(() => store.Save(first, overwrite: true), "invalid file ID rejected");
            var empty = Example("brig", "bare boat");
            empty.Sails.Clear();
            store.Save(empty, overwrite: false);
            Check(store.List("brig").Any(p => p.Sails.Count == 0), "empty rig supported");
            Console.WriteLine($"Logic checks passed: {checks}");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static Preset Example(string boat, string name) =>
        new Preset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            BoatType = boat,
            PartsFingerprint = "layout",
            PartOptions = new[] { 1, 2 },
            Sails = new List<SailPreset>
            {
                new SailPreset
                {
                    PrefabIndex = 401,
                    PrefabName = "custom-sail",
                    MastIndex = 128,
                    MastName = "extended-mast",
                    Height = 12.5f,
                    ScaleY = 0.8f,
                    ScaleZ = 1.3f,
                    Color = 11,
                    HasExpansion = true,
                    Angle = 350,
                    Flipped = true,
                    Texture = "cloth",
                },
            },
        };

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        checks++;
    }

    private static void Throws(Action action, string message)
    {
        try
        {
            action();
        }
        catch
        {
            checks++;
            return;
        }
        throw new Exception("Expected rejection: " + message);
    }
}
