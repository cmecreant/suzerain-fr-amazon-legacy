[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GamePath,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedGamePath = (Resolve-Path -LiteralPath $GamePath).Path

$requiredFiles = @(
    "Suzerain.exe",
    "MelonLoader\net6\MelonLoader.dll",
    "MelonLoader\net6\0Harmony.dll",
    "Suzerain_Data\Managed\Assembly-CSharp-firstpass.dll",
    "Suzerain_Data\Managed\Newtonsoft.Json.dll",
    "Suzerain_Data\Managed\UnityEngine.CoreModule.dll"
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $resolvedGamePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Fichier requis introuvable : $fullPath"
    }
}

$projects = @(
    "src\SuzerainFrenchLegacy\SuzerainFrenchLegacy.csproj",
    "src\SuzerainFrenchDataLegacy\SuzerainFrenchDataLegacy.csproj"
)

foreach ($project in $projects) {
    $projectPath = Join-Path $repoRoot $project
    dotnet build $projectPath `
        --configuration $Configuration `
        --nologo `
        --verbosity minimal `
        "-p:GamePath=$resolvedGamePath"

    if ($LASTEXITCODE -ne 0) {
        throw "Échec de la compilation de $project"
    }
}

$outputDirectory = Join-Path $repoRoot "artifacts\build"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$outputs = @(
    "src\SuzerainFrenchLegacy\bin\$Configuration\netstandard2.1\SuzerainFrenchLegacy.dll",
    "src\SuzerainFrenchDataLegacy\bin\$Configuration\netstandard2.1\SuzerainFrenchDataLegacy.dll"
)

foreach ($relativePath in $outputs) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $relativePath) `
        -Destination $outputDirectory `
        -Force
}

Write-Host "Compilation terminée : $outputDirectory"
