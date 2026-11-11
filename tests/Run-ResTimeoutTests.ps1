$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/res-timeout-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
$source = [IO.File]::ReadAllText((Join-Path $repo 'OQSDrug/Form1.cs'))
$start = $source.IndexOf('        private async Task<bool> ExpireMissingResponseAsync(')
$end = $source.IndexOf('        private async Task ReloadRsbXmlAsync(', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Timeout method was not found.' }
$method = $source.Substring($start, $end - $start).Replace('private async', 'public async')
$hostSource = "using System; using System.Collections.Generic; using System.Data; using System.Data.Common; using System.IO; using System.Linq; using System.Threading.Tasks;`r`ninternal partial class TimeoutTestHost {`r`n$method`r`n}`r`n"
$hostPath = Join-Path $output 'TimeoutTestHost.cs'
[IO.File]::WriteAllText($hostPath, $hostSource, [Text.UTF8Encoding]::new($true))
& $compiler /nologo /target:exe "/out:$output/ResTimeoutTests.exe" $hostPath "$PSScriptRoot/ResTimeoutTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Timeout test compilation failed.' }
& "$output/ResTimeoutTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Timeout tests failed.' }
