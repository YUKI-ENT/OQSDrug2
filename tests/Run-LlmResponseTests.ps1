$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
$msbuild = & "$programFilesX86/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
& $msbuild "$repo/OQSDrug.sln" /t:Build /p:Configuration=Debug /p:Platform=x86 /p:SignAssembly=false /p:SignManifests=false /v:quiet /nologo /clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$output = Join-Path $repo 'OQSDrug/obj/llm-response-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe /platform:x86 /r:System.Data.dll /r:System.Web.Extensions.dll "/out:$output/LlmResponseTests.exe" "$PSScriptRoot/LlmResponseTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/LlmResponseTests.exe" "$repo/OQSDrug/bin/x86/Debug"
if ($LASTEXITCODE -ne 0) { throw 'LLM response tests failed.' }
