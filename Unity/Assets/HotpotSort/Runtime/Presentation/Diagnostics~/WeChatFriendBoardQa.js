'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const crypto=require('node:crypto');
const source=path.resolve(__dirname,'../../../WeChatOpenData/index.js');
const board=require(source), results=[];
const date='2026-09-22T01:00:00.000Z';
const score=(n,owner='owner')=>JSON.stringify({schema:1,firstWins:n,updatedAtUtc:date,ownerMarker:owner});
const row=(name,n,owner='owner')=>({nickname:name,avatarUrl:'https://avatar/'+name,KVDataList:[{key:board.SCORE_KEY,value:score(n,owner)}]});
const view={x:12,y:48,width:720,height:1280,dpr:2};
const msg=(type,id=1,epoch=1,payload)=>JSON.stringify({version:1,type,viewEpoch:epoch,requestId:id,payload:payload || (type==='close'?{}:view)});
function test(name,run){try{run();results.push({name,passed:true});}catch(e){results.push({name,passed:false,error:e.stack});}}
function rig(options={}){
  const calls={friend:[],own:[],user:[],write:[],images:[],draw:[]},timeouts=new Map(),timerHistory=[],listeners={message:[],start:[],move:[],end:[]};let next=1;
  const register=(type,listener)=>{listeners[type].push(listener);calls[type]=listener;};
  const ctx=new Proxy({measureText:s=>({width:Array.from(s).length*8})},{get:(t,p)=>p in t?t[p]:(...args)=>calls.draw.push([p,...args])});
  const canvas={width:1,height:1,getContext:()=>ctx};
  const api={getSharedCanvas:()=>canvas,onMessage:f=>register('message',f),
    getFriendCloudStorage:o=>calls.friend.push(o),getUserCloudStorage:o=>calls.own.push(o),getUserInfo:o=>calls.user.push(o),setUserCloudStorage:o=>calls.write.push(o),
    createImage:()=>{const value={};calls.images.push(value);return value;},
    onTouchStart:f=>register('start',f),onTouchMove:f=>register('move',f),onTouchEnd:f=>register('end',f)};
  const timers={setTimeout:f=>{const id=next++;timeouts.set(id,f);timerHistory.push(f);return id;},clearTimeout:id=>timeouts.delete(id)};
  const surface=options.raw?null:board.create(api,timers);
  const complete=(records,own=score(4),name='本人')=>{
    calls.friend.at(-1).success({data:records});
    calls.own.at(-1).success({KVDataList:own===null?[]:[{key:board.SCORE_KEY,value:own}]});
    calls.user.at(-1).success({data:[{nickName:name,avatarUrl:'https://avatar/self'}]});
  };
  return {api,timers,surface,calls,canvas,timeouts,complete,listeners,timerHistory};
}
function touch(v,x,y){return {touches:[{clientX:(v.x+x*v.width/360)/v.dpr,clientY:(v.y+y*v.width/360)/v.dpr}]};}
function finalRows(r){
  const begin=r.calls.draw.findLastIndex(c=>c[0]==='setTransform');
  return r.calls.draw.slice(begin).filter(c=>c[0]==='fillText'&&String(c[1]).startsWith('friend-')).map(c=>({name:c[1],y:c[3]}));
}
const scrollRows=()=>Array.from({length:24},(_,i)=>row('friend-'+i,24-i,'other-'+i));
test('strict string protocol and payload validation',()=>{
  assert.ok(board.parseMessage(msg('open')));
  const bad=[null,{},'[]','null','{bad','"text"',msg('unknown'),msg('open',0),msg('open',1,0),
    msg('open',1,1,{...view,width:0}),msg('open',1,1,{...view,dpr:99}),msg('open',1,1,{...view,extra:1}),
    msg('open',1,1,{...view,height:8192,width:4096}),msg('close',1,1,{x:1}),
    msg('publishScore',1,0,{firstWins:-1,ownerMarker:'x',updatedAtUtc:date}),
    msg('publishScore',1,0,{firstWins:1,ownerMarker:'x',updatedAtUtc:'2026-02-31T01:00:00.000Z'}),
    msg('open').replace('"version":1','"version":2')];
  bad.forEach(v=>assert.equal(board.parseMessage(v),null));
});
test('competition ranks, stable platform ties, real score validation',()=>{
  const records=[row('a',4),row('b',9),row('c',9),row('d',0),row('invalid',-1)];
  const ranked=board.rankRows(records);assert.deepEqual(ranked.map(r=>r.nickname),['b','c','a','d']);assert.deepEqual(ranked.map(r=>r.rank),[1,1,3,4]);
  assert.equal(board.score(score(1.1)),null);assert.equal(board.score(score(2147483648)),null);assert.equal(board.score('{"schema":2}'),null);
});
test('missing owner marker preserves score but cannot identify self',()=>{
  const raw=JSON.parse(score(7));delete raw.ownerMarker;
  assert.equal(board.score(JSON.stringify(raw)).ownerMarker,'');
  const r=rig();r.surface.receive(msg('open'));r.complete([{nickname:'真实昵称',KVDataList:[{key:board.SCORE_KEY,value:JSON.stringify(raw)}]}],JSON.stringify(raw));
  assert.equal(r.surface.snapshot().rows[0].isSelf,false);assert.equal(r.surface.snapshot().self.rank,null);
});
test('duplicate owner marker never guesses self rank',()=>{
  const r=rig();r.surface.receive(msg('open'));r.complete([row('a',7),row('b',4)]);
  assert.equal(r.surface.snapshot().self.rank,null);assert.ok(r.surface.snapshot().rows.every(x=>!x.isSelf));
});
test('unique self marker, self only and actual nickname',()=>{
  const r=rig();r.surface.receive(msg('open'));assert.equal(r.surface.snapshot().status,'loading');r.complete([row('实际用户',4)]);
  assert.equal(r.surface.snapshot().status,'self-only');assert.equal(r.surface.snapshot().self.rank,1);assert.equal(r.surface.snapshot().self.nickname,'本人');
});
test('no score is not synthesized zero',()=>{
  const r=rig();r.surface.receive(msg('open'));r.complete([],null);assert.equal(r.surface.snapshot().status,'no-score');assert.equal(r.surface.snapshot().self.firstWins,null);
});
test('permission failure and tap retry',()=>{
  const r=rig();r.surface.receive(msg('open'));r.calls.friend[0].fail({errMsg:'scope denied'});r.calls.own[0].success({KVDataList:[]});r.calls.user[0].fail({errMsg:'no profile'});
  assert.equal(r.surface.snapshot().status,'permission');r.calls.start({touches:[{clientX:25,clientY:110}]});assert.equal(r.calls.friend.length,2);
});
test('network failure differs from permissions',()=>{
  const r=rig();r.surface.receive(msg('open'));r.calls.friend[0].fail({errMsg:'network disconnected'});r.calls.own[0].success({KVDataList:[]});r.calls.user[0].success({data:[]});assert.equal(r.surface.snapshot().status,'network');
});
test('missing developer privacy declaration is not a network or player permission failure',()=>{
  const r=rig();r.surface.receive(msg('open'));
  r.calls.friend[0].fail({errMsg:'getFriendCloudStorage:fail please go to mp to announce your privacy usage'});
  r.calls.own[0].success({KVDataList:[]});r.calls.user[0].success({data:[]});
  assert.equal(r.surface.snapshot().status,'privacy-declaration');
  assert.ok(r.calls.draw.some(c=>c[0]==='fillText'&&c[1]==='开发者尚未完成微信隐私声明'));
  r.calls.start({touches:[{clientX:25,clientY:110}]});assert.equal(r.calls.friend.length,2);
});
test('timeouts settle missing callbacks without endless loading',()=>{
  const r=rig();r.surface.receive(msg('open'));Array.from(r.timeouts.values()).forEach(f=>f());assert.equal(r.surface.snapshot().status,'network');assert.equal(r.timeouts.size,0);
});
test('close cancels callbacks and timers; stale epoch cannot reopen',()=>{
  const r=rig();r.surface.receive(msg('open'));const late=r.calls.friend[0];assert.ok(r.surface.receive(msg('close',2)));late.success({data:[row('late',10)]});
  assert.equal(r.surface.snapshot().status,'closed');assert.equal(r.timeouts.size,0);assert.equal(r.surface.receive(msg('open',3)),false);
  assert.equal(r.surface.receive(msg('open',4,2)),true);assert.equal(r.surface.receive(msg('close',5,1)),false);
});
test('refresh supersedes in-flight callbacks within same epoch',()=>{
  const r=rig();r.surface.receive(msg('open'));const old=r.calls.friend[0];r.surface.receive(msg('refresh',2));old.success({data:[row('stale',99)]});r.complete([row('current',2,'other')]);
  assert.equal(r.surface.snapshot().rows[0].nickname,'current');assert.equal(r.surface.receive(msg('refresh',2)),false);
});
test('invalid high request does not suppress next valid message',()=>{
  const r=rig();r.surface.receive(msg('open'));assert.equal(r.surface.receive(msg('refresh',999,99)),false);assert.equal(r.surface.receive(msg('refresh',2)),true);
});
test('avatar errors and long unicode nickname keep renderer alive',()=>{
  const r=rig();r.surface.receive(msg('open'));r.complete([row('🍲很长的名字'.repeat(70),8,'other')]);
  r.calls.images.forEach(i=>i.onerror());assert.equal(r.surface.snapshot().status,'ready');
  const texts=r.calls.draw.filter(x=>x[0]==='fillText').map(x=>String(x[1]));assert.ok(texts.some(x=>x.endsWith('…')));assert.ok(texts.every(x=>x.length<100));
});
test('closing disconnects image callbacks and new viewport resizes canvas',()=>{
  const r=rig();r.surface.receive(msg('open'));r.complete([row('one',3)]);const image=r.calls.images[0];r.surface.receive(msg('close',2));assert.equal(image.onload,null);
  r.surface.receive(msg('open',3,2,{x:0,y:80,width:1440,height:2400,dpr:3}));assert.equal(r.canvas.width,1440);assert.equal(r.canvas.height,2400);
});
test('publish reads remote and never replaces higher score with zero',()=>{
  const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:0,ownerMarker:'device',updatedAtUtc:date}));r.calls.own[0].success({KVDataList:[{key:board.SCORE_KEY,value:score(21,'remote')}]});
  assert.equal(r.calls.write.length,0);assert.equal(r.surface.snapshot().writing,false);
});
test('publish maximum, preserve remote marker and serialize writes',()=>{
  const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:7,ownerMarker:'device',updatedAtUtc:date}));
  r.surface.receive(msg('publishScore',2,0,{firstWins:9,ownerMarker:'device',updatedAtUtc:date}));
  r.calls.own[0].success({KVDataList:[{key:board.SCORE_KEY,value:score(3,'remote')}]});
  const value=JSON.parse(r.calls.write[0].KVDataList[0].value);assert.equal(value.firstWins,9);assert.equal(value.ownerMarker,'remote');
  assert.equal(r.calls.own.length,1);r.calls.write[0].success({});assert.equal(r.calls.own.length,2);
  r.calls.own[1].success({KVDataList:[{key:board.SCORE_KEY,value:score(9,'remote')}]});assert.equal(r.calls.write.length,1);
});
test('failed, malformed or future-schema remote reads prohibit writes',()=>{
  for(const mode of ['fail','bad','future']){
    const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:5,ownerMarker:'device',updatedAtUtc:date}));
    if(mode==='fail')r.calls.own[0].fail({errMsg:'network'});else r.calls.own[0].success({KVDataList:[{key:board.SCORE_KEY,value:mode==='bad'?'oops':'{"schema":2}'}]});
    assert.equal(r.calls.write.length,0);assert.equal(r.surface.snapshot().writing,false);
  }
});
test('write failure is not success and next explicit publish can retry',()=>{
  const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:4,ownerMarker:'device',updatedAtUtc:date}));r.calls.own[0].success({KVDataList:[]});r.calls.write[0].fail({errMsg:'network'});
  r.surface.receive(msg('publishScore',2,0,{firstWins:2,ownerMarker:'device',updatedAtUtc:date}));r.calls.own[1].success({KVDataList:[]});assert.equal(JSON.parse(r.calls.write[1].KVDataList[0].value).firstWins,4);
});
test('late publication cannot reopen closed display',()=>{
  const r=rig();r.surface.receive(msg('open'));r.surface.receive(msg('publishScore',2,1,{firstWins:6,ownerMarker:'device',updatedAtUtc:date}));const read=r.calls.own.at(-1);
  r.surface.receive(msg('close',3));read.success({KVDataList:[]});r.calls.write[0].success({});assert.equal(r.surface.snapshot().status,'closed');assert.equal(r.calls.friend.length,1);
});
test('publication begun before opening refreshes the current board on success',()=>{
  const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:6,ownerMarker:'owner',updatedAtUtc:date}));
  const publishRead=r.calls.own[0];r.surface.receive(msg('open',2,1));r.complete([],null);
  publishRead.success({KVDataList:[]});r.calls.write[0].success({});
  assert.equal(r.calls.friend.length,2);assert.equal(r.surface.snapshot().status,'loading');
  r.complete([row('本人',6)],score(6));assert.equal(r.surface.snapshot().status,'self-only');assert.equal(r.surface.snapshot().self.firstWins,6);
});
test('publication spanning close and reopen reloads only the new visible epoch',()=>{
  const r=rig();r.surface.receive(msg('open'));r.surface.receive(msg('publishScore',2,1,{firstWins:6,ownerMarker:'owner',updatedAtUtc:date}));
  const publishRead=r.calls.own.at(-1);r.surface.receive(msg('close',3));r.surface.receive(msg('open',4,2));
  publishRead.success({KVDataList:[]});r.calls.write[0].success({});
  assert.equal(r.surface.snapshot().epoch,2);assert.equal(r.calls.friend.length,3);
  r.complete([row('本人',6)],score(6));assert.equal(r.surface.snapshot().self.rank,1);
});
test('failed score upload is bounded and retried by explicit board refresh',()=>{
  const r=rig();r.surface.receive(msg('open'));r.complete([],null);
  r.surface.receive(msg('publishScore',2,1,{firstWins:6,ownerMarker:'owner',updatedAtUtc:date}));
  r.calls.own.at(-1).success({KVDataList:[]});r.calls.write[0].fail({errMsg:'network'});
  assert.equal(r.calls.write.length,1);assert.equal(r.surface.snapshot().writing,false);assert.equal(r.timeouts.size,0);
  r.surface.receive(msg('refresh',3));const retryRead=r.calls.own.at(-2);
  retryRead.success({KVDataList:[]});assert.equal(r.calls.write.length,2);
  assert.equal(JSON.parse(r.calls.write[1].KVDataList[0].value).firstWins,6);
  r.calls.write[1].success({});r.complete([row('本人',6)],score(6));assert.equal(r.surface.snapshot().status,'self-only');
});
test('failed read retries at next open and retains remote higher score',()=>{
  const r=rig();r.surface.receive(msg('publishScore',1,0,{firstWins:6,ownerMarker:'owner',updatedAtUtc:date}));
  r.calls.own[0].fail({errMsg:'network'});assert.equal(r.calls.own.length,1);assert.equal(r.timeouts.size,0);
  r.surface.receive(msg('open',2));r.calls.own[1].success({KVDataList:[{key:board.SCORE_KEY,value:score(9)}]});
  assert.equal(r.calls.write.length,0);r.complete([row('本人',9)],score(9));assert.equal(r.surface.snapshot().self.firstWins,9);
});
test('self-contained production entry runs without CommonJS or SDK template',()=>{
  const r=rig({raw:true});vm.runInNewContext(fs.readFileSync(source,'utf8'),{wx:r.api,setTimeout:r.timers.setTimeout,clearTimeout:r.timers.clearTimeout});
  r.calls.message(msg('open'));assert.equal(r.calls.friend.length,1);
  const code=fs.readFileSync(source,'utf8');for(const pattern of [/Math\s*\.\s*random/,/getGroupCloudStorage/,/showGroupFriendsRank/,/user_rank/,/最强战力/,/shareMessageToFriend/,/console\s*\./,/\brequire\s*\(/,/\bimport\s/])assert.equal(pattern.test(code),false);
});
for(const dpr of [1,2,3]){
  test('DPR '+dpr+' nonzero offsets map retry touches to the same logical region',()=>{
    const v={x:23*dpr,y:61*dpr,width:360*dpr,height:640*dpr,dpr},r=rig();
    r.surface.receive(msg('open',1,1,v));r.calls.friend[0].fail({errMsg:'network'});r.calls.own[0].success({KVDataList:[]});r.calls.user[0].success({data:[]});
    const outside=[[-1,90],[360,90],[80,-1],[80,640]];
    const drawCount=r.calls.draw.length;
    outside.forEach(([x,y])=>{r.calls.start(touch(v,x,y));r.calls.move(touch(v,x,y));r.calls.end();});
    assert.equal(r.calls.friend.length,1);assert.equal(r.calls.draw.length,drawCount);
    r.calls.start(touch(v,80,90));assert.equal(r.calls.friend.length,2);assert.equal(r.surface.snapshot().status,'loading');
    assert.equal(r.canvas.width,v.width);assert.equal(r.canvas.height,v.height);
  });
  test('DPR '+dpr+' scrolling clamps at both ends and outside/closed touches do nothing',()=>{
    const v={x:23*dpr,y:61*dpr,width:360*dpr,height:640*dpr,dpr},r=rig();
    r.surface.receive(msg('open',1,1,v));r.complete(scrollRows());
    const top=finalRows(r);assert.equal(top[0].name,'friend-0');
    r.calls.start(touch(v,100,300));r.calls.move(touch(v,100,260));r.calls.end();
    assert.equal(finalRows(r).find(x=>x.name==='friend-0').y,top[0].y-40);
    r.calls.start(touch(v,100,260));r.calls.move(touch(v,100,500));r.calls.end();assert.deepEqual(finalRows(r),top);
    r.calls.start(touch(v,100,260));r.calls.move(touch(v,100,500));r.calls.end();assert.deepEqual(finalRows(r),top);
    for(let i=0;i<8;i++){r.calls.start(touch(v,100,500));r.calls.move(touch(v,100,60));r.calls.end();}
    const bottom=finalRows(r);assert.equal(bottom.at(-1).name,'friend-23');assert.equal(bottom.at(-1).y+30,536);
    r.calls.start(touch(v,100,500));r.calls.move(touch(v,100,60));r.calls.end();assert.deepEqual(finalRows(r),bottom);
    const count=r.calls.draw.length;
    for(const [x,y] of [[-1,200],[360,200],[100,-1],[100,640]]){
      r.calls.start(touch(v,x,y));r.calls.move(touch(v,100,200));r.calls.end();
    }
    assert.equal(r.calls.draw.length,count);assert.deepEqual(finalRows(r),bottom);
    r.calls.start(touch(v,100,300));r.calls.move(touch(v,-1,300));assert.equal(r.calls.draw.length,count);r.calls.end();
    r.surface.receive(msg('close',2));const closedDrawCount=r.calls.draw.length,reads=r.calls.friend.length;
    r.calls.start(touch(v,100,90));r.calls.move(touch(v,100,300));r.calls.end();
    assert.equal(r.calls.draw.length,closedDrawCount);assert.equal(r.calls.friend.length,reads);assert.equal(r.surface.snapshot().status,'closed');
  });
}
test('20 open refresh close cycles retain one listener per type and no timers or late writes to canvas',()=>{
  const r=rig();let id=0;const imageCallbacks=[];
  for(let epoch=1;epoch<=20;epoch++){
    assert.ok(r.surface.receive(msg('open',++id,epoch)));assert.equal(r.timeouts.size,3);
    const old=r.calls.friend.at(-1);r.surface.receive(msg('refresh',++id,epoch));assert.equal(r.timeouts.size,3);
    old.success({data:[row('late-'+epoch,999)]});assert.equal(r.surface.snapshot().status,'loading');
    r.complete(scrollRows());assert.equal(r.timeouts.size,0);assert.equal(r.surface.snapshot().status,'ready');
    r.calls.images.forEach(image=>{if(image.onload)imageCallbacks.push(image.onload);});
    r.surface.receive(msg('refresh',++id,epoch));assert.equal(r.timeouts.size,3);
    r.surface.receive(msg('close',++id,epoch));assert.equal(r.timeouts.size,0);assert.equal(r.surface.snapshot().pendingTimers,0);
    const drawCount=r.calls.draw.length,snapshot=r.surface.snapshot();
    r.calls.friend.forEach(call=>{call.success({data:[row('very-late',999)]});call.fail({errMsg:'network'});});
    r.calls.own.forEach(call=>call.success({KVDataList:[{key:board.SCORE_KEY,value:score(999)}]}));
    r.calls.user.forEach(call=>call.success({data:[{nickName:'very-late'}]}));
    r.timerHistory.forEach(callback=>callback());imageCallbacks.forEach(callback=>callback());
    r.calls.start(touch(view,100,100));r.calls.move(touch(view,100,200));r.calls.end();
    assert.deepEqual(r.surface.snapshot(),snapshot);assert.equal(r.calls.draw.length,drawCount);assert.equal(r.timeouts.size,0);
    assert.deepEqual(Object.fromEntries(Object.entries(r.listeners).map(([key,value])=>[key,value.length])),{message:1,start:1,move:1,end:1});
    assert.ok(r.calls.images.every(image=>image.onload===null&&image.onerror===null));
  }
  assert.equal(r.calls.friend.length,60);assert.equal(r.calls.write.length,0);
});
if(process.argv[3])test('actual C# serializer fixtures accepted by production parser',()=>{
  const fixtures=JSON.parse(fs.readFileSync(process.argv[3],'utf8'));assert.ok(fixtures.length>=7);
  const r=rig();fixtures.forEach(raw=>{assert.ok(board.parseMessage(raw));assert.ok(r.surface.receive(raw));});assert.equal(r.surface.snapshot().status,'closed');
});
const report={task:'TASK-008',version:1,scope:'open-data deterministic adapter QA; not device or visual acceptance',sourceSha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),passed:results.filter(x=>x.passed).length,total:results.length,results};
if(process.argv[2])fs.writeFileSync(process.argv[2],JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));process.exitCode=report.passed===report.total?0:1;
