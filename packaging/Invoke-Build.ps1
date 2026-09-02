param(
    [ValidateSet('all','clean','restore','build','test','pack','validate')][string]$Section = 'all',
    [ValidateSet('Debug','Staging','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
$solution = Get-RepositorySolution -RepositoryRoot $repositoryRoot
$project = Join-Path $repositoryRoot 'Icod.Timing.csproj'
$artifacts = Join-Path $repositoryRoot 'artifacts'
function Clean { Invoke-DotNet -Arguments @('clean',$solution,'-c',$Configuration) }
function Restore { Invoke-DotNet -Arguments @('restore',$solution) }
function Build { Invoke-DotNet -Arguments @('build',$solution,'-c',$Configuration,'--no-restore') }
function Test { Invoke-DotNet -Arguments @('test',$solution,'-c',$Configuration,'--no-build','--no-restore') }
function Pack { New-Item -ItemType Directory -Path $artifacts -Force | Out-Null; Invoke-DotNet -Arguments @('pack',$project,'-c',$Configuration,'--no-build','--no-restore','-o',$artifacts) }
function Validate { & (Join-Path $PSScriptRoot 'VerifyPackageArtifact.ps1') -ArtifactDirectory $artifacts -Configuration $Configuration }
switch ($Section) {
    'all' { Clean; Restore; Build; Test; Pack; Validate }
    'clean' { Clean }
    'restore' { Restore }
    'build' { Build }
    'test' { Test }
    'pack' { Pack }
    'validate' { Validate }
}
