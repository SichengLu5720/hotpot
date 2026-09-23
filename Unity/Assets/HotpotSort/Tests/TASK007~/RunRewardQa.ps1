param([Parameter(Mandatory=$true)][string]$Revision)
$ErrorActionPreference='Stop'
if($Revision -notmatch '^[a-z0-9-]+$'){throw 'Invalid revision'}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
$output=Join-Path $repo ".harness/qa/TASK-007/v1/$Revision"
if(Test-Path -LiteralPath $output){throw 'Use a new evidence revision'}
New-Item -ItemType Directory -Path $output | Out-Null
$results=@()
Push-Location $repo
try {
    foreach($entry in @(
        @{name='native-boundary';arguments=@('run','--project',"$PSScriptRoot/NativeRewardQa.csproj",'--',"$output/native-boundary.json")},
        @{name='adapter';arguments=@('run','--project',"$PSScriptRoot/RewardAdapterQa.csproj")},
        @{name='sdk-compile';arguments=@('build',"$PSScriptRoot/NativeSdkCompile.csproj",'--nologo')},
        @{name='coordinator';arguments=@('run','--project','Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/JointIntegrationQa.csproj','--',"$output/coordinator.json")},
        @{name='revival';arguments=@('run','--project','Unity/Assets/HotpotSort/Runtime/Presentation/Diagnostics~/RevivalRuleQa.csproj','--',"$output/revival.json")}
    )) {
        $arguments=$entry.arguments
        $log=& dotnet @arguments 2>&1
        $code=$LASTEXITCODE
        $log | Out-File -LiteralPath "$output/$($entry.name).log" -Encoding utf8
        $results+=@{name=$entry.name;exitCode=$code}
    }
    $report=@{task='TASK-007';version=1;checkpoint='HC-02-v1-Code';utc=[DateTime]::UtcNow.ToString('o');results=$results;contracts='Actual current shared contracts, no enum substitutes';limitations='SDK/engine boundary fakes and SDK public-surface compilation; no device, live ad, upload or release';sourceHash=(Get-FileHash -LiteralPath 'Unity/Assets/HotpotSort/Runtime/Platform/WeChat/WeChatRewardLifecycle.cs' -Algorithm SHA256).Hash}
    $report | ConvertTo-Json -Depth 5 | Out-File -LiteralPath "$output/report.json" -Encoding utf8
    $report | ConvertTo-Json -Depth 5
    if(@($results|Where-Object exitCode -ne 0).Count){exit 1}
} finally {Pop-Location}
exit 0
