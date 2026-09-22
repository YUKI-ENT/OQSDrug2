$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/auto-llm-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe /r:System.Net.Http.dll "/out:$output/AutoLlmTests.exe" "$repo/OQSDrug/AutoLlmQueue.cs" "$repo/OQSDrug/LlmRetryPolicy.cs" "$PSScriptRoot/AutoLlmTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/AutoLlmTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Auto LLM tests failed.' }
