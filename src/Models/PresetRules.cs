// Validates portable data before any shipyard state is changed.
using System;
using System.IO;
using System.Linq;

namespace SaveShipConfiguration.Models
{
    internal static class PresetRules
    {
        internal static string BoatType(string name)
        {
            name = name ?? "";
            while (name.EndsWith("(Clone)", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 7).TrimEnd();
            return name;
        }

        internal static string Name(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0 || value.Length > 80 || value.Any(char.IsControl))
                throw new InvalidDataException(
                    "Use a name of 1–80 characters without control characters."
                );
            return value;
        }

        internal static void Validate(Preset preset)
        {
            if (preset == null || preset.Version != 1)
                throw new InvalidDataException("Unsupported preset format.");
            if (!Guid.TryParseExact(preset.Id, "N", out _))
                throw new InvalidDataException("Invalid preset ID.");
            Name(preset.Name);
            if (
                string.IsNullOrEmpty(preset.BoatType)
                || string.IsNullOrEmpty(preset.PartsFingerprint)
                || preset.PartOptions == null
                || preset.PartOptions.Any(n => n < 0)
                || preset.Sails == null
            )
                throw new InvalidDataException("Incomplete preset.");
            foreach (var sail in preset.Sails)
            {
                if (
                    sail == null
                    || sail.PrefabIndex < 0
                    || sail.MastIndex < 0
                    || sail.Color < 0
                    || string.IsNullOrEmpty(sail.PrefabName)
                    || string.IsNullOrEmpty(sail.MastName)
                    || !Finite(sail.Height)
                    || !Finite(sail.ScaleY)
                    || !Finite(sail.ScaleZ)
                    || sail.ScaleY <= 0
                    || sail.ScaleZ <= 0
                    || (
                        sail.HasExpansion
                        && (!Finite(sail.Angle) || string.IsNullOrEmpty(sail.Texture))
                    )
                )
                    throw new InvalidDataException("Invalid sail settings.");
            }
        }

        internal static void ValidateLayout(
            Preset preset,
            string boatType,
            string fingerprint,
            int[] optionCounts
        )
        {
            Validate(preset);
            if (preset.BoatType != boatType)
                throw new InvalidDataException("This preset belongs to another boat type.");
            if (
                preset.PartsFingerprint != fingerprint
                || preset.PartOptions.Length != optionCounts.Length
            )
                throw new InvalidDataException(
                    "Boat parts have changed since this preset was saved. Recreate the preset."
                );
            for (int i = 0; i < optionCounts.Length; i++)
                if (preset.PartOptions[i] >= optionCounts[i])
                    throw new InvalidDataException("A saved part option is unavailable.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
