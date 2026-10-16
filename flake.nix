{
  description = "Development environment for the SaveShipConfiguration mod";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-26.05";

  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        packages = [
          pkgs.dotnet-sdk_8
          pkgs.ilspycmd
          pkgs.pre-commit
          pkgs.prettier
          pkgs.python3
          pkgs.ruff
        ];
      };
    };
}
