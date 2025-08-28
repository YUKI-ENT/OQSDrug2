param(
  [string]$Solution      = "$PSScriptRoot\..\OQSDrug.sln",
  [string]$ProjectDir    = "$PSScriptRoot\..\OQSDrug",
  [string]$Configuration = "Release",
  [string]$InnoScript    = "$PSScriptRoot\..\installer.iss",
  [switch]$SkipBuild,            # ← ここを switch に
  [string]$MsBuildPath   = ""
)

$ErrorActionPreference = 'Stop'
function Info($m){ Write-Host "[INFO] $m" -ForegroundColor Cyan }
function Warn($m){ Write-Host "[WARN] $m" -ForegroundColor Yellow }
function Err ($m){ Write-Host "[ERR ] $m" -ForegroundColor Red }

# 引数の正規化
$ProjectDir = $ProjectDir.Trim('"')
if ($ProjectDir.EndsWith('\')) { $ProjectDir = $ProjectDir.TrimEnd('\') }
$Solution   = $Solution.Trim('"')
$InnoScript = $InnoScript.Trim('"')

# パス検証
if (-not (Test-Path $Solution))   { Err "Solution not found: $Solution"; exit 1 }
if (-not (Test-Path $ProjectDir)) { Err "ProjectDir not found: $ProjectDir"; exit 1 }
if (-not (Test-Path $InnoScript)) { Err "Inno script not found: $InnoScript"; exit 1 }

# （必要なときだけ）ビルド実行
if (-not $SkipBuild) {
  if (-not $MsBuildPath -or -not (Test-Path $MsBuildPath)) {
    $guess = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $guess) { $MsBuildPath = $guess } else { $MsBuildPath = "msbuild.exe" }
  }
  Info "MSBuild: $MsBuildPath"
  Info "Build solution: $Solution ($Configuration)"
  & $MsBuildPath $Solution /m /p:Configuration=$Configuration | Out-Host
}

# バージョン抽出（InformationalVersion → AssemblyVersion → EXE ProductVersion）
$asmInfo = Join-Path $ProjectDir "Properties\AssemblyInfo.cs"
$version = ""
if (Test-Path $asmInfo) {
  $src = Get-Content $asmInfo -Raw
  $m1 = [regex]::Match($src, 'AssemblyInformationalVersion\("([^"]+)"\)')
  if ($m1.Success) { $version = $m1.Groups[1].Value }
  if (-not $version) {
    $m2 = [regex]::Match($src, 'AssemblyVersion\("([^"]+)"\)')
    if ($m2.Success) { $version = $m2.Groups[1].Value }
  }
}
if (-not $version) {
  $exePath = Join-Path $ProjectDir "bin\x86\$Configuration\OQSDrug.exe"
  if (Test-Path $exePath) {
    $fvi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
    if ($fvi.ProductVersion) { $version = $fvi.ProductVersion }
    elseif ($fvi.FileVersion) { $version = $fvi.FileVersion }
  }
}
if (-not $version) { $version = "0.0.0" }
Info "AppVersion = $version"

# ISCC 検出
$ISCC = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $ISCC)) { $ISCC = "C:\Program Files\Inno Setup 6\ISCC.exe" }
if (-not (Test-Path $ISCC)) { Err "ISCC.exe not found. Install Inno Setup 6."; exit 1 }
Info "ISCC: $ISCC"

# 出力確認（x86 固定）
$releaseDir = Join-Path $ProjectDir "bin\x86\$Configuration"
if (-not (Test-Path $releaseDir)) { Err "Release 出力が見つかりません: $releaseDir"; exit 1 }

# ---- ここから追加：本体 exe に署名 ----
$exePath = Join-Path $releaseDir "OQSDrug.exe"
if (Test-Path $exePath) {
  $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
  Info "Sign exe: $exePath"
  & $signtool sign `
    /sha1 89AA6D9BABBAAE6672A34DE9F07E47359389ACF2 `
    /s My `
    /fd SHA256 `
    /tr http://timestamp.digicert.com `
    /td SHA256 `
    "$exePath"
}

# Inno コンパイル
Info "Compile Inno script"
& "$ISCC" "/DAppVersion=$version" "$InnoScript" | Out-Host

# 生成物案内
$outDir = Join-Path (Split-Path $InnoScript -Parent) "Output"
if (Test-Path $outDir) {
  $exe = Get-ChildItem $outDir -Filter "OQSDrug_v*.exe" -ErrorAction SilentlyContinue |
         Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($exe) { Write-Host "`n✅ 完了: $($exe.FullName)" -ForegroundColor Green }
  else { Warn "Output は見つかりましたが成果物がありません: $outDir" }
} else {
  Warn "Output フォルダが見つかりません。Innoの設定をご確認ください。"
}
