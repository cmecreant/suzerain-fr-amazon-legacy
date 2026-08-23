[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = "Stop"
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path

$requiredFiles = @(
    "Mods\SuzerainTrad.dll",
    "Mods\SuzerainFrenchLegacy.dll",
    "Mods\SuzerainFrenchDataLegacy.dll",
    "Mods\SuzerainFrenchLegacy\legacy_dialogues_fr.json",
    "Mods\SuzerainTrad\Languages\French\UITranslations.json"
)

$allPresent = $true
foreach ($relativePath in $requiredFiles) {
    $present = Test-Path -LiteralPath (Join-Path $resolvedGamePath $relativePath) -PathType Leaf
    [pscustomobject]@{
        Fichier = $relativePath
        Présent = $present
    }
    $allPresent = $allPresent -and $present
}

$dataPath = Join-Path $resolvedGamePath "Mods\SuzerainTrad\Languages\French\DataTranslations"
$dataFileCount = if (Test-Path -LiteralPath $dataPath) {
    @(Get-ChildItem -LiteralPath $dataPath -Filter "*.txt" -File).Count
} else {
    0
}

Write-Host "Dictionnaires de données : $dataFileCount"

$latestLog = Join-Path $resolvedGamePath "MelonLoader\Latest.log"
if (Test-Path -LiteralPath $latestLog -PathType Leaf) {
    Select-String -LiteralPath $latestLog -Pattern @(
        "Traductions UI chargées",
        "fiches françaises chargés",
        "Adaptateur Amazon actif",
        "Dialogues français appliqués",
        "Informations françaises appliquées"
    ) | ForEach-Object { $_.Line }
}

if (-not $allPresent -or $dataFileCount -eq 0) {
    throw "Installation incomplète."
}

Write-Host "Installation complète."
