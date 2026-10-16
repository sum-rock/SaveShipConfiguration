// Reads independent JSON presets and replaces files atomically in the same directory.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using SaveShipConfiguration.Models;

namespace SaveShipConfiguration.Storage
{
    internal sealed class PresetStore
    {
        private readonly string directory;
        private readonly Action<string> report;

        internal PresetStore(string directory, Action<string> report)
        {
            this.directory = directory;
            this.report = report;
        }

        internal List<Preset> List(string boatType)
        {
            var presets = new List<Preset>();
            if (!Directory.Exists(directory))
                return presets;
            foreach (string path in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    using (var stream = File.OpenRead(path))
                    {
                        var preset = (Preset)
                            new DataContractJsonSerializer(typeof(Preset)).ReadObject(stream);
                        PresetRules.Validate(preset);
                        if (Path.GetFileNameWithoutExtension(path) != preset.Id)
                            throw new InvalidDataException(
                                "Preset ID does not match its filename."
                            );
                        if (preset.BoatType == boatType)
                            presets.Add(preset);
                    }
                }
                catch (Exception ex)
                {
                    report("Cannot read " + Path.GetFileName(path) + ": " + ex.Message);
                }
            }
            return presets.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal void Save(Preset preset, bool overwrite)
        {
            preset.Name = PresetRules.Name(preset.Name);
            PresetRules.Validate(preset);
            var duplicate = List(preset.BoatType)
                .FirstOrDefault(p =>
                    string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase)
                );
            if (duplicate != null)
            {
                if (!overwrite)
                    throw new InvalidOperationException("A preset with that name already exists.");
                preset.Id = duplicate.Id;
            }
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, preset.Id + ".json");
            if (File.Exists(path) && !overwrite)
                throw new IOException("Preset already exists.");
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                {
                    new DataContractJsonSerializer(typeof(Preset)).WriteObject(stream, preset);
                    stream.Flush(flushToDisk: true);
                }
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }
}
