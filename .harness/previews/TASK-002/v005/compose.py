"""v005: remove device cards; preserve background, hero, primary button, review guides."""
from pathlib import Path
import hashlib,json
from PIL import Image
HERE=Path(__file__).resolve().parent
V3=HERE.parent/'v003'
source=(V3/'compose.py').read_text(encoding='utf-8')
prefix,suffix=source.split('results=[entry',1)
exec(compile(prefix,str(V3/'compose.py'),'exec'))
original_text=text
original_rr=rr
text_layers=[]
removed_shapes=[]
def text(im,xy,value,size=24,color=INK,b=False,anchor=None):
    if im.width==2186*S:return original_text(im,xy,value,size,color,b,anchor)
    if value=='开始今日挑战':
        text_layers.append({'viewport_width':im.width//S,'text':'开始下火锅'})
        return original_text(im,xy,'开始下火锅',size,color,b,anchor)
def center(im,x,y,t,n=26,color=INK,b=False):return text(im,(x,y),t,n,color,b,'mm')
def rr(im,box,fill,r=24,outline=None,width=2):
    if im.width==2186*S or fill==RED:return original_rr(im,box,fill,r,outline,width)
    removed_shapes.append({'viewport_width':im.width//S,'bounds':list(box),'fill':fill})
suffix='results=[entry'+suffix
changes={
    'v003':'v005',
    '窄屏局部放大 · 信息模块':'局部放大 · 保留主按钮',
    'crop((18,410,302,507))':'crop((20,560,300,655))',
    '宽屏：两侧信息区保留，中央主视觉放大。':'三个视口：顶部空框与两侧空卡全部移除。',
    '窄屏：说明下移为双列，优先保证文字可读。':'保留奶油背景、中央食材与唯一主按钮。',
    '顶部状态 → 中央食材 → 底部唯一开始按钮':'奶油背景 → 中央食材 → 底部唯一主按钮',
    '左右模块为规则说明，不注册按钮或新功能。':'设备内无标题、无空卡、无其他说明文字。',
    '实际绑定平台 safeArea；日期、标题由控件渲染。':'实际绑定平台 safeArea；唯一按钮字走原生控件。',
}
for old,new in changes.items():suffix=suffix.replace(old,new)
exec(compile(suffix,str(V3/'compose.py'),'exec'))
layout=json.loads((HERE/'layout-spec.json').read_text(encoding='utf-8'))
for r in layout:
    r.pop('information_layout',None)
    r['device_content']=['cream background','plate and food hero','primary button','review-only safety dashed guides']
    r['device_text_whitelist']=['开始下火锅']
    r['empty_cards']='removed: header and both information cards'
(HERE/'layout-spec.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'layer-audit.json').write_text(json.dumps({'text_layers':text_layers,'removed_shapes':removed_shapes},ensure_ascii=False,indent=2),encoding='utf-8')
assert len(text_layers)==3 and len(removed_shapes)==9
paths=[SOURCE,V1/'compose.py',V3/'compose.py',ROOT/'.harness/references/TASK-002/review-board-reference.png',HERE/'compose.py',HERE/'layout-spec.json',HERE/'layer-audit.json',HERE/'README.md',*sorted(HERE.glob('*.png')),*sorted(ASSETS.glob('*.png'))]
records=[]
for p in paths:
    r={'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
    if p.suffix=='.png':
        with Image.open(p) as a:r.update(width=a.width,height=a.height,format=a.format)
    records.append(r)
(HERE/'content-manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print('v005: removed 3 empty cards per view; preserved one button label per view.')
