$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$msbuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild was not found.' }
$output = Join-Path $repo 'OQSDrug/obj/pmda-search-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path (Split-Path $msbuild) 'Roslyn/csc.exe'
$source = [IO.File]::ReadAllText((Join-Path $repo 'OQSDrug/CommonFunctions.cs'))
$start = $source.IndexOf('        public static async Task<List<Tuple<string[], double>>> FuzzySearchAsync(')
$end = $source.IndexOf('        private static async Task<List<Tuple<string[], double>>> SearchSgmlRawdataAsync(', $start)
$helperStart = $source.IndexOf('        public static string RemoveCampany(')
$helperEnd = $source.IndexOf('        public static void SaveViewerSettings(', $helperStart)
if ($start -lt 0 -or $end -lt 0 -or $helperStart -lt 0 -or $helperEnd -lt 0) { throw 'Search methods were not found.' }
# Compile production search and scoring methods; replace only the database boundary.
$methods = $source.Substring($start, $end - $start) + $source.Substring($helperStart, $helperEnd - $helperStart)
$hostSource = "using System; using System.Collections.Generic; using System.Linq; using System.Text; using System.Text.RegularExpressions; using System.Threading.Tasks;`r`ninternal static partial class SearchHost {`r`n$methods`r`n}`r`n"
$hostPath = Join-Path $output 'SearchHost.cs'
[IO.File]::WriteAllText($hostPath, $hostSource, [Text.UTF8Encoding]::new($true))
& $compiler /nologo /target:exe "/out:$output/PmdaSearchTests.exe" $hostPath "$PSScriptRoot/PmdaSearchTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'PMDA search test compilation failed.' }
& "$output/PmdaSearchTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'PMDA search tests failed.' }
