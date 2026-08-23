[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,

    [string]$Version = "1.0.0",

    [switch]$IHaveRedistributionPermission
)

$ErrorActionPreference = "Stop"

if (-not $IHaveRedistributionPermission) {
    throw "Archive non créée : confirmez d'abord l'autorisation de redistribuer les fichiers de traduction, puis utilisez -IHaveRedistributionPermission."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path
$artifactsRoot = Join-Path $repoRoot "artifacts"
$releaseRoot = Join-Path $artifactsRoot "release"
$packageName = "Suzerain-FR-Amazon-Legacy-$Version"
$stageRoot = Join-Path $artifactsRoot "package"
$stagePath = Join-Path $stageRoot $packageName
$modsPath = Join-Path $stagePath "Mods"

$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot)
$resolvedStagePath = [System.IO.Path]::GetFullPath($stagePath)
if (-not $resolvedStagePath.StartsWith($resolvedArtifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Le dossier temporaire calculé sort du dossier artifacts."
}

& (Join-Path $PSScriptRoot "build.ps1") -GamePath $resolvedGamePath -Configuration Release

$requiredTranslationFiles = @(
    "Mods\SuzerainTrad.dll",
    "Mods\SuzerainFrenchLegacy\legacy_dialogues_fr.json",
    "Mods\SuzerainTrad\Languages\French\UITranslations.json"
)

foreach ($relativePath in $requiredTranslationFiles) {
    $fullPath = Join-Path $resolvedGamePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Traduction requise introuvable : $fullPath"
    }
}

$dataTranslations = Join-Path $resolvedGamePath "Mods\SuzerainTrad\Languages\French\DataTranslations"
if (-not (Test-Path -LiteralPath $dataTranslations -PathType Container)) {
    throw "Dossier de traduction introuvable : $dataTranslations"
}

if (Test-Path -LiteralPath $stagePath) {
    Remove-Item -LiteralPath $stagePath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $modsPath | Out-Null

Copy-Item -LiteralPath (Join-Path $repoRoot "artifacts\build\SuzerainFrenchLegacy.dll") `
    -Destination $modsPath
Copy-Item -LiteralPath (Join-Path $repoRoot "artifacts\build\SuzerainFrenchDataLegacy.dll") `
    -Destination $modsPath
Copy-Item -LiteralPath (Join-Path $resolvedGamePath "Mods\SuzerainTrad.dll") `
    -Destination $modsPath
Copy-Item -LiteralPath (Join-Path $resolvedGamePath "Mods\SuzerainFrenchLegacy") `
    -Destination $modsPath `
    -Recurse
Copy-Item -LiteralPath (Join-Path $resolvedGamePath "Mods\SuzerainTrad") `
    -Destination $modsPath `
    -Recurse

Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination $stagePath
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination $stagePath
Copy-Item -LiteralPath (Join-Path $repoRoot "THIRD-PARTY-NOTICES.md") -Destination $stagePath

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
$zipPath = Join-Path $releaseRoot "$packageName.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stagePath "*") -DestinationPath $zipPath

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
$checksumPath = "$zipPath.sha256"
Set-Content -LiteralPath $checksumPath `
    -Value "$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($zipPath))" `
    -Encoding ascii

Write-Host "Archive créée : $zipPath"
Write-Host "Empreinte créée : $checksumPath"
