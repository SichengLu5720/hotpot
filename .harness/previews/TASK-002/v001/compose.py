"""Local editable Design Composite. Run from any cwd with Pillow installed.
No image generation, no production assets, no game execution.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
REF = ROOT / '.harness/references/prototype-v0.1/HotpotSort_Prototype_v0.1'
SOURCE = REF / 'qa/browser_mobile.png'
ASSETS = REF / 'Unity/Assets/HotpotSort/Resources/Hotpot'
W,H,S=780,1688,2
INK='#31584d'; MUTED='#718574'; RED='#ce6b53'; CREAM='#fffbed'; LINE='#d5ddc1'
fontroot=Path('C:/Windows/Fonts')
def font(n,b=False): return ImageFont.truetype(str(fontroot/('msyhbd.ttc' if b else 'msyh.ttc')),n*S)
def rr(im,box,fill,r=24,outline=None,width=2):
    ImageDraw.Draw(im).rounded_rectangle(tuple(int(v*S) for v in box),r*S,fill,outline,width*S)
def text(im,xy,value,size=24,color=INK,b=False,anchor=None):
    ImageDraw.Draw(im).text((xy[0]*S,xy[1]*S),value,font=font(size,b),fill=color,anchor=anchor)
def sprite(im,name,box):
    a=Image.open(ASSETS/name).convert('RGBA')
    scale=min(box[2]*S/a.width,box[3]*S/a.height)
    a=a.resize((round(a.width*scale),round(a.height*scale)),Image.Resampling.LANCZOS)
    im.alpha_composite(a,(int((box[0]+box[2]/2)*S-a.width/2),int((box[1]+box[3]/2)*S-a.height/2)))
def food(im,i,x,y,size=82): sprite(im,f'food_{i:02d}.png',(x,y,size,size))
def center(im,x,y,t,n=26,color=INK,b=False): text(im,(x,y),t,n,color,b,'mm')
def button(im,box,label,primary=True):
    rr(im,box,RED if primary else '#e6ebd8',20)
    center(im,(box[0]+box[2])/2,(box[1]+box[3])/2,label,25,'#fffbed' if primary else INK,True)
def save(im,name):
    im.convert('RGB').resize((W,H),Image.Resampling.LANCZOS).save(HERE/name)
def base(): return Image.open(SOURCE).convert('RGBA').resize((W*S,H*S),Image.Resampling.LANCZOS)
def flat(im,box,color): ImageDraw.Draw(im).rectangle(tuple(int(v*S) for v in box),fill=color)

im=base()
# Preserve screenshot perimeter and warm background; replace only named UI regions.
flat(im,(12,20,768,573),'#f8f5e5')
rr(im,(34,46,102,116),RED,23); center(im,68,79,'锅',38,'#fffbed',True)
text(im,(123,43),'下锅喽',43,b=True); text(im,(126,99),'每日挑战 · DAILY',19,MUTED)
rr(im,(669,47,741,119),'#fffbed',36,LINE)
rr(im,(695,69,701,96),INK,3);rr(im,(710,69,716,96),INK,3)
text(im,(36,151),'今日订单',26,b=True);text(im,(744,158),'同类 3 件 · 自动出锅',21,MUTED,anchor='ra')
for x,i,name,progress in [(30,0,'肥牛卷',1),(212,5,'豆腐',0)]:
    rr(im,(x,202,x+166,417),CREAM,29,LINE)
    text(im,(x+143,216),f'{progress}/3',20,MUTED,anchor='ra')
    food(im,i,x+27,235,115);center(im,x+83,353,name,26,b=True)
    for k in range(3): rr(im,(x+53+k*23,382,x+66+k*23,395),RED if k<progress else '#dfdfc9',7)
for x in [394,576]:
    rr(im,(x,202,x+168,417),'#e8ecda',29,LINE)
    center(im,x+84,277,'+',54,'#a6b69d');center(im,x+84,353,'待开放',24,'#80937d')
text(im,(36,442),'暂存食材',25,b=True);text(im,(743,447),'0 / 5',23,MUTED,anchor='ra')
rr(im,(30,484,125,568),'#e1e8d1',21)
center(im,77,509,'已出锅',18,MUTED);center(im,77,542,'0 / 183',21,b=True)
for x in [144,260,376,492,608]:
    rr(im,(x,484,x+104,568),CREAM,19,'#cbd9be');center(im,x+52,523,'·',31,'#c6d6b8')
# Board retains screenshot bounds and source palette. Clean food PNGs replace debug-labeled sprites.
rr(im,(26,578,754,1562),'#e4ead4',40,'#ccd9bc')
for y in range(600,1560,56):
    ImageDraw.Draw(im).line((32*S,y*S,747*S,y*S),fill='#dfe6ce',width=S)
plates=[(58,774,214,[0,4,0,4,5]),(260,706,211,[6,7,6,3,0]),(520,744,193,[12,12,6,5]),
        (91,989,193,[10,5,0,0]),(293,918,153,[11,5]),(565,938,170,[10,10,6]),
        (27,1166,165,[0,0,5]),(262,1072,210,[8,5,7,9,8]),(483,1090,192,[3,0,0,5]),
        (197,1257,153,[0,5]),(348,1269,193,[5,6,4,4]),(589,1257,163,[2,2,3]),
        (29,1340,196,[1,2,3,3]),(236,1401,151,[0,5]),(506,1402,153,[4,4])]
positions={2:[(.16,.24),(.52,.25)],3:[(.32,.1),(.12,.5),(.55,.5)],4:[(.15,.12),(.54,.12),(.15,.52),(.54,.52)],5:[(.1,.14),(.57,.14),(.35,.38),(.1,.6),(.59,.6)]}
for x,y,d,items in plates:
    sprite(im,'plate.png',(x,y,d,d))
    for i,(px,py) in zip(items,positions[len(items)]): food(im,i,x+px*d,y+py*d,d*.34)
rr(im,(26,1574,754,1655),'#f7f7e4',28)
center(im,390,1601,'点一件食材，同类满 3 件出锅',24,b=True)
center(im,390,1633,'先看订单，再选食材',19,MUTED)
center(im,390,1673,'DESIGN COMPOSITE · 780 × 1688 · 预览示意',13,INK)
save(im,'gameplay.png')

board=base();flat(board,(12,20,768,1663),'#f8f5e5')
text(board,(34,36),'每日挑战 · 流程状态',33,b=True)
text(board,(36,89),'Design Composite / 状态分区示意 / 非设备截图',19,MUTED)
def panel(y,k,title):
    rr(board,(28,y,752,y+343),CREAM,28,LINE)
    rr(board,(48,y+19,91,y+60),'#e4e9d6',13);center(board,70,y+39,k,21,b=True)
    text(board,(108,y+20),title,26,b=True)
panel(135,'01','今日入口')
food(board,0,60,222,110);food(board,5,166,225,106)
text(board,(310,216),'今日这一锅，等你开席',29,b=True)
text(board,(310,264),'2026 年 9 月 20 日',23,MUTED)
button(board,(310,316,709,380),'开始今日挑战')
text(board,(52,424),'日期由会话提供；点击开始创建今日挑战。',20,MUTED)
panel(494,'02','暂停')
text(board,(55,582),'歇一会儿，食材都在',31,b=True)
text(board,(55,634),'继续后回到当前这一局。',22,MUTED)
button(board,(52,699,386,766),'继续游戏')
button(board,(404,699,728,766),'退出本局',False)
text(board,(52,790),'弹层遮住盘区输入；背景保留当前棋面。',19,MUTED)
panel(853,'03','结果 · 完成 / 未完成')
text(board,(54,939),'今日已完成',32,b=True)
text(board,(54,987),'用时 06:24     完成订单 61',23,MUTED)
text(board,(54,1024),'点击 183 · 拒绝 0 · 暂存峰值 4',21,MUTED)
text(board,(54,1060),'自动吸收 12 · 重试 0',21,MUTED)
button(board,(52,1109,386,1175),'同日再试一次')
button(board,(404,1109,728,1175),'返回入口',False)
panel(1212,'04','异常中止 · 区别于玩家失败')
text(board,(55,1298),'本局已中止',32,b=True)
text(board,(55,1344),'游戏数据异常，暂时无法继续。',23,MUTED)
text(board,(55,1381),'原因详情由会话提供。',21,MUTED)
button(board,(52,1437,386,1502),'重试本日')
button(board,(404,1437,728,1502),'返回入口',False)
text(board,(36,1580),'失败态：标题“本次未完成”，说明“暂存溢出”。',21,INK)
text(board,(36,1617),'满 5 格仍继续；数值仅为排版样例，运行时读真实事实。',20,MUTED)
center(board,390,1673,'DESIGN COMPOSITE · 780 × 1688 · v001',13)
save(board,'flow-states.png')

# Program-derived content identity, not a claim of visual or game QA passing.
paths=[SOURCE,*sorted(ASSETS.glob('*.png')),HERE/'compose.py',HERE/'gameplay.png',HERE/'flow-states.png']
records=[]
for p in paths:
    rec={'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
    if p.suffix=='.png':
        with Image.open(p) as a: rec.update(width=a.width,height=a.height,format=a.format)
    records.append(rec)
(HERE/'content-manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'outputs':['gameplay.png','flow-states.png'],'dimensions':[W,H],'external_generation_calls':0}))
