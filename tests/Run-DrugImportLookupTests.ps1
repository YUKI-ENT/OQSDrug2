$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/drug-import-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
& $compiler /nologo /target:exe "/out:$output/DrugImportLookupTests.exe" "$repo/OQSDrug/DrugImportLookup.cs" "$PSScriptRoot/DrugImportLookupTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& "$output/DrugImportLookupTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Import lookup tests failed.' }

# Compile the production diagnosis import method with a fake DB, without a UI or real database.
$source = [IO.File]::ReadAllText((Join-Path $repo 'OQSDrug/Form1.cs'))
$start = $source.IndexOf('        private async Task<int> ProcessSinryoInfoAsync(')
$end = $source.IndexOf('        /// <summary>', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Diagnosis import method was not found.' }
$method = $source.Substring($start, $end - $start).Replace('private async', 'public async')
$hostSource = "using System; using System.Collections.Generic; using System.Data; using System.Data.Common; using System.Threading.Tasks; using System.Xml; using OQSDrug;`r`ninternal partial class SinryoImportTestHost {`r`n$method`r`n}`r`n"
$hostPath = Join-Path $output 'SinryoImportTestHost.cs'
[IO.File]::WriteAllText($hostPath, $hostSource, [Text.UTF8Encoding]::new($true))
& $compiler /nologo /target:exe /r:Microsoft.CSharp.dll "/out:$output/SinryoImportCacheTests.exe" $hostPath "$repo/OQSDrug/ResImportTiming.cs" "$PSScriptRoot/SinryoImportCacheTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Diagnosis cache test compilation failed.' }
& "$output/SinryoImportCacheTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Diagnosis cache tests failed.' }
