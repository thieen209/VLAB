[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [string]$ProjectPath=(Split-Path -Parent $PSScriptRoot),
    [string]$LogPath
)

$ErrorActionPreference='Stop'
$vlabProject=(Resolve-Path -LiteralPath $ProjectPath).Path
$vlabEditor=(Resolve-Path -LiteralPath $UnityEditor).Path
if($vlabProject -match '[^\x00-\x7F]') {
    throw 'Unity Android tools require an ASCII-only project path. Build a source copy in such a folder.'
}
if(!(Test-Path -LiteralPath (Join-Path $vlabProject 'ProjectSettings\ProjectVersion.txt'))) {
    throw 'ProjectPath is not a Unity project.'
}
if(!$LogPath) { $LogPath=Join-Path $vlabProject 'Logs\Android-build.log' }
$vlabLog=[IO.Path]::GetFullPath($LogPath)
New-Item -ItemType Directory -Path (Split-Path -Parent $vlabLog) -Force | Out-Null
$vlabJavaTemp=Join-Path $vlabProject 'Library\JavaTemp'
New-Item -ItemType Directory -Path $vlabJavaTemp -Force | Out-Null
$vlabJavaTemp=$vlabJavaTemp.Replace('\','/')
$vlabPreviousJavaOptions=$env:JAVA_TOOL_OPTIONS
$vlabPreviousTemp=$env:TEMP
$vlabPreviousTmp=$env:TMP
try {
    # Set this before Unity starts: its Android tooling caches the child environment.
    # ASCII temporary paths avoid Windows JDK socket failures for accented user names.
    $env:JAVA_TOOL_OPTIONS=($vlabPreviousJavaOptions+' -Djava.io.tmpdir="'+$vlabJavaTemp+'" -Djdk.net.unixdomain.tmpdir="'+$vlabJavaTemp+'"').Trim()
    # Unity sanitizes Java option variables for Gradle, so its inherited OS temp
    # directory must also be ASCII. This changes only this launcher and its children.
    $env:TEMP=$vlabJavaTemp
    $env:TMP=$vlabJavaTemp
    $vlabArguments='-batchmode -nographics -projectPath "'+$vlabProject+'" -buildTarget Android -quit -executeMethod VLabUnifiedBuild.BuildAndroid -logFile "'+$vlabLog+'"'
    $vlabProcess=Start-Process -FilePath $vlabEditor -ArgumentList $vlabArguments -WindowStyle Hidden -Wait -PassThru
    if($vlabProcess.ExitCode -ne 0) { throw "Unity Android build failed (exit $($vlabProcess.ExitCode)). See $vlabLog" }
    Write-Output (Join-Path $vlabProject 'Builds\Android\VLAB.apk')
}
finally {
    $env:JAVA_TOOL_OPTIONS=$vlabPreviousJavaOptions
    $env:TEMP=$vlabPreviousTemp
    $env:TMP=$vlabPreviousTmp
}
