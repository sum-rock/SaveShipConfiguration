# SaveShipConfiguration development

## Tooling and repository workflow

Version: **0.1.0**. Plugin GUID: `com.august.saveshipconfiguration`. The root
AGENTS.md is copied verbatim from MoreSailwindSails at the user's request; its
original heading and sail-specific guidance remain. Applicable workflow rules
are unchanged. This project uses LogicChecks and AssemblyChecks as its two
suites.

The pinned Nix flake, CSharpier 1.3.0 manifest, formatting configuration, and
pre-commit hooks come from MoreSailwindSails. The shell supplies .NET 8, ILSpy,
Python, Ruff, Prettier, and pre-commit. The plugin targets netstandard2.0 and
references installed BepInEx 5, Harmony, and Unity/game assemblies. No
proprietary assemblies or extracted assets belong in this repository.

## Build and automated checks

From the repository root:

```sh
nix develop path:. -c dotnet tool restore
nix develop path:. -c dotnet restore
nix develop path:. -c dotnet csharpier check .
nix develop path:. -c dotnet build -c Release --no-restore
nix develop path:. -c dotnet run --project tests/LogicChecks -c Release --no-restore
nix develop path:. -c dotnet run --project tests/AssemblyChecks -c Release --no-restore
nix develop path:. -c prettier --check "**/*.md"
nix develop path:. -c ruff format --check .
git diff --check
```

`path:.` also works when this workspace lacks usable Git metadata. In a regular
Git checkout, `nix develop` is equivalent; the copied pre-commit hooks use that
form. Run `pre-commit install` manually inside the shell to enable hooks. Use
CSharpier `format` and Prettier `--write` when fixing formatting.

Default game location: `~/.local/share/Steam/steamapps/common/Sailwind`.
Override with `-p:SailwindDir="/path/to/Sailwind"` for restore/build/test
commands; AssemblyChecks also takes the game location as an argument after `--`.
Output: `src/bin/Release/netstandard2.0/SaveShipConfiguration.dll`. Builds and
checks never install the plugin or modify game saves.

LogicChecks runs portable JSON, naming, filtering, and validation scenarios in a
temporary directory. AssemblyChecks inspects installed API signatures and
Harmony injections without patching or executing Unity. Its optional SE checks
run when Shipyard Expansion is installed at its usual plugin path.

## Manual local installation

Close Sailwind, then run from the repository root:

```sh
./scripts/install-local.sh
./scripts/install-local.sh "/path/to/Sailwind"
```

The optional argument selects another game installation. Like MoreSailwindSails'
installer, this script requires an existing Release DLL and BepInEx plugins
directory, rebuilds Release, then installs only the DLL into
`BepInEx/plugins/SaveShipConfiguration/`. It resolves the repository location
independently of the calling directory and forwards the selected game directory
to the build as `SailwindDir`.

This is a manual maintainer workflow. Agents must never execute files from
`scripts/` or use it as their working directory. Script validation is limited to
syntax and static review; installation has not been exercised in-game.

## Implementation decisions

- Presets are independent version-1 JSON documents with generated GUID
  filenames. Each stores a normalized internal boat name, ordered-part
  fingerprint and option indices, and ordered sail records. No game-save slot or
  individual boat ID is persisted. File replacement uses a flushed temporary
  file in the same directory.
- Save reads pending `BoatPartsOrder.orderedOptions` and live sail components;
  invoking native GetData would miss pending parts and trigger SE side effects.
- Load validates structure, boat layout, mast/prefab identities, colors, sail
  counts, and optional textures before changing the order. It cancels to the
  native installed baseline, removes sails through native refund logic, assigns
  the parts order, and adds new sails through ShipyardSailInstaller. Existing
  pending edits are discarded. It does not optimize purchase cost.
- Each added sail waits through native deferred placement and component startup
  before restoring settings. Restored dimensions and SE appearance are checked
  for silent clamping or rejection. The native DoRecheck enumerator is driven to
  completion, then its collision checkers finish before confirmation is
  reenabled. Native obstruction and dependency errors remain visible in the
  order.
- SE rotation/flip/texture access is isolated behind reflection. At every native
  ResetOrder, preserve its per-boat `SEboatSails` modData value; restore that
  value before CancelOrder calls LoadData. This tracks admission, confirmation,
  repeated preset loads, cancellation, and discharge without changing SE or game
  files.
- The full native mast-array length is covered during cancellation, including
  MoreSailwindSails' extended stay indices. Parts are identified by an ordered
  fingerprint; this intentionally rejects presets after layout changes rather
  than guessing a migration.
- The IMGUI panel blocks the native mouse pointer, held shipyard buttons, sail
  sizing hotkeys, and boat camera input while hovered, naming, or loading.
  Loading errors attempt to return to the original installed baseline and report
  recovery failures. This is not a general third-party mod compatibility layer.

## Runtime validation

Implementation checks (2026-10-15): Release build passed with zero warnings and
errors; 22 logic checks and 33 installed-assembly checks passed, including the
installed Shipyard Expansion adapter. CSharpier and Prettier checks passed; Ruff
found no Python files. AGENTS.md and flake.lock match MoreSailwindSails byte for
byte. Git status/diff checks are unavailable because this workspace's `.git`
directory does not contain usable repository metadata.

Automated checks do not establish rendering, input isolation, sail
initialization, collision timing, or successful native purchase/cancellation
behavior in Unity. No in-game validation has been performed during
implementation.

Manual acceptance checklist:

1. Save a pending configuration with mast/part changes and resized/recolored
   sails; confirm it captures the preview rather than only installed parts.
2. Reload it on the same boat and another boat of the same type, including
   another save. Ensure other boat types show separate lists; restart to check
   persistence.
3. Switch repeatedly between two presets and check totals/refunds do not
   accumulate. Compare pricing with manually removing and buying the same sails.
4. Cancel and discharge without purchase; verify all original parts, sails,
   colors, and SE rotation/flip/textures return. Confirm an order, save/reload
   the game, then cancel another preset to check the newly confirmed baseline.
5. Exercise MoreSailwindSails flying sails and staysails on extended mast
   indices, with and without SE. Test an empty rig and a boat with no presets.
6. Type digits in a name, drag/scroll the panel, replace a duplicate name, and
   click over underlying native/SE buttons. No shipyard edit should leak
   through.
7. Try missing sails, changed part layouts, invalid JSON, and unavailable
   textures. Preflight failures should preserve the pending order. A mid-load
   failure should restore the installed baseline and leave confirmation
   unavailable during loading.

Installation is manual. Close the game before copying the Release DLL. Report
observed game behavior separately from automated results and update these notes
when runtime checks are actually performed.
