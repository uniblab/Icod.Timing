param(
    [Parameter(Mandatory = $true)][string]$SourceDirectory,
    [Parameter(Mandatory = $true)][string]$DestinationDirectory,
    [Parameter(Mandatory = $true)][string]$ExpectedVersion
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
foreach ($name in @('SourceDirectory','DestinationDirectory')) {
    $value = Get-Variable -Name $name -ValueOnly
    if (-not [System.IO.Path]::IsPathRooted($value)) { $value = Join-Path $repositoryRoot $value }
    Set-Variable -Name $name -Value ([System.IO.Path]::GetFullPath($value))
}
if (Test-Path -LiteralPath $DestinationDirectory) { Remove-Item -LiteralPath $DestinationDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
$selected = 0
foreach ($package in @(Get-ChildItem -LiteralPath $SourceDirectory -File | Where-Object { $_.Extension -in @('.nupkg','.snupkg') })) {
    if ($package.Extension -eq '.nupkg' -and -not $package.Name.EndsWith('.symbols.nupkg')) {
        $metadata = Get-PackageMetadata -PackagePath $package.FullName
        if ($metadata.Version -ne $ExpectedVersion) { continue }
    }
    Copy-Item -LiteralPath $package.FullName -Destination (Join-Path $DestinationDirectory $package.Name)
    $selected++
}
if (2 -ne $selected) { throw "Expected one .nupkg and one .snupkg for release $ExpectedVersion; selected $selected files." }
