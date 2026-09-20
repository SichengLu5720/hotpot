"""Headless browser regression. Uses inline bundle injection: no local server needed.
Requirements (tests only): Python 3 + playwright + a Chromium installation.
"""
from pathlib import Path
import json, shutil
from playwright.sync_api import sync_playwright
ROOT=Path(__file__).resolve().parents[1]
HTML=(ROOT/'Web/standalone.html').read_text(encoding='utf-8')
results=[]
def check(name,fn):
    try:
        details=fn()
        results.append({'name':name,'passed':True,'details':details})
        print('PASS',name)
    except Exception as exc:
        results.append({'name':name,'passed':False,'error':str(exc)})
        print('FAIL',name,str(exc))
def require(condition,message='assertion failed'):
    if not condition:raise AssertionError(message)
def click_token(page,matching=True,touch=False):
    point=page.evaluate('''matching=>{const g=hotpot.game,w=hotpot.world;for(const p of g.active)for(const t of p.items){if(Boolean(g.findTarget(t.kind))!==matching)continue;const q=w.position(p,t);if(w.hit(g,q.x,q.y)?.item.id!==t.id)continue;const r=document.getElementById('game').getBoundingClientRect();return {x:r.x+q.x*r.width/420,y:r.y+q.y*r.height/900,id:t.id};}return null;}''',matching)
    require(point is not None,'no clickable item found')
    if touch:page.touchscreen.tap(point['x'],point['y'])
    else:page.mouse.click(point['x'],point['y'])
    page.evaluate('hotpot.step(.5)')
    return point
with sync_playwright() as p:
    exe=shutil.which('chromium')
    browser=p.chromium.launch(**({'executable_path':exe} if exe else {}),headless=True,args=['--no-sandbox'])
    errors=[];requests=[]
    page=browser.new_page(viewport={'width':1240,'height':980},device_scale_factor=1,accept_downloads=True)
    page.on('pageerror',lambda e:errors.append(str(e)));page.on('request',lambda r:requests.append(r.url))
    def initial():
        page.set_content(HTML,wait_until='load');page.wait_for_timeout(50)
        s=page.evaluate('hotpot.snapshot()')
        require(len(s['buffer'])==5);require([o['kind'] for o in s['orders']]==[0,5,-1,-1]);require(1<len(s['active'])<33)
        return {'active':len(s['active']),'pending':len(s['pending'])}
    check('desktop boots with two goals, five buffer cells and gated supply',initial)
    def pause():
        page.click('#pause-btn');require(page.locator('#overlay').get_attribute('class')=='overlay open')
        before=page.evaluate('hotpot.state.clock');page.wait_for_timeout(120);require(page.evaluate('hotpot.state.clock')==before)
        page.click('#resume');require(page.evaluate('hotpot.state.paused') is False)
    check('pause freezes simulation and resume continues',pause)
    def transfer():
        point=click_token(page,True);require(page.evaluate('hotpot.game.moves')==1);require(page.evaluate('hotpot.game.buffer.filter(Boolean).length')==0)
        return point
    check('real pointer click transfers matching ingredient to target',transfer)
    def retry():
        page.click('#restart-btn');s=page.evaluate('hotpot.snapshot()');require(s['moves']==0 and s['seed']==7 and s['completed']==0)
    check('restart clears gameplay state and keeps seed',retry)
    def doubleclick():
        page.evaluate('''()=>{const p=hotpot.game.active[0],q=hotpot.world.position(p,p.items[0]),r=document.getElementById('game').getBoundingClientRect(),c=document.getElementById('game');const args={clientX:r.x+q.x*r.width/420,clientY:r.y+q.y*r.height/900,bubbles:true};c.dispatchEvent(new PointerEvent('pointerdown',args));c.dispatchEvent(new PointerEvent('pointerdown',args));}''')
        require(page.evaluate('hotpot.game.moves')==1)
    check('rapid duplicate pointer input does not duplicate items',doubleclick)
    def config():
        page.click('#pause-btn');page.select_option('#level','B');page.fill('#seed','13');page.click('#apply');s=page.evaluate('hotpot.snapshot()');require(s['level']=='B' and s['seed']==13);require([o['kind'] for o in s['orders'][:2]]==[0,6])
    check('settings can select skeleton B and seed 13',config)
    def debug():
        page.click('#debug-btn');require(page.evaluate('hotpot.state.debug') is True);page.evaluate('hotpot.game.audit();hotpot.world.audit(hotpot.game)')
    check('debug overlay and model/world audits work',debug)
    def lost():
        page.evaluate("hotpot.reset('A',7,true)")
        for _ in range(5):click_token(page,False)
        page.wait_for_timeout(40);s=page.evaluate('hotpot.snapshot()');require(s['status']=='lost');require(sum(x is not None for x in s['buffer'])==5);require(page.locator('#modal-title').inner_text()=='暂存区满了')
        return {'reason':s['reason'],'moves':s['moves']}
    check('five real nonmatching clicks reach explicit failure screen',lost)
    def full_win():
        result=page.evaluate('''()=>{hotpot.reset('A',7,true);let turns=0;while(hotpot.game.status==='playing'&&turns++<240){hotpot.step(1.35);const g=hotpot.game,w=hotpot.world;const choices=g.active.flatMap(p=>p.items.map(t=>({p,t,q:w.position(p,t)}))).filter(o=>w.hit(g,o.q.x,o.q.y)?.item.id===o.t.id);let pick=choices.filter(o=>g.findTarget(o.t.kind)).sort((a,b)=>a.p.items.length-b.p.items.length||a.p.id-b.p.id)[0];if(!pick){const bc=Array(16).fill(0);g.buffer.filter(Boolean).forEach(t=>bc[t.kind]++);pick=choices.sort((a,b)=>a.p.items.length-b.p.items.length||bc[b.t.kind]-bc[a.t.kind]||a.p.id-b.p.id)[0];}if(!pick)break;const r=document.getElementById('game').getBoundingClientRect();document.getElementById('game').dispatchEvent(new PointerEvent('pointerdown',{clientX:r.x+pick.q.x*r.width/420,clientY:r.y+pick.q.y*r.height/900,bubbles:true}));}hotpot.step(2);hotpot.game.audit();return hotpot.snapshot();}''')
        require(result['status']=='won',str(result));page.wait_for_timeout(40);require(page.locator('#modal-title').inner_text()=='全部出锅！');return {'completed':result['completed'],'moves':result['moves'],'seed':result['seed']}
    check('full skeleton A reaches victory using gated supply and pointer events',full_win)
    def export():
        with page.expect_download(timeout=3000) as info:page.click('#export-mobile')
        download=info.value;out=ROOT/'qa/browser-export-example.json';download.save_as(out);record=json.loads(out.read_text());require(record['final']['status']=='won');require(record['commands']);return {'file':download.suggested_filename,'commands':len(record['commands'])}
    check('browser exports a real JSON replay download',export)
    def screenshot():
        page.evaluate("hotpot.reset('A',7,true);hotpot.state.debug=false;hotpot.draw()")
        page.screenshot(path=str(ROOT/'qa/browser_desktop.png'));page.locator('canvas').screenshot(path=str(ROOT/'qa/browser_game.png'));return 'qa/browser_game.png'
    check('desktop screenshot produced from running game',screenshot)
    mobile=browser.new_page(viewport={'width':390,'height':844},device_scale_factor=2,is_mobile=True,has_touch=True)
    mobile.on('pageerror',lambda e:errors.append(str(e)))
    def mobile_click():
        mobile.set_content(HTML,wait_until='load');mobile.wait_for_timeout(40)
        before=mobile.evaluate('hotpot.game.moves');click_token(mobile,True,True);require(mobile.evaluate('hotpot.game.moves')==before+1)
        bounds=mobile.locator('canvas').bounding_box();require(bounds['x']>=-1 and bounds['y']>=-1 and bounds['x']+bounds['width']<=391 and bounds['y']+bounds['height']<=845)
        mobile.screenshot(path=str(ROOT/'qa/browser_mobile.png'));return bounds
    check('390x844 touch viewport: scaled hit-test, visible board, no overflow',mobile_click)
    def mobile_pause():
        r=mobile.locator('canvas').bounding_box();mobile.touchscreen.tap(r['x']+381*r['width']/420,r['y']+42*r['height']/900)
        require(mobile.evaluate('hotpot.state.paused') is True);require(mobile.locator('#resume').is_visible());mobile.click('#resume');require(mobile.evaluate('hotpot.state.paused') is False)
    check('mobile pause and resume reachable from in-game HUD',mobile_pause)
    check('no uncaught browser errors',lambda:require(not errors,str(errors)))
    check('offline bundle emits no external network requests',lambda:require(not requests,str(requests)))
    version=browser.version;browser.close()
report={'browser':version,'mode':'headless Chromium; standalone HTML injected with set_content (file/localhost navigation restricted in environment)', 'passed':sum(r['passed'] for r in results),'failed':sum(not r['passed'] for r in results),'results':results,'uncaughtErrors':errors,'requests':requests,'scope':'Browser execution only, not Unity Editor or physical Android/iOS device validation.'}
(ROOT/'qa/browser-tests.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
if report['failed']:raise SystemExit(1)
