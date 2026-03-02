$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$extensionRoot = Resolve-Path (Join-Path $scriptDirectory "..")
Set-Location $extensionRoot

npm run package:local

$manifest = Get-Content -Raw -Path (Join-Path $extensionRoot "package.json") | ConvertFrom-Json
$vsixName = "{0}-{1}.vsix" -f $manifest.name, $manifest.version
$vsixPath = Join-Path $extensionRoot $vsixName

if (-not (Test-Path $vsixPath)) {
    throw "VSIX not found: $vsixPath"
}

code --install-extension $vsixPath --force

Write-Host ""
Write-Host "Installed $vsixName into your local VS Code profile."
Write-Host "Run 'Developer: Reload Window' in VS Code to load the updated extension in this window."
