
param(
    [string]$GtaVPath = "$env:ProgramFiles\Rockstar Games\Grand Theft Auto V",
    [string]$ScriptHookVDotNetPath = ""
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "src\OfflineInteriors\OfflineInteriors.csproj"
$OutputDirectory = Join-Path $ProjectRoot "artifacts\Release"
$BuildConfiguration = "Release"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " OfflineInteriors Build" -ForegroundColor Cyan
Write-Host " Story Mode Edition" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if ([string]::IsNullOrWhiteSpace($ScriptHookVDotNetPath)) {
    $ScriptHookVDotNetPath = Join-Path $GtaVPath "scripts"
}

$ScriptHookDll = Join-Path $ScriptHookVDotNetPath "ScriptHookVDotNet3.dll"

if (-not (Test-Path $ScriptHookDll)) {
    Write-Host "ScriptHookVDotNet3.dll was not found." -ForegroundColor Yellow
    Write-Host "Expected location: $ScriptHookDll" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "If your installation is elsewhere, pass the correct paths:"
    Write-Host '.\build.ps1 -GtaVPath "D:\Games\GTAV"'
    Write-Host 'or'
    Write-Host '.\build.ps1 -ScriptHookVDotNetPath "D:\Games\GTAV\scripts"'
    throw "Cannot build without the required scripting reference."
}

Write-Host "Project: $ProjectFile" -ForegroundColor Gray
Write-Host "GTA V: $GtaVPath" -ForegroundColor Gray
Write-Host "ScriptHookVDotNet: $ScriptHookDll" -ForegroundColor Gray
Write-Host ""

dotnet build $ProjectFile `
    --configuration $BuildConfiguration `
    --property:GtaVPath="$GtaVPath" `
    --property:ScriptHookVDotNetPath="$ScriptHookVDotNetPath"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Fix the compiler errors before continuing."
}

$BuiltDll = Join-Path $ProjectRoot "src\OfflineInteriors\bin\Release\OfflineInteriors.dll"

if (-not (Test-Path $BuiltDll)) {
    throw "Build succeeded, but the expected DLL was not found: $BuiltDll"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

Copy-Item -Path $BuiltDll `
    -Destination (Join-Path $OutputDirectory "OfflineInteriors.dll") `
    -Force

# Stage the runtime data beside the release artifact so it can be copied to
# the GTA V root as OfflineInteriors\data\interiors.json during installation.
$DataOutputDirectory = Join-Path $OutputDirectory "OfflineInteriors\data"
New-Item -ItemType Directory -Path $DataOutputDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $ProjectRoot "data\interiors.json") `
    -Destination (Join-Path $DataOutputDirectory "interiors.json") -Force

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Release DLL: $OutputDirectory\OfflineInteriors.dll" -ForegroundColor Cyan
Write-Host ""
Write-Host "The DLL has been staged, not installed into GTA V." -ForegroundColor Yellow
