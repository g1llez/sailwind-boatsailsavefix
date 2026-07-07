param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Sailwind"
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Icon = Join-Path $Root "icon.png"
$DllBuilt = Join-Path $Root "bin\Release\netstandard2.0\BoatSailSaveFix.dll"
$ZipPath = Join-Path $Root "g1llez-BoatSailSaveFix-1.0.0.zip"
$Staging = Join-Path $Root "_staging"

& dotnet build (Join-Path $Root "BoatSailSaveFix.csproj") -c Release -p:SailwindDir="$GameDir"

if (-not (Test-Path $Icon)) {
    throw "Missing icon.png (256x256) at repo root."
}

if (-not (Test-Path $DllBuilt)) {
    throw "No BoatSailSaveFix.dll found after build."
}

foreach ($f in @("manifest.json", "README.md", "CHANGELOG.md")) {
    if (-not (Test-Path (Join-Path $Root $f))) {
        throw "Missing $f"
    }
}

if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }
if (Test-Path $Staging) { Remove-Item -Recurse -Force $Staging }

$PluginStaging = Join-Path $Staging "BoatSailSaveFix"
New-Item -ItemType Directory -Force -Path $PluginStaging | Out-Null
Copy-Item -Force $DllBuilt (Join-Path $PluginStaging "BoatSailSaveFix.dll")
Copy-Item -Force (Join-Path $Root "manifest.json") $Staging
Copy-Item -Force (Join-Path $Root "README.md") $Staging
Copy-Item -Force (Join-Path $Root "CHANGELOG.md") $Staging
Copy-Item -Force $Icon $Staging

Push-Location $Staging
try {
    Compress-Archive -Path ".\*" -DestinationPath $ZipPath -CompressionLevel Optimal -Force
} finally {
    Pop-Location
}

Remove-Item -Recurse -Force $Staging

Write-Host "DLL source: $DllBuilt"
Write-Host "Thunderstore zip: $ZipPath"
Write-Host "Upload: https://thunderstore.io/package/create/ (community: Sailwind)"
