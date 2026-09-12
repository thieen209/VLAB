[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$UnityEditor)
$ErrorActionPreference='Stop'
$vlabRoot=Split-Path -Parent $PSScriptRoot
$vlabApk=Join-Path $vlabRoot 'Builds\Android\VLAB.apk'
$vlabOutput=Join-Path $vlabRoot 'TestResults\Unified'
$vlabAndroid=Join-Path (Split-Path -Parent $UnityEditor) 'Data\PlaybackEngines\AndroidPlayer'
$vlabBuildTools=Join-Path $vlabAndroid 'SDK\build-tools\36.0.0'
$vlabJava=Join-Path $vlabAndroid 'OpenJDK\bin\java.exe'
if(!(Test-Path -LiteralPath $vlabApk)){throw 'Build the Android APK first.'}
$vlabSignature=& $vlabJava -jar (Join-Path $vlabBuildTools 'lib\apksigner.jar') verify --verbose --print-certs $vlabApk
if($LASTEXITCODE -ne 0){throw 'APK signature verification failed.'}
& (Join-Path $vlabBuildTools 'zipalign.exe') -c -P 16 4 $vlabApk
if($LASTEXITCODE -ne 0){throw 'APK package alignment verification failed.'}
$vlabSignature + '16 KB package alignment: passed' + ('Recorded UTC: '+[datetime]::UtcNow.ToString('o')) | Set-Content -LiteralPath (Join-Path $vlabOutput 'apk-verification.txt') -Encoding utf8
$vlabBadging=& (Join-Path $vlabBuildTools 'aapt.exe') dump badging $vlabApk
if($LASTEXITCODE -ne 0){throw 'APK manifest inspection failed.'}
$vlabBadging | Where-Object { $_ -match '^(package:|sdkVersion:|targetSdkVersion:|uses-permission:|application-label:|launchable-activity:|native-code:)' } | Set-Content -LiteralPath (Join-Path $vlabOutput 'apk-manifest.txt') -Encoding utf8
$vlabArchive=[IO.Compression.ZipFile]::OpenRead($vlabApk)
try {
    $vlabArchive.Entries | Where-Object { $_.FullName -match '(^lib/|Subsystems.*json$)' } | ForEach-Object { $_.FullName } | Set-Content -LiteralPath (Join-Path $vlabOutput 'apk-native-libraries.txt') -Encoding utf8
} finally { $vlabArchive.Dispose() }
(Get-FileHash -LiteralPath $vlabApk -Algorithm SHA256).Hash | Set-Content -LiteralPath (Join-Path $vlabOutput 'apk-sha256.txt') -Encoding ascii
Write-Output 'APK signature, package alignment, manifest and native library inventory verified.'
