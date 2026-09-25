'use strict';
const crypto=require('crypto');
const candidates=['clear','tomato','mushroom'];
const actions=['brothRead','brothCreateInvitation','brothInspectInvitation','brothConfirmAssist','brothClaim','brothSelect'];
function fail(code){throw Object.assign(new Error(code),{code});}
function normalize(p){
 p.ownedBroths=[...new Set(['red',...(Array.isArray(p.ownedBroths)?p.ownedBroths:[]).filter(x=>candidates.includes(x))])];
 p.brothActivityChoice=candidates.includes(p.brothActivityChoice)?p.brothActivityChoice:'';
 if(p.brothActivityChoice&&!p.ownedBroths.includes(p.brothActivityChoice))p.ownedBroths.push(p.brothActivityChoice);
 p.brothActivityQualified=p.brothActivityQualified===true||!!p.brothActivityChoice;
 if(!p.ownedBroths.includes(p.currentBroth))p.currentBroth='red';
 return p;
}
const day=ms=>new Date(ms+8*3600000).toISOString().slice(0,10);
function operation(value){if(typeof value!=='string'||!value.length||value.length>160||/[\x00-\x1f]/.test(value))fail('InvalidPayload');return value;}
function token(value){if(typeof value!=='string'||! /^[a-f0-9]{64}$/.test(value))fail('InvalidPayload');return value;}
async function execute({tx,p,key,save,actor,event,time,environment,empty}){
 const action=event.action;
 if(action==='brothRead')return {snapshot:p};
 async function invitation(id){const v=await tx.get('brothInvitation:'+token(id));if(!v)fail('NotFound');return v;}
 async function view(id,v){
  const owner=v.owner===actor?p:normalize(await tx.get('profile:'+v.owner)||empty(environment,v.owner));
  const ledger=await tx.get('brothDay:'+actor+':'+day(time));
  return {invitationId:id,isInitiator:v.owner===actor,canAssist:v.owner!==actor&&!owner.brothActivityQualified&&(!ledger||ledger.owners.length<3)};
 }
 if(action==='brothInspectInvitation'){const v=await invitation(event.invitationId);return {snapshot:p,invitation:await view(event.invitationId,v)};}
 const opKey='operation:'+actor+':'+operation(event.operationId);
 // Canonical key order prevents harmless JSON property ordering from changing identity.
 const args={...event};delete args.requestId;
 const fingerprint=crypto.createHash('sha256').update(JSON.stringify(Object.keys(args).sort().map(k=>[k,args[k]]))).digest('hex');
 const receipt=await tx.get(opKey);
 if(receipt){if(receipt.fingerprint!==fingerprint)fail('OperationConflict');return {snapshot:p,...(receipt.invitationId?{invitation:await view(receipt.invitationId,await invitation(receipt.invitationId))}:{})};}
 let invitationId;
 if(action==='brothCreateInvitation'){
  if(!p.entryUnlocked)fail('EntryLocked');
  if(p.brothActivityQualified)fail('ActivityComplete');
  const ownerKey='brothOwnerInvitation:'+actor,existing=await tx.get(ownerKey);
  invitationId=existing?existing.invitationId:crypto.randomBytes(32).toString('hex');
  if(!existing){if(await tx.get('brothInvitation:'+invitationId))fail('ServerFailure');await tx.set('brothInvitation:'+invitationId,{owner:actor,createdAt:time});await tx.set(ownerKey,{invitationId});}
 }else if(action==='brothConfirmAssist'){
  const v=await invitation(event.invitationId);
  if(v.owner===actor)fail('SelfAssist');
  const pairKey='brothAssist:'+actor+':'+v.owner;
  if(await tx.get(pairKey))fail('AlreadyAssisted');
  const ownerKey='profile:'+v.owner,owner=normalize(await tx.get(ownerKey)||empty(environment,v.owner));
  if(owner.brothActivityQualified)fail('ActivityComplete');
  const ledgerKey='brothDay:'+actor+':'+day(time),ledger=await tx.get(ledgerKey)||{owners:[]};
  if(ledger.owners.length>=3)fail('DailyLimit');
  owner.brothActivityQualified=true;
  owner.brothActivityAssistant=actor;
  for(let i=0;i<3;i++)p.tools[i]++;
  ledger.owners.push(v.owner);
  v.assistant=actor;v.completedAt=time;
  await save(ownerKey,owner);
  await tx.set('brothInvitation:'+event.invitationId,v);
  await tx.set(pairKey,{invitationId:event.invitationId,completedAt:time});
  await tx.set(ledgerKey,ledger);
 }else if(action==='brothClaim'||action==='brothSelect'){
  if(action==='brothClaim'&&p.brothActivityChoice)fail('AlreadyChosen');
  if(event.expectedRevision!==p.serverRevision)fail('StaleRevision');
  if(action==='brothClaim'){
   if(!candidates.includes(event.brothId))fail('InvalidPayload');
   if(!p.brothActivityQualified)fail('NotQualified');
   p.brothActivityChoice=event.brothId;
   if(!p.ownedBroths.includes(event.brothId))p.ownedBroths.push(event.brothId);
  }else if(!p.ownedBroths.includes(event.brothId))fail('NotOwned');
  p.currentBroth=event.brothId;
 }
 await save(key,p);
 await tx.set(opKey,{fingerprint,invitationId:invitationId||null});
 return {snapshot:p,...(invitationId?{invitation:await view(invitationId,await invitation(invitationId))}:{})};
}
module.exports={actions,normalize,day,execute};
