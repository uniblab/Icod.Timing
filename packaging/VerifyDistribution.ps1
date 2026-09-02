param([ValidateSet('Debug','Staging','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
$solution = Get-RepositorySolution -RepositoryRoot $repositoryRoot
$project = Join-Path $repositoryRoot 'Icod.Timing.csproj'
$packages = Join-Path $repositoryRoot 'artifacts/distribution-validation'
if (Test-Path -LiteralPath $packages) { Remove-Item -LiteralPath $packages -Recurse -Force }
New-Item -ItemType Directory -Path $packages -Force | Out-Null
Invoke-DotNet -Arguments @('restore',$solution)
Invoke-DotNet -Arguments @('build',$solution,'-c',$Configuration,'--no-restore','-p:ContinuousIntegrationBuild=true')
Invoke-DotNet -Arguments @('test',$solution,'-c',$Configuration,'--no-build','--no-restore','--logger','trx')
Invoke-DotNet -Arguments @('pack',$project,'-c',$Configuration,'--no-build','--no-restore','-o',$packages,'-p:ContinuousIntegrationBuild=true')
& (Join-Path $PSScriptRoot 'VerifyPackageArtifact.ps1') -ArtifactDirectory $packages -Configuration $Configuration
