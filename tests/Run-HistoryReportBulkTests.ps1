$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
& $msbuild "$repo/OQSDrug/OQSDrug.csproj" /t:Build /p:Configuration=Debug /p:Platform=x86 /p:SignManifests=false /p:SignAssembly=false /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$output = Join-Path $repo 'OQSDrug/bin/x86/Debug/history-report-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe /platform:x86 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Data.dll "/out:$output/HistoryReportBulkTests.exe" "$PSScriptRoot/HistoryReportBulkTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/HistoryReportBulkTests.exe" "$repo/OQSDrug/bin/x86/Debug/OQSDrug.exe" $output
if ($LASTEXITCODE -ne 0) { throw 'History report / bulk tests failed.' }
