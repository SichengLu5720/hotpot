"""v004 local selective text-layer revision. Keeps v003 geometry unchanged.
No old-version writes: executing source inherits this __file__, so HERE is v004.
"""
from pathlib import Path
import json,hashlib
from PIL import Image
HERE=Path(__file__).resolve().parent
V3=HERE.parent/'v003'
source=(V3/'compose.py').read_text(encoding='utf-8')
prefix,suffix=source.split("results=[entry",1)
exec(compile(prefix,str(V3/'compose.py'),'exec'))
original_text=text
original_center=center
text_layers=[]
def text(im,xy,value,size=24,color=INK,b=False,anchor=None):
    if im.width==2186*S:
        return original_text(im,xy,value,size,color,b,anchor)
    if value=='开始今日挑战':
        text_layers.append({'viewport_width':im.width//S,'text':'开始下火锅','anchor':'center','position':list(xy),'font_size':size})
        return original_text(im,xy,'开始下火锅',size,color,b,anchor)
    # All other device text layers are deliberately absent, including guides.
def center(im,x,y,t,n=26,color=INK,b=False):
    return text(im,(x,y),t,n,color,b,'mm')
suffix="results=[entry"+suffix
suffix=suffix.replace('v003','v004').replace('窄屏局部放大 · 信息模块','窄屏局部放大 · 无字装饰卡')
suffix=suffix.replace('宽屏：两侧信息区保留，中央主视觉放大。','宽屏：保留两侧无字装饰卡与中央食材。')
suffix=suffix.replace('窄屏：说明下移为双列，优先保证文字可读。','窄屏：装饰卡下移双列，设备内仅保留按钮字。')
suffix=suffix.replace('左右模块为规则说明，不注册按钮或新功能。','两侧空卡仅为装饰，不注册按钮或新功能。')
suffix=suffix.replace('实际绑定平台 safeArea；日期、标题由控件渲染。','实际绑定平台 safeArea；唯一按钮字走原生控件。')
exec(compile(suffix,str(V3/'compose.py'),'exec'))
layout=json.loads((HERE/'layout-spec.json').read_text(encoding='utf-8'))
for r in layout:
    r['decoration_layout']=r.pop('information_layout')
    r['device_text_whitelist']=['开始下火锅']
    r['noninteractive_cards']='blank decorative shapes only; no labels or semantic actions'
(HERE/'layout-spec.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'text-layer-audit.json').write_text(json.dumps(text_layers,ensure_ascii=False,indent=2),encoding='utf-8')
paths=[SOURCE,V1/'compose.py',V3/'compose.py',ROOT/'.harness/references/TASK-002/review-board-reference.png',HERE/'compose.py',HERE/'layout-spec.json',HERE/'text-layer-audit.json',HERE/'README.md',*sorted(HERE.glob('*.png')),*sorted(ASSETS.glob('*.png'))]
manifest=[]
for p in paths:
    r={'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
    if p.suffix=='.png':
        with Image.open(p) as a:r.update(width=a.width,height=a.height,format=a.format)
    manifest.append(r)
(HERE/'content-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
assert len(text_layers)==3 and all(t['text']=='开始下火锅' for t in text_layers)
print('v004: exactly one device text layer per view; label = 开始下火锅')
