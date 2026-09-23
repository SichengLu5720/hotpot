"""TASK-002 v9 reproducible OFL font derivative. Requires fonttools==4.60.1.

Run with --check after any runtime copy change. Nicknames are never inputs.
Generation writes only versioned TASK002 font artifacts; original fonts are untouched.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path
import fontTools
from fontTools import subset
from fontTools.ttLib import TTFont

UNITY = Path(__file__).resolve().parents[4]
ROOT = UNITY / "Assets/HotpotSort"
SOURCE = ROOT / "Resources/Hotpot/TASK001/v7/r001/fonts/readable.otf"
OUT = ROOT / "Resources/Hotpot/TASK002/v9/r001/fonts"
TOKEN = re.compile(r'//[^\r\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])+\'')
ESCAPE = re.compile(r'\\(?:u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8}|x[0-9a-fA-F]{1,4}|.)')
ESCAPES = {'n':'\n','r':'\r','t':'\t','0':'\0','a':'\a','b':'\b','f':'\f','v':'\v'}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def inventory():
    chars = set(map(chr,range(32,127)))
    # The checked-in inventory is an additive compatibility seed: copy changes
    # must not silently remove glyphs that an accepted build already supported.
    retained = OUT / 'glyphs.txt'
    if retained.exists():
        chars.update(retained.read_text(encoding='utf-8'))
    sources = []
    data=[p for p in (ROOT/'Resources').rglob('*.json') if 'TASK002' not in p.parts]
    for path in sorted([*(ROOT / 'Runtime').rglob('*.cs'), *(ROOT / 'Contracts').rglob('*.cs'), *data]):
        if 'Editor' in path.parts or 'Diagnostics~' in path.parts:
            continue
        for match in TOKEN.finditer(path.read_text(encoding='utf-8-sig')):
            literal = match.group()
            if literal.startswith(('//','/*')):
                continue
            if literal.startswith('@"'):
                content = literal[2:-1].replace('""','"')
            else:
                def unescape(m):
                    token=m.group()[1:]
                    return chr(int(token[1:],16)) if token[0] in 'uUx' else ESCAPES.get(token,token)
                content=ESCAPE.sub(unescape,literal[1:-1])
            chars.update(ch for ch in content if ord(ch)>=32 and not 0x7f<=ord(ch)<=0x9f and not 0xd800<=ord(ch)<=0xdfff)
        sources.append({'path':path.relative_to(UNITY).as_posix(),'sha256':digest(path)})
    return ''.join(sorted(chars)), sources

def build():
    if fontTools.__version__ != '4.60.1':
        raise RuntimeError('Use pinned fonttools==4.60.1 for deterministic generation')
    chars,sources=inventory()
    font=TTFont(SOURCE,recalcTimestamp=False)
    missing=sorted(set(map(ord,chars))-set(font.getBestCmap()))
    if missing:
        raise ValueError('Source font missing '+','.join(f'U+{c:04X}' for c in missing))
    license_text=(SOURCE.parent/'NotoSansCJKsc-LICENSE.txt').read_text(encoding='utf-8')
    source_names={i:font['name'].getDebugName(i) for i in (0,1,2,4,6,13,14)}
    options=subset.Options()
    options.recalc_timestamp=False
    options.canonical_order=True
    options.name_IDs=['*']
    options.name_legacy=True
    options.name_languages=['*']
    options.layout_features=['*']
    sub=subset.Subsetter(options=options)
    sub.populate(unicodes=list(map(ord,chars)))
    sub.subset(font)
    # Give the modified font its own family/PostScript names while retaining copyright/license.
    names={1:'Hotpot Modern Sans',3:'HotpotModernSans-TASK002-v9-r001',4:'Hotpot Modern Sans Regular',6:'HotpotModernSans-Regular',16:'Hotpot Modern Sans',17:'Regular'}
    for record in font['name'].names:
        if record.nameID in names:
            record.string=names[record.nameID].encode(record.getEncoding())
    if 'CFF ' in font:
        cff=font['CFF '].cff
        cff.fontNames=['HotpotModernSans-Regular']
        cff.topDictIndex[0].FamilyName='Hotpot Modern Sans'
        cff.topDictIndex[0].FullName='Hotpot Modern Sans Regular'
    OUT.mkdir(parents=True,exist_ok=True)
    target=OUT/'modern-sans.otf'
    font.save(target,reorderTables=True)
    (OUT/'glyphs.txt').write_text(chars,encoding='utf-8',newline='\n')
    (OUT/'OFL.txt').write_text(license_text,encoding='utf-8',newline='\n')
    manifest={'task':'TASK-002','version':9,'fontRole':'modern-sans','resourceAddress':'Hotpot/TASK002/v9/r001/fonts/modern-sans',
        'generator':'Assets/HotpotSort/Editor/TASK002V9/build_font_subset.py','generatorSha256':digest(Path(__file__)),'generationCommand':'python Unity/Assets/HotpotSort/Editor/TASK002V9/build_font_subset.py',
        'source':SOURCE.relative_to(UNITY).as_posix(),'sourceSha256':digest(SOURCE),'sourceNames':source_names,
        'license':'SIL Open Font License 1.1','licenseSha256':digest(OUT/'OFL.txt'),
        'fontToolsVersion':fontTools.__version__,'glyphInventoryPolicy':'Retain checked-in glyph inventory; add ASCII 32-126, C# runtime/contracts literals and Resources JSON strings; Editor, Diagnostics~ and generated TASK002 evidence excluded; open-data nicknames use system fonts',
        'glyphCount':len(chars),'glyphsSha256':digest(OUT/'glyphs.txt'),'fontSha256':digest(target),'fontBytes':target.stat().st_size,
        'parameters':{'recalc_timestamp':False,'canonical_order':True,'name_IDs':['*'],'name_legacy':True,'name_languages':['*'],'layout_features':['*'],'derivativeFamily':'Hotpot Modern Sans'},'sources':sources}
    (OUT/'font-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    return check()

def check():
    chars,_=inventory()
    manifest=json.loads((OUT/'font-manifest.json').read_text(encoding='utf-8'))
    font=TTFont(OUT/'modern-sans.otf')
    assert set(map(ord,chars))<=set(font.getBestCmap()), 'Runtime text has missing glyphs; regenerate subset'
    assert set(chars)<=set((OUT/'glyphs.txt').read_text(encoding='utf-8')), 'Runtime text inventory grew; regenerate subset'
    for name,key in [('modern-sans.otf','fontSha256'),('glyphs.txt','glyphsSha256'),('OFL.txt','licenseSha256')]:
        assert digest(OUT/name)==manifest[key], 'Font artifact hash mismatch: '+name
    assert digest(SOURCE)==manifest['sourceSha256'], 'Source font changed'
    return {'status':'PASS','glyphCount':len(chars),'fontBytes':(OUT/'modern-sans.otf').stat().st_size,'fontSha256':digest(OUT/'modern-sans.otf'),'fontToolsVersion':fontTools.__version__}

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--check',action='store_true')
    args=parser.parse_args()
    print(json.dumps(check() if args.check else build()))
