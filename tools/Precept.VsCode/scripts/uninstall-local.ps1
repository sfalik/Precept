$ErrorActionPreference = "Stop"

$manifestPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) "package.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$extensionId = "{0}.{1}" -f $manifest.publisher, $manifest.name

$codeCli = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code\bin\code.cmd"
if (-not (Test-Path $codeCli)) {
    $codeCli = "code"
}

& $codeCli --uninstall-extension $extensionId
if ($LASTEXITCODE -ne 0) {
    throw "Extension uninstall failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Uninstalled $extensionId from your local VS Code profile."
Write-Host "Run 'Developer: Reload Window' in VS Code if this window currently has the packaged extension loaded."