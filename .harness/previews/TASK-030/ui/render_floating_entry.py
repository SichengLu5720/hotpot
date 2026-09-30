from pathlib import Path
import hashlib
from PIL import Image, ImageDraw, ImageFont, ImageEnhance, ImageChops

ROOT=Path('D:/火锅消消')
OUT=ROOT/'.harness/previews/TASK-030/ui'
base=Image.open(ROOT/'.harness/qa/TASK-019/entry-1080x1920.png').convert('RGBA')
preserve={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('*.png') if not p.name.startswith('home-floating-entry-')}
source=Image.open(OUT/'ingredients-basket-icon.png').convert('RGBA')
source=source.crop(source.getchannel('A').point(lambda a:255 if a>=16 else 0).getbbox())
source.thumbnail((104,104),Image.Resampling.LANCZOS)
for locked in [False,True]:
    im=base.copy(); icon=source.copy()
    if locked:
        alpha=icon.getchannel('A')
        icon=ImageEnhance.Color(icon).enhance(.08)
        icon=ImageEnhance.Brightness(icon).enhance(.66)
        icon.putalpha(alpha)
    im.alpha_composite(icon,(102-icon.width//2,584-icon.height//2))
    d=ImageDraw.Draw(im)
    d.text((102,666),'备菜',font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',32),anchor='mm',fill='#CEBFA4' if locked else '#F7E4BE')
    if locked:
        x,y=139,621
        d.arc((x-8,y-14,x+8,y+5),180,360,fill='#F7E4BE',width=3)
        d.rounded_rectangle((x-12,y-3,x+12,y+16),4,fill='#F7E4BE',outline='#39281B',width=1)
        d.ellipse((x-2,y+3,x+2,y+7),fill='#39281B')
    name='home-floating-entry-'+('locked' if locked else 'unlocked')+'.png'
    im.convert('RGB').save(OUT/name)
    diff=ImageChops.difference(base.convert('RGB'),im.convert('RGB'))
    ImageDraw.Draw(diff).rectangle((48,530,156,690),fill=(0,0,0))
    assert diff.getbbox() is None
    assert im.size==(1080,1920)
for name,digest in preserve.items():assert hashlib.sha256((OUT/name).read_bytes()).hexdigest()==digest,name
print('Two 1080x1920 floating-entry previews. All old PNG bytes preserved. Outside entry region identical to baseline.')
