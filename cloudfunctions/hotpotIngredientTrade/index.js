'use strict';
const cloud=require('wx-server-sdk'),crypto=require('crypto');
const {createHandler}=require('./trade');
cloud.init({env:cloud.DYNAMIC_CURRENT_ENV});
const db=cloud.database({throwOnNotFound:false});
const handle=createHandler({environment:process.env.HOTPOT_PROFILE_ENVIRONMENT,
 // No reliable server-side WeChat friendship verifier is configured. Trade writes
 // fail closed until an independently authenticated verifier is implemented.
 verifyFriend:null,
 transaction:run=>db.runTransaction(async transaction=>{
  const ref=key=>transaction.collection('hotpot_ingredient_trade_v1').doc(crypto.createHash('sha256').update(key).digest('hex'));
  return run({get:async key=>{const value=(await ref(key).get()).data;if(!value)return null;const {_id,...result}=value;return result;},set:(key,value)=>ref(key).set({data:value})});
 },3)});
exports.main=event=>handle(event,cloud.getWXContext());
