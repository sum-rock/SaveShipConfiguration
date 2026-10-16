#!/usr/bin/env bash
# Rebuild and install the local Release plugin into a Sailwind installation.
set -euo pipefail

repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
game_dir="${1:-$HOME/.local/share/Steam/steamapps/common/Sailwind}"
dll="$repo_dir/src/bin/Release/netstandard2.0/SaveShipConfiguration.dll"

if [[ $# -gt 1 ]]; then
    printf 'Usage: %s [Sailwind directory]\n' "$0" >&2
    exit 1
fi

if [[ ! -f "$dll" ]]; then
    printf 'Release DLL not found. Build first from the repository root: nix develop path:. -c dotnet build -c Release\n' >&2
    exit 1
fi

if [[ ! -d "$game_dir/BepInEx/plugins" ]]; then
    printf 'BepInEx plugins directory not found: %s\n' "$game_dir/BepInEx/plugins" >&2
    exit 1
fi

# Resolve a relative game path before switching to the repository root.
game_dir="$(cd -- "$game_dir" && pwd)"
destination="$game_dir/BepInEx/plugins/SaveShipConfiguration/SaveShipConfiguration.dll"

cd -- "$repo_dir"
nix develop path:. -c dotnet build src/SaveShipConfiguration.csproj -c Release -t:Rebuild "-p:SailwindDir=$game_dir"
install -Dm644 -- "$dll" "$destination"
printf 'Installed %s\n' "$destination"
