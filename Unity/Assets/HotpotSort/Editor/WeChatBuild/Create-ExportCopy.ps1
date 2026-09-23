param([Parameter(Mandatory=$true)][string]$Destination,[string]$SourceProject=(Join-Path $PSScriptRoot '../../../..'),[string]$DependencyManifest)
$ErrorActionPreference='Stop'
$PSDefaultParameterValues['Get-Content:Encoding']='utf8'
$source=(Resolve-Path -LiteralPath $SourceProject).Path.TrimEnd('\','/')
$target=[IO.Path]::GetFullPath($Destination).TrimEnd('\','/')
$workspace=Split-Path $source -Parent
if(-not $target.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $target.StartsWith($source+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $target -eq $source){throw 'Build copy must be a new workspace child outside the source Unity project'}
if(Test-Path -LiteralPath $target){throw 'Build copy destination already exists; use a new evidence revision'}
$exclude=@()
foreach($version in @('v3','v7')){foreach($font in @('display.ttf','readable.otf')){$base="Assets/HotpotSort/Resources/Hotpot/TASK001/$version/r001/fonts/$font";$exclude+=$base;$exclude+="$base.meta"}}
$guids=@()
foreach($relative in $exclude|Where-Object {$_ -like '*.meta'}){$path=Join-Path $source $relative;if(Test-Path -LiteralPath $path){$match=[regex]::Match((Get-Content -LiteralPath $path -Raw),'(?m)^guid: ([0-9a-f]{32})');if($match.Success){$guids+=$match.Groups[1].Value}}}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $source 'Assets') -Recurse -File | Where-Object {$_.Extension -in @('.unity','.prefab','.asset')}){
  $content=Get-Content -LiteralPath $file.FullName -Raw
  foreach($guid in $guids){if($content.Contains($guid)){throw ('Legacy font serialized reference must be migrated before copy: '+$file.FullName.Substring($source.Length+1))}}
}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $source 'Assets/HotpotSort/Runtime') -Recurse -Filter '*.cs' -File | Where-Object {$_.FullName -notmatch '[\\/]Editor[\\/]|[\\/]Diagnostics~[\\/]'}){
  $content=Get-Content -LiteralPath $file.FullName -Raw
  if($content -match 'fonts/(readable|display)|CreateDynamicFontFromOSFont|GetBuiltinResource<Font>'){throw ('Legacy/system runtime font dependency: '+$file.FullName.Substring($source.Length+1))}
}
$manifest=Get-Content -LiteralPath (Join-Path $source 'Assets/HotpotSort/Resources/Hotpot/TASK002/v9/r001/fonts/font-manifest.json') -Raw|ConvertFrom-Json
$modern='Assets/HotpotSort/Resources/Hotpot/TASK002/v9/r001/fonts/modern-sans.otf'
if((Get-FileHash -LiteralPath (Join-Path $source $modern)).Hash.ToLowerInvariant() -ne $manifest.fontSha256){throw 'Modern font does not match provenance'}
if([string]::IsNullOrWhiteSpace($DependencyManifest) -or !(Test-Path -LiteralPath $DependencyManifest)){throw 'Accepted Unity dependency proof is required before historical exclusion'}
$proof=Get-Content -LiteralPath $DependencyManifest -Raw|ConvertFrom-Json
if($proof.status -ne 'PASS' -or $proof.staged -or [IO.Path]::GetFullPath($proof.sourceProject).TrimEnd('\','/') -ne $source -or $proof.textureProbes -ne 89 -or $proof.alphaProbes -ne 16 -or $proof.mustKeep.Count -lt 90){throw 'Invalid source dependency proof'}
$historical='Assets/HotpotSort/Resources/Hotpot/TASK001/v3/r001'
function Historical([string]$relative){return $relative -ceq "$historical.meta" -or $relative.StartsWith($historical+'/',[StringComparison]::Ordinal)}
$proofInputs=@{};foreach($entry in $proof.inputs){if($proofInputs.ContainsKey($entry.path)){throw 'Duplicate proof path'};$proofInputs[$entry.path]=$entry}
foreach($entry in $proof.mustKeep){if((Historical $entry.path) -or $entry.path -in $exclude){throw 'Must-keep dependency intersects exclusion'}}
$entries=@()
foreach($folder in @('Assets','Packages','ProjectSettings')){
  foreach($file in Get-ChildItem -LiteralPath (Join-Path $source $folder) -Recurse -File){
    $relative=$file.FullName.Substring($source.Length+1).Replace('\','/')
    if($relative -match '/__pycache__/|\.pyc$|/Diagnostics~/bin/|/Diagnostics~/obj/'){continue}
    $digest=(Get-FileHash -LiteralPath $file.FullName).Hash
    if(!$proofInputs.ContainsKey($relative) -or $proofInputs[$relative].sha256 -ne $digest -or $proofInputs[$relative].bytes -ne $file.Length){throw ('Stale dependency proof: '+$relative)}
    $isFont=$relative -in $exclude;$isHistorical=Historical $relative
    $entries+=@{path=$relative;guid=$proofInputs[$relative].guid;sha256=$digest;bytes=$file.Length;excluded=($isFont -or $isHistorical);exclusionReason=if($isFont){'Unreferenced legacy large font'}elseif($isHistorical){'v3/r001 outside frozen Boot/v7 serialized and dynamic dependency closure'}else{''}}
  }
}
if($entries.Count -ne $proof.inputs.Count){throw 'Dependency proof inventory differs from source'}
New-Item -ItemType Directory -Path $target|Out-Null
foreach($entry in $entries|Where-Object {-not $_.excluded}){
  $output=Join-Path $target $entry.path
  New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent)|Out-Null
  Copy-Item -LiteralPath (Join-Path $source $entry.path) -Destination $output
  if((Get-FileHash -LiteralPath $output).Hash -ne $entry.sha256){throw 'Build copy file hash mismatch'}
}
$sourceChanged=@($entries|Where-Object {(Get-FileHash -LiteralPath (Join-Path $source $_.path)).Hash -ne $_.sha256})
foreach($entry in $proof.mustKeep){$kept=Join-Path $target $entry.path;if(!(Test-Path -LiteralPath $kept) -or (Get-FileHash -LiteralPath $kept).Hash -ne $entry.sha256){throw 'Must-keep staged dependency absent or changed'}}
$result=@{status=if($sourceChanged.Count){'RETRY_NEEDED'}else{'PASS'};sourceProject=$source;copiedProject=$target;sourceUnchanged=($sourceChanged.Count -eq 0);dependencyProofSha256=(Get-FileHash -LiteralPath $DependencyManifest).Hash;mustKeep=$proof.mustKeep;excluded=@($entries|Where-Object excluded);excludedFontBytes=($entries|Where-Object {$_.path -in $exclude -and $_.path -notlike '*.meta'}|Measure-Object bytes -Sum).Sum;excludedHistoricalBytes=($entries|Where-Object {(Historical $_.path) -and $_.path -notlike '*.meta' -and $_.path -notin $exclude}|Measure-Object bytes -Sum).Sum;copiedFiles=@($entries|Where-Object {-not $_.excluded}).Count;modernFontSha256=$manifest.fontSha256;entries=$entries;realSdkExport=$false}
$result|ConvertTo-Json -Depth 6|Set-Content (Join-Path (Split-Path $target -Parent) 'build-copy-manifest.json')
if($sourceChanged.Count){throw 'Source changed during copy; this copy is not a valid candidate'}
foreach($relative in $exclude){if(Test-Path -LiteralPath (Join-Path $target $relative)){throw 'Legacy font survived staged exclusion'}}
if(Test-Path -LiteralPath (Join-Path $target $historical)){throw 'Historical assets survived staging'}
Write-Output "TASK002_BUILD_COPY_READY copiedFiles=$($result.copiedFiles) excludedFontBytes=$($result.excludedFontBytes)"
