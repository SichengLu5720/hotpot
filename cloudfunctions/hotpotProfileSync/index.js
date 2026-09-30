'use strict';
const cloud=require('wx-server-sdk');
const {createHandler}=require('./profile');
cloud.init({env:cloud.DYNAMIC_CURRENT_ENV});
// wx-server-sdk defaults to throwing for a missing document, including in a
// transaction. Disable only that case; permission/network errors still propagate.
const db=cloud.database({throwOnNotFound:false});
const COLLECTION='hotpot_profiles_v1';
const handle=createHandler({
  environment:process.env.HOTPOT_PROFILE_ENVIRONMENT,
  transaction:async(key,merge)=>db.runTransaction(async transaction=>{
    const reference=transaction.collection(COLLECTION).doc(key);
    const result=await reference.get();
    // CloudBase transactions return data=null/undefined for absent documents.
    // Transport/database errors propagate, never masquerade as an empty profile.
    const candidate=merge(result.data==null?null:result.data);
    if(candidate.changed)await reference.set({data:candidate.stored});
    return candidate.snapshot;
  },3)
});
// Deploy ONLY as a native WeChat client-callable function. Do not expose HTTP/Web,
// timers or cross-function triggers: getWXContext dynamic identity must not be reused
// across mixed invocation types. No identity from event/document is trusted.
exports.main=async(event,context)=>handle(event,cloud.getWXContext());
