
# Automates WSHV release phase 3: update the NINA plugin manifests fork and push a branch.
#   1. Sync the fork with upstream
#   2. Update the local clone to match the fork's main branch
#   3. Create a new feature branch
#   4. Replace the existing manifest with the new one
#   5. Commit the changes
#   6. Push the branch to the fork (user creates PR manually)

[CmdletBinding()]
param(
    [Parameter(Mandatory, HelpMessage="Version string for the release (e.g. '2.3.1.0')")]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$forkRepo       = "tcpalmer/nina.plugin.manifests"
$manifestsRepo  = "C:\Users\Tom\source\repos\nina.plugin.manifests"
$manifestDir    = "$manifestsRepo\manifests\w\Web Session History Viewer\3.0.0"
$sourceManifest = "C:\Users\Tom\source\repos\package\manifest.json"
$destManifest   = "$manifestDir\manifest.json"
$branch         = "feature/Web_Session_History_Viewer-$Version"

# --- Validate prerequisites ---

if (-not (Test-Path $manifestsRepo)) {
    Write-Error "Manifests repo not found: $manifestsRepo"
    exit 1
}

if (-not (Test-Path $sourceManifest)) {
    Write-Error "manifest.json not found: $sourceManifest (run releasePart1.ps1 first)"
    exit 1
}

# The package root manifest.json is shared by all plugins, so make sure it is this one.
$manifest = Get-Content $sourceManifest -Raw | ConvertFrom-Json
$manifestVersion = "$($manifest.Version.Major).$($manifest.Version.Minor).$($manifest.Version.Patch).$($manifest.Version.Build)"
if ($manifest.Name -ne "Web Session History Viewer" -or $manifestVersion -ne $Version) {
    Write-Error "manifest.json is for '$($manifest.Name)' $manifestVersion, expected 'Web Session History Viewer' $Version"
    exit 1
}

if (-not (Test-Path $manifestDir)) {
    Write-Error "Manifest directory not found: $manifestDir"
    exit 1
}

# --- Step 1: Sync the fork with upstream ---

Write-Host "Syncing fork with upstream ..."
gh repo sync $forkRepo
Write-Host ""

# --- Step 2: Switch to main and pull ---

Write-Host "Updating local clone ..."
git -C $manifestsRepo checkout main
git -C $manifestsRepo pull
Write-Host ""

# --- Step 3: Create and switch to the feature branch ---

Write-Host "Creating branch $branch ..."
git -C $manifestsRepo checkout -b $branch
Write-Host ""

# --- Step 4: Replace the existing manifest ---

Write-Host "Copying manifest.json -> $destManifest ..."
Copy-Item -Path $sourceManifest -Destination $destManifest -Force
Write-Host ""

# --- Step 5: Commit ---

Write-Host "Committing ..."
git -C $manifestsRepo add --all
git -C $manifestsRepo commit -m "Web Viewer $Version"
Write-Host ""

# --- Step 6: Push to fork ---

Write-Host "Pushing $branch to fork ..."
git -C $manifestsRepo push origin $branch
Write-Host ""

Write-Host "Part 3 complete. Review the branch and create the PR at:"
Write-Host "  https://github.com/$forkRepo/compare/$branch"
