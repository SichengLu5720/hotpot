param([switch]$IsolatedContracts)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../../..'))
$outDir = Join-Path $repo '.harness/qa/TASK-007/v1/adapter-r001'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$property = if ($IsolatedContracts) { '-p:IsolatedContracts=true' } else { '-p:IsolatedContracts=false' }
$results = @()
foreach ($entry in @(
    @{ name='lifecycle-matrix'; args=@('run','--project',(Join-Path $PSScriptRoot 'WeChatRewardQa.csproj'),$property,'--no-launch-profile') },
    @{ name='sdk-surface-compile'; args=@('build',(Join-Path $PSScriptRoot 'WeChatRewardQaSdkCompile.csproj'),$property,'--nologo') }
)) {
    $arguments = $entry.args
    $output = & dotnet @arguments 2>&1
    $code = $LASTEXITCODE
    $log = Join-Path $outDir ($entry.name+'.log')
    $output | Out-File -LiteralPath $log -Encoding utf8
    $results += @{ name=$entry.name; exitCode=$code; log=$log }
}
$report = @{
    task='TASK-007'; version=1; checkpoint='HC-02-v1-Code Accepted'; utc=[DateTime]::UtcNow.ToString('o');
    isolatedContracts=[bool]$IsolatedContracts; results=$results;
    scope='Adapter lifecycle fakes and real vendored SDK public surface compile; not Unity player, export, or device QA';
    caveat='SDK surface compile uses the installed editor SDK facade with UNITY_WEBGL adapter branch. Integrated build must validate the full exported player.'
}
$report | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $outDir 'result.json') -Encoding utf8
$report | ConvertTo-Json -Depth 5
if (@($results | Where-Object exitCode -ne 0).Count) { exit 1 }
exit 0
