# SaveShipConfiguration

A bare-bones Sailwind shipyard preset mod for repeated QA setups. Version 0.1.0.
Requires BepInEx 5; built against the locally installed Sailwind 0.39
assemblies. MoreSailwindSails and Shipyard Expansion are optional.

1. Visit a shipyard and configure your boat, including any pending changes.
2. Click **Save** in the draggable **Ship configurations** window, enter a name,
   and save. Reusing a name asks before replacing it.
3. Click a saved name to replace the pending configuration. Review the native
   order, then confirm and pay normally, or cancel.

Presets are shared across saves for the same boat type. They include parts,
rigging, sails, dimensions, placement, colors, and supported Shipyard Expansion
rotation, flip, and textures. Loading rebuilds the sails, so normal replacement
charges and refunds apply. It discards the current unconfirmed order, including
cleaning/repair requests. Cargo, placed furniture, damage, and cleaning/repair
services are not part of a preset.

Install `SaveShipConfiguration.dll` manually under
`<Sailwind>/BepInEx/plugins/SaveShipConfiguration/` with the game closed.
Presets are separate JSON files under
`<Sailwind>/BepInEx/config/SaveShipConfiguration/`. Remove a JSON file while the
game is closed to delete a preset. Removing the DLL leaves your boat and preset
files intact.

Changed boat part layouts or missing mod assets cause presets to be rejected;
recreate affected presets after changing mods. Arbitrary third-party shipyard
extensions are not supported. In-game behavior still needs manual validation.

See [development instructions](docs/DEVELOPMENT.md) for builds, checks, and the
runtime QA checklist.
