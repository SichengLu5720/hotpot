"""Rebuild portable data from delivered content skeletons; no APK/assets required."""
from pathlib import Path
import json, hashlib
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'shared/reference'
NAMES=['肥牛卷','虾仁','香菇','玉米','西兰花','豆腐','藕片','鱼丸','辣椒','南瓜','青菜','蟹棒','土豆','鸡蛋','魔芋结','胡萝卜']
COLORS=['#DE746C','#F49A7E','#917059','#EDC455','#7BA47A','#F4DEAF','#D8B48F','#EEE5CA','#D64C43','#E99749','#659877','#E06B60','#CCA75F','#F4D47E','#B8CEC3','#EA8655']
levels=[]
for label,diff in [('A',1),('B',2),('C',3)]:
 s=json.loads((SOURCE/f'skeleton_{label}.json').read_text())
 levels.append({'id':label,'name':f'骨架 {label}','difficulty':diff,'sourceLevel':s['source']['base_level'], 'plates':[''.join(b['fish_symbols']) for b in s['bubbles']]})
# Independently authored 27-item smoke-test fixture: nine of each of A/B/C.
levels.append({'id':'P','name':'练习','difficulty':1,'sourceLevel':0,'plates':['AAB','BBC','CCA','ABC','AAB','BBC','CCA','ABC','ABC']})
w=json.loads((SOURCE/'LevelDifficultyConfig.json').read_text())
weights=[{'difficulty':r['Difficulty'],'progress':r['LevelProgress'],'temp':r['TempCount'],'weights':[r[f'Order{i}'] for i in range(1,6)]} for r in w]
data={'schemaVersion':1,'title':'开锅啦 · 食材整理','prototypeVersion':'0.1.0',
 'rules':{'bufferCapacity':5,'orderSlots':4,'openOrderSlots':2,'orderSize':3,'openingLookahead':10,'failOnFull':True,'width':420,'height':900,'playTop':304,'playBottom':828,'spawnGate':377,'spawnInterval':0.20,'gravity':960,'physicsStep':0.008333333333333333,'plateRadii':[0,33,40,46,52,57]},
 'ingredients':[{'id':i,'symbol':chr(65+i),'name':n,'color':COLORS[i]} for i,n in enumerate(NAMES)],
 'levels':levels,'difficultyRows':weights}
text=json.dumps(data,ensure_ascii=False,indent=2)
(ROOT/'shared/game-data.json').write_text(text,encoding='utf-8')
(ROOT/'Web/js/data.js').write_text('/* Generated from shared/game-data.json by tools/sync_data.py. */\n(function(r){const d='+text+'; if(typeof module!=="undefined"&&module.exports)module.exports=d; else r.HotpotData=d;})(typeof globalThis!=="undefined"?globalThis:this);\n',encoding='utf-8')
(ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json').write_text(text,encoding='utf-8')
for l in levels:
 from collections import Counter
 c=Counter(''.join(l['plates'])); assert all(n%3==0 for n in c.values()),(l,c)
 print(l['id'],len(l['plates']),sum(c.values()),len(c))
provenance={'levels':[{**json.loads((SOURCE/f'skeleton_{k}.json').read_text())['source'],'label':k,'file_sha256':hashlib.sha256((SOURCE/f'skeleton_{k}.json').read_bytes()).hexdigest()} for k in 'ABC'],'notes':'A/B/C are analysis fixtures extracted from user-supplied APK; P and all implementation/art code are newly authored. Do not confuse data provenance with a license grant.'}
(ROOT/'shared/provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2))
