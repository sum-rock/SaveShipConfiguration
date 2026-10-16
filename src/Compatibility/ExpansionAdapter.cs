// Optional reflection bridge to installed Shipyard Expansion sail settings.
using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using SaveShipConfiguration.Models;
using UnityEngine;

namespace SaveShipConfiguration.Compatibility
{
    internal sealed class ExpansionAdapter
    {
        internal const string Guid = "com.nandbrew.shipyardexpansion";
        private readonly Type scalerType;
        private readonly Type textureType;
        private readonly PropertyInfo angle;
        private readonly PropertyInfo flipped;
        private readonly FieldInfo texture;
        private readonly FieldInfo textures;
        private readonly MethodInfo setAngle;
        private readonly MethodInfo flip;
        private readonly MethodInfo setTexture;
        private readonly bool installed;

        internal ExpansionAdapter()
        {
            installed = Chainloader.PluginInfos.ContainsKey(Guid);
            if (!installed)
                return;
            scalerType = AccessTools.TypeByName("ShipyardExpansion.SailScaler");
            textureType = AccessTools.TypeByName("ShipyardExpansion.Scripts.SailTextureChanger");
            angle = scalerType?.GetProperty("Angle");
            flipped = scalerType?.GetProperty("Flipped");
            texture = textureType?.GetField("textureIndex");
            textures = textureType?.GetField("textures");
            setAngle = scalerType?.GetMethod("SetAngle", new[] { typeof(float) });
            flip = scalerType?.GetMethod("FlipJib", new[] { typeof(bool) });
            setTexture = textureType?.GetMethod("SetTexture", new[] { typeof(string) });
        }

        private void RequireApi()
        {
            if (
                !installed
                || angle == null
                || flipped == null
                || texture == null
                || textures == null
                || setAngle == null
                || flip == null
                || setTexture == null
            )
                throw new InvalidOperationException(
                    "This preset requires a supported Shipyard Expansion installation."
                );
        }

        internal void Capture(Sail sail, SailPreset saved)
        {
            if (!installed)
                return;
            RequireApi();
            var scaler = sail.GetComponent(scalerType);
            var changer = sail.GetComponent(textureType);
            if (!scaler || !changer)
                throw new InvalidOperationException(
                    "Shipyard Expansion components are missing from " + sail.name
                );
            saved.HasExpansion = true;
            saved.Angle = (float)angle.GetValue(scaler, null);
            saved.Flipped = (bool)flipped.GetValue(scaler, null);
            saved.Texture = (string)texture.GetValue(changer);
        }

        internal void Validate(SailPreset saved, GameObject prefab)
        {
            if (!saved.HasExpansion)
                return;
            RequireApi();
            if (!prefab.GetComponent(scalerType) || !prefab.GetComponent(textureType))
                throw new InvalidOperationException(
                    "Shipyard Expansion components are unavailable for " + saved.PrefabName
                );
            var catalog = textures.GetValue(null) as IDictionary;
            if (catalog == null || !catalog.Contains(saved.Texture))
                throw new InvalidOperationException(
                    "Saved sail texture is unavailable: " + saved.Texture
                );
        }

        internal void Apply(Sail sail, SailPreset saved)
        {
            if (!saved.HasExpansion)
                return;
            RequireApi();
            var scaler = sail.GetComponent(scalerType);
            var changer = sail.GetComponent(textureType);
            setAngle.Invoke(scaler, new object[] { saved.Angle });
            flip.Invoke(scaler, new object[] { saved.Flipped });
            setTexture.Invoke(changer, new object[] { saved.Texture });
            if (
                Mathf.Abs(Mathf.DeltaAngle((float)angle.GetValue(scaler, null), saved.Angle))
                    > 0.01f
                || (bool)flipped.GetValue(scaler, null) != saved.Flipped
                || (string)texture.GetValue(changer) != saved.Texture
            )
                throw new InvalidOperationException(
                    "The sail mod rejected the saved rotation, flip, or texture."
                );
        }
    }
}
