'use strict';
const assert=require('assert/strict');
const p=require('./profile');
const context={SOURCE:'wx_client',APPID:'fixture-app',OPENID:'fixture-owner',ENV:'fixture-cloud'};
const environment='development',account=p.trustedIdentity(context,environment);
const epoch='2026-09-21T22:00:00.0000000+00:00';
const clone=value=>JSON.parse(JSON.stringify(value));
let request=0,groups=0;
function envelope(doc,action='sync'){return{protocolVersion:1,requestId:(++request).toString(16).padStart(32,'0'),action,environment,document:doc};}
function document(){return p.empty(environment,account);}
function award(id,time=epoch,day='20260922'){return{requestId:id,quotaDay:day,effectiveUtc:time,rewardKind:0,route:2,timeSource:0};}
function add(doc,fact){doc.rewards.push(fact);doc.pending.push({operationId:'reward:'+fact.requestId,kind:'reward',entityId:fact.requestId});return doc;}
function database(){
  const records=new Map(),versions=new Map();let writes=0,conflicts=0;
  return{records,get writes(){return writes;},get conflicts(){return conflicts;},transaction:async(key,merge)=>{
    for(let attempt=0;attempt<4;attempt++){
      const version=versions.get(key)||0,read=records.has(key)?clone(records.get(key)):null;
      await Promise.resolve();const candidate=merge(read);await Promise.resolve();
      if(version!==(versions.get(key)||0)){conflicts++;continue;}
      if(candidate.changed){records.set(key,clone(candidate.stored));versions.set(key,version+1);writes++;}
      return candidate.snapshot;
    }throw Error('transaction retry exhausted');
  }};
}
function rig(){const db=database();return{db,handle:p.createHandler({environment,transaction:db.transaction,now:()=>new Date('2026-09-22T01:00:00.000Z')})};}
// Exact quota projection is test-only. Runtime quota authority remains ProfileStore.
function available(d,day,now){const q=d.legacyQuotas.find(q=>q.day===day);const facts=d.rewards.filter(r=>r.quotaDay===day&&[1,2].includes(r.route)&&!d.legacyRequestIds.includes(r.requestId));const used=(q?.used||0)+facts.length;const times=[q?.lastEffectiveUtc,...facts.map(r=>r.effectiveUtc)].filter(Boolean);const last=times.sort().at(-1);return used<3&&(!last||Date.parse(now)>=Date.parse(last)+(used===1?5:15)*60000);}
async function test(name,body){await body();groups++;console.log(name+' PASS');}
(async()=>{
 await test('S01-trusted-context-handshake-and-forged-account',async()=>{const r=rig();const result=await r.handle(envelope(undefined,'identity'),context);assert.equal(result.accountId,account);assert.equal(r.db.writes,0);const d=document();d.account='forged-owner';assert.equal((await r.handle(envelope(d),context)).error,'WrongPartition');assert.equal(r.db.writes,0);for(const ctx of [{...context,OPENID:''},{...context,SOURCE:'wx_http'},{...context,ENV:''}])assert.notEqual((await r.handle(envelope(document()),ctx)).status,'Synced');});
 await test('S02-union-idempotency-time-and-minimal-storage',async()=>{const r=rig(),d=add(document(),award('one'));d.firstWinDays=['20260922'];const first=await r.handle(envelope(d),context),second=await r.handle(envelope(d),context);assert.equal(first.status,'Synced');assert.equal(first.confirmationCursor,second.confirmationCursor);assert.equal(r.db.writes,1);assert.equal(second.snapshot.rewards[0].effectiveUtc,epoch);assert.deepEqual(second.acknowledgedOperations,['reward:one']);assert.equal(r.db.records.get(account).pending,undefined);assert.equal(r.db.records.get(account).settings,undefined);assert.match(second.serverUtc,/\.\d{7}\+00:00$/);});
 await test('S03-three-device-concurrent-first-write',async()=>{const r=rig();const inputs=[0,1,2].map(i=>{const d=add(document(),award('device-'+i));d.firstWinDays=['2026092'+i];return d;});const responses=await Promise.all(inputs.map(d=>r.handle(envelope(d),context)));assert.ok(responses.every(r=>r.status==='Synced'));const latest=await r.handle(envelope(document()),context);assert.equal(latest.snapshot.rewards.length,3);assert.equal(latest.snapshot.firstWinDays.length,3);assert.ok(r.db.conflicts>=2);assert.equal(available(latest.snapshot,'20260922','2030-01-01T00:00:00Z'),false);});
 await test('S04-five-fifteen-minute-boundaries',async()=>{const r=rig();let response=await r.handle(envelope(add(document(),award('one'))),context);assert.equal(available(response.snapshot,'20260922','2026-09-21T22:04:59.999Z'),false);assert.equal(available(response.snapshot,'20260922','2026-09-21T22:05:00.000Z'),true);response=await r.handle(envelope(add(document(),award('two','2026-09-21T22:05:00.0000000+00:00'))),context);assert.equal(available(response.snapshot,'20260922','2026-09-21T22:19:59.999Z'),false);assert.equal(available(response.snapshot,'20260922','2026-09-21T22:20:00.000Z'),true);});
 await test('S05-cross-six-am-delayed-active-day-preserved',async()=>{const r=rig();const d=add(document(),award('late','2026-09-21T22:01:00.0000000+00:00','20260921'));add(d,award('new','2026-09-21T22:02:00.0000000+00:00','20260922'));const result=await r.handle(envelope(d),context);assert.equal(result.status,'Synced');assert.equal(result.snapshot.rewards.find(r=>r.requestId==='late').quotaDay,'20260921');assert.equal(result.snapshot.rewards.find(r=>r.requestId==='late').effectiveUtc,d.rewards[0].effectiveUtc);});
 await test('S06-migrated-baseline-does-not-invent-timestamps',async()=>{const r=rig(),d=document();d.legacyRequestIds=['old'];d.legacyQuotas=[{day:'20260922',used:2,lastEffectiveUtc:'',committedRequestId:'old',dataVersion:0}];d.pending=[{operationId:'migration:baseline',kind:'migration',entityId:'baseline'}];add(d,award('old'));const result=await r.handle(envelope(d),context);assert.equal(result.status,'Synced');assert.equal(result.snapshot.legacyQuotas[0].lastEffectiveUtc,'');assert.equal(result.snapshot.legacyQuotas[0].used,2);assert.equal(available(result.snapshot,'20260922',epoch),true);const old=document();old.schemaVersion=0;assert.equal((await r.handle(envelope(old),context)).error,'UnsupportedSchema');});
 await test('S07-malformed-oversized-enums-utc-outbox',async()=>{const r=rig();for(const mutate of [d=>d.settings={},d=>d.rewards=[award('bad','2026-02-30T00:00:00.0000000+00:00')],d=>d.rewards=[{...award('bad'),route:99}],d=>d.rewards=[{...award('bad'),rewardKind:6}],d=>d.rewards=[{...award('bad'),timeSource:2}],d=>d.rewards=[award('bad','2026-09-22T00:00:00.0000000+08:00')],d=>d.firstWinDays=['20260230'],d=>d.firstWinDays=['20260922','20260922'],d=>d.pending=[{operationId:'win:20260922',kind:'win',entityId:'20260922'}],d=>d.firstWinDays=Array(4097).fill('20260922'),d=>d.account='a'.repeat(p.MAX_BYTES+1)]){const d=document();mutate(d);assert.equal((await r.handle(envelope(d),context)).status,'Rejected');}assert.equal(r.db.writes,0);});
 await test('S14-third-pot-video-roundtrip-idempotency',async()=>{
   const r=rig(),fact={...award('third-pot-video'),rewardKind:5,route:3},d=add(document(),fact);
   const first=await r.handle(envelope(d),context),repeat=await r.handle(envelope(d),context),read=await r.handle(envelope(document()),context);
   assert.equal(first.status,'Synced');assert.equal(repeat.status,'Synced');assert.equal(read.status,'Synced');
   assert.deepEqual(read.snapshot.rewards,[fact]);assert.equal(first.confirmationCursor,repeat.confirmationCursor);assert.equal(r.db.writes,1);
   assert.deepEqual(repeat.acknowledgedOperations,['reward:third-pot-video']);assert.equal(available(read.snapshot,'20260922',epoch),true);
 });
 await test('S08-immutable-collision-no-partial-write',async()=>{const r=rig();await r.handle(envelope(add(document(),award('one'))),context);const before=clone(r.db.records.get(account));const d=add(document(),award('one','2026-09-22T01:00:00.0000000+00:00'));d.firstWinDays=['20260923'];assert.equal((await r.handle(envelope(d),context)).error,'ImmutableConflict');assert.deepEqual(r.db.records.get(account),before);});
 await test('S09-environment-owner-isolation-and-spoofed-userinfo',async()=>{const r=rig();const evt=envelope(document());evt.userInfo={openId:'forged-owner'};assert.equal((await r.handle(evt,context)).accountId,account);assert.notEqual(p.trustedIdentity({...context,OPENID:'another'},environment),account);assert.notEqual(p.trustedIdentity({...context,ENV:'another-cloud'},environment),account);assert.notEqual(p.trustedIdentity(context,'production'),account);evt.environment='production';assert.equal((await r.handle(evt,context)).error,'WrongPartition');});
 await test('S10-database-failure-and-corrupt-record-fail-closed',async()=>{const fail=p.createHandler({environment,transaction:async()=>{throw Error('private details');}});const response=await fail(envelope(document()),context);assert.equal(response.error,'ServerFailure');assert.ok(!JSON.stringify(response).includes('private'));const r=rig();r.db.records.set(account,{schemaVersion:0});assert.equal((await r.handle(envelope(document()),context)).status,'Rejected');assert.equal(r.db.writes,0);});
 await test('S11-submillisecond-original-utc-and-legacy-max',async()=>{const a=document(),b=document();a.legacyQuotas=[{day:'20260922',used:1,lastEffectiveUtc:'2026-09-21T22:00:00.0000001+00:00',committedRequestId:'a',dataVersion:2}];b.legacyQuotas=[{...a.legacyQuotas[0],used:2,lastEffectiveUtc:'2026-09-21T22:00:00.0000002+00:00'}];const merged=p.merge(a,b);assert.equal(merged.legacyQuotas[0].lastEffectiveUtc,b.legacyQuotas[0].lastEffectiveUtc);assert.equal(merged.legacyQuotas[0].used,2);assert.equal(merged.legacyQuotas[0].dataVersion,2);});
 await test('S12-production-entry-transaction-shape-and-missing-document',async()=>{
   const vm=require('vm'),fs=require('fs');let stored=null,gets=0,sets=0;const exports={};
   const cloud={DYNAMIC_CURRENT_ENV:Symbol(),init:()=>{},getWXContext:()=>context,database:options=>{assert.equal(options.throwOnNotFound,false);return{runTransaction:async(callback,retries)=>{assert.equal(retries,3);return callback({collection:name=>{assert.equal(name,'hotpot_profiles_v1');return{doc:key=>{assert.equal(key,account);return{get:async()=>{gets++;return{data:stored};},set:async options=>{assert.ok(options.data);sets++;stored=clone(options.data);}};}};}});}};}};
   vm.runInNewContext(fs.readFileSync(require.resolve('./index'),'utf8'),{exports,require:name=>name==='wx-server-sdk'?cloud:p,process:{env:{HOTPOT_PROFILE_ENVIRONMENT:environment}}});
   assert.equal((await exports.main(envelope(document()),{})).status,'Synced');assert.equal(sets,1);assert.equal(gets,1);assert.equal((await exports.main(envelope(document()),{})).status,'Synced');assert.equal(sets,1);
 });
 await test('S13-native-envelope-metadata-is-not-identity',async()=>{
   const r=rig(),evt=envelope(undefined,'identity');
   evt.tcbContext={OPENID:'forged-owner',APPID:'forged-app',SOURCE:'wx_client'};
   evt.userInfo={openId:'another-forgery'};
   const identity=await r.handle(evt,context);
   assert.equal(identity.status,'Synced');assert.equal(identity.accountId,account);assert.equal(r.db.writes,0);
   assert.equal((await r.handle(evt,{...context,SOURCE:'wx_http'})).status,'Rejected');
   const sync=envelope(document());sync.tcbContext=evt.tcbContext;
   assert.equal((await r.handle(sync,context)).status,'Synced');
   evt.unrecognized=true;assert.equal((await r.handle(evt,context)).status,'Rejected');
 });
 await test('S15-tutorial-flags-old-docs-cross-device-and-idempotent-ack',async()=>{
   const r=rig(),first=document();first.warmupTutorialCompleted=true;first.pending=[{operationId:'tutorial:warmup',kind:'tutorial',entityId:'warmup'}];
   const one=await r.handle(envelope(first),context);assert.equal(one.status,'Synced');assert.equal(one.snapshot.warmupTutorialCompleted,true);assert.equal(one.snapshot.bufferWarningCompleted,false);assert.deepEqual(one.acknowledgedOperations,['tutorial:warmup']);
   const second=document();second.bufferWarningCompleted=true;second.pending=[{operationId:'tutorial:buffer',kind:'tutorial',entityId:'buffer'}];
   const two=await r.handle(envelope(second),context);assert.equal(two.snapshot.warmupTutorialCompleted,true);assert.equal(two.snapshot.bufferWarningCompleted,true);
   const old=await r.handle(envelope(document()),context);assert.equal(old.snapshot.warmupTutorialCompleted,true);assert.equal(old.snapshot.bufferWarningCompleted,true);assert.equal(old.confirmationCursor,two.confirmationCursor);
   const bad=document();bad.warmupTutorialCompleted='true';assert.equal((await r.handle(envelope(bad),context)).status,'Rejected');
   const missing=document();missing.pending=first.pending;assert.equal((await r.handle(envelope(missing),context)).status,'Rejected');
 });
 console.log('PROFILE_CLOUD_MERGE_PASS groups='+groups+' externalRequests=0');
})().catch(error=>{console.error(error);process.exitCode=1;});
