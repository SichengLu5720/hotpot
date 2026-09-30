$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$fonts=[Drawing.Text.PrivateFontCollection]::new()
$fonts.AddFontFile('D:/火锅消消/Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v7/r001/fonts/readable.otf')
$bitmap=[Drawing.Bitmap]::new(1080,1920);$g=[Drawing.Graphics]::FromImage($bitmap);$g.Clear([Drawing.ColorTranslator]::FromHtml('#FFF5E5'));$g.TextRenderingHint='AntiAliasGridFit'
$format=[Drawing.StringFormat]::new();$format.Alignment='Center';$format.LineAlignment='Center';$ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#513320'))
function Label([string]$text,[single]$y,[single]$size,[single]$height){$font=[Drawing.Font]::new($fonts.Families[0],$size,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel);$g.DrawString($text,$font,$ink,[Drawing.RectangleF]::new(65,$y,950,$height),$format);$font.Dispose()}
Label '火锅消消' 85 96 150
Label '现代中文黑体 · 字形证据' 290 48 100
$lines=@('每日挑战  好友榜  累计每日首通','正在加载好友成绩…','分享未完成，未获得奖励','未扣除额度，可重新尝试','暂存已满  每局仅可成功复活一次','07:42    1/3    10:00    0/3')
for($i=0;$i -lt $lines.Count;$i++){Label $lines[$i] (480+$i*165) $(if($i -eq 5){64}else{44}) 130}
Label "正文14.7px · 辅助字最低12px`n仅字形/尺寸证据，不作为A/B皮肤候选" 1630 36 180
$g.Dispose();$bitmap.Save("$PSScriptRoot/typography-modern.png",[Drawing.Imaging.ImageFormat]::Png)
$small=[Drawing.Bitmap]::new(360,640);$draw=[Drawing.Graphics]::FromImage($small);$draw.InterpolationMode='HighQualityBicubic';$draw.DrawImage($bitmap,0,0,360,640);$draw.Dispose();$small.Save("$PSScriptRoot/typography-modern-360.png",[Drawing.Imaging.ImageFormat]::Png);$small.Dispose();$bitmap.Dispose();$fonts.Dispose();$format.Dispose();$ink.Dispose()
