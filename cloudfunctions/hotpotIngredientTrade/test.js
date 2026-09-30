'use strict';
const assert=require('node:assert/strict');
const {createHandler,account,empty,rewardDay}=require('./trade');
const context=id=>({SOURCE:'wx_client',APPID:'app',OPENID:id,ENV:'env'}),environment='development';
const a=account(context('a'),environment),b=account(context('b'),environment);
let storage={},clock=Date.parse('2026-09-25T01:00:00Z'),tail=Promise.resolve(),counter=0;
const copy=x=>JSON.parse(JSON.stringify(x));
const transaction=fn=>{const task=tail.then(async()=>{const next=copy(storage),result=await fn({get:async k=>next[k]?copy(next[k]):null,set:async(k,v)=>{next[k]=copy(v);}});storage=next;return result;});tail=task.catch(()=>{});return task;};
const dependencies={environment,transaction,now:()=>clock,random:()=>16,verifyFriend:async()=>true};
const handle=createHandler(dependencies);
const call=(who,action,fields={})=>handle({protocolVersion:1,requestId:'call'+(++counter),environment,action,operationId:'op'+counter,...fields},context(who));
function seed(){storage={};for(const owner of [a,b]){const p=empty(environment,owner);p.duplicates[0]=2;p.duplicates[1]=2;storage['profile:'+owner]=p;}}
async function create(id){return call('a','create',{targetAccount:b,offeredId:'food_00',receivedId:'food_01',tradeId:id});}
(async()=>{
 seed();assert.equal(rewardDay(Date.parse('2026-09-24T21:59:59Z')),'20260924');assert.equal(rewardDay(Date.parse('2026-09-24T22:00:00Z')),'20260925');
 let r=await call('a','sync',{pendingWins:[{sessionId:'s',day:'20260925',utc:'2026-09-25T00:00:00Z'}]});assert.equal(r.snapshot.unlocked.length,17);
 r=await call('a','sync',{pendingWins:[{sessionId:'s',day:'20260925',utc:'2026-09-25T00:00:00Z'}]});assert.equal(r.snapshot.rewards.length,1);
 await create('t');const results=await Promise.all([call('b','accept',{tradeId:'t',operationId:'accept'}),call('b','accept',{tradeId:'t',operationId:'accept'})]);assert(results.every(x=>x.status==='Synced'));assert.equal(storage['profile:'+a].duplicates[0],1);assert.equal(storage['profile:'+b].duplicates[0],3);
 assert.equal((await call('b','accept',{tradeId:'t',operationId:'again'})).error,'TradeClosed');
 assert.equal((await call('b','reject',{tradeId:'t',operationId:'accept'})).error,'OperationConflict');
 await create('withdraw');await call('a','withdraw',{tradeId:'withdraw'});assert.equal((await call('b','accept',{tradeId:'withdraw'})).error,'TradeClosed');
 await create('expire');clock+=86400000;assert.equal((await call('b','accept',{tradeId:'expire'})).error,'TradeExpired');
 seed();await create('insufficient');storage['profile:'+b].duplicates[1]=0;let before=copy(storage);assert.equal((await call('b','accept',{tradeId:'insufficient'})).error,'InsufficientInventory');assert.deepEqual(storage,before);
 seed();storage['profile:'+a].duplicates[0]=1;await create('race1');await create('race2');const race=await Promise.all([call('b','accept',{tradeId:'race1'}),call('b','accept',{tradeId:'race2'})]);assert.equal(race.filter(x=>x.status==='Synced').length,1);
 before=copy(storage);assert.equal((await call('a','selection',{selected:storage['profile:'+a].selected,expectedRevision:0})).error,'StaleRevision');assert.deepEqual(storage,before);
 assert.equal((await call('a','read',{account:b})).error,'WrongPartition');assert.deepEqual(storage,before);
 const closed=createHandler({...dependencies,verifyFriend:null});assert.equal((await closed({protocolVersion:1,requestId:'x',environment,action:'create',operationId:'x',targetAccount:b,offeredId:'food_00',receivedId:'food_01',tradeId:'x'},context('a'))).error,'FriendVerificationUnavailable');assert.deepEqual(storage,before);
 assert.equal((await handle({requestId:'x'}, {...context('a'),SOURCE:'http'})).error,'Unauthenticated');
 seed();const full=storage['profile:'+a];full.unlocked=Array.from({length:32},(_,i)=>'food_'+String(i).padStart(2,'0'));const toolsHandle=createHandler({...dependencies,random:()=>1});r=await toolsHandle({protocolVersion:1,requestId:'tools',environment,action:'sync',operationId:'tools',pendingWins:[{sessionId:'full',day:'20260925',utc:'2026-09-25T00:00:00Z'}]},context('a'));assert.deepEqual(r.snapshot.tools,[0,2,0]);
 console.log('PASS: reward day, reward dedupe, two tool rolls, repeated accept, operation conflict, withdrawal, expiry, insufficient inventory rollback, concurrent accept, stale revision, identity spoof, friend fail-closed');
})().catch(e=>{console.error(e);process.exitCode=1;});
