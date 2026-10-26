$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/rsb-reload-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe /platform:x86 /r:System.Net.Http.dll /r:Microsoft.CSharp.dll "/out:$output/RsbXmlReloadHttpTests.exe" "$repo/OQSDrug/RsbXmlReloadHttp.cs" "$PSScriptRoot/RsbXmlReloadHttpTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/RsbXmlReloadHttpTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'RSBase HTTP tests failed.' }
