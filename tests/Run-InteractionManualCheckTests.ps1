$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/interaction-manual-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
# Use the actual snapshot and confirmation models; replace only external dependencies.
$source = Get-Content "$repo/OQSDrug/MedicationInteractionCheck.cs" -Raw -Encoding UTF8
$modelEnd = $source.IndexOf('    internal static class MedicationInteractionCheck')
if ($modelEnd -lt 0) { throw 'Interaction models were not found.' }
$models = $source.Substring(0, $modelEnd).Replace('using Npgsql;', '') + "`n}"
Set-Content "$output/Models.cs" $models -Encoding UTF8
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe /r:System.Windows.Forms.dll /r:System.Data.dll "/out:$output/InteractionManualCheckTests.exe" "$output/Models.cs" "$repo/OQSDrug/Form1.InteractionCheck.cs" "$PSScriptRoot/InteractionManualCheckTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/InteractionManualCheckTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Manual interaction tests failed.' }
