# Downloads dist-{VERSION}.zip from the latest published release of the web client and
# unpacks it into <PluginDir>\dist. Called from the PostBuild target in NINA.Plugin.Web.csproj.
param([Parameter(Mandatory = $true)][string]$PluginDir)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$release = Invoke-RestMethod -Uri "https://api.github.com/repos/tcpalmer/nina.plugin.web.client/releases/latest" -Headers @{ "User-Agent" = "NINA.Plugin.Web-build" }
$asset = $release.assets | Where-Object { $_.name -like "dist-*.zip" } | Select-Object -First 1
if (-not $asset) { throw "No dist-*.zip asset found in latest web client release $($release.tag_name)" }

Write-Host "Downloading $($asset.name) from web client release $($release.tag_name)"
$zip = Join-Path $PluginDir $asset.name
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing

# The zip contents are the dist files themselves, so extract into a dist folder
$dist = Join-Path $PluginDir "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $dist -Force
Remove-Item $zip -Force
