param(
    [Parameter(Mandatory = $true)][string]$ArtifactDirectory,
    [ValidateSet('Debug','Staging','Release')][string]$Configuration = 'Release',
    [string]$ExpectedVersion = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
if (-not [System.IO.Path]::IsPathRooted($ArtifactDirectory)) { $ArtifactDirectory = Join-Path $repositoryRoot $ArtifactDirectory }
$ArtifactDirectory = [System.IO.Path]::GetFullPath($ArtifactDirectory)
$project = Join-Path $repositoryRoot 'Icod.Timing.csproj'
$expectedId = Get-MSBuildProperty -ProjectPath $project -Name 'PackageId' -Configuration $Configuration
if ([string]::IsNullOrWhiteSpace($expectedId)) { $expectedId = 'Icod.Timing' }
$expectedProjectVersion = Get-MSBuildProperty -ProjectPath $project -Name 'PackageVersion' -Configuration $Configuration
if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) { $ExpectedVersion = $expectedProjectVersion }
$package = @(Get-ChildItem -LiteralPath $ArtifactDirectory -Filter '*.nupkg' -File | Where-Object { -not $_.Name.EndsWith('.symbols.nupkg') })
$symbols = @(Get-ChildItem -LiteralPath $ArtifactDirectory -Filter '*.snupkg' -File)
if (1 -ne $package.Count) { throw "Expected exactly one .nupkg; found $($package.Count)." }
if (1 -ne $symbols.Count) { throw "Expected exactly one .snupkg; found $($symbols.Count)." }
$metadata = Get-PackageMetadata -PackagePath $package[0].FullName
if ($metadata.Id -ne $expectedId) { throw "Expected package '$expectedId'; found '$($metadata.Id)'." }
if ($metadata.Version -ne $ExpectedVersion) { throw "Expected version '$ExpectedVersion'; found '$($metadata.Version)'." }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($package[0].FullName)
try {
    $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\\','/') })
    foreach ($required in @('README.md','LICENSE','icon.png')) {
        if ($required -notin $names) { throw "Package is missing '$required'." }
    }
    foreach ($tfm in @('net7.0','net8.0','net9.0','net10.0')) {
        foreach ($file in @("lib/$tfm/Icod.Timing.dll","lib/$tfm/Icod.Timing.xml")) {
            if ($file -notin $names) { throw "Package is missing '$file'." }
        }
    }
} finally { $archive.Dispose() }
$symbolArchive = [System.IO.Compression.ZipFile]::OpenRead($symbols[0].FullName)
try {
    $names = @($symbolArchive.Entries | ForEach-Object { $_.FullName.Replace('\\','/') })
    foreach ($tfm in @('net7.0','net8.0','net9.0','net10.0')) {
        $file = "lib/$tfm/Icod.Timing.pdb"
        if ($file -notin $names) { throw "Symbol package is missing '$file'." }
    }
} finally { $symbolArchive.Dispose() }
Write-Host "Exact Icod.Timing package verification succeeded ($Configuration, $ExpectedVersion)."
