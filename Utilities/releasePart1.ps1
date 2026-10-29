
# Automates WSHV release packaging:
#   1. Do a clean build as usual (post-build installs the plugin and web client dist).
#   2. Rename the DLL and plugin folder in the NINA Plugins directory
#   3. Move the versioned folder to the package repo
#   4. Run CreateNET7Manifest.ps1 to generate the manifest and zip archive
#   5. Move the generated zip into the package subfolder

[CmdletBinding()]
param(
    [Parameter(Mandatory, HelpMessage="Version string for the release (e.g. '2.3.1.0')")]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$packageName    = "NINA.Plugin.Web"
$installName    = "Web Session History Viewer"
$dllName        = "Web.NINAPlugin.dll"
$versionedName  = "$packageName-$Version"
$pluginsDir     = "C:\Users\Tom\AppData\Local\NINA\Plugins\3.0.0"
$packageRoot    = "C:\Users\Tom\source\repos\package"
$packageDir     = Join-Path $packageRoot $packageName
$manifestScript = Join-Path $packageRoot "CreateNET7Manifest.ps1"

# --- Validate prerequisites ---

$pluginFolder = Join-Path $pluginsDir $installName
if (-not (Test-Path $pluginFolder)) {
    Write-Error "Plugin folder not found: $pluginFolder"
    exit 1
}

$dllSource = Join-Path $pluginFolder $dllName
if (-not (Test-Path $dllSource)) {
    Write-Error "Plugin DLL not found: $dllSource"
    exit 1
}

foreach ($dependency in @("EmbedIO.dll", "Swan.Lite.dll")) {
    $dependencyPath = Join-Path $pluginFolder $dependency
    if (-not (Test-Path $dependencyPath)) {
        Write-Error "Dependency not found: $dependencyPath"
        exit 1
    }
}

# The plugin fails to initialize at runtime if the web client is missing.
$clientVersion = Join-Path $pluginFolder "dist\webClientVersion.json"
if (-not (Test-Path $clientVersion)) {
    Write-Error "Web client not found: $clientVersion (rebuild with network access)"
    exit 1
}

if (-not (Test-Path $packageDir)) {
    Write-Error "Package directory not found: $packageDir"
    exit 1
}

if (-not (Test-Path $manifestScript)) {
    Write-Error "Manifest script not found: $manifestScript"
    exit 1
}

$packageTarget = Join-Path $packageDir $versionedName
if (Test-Path $packageTarget) {
    Write-Error "Package folder already exists: $packageTarget"
    exit 1
}

# --- Step 2a: Rename the DLL inside the plugin folder ---

$dllDest = Join-Path $pluginFolder "$versionedName.dll"
Write-Host "Renaming DLL ..."
Write-Host "  $dllSource"
Write-Host "  -> $dllDest"
Rename-Item -Path $dllSource -NewName "$versionedName.dll"

# --- Step 2b: Rename the plugin folder ---

$versionedFolder = Join-Path $pluginsDir $versionedName
Write-Host "Renaming plugin folder ..."
Write-Host "  $pluginFolder"
Write-Host "  -> $versionedFolder"
Rename-Item -Path $pluginFolder -NewName $versionedName

# --- Step 3: Move versioned folder to the package directory ---

Write-Host "Moving plugin folder to package directory ..."
Write-Host "  $versionedFolder"
Write-Host "  -> $packageTarget"
Move-Item -Path $versionedFolder -Destination $packageTarget

# --- Step 4: Run CreateNET7Manifest.ps1 ---

$dllPath      = Join-Path $packageTarget "$versionedName.dll"
$installerUrl = "https://github.com/tcpalmer/nina.plugin.web/releases/download/v$Version/$versionedName.zip"

Write-Host "Running CreateNET7Manifest.ps1 ..."
Write-Host "  -file $dllPath"
Write-Host "  -installerUrl $installerUrl"
# The manifest script outputs the zip with a relative path, so run it from the package root.
Push-Location $packageRoot
try {
    & $manifestScript -file $dllPath -createArchive -includeAll -installerUrl $installerUrl
} finally {
    Pop-Location
}

# --- Step 5: Move the generated zip into the package subdirectory ---

$zipSource = Join-Path $packageRoot "$versionedName.zip"
$zipDest   = Join-Path $packageDir "$versionedName.zip"

if (-not (Test-Path $zipSource)) {
    Write-Error "Expected zip not found after manifest script: $zipSource"
    exit 1
}

Write-Host "Moving zip to package directory ..."
Write-Host "  $zipSource"
Write-Host "  -> $zipDest"
Move-Item -Path $zipSource -Destination $zipDest

Write-Host "`nPart 1 complete. Artifacts in: $packageDir"
