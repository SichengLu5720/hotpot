"""Export newly authored Canvas artwork as Unity PNG textures. Requires Playwright.
No OS fonts are copied. Ingredient exports have no text and no external requests.
"""
from pathlib import Path
import base64, json
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'Unity/Assets/HotpotSort/Resources/Hotpot'
with sync_playwright() as p:
    import shutil
    exe=shutil.which('chromium')
    browser=p.chromium.launch(**({'executable_path':exe} if exe else {}),headless=True,args=['--no-sandbox'])
    page=browser.new_page()
    page.set_content('<canvas id="c" width="128" height="128"></canvas><script>'+ (ROOT/'Web/js/art.js').read_text()+'</script>')
    for kind in range(16):
        data=page.evaluate('''k=>{const c=document.getElementById('c'),x=c.getContext('2d');x.clearRect(0,0,128,128);HotpotArt.food(x,k,64,60,48,false);return c.toDataURL('image/png').split(',')[1];}''',kind)
        (out/f'food_{kind:02}.png').write_bytes(base64.b64decode(data))
    data=page.evaluate('''()=>{const c=document.getElementById('c'),x=c.getContext('2d');x.clearRect(0,0,128,128);HotpotArt.plate(x,64,62,58,0,false);return c.toDataURL('image/png').split(',')[1];}''')
    (out/'plate.png').write_bytes(base64.b64decode(data))
    browser.close()
print('Exported 16 ingredients and one plate to Unity Resources.')
