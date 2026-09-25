'use strict';
const actions=['reserveTool','commitTool','cancelTool'];
function fail(code){throw Object.assign(new Error(code),{code});}
function live(p,time){return (p.toolReservations||[]).filter(r=>r.expiresAt>time);}
function available(p,tool,time){return p.tools[tool]-live(p,time).filter(r=>r.tool===tool).length;}
async function execute({tx,p,key,save,actor,event,time}){
 const op=event.operationId,session=event.sessionId,tool=event.tool;
 if(typeof op!=='string'||!op.length||op.length>160||typeof session!=='string'||!session.length||session.length>160||tool!==0)fail('InvalidPayload');
 const reservationKey='tool:'+actor+':'+op;
 let reservation=await tx.get(reservationKey);
 if(reservation&&(reservation.tool!==tool||reservation.session!==session))fail('OperationConflict');
 p.toolReservations=live(p,time);
 if(event.action==='reserveTool'){
  if(reservation){if(reservation.status!=='Reserved'||reservation.expiresAt<=time)fail('ReservationClosed');}
  else{
   if(available(p,tool,time)<1)fail('InsufficientInventory');
   reservation={status:'Reserved',tool,session,expiresAt:time+120000};
   p.toolReservations.push({id:op,tool,expiresAt:reservation.expiresAt});
  }
 }else if(event.action==='cancelTool'){
  // A tombstone also prevents a delayed reserve request from taking inventory.
  if(!reservation)reservation={status:'Cancelled',tool,session,expiresAt:time};
  if(reservation.status==='Reserved')reservation.status='Cancelled';
  p.toolReservations=p.toolReservations.filter(r=>r.id!==op);
 }else{
  if(!reservation||reservation.status==='Cancelled'||reservation.expiresAt<=time&&reservation.status!=='Committed')fail('ReservationClosed');
  if(reservation.status!=='Committed'){
   if(p.tools[tool]<1)fail('InsufficientInventory');
   p.tools[tool]--;p.appliedOperations.push(op);reservation.status='Committed';
  }
  p.toolReservations=p.toolReservations.filter(r=>r.id!==op);
 }
 await tx.set(reservationKey,reservation);await save(key,p);
 return {snapshot:p,toolReservationStatus:reservation.status};
}
module.exports={actions,execute,available};
