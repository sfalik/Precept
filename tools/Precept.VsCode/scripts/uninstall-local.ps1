$ErrorActionPreference = "Stop"

$manifestPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) "package.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$extensionId = "{0}.{1}" -f $manifest.publisher, $manifest.name

# Detect whether this terminal was launched by VS Code Insiders or stable.
$isInsiders = $env:VSCODE_GIT_ASKPASS_NODE -and ($env:VSCODE_GIT_ASKPASS_NODE -match 'Insiders')
if ($isInsiders) {
    $codeCli = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code Insiders\bin\code-insiders.cmd"
    if (-not (Test-Path $codeCli)) { $codeCli = "code-insiders" }
} else {
    $codeCli = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code\bin\code.cmd"
    if (-not (Test-Path $codeCli)) { $codeCli = "code" }
}

& $codeCli --uninstall-extension $extensionId
if ($LASTEXITCODE -ne 0) {
    throw "Extension uninstall failed with exit code $LASTEXITCODE"
}

Write-Host ""
$hostLabel = if ($isInsiders) { "VS Code Insiders" } else { "VS Code" }
Write-Host "Uninstalled $extensionId from your local $hostLabel profile."
Write-Host "Run 'Developer: Reload Window' in VS Code if this window currently has the packaged extension loaded."