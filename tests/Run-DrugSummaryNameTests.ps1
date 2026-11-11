$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
$msbuild = & "$programFilesX86/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/drug-summary-name-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
$vb = Join-Path $programFilesX86 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Microsoft.VisualBasic.dll'
& $compiler /nologo /target:exe "/r:$vb" "/out:$output/DrugSummaryNameTests.exe" "$repo/OQSDrug/DrugSummaryName.cs" "$PSScriptRoot/DrugSummaryNameTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/DrugSummaryNameTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Drug summary name tests failed.' }
