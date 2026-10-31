[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [string]$MinecraftJar,
    [string]$EnigmaticLegacyPackage
)
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$tmlRoot = (Resolve-Path -LiteralPath $TModLoaderPath).Path
$runtime = Join-Path $tmlRoot 'dotnet/dotnet.exe'
$assembly = Join-Path $tmlRoot 'tModLoader.dll'
if (!(Test-Path -LiteralPath $runtime) -or !(Test-Path -LiteralPath $assembly)) {
    throw 'Choose a Windows tModLoader 1.4.4 installation containing its bundled dotnet runtime.'
}
$assets = Get-Content -LiteralPath (Join-Path $repoRoot 'external-assets.json') -Raw | ConvertFrom-Json
$missing = @($assets | Where-Object { !(Test-Path -LiteralPath (Join-Path $repoRoot $_.path)) })
if ($missing.Count -gt 0) {
    if (!$MinecraftJar) { throw 'Supply -MinecraftJar with your local Minecraft 1.20.1 client JAR. See README.md.' }
    & python (Join-Path $repoRoot 'tools/restore_external_assets.py') $MinecraftJar
    if ($LASTEXITCODE -ne 0) { throw 'Local Minecraft asset restoration failed.' }
}
$profile = Join-Path $repoRoot '.build/PortBuildProfile'
$mods = Join-Path $profile 'Mods'
$artifacts = Join-Path $repoRoot '.artifacts'
New-Item -ItemType Directory -Path $mods,$artifacts -Force | Out-Null
if (!$EnigmaticLegacyPackage) { throw 'Supply -EnigmaticLegacyPackage with EnigmaticLegacy.tmod built from the companion source repository (0.1.19+).' }
$dependencyPath = (Resolve-Path -LiteralPath $EnigmaticLegacyPackage).Path
Copy-Item -LiteralPath $dependencyPath -Destination (Join-Path $mods 'EnigmaticLegacy.tmod') -Force
$modules = @('RelicExpansions','CelestialCore','CelestialArtifacts')
Push-Location -LiteralPath $tmlRoot
try {
    foreach ($name in $modules) {
        $source = Join-Path $repoRoot ('ModSources/' + $name)
        & $runtime $assembly -server -build $source -tmlsavedirectory $profile
        if ($LASTEXITCODE -ne 0) { throw ('Build failed: ' + $name) }
        Copy-Item -LiteralPath (Join-Path $mods ($name + '.tmod')) -Destination $artifacts -Force
    }
} finally { Pop-Location }
Write-Output ('Built packages: ' + $artifacts)
