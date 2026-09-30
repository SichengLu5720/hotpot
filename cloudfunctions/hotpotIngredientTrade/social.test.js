'use strict';
const assert=require('node:assert/strict');
const {createHandler,account,empty}=require('./trade');
const environment='development',context=id=>({SOURCE:'wx_client',ENV:'env',APPID:'app',OPENID:id});
const identity=id=>account(context(id),environment),copy=x=>JSON.parse(JSON.stringify(x));
let data={},tail=Promise.resolve(),clock=Date.parse('2026-09-26T00:00:00Z'),counter=0,failAt=0;
const transaction=fn=>{const task=tail.then(async()=>{const next=copy(data);let writes=0;const result=await fn({get:async k=>next[k]?copy(next[k]):null,set:async(k,v)=>{next[k]=copy(v);if(++writes===failAt)throw Error('injected');}});data=next;return result;});tail=task.catch(()=>{});return task;};
const handle=createHandler({environment,transaction,now:()=>clock});
const call=(who,action,fields={})=>handle({protocolVersion:1,environment,requestId:'r'+(++counter),operationId:'o'+counter,action,...fields},context(who));
const profile=who=>data['profile:'+identity(who)];
function seed(){data={};failAt=0;for(const who of ['a','b','c']){const p=empty(environment,identity(who));p.duplicates[0]=2;p.duplicates[1]=2;data['profile:'+identity(who)]=p;}}
const create=(fields={})=>call('a','createLink',{offeredId:'food_00',receivedId:'food_01',...fields});
(async()=>{
 seed();let r=await create({operationId:'create'});assert.equal(r.status,'Synced');let id=r.trade.requestId;assert.match(id,/^[a-f0-9]{64}$/);assert.equal(r.trade.recipient,'');assert.equal(r.trade.linkExchange,true);
 assert.equal((await create({operationId:'create'})).trade.requestId,id);
 assert.equal((await create({operationId:'create',receivedId:'food_02'})).error,'OperationConflict');
 assert.equal((await call('b','getTrade',{tradeId:id})).status,'Synced');assert.equal(profile('b').entryUnlocked,false);
 assert.equal((await call('a','accept',{tradeId:id})).error,'NotAuthorized');assert.equal((await call('b','withdraw',{tradeId:id})).error,'NotAuthorized');
 const result=await Promise.all([call('b','accept',{tradeId:id,operationId:'accept'}),call('b','accept',{tradeId:id,operationId:'accept'})]);assert(result.every(x=>x.status==='Synced'));
 assert.equal(profile('a').duplicates[0],1);assert.equal(profile('a').duplicates[1],3);assert.equal(profile('b').duplicates[0],3);assert.equal(profile('b').duplicates[1],1);assert.equal(profile('a').unlocked.length,16);
 assert.equal((await call('c','accept',{tradeId:id})).error,'NotParticipant');assert.equal((await create({operationId:'create'})).trade.status,'Accepted');
 assert.equal((await call('b','reject',{tradeId:id,operationId:'accept'})).error,'OperationConflict');
 assert.equal((await call('b','list')).trades.length,1);
 seed();id=(await create()).trade.requestId;const race=await Promise.all(['b','c'].map(who=>call(who,'accept',{tradeId:id})));assert.equal(race.filter(x=>x.status==='Synced').length,1);assert.equal(profile('a').duplicates[0],1);
 seed();id=(await create()).trade.requestId;await call('b','reject',{tradeId:id});assert.equal((await call('b','accept',{tradeId:id})).error,'TradeClosed');assert.equal(profile('a').duplicates[0],2);
 seed();id=(await create()).trade.requestId;await call('a','withdraw',{tradeId:id});assert.equal((await call('b','accept',{tradeId:id})).error,'TradeClosed');
 seed();id=(await create()).trade.requestId;clock+=86400000;assert.equal((await call('b','getTrade',{tradeId:id})).trade.status,'Expired');assert.equal((await call('b','accept',{tradeId:id})).error,'TradeExpired');
 for(const who of ['a','b']){seed();id=(await create()).trade.requestId;profile(who).duplicates[who==='a'?0:1]=0;const before=copy(data);assert.equal((await call('b','accept',{tradeId:id})).error,'InsufficientInventory');assert.deepEqual(data,before);}
 // Five writes commit the two profiles, recipient index, trade and operation.
 for(let write=1;write<=5;write++){seed();id=(await create()).trade.requestId;const before=copy(data);failAt=write;assert.equal((await call('b','accept',{tradeId:id})).error,'ServerFailure');assert.deepEqual(data,before);}
 seed();profile('a').duplicates[16]=1;profile('a').unlocked.push('food_16');id=(await create({offeredId:'food_16'})).trade.requestId;assert.equal((await call('b','accept',{tradeId:id})).status,'Synced');assert(profile('b').unlocked.includes('food_16'));assert.equal(profile('b').duplicates[16],0);assert(profile('a').unlocked.includes('food_16'));
 assert.equal((await create({offeredId:'food_00',receivedId:'food_00'})).error,'InvalidTrade');
 assert.equal((await call('b','getTrade',{tradeId:'unknown'})).error,'TradeNotFound');
 assert.equal((await call('b','getTrade',{tradeId:id,account:identity('a')})).error,'WrongPartition');
 console.log('PASS TASK036 link exchange: opaque/retry/conflict, no-first-win recipient, self/withdraw permissions, concurrent accept, duplicate callbacks, reject/withdraw/24h expiry, both inventories, every-write rollback, permanent ownership, actor spoof');
})().catch(e=>{console.error(e);process.exitCode=1;});
