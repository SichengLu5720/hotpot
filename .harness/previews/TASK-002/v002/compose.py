"""v002: local layout-only revision, zero external generation.
Reuses v001 drawing helpers without executing its composition or writing v001.
"""
from pathlib import Path
import hashlib,json
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
V1=HERE.parent/'v001'
helper_source=(V1/'compose.py').read_text(encoding='utf-8').split('\nim=base()')[0]
exec(compile(helper_source,str(V1/'compose.py'),'exec'))
# __file__ remains this file, so helpers save only inside v002.
(HERE/'gameplay.png').write_bytes((V1/'gameplay.png').read_bytes())
board=base();flat(board,(12,20,768,1664),'#f8f5e5')
text(board,(32,28),'今日入口 · 纵向布局',30,b=True)
text(board,(33,74),'Design Composite / 入口主稿 + 流程状态',18,MUTED)
rr(board,(28,115,752,1054),CREAM,32,LINE)
# Top status strip: established title and date, no invented currencies.
rr(board,(50,138,730,226),'#e6ebd8',23)
text(board,(72,153),'下锅喽',30,b=True)
text(board,(72,195),'每日挑战',16,MUTED)
text(board,(707,172),'2026 年 9 月 20 日',22,INK,anchor='ra')
center(board,390,291,'今日这一锅',38,b=True)
center(board,390,338,'等你开席',25,MUTED)
# Central main content: original transparent plate and foods only.
sprite(board,'plate.png',(230,406,320,320))
food(board,0,269,447,110);food(board,5,386,461,108)
food(board,4,315,569,112)
# Side regions conservatively map to noninteractive information, not new features.
for x,title,lines in [(48,'今日规则',['同类 3 件','自动出锅']),(566,'玩法提示',['先看订单','再选食材'])]:
    rr(board,(x,409,x+166,589),'#edf0df',22,LINE)
    center(board,x+83,442,title,24,b=True)
    center(board,x+83,490,lines[0],21)
    center(board,x+83,526,lines[1],21)
    center(board,x+83,565,'信息区 · 非按钮',15,MUTED)
rr(board,(215,740,565,795),'#edf0df',24)
center(board,390,767,'五格暂存 · 满格仍可继续',22,INK)
center(board,390,845,'每日一局，认真挑好每一件',23,MUTED)
button(board,(152,900,628,978),'开始今日挑战')
center(board,390,1015,'日期为排版示例，实际由会话提供',17,MUTED)

def state(y,num,title,description,left,right):
    rr(board,(28,y,752,y+175),CREAM,24,LINE)
    text(board,(48,y+15),num+'  '+title,25,b=True)
    text(board,(48,y+59),description,19,MUTED)
    button(board,(48,y+105,379,y+157),left)
    button(board,(399,y+105,730,y+157),right,False)
state(1075,'02','暂停','歇一会儿，食材都在。遮罩拦截盘区点击。','继续游戏','退出本局')
state(1266,'03','结果 · 完成 / 未完成','展示真实统计；失败仅由溢出事件触发。','同日再试一次','返回入口')
state(1457,'04','异常中止','本局已中止。展示会话提供的异常原因。','重试本日','返回入口')
center(board,390,1661,'DESIGN COMPOSITE · 780 × 1688 · v002',15)
save(board,'flow-states.png')
paths=[SOURCE,ROOT/'.harness/references/TASK-002/ui-style-reference-2026-09.jpg',V1/'compose.py',V1/'gameplay.png',HERE/'compose.py',HERE/'gameplay.png',HERE/'flow-states.png',*sorted(ASSETS.glob('*.png'))]
records=[]
for p in paths:
    r={'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size}
    if p.suffix.lower() in ['.png','.jpg']:
        with Image.open(p) as a:r.update(width=a.width,height=a.height,format=a.format)
    records.append(r)
(HERE/'content-manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print('v002: two 780x1688 previews; v001 untouched; external generation 0')
