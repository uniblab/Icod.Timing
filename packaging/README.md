# Icod.Timing build and distribution tooling

`Icod.Timing` follows the canonical Icod C#/.NET lifecycle while preserving its four-framework library package contract.

| Lifecycle | Configuration | Entry point |
| --- | --- | --- |
| local `build.cmd` / `build.sh` | `Debug` | `packaging/Invoke-Build.ps1` |
| pull request | `Staging` | `.github/workflows/pull-request.yaml` |
| push to `main` | `Release` | `.github/workflows/main.yaml` |
| manual diagnostic | selected | `.github/workflows/distribution-validation.yaml` |
| `v*` tag contained in `main` | `Release` | `.github/workflows/release.yaml` |

The package verifier requires the exact generated `Icod.Timing` `.nupkg` and matching `.snupkg`, including README, LICENSE, icon, DLL/XML assets for `net7.0`, `net8.0`, `net9.0`, and `net10.0`, and PDB symbol payloads for all four frameworks.

Ordinary pushes to `main` validate but never publish. Tagged releases publish the same exact validated package to NuGet.org and GitHub Packages in parallel, then create the GitHub Release.
