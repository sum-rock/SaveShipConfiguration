// One sail's native settings and optional Shipyard Expansion appearance.
namespace SaveShipConfiguration.Models
{
    public sealed class SailPreset
    {
        public int PrefabIndex;
        public string PrefabName;
        public int MastIndex;
        public string MastName;
        public float Height;
        public float ScaleY;
        public float ScaleZ;
        public int Color;
        public bool HasExpansion;
        public float Angle;
        public bool Flipped;
        public string Texture;
    }
}
