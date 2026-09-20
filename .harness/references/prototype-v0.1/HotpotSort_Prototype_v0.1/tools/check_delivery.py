"""Delivery integrity checks. NOT a Unity compiler or runtime validation."""
from pathlib import Path
import hashlib, json, re
ROOT=Path(__file__).resolve().parents[1]
checks=[]
def check(name,condition):
    checks.append({'name':name,'passed':bool(condition)})
    if not condition:raise AssertionError(name)
source=json.loads((ROOT/'shared/game-data.json').read_text())
unity=json.loads((ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json').read_text())
check('Unity resource JSON equals shared data',source==unity)
check('Four full level arrays',[(l['id'],len(l['plates']),sum(map(len,l['plates']))) for l in source['levels']]==[('A',33,123),('B',44,162),('C',50,183),('P',9,27)])
check('17 Unity texture assets',len(list((ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot').glob('*.png')))==17)
check('No font binaries included',not any(p.suffix.lower() in ('.ttf','.otf','.woff','.woff2') for p in ROOT.rglob('*')))
check('No APK or native original game binaries included',not any(p.suffix.lower() in ('.apk','.xapk','.so','.dll') for p in ROOT.rglob('*')))
check('All Unity assets have meta files',all(Path(str(p)+'.meta').exists() for p in (ROOT/'Unity/Assets').rglob('*') if p.suffix!='.meta'))
cs=(ROOT/'Unity/Assets/HotpotSort/Runtime/View/HotpotApp.cs.meta').read_text();guid=re.search(r'guid: (\w+)',cs)[1]
scene=(ROOT/'Unity/Assets/HotpotSort/Scenes/Boot.unity').read_text();check('Boot scene references included entry script GUID',guid in scene)
check('No NotImplementedException in implementation',not any('NotImplementedException' in p.read_text() for p in ROOT.rglob('*.cs')))
core=json.loads((ROOT/'qa/core-tests.json').read_text());browser=json.loads((ROOT/'qa/browser-tests.json').read_text())
check('Recorded JS tests pass',core['failed']==0 and core['passed']==29)
check('Recorded browser tests pass',browser['failed']==0 and browser['passed']==15)
check('Does not fabricate a Unity pass report',not (ROOT/'qa/unity-validation.json').exists())
check('Standalone has no external script/style dependencies',not re.search(r'<script src=|<link rel="stylesheet"', (ROOT/'Web/standalone.html').read_text()))
report={'passed':True,'checks':checks,'unityStatus':'Source/assets checked only; no C# compiler, Unity import, editor play, or native build executed.'}
(ROOT/'qa/delivery-integrity.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
manifest=[]
for p in sorted(ROOT.rglob('*')):
    if p.is_file() and p.name!='MANIFEST_SHA256.json' and '__pycache__' not in str(p):
        b=p.read_bytes();manifest.append({'path':p.relative_to(ROOT).as_posix(),'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()})
(ROOT/'MANIFEST_SHA256.json').write_text(json.dumps({'files':manifest},ensure_ascii=False,indent=2))
print(f'{len(checks)} integrity checks passed; {len(manifest)} files hashed. Unity compilation is NOT tested.')
