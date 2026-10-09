param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\dist')
)

$ErrorActionPreference = 'Stop'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$packageDirectory = Join-Path $outputRoot 'ZonyLrcTools-Windows-GUI-x64'
$project = Join-Path $sourceRoot 'src\ZonyLrcTools.Desktop\ZonyLrcTools.Desktop.csproj'
New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null

& $Dotnet publish $project -c Release -r win-x64 --self-contained true `
    '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' `
    '-p:DebugType=None' '-p:DebugSymbols=false' -o $packageDirectory
if ($LASTEXITCODE -ne 0) { throw "GUI 发布失败，退出码：$LASTEXITCODE" }

Copy-Item -LiteralPath (Join-Path $sourceRoot 'LICENSE') -Destination $packageDirectory
Copy-Item -LiteralPath (Join-Path $sourceRoot 'WINDOWS-GUI.md') -Destination (Join-Path $packageDirectory '使用说明.md')
Compress-Archive -Path $packageDirectory -DestinationPath (Join-Path $outputRoot 'ZonyLrcTools-Windows-GUI-x64.zip') -Force
Get-ChildItem -LiteralPath $outputRoot | Select-Object Name, Length
