# Working on MoreSailwindSails

Read [README.md](README.md), [src/Plugin.cs](src/Plugin.cs), relevant code and
the applicable sections of [DEVELOPMENT.md](docs/DEVELOPMENT.md) before editing.
Check `git status --short` and preserve user changes. Current user instructions
take precedence over historical design choices.

## Scope and workflow

- Use named arguments rather than positional arguments whenever possible to
  improve readability.
- Prefer one class per file as a rule and describe the responsibility of the
  class within a code comment at the top of the file. Stay/sail feature patches
  are an exception: group their top-level patch classes by integration
  responsibility in one file per integration within each feature's `Patches/`
  directory. Include a file responsibility comment and class responsibility
  comments.
- Update `README.md` only when the user explicitly requests a README change.
  Feature, bug-fix, release and general documentation work do not imply
  permission to edit it; put technical and validation updates in
  `docs/DEVELOPMENT.md`.
- Implementation requests authorize editing, building and checking. Do not
  commit, push, alter saves or replace installed game files as part of a build.
  **Never execute files from `scripts/` or use it as the working directory.**
- Use installed assemblies/assets as the behavioral reference; upstream source
  may differ. Never commit proprietary assemblies or extracted game assets.

## Guidlines

- Always drive to simplicity. Choices should be made preserve straight forward
  code paths with minimal complexity.
- Avoid patching external dependencies.
- Ask for clarification from the user if directives appear to require complex
  implementation.
- Resist changes that would break save compatability relative to the last tagged
  release.
- Record technical and architectual decisions in
  [DEVELOPMENT](docs/DEVELOPMENT.md). Write clearly and concisely. When adding
  to the document on a subject already present, squash the content to eliminate
  duplication.

## Verification and handoff

Use the pinned Nix formatting environment and the
[build and check commands](docs/DEVELOPMENT.md#build-and-automated-checks).
Always build Release for plugin-affecting changes and run both check suites. Use
CSharpier `check` for read-only work and `format` to fix formatting. For
Markdown, use Prettier `--check` to verify and `--write` to fix formatting.
Documentation-only changes normally need diff/link/path review, not a build. Do
not change dependencies to bypass first-time restore/network failures.

Report automated results separately from observed game behavior. Follow the
[runtime validation guidance](docs/DEVELOPMENT.md#runtime-validation) and keep
validation notes current.

At handoff, report the version, changes, checks actually run, remaining in-game
uncertainty and built DLL path when applicable.
