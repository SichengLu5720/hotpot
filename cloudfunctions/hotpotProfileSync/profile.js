'use strict';
const crypto=require('crypto');
const MAX_BYTES=4*1024*1024;
const LIMITS={firstWinDays:4096,rewards:16384,legacyQuotas:4096,legacyRequestIds:16384,pending:16384};
class Rejected extends Error {constructor(code){super(code);this.code=code;}}
function fail(code='InvalidPayload'){throw new Rejected(code);}
function plain(value){if(!value||typeof value!=='object'||Array.isArray(value)||![Object.prototype,null].includes(Object.getPrototypeOf(value)))fail();}
function keys(value,allowed){plain(value);if(Object.keys(value).some(k=>!allowed.includes(k)))fail();}
function text(value,max=160){if(typeof value!=='string'||!value.length||value.length>max||/[\x00-\x1f\x7f]/.test(value))fail();return value;}
function optional(value,max=160){if(value===undefined||value===null||value==='')return '';return text(value,max);}
function integer(value,min,max=Number.MAX_SAFE_INTEGER){if(!Number.isSafeInteger(value)||value<min||value>max)fail();return value;}
function bounded(value){let serialized;try{serialized=JSON.stringify(value);}catch{fail();}if(!serialized||Buffer.byteLength(serialized,'utf8')>MAX_BYTES)fail('PayloadTooLarge');}
function day(value){if(typeof value!=='string'||!/^\d{8}$/.test(value))fail();const iso=value.slice(0,4)+'-'+value.slice(4,6)+'-'+value.slice(6,8);const d=new Date(iso+'T00:00:00.000Z');if(!Number.isFinite(d.valueOf())||d.toISOString().slice(0,10)!==iso)fail();return value;}
function utc(value){if(typeof value!=='string'||!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}(\+00:00|Z)$/.test(value))fail();const date=new Date(value);if(!Number.isFinite(date.valueOf())||date.toISOString().slice(0,19)!==value.slice(0,19))fail();return value;}
function utcNow(now){const date=new Date(now);if(!Number.isFinite(date.valueOf()))fail('ServerFailure');return date.toISOString().replace(/\.(\d{3})Z$/,(_,ms)=>'.'+ms+'0000+00:00');}
function array(value,name,key,validate){if(!Array.isArray(value)||value.length>LIMITS[name])fail('PayloadTooLarge');const seen=new Set();return value.map(item=>{const result=validate(item),id=key(result);if(seen.has(id))fail();seen.add(id);return result;});}
function reward(r){keys(r,['requestId','quotaDay','effectiveUtc','rewardKind','route','timeSource']);return{requestId:text(r.requestId),quotaDay:day(r.quotaDay),effectiveUtc:utc(r.effectiveUtc),rewardKind:integer(r.rewardKind,-1,5),route:integer(r.route,0,3),timeSource:integer(r.timeSource,0,1)};}
function quota(q){keys(q,['day','used','lastEffectiveUtc','committedRequestId','dataVersion']);return{day:day(q.day),used:integer(q.used,0,1000000),lastEffectiveUtc:optional(q.lastEffectiveUtc)?utc(q.lastEffectiveUtc):'',committedRequestId:optional(q.committedRequestId),dataVersion:integer(q.dataVersion,0,2)};}
function validateDocument(d,environment,account){
  bounded(d);keys(d,['schemaVersion','localRevision','environment','account','confirmationCursor','firstWinDays','legacyRequestIds','rewards','legacyQuotas','pending','warmupTutorialCompleted','bufferWarningCompleted']);
  for(const key of ['warmupTutorialCompleted','bufferWarningCompleted'])if(d[key]!==undefined&&typeof d[key]!=='boolean')fail();
  if(d.schemaVersion!==1)fail('UnsupportedSchema');
  if(d.environment!==environment)fail('WrongPartition');
  // Caller-supplied account never selects a database key or identity.
  text(d.account,160);if(d.account!==account)fail('WrongPartition');integer(d.localRevision,0);optional(d.confirmationCursor,160);
  const value={schemaVersion:1,localRevision:d.localRevision,environment,account,confirmationCursor:optional(d.confirmationCursor),
    warmupTutorialCompleted:d.warmupTutorialCompleted===true,bufferWarningCompleted:d.bufferWarningCompleted===true,
    firstWinDays:array(d.firstWinDays,'firstWinDays',x=>x,day),legacyRequestIds:array(d.legacyRequestIds,'legacyRequestIds',x=>x,x=>text(x)),
    rewards:array(d.rewards,'rewards',x=>x.requestId,reward),legacyQuotas:array(d.legacyQuotas,'legacyQuotas',x=>x.day,quota),pending:[]};
  value.pending=array(d.pending,'pending',x=>x.operationId,p=>{
    keys(p,['operationId','kind','entityId']);text(p.entityId);text(p.operationId,180);
    if(!['win','reward','migration','tutorial'].includes(p.kind)||p.operationId!==p.kind+':'+p.entityId)fail();
    if(p.kind==='tutorial'&&!(p.entityId==='warmup'&&value.warmupTutorialCompleted||p.entityId==='buffer'&&value.bufferWarningCompleted))fail();
    if(p.kind==='win'&&!value.firstWinDays.includes(p.entityId)||p.kind==='reward'&&!value.rewards.some(r=>r.requestId===p.entityId)||p.kind==='migration'&&p.entityId!=='baseline')fail();
    return{operationId:p.operationId,kind:p.kind,entityId:p.entityId};
  });return value;
}
function trustedIdentity(context,environment){
  if(!['development','staging','production'].includes(environment))fail('NotConfigured');
  if(!context||!['wx_client','wx_devtools'].includes(context.SOURCE))fail('Unauthenticated');
  const app=text(context.APPID,128),openid=text(context.OPENID,128),env=text(context.ENV,128);
  return 'wx_'+crypto.createHash('sha256').update(JSON.stringify([env,environment,app,openid])).digest('hex');
}
function empty(environment,account){return{schemaVersion:1,localRevision:0,environment,account,confirmationCursor:'',firstWinDays:[],legacyRequestIds:[],rewards:[],legacyQuotas:[],pending:[]};}
function merge(remote,incoming){
  const result=validateDocument(remote,incoming.environment,incoming.account);validateDocument(incoming,incoming.environment,incoming.account);
  result.warmupTutorialCompleted=result.warmupTutorialCompleted||incoming.warmupTutorialCompleted===true;
  result.bufferWarningCompleted=result.bufferWarningCompleted||incoming.bufferWarningCompleted===true;
  result.firstWinDays=[...new Set([...result.firstWinDays,...incoming.firstWinDays])].sort();
  result.legacyRequestIds=[...new Set([...result.legacyRequestIds,...incoming.legacyRequestIds])].sort();
  const rewards=new Map(result.rewards.map(r=>[r.requestId,r]));
  for(const r of incoming.rewards){if(rewards.has(r.requestId)&&JSON.stringify(rewards.get(r.requestId))!==JSON.stringify(r))fail('ImmutableConflict');if(!rewards.has(r.requestId))rewards.set(r.requestId,r);}
  result.rewards=[...rewards.values()].sort((a,b)=>a.requestId<b.requestId?-1:a.requestId>b.requestId?1:0);
  const quotas=new Map(result.legacyQuotas.map(q=>[q.day,q]));
  for(const q of incoming.legacyQuotas){const old=quotas.get(q.day);if(!old){quotas.set(q.day,{...q});continue;}old.used=Math.max(old.used,q.used);if(q.lastEffectiveUtc&&(!old.lastEffectiveUtc||q.lastEffectiveUtc.slice(0,27)>old.lastEffectiveUtc.slice(0,27)))old.lastEffectiveUtc=q.lastEffectiveUtc;}
  result.legacyQuotas=[...quotas.values()].sort((a,b)=>a.day.localeCompare(b.day));result.pending=[];result.localRevision=0;result.confirmationCursor='';
  validateDocument(result,incoming.environment,incoming.account);return result;
}
function facts(document){const {schemaVersion,environment,account,firstWinDays,legacyRequestIds,rewards,legacyQuotas}=document;return{schemaVersion,environment,account,firstWinDays,legacyRequestIds,rewards,legacyQuotas,warmupTutorialCompleted:document.warmupTutorialCompleted===true,bufferWarningCompleted:document.bufferWarningCompleted===true};}
function cursor(document){return crypto.createHash('sha256').update(JSON.stringify(facts(document))).digest('hex');}
function fromStored(record,environment,account){
  if(record===null)return empty(environment,account);
  keys(record,['_id','schemaVersion','environment','account','firstWinDays','legacyRequestIds','rewards','legacyQuotas','updatedUtc','warmupTutorialCompleted','bufferWarningCompleted']);utc(record.updatedUtc);
  return validateDocument({...facts(record),localRevision:0,confirmationCursor:'',pending:[]},environment,account);
}
// transaction(key, mergeCallback) must atomically read, merge and replace ONE document.
function createHandler({environment,transaction,now=()=>new Date()}){
  return async function handle(event,context){
    let requestId='';try{
      // Native CloudBase injects tcbContext/userInfo into the envelope. They are
      // transport metadata only; identity still comes solely from getWXContext.
      bounded(event);keys(event,['protocolVersion','requestId','action','environment','document','userInfo','tcbContext']);
      if(event.protocolVersion!==1)fail('UnsupportedSchema');requestId=text(event.requestId,64);
      if(!/^[a-f0-9]{32}$/.test(requestId))fail();if(event.environment!==environment)fail('WrongPartition');
      const account=trustedIdentity(context,environment);
      if(event.action==='identity'){
        if(event.document!==undefined&&event.document!==null)fail();
        return{protocolVersion:1,requestId,status:'Synced',accountId:account,serverUtc:utcNow(now())};
      }
      if(event.action!=='sync')fail();const incoming=validateDocument(event.document,environment,account);
      const snapshot=await transaction(account,record=>{
        const original=fromStored(record,environment,account),merged=merge(original,incoming);
        const changed=!record||cursor(original)!==cursor(merged);
        const stored={...facts(merged),updatedUtc:changed?utcNow(now()):record.updatedUtc};
        bounded(stored);
        bounded({protocolVersion:1,requestId,status:'Synced',accountId:account,snapshot:{...merged,confirmationCursor:cursor(merged)},confirmationCursor:cursor(merged),acknowledgedOperations:incoming.pending.map(p=>p.operationId),serverUtc:utcNow(now())});
        return{stored,changed,snapshot:merged};
      });
      const confirmationCursor=cursor(snapshot);snapshot.confirmationCursor=confirmationCursor;
      const response={protocolVersion:1,requestId,status:'Synced',accountId:account,snapshot,confirmationCursor,acknowledgedOperations:incoming.pending.map(p=>p.operationId),serverUtc:utcNow(now())};bounded(response);return response;
    }catch(error){return{protocolVersion:1,requestId,status:error instanceof Rejected?'Rejected':'Failed',error:error instanceof Rejected?error.code:'ServerFailure'};}
  };
}
module.exports={MAX_BYTES,LIMITS,Rejected,validateDocument,trustedIdentity,empty,merge,facts,cursor,utc,utcNow,createHandler};
