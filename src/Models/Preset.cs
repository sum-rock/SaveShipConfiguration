// Portable shipyard configuration; contains no Unity objects or game-save identity.
using System.Collections.Generic;

namespace SaveShipConfiguration.Models
{
    public sealed class Preset
    {
        public int Version = 1;
        public string Id;
        public string Name;
        public string BoatType;
        public string PartsFingerprint;
        public int[] PartOptions;
        public List<SailPreset> Sails = new List<SailPreset>();
    }
}
