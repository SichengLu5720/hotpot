'use strict';
const $ = id => document.getElementById(id);
const fragment = new URLSearchParams(location.hash.slice(1));
if (fragment.get('token')) { sessionStorage.setItem('harnessToken', fragment.get('token')); history.replaceState(null, '', '/'); }
const token = sessionStorage.getItem('harnessToken') || '';
let state, config, previewed = null;
const effortValues = ['', 'none', 'minimal', 'low', 'medium', 'high', 'xhigh', 'max', 'ultra'];
function node(tag, text, cls) { const n = document.createElement(tag); if (text !== undefined) n.textContent = text; if(cls) n.className = cls; return n; }
function message(text, error=false) { $('message').textContent = text; $('message').className = error ? 'error' : ''; }
function dirty() { previewed=null; $('apply').disabled=true; $('diff').textContent='草稿已变化，请重新预览。'; }
function select(options, value, change) { const s=node('select'); for(const [v,t] of options){const o=node('option',t);o.value=v;s.append(o);} s.value=value; s.addEventListener('change',()=>change(s.value));return s; }
function textInput(value, change) { const i=node('input');i.type='text';i.value=value;i.placeholder='继承';i.addEventListener('input',()=>{change(i.value);dirty();});return i; }
async function api(path, data) { const r=await fetch(path,{method:data?'POST':'GET',headers:{'X-Harness-Token':token,...(data?{'Content-Type':'application/json'}:{})},body:data?JSON.stringify(data):undefined});const v=await r.json();if(!r.ok)throw Error(v.error||r.statusText);return v; }
function render(){
 $('root').textContent=state.root;
 $('sync').textContent=state.rows.filter(r=>r.synced&&!r.manual_drift).length+' / '+state.rows.length+' 已同步';
 $('observed').textContent=state.rows.filter(r=>r.recent_runs.some(x=>x.observed)).length+' / '+state.rows.length+' 有宿主记录';
 const profiles=Object.keys(config.profiles).map(x=>[x,x]);
 $('active-profile').replaceChildren(...select(profiles,config.active_profile,()=>{}).children);$('active-profile').value=config.active_profile;
 $('batch-profile').replaceChildren(...select(profiles,config.active_profile,()=>{}).children);$('batch-profile').value=config.active_profile;
 $('profiles').replaceChildren();
 for(const [id,p] of Object.entries(config.profiles)){const tr=node('tr');tr.append(node('td',id));let td=node('td');td.append(textInput(p.model,v=>p.model=v));tr.append(td);td=node('td');td.append(select(effortValues.map(x=>[x,x||'继承宿主']),p.effort,v=>{p.effort=v;dirty();}));tr.append(td);$('profiles').append(tr);}
 $('agents').replaceChildren();
 for(const r of state.rows){const a=config.agents[r.role],tr=node('tr');let td=node('td');let label=node('label');const check=node('input');check.type='checkbox';check.checked=a.enabled;check.addEventListener('change',()=>{a.enabled=check.checked;dirty();});label.append(check,node('span',r.display_name,'role-name'));td.append(label,node('code',r.role,'role-key'),node('span',r.caption,'role-caption'));const b=r.backend_binding;td.append(node('span','后端绑定：'+b.config_key+' → '+b.agent_file+'\n派发步骤：'+(b.dispatch_steps.join(', ')||'无'),'role-binding'));tr.append(td);
 td=node('td');td.append(select([['','默认档位'],...profiles],a.profile,v=>{a.profile=v;dirty();}));tr.append(td);
 td=node('td');td.append(textInput(a.model,v=>a.model=v));tr.append(td);
 td=node('td');td.append(select(effortValues.map(x=>[x,x||'继承档位']),a.effort,v=>{a.effort=v;dirty();}));tr.append(td);
 td=node('td');td.append(node('div',(r.native.model||'继承宿主')+'\n'+(r.native.effort||'继承 effort'),'native'));td.append(node('small',r.manual_drift?'手工漂移':(r.synced?'已同步':'待同步'),r.synced&&!r.manual_drift?'ok':'warn'));tr.append(td);
 const latest=r.recent_runs[0];td=node('td');td.append(node('div',latest?(latest.observed?latest.observed.model+' / '+(latest.observed.effort||'未记录'): '实际模型未知')+'\n'+latest.run_id+' · '+latest.status:'暂无运行记录','observed'));tr.append(td);$('agents').append(tr);}
 $('transactions').replaceChildren();for(const t of state.transactions.filter(t=>t.status==='committed')){const o=node('option',t.id+' · '+t.at.slice(0,19));o.value=t.id;$('transactions').append(o);}
 if(state.pending_transactions.length)message('存在未完成事务。请先在终端运行 models recover。',true);
}
async function reload(){state=await api('/api/status');config=structuredClone(state.config);render();previewed=null;$('apply').disabled=true;}
function action(id, fn){$(id).addEventListener('click',async()=>{try{$(id).disabled=true;await fn();}catch(e){message(e.message,true);}finally{if(id!=='apply'||previewed)$(id).disabled=false;}});}
$('active-profile').addEventListener('change',()=>{config.active_profile=$('active-profile').value;dirty();});
$('reconcile').addEventListener('change',dirty);
action('reload',async()=>{await reload();message('已重新读取文件；未应用草稿被丢弃。');});
action('batch',async()=>{for(const a of Object.values(config.agents)){a.profile=$('batch-profile').value;a.model='';a.effort='';}render();dirty();message('已修改所有 Agent 的草稿，尚未应用。');});
action('preview',async()=>{const p=await api('/api/preview',{config,reconcile:$('reconcile').checked});previewed={etag:p.etag,config:structuredClone(config)};$('diff').textContent=p.diff||'无文件差异。';$('apply').disabled=false;message('预览完成。Apply 仅写本工作区配置。');});
action('apply',async()=>{if(!previewed)throw Error('请先预览');if(!confirm('应用这些配置到当前工作区？仅影响新派发。'))return;const p=await api('/api/apply',{config:previewed.config,etag:previewed.etag,reconcile:$('reconcile').checked,confirmed:true});await reload();$('diff').textContent='已应用 '+p.transaction;message('已写入配置。宿主重新加载后的新派发才能使用；实际模型仍需运行证据确认。');});
action('rollback',async()=>{const id=$('transactions').value;if(!id)throw Error('没有可回滚事务');if(!confirm('回滚 '+id+'？不撤销已产生的代码或资源。'))return;const p=await api('/api/rollback',{transaction:id,confirmed:true});await reload();message('已回滚；事务 '+p.transaction);});
reload().catch(e=>message(e.message+'；请使用终端打印的含 token 地址。',true));
