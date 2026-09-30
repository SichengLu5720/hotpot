$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=(Get-Location).Path
$out=$PSScriptRoot
$evidence=Join-Path $repo '.harness/art-production/TASK-001/v9/r001'
$final=Join-Path $evidence 'iteration-04'
$extra=Join-Path $evidence 'friend-loading-final-02'
$baseline=Join-Path $evidence 'baseline'
New-Item -ItemType Directory -Force (Join-Path $out 'screens'),(Join-Path $out 'qa360') | Out-Null
$states=@('entry','gameplay-same-state','four-pots','crowded','pause','settings','win','timeout','revival-share-offer','revival-ad-offer','friends-empty','reward-share-simulation','reward-ad-simulation')
$font=[Drawing.Font]::new('Microsoft YaHei UI',15)
$brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#403931'))
$board=[Drawing.Bitmap]::new(1440,2064);$g=[Drawing.Graphics]::FromImage($board);$g.Clear([Drawing.ColorTranslator]::FromHtml('#fffbf2'));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$i=0
foreach($name in $states){
 $source=Join-Path $final "$name-1080x1920.png";Copy-Item -LiteralPath $source -Destination (Join-Path $out "screens/$name.png")
 $src=[Drawing.Bitmap]::FromFile($source);$thumb=[Drawing.Bitmap]::new(360,640);$draw=[Drawing.Graphics]::FromImage($thumb);$draw.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$draw.DrawImage($src,[Drawing.Rectangle]::new(0,0,360,640));$draw.Dispose();$thumb.Save((Join-Path $out "qa360/$name.png"),[Drawing.Imaging.ImageFormat]::Png)
 $x=($i%4)*360;$y=[int][Math]::Floor($i/4)*516;$g.DrawString($name,$font,$brush,$x+8,$y+8);$g.DrawImage($src,[Drawing.Rectangle]::new($x+45,$y+32,270,480));$i++;$thumb.Dispose();$src.Dispose()
}
foreach($name in @('loading','loading-failed','friend-surface-diagnostic')){Copy-Item -LiteralPath (Join-Path $extra "$name-1080x1920.png") -Destination (Join-Path $out "screens/$name.png")}
$g.Dispose();$board.Save((Join-Path $out 'all-states.png'),[Drawing.Imaging.ImageFormat]::Png);$board.Dispose()
$comparison=[Drawing.Bitmap]::new(1080,1020);$g=[Drawing.Graphics]::FromImage($comparison);$g.Clear([Drawing.ColorTranslator]::FromHtml('#fffbf2'));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawString('BEFORE / same fixture',$font,$brush,16,10);$g.DrawString('r017 / same fixture',$font,$brush,556,10)
$a=[Drawing.Bitmap]::FromFile((Join-Path $baseline 'gameplay-same-state-1080x1920.png'));$b=[Drawing.Bitmap]::FromFile((Join-Path $final 'gameplay-same-state-1080x1920.png'));$g.DrawImage($a,[Drawing.Rectangle]::new(0,50,540,960));$g.DrawImage($b,[Drawing.Rectangle]::new(540,50,540,960));$a.Dispose();$b.Dispose();$g.Dispose();$comparison.Save((Join-Path $out 'before-after.png'),[Drawing.Imaging.ImageFormat]::Png);$comparison.Dispose();$font.Dispose();$brush.Dispose()
$baseState=Get-Content (Join-Path $baseline 'same-state.json') -Raw|ConvertFrom-Json
$finalState=Get-Content (Join-Path $final 'same-state.json') -Raw|ConvertFrom-Json
$basePlates=$baseState.plates|ConvertTo-Json -Depth 20 -Compress
$finalPlates=$finalState.plates|ConvertTo-Json -Depth 20 -Compress
if($basePlates-ne$finalPlates){throw 'Same fixture geometry changed'}
$assetRoot=Join-Path $repo 'Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v7/r001'
$assetHashes=@(Get-ChildItem $assetRoot -Recurse -Filter '*.png'|ForEach-Object{[pscustomobject]@{path=$_.FullName.Substring($repo.Length+1);sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$changes=@()
foreach($name in @('GameplayView.cs','GameplayViewV7.cs','GameplayFeedback.cs')){
 $path="Unity/Assets/HotpotSort/Runtime/Presentation/$name"
 $changes+=[pscustomobject]@{path=$path;beforeSha256=(Get-FileHash (Join-Path $evidence "before/$name") -Algorithm SHA256).Hash;afterSha256=(Get-FileHash (Join-Path $repo $path) -Algorithm SHA256).Hash}
}
foreach($path in @('Unity/Assets/HotpotSort/Runtime/Presentation/ModernPresentation.cs','Unity/Assets/HotpotSort/Runtime/Presentation/GameplayViewFriendBoard.cs','Unity/Assets/HotpotSort/Runtime/Presentation/Editor/HotpotSort.VisualIteration.Editor.asmdef','Unity/Assets/HotpotSort/Runtime/Presentation/Editor/R017VisualChecks.cs')){
 $changes+=[pscustomobject]@{path=$path;beforeSha256=$null;afterSha256=(Get-FileHash (Join-Path $repo $path) -Algorithm SHA256).Hash}
}
$metaPaths=@('ModernPresentation.cs.meta','GameplayViewFriendBoard.cs.meta','Editor.meta','Editor/HotpotSort.VisualIteration.Editor.asmdef.meta','Editor/R017VisualChecks.cs.meta')|ForEach-Object{"Unity/Assets/HotpotSort/Runtime/Presentation/$_"}
$sameUv=(Get-FileHash (Join-Path $evidence 'before/V7Art.cs') -Algorithm SHA256).Hash-eq(Get-FileHash (Join-Path $repo 'Unity/Assets/HotpotSort/Runtime/Presentation/V7Art.cs') -Algorithm SHA256).Hash
$result=[ordered]@{task='TASK-001';version=9;revision='r017';status='Completed visual candidate; human approval pending';input='PM resolved autonomous modern UI direction; preserve accepted r013 geometry';actualRuntimeEvidence=@('iteration-04','friend-loading-final-02');geometryIdentical=$true;foodUvSourceUnchanged=$sameUv;formalBitmapChanges=0;newBitmapAssets=0;productionChanges=$changes;generatedMetaPaths=$metaPaths;formalBitmapHashes=$assetHashes;checks=@((Get-Content (Join-Path $final 'checks.txt')).Count,(Get-Content (Join-Path $extra 'checks.txt')).Count);supportedCaptures=@('720x1280','1080x1920','1440x3200');notClaimed=@('Human Accepted','WeChat true friend data','Bootstrap SharedTexture integration','device FPS','publication');remainingDependency='Core integrator binds SharedTexture getter, CloseFriendSurface, viewport callback and preserves session pause ownership via modal callback.'}
$result|ConvertTo-Json -Depth 10|Set-Content (Join-Path $out 'evidence.json') -Encoding UTF8
Get-ChildItem $out -Recurse -Filter '*.png'|ForEach-Object{[pscustomobject]@{path=$_.FullName.Substring($out.Length+1);sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}|ConvertTo-Json|Set-Content (Join-Path $out 'image-hashes.json') -Encoding UTF8
'Review rendered. Food/plate fixture geometry is identical; source changes and image hashes recorded.'
