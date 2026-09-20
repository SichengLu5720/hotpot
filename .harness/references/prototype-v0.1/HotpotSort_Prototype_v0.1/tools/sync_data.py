"""Sync edited shared/game-data.json to the browser and Unity; stdlib only."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
text=(ROOT/'shared/game-data.json').read_text(encoding='utf-8')
data=json.loads(text)
assert data['schemaVersion']==1
(ROOT/'Web/js/data.js').write_text('/* Generated from shared/game-data.json by tools/sync_data.py. */\n(function(r){const d='+text+'; if(typeof module!=="undefined"&&module.exports)module.exports=d; else r.HotpotData=d;})(typeof globalThis!=="undefined"?globalThis:this);\n',encoding='utf-8')
(ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot/game-data.json').write_text(text,encoding='utf-8')
print('Synchronized browser data.js and Unity Resources JSON.')
