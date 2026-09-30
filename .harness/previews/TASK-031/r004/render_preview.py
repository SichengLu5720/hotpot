from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageChops
import shutil,json,hashlib
ROOT=Path('D:/火锅消消'); OUT=ROOT/'.harness/previews/TASK-031/r004'
SRC=ROOT/'.harness/previews/TASK-031/r003'; UI=ROOT/'.harness/previews/TASK-030/ui'
old_files=[p for rev in ['r001','r002','r003'] for p in (SRC.parent/rev).iterdir() if p.is_file()]
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in old_files}
BASE=Image.open(UI/'home-unified-entry-unlocked.png').convert('RGBA')
POS=(18,698); OLD=(70,698); SIZE=(168,140)
assert 38+128/2==POS[0]+SIZE[0]/2==102
shutil.copyfile(SRC/'home-before-first-win.png',OUT/'home-before-first-win.png')
results={}
for name,filename in [('home-after-first-win','broth-activity-entry-badge.png'),('home-after-first-win-alert','broth-activity-entry-badge-alert.png')]:
    mark=Image.open(SRC/filename).convert('RGBA')
    im=BASE.copy(); im.alpha_composite(mark,POS); im=im.convert('RGB'); im.save(OUT/(name+'.png'))
    diff=ImageChops.difference(im,Image.open(SRC/(name+'.png')).convert('RGB'))
    bbox=diff.getbbox(); ImageDraw.Draw(diff).rectangle((18,698,238,838),fill=(0,0,0)); assert diff.getbbox() is None
    base_diff=ImageChops.difference(im,BASE.convert('RGB'));ImageDraw.Draw(base_diff).rectangle((18,698,186,838),fill=(0,0,0));assert base_diff.getbbox() is None
    results[name]={'difference_bbox_from_r003':bbox,'outside_old_and_new_regions_changed_pixels':0,'outside_new_region_vs_base_changed_pixels':0}
names=['home-before-first-win','home-after-first-win','home-after-first-win-alert']
for name in names:
    p=Image.open(OUT/(name+'.png'));assert p.size==(1080,1920)
    p.resize((360,640),Image.Resampling.LANCZOS).save(OUT/(name+'-360.png'))
board=Image.new('RGB',(1080,700),'#241A12');d=ImageDraw.Draw(board)
for i,(name,label) in enumerate(zip(names,['首次通关前 · 隐藏','首次通关后 · 同轴开放','同轴开放 · 红点示例'])):
    d.text((i*360+180,27),label,font=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',23),fill='#F7E4BE',anchor='mm')
    board.paste(Image.open(OUT/(name+'-360.png')),(i*360,60))
board.save(OUT/'home-three-states.png')
detail=Image.new('RGB',(1040,540))
for i,p in enumerate([BASE.convert('RGB'),Image.open(OUT/'home-after-first-win.png')]):
    detail.paste(p.crop((0,660,260,930)).resize((520,540),Image.Resampling.LANCZOS),(i*520,0))
detail.save(OUT/'alignment-detail.png')
for p,h in hashes.items(): assert hashlib.sha256(Path(p).read_bytes()).hexdigest()==h
assert (OUT/'home-before-first-win.png').read_bytes()==(SRC/'home-before-first-win.png').read_bytes()
report={'task':'TASK-031','revision':'r004','status':'Needs Clarification: horizontal-only alignment introduces slight red-cloth edge overlap','red_cloth_edge_overlap_detected':True,'ingredient_axis_x':102,'activity_axis_x':102,'x_translation':-52,'y_translation':0,
 'new_activity_region':[18,698,186,838],'old_activity_region':[70,698,238,838],
 'badge_source':'r003 PNG reused unchanged; horizontal label retained','before_first_win_byte_identical':True,'before_first_win_activity_pixels':0,
 'r001_r003_unchanged':True,'image_checks':results,
 'files':{p.name:{'size':list(Image.open(p).size),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in OUT.glob('*.png')}}
(OUT/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
