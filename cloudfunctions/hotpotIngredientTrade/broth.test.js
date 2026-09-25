'use strict';
const assert=require('node:assert/strict');
const {createHandler,account,empty}=require('./trade');
const {day}=require('./broth');
const environment='development',context=id=>({SOURCE:'wx_client',APPID:'app',OPENID:id,ENV:'env'});
const owner=id=>account(context(id),environment),copy=x=>JSON.parse(JSON.stringify(x));
let storage={},tail=Promise.resolve(),clock=Date.parse('2026-09-25T15:59:59Z'),counter=0,failAt=0;
const transaction=fn=>{const task=tail.then(async()=>{
 const next=copy(storage);let writes=0;
 const result=await fn({get:async k=>next[k]?copy(next[k]):null,set:async(k,v)=>{next[k]=copy(v);if(++writes===failAt)throw new Error('injected');}});
 storage=next;return result;
});tail=task.catch(()=>{});return task;};
const handle=createHandler({environment,transaction,now:()=>clock});
const call=(who,action,fields={})=>handle({protocolVersion:1,environment,requestId:'r'+(++counter),operationId:'op'+counter,action,...fields},context(who));
const profile=who=>storage['profile:'+owner(who)];
function reset(){storage={};failAt=0;clock=Date.parse('2026-09-25T15:59:59Z');}
async function invite(who){const p=empty(environment,owner(who));p.entryUnlocked=true;storage['profile:'+owner(who)]=p;const r=await call(who,'brothCreateInvitation');assert.equal(r.status,'Synced');assert.match(r.invitation.invitationId,/^[a-f0-9]{64}$/);assert(!JSON.stringify(r.invitation).includes(owner(who)));return r.invitation.invitationId;}
(async()=>{
 reset();let r=await call('old','read');assert.deepEqual(r.snapshot.ownedBroths,['red']);assert.equal(r.snapshot.currentBroth,'red');
 assert.equal((await call('old','brothCreateInvitation')).error,'EntryLocked');
 let id=await invite('a');assert.equal((await call('a','brothConfirmAssist',{invitationId:id})).error,'SelfAssist');
 assert.equal((await call('b','brothInspectInvitation',{invitationId:id})).invitation.canAssist,true);
 assert.equal(profile('b'),undefined); // inspecting never awards or creates inventory
 const fields={invitationId:id,operationId:'assist'};
 r=await call('b','brothConfirmAssist',fields);assert.equal(r.status,'Synced');assert.deepEqual(profile('b').tools,[1,1,1]);assert.equal(profile('b').entryUnlocked,false);assert(profile('a').brothActivityQualified);
 assert.equal((await call('b','brothConfirmAssist',fields)).status,'Synced');assert.deepEqual(profile('b').tools,[1,1,1]);
 assert.equal((await call('b','brothConfirmAssist',{...fields,invitationId:'0'.repeat(64)})).error,'OperationConflict');
 assert.equal((await call('b','brothConfirmAssist',{invitationId:id})).error,'AlreadyAssisted');
 assert.equal((await call('c','brothConfirmAssist',{invitationId:id})).error,'ActivityComplete');assert.equal(profile('c'),undefined);
 assert.equal((await call('b','brothInspectInvitation',{invitationId:id})).invitation.canAssist,false);
 const rev=profile('a').serverRevision;
 const claims=await Promise.all(['clear','tomato'].map(brothId=>call('a','brothClaim',{brothId,expectedRevision:rev})));
 assert.equal(claims.filter(x=>x.status==='Synced').length,1);assert.equal(claims.filter(x=>x.error==='AlreadyChosen').length,1);
 assert.equal(profile('a').brothActivityChoice,'clear');assert.equal(profile('a').currentBroth,'clear');
 profile('a').ownedBroths.push('tomato');r=await call('a','brothSelect',{brothId:'tomato',expectedRevision:profile('a').serverRevision,operationId:'select'});assert.equal(r.status,'Synced');assert.equal(profile('a').brothActivityChoice,'clear');
 assert.equal((await call('a','brothSelect',{brothId:'tomato',expectedRevision:r.snapshot.serverRevision-1,operationId:'select'})).status,'Synced');
 assert.equal((await call('a','brothSelect',{brothId:'red',expectedRevision:r.snapshot.serverRevision,operationId:'select'})).error,'OperationConflict');
 assert.equal((await call('a','brothSelect',{brothId:'mushroom',expectedRevision:profile('a').serverRevision})).error,'NotOwned');
 assert.equal((await call('a','brothSelect',{brothId:'red',expectedRevision:0})).error,'StaleRevision');
 assert.equal((await call('a','brothClaim',{brothId:'tomato',expectedRevision:profile('a').serverRevision})).error,'AlreadyChosen');
 reset();id=await invite('a');const race=await Promise.all(['b','c'].map(who=>call(who,'brothConfirmAssist',{invitationId:id})));
 assert.equal(race.filter(x=>x.status==='Synced').length,1);assert.equal(race.filter(x=>x.error==='ActivityComplete').length,1);
 assert.equal(['b','c'].reduce((n,who)=>n+(profile(who)?profile(who).tools[0]:0),0),1);
 reset();const tokens=[];for(let i=0;i<4;i++)tokens.push(await invite('host'+i));
 for(let i=0;i<3;i++)assert.equal((await call('helper','brothConfirmAssist',{invitationId:tokens[i]})).status,'Synced');
 assert.equal((await call('helper','brothConfirmAssist',{invitationId:tokens[3]})).error,'DailyLimit');assert.deepEqual(profile('helper').tools,[3,3,3]);
 assert.equal(day(clock),'2026-09-25');clock+=1000;assert.equal(day(clock),'2026-09-26');
 assert.equal((await call('helper','brothConfirmAssist',{invitationId:tokens[3]})).status,'Synced');assert.deepEqual(profile('helper').tools,[4,4,4]);
 assert.equal((await call('helper','brothConfirmAssist',{invitationId:tokens[0]})).error,'AlreadyAssisted');
 // Fail every write, including the receipt after both profiles were staged.
 for(let write=1;write<=6;write++){
  reset();id=await invite('a');const before=copy(storage);failAt=write;
  assert.equal((await call('b','brothConfirmAssist',{invitationId:id})).error,'ServerFailure');assert.deepEqual(storage,before);
  failAt=0;assert.equal((await call('b','brothConfirmAssist',{invitationId:id})).status,'Synced');assert.deepEqual(profile('b').tools,[1,1,1]);
 }
 reset();id=await invite('a');r=await call('a','brothCreateInvitation',{operationId:'inv'});const again=await call('a','brothCreateInvitation',{operationId:'inv'});assert.equal(r.invitation.invitationId,again.invitation.invitationId);assert.equal(id,r.invitation.invitationId);
 assert.equal((await call('b','brothClaim',{brothId:'clear',expectedRevision:1})).error,'NotQualified');
 assert.equal((await call('b','brothConfirmAssist',{invitationId:id,account:owner('a')})).error,'WrongPartition');
 assert.equal((await call('b','brothConfirmAssist',{invitationId:'bogus'})).error,'InvalidPayload');
 reset();id=await invite('a');await call('b','brothConfirmAssist',{invitationId:id});
 const claim={brothId:'mushroom',expectedRevision:profile('a').serverRevision,operationId:'claim'};
 const retries=await Promise.all([call('a','brothClaim',claim),call('a','brothClaim',claim)]);assert(retries.every(x=>x.status==='Synced'));
 assert.equal(profile('a').ownedBroths.filter(x=>x==='mushroom').length,1);
 assert.equal((await call('a','brothClaim',{...claim,brothId:'clear'})).error,'OperationConflict');
 console.log('PASS: legacy red, opaque invitation, locked origin/new helper, self/repeat/receipt conflict, three tools, exhausted activity, concurrent assist/claim, permanent choice, future ownership, selection revision, Beijing midnight/limit, every-write rollback, old links, identity partition');
})().catch(e=>{console.error(e);process.exitCode=1;});
