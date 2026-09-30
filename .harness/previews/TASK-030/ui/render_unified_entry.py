from pathlib import Path
import hashlib
from PIL import Image, ImageDraw, ImageFont, ImageEnhance, ImageChops, ImageFilter

ROOT=Path('D:/火锅消消'); OUT=ROOT/'.harness/previews/TASK-030/ui'
outputs={'ingredient-entry-badge.png','home-unified-entry-unlocked.png','home-unified-entry-locked.png'}
preserve={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('*.png') if p.name not in outputs}
base=Image.open(ROOT/'.harness/qa/TASK-019/entry-1080x1920.png').convert('RGBA')
s=4
badge=Image.new('RGBA',(128*s,148*s))
icon=Image.open(OUT/'ingredients-basket-icon.png').convert('RGBA')
icon=icon.crop(icon.getchannel('A').point(lambda a:255 if a>=16 else 0).getbbox())
icon.thumbnail((104*s,104*s),Image.Resampling.LANCZOS)
badge.alpha_composite(icon,((128*s-icon.width)//2,8*s))
d=ImageDraw.Draw(badge)
d.text((64*s,109*s),'备菜',font=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',34*s),anchor='mm',fill='#F7E4BE',stroke_width=3*s,stroke_fill='#2D1A0D')
# A single silhouette shadow unifies the basket and overlapping title, without a panel.
alpha=badge.getchannel('A')
shadow=Image.new('RGBA',badge.size,(25,12,5,0))
shadow.putalpha(alpha.filter(ImageFilter.GaussianBlur(1.3*s)).point(lambda a:int(a*.42)))
joined=Image.new('RGBA',badge.size); joined.alpha_composite(shadow,(0,2*s)); joined.alpha_composite(badge)
badge=joined.resize((128,148),Image.Resampling.LANCZOS)
badge.save(OUT/'ingredient-entry-badge.png')
assert badge.getchannel('A').getextrema()==(0,255)
for locked in [False,True]:
    im=base.copy(); mark=badge.copy()
    if locked:
        alpha=mark.getchannel('A')
        mark=ImageEnhance.Color(mark).enhance(.05)
        mark=ImageEnhance.Brightness(mark).enhance(.67); mark.putalpha(alpha)
    im.alpha_composite(mark,(38,524))
    if locked:
        d=ImageDraw.Draw(im); x,y=145,622
        d.arc((x-8,y-14,x+8,y+5),180,360,fill='#F7E4BE',width=3)
        d.rounded_rectangle((x-12,y-3,x+12,y+16),4,fill='#F7E4BE',outline='#39281B',width=1)
        d.ellipse((x-2,y+3,x+2,y+7),fill='#39281B')
    im.convert('RGB').save(OUT/('home-unified-entry-'+('locked' if locked else 'unlocked')+'.png'))
    diff=ImageChops.difference(base.convert('RGB'),im.convert('RGB'))
    ImageDraw.Draw(diff).rectangle((38,524,166,672),fill=(0,0,0))
    assert diff.getbbox() is None
    assert im.size==(1080,1920)
for name,h in preserve.items():assert hashlib.sha256((OUT/name).read_bytes()).hexdigest()==h,name
print('Badge:',badge.size,'RGBA alpha',badge.getchannel('A').getextrema(),'transparent',badge.getchannel('A').histogram()[0])
print('Both home images 1080x1920; outside badge unchanged; all prior PNG bytes preserved.')
