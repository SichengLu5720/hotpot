param([Parameter(Mandatory=$true)][string]$SourcePackage,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$source=(Resolve-Path -LiteralPath $SourcePackage).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Candidate destination already exists'}
$asset=Join-Path $repo 'Unity/Assets/HotpotSort/Editor/WeChatBuild/ShareExport/share-theme.png'
if((Get-FileHash -LiteralPath $asset).Hash.ToLowerInvariant() -ne '8440f7931ca7f41e459884ee171a5ea00e9935d478207a95d0618d1c1ce16908'){throw 'Unexpected optimized image'}
New-Item -ItemType Directory -Path $target | Out-Null
robocopy $source $target /E /XD .plugincache /R:1 /W:1 /NFL /NDL /NJH /NJS | Out-Null
if($LASTEXITCODE -gt 7){throw 'Candidate copy failed'}
Copy-Item -LiteralPath $asset -Destination (Join-Path $target 'hotpot/share-theme.png')
$evidencePath=Join-Path $target 'hotpot/export-validation.json'
$evidence=Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
$evidence.shareImageSha256=(Get-FileHash -LiteralPath $asset).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($evidencePath,($evidence|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$changed=@()
foreach($file in Get-ChildItem -LiteralPath $target -File -Recurse){
  $relative=[IO.Path]::GetRelativePath($target,$file.FullName)
  $previous=Join-Path $source $relative
  if(!(Test-Path -LiteralPath $previous) -or (Get-FileHash -LiteralPath $previous).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){$changed+=$relative.Replace('\','/')}
}
$allowed=@('hotpot/share-theme.png','hotpot/export-validation.json')
if($changed.Count -ne 2 -or @($changed|Where-Object {$_ -notin $allowed}).Count){throw 'Unexpected candidate difference'}
[ordered]@{status='PASS';changedFiles=$changed;shareBytes=(Get-Item -LiteralPath $asset).Length;sourcePackagePreserved=$true}|ConvertTo-Json -Depth 4
