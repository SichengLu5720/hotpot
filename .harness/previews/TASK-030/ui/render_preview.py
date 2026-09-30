from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageChops, ImageEnhance
import sys

ROOT=Path('D:/火锅消消')
OUT=ROOT/'.harness/previews/TASK-030/ui'
BASE=Image.open(ROOT/'.harness/qa/TASK-019/entry-1080x1920.png').convert('RGBA')
CREAM='#F7E4BE'; GOLD='#C68C4F'; DARK='#28190F'
def font(n,b=False): return ImageFont.truetype('C:/Windows/Fonts/msyh'+('bd' if b else '')+'.ttc',n)
def text(im,xy,s,n=28,c=CREAM,b=False,anchor='mm'):
    ImageDraw.Draw(im).text(xy,s,font=font(n,b),fill=c,anchor=anchor)
def box(im,r,fill=DARK,outline=GOLD,radius=22,width=2):
    ImageDraw.Draw(im).rounded_rectangle(r,radius,fill,outline,width)
def lock(im,x,y,s=1,c=CREAM):
    d=ImageDraw.Draw(im); d.arc((x-12*s,y-20*s,x+12*s,y+8*s),180,360,fill=c,width=max(2,int(4*s)))
    d.rounded_rectangle((x-18*s,y-5*s,x+18*s,y+22*s),5*s,fill=c)
    d.ellipse((x-3*s,y+3*s,x+3*s,y+9*s),fill=DARK)
def basket(im,x,y,muted=False):
    d=ImageDraw.Draw(im); c='#A99A80' if muted else CREAM
    d.arc((x-25,y-32,x+25,y+20),180,360,fill=c,width=5)
    d.polygon([(x-35,y-5),(x+35,y-5),(x+26,y+32),(x-26,y+32)],fill=c)
    for dx in [-15,0,15]: d.line((x+dx,y+3,x+dx,y+24),fill='#624127',width=3)
    d.line((x-30,y+11,x+30,y+11),fill='#624127',width=3)
    d.ellipse((x-27,y-20,x-7,y-1),fill=c); d.ellipse((x+3,y-25,x+26,y-3),fill=c)
for locked in [True,False]:
    im=BASE.copy(); d=ImageDraw.Draw(im)
    box(im,(40,526,126,750),'#25180F','#D79E5C',25,3)
    box(im,(47,533,119,743),'#25180F','#714422',20,1)
    icon=Image.open(OUT/'ingredients-basket-icon.png').convert('RGBA')
    icon=icon.crop(icon.getchannel('A').point(lambda a:255 if a>=16 else 0).getbbox())
    icon.thumbnail((56,56),Image.Resampling.LANCZOS)
    if locked:
        alpha=icon.getchannel('A')
        icon=ImageEnhance.Color(icon).enhance(.15)
        icon=ImageEnhance.Brightness(icon).enhance(.60)
        icon.putalpha(alpha)
    im.alpha_composite(icon,(83-icon.width//2,572-icon.height//2))
    text(im,(83,636),'备',28); text(im,(83,674),'菜',28)
    if locked:
        lock(im,83,717,.40,c='#B6A084')
    im.convert('RGB').save(OUT/('home-sidebar-locked.png' if locked else 'home-sidebar-unlocked.png'))

names=['肥牛卷','羊肉卷','午餐肉','毛肚','虾','鱼片','鱿鱼','蟹柳','鱼丸','豆腐','鸭血','鹌鹑蛋','玉米段','土豆片','金针菇','青菜','虾滑','牛肉丸','牛肉滑','鸭肠','黄喉','响铃卷','小郡肝','藕片','娃娃菜','豆皮','贡菜','川粉','冻豆腐','海带结','香菇','鲜竹笋']
files=['shrimp-paste','beef-ball','beef-paste','duck-intestine','yellow-throat','fried-beancurd-roll','chicken-gizzard','lotus-root','baby-cabbage','tofu-skin','celtuce-stem','wide-vermicelli','frozen-tofu','kelp-knot','shiitake','bamboo-shoot']
paths=[ROOT/f'Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v10/r001/food/food_{i:02}.png' for i in range(16)]
paths += [ROOT/f'.harness/previews/TASK-030/VIS-{i//2+1:02}/{f}.png' for i,f in enumerate(files)]
unlocked=set(range(16))|{16,17,23,24}
selected=(set(range(16))|{16,24})
copies={12:2,14:1,16:3,17:1,23:0,24:0}
def food(im,path,center,locked=False):
    p=Image.open(path).convert('RGBA'); a=p.getchannel('A'); bounds=a.point(lambda x:255 if x>=16 else 0).getbbox()
    p=p.crop(bounds); p.thumbnail((118,103),Image.Resampling.LANCZOS)
    if locked:
        g=ImageOps.grayscale(p); p=Image.merge('RGBA',(g,g,g,p.getchannel('A').point(lambda x:int(x*.30))))
    im.alpha_composite(p,(int(center[0]-p.width/2),int(center[1]-p.height/2)))
def collection(under=False):
    im=BASE.copy(); im.alpha_composite(Image.new('RGBA',im.size,(18,10,5,205)))
    # Reuse an unobstructed wood sample; no generated replacement textures.
    wood=BASE.crop((180,1335,900,1425)).resize((788,1618),Image.Resampling.LANCZOS)
    mask=Image.new('L',wood.size); ImageDraw.Draw(mask).rounded_rectangle((0,0,787,1617),34,fill=255)
    wood.putalpha(mask); im.alpha_composite(wood,(146,178))
    im.alpha_composite(Image.new('RGBA',(772,1602),(29,16,8,125)),(154,186))
    ImageDraw.Draw(im).rounded_rectangle((146,178,934,1796),34,outline=GOLD,width=3)
    text(im,(540,248),'备菜',54,b=True)
    d=ImageDraw.Draw(im); d.line((211,238,193,256,211,274),fill=CREAM,width=5)
    sel=selected-{12,14,16} if under else selected
    for pos,i in enumerate(range(12,32)):
        row,col=divmod(pos,4); x=182+col*184; y=340+row*254
        ok=i in unlocked; checked=i in sel
        box(im,(x,y,x+164,y+234),'#423023' if ok else '#30271E','#D8A35D' if checked else '#795A3A',18,3 if checked else 1)
        food(im,paths[i],(x+82,y+106),not ok)
        text(im,(x+82,y+190),names[i],24,c=CREAM if ok else '#9B9386')
        if ok:
            d=ImageDraw.Draw(im); d.ellipse((x+127,y+12,x+151,y+36),fill='#D7AD6E' if checked else '#33261B',outline=GOLD,width=2)
            if checked:d.line((x+132,y+24,x+138,y+30,x+146,y+19),fill='#332012',width=3)
            if copies.get(i,0)>0:text(im,(x+30,y+25),'×'+str(copies[i]),20,c='#C7A77F')
        else: lock(im,x+82,y+106,.55)
    d=ImageDraw.Draw(im); d.rounded_rectangle((913,345,918,1586),3,fill='#35251A'); d.rounded_rectangle((913,809,918,1586),3,fill='#B38853')
    box(im,(390,1674,690,1737),'#50321C','#B8854C',25)
    text(im,(540,1705),'好友交换',28)
    if under:
        box(im,(228,919,852,1009),'#291B12','#E2B371',25,3)
        text(im,(540,964),'食材不够吃啦～请再多选点',30,b=True)
    return im.convert('RGB')
if '--home-only' not in sys.argv:
    collection().save(OUT/'collection-grid.png')
    collection(True).save(OUT/'collection-under16.png')
for p in [OUT/n for n in ['home-sidebar-locked.png','home-sidebar-unlocked.png','collection-grid.png','collection-under16.png']]:
    assert Image.open(p).size==(1080,1920)
for p in ['home-sidebar-locked.png','home-sidebar-unlocked.png']:
    diff=ImageChops.difference(BASE.convert('RGB'),Image.open(OUT/p).convert('RGB'))
    draw=ImageDraw.Draw(diff)
    draw.rectangle((40,526,126,750),fill=(0,0,0))
    assert diff.getbbox() is None, 'Home changed outside sidebar'
print('Rendered 4 previews at 1080x1920; 32 source paths loaded successfully.')
