# Rebuilds the documentation site from scratch and serves it locally.
#
# The generated API metadata (api/*.yml) is cleared first because docfx never deletes metadata for types that were renamed or removed, and stale
# files cause invalid link warnings and ghost pages in the built site.

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host 'Clearing generated API metadata and previous site output...'
Get-ChildItem -Path 'api' -Filter '*.yml' -File | Remove-Item -Force
if (Test-Path 'api/.manifest') { Remove-Item 'api/.manifest' -Force }
if (Test-Path '_site') { Remove-Item '_site' -Recurse -Force }

# The WPF package is documented from its Release assembly (see docfx.json), so it has to be built first.
Write-Host 'Building Singulink.UI.Icons.Wpf (Release) for API metadata...'
dotnet build ../Source/Singulink.UI.Icons.Wpf/Singulink.UI.Icons.Wpf.csproj -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Generating API metadata...'
docfx metadata docfx.json
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Building site...'
docfx build docfx.json
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Serving site (Ctrl+C to stop)...'
docfx serve _site
