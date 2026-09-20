"""Reproducible local review board; no external generation; no writes to older versions."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
V1=HERE.parent/'v001'
exec(compile((V1/'compose.py').read_text(encoding='utf-8').split('\nim=base()')[0],str(V1/'compose.py'),'exec'))

def line(im,points,color='#aeba9b',width=1):
    ImageDraw.Draw(im).line([(int(x*S),int(y*S)) for x,y in points],fill=color,width=width*S)
def guide(im,w,h,top,bottom):
    # Illustrative insets chosen for composition only, never device measurements.
    for y in [top,h-bottom]:
        for x in range(8,w-8,10):line(im,[(x,y),(min(x+5,w-8),y)],'#af8b6a')
    center(im,w/2,top/2,'安全区示意 · 非实测',10,'#93775e')
    center(im,w/2,h-bottom/2,'Design Composite',10,'#93775e')

def entry(w,h,kind):
    # Approved baseline supplies warm canvas; all component layers are editable.
    a=Image.open(SOURCE).convert('RGBA').resize((w*S,h*S),Image.Resampling.LANCZOS)
    flat(a,(0,0,w,h),'#f8f5e5')
    narrow=kind=='narrow'; wide=kind=='wide'
    m=16 if not wide else 32
    top,bottom=(36,26) if narrow else ((26,24) if not wide else (32,28))
    guide(a,w,h,top,bottom)
    hy=top+13; hh=64 if wide else 56
    rr(a,(m,hy,w-m,hy+hh),'#e5ead7',15)
    text(a,(m+14,hy+8),'下锅喽',25 if wide else 22,b=True)
    text(a,(m+15,hy+37),'每日挑战',12,MUTED)
    text(a,(w-m-13,hy+20),'2026.09.20',16 if wide else 13,INK,anchor='ra')
    titley=145 if narrow else (199 if wide else 182)
    center(a,w/2,titley,'今日这一锅',34 if wide else 28,b=True)
    center(a,w/2,titley+40,'等你开席',21 if wide else 18,MUTED)
    d=190 if narrow else (330 if wide else 202)
    py=208 if narrow else (308 if wide else 284)
    px=(w-d)/2
    sprite(a,'plate.png',(px,py,d,d))
    food(a,0,px+d*.15,py+d*.16,d*.34)
    food(a,5,px+d*.54,py+d*.2,d*.33)
    food(a,4,px+d*.34,py+d*.54,d*.35)
    if narrow:
        cards=[(22,416,132,85),(166,416,132,85)]
    else:
        cw=122 if wide else 79
        cy=391 if wide else 324
        cards=[(m,cy,cw,122 if wide else 110),(w-m-cw,cy,cw,122 if wide else 110)]
    for box,title,rows in zip(cards,['今日规则','玩法提示'],[['同类 3 件','自动出锅'],['先看订单','再选食材']]):
        x,y,cw,ch=box
        rr(a,(x,y,x+cw,y+ch),'#e9eddb',13,'#d4ddc3',1)
        center(a,x+cw/2,y+20,title,17 if wide else 14,b=True)
        center(a,x+cw/2,y+43,rows[0],15 if wide else 12)
        center(a,x+cw/2,y+63,rows[1],15 if wide else 12)
        if not narrow:center(a,x+cw/2,y+ch-15,'信息区',12,MUTED)
    hinty=525 if narrow else (744 if wide else 640)
    center(a,w/2,hinty,'五格暂存 · 满格仍可继续',20 if wide else 16,INK)
    center(a,w/2,hinty+34,'每日一局，认真挑好每一件',17 if wide else 13,MUTED)
    bw=min(w-2*m-12,420 if wide else 300)
    by=h-bottom-90
    rr(a,((w-bw)/2,by,(w+bw)/2,by+56),RED,18)
    center(a,w/2,by+28,'开始今日挑战',24 if wide else 21,'#fffbed',True)
    center(a,w/2,by+73,'侧边内容仅作说明，无交互入口',12,MUTED)
    out=a.convert('RGB').resize((w,h),Image.Resampling.LANCZOS)
    name=f'entry-{w}x{h}.png';out.save(HERE/name)
    return out,name,{'viewport':[w,h],'design_insets_only':{'top':top,'bottom':bottom},'information_layout':'below hero, two columns' if narrow else 'left and right of hero','actual_safeArea':'unknown; use platform runtime values'}

results=[entry(390,844,'regular'),entry(320,693,'narrow'),entry(700,1000,'wide')]
B_W,B_H=2186,1645
board=Image.new('RGBA',(B_W*S,B_H*S),'#203a3f')
WHITE='#f7f1de';SUB='#b4c9bd';ACC='#eaa98c'
center(board,1093,64,'下锅喽 · 今日入口布局评审',45,WHITE,True)
center(board,1093,122,'TASK-002  /  v003  /  Design Composite  /  待评审',25,SUB)
center(board,1093,164,'三种设计视口 · 相同内容与交互语义 · 非 Unity / 微信实机截图',22,SUB)
placements=[(80,239,480),(700,239,395),(1270,239,700)]
labels=['常规竖屏 390 × 844','窄屏安全区 320 × 693','较宽竖屏 700 × 1000']
for (a,name,_),(x,y,dw),label in zip(results,placements,labels):
    dh=round(a.height*dw/a.width)
    center(board,x+dw/2,208,label,26,WHITE,True)
    rr(board,(x-6,y-6,x+dw+6,y+dh+6),'#152c30',10)
    resized=a.convert('RGBA').resize((dw*S,dh*S),Image.Resampling.LANCZOS)
    board.alpha_composite(resized,(x*S,y*S))
    center(board,x+dw/2,y+dh+28,'设计合成 · 尺寸为布局目标',18,SUB)

# Narrow-screen explanatory local enlargement, from this generated view only.
text(board,(690,1174),'窄屏局部放大 · 信息模块',21,WHITE,True)
crop=results[1][0].crop((18,410,302,507)).resize((440*S,150*S),Image.Resampling.LANCZOS)
board.alpha_composite(crop.convert('RGBA'),(675*S,1211*S))
text(board,(1269,1281),'宽屏：两侧信息区保留，中央主视觉放大。',21,SUB)
text(board,(1269,1320),'窄屏：说明下移为双列，优先保证文字可读。',21,SUB)
line(board,[(70,1402),(2116,1402)],'#496064',2)
text(board,(80,1430),'01  布局关系',24,WHITE,True)
text(board,(80,1470),'顶部状态 → 中央食材 → 底部唯一开始按钮',20,SUB)
text(board,(80,1505),'左右模块为规则说明，不注册按钮或新功能。',20,SUB)
text(board,(810,1430),'02  安全区与动态文字',24,WHITE,True)
text(board,(810,1470),'虚线 / 留白均为设计示意，非测得设备边距。',20,SUB)
text(board,(810,1505),'实际绑定平台 safeArea；日期、标题由控件渲染。',20,SUB)
text(board,(1520,1430),'03  美术与实现',24,WHITE,True)
text(board,(1520,1470),'复用食材 / 盘子；奶油底 + 原生 UI。',20,SUB)
text(board,(1520,1505),'无外部生成；未做真机 QA；内容待批准。',20,SUB)
center(board,1093,1587,'布局与信息优先级供评审；不沿用参考项目的品牌、美术、活动或按钮。',22,ACC)
board.convert('RGB').resize((B_W,B_H),Image.Resampling.LANCZOS).save(HERE/'entry-layout-review-board.png')
(HERE/'layout-spec.json').write_text(json.dumps([x[2] for x in results],ensure_ascii=False,indent=2),encoding='utf-8')
paths=[SOURCE,V1/'compose.py',ROOT/'.harness/references/TASK-002/review-board-reference.png',HERE/'compose.py',HERE/'layout-spec.json',*sorted(HERE.glob('*.png')),*sorted(ASSETS.glob('*.png'))]
manifest=[]
for p in paths:
    r={'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
    if p.suffix=='.png':
        with Image.open(p) as a:r.update(width=a.width,height=a.height,format=a.format)
    manifest.append(r)
(HERE/'content-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Rendered 2186x1645 review board and 3 independent entry composites. External generation: 0.')
