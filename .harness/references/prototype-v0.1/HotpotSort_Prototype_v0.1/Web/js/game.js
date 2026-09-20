(function(){'use strict';
const D=window.HotpotData,{Game}=window.HotpotCore,{World}=window.HotpotPhysics,A=window.HotpotArt;
const canvas=document.getElementById('game'),ctx=canvas.getContext('2d'),W=D.rules.width,H=D.rules.height;
const $=id=>document.getElementById(id),state={game:null,world:null,paused:false,debug:false,clock:0,accumulator:0,busyUntil:0,flights:[],tasks:[],ui:null,hover:null,overlayShown:false};
const C={ink:'#2F5048',muted:'#86927C',cream:'#FBF5E6',coral:'#CB6956',jade:'#8DAA97'};
function text(s,x,y,size=12,color=C.ink,align='left',weight=500){ctx.fillStyle=color;ctx.font=`${weight} ${size}px system-ui,-apple-system,"Microsoft YaHei",sans-serif`;ctx.textAlign=align;ctx.textBaseline='middle';ctx.fillText(s,x,y);}
function panel(x,y,w,h,r,fill,stroke=null){A.round(ctx,x,y,w,h,r);ctx.fillStyle=fill;ctx.fill();if(stroke){ctx.strokeStyle=stroke;ctx.lineWidth=1;ctx.stroke();}}
function copyUI(){return {orders:state.game.orders.map(o=>({open:o.open,kind:o.kind,items:o.items.map(t=>({...t}))})),buffer:state.game.buffer.map(t=>t?{...t}:null)};}
function orderPoint(slot){return {x:61+slot*98,y:151};}function bufferPoint(slot){return {x:105+slot*63,y:277};}
function reset(id='A',seed=7,failOnFull=true){
 state.game=new Game(D,id,seed,{failOnFull});state.world=new World(D.rules,state.game.seed);state.ui=copyUI();state.game.drainEvents();state.clock=0;state.accumulator=0;state.busyUntil=0;state.flights=[];state.tasks=[];state.hover=null;state.paused=false;state.overlayShown=false;$('overlay').classList.remove('open');
 $('level').value=id;$('seed').value=state.game.seed;$('failure').value=failOnFull?'full':'overflow';
 // Fixed, deterministic warm-up: all plates still pass the same height gate.
 for(let i=0;i<480;i++)state.world.step(state.game,1/120);state.game.drainEvents();state.world.audit(state.game);canvas.focus({preventScroll:true});
}
function toast(msg){$('toast').textContent=msg;$('toast').classList.add('show');clearTimeout(toast.timer);toast.timer=setTimeout(()=>$('toast').classList.remove('show'),2000);}
function showMenu(end=false){
 state.paused=true;state.overlayShown=true;const g=state.game;
 $('modal-tag').textContent=end?(g.status==='won'?'HOT POT SORT / ALL SERVED':'HOT POT SORT / TRY AGAIN'):'HOT POT SORT / PAUSED';
 $('modal-title').textContent=end?(g.status==='won'?'全部出锅！':'暂存区满了'):'歇口气，再开锅';
 $('modal-desc').textContent=end?(g.status==='won'?`${g.total} 件食材，${g.total/3} 份订单。本局点击 ${g.moves} 次。`:`已完成 ${g.completed} / ${g.total} 件。${g.rules.failOnFull?'本局约定：第五格占满，自动结算后失败。':'本局约定：下一件需要暂存但没有空位时失败。'}`):'点击盘里的单件食材。相同食材凑满三件完成订单，没有对应订单则进入五格暂存。';
 $('resume').style.display=end?'none':'block';$('apply').textContent=end?'重开 / 使用以上配置':'按以上配置重开';
 $('level').value=g.level.id;$('seed').value=g.seed;$('failure').value=g.rules.failOnFull?'full':'overflow';$('overlay').classList.add('open');(end?$('apply'):$('resume')).focus();
}
function resume(){if(state.game.status!=='playing')return;state.paused=false;state.overlayShown=false;$('overlay').classList.remove('open');canvas.focus({preventScroll:true});}
function exportLog(){
 const record=state.game.exportReplay();record.physics={note:'Final positions only; command replay is logic-only, not a timed physics replay.',steps:state.world.steps,bodies:state.world.bodies.map(b=>({...b}))};
 const blob=new Blob([JSON.stringify(record,null,2)],{type:'application/json'}),u=URL.createObjectURL(blob),a=document.createElement('a');a.href=u;a.download=`hotpot-${record.level}-seed${record.seed}-${record.final.status}.json`;a.click();setTimeout(()=>URL.revokeObjectURL(u),1500);toast('本局规则、操作与选单原因已导出');
}
function schedule(events,source){
 let cursor=state.clock;const duration=matchMedia('(prefers-reduced-motion: reduce)').matches?.06:.19;
 for(const e of events){
   if(e.type==='move'){
     const from=e.from==='plate'?source:bufferPoint(e.fromSlot),to=e.to==='order'?orderPoint(e.slot):bufferPoint(e.slot);
     if(e.from==='buffer')state.tasks.push({at:cursor,fn:()=>{state.ui.buffer[e.fromSlot]=null;}});
     state.flights.push({kind:e.kind,from,to,start:cursor,end:cursor+duration});cursor+=duration;
     state.tasks.push({at:cursor,fn:()=>{const t={id:e.id,kind:e.kind};if(e.to==='order')state.ui.orders[e.slot].items.push(t);else state.ui.buffer[e.slot]=t;}});
   }else if(e.type==='complete'){
     state.tasks.push({at:cursor+.03,fn:()=>{state.ui.orders[e.slot].items=[];state.ui.orders[e.slot].kind=-1;}});cursor+=.08;
   }else if(e.type==='target')state.tasks.push({at:cursor,fn:()=>{state.ui.orders[e.slot].kind=e.kind;}});
 }
 state.busyUntil=cursor+.03;
}
function tap(id){
 if(state.paused||state.clock<state.busyUntil||state.game.status!=='playing')return false;
 const p=state.game.active.find(p=>p.items.some(t=>t.id===id));if(!p)return false;
 const t=p.items.find(t=>t.id===id),position=state.world.position(p,t);if(!position)return false;
 const result=state.game.tap(id);const events=state.game.drainEvents();state.world.sync(state.game);
 if(result.accepted){schedule(events,position);state.hover=null;}else if(state.game.status==='lost')state.busyUntil=state.clock+.1;
 return result.accepted;
}
function coords(e){const r=canvas.getBoundingClientRect();return {x:(e.clientX-r.left)*W/r.width,y:(e.clientY-r.top)*H/r.height};}
canvas.addEventListener('pointerdown',e=>{e.preventDefault();const q=coords(e);if(q.y<78&&q.x>346){if(state.paused)resume();else showMenu();return;}if(state.paused)return;if(q.y>106&&q.y<221&&q.x>204){toast('预留订单位：原型不开放扩展');return;}const hit=state.world.hit(state.game,q.x,q.y);if(hit)tap(hit.item.id);});
canvas.addEventListener('pointermove',e=>{const q=coords(e);state.hover=state.world.hit(state.game,q.x,q.y);canvas.style.cursor=state.hover||q.y<78&&q.x>346?'pointer':'default';});canvas.addEventListener('pointerleave',()=>state.hover=null);
$('pause-btn').onclick=()=>showMenu();$('restart-btn').onclick=()=>reset(state.game.level.id,state.game.seed,state.game.rules.failOnFull);$('debug-btn').onclick=()=>{state.debug=!state.debug;toast(state.debug?'调试信息已打开':'调试信息已关闭');};$('export-btn').onclick=exportLog;$('export-mobile').onclick=exportLog;$('resume').onclick=resume;
$('apply').onclick=()=>{const seed=Number($('seed').value);if(!Number.isInteger(seed)||seed<1||seed>4294967295){toast('种子请输入 1 到 4294967295 的整数');return;}reset($('level').value,seed,$('failure').value==='full');};
document.addEventListener('keydown',e=>{if(['INPUT','SELECT','TEXTAREA'].includes(document.activeElement.tagName))return;if(e.key==='Escape'){e.preventDefault();if(state.paused)resume();else showMenu();}else if(e.key.toLowerCase()==='r'){reset(state.game.level.id,state.game.seed,state.game.rules.failOnFull);}else if(e.key.toLowerCase()==='d')state.debug=!state.debug;
 // Trap keyboard focus in the open dialog.
 if(e.key==='Tab'&&state.paused){const els=[...$('overlay').querySelectorAll('button,input,select,summary')].filter(el=>el.offsetParent!==null);const first=els[0],last=els[els.length-1];if(e.shiftKey&&document.activeElement===first){last.focus();e.preventDefault();}else if(!e.shiftKey&&document.activeElement===last){first.focus();e.preventDefault();}}
});
document.addEventListener('visibilitychange',()=>{if(document.hidden&&state.game.status==='playing'&&!state.paused)showMenu();});
function drawOrder(o,i){
 const x=16+i*98,p=orderPoint(i);panel(x,104,90,117,16,o.open?'#FFFBEF':'#E7EBDB',o.open?'#D9DECA':'#D5DDCC');
 if(!o.open){text('+',p.x,146,37,'#A6B5A0','center',400);text('待开放',p.x,188,10,'#91A18D','center');return;}
 // Copper hotpot icon, decorative only: an order still just needs 3 identical items.
 A.line(ctx,[[p.x-33,p.y],[p.x-38,p.y],[p.x-38,p.y+10],[p.x-30,p.y+10]],'#C1A785',3);A.line(ctx,[[p.x+33,p.y],[p.x+38,p.y],[p.x+38,p.y+10],[p.x+30,p.y+10]],'#C1A785',3);
 A.oval(ctx,p.x,p.y+9,32,20,'#C58E66','#A27C59');A.oval(ctx,p.x,p.y-2,33,23,'#E7C697','#B99A70');A.oval(ctx,p.x,p.y-3,27,17,'#EFE2B7');
 if(o.kind>=0){A.food(ctx,o.kind,p.x,p.y-5,21,false);text(D.ingredients[o.kind].name,p.x,183,11,C.ink,'center',600);}else{text('✓',p.x,p.y-4,24,C.jade,'center');text('已出齐',p.x,183,11,C.muted,'center');}
 for(let j=0;j<3;j++)A.disk(ctx,p.x+(j-1)*11,203,3.2,j<o.items.length?C.coral:'#DEDCC6');text(`${o.items.length}/3`,x+72,114,8,'#9A9E85','center');
}
function draw(){
 const dpr=Math.min(window.devicePixelRatio||1,2);if(canvas.width!==Math.round(W*dpr)||canvas.height!==Math.round(H*dpr)){canvas.width=Math.round(W*dpr);canvas.height=Math.round(H*dpr);}ctx.setTransform(dpr,0,0,dpr,0,0);ctx.clearRect(0,0,W,H);
 const g=state.game,world=state.world;
 const bg=ctx.createLinearGradient(0,0,0,H);bg.addColorStop(0,'#F9F3E2');bg.addColorStop(.4,'#E7EAD8');bg.addColorStop(1,'#CCDCC6');ctx.fillStyle=bg;ctx.fillRect(0,0,W,H);
 // Table grain, deterministic and subtle.
 for(let y=320;y<845;y+=30){ctx.strokeStyle='rgba(113,143,109,.045)';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(6,y);ctx.bezierCurveTo(140,y-5,250,y+6,414,y);ctx.stroke();}
 panel(14,305,392,531,20,'rgba(236,240,220,.36)','rgba(158,181,152,.32)');
 if(state.debug){ctx.setLineDash([5,4]);A.line(ctx,[[20,D.rules.spawnGate],[400,D.rules.spawnGate]],'#C47759',1);ctx.setLineDash([]);text('生成高度线 · 另有生成点防重叠检查',25,D.rules.spawnGate-9,8,'#A37458');}
 ctx.save();ctx.beginPath();ctx.rect(12,304,396,530);ctx.clip();
 for(const b of world.bodies){const p=g.active.find(p=>p.id===b.id);if(!p)continue;A.plate(ctx,b.x,b.y,b.r,b.id,state.debug);
   for(const t of p.items){const q=world.position(p,t);if(state.hover&&state.hover.item.id===t.id){A.disk(ctx,q.x,q.y,q.r+4,'rgba(228,177,90,.18)','#D7A557',1.2);}A.food(ctx,t.kind,q.x,q.y,q.r,true);}
 }
 ctx.restore();
 // HUD is drawn over the board and cannot be picked through.
 const hg=ctx.createLinearGradient(0,0,0,306);hg.addColorStop(0,'#FBF6E7');hg.addColorStop(1,'#F2F0DC');ctx.fillStyle=hg;ctx.fillRect(0,0,W,304);
 panel(20,23,36,37,11,C.coral);text('锅',38,42,22,'#FFF4DD','center',700);text('开锅啦',68,38,25,C.ink,'left',700);text('HOT POT SORT',69,59,8.5,'#889580','left',600);
 panel(271,29,72,25,12,'#E3E9D7');text(g.level.name,307,42,11,'#667F66','center',600);A.disk(ctx,381,42,18,'#F8F8EA','#D4DDC9');A.line(ctx,[[377,36],[377,48]],'#6D8975',2.5);A.line(ctx,[[385,36],[385,48]],'#6D8975',2.5);
 text('今日订单',19,87,11,'#748572','left',600);text('同类 3 件 · 自动出锅',401,87,9.5,'#97A087','right');
 state.ui.orders.forEach(drawOrder);
 text('暂存食材',20,241,10,'#728771','left',600);text(`${state.ui.buffer.filter(Boolean).length} / 5`,399,241,10,state.ui.buffer.filter(Boolean).length>=4?C.coral:'#96A086','right');
 panel(16,255,50,46,10,'#DFE6D2');text('已出锅',41,267,8,'#7F9075','center');text(`${g.completed}/${g.total}`,41,285,11,C.ink,'center',700);
 for(let i=0;i<5;i++){const p=bufferPoint(i);panel(p.x-28,255,56,46,9,'#FEFBEF','#C7D5BD');if(state.ui.buffer[i])A.food(ctx,state.ui.buffer[i].kind,p.x,p.y,17,true);else{text('·',p.x,p.y,21,'#D3DDC6','center');}}
 // Footer is UI, not extra game mechanics.
 panel(14,842,392,47,15,'rgba(251,249,230,.78)');text('点一件食材，同类凑满 3 件出锅',210,858,11,'#52735C','center',600);
 text(`${world.spawned} / ${g.level.plates.length} 盘已上桌  ·  ${g.completed/3} 份已完成  ·  种子 ${g.seed}`,210,877,8.6,'#87977C','center');
 for(const f of state.flights){if(state.clock<f.start||state.clock>f.end)continue;const v=(state.clock-f.start)/(f.end-f.start),t=1-Math.pow(1-v,3),x=f.from.x+(f.to.x-f.from.x)*t,y=f.from.y+(f.to.y-f.from.y)*t-Math.sin(v*Math.PI)*20;A.food(ctx,f.kind,x,y,19,true);}
 if(state.debug){panel(24,312,269,45,8,'rgba(40,70,58,.86)');text(`active=${g.active.length} pending=${g.pending.length} buffer=${g.buffer.filter(Boolean).length} rng=${g.rng.state}`,32,324,8.5,'#F0F2DD','left');text(`供给 ${world.blocked?'暂停':'可继续'}  |  commands=${g.commands.length}  |  ${g.status}`,32,340,8.5,'#F0F2DD','left');}
 if(state.hover&&!state.paused&&state.clock>=state.busyUntil){const q=state.hover.point,x=Math.min(344,Math.max(24,q.x-31)),y=Math.max(311,q.y-42);panel(x,y,65,20,7,'#315549E8');text(D.ingredients[state.hover.item.kind].name,x+32.5,y+10,9,'#FFF9E7','center');}
}
let last=performance.now();function frame(now){
 try{
  const dt=Math.min(.05,Math.max(0,(now-last)/1000));last=now;
  if(!state.paused){state.clock+=dt;state.tasks.sort((a,b)=>a.at-b.at);while(state.tasks.length&&state.tasks[0].at<=state.clock)state.tasks.shift().fn();state.flights=state.flights.filter(f=>f.end>=state.clock);
    state.accumulator+=dt;while(state.accumulator>=D.rules.physicsStep){state.world.step(state.game,D.rules.physicsStep);state.accumulator-=D.rules.physicsStep;}state.game.drainEvents();
    if(state.game.status!=='playing'&&state.clock>=state.busyUntil&&!state.overlayShown){state.ui=copyUI();showMenu(true);}
  }draw();requestAnimationFrame(frame);
 }catch(err){$('error-banner').style.display='block';$('error-banner').textContent='运行错误：'+err.message;console.error(err);}
}
// Test/debug bridge is intentionally explicit, never used to rescue normal gameplay.
window.hotpot={get game(){return state.game;},get world(){return state.world;},get state(){return state;},snapshot:()=>state.game.snapshot(),reset,tap,showMenu,resume,exportLog,draw,
 step(seconds){for(let i=0;i<Math.floor(seconds*120);i++){state.clock+=1/120;while(state.tasks.length&&state.tasks[0].at<=state.clock)state.tasks.shift().fn();state.world.step(state.game,1/120);}draw();}};
reset();requestAnimationFrame(frame);
})();
