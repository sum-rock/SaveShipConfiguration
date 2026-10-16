# Working on SaveShipConfiguration

Read [README.md](README.md), [src/Plugin.cs](src/Plugin.cs), relevant code and
the applicable sections of [DEVELOPMENT.md](docs/DEVELOPMENT.md) before editing.
Check `git status --short` and preserve user changes. Current user instructions
take precedence over historical design choices.

## Scope and workflow

- Use named arguments rather than positional arguments whenever possible to
  improve readability.
- Prefer one class per file and describe its responsibility in a comment at the
  top of the file.
- Update `README.md` only when the user explicitly requests a README change.
  Feature, bug-fix, release and general documentation work do not imply
  permission to edit it; put technical and automated-check updates in
  `docs/DEVELOPMENT.md`.
- Implementation requests authorize editing, building and checking. Do not
  commit, push, alter saves or replace installed game files as part of a build.
  **Never execute files from `scripts/` or use it as the working directory.**
- Use installed assemblies/assets as the behavioral reference; upstream source
  may differ. Never commit proprietary assemblies or extracted game assets.

## Design guidelines

- Resist complexity. Choose the simplest, smallest implementation that meets the
  request. Keep code paths straightforward and avoid speculative features,
  abstractions, and infrastructure.
- Do not require other gameplay mods. Core preset functionality must work with
  the base game and BepInEx alone.
- Support Shipyard Expansion when installed, but never require it. Keep its
  integration optional and isolated so its absence does not prevent the plugin
  from loading or using native shipyard features.
- Keep integration patches narrowly scoped; do not modify game or dependency
  assemblies.
- Preserve saved preset compatibility and the game's order confirmation and
  cancellation behavior. Presets belong in separate files, outside game saves.
- Record technical and architectural decisions in
  [DEVELOPMENT.md](docs/DEVELOPMENT.md). Update existing sections instead of
  duplicating information.

## Verification and handoff

Use the pinned Nix formatting environment and the
[build and check commands](docs/DEVELOPMENT.md#build-and-automated-checks).
Always build Release for plugin-affecting changes and run LogicChecks and
AssemblyChecks. Use CSharpier `check` for read-only work and `format` to fix
formatting. For Markdown, use Prettier `--check` to verify and `--write` to fix
formatting. Documentation-only changes normally need diff/link/path review, not
a build. Do not change dependencies to bypass first-time restore/network
failures.

In-game validation is implied and must never be explicitly referenced in
`README.md` or `docs/`. Do not add reminders, status disclaimers, or checklists
about it. Report only checks actually performed.

At handoff, report the version, changes, checks actually run, and built DLL path
when applicable.
