from pathlib import Path
import ast, hashlib
from PIL import Image, ImageDraw

OUT=Path(__file__).resolve().parent
# Reuse pure drawing helpers/data without executing the previous renderer's output writes.
source=ast.parse((OUT/'render_preview.py').read_text(encoding='utf-8'))
safe=[n for n in source.body if isinstance(n,(ast.Import,ast.ImportFrom,ast.Assign,ast.AugAssign,ast.FunctionDef))]
exec(compile(ast.Module(body=safe,type_ignores=[]),'render_preview_helpers','exec'),globals())
old={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('*.png')}
table=Image.open(ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v10/r001/background/table.png').convert('RGBA').resize((1080,1920),Image.Resampling.LANCZOS)
# Existing restaurant edge dressing is preserved in the outer 128 px.
table.alpha_composite(BASE.crop((0,0,128,1920)),(0,0))
table.alpha_composite(BASE.crop((952,0,1080,1920)),(952,0))
table.alpha_composite(Image.new('RGBA',table.size,(21,12,6,115)))

def render(under=False):
    im=table.copy()
    text(im,(540,239),'备菜',54,b=True)
    d=ImageDraw.Draw(im); d.line((178,225,160,243,178,261),fill=CREAM,width=5)
    sel=selected-{12,14,16} if under else selected
    for pos,i in enumerate(range(12,32)):
        row,col=divmod(pos,4); cx=231+col*206; cy=416+row*250
        ok=i in unlocked; checked=i in sel
        food(im,paths[i],(cx,cy),not ok)
        text(im,(cx,cy+91),names[i],26,c=CREAM if ok else '#AEA394')
        d=ImageDraw.Draw(im)
        if ok:
            d.ellipse((cx+48,cy-69,cx+72,cy-45),fill='#D7AD6E' if checked else '#33261B',outline=GOLD,width=2)
            if checked:d.line((cx+53,cy-57,cx+59,cy-51,cx+67,cy-62),fill='#332012',width=3)
            if copies.get(i,0)>0:text(im,(cx-51,cy-57),'×'+str(copies[i]),20,c='#D7BD94')
        else:lock(im,cx+54,cy-51,.50)
    d.rounded_rectangle((965,354,970,1550),3,fill='#49301C')
    d.rounded_rectangle((965,803,970,1550),3,fill='#B38853')
    box(im,(370,1683,710,1757),'#50321C','#B8854C',27,2)
    text(im,(540,1720),'好友交换',30)
    if under:
        box(im,(228,924,852,1014),'#291B12','#E2B371',25,3)
        text(im,(540,969),'食材不够吃啦～请再多选点',30,b=True)
    return im.convert('RGB')

for under,name in [(False,'collection-grid-no-cards.png'),(True,'collection-under16-no-cards.png')]:
    im=render(under); im.save(OUT/name); assert im.size==(1080,1920)
for name,digest in old.items():
    if name not in ['collection-grid-no-cards.png','collection-under16-no-cards.png']:
        assert hashlib.sha256((OUT/name).read_bytes()).hexdigest()==digest,name
print('Two 1080x1920 no-card previews rendered. All prior PNG bytes preserved.')
