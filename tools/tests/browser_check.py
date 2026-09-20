"""Optional offline Chromium UI test; not collected by the standard-library test suite.
Requires Playwright and an installed browser. Set HARNESS_CHROMIUM as needed.
HTTP endpoints are tested separately in test_v1.py. No browser network policy changes.
"""
from pathlib import Path
import sys, tempfile, shutil, threading, json, os
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
from harnesslib import dashboard,models
from playwright.sync_api import sync_playwright
with tempfile.TemporaryDirectory(prefix='harness-ui-') as d:
    root=Path(d)/'workspace'
    shutil.copytree(ROOT,root,ignore=shutil.ignore_patterns('__pycache__','*.pyc'))
    def bridge(path, options):
        from harnesslib.common import json_read
        data=json.loads(options.get('body') or '{}')
        try:
            if path == '/api/status':
                out=dashboard.status_payload(root)
                out['transactions']=[]
                for f in (root/'.harness/model-transactions').glob('*/journal.json'):
                    j=json_read(f);out['transactions'].append({k:j[k] for k in ('id','at','status')})
                out['transactions'].sort(key=lambda x:x['at'],reverse=True)
            elif path == '/api/preview':
                q=models.preview(root,models.serialize(data['config']),reconcile=data.get('reconcile',False))
                out={k:q[k] for k in ('etag','diff')}
            elif path == '/api/apply':
                out=models.apply(root,models.serialize(data['config']),data['etag'],reconcile=data.get('reconcile',False))
            elif path == '/api/rollback':
                out=models.rollback(root,data['transaction'])
            else:raise RuntimeError('unknown offline endpoint')
            return {'ok':True,'value':out}
        except Exception as e:return {'ok':False,'value':{'error':str(e)}}
    result={};errors=[]
    try:
        with sync_playwright() as p:
            browser=p.chromium.launch(executable_path=(os.environ.get('HARNESS_CHROMIUM') or shutil.which('chromium')),headless=True,args=['--no-sandbox'])
            page=browser.new_page(viewport={'width':1440,'height':1100},device_scale_factor=1)
            page.on('pageerror',lambda e:errors.append(str(e)))
            # Browser navigation to loopback is blocked by administrator policy.
            # Test frontend offline against the same local Python model backend instead.
            page.expose_function('modelBridge',bridge)
            html=(ROOT/'tools/harnesslib/web/index.html').read_text(encoding='utf-8').replace('<link rel="stylesheet" href="/style.css">','').replace('<script src="/app.js"></script>','')
            page.set_content(html)
            page.add_style_tag(content=(ROOT/'tools/harnesslib/web/style.css').read_text(encoding='utf-8'))
            page.evaluate("""() => {
              Object.defineProperty(window,'sessionStorage',{value:{getItem:()=> 'offline-test',setItem:()=>{}}});
              window.fetch=async(path,options)=>{const r=await window.modelBridge(path,options||{});return {ok:r.ok,json:async()=>r.value,statusText:'offline-test'};};
            }""")
            page.add_script_tag(content=(ROOT/'tools/harnesslib/web/app.js').read_text(encoding='utf-8'))
            page.wait_for_function("document.querySelectorAll('#agents tr').length === 4")
            page.screenshot(path=str(Path(tempfile.gettempdir())/'harness-dashboard-preview.png'),full_page=True)
            result['initial_four_agents']=page.locator('#agents tr').count()==4
            row=page.locator('#agents tr').filter(has_text='code_builder')
            row.locator('input[type=text]').fill('synthetic-ui-model')
            page.locator('#preview').click()
            page.wait_for_function("document.querySelector('#diff').textContent.includes('synthetic-ui-model')")
            result['preview_no_write']=models.status(root)['config']['agents']['code_builder']['model']==''
            page.on('dialog',lambda dlg:dlg.accept())
            page.locator('#apply').click()
            page.wait_for_function("document.querySelector('#message').textContent.includes('已写入配置')")
            result['apply_writes']=models.status(root)['config']['agents']['code_builder']['model']=='synthetic-ui-model'
            page.locator('#rollback').click()
            page.wait_for_function("document.querySelector('#message').textContent.includes('已回滚')")
            result['rollback_restores']=models.status(root)['config']['agents']['code_builder']['model']==''
            page.locator('#batch-profile').select_option('economy')
            page.locator('#batch').click()
            result['batch_draft']=all(v=='economy' for v in page.locator('#agents tr td:nth-child(2) select').evaluate_all('(els)=>els.map(e=>e.value)'))
            page.set_viewport_size({'width':720,'height':1000})
            result['mobile_no_body_overflow']=page.evaluate('document.documentElement.scrollWidth<=window.innerWidth')
            browser.close()
        result['page_errors']=errors
    finally:
        pass
    result['method']='offline Chromium frontend with Python backend bridge; separate real HTTP tests cover endpoints'
    print(json.dumps(result,ensure_ascii=False,indent=2))
    assert all(result[k] for k in result if k not in ('page_errors','method')) and not errors
