
# Automates WSHV release step 2: create the GitHub release.
#   - Tags the repo with vVERSION and creates the release
#   - Extracts the top bullet-point block from CHANGELOG.md as the release description
#   - Attaches the plugin zip produced by releasePart1.ps1
#   - GitHub auto-generates source code zip and tar.gz for every tagged release

[CmdletBinding()]
param(
    [Parameter(Mandatory, HelpMessage="Version string for the release (e.g. '2.3.1.0')")]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$packageName   = "NINA.Plugin.Web"
$versionedName = "$packageName-$Version"
$tag           = "v$Version"
$repo          = "tcpalmer/nina.plugin.web"
$packageDir    = "C:\Users\Tom\source\repos\package\$packageName"
$zipPath       = Join-Path $packageDir "$versionedName.zip"
$changelogPath = Join-Path $PSScriptRoot "..\CHANGELOG.md"

# --- Validate prerequisites ---

if (-not (Test-Path $zipPath)) {
    Write-Error "Release zip not found: $zipPath"
    exit 1
}

if (-not (Test-Path $changelogPath)) {
    Write-Error "CHANGELOG.md not found: $changelogPath"
    exit 1
}

# --- Extract the top bullet-point block from CHANGELOG.md ---

$notes = [System.Collections.Generic.List[string]]::new()
$inBlock = $false

foreach ($line in (Get-Content $changelogPath)) {
    if ($line -match '^## ') {
        if ($inBlock) { break }
        # Guard against publishing the notes of a different version.
        if ($line -notmatch "^## $([regex]::Escape($Version))\b") {
            Write-Error "Top CHANGELOG.md entry is not for $Version : $line"
            exit 1
        }
        $inBlock = $true
        continue
    }
    if ($inBlock) {
        $notes.Add($line)
    }
}

# Trim leading/trailing blank lines
while ($notes.Count -gt 0 -and $notes[0].Trim() -eq '')              { $notes.RemoveAt(0) }
while ($notes.Count -gt 0 -and $notes[$notes.Count - 1].Trim() -eq '') { $notes.RemoveAt($notes.Count - 1) }

if ($notes.Count -eq 0) {
    Write-Error "No release notes found in CHANGELOG.md for $Version"
    exit 1
}

$releaseNotes = $notes -join "`n"

# --- Write notes to a temp file so gh receives them verbatim ---

$tempNotes = [System.IO.Path]::GetTempFileName()
try {
    [System.IO.File]::WriteAllText($tempNotes, $releaseNotes, [System.Text.Encoding]::UTF8)

    Write-Host "Creating GitHub release $tag ..."
    Write-Host "  Repo:    $repo"
    Write-Host "  Zip:     $zipPath"
    Write-Host "  Notes:`n$releaseNotes`n"

    gh release create $tag `
        --repo $repo `
        --title $tag `
        --latest `
        --notes-file $tempNotes `
        $zipPath

    Write-Host "`nPart 2 complete. Release $tag created at: https://github.com/$repo/releases/tag/$tag"
} finally {
    Remove-Item $tempNotes -ErrorAction SilentlyContinue
}
