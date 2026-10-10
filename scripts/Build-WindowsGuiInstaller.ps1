param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\dist'),
    [string]$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = 'Stop'
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$packageRoot = Join-Path $outputRoot 'ZonyLrcTools-Windows-GUI-x64'
$zipPath = Join-Path $outputRoot 'ZonyLrcTools-Windows-GUI-x64.zip'
if (!(Test-Path -LiteralPath (Join-Path $packageRoot 'ZonyLrcTools.Desktop.exe')) -or
    !(Test-Path -LiteralPath $zipPath)) { throw 'Run Publish-WindowsGui.ps1 first.' }
if (!(Test-Path -LiteralPath $Iscc)) { throw "Inno Setup compiler not found: $Iscc" }

& $Iscc "/DAppVersion=$Version" "/DPackageDir=$packageRoot" "/DOutputDir=$outputRoot" `
    (Join-Path $PSScriptRoot 'windows-gui.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed: $LASTEXITCODE" }

$setupPath = Join-Path $outputRoot "ZonyLrcTools-Windows-GUI-x64-Setup-$Version.exe"
if (!(Test-Path -LiteralPath $setupPath)) { throw 'Installer output is missing.' }
$hashLines = foreach ($assetPath in @($zipPath, $setupPath)) {
    $hash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($assetPath))"
}
$hashLines | Set-Content -LiteralPath (Join-Path $outputRoot 'SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath $zipPath, $setupPath | Select-Object Name, Length
