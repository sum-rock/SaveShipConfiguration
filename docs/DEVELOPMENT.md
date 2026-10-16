# SaveShipConfiguration development

## Tooling and repository workflow

Version: **0.1.0**. Plugin GUID: `com.august.saveshipconfiguration`. See
[AGENTS.md](../AGENTS.md) for repository workflow. This project uses LogicChecks
and AssemblyChecks as its two check suites.

The pinned Nix shell supplies .NET 8, ILSpy, Python, Ruff, Prettier, and
pre-commit. The .NET tool manifest pins CSharpier 1.3.0, and pre-commit hooks
apply the repository's formatting configuration. The plugin targets
netstandard2.0 and references installed BepInEx 5, Harmony, and Unity/game
assemblies. No proprietary assemblies or extracted assets belong in this
repository.

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
Git checkout, `nix develop` is equivalent; the pre-commit hooks use that form.
Run `pre-commit install` manually inside the shell to enable hooks. Use
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

The optional argument selects another game installation. The script requires an
existing Release DLL and BepInEx plugins directory, rebuilds Release, then
installs only the DLL into `BepInEx/plugins/SaveShipConfiguration/`. It resolves
the repository location independently of the calling directory and forwards the
selected game directory to the build as `SailwindDir`.

This is a manual maintainer workflow. Agents must never execute files from
`scripts/` or use it as their working directory. Script validation is limited to
syntax and static review.

## Implementation decisions

- Presets are independent version-1 JSON documents with generated GUID
  filenames. Each stores a normalized internal boat name, ordered-part
  fingerprint and option indices, and ordered sail records. No game-save slot or
  individual boat ID is persisted. File replacement uses a flushed temporary
  file in the same directory.
- Save reads pending `BoatPartsOrder.orderedOptions` and live sail components;
  invoking native GetData would miss pending parts and trigger SE side effects.
- **Clear rigging** removes sails, stays, shrouds, masts, then bowsprits, in
  that order. Sails use native refund handling; each subsequent group selects
  the native `(no ...)` options and refreshes the pending order before the next
  group. Installed boat assets use name-based rigging identities across multiple
  categories, so categories and option indices are not hardcoded. Parts without
  a removal option remain and are reported. Other pending changes are preserved;
  native order validation, confirmation, and cancellation still apply.
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
  indices added by mods. Registered parts and prefabs are discovered at runtime
  without a dependency on a specific content mod. Parts are identified by an
  ordered fingerprint; this intentionally rejects presets after layout changes
  rather than guessing a migration.
- The IMGUI panel blocks the native mouse pointer, held shipyard buttons, sail
  sizing hotkeys, and boat camera input while hovered, naming, or loading.
  Loading errors attempt to return to the original installed baseline and report
  recovery failures. This is not a general third-party mod compatibility layer.

## Automated check results

Implementation checks (2026-10-15): Release build passed with zero warnings and
errors; 22 logic checks and 45 installed-assembly checks passed, including
rigging group classification and the installed Shipyard Expansion adapter.
CSharpier, Prettier, and Git diff checks passed; Ruff found no Python files.
