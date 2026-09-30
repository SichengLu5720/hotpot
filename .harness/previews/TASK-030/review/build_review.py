from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib

OUT=Path(__file__).parent
ROOT=OUT.parent
items=[('虾滑','shrimp-paste'),('牛肉丸','beef-ball'),('牛肉滑','beef-paste'),('鸭肠','duck-intestine'),('黄喉','yellow-throat'),('响铃卷','fried-beancurd-roll'),('小郡肝','chicken-gizzard'),('藕片','lotus-root'),('娃娃菜','baby-cabbage'),('豆皮','tofu-skin'),('贡菜','celtuce-stem'),('川粉','wide-vermicelli'),('冻豆腐','frozen-tofu'),('海带结','kelp-knot'),('香菇','shiitake'),('鲜竹笋','bamboo-shoot')]
font='C:/Windows/Fonts/msyh.ttc'
def f(n): return ImageFont.truetype(font,n)
def sheet(cell,extent,title,path):
    top=100
    im=Image.new('RGB',(cell*4,top+cell*4),(244,242,236))
    d=ImageDraw.Draw(im)
    d.text((24,14),title,font=f(26),fill=(45,42,37))
    d.text((24,55),'候选审核 · 尚未接入游戏 · 按可见轮廓等比例缩放',font=f(16),fill=(95,90,80))
    for i,r in enumerate(records):
        x=(i%4)*cell;y=top+(i//4)*cell
        for yy in range(y+8,y+cell-48,12):
            for xx in range(x+8,x+cell-8,12):
                c=(231,232,231) if ((xx-x-8)//12+(yy-y-8)//12)%2 else (247,247,246)
                d.rectangle((xx,yy,min(xx+11,x+cell-9),min(yy+11,y+cell-49)),fill=c)
        source=Image.open(r['source']).convert('RGBA')
        b=r['visible_bbox'];scale=extent/max(b[2]-b[0],b[3]-b[1])
        source=source.resize((round(source.width*scale),round(source.height*scale)),Image.Resampling.LANCZOS)
        px=round(x+cell/2-(b[0]+b[2])*scale/2);py=round(y+8+(cell-56)/2-(b[1]+b[3])*scale/2)
        im.paste(source,(px,py),source)
        label=f"{i+1:02d}  {r['name']}"
        d.text((x+cell//2,y+cell-36),label,font=f(20),fill=(40,38,33),anchor='mt')
    im.save(OUT/path)
records=[]
for i,(name,slug) in enumerate(items):
    p=ROOT/f'VIS-{i//2+1:02d}'/(slug+'.png')
    im=Image.open(p).convert('RGBA');a=im.getchannel('A');b=a.getbbox();w,h=im.size
    vb=a.point(lambda v:255 if v>=16 else 0).getbbox()
    records.append(dict(number=i+1,name=name,source=str(p),size=[w,h],alpha_extrema=a.getextrema(),transparent_pixels=a.histogram()[0],bbox=b,visible_bbox=vb,visible_min_margin=min(vb[0],vb[1],w-vb[2],h-vb[3]),margins=[b[0],b[1],w-b[2],h-b[3]],min_margin=min(b[0],b[1],w-b[2],h-b[3]),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
assert len(records)==16 and len({r['source'] for r in records})==16
sheet(340,250,'新增食材 · 16种统一审核', 'ingredient-review-grid.png')
sheet(200,64,'新增食材 · 64px轮廓辨识', 'ingredient-small-scale.png')
(OUT/'ingredients-index.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
observations=[
('粉橙颗粒团块可见，细碎虾肉细节有所合并','与现有鱼丸同为团块，但颜色和不规则外缘可区分'),
('棕色圆球与十字切口清楚','与现有白鱼丸轮廓相同，主要依赖棕白色差'),
('深红扁团清楚，纤维缩小后弱化','与肥牛卷同属红肉，缺少卷孔可区分；与小郡肝需注意'),
('粉色S形波浪长条明显','与鱿鱼条可能同属浅粉，但宽褶皱和S形有区别'),
('三片淡金弯片可读，解剖辨识较依赖名称','与鱼片同属浅色片状；形似弯饺皮的风险需用户判断'),
('金黄中空筒口仍清楚','现有食材无同色中空卷；与肉卷通过色彩区分'),
('紫红厚块及深切口可见','与牛肉滑较接近，紫红色和粗大切口是主要区别'),
('双圆片和放射孔洞清楚','与土豆片同色，孔洞为强区别'),
('单片黄白叶子轮廓清楚，叶脉与薄叶边缘可读','与青菜共享叶菜语义，但单片形态及黄白色有区别'),
('金色薄片折叠可读','与白豆腐区分明显；与本批川粉的配色和折叠形状接近'),
('翠绿细梗束清楚','与青菜同绿但无叶片；与金针菇形似束状而色差明显'),
('宽带交叠清楚，半透感缩小后减弱','与本批豆皮接近，是本批优先人工审阅组合'),
('金黄方块多孔感可读','与现有豆腐方块轮廓相同，靠黄色孔隙区分；可能被看作面包'),
('深绿结与环孔清楚','与青菜同色系但结形区别强；外形略像蝴蝶结'),
('棕圆盖和白色星切口明确','与牛肉丸同棕圆形，星形白切口区分明显'),
('尖三角笋片与分节清楚','与鱼片同浅色，尖角和竹节区分明显')]
lines=['# TASK-030 食材候选审核说明','', '这是一组候选审核材料，不是正式资产；尚未接入游戏，最终审美由用户判断。', '', '排版工具：Python / Pillow，微软雅黑中文字体。源图只读；保留完整像素画布，仅在审核副本上等比例重采样与透明合成，不重绘、不裁去源图像素。4×4固定顺序；大图主体最长边250px，小图主体最长边64px。', '', '边界口径：完整边界为Alpha>0；主体边界为Alpha≥16，坐标右/下为不包含端点。外围低Alpha残留会使完整边界触边，但不代表主体被裁切。所有图片均1254×1254 RGBA，Alpha范围0–255。JSON另存透明像素数及源文件SHA-256。', '', '## 逐项检查','', '|序号 / 名称|源路径（相对TASK-030）|完整边界 / 最小边距|主体边界 / 最小边距|64px观察|混淆关注|','|---|---|---|---|---|---|']
for r,(obs,risk) in zip(records,observations):
    lines.append(f"|{r['number']:02d} {r['name']}|{Path(r['source']).relative_to(ROOT).as_posix()}|{r['bbox']} / {r['min_margin']}px|{r['visible_bbox']} / {r['visible_min_margin']}px|{obs}|{risk}|")
lines+=['', '## 实际检查与限制','', '- 已检查16个独立源路径、固定顺序、无重复；每项均有真实Alpha及全透明像素。', '- 已打开大图及64px图检查中文、缺图、排列、主体完整性；没有发现审核排版新增裁切或标签乱码。', '- 主体边距低于8%画布（约100px）的候选：虾滑、黄喉、小郡肝、藕片、娃娃菜、海带结、香菇、鲜竹笋。当前保留原候选并记录，不擅自修改源图。', '- 这两张图是静态目标预览，不是游戏运行截图；64px为本工作包指定语义尺寸，未宣称实机尺寸验证。', '- 未运行Unity、编译或正式主题接入；没有变更任何玩法、状态、正式图片或源候选。']
lines=[line.replace('藕片、娃娃菜、海带结','藕片、海带结') for line in lines]
(OUT/'review-notes.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print([(r['number'],r['visible_bbox'],r['visible_min_margin']) for r in records])
