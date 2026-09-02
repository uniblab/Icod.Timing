param(
    [ValidateSet('Debug','Staging','Release')][string]$Configuration = 'Release',
    [string]$GitHubOutputPath = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Import-Module (Join-Path $PSScriptRoot 'RepositoryTools.psm1') -Force
$solutionPath = Get-RepositorySolution -RepositoryRoot $repositoryRoot
$result = [ordered]@{ RepositoryRoot = $repositoryRoot; HasSolution = $true; SolutionPath = $solutionPath; HasExecutables = $false }
if (-not [string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
    "has_solution=true" >> $GitHubOutputPath
    "solution_path=$solutionPath" >> $GitHubOutputPath
    "has_executables=false" >> $GitHubOutputPath
}
[pscustomobject]$result
