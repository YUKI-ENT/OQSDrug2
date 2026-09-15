$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
& $msbuild "$repo/OQSDrug/OQSDrug.csproj" /t:Build /p:Configuration=Debug /p:Platform=x86 /p:SignManifests=false /p:SignAssembly=false /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$output = Join-Path $repo 'OQSDrug/bin/x86/Debug/com-smoke-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
$dataReference = "${env:ProgramFiles(x86)}/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/System.Data.dll"
& $compiler /nologo /target:exe /platform:x86 /r:System.Windows.Forms.dll /r:System.Drawing.dll "/r:$dataReference" "/out:$output/DynamicsComSmokeTests.exe" "$PSScriptRoot/DynamicsComSmokeTests.cs" "$PSScriptRoot/MedicationInteractionTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/DynamicsComSmokeTests.exe" "$repo/OQSDrug/bin/x86/Debug/OQSDrug.exe" $output
if ($LASTEXITCODE -ne 0) { throw 'Smoke tests failed.' }
