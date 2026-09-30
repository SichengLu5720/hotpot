param([Parameter(Mandatory=$true)][string]$EvidenceName)
$ErrorActionPreference='Stop'
if ($EvidenceName -notmatch '^[a-z0-9-]+$') { throw 'Invalid evidence name' }
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../../..'))
$outDir=Join-Path $repo ".harness/qa/TASK-007/v1/qa-r001/$EvidenceName"
if(Test-Path -LiteralPath $outDir){throw 'Evidence directory already exists; use a new revision'}
New-Item -ItemType Directory -Path $outDir | Out-Null
$results=@()
Push-Location $repo
try {
    foreach($entry in @(
        @{name='adapter';arguments=@('run','--project',"$PSScriptRoot/WeChatRewardQa.csproj",'-p:IsolatedContracts=false','--no-launch-profile')},
        @{name='sdk-surface';arguments=@('build',"$PSScriptRoot/WeChatRewardQaSdkCompile.csproj",'-p:IsolatedContracts=false','--nologo')},
        @{name='core-revival';arguments=@('run','--project',"$PSScriptRoot/RevivalRuleQa.csproj",'--',"$outDir/core-revival.json")},
        @{name='coordinator-integration';arguments=@('run','--project',"$PSScriptRoot/JointIntegrationQa.csproj",'--',"$outDir/coordinator-integration.json")}
    )) {
        $arguments=$entry.arguments
        $output=& dotnet @arguments 2>&1
        $exitCode=$LASTEXITCODE
        $log=Join-Path $outDir ($entry.name+'.log')
        $output | Out-File -LiteralPath $log -Encoding utf8
        $results+=@{name=$entry.name;exitCode=$exitCode;log=$log}
    }
    $hashes=@{}
    foreach($file in @('Unity/Assets/HotpotSort/Contracts/DevelopmentServices.cs','Unity/Assets/HotpotSort/Runtime/Session/RewardCoordinator.cs','Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyViewMapper.cs','Unity/Assets/HotpotSort/Runtime/Bootstrap/DailyProductionComposition.cs')) {
        $hashes[$file]=(Get-FileHash -Algorithm SHA256 -LiteralPath $file).Hash
    }
    $report=@{tasks=@('TASK-001 v9','TASK-007 v1');checkpoint='Technical HC-02 Accepted';utc=[DateTime]::UtcNow.ToString('o');isolatedContracts=$false;results=$results;sourceHashes=$hashes;scope='Deterministic adapters, real contracts, core revival, coordinator integration and SDK public surface compile. No platform calls, export, rendering or device acceptance.'}
    $report | ConvertTo-Json -Depth 6 | Out-File -LiteralPath "$outDir/report.json" -Encoding utf8
    $report | ConvertTo-Json -Depth 6
    if(@($results | Where-Object exitCode -ne 0).Count){exit 1}
} finally {Pop-Location}
exit 0
