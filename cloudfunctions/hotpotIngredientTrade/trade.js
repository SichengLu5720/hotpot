'use strict';
const crypto=require('crypto');
const broth=require('./broth');
const tools=require('./tools');
const ids=Array.from({length:32},(_,i)=>'food_'+String(i).padStart(2,'0'));
const clone=x=>JSON.parse(JSON.stringify(x));
function fail(code){throw Object.assign(new Error(code),{code});}
function text(x){if(typeof x!=='string'||!x.length||x.length>160||/[\x00-\x1f]/.test(x))fail('InvalidPayload');return x;}
function account(context,environment){
  if(!['development','staging','production'].includes(environment))fail('NotConfigured');
  if(!context||!['wx_client','wx_devtools'].includes(context.SOURCE))fail('Unauthenticated');
  return 'wx_'+crypto.createHash('sha256').update(JSON.stringify([text(context.ENV),environment,text(context.APPID),text(context.OPENID)])).digest('hex');
}
function empty(environment,account){return {schemaVersion:1,environment,account,revision:0,serverRevision:1,entryUnlocked:false,unlocked:ids.slice(0,16),selected:ids.slice(0,16),duplicates:Array(32).fill(0),tools:[0,0,0],pendingWins:[],rewards:[],appliedOperations:[]};}
function rewardDay(ms){return new Date(ms+7200000).toISOString().slice(0,10).replace(/-/g,'');}
function utc(ms){return new Date(ms).toISOString().replace(/\.(\d{3})Z$/,'.$10000+00:00');}
function gain(p,id){if(p.unlocked.includes(id))p.duplicates[ids.indexOf(id)]++;else p.unlocked.push(id);}
// transaction runs the entire callback atomically; friend verification is a trusted
// server dependency, NEVER an event flag, share callback or caller-provided list.
function createHandler({environment,transaction,verifyFriend=null,now=Date.now,random=n=>crypto.randomInt(n)}){
 return async(event,context)=>{
  const response={protocolVersion:1,requestId:event&&event.requestId,status:'Rejected',error:'',friendVerificationAvailable:!!verifyFriend};
  try{
   const actor=account(context,environment);response.accountId=actor;
   if(!event||event.protocolVersion!==1||event.environment!==environment||Buffer.byteLength(JSON.stringify(event))>262144)fail('InvalidPayload');
   text(event.requestId);const action=text(event.action),time=now();
   if(event.account&&event.account!==actor)fail('WrongPartition');
   const result=await transaction(async tx=>{
    const key='profile:'+actor;let p=await tx.get(key);if(!p)p=empty(environment,actor);broth.normalize(p);
    const save=async(k,v)=>{v.serverRevision++;v.revision=v.serverRevision;await tx.set(k,v);};
    await broth.expire({p,key,save,time});
    if(broth.actions.includes(action))return broth.execute({tx,p,key,save,actor,event,time,environment,empty});
    if(tools.actions.includes(action))return tools.execute({tx,p,key,save,actor,event,time});
    if(action==='read')return {snapshot:p};
    if(action==='list'){
     const index=await tx.get('index:'+actor)||{ids:[]},trades=[];
     for(const id of index.ids){const t=await tx.get('trade:'+id);if(t)trades.push({...t,status:t.status==='Pending'&&time>=Date.parse(t.expiresUtc)?'Expired':t.status});}
     return {snapshot:p,trades};
    }
    const op=text(event.operationId), opKey='operation:'+actor+':'+op;
    const fingerprint=crypto.createHash('sha256').update(JSON.stringify({...event,requestId:null})).digest('hex');
    const receipt=await tx.get(opKey);
    if(receipt){if(receipt.fingerprint!==fingerprint)fail('OperationConflict');let previous=receipt.trade;if(previous){previous=await tx.get('trade:'+previous.requestId)||previous;if(previous.status==='Pending'&&time>=Date.parse(previous.expiresUtc))previous={...previous,status:'Expired'};}return {snapshot:p,trade:previous};}
    let trade;
    if(action==='sync'){
     if(!Array.isArray(event.pendingWins)||event.pendingWins.length>366)fail('InvalidPayload');
     for(const win of event.pendingWins){
      text(win.sessionId);const ms=Date.parse(win.utc);
      if(!Number.isFinite(ms)||ms>time||win.day!==rewardDay(ms))fail('InvalidWin');
      p.entryUnlocked=true;
      if(p.rewards.some(r=>r.day===win.day||r.sessionId===win.sessionId))continue;
      const r={day:win.day,sessionId:win.sessionId,ingredientId:'',firstUnlock:false,tools:[]};
      if(p.unlocked.length===32){for(let i=0;i<2;i++){const tool=random(3);p.tools[tool]++;r.tools.push(tool);}}
      else {r.ingredientId=ids[random(32)];r.firstUnlock=!p.unlocked.includes(r.ingredientId);gain(p,r.ingredientId);}
      p.rewards.push(r);
     }
    }else if(action==='selection'){
     if(event.expectedRevision!==p.serverRevision)fail('StaleRevision');
     const selected=event.selected;
     if(!Array.isArray(selected)||selected.length<16||selected.length>32||new Set(selected).size!==selected.length||selected.some(id=>!p.unlocked.includes(id)))fail('InvalidSelection');
     p.selected=selected.slice();
    }else if(action==='consume'){
     const tool=event.tool;if(!Number.isInteger(tool)||tool<0||tool>2||tools.available(p,tool,time)<1)fail('InsufficientInventory');p.tools[tool]--;p.appliedOperations.push(op);
    }else if(action==='create'||action==='createLink'){
     const target=action==='createLink'?'':text(event.targetAccount);
     if(action==='create'){
      if(!/^wx_[a-f0-9]{64}$/.test(target)||target===actor)fail('InvalidTarget');
      if(!verifyFriend)fail('FriendVerificationUnavailable');
      if(await verifyFriend(actor,target,context)!==true)fail('NotFriends');
     }
     if(!ids.includes(event.offeredId)||!ids.includes(event.receivedId)||event.offeredId===event.receivedId)fail('InvalidTrade');
     if(p.duplicates[ids.indexOf(event.offeredId)]<1)fail('InsufficientInventory');
     const requestId=action==='createLink'?crypto.randomBytes(32).toString('hex'):text(event.tradeId),tradeKey='trade:'+requestId;if(await tx.get(tradeKey))fail('TradeIdConflict');
     trade={requestId,linkExchange:action==='createLink',initiator:actor,recipient:target,offeredId:event.offeredId,receivedId:event.receivedId,status:'Pending',createdUtc:utc(time),expiresUtc:utc(time+86400000)};await tx.set(tradeKey,trade);
     for(const owner of [actor,target].filter(Boolean)){const index=await tx.get('index:'+owner)||{ids:[]};if(index.ids.length>=1024)fail('RequestLimit');index.ids.push(requestId);await tx.set('index:'+owner,index);}
    }else if(['accept','reject','withdraw','getTrade'].includes(action)){
     const tradeKey='trade:'+text(event.tradeId);trade=await tx.get(tradeKey);if(!trade)fail('TradeNotFound');
     const bearer=trade.linkExchange&&/^[a-f0-9]{64}$/.test(event.tradeId)&&!trade.recipient&&actor!==trade.initiator;
     if(![trade.initiator,trade.recipient].includes(actor)&&!bearer)fail('NotParticipant');
     if(action==='getTrade')return {snapshot:p,trade:{...trade,status:trade.status==='Pending'&&time>=Date.parse(trade.expiresUtc)?'Expired':trade.status}};
     if(action==='withdraw'?actor!==trade.initiator:actor!==trade.recipient&&!bearer)fail('NotAuthorized');
     if(trade.status!=='Pending')fail('TradeClosed');if(time>=Date.parse(trade.expiresUtc))fail('TradeExpired');
     if(action==='accept'){
      if(!trade.linkExchange){if(!verifyFriend)fail('FriendVerificationUnavailable');if(await verifyFriend(trade.initiator,actor,context)!==true)fail('NotFriends');}
      const otherKey='profile:'+trade.initiator,other=await tx.get(otherKey);
      const a=ids.indexOf(trade.offeredId),b=ids.indexOf(trade.receivedId);
      if(!other||other.duplicates[a]<1||p.duplicates[b]<1)fail('InsufficientInventory');
      other.duplicates[a]--;p.duplicates[b]--;gain(other,trade.receivedId);gain(p,trade.offeredId);await save(otherKey,other);trade.status='Accepted';
     }else trade.status=action==='withdraw'?'Withdrawn':'Rejected';
     if(bearer){trade.recipient=actor;const index=await tx.get('index:'+actor)||{ids:[]};if(index.ids.length>=1024)fail('RequestLimit');index.ids.push(trade.requestId);await tx.set('index:'+actor,index);}
     await tx.set(tradeKey,trade);
    }else fail('UnknownAction');
    await save(key,p);await tx.set(opKey,{fingerprint,trade:trade||null});return {snapshot:p,trade};
   });return {...response,...result,status:'Synced'};
  }catch(error){return {...response,error:error.code||'ServerFailure'};}
 };
}
module.exports={createHandler,account,empty,rewardDay};
