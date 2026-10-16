// Minimal shipyard-only IMGUI list, naming dialog, and guarded preset operations.
using System;
using System.Collections.Generic;
using System.Linq;
using SaveShipConfiguration.Compatibility;
using SaveShipConfiguration.Models;
using SaveShipConfiguration.ShipyardIntegration;
using SaveShipConfiguration.Storage;
using UnityEngine;

namespace SaveShipConfiguration
{
    internal sealed class PresetPanel : MonoBehaviour
    {
        private PresetStore store;
        private ExpansionAdapter expansion;
        private PresetApplier applier;
        private Shipyard current;
        private GameObject boat;
        private List<Preset> presets = new List<Preset>();
        private string boatType;
        private string message = "";
        private string nameText = "";
        private Preset pending;
        private bool replacing;
        private bool focusName;
        private bool visible;
        private Vector2 scroll;
        private Rect window = new Rect(16, 80, 320, 420);
        private int lastBlockedFrame = -10;
        internal bool Busy => applier != null && applier.Busy;

        internal void Initialize(PresetStore store, ExpansionAdapter expansion)
        {
            this.store = store;
            this.expansion = expansion;
            applier = new PresetApplier(expansion, text => message = text);
        }

        internal void SetVisible(bool value)
        {
            visible = value;
            if (!value)
                pending = null;
        }

        private bool IsVisible =>
            visible && GameState.currentShipyard && boat && current == GameState.currentShipyard;

        internal bool BlocksInput
        {
            get
            {
                if (!IsVisible)
                    return false;
                bool block =
                    Busy
                    || pending != null
                    || window.Contains(
                        new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)
                    );
                if (block)
                    lastBlockedFrame = Time.frameCount;
                // Keep a dialog-closing click blocked through the rest of this frame.
                return block || lastBlockedFrame == Time.frameCount;
            }
        }

        private void Update()
        {
            var yard = GameState.currentShipyard;
            var activeBoat = yard ? yard.GetCurrentBoat() : null;
            if (current == yard && boat == activeBoat)
                return;
            current = yard;
            boat = activeBoat;
            pending = null;
            replacing = false;
            message = "";
            scroll = Vector2.zero;
            presets = new List<Preset>();
            if (boat)
            {
                boatType = PresetRules.BoatType(boat.name);
                Guard(() => presets = store.List(boatType));
            }
        }

        private void OnGUI()
        {
            if (!IsVisible || store == null)
                return;
            window.width = Mathf.Min(320, Screen.width - 16);
            window.height = Mathf.Min(420, Screen.height - 32);
            window.x = Mathf.Clamp(window.x, 0, Screen.width - window.width);
            window.y = Mathf.Clamp(window.y, 0, Screen.height - window.height);
            window = GUILayout.Window(817203, window, Draw, "Ship configurations");
        }

        private void Draw(int id)
        {
            GUILayout.Label(boatType);
            if (pending != null)
            {
                GUILayout.Label(
                    replacing ? "Replace the existing configuration?" : "Name this configuration:"
                );
                GUI.SetNextControlName("PresetName");
                string edited = GUILayout.TextField(nameText, 80);
                if (edited != nameText)
                    replacing = false;
                nameText = edited;
                if (focusName)
                {
                    GUI.FocusControl("PresetName");
                    focusName = false;
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(replacing ? "Replace" : "Save"))
                    Guard(Save);
                if (GUILayout.Button("Cancel"))
                {
                    lastBlockedFrame = Time.frameCount;
                    pending = null;
                    replacing = false;
                    GUI.FocusControl(null);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUI.enabled = !Busy;
                if (GUILayout.Button("Save"))
                    Guard(() =>
                    {
                        pending = PresetCapture.Capture(current, expansion);
                        nameText = "";
                        replacing = false;
                        focusName = true;
                        message = "";
                    });
                if (GUILayout.Button("Clear rigging"))
                    Guard(() => message = RiggingClearer.Clear(shipyard: current));
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
                if (presets.Count == 0)
                    GUILayout.Label("No saved configurations for this boat.");
                foreach (var preset in presets)
                    if (GUILayout.Button(preset.Name))
                        StartCoroutine(applier.Start(preset, current));
                GUILayout.EndScrollView();
                GUI.enabled = true;
            }
            GUILayout.Label(message, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUI.DragWindow(new Rect(0, 0, window.width, 22));
        }

        private void Save()
        {
            pending.Name = PresetRules.Name(nameText);
            bool duplicate = presets.Any(p =>
                string.Equals(p.Name, pending.Name, StringComparison.OrdinalIgnoreCase)
            );
            if (duplicate && !replacing)
            {
                replacing = true;
                return;
            }
            store.Save(pending, overwrite: replacing);
            message = "Saved " + pending.Name + ".";
            presets = store.List(boatType);
            lastBlockedFrame = Time.frameCount;
            pending = null;
            replacing = false;
            GUI.FocusControl(null);
        }

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                message = ex.GetBaseException().Message;
                Plugin.Log.LogError(ex);
            }
        }

        internal void Shutdown()
        {
            StopAllCoroutines();
            applier?.Shutdown();
        }
    }
}
