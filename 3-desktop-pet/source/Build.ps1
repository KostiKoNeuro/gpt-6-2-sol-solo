$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The Windows .NET Framework C# compiler was not found.'
}

$sourceDir = Join-Path $PSScriptRoot 'src'
$assetDir = Join-Path $PSScriptRoot 'assets'
$iconPath = Join-Path $assetDir 'pip.ico'
$outputPath = Join-Path $PSScriptRoot 'Pip.exe'
$sources = Get-ChildItem -LiteralPath $sourceDir -Filter '*.cs' | ForEach-Object FullName
$sprites = Get-ChildItem -LiteralPath (Join-Path $assetDir 'sprites') -Filter '*.png'
$references = '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll'
$resources = $sprites | ForEach-Object { "/resource:$($_.FullName),Pip.Art.$($_.Name)" }
$manifest = Join-Path $sourceDir 'Pip.manifest'
$options = '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/warnaserror+', "/out:$outputPath"
& $compiler @options @references "/win32manifest:$manifest" "/win32icon:$iconPath" "/resource:$iconPath,Pip.Art.pip.ico" @resources @sources
if ($LASTEXITCODE -ne 0) { throw "Pip could not be built (compiler exit $LASTEXITCODE)." }
Write-Output "Built $outputPath"
