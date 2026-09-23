param([Parameter(Mandatory=$true)][string]$SourceProject,[Parameter(Mandatory=$true)][string]$PlanPath,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$PSDefaultParameterValues['Get-Content:Encoding']='utf8'
$source=(Resolve-Path -LiteralPath $SourceProject).Path.TrimEnd('\','/')
$target=[IO.Path]::GetFullPath($Destination).TrimEnd('\','/')
if(Test-Path -LiteralPath $target){throw 'Never overwrite an isolated bundle project'}
if($target -eq $source -or $target.StartsWith($source+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Destination must be outside source'}
if($target -notmatch '[\\/]\.harness[\\/]qa[\\/](TASK-002[\\/]v10|TASK-012|TASK-013)[\\/]'){throw 'Only authorized evidence staging paths are allowed'}
$plan=Get-Content -LiteralPath $PlanPath -Raw|ConvertFrom-Json
if($plan.schemaVersion -ne 1 -or [IO.Path]::GetFullPath($plan.sourceProject).TrimEnd('\','/') -ne $source -or $plan.assets.Count -lt 1 -or @($plan.assets|Where-Object remote).Count -lt 16){throw 'Invalid AssetDatabase asset plan'}
$themeMatch=[regex]::Match($plan.assets[0].path,'^Assets/HotpotSort/Resources/(Hotpot/TASK001/v[0-9]+/r[0-9]+)/')
if(!$themeMatch.Success){throw 'Invalid theme root'}
$selectedRoot='Assets/HotpotSort/Resources/'+$themeMatch.Groups[1].Value+'/'
$remote=@{};$guids=@()
foreach($asset in $plan.assets){
 foreach($pair in @(@($asset.path,$asset.sha256),@(($asset.path+'.meta'),$asset.metaSha256))){if((Get-FileHash -LiteralPath (Join-Path $source $pair[0])).Hash -ne $pair[1]){throw 'Stale asset or importer plan'}}
 if($asset.remote){if(!$asset.path.StartsWith($selectedRoot) -or !$asset.destination.StartsWith('Assets/HotpotSort/RemoteAssetBundles/v10/')){throw 'Unsafe remote mapping'};$remote[$asset.path]=$asset.destination;$remote[$asset.path+'.meta']=$asset.destination+'.meta';$guids+=$asset.guid}
}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $source 'Assets') -Recurse -File|Where-Object {$_.Extension -in @('.unity','.prefab','.asset')}){foreach($guid in $guids){if((Get-Content -LiteralPath $file.FullName -Raw).Contains($guid)){throw 'Serialized bootstrap dependency references remote asset'}}}
$entries=@()
foreach($folder in @('Assets','Packages','ProjectSettings')){foreach($f in Get-ChildItem -LiteralPath (Join-Path $source $folder) -Recurse -File){
 $rel=$f.FullName.Substring($source.Length+1).Replace('\','/')
 if($rel -match '/__pycache__/|\.pyc$|/Diagnostics~/(bin|obj)/'){continue}
 $historical=($rel -match '^Assets/HotpotSort/Resources/Hotpot/TASK001/v[0-9]+/r[0-9]+(/|\.meta$)') -and !$rel.StartsWith($selectedRoot) -and $rel -ne ($selectedRoot.TrimEnd('/')+'.meta')
 $font=$rel -match '^Assets/HotpotSort/Resources/Hotpot/TASK001/v[37]/r001/fonts/(display\.ttf|readable\.otf)(\.meta)?$'
 $meta=if($rel.EndsWith('.meta')){$f.FullName}else{$f.FullName+'.meta'};$guid=''
 if(Test-Path -LiteralPath $meta){$m=[regex]::Match((Get-Content -LiteralPath $meta -Raw),'(?m)^guid: ([0-9a-f]{32})');if($m.Success){$guid=$m.Groups[1].Value}}
 $entries+=@{path=$rel;destination=if($remote.ContainsKey($rel)){$remote[$rel]}else{$rel};sha256=(Get-FileHash -LiteralPath $f.FullName).Hash;bytes=$f.Length;guid=$guid;excluded=($historical -or $font);reason=if($font){'Legacy font'}elseif($historical){'Verified historical v3'}elseif($remote.ContainsKey($rel)){'Remote AssetBundle outside Resources; bytes and GUID unchanged'}else{'Bootstrap or unchanged project input'}}
}}
New-Item -ItemType Directory -Path $target|Out-Null
foreach($entry in $entries|Where-Object {-not $_.excluded}){
 $out=[IO.Path]::GetFullPath((Join-Path $target $entry.destination));if(!$out.StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Copy escapes isolated root'}
 New-Item -ItemType Directory -Force -Path (Split-Path $out -Parent)|Out-Null
 Copy-Item -LiteralPath (Join-Path $source $entry.path) -Destination $out
 if((Get-FileHash -LiteralPath $out).Hash -ne $entry.sha256){throw 'Copied file bytes changed'}
}
foreach($asset in $plan.assets){if($asset.remote -and (Test-Path -LiteralPath (Join-Path $target $asset.path))){throw 'Remote PNG duplicated in Resources'}}
$changed=@($entries|Where-Object {(Get-FileHash -LiteralPath (Join-Path $source $_.path)).Hash -ne $_.sha256})
if($changed.Count){throw 'Source changed during copy; retry from a fresh revision'}
$marker=Join-Path $target 'Assets/HotpotSort/RemoteAssetBundles/v10/staging-plan.json'
Copy-Item -LiteralPath $PlanPath -Destination $marker
@{status='PASS';sourceProject=$source;copiedProject=$target;sourceUnchanged=$true;planSha256=(Get-FileHash $PlanPath).Hash;remotePngCount=@($plan.assets|Where-Object remote).Count;localPngCount=@($plan.assets|Where-Object {-not $_.remote}).Count;entries=$entries}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path (Split-Path $target -Parent) 'build-copy-manifest.json') -Encoding utf8
Write-Output 'TASK002_V10_COPY_PASS sourceUnchanged=true'
