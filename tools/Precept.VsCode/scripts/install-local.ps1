$ErrorActionPreference = "Stop"

# Ensure Node/npm is on PATH (may be missing in pwsh-based VS Code task shells).
$nodePaths = @(
    "C:\Program Files\nodejs",
    "$env:APPDATA\npm"
)
foreach ($p in $nodePaths) {
    if ((Test-Path $p) -and ($env:PATH -notlike "*$p*")) {
        $env:PATH = "$p;$env:PATH"
    }
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$extensionRoot = Resolve-Path (Join-Path $scriptDirectory "..")
Set-Location $extensionRoot

npm run package:local
if ($LASTEXITCODE -ne 0) {
    throw "Packaging failed with exit code $LASTEXITCODE"
}

$manifest = Get-Content -Raw -Path (Join-Path $extensionRoot "package.json") | ConvertFrom-Json
$vsixName = "{0}-{1}.vsix" -f $manifest.name, $manifest.version
$vsixPath = Join-Path $extensionRoot $vsixName

if (-not (Test-Path $vsixPath)) {
    throw "VSIX not found: $vsixPath"
}

# Detect whether this terminal was launched by VS Code Insiders or stable.
$isInsiders = $env:VSCODE_GIT_ASKPASS_NODE -and ($env:VSCODE_GIT_ASKPASS_NODE -match 'Insiders')
if ($isInsiders) {
    $codeCli = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code Insiders\bin\code-insiders.cmd"
    if (-not (Test-Path $codeCli)) { $codeCli = "code-insiders" }
} else {
    $codeCli = Join-Path $env:LOCALAPPDATA "Programs\Microsoft VS Code\bin\code.cmd"
    if (-not (Test-Path $codeCli)) { $codeCli = "code" }
}

& $codeCli --install-extension $vsixPath --force
if ($LASTEXITCODE -ne 0) {
    throw "Extension install failed with exit code $LASTEXITCODE"
}

Write-Host ""
$hostLabel = if ($isInsiders) { "VS Code Insiders" } else { "VS Code" }
Write-Host "Installed $vsixName into your local $hostLabel profile."
Write-Host "Run 'Developer: Reload Window' in VS Code to load the updated extension in this window."
