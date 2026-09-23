'use strict';
const fs=require('fs'),vm=require('vm'),path=require('path'),assert=require('assert/strict');
const source=fs.readFileSync(path.join(__dirname,'../../Platform/WeChat/HotpotProfileFunctionBridge.jslib'),'utf8');
const executable=source.replace("{{{ makeDynCall('viii','callback') }}}",'callback');
let groups=0;
function rig(options={}){
 const calls={init:0,requests:[],events:[]},timers=new Map();let timerId=0;
 const context={LibraryManager:{library:{}},mergeInto:(a,b)=>Object.assign(a,b),stringToNewUTF8:s=>s,_free:()=>{},UTF8ToString:s=>s,lengthBytesUTF8:s=>Buffer.byteLength(s,'utf8'),
   setTimeout:fn=>{timers.set(++timerId,fn);return timerId;},clearTimeout:id=>timers.delete(id),
   wx:{cloud:{init:()=>{calls.init++;if(options.initFailure)throw Error('private init');},callFunction:request=>{if(options.callThrow)throw Error('private error');calls.requests.push(request);return Promise.resolve();}}}};
 vm.runInNewContext(executable,context);context.HotpotProfileFunctions=context.LibraryManager.library.$HotpotProfileFunctions;
 const h=context.HotpotProfileFunctions;
 const json=JSON.stringify({protocolVersion:1,requestId:'a'.repeat(32),action:'identity',environment:'development',document:null});
 return{calls,timers,h,start:(id=1,body=json)=>h.start(id,'fixture-env','hotpotProfileSync',body,(...event)=>calls.events.push(event)),success:(i=0,mutate=x=>x)=>{const q=calls.requests[i];q.success({result:mutate({protocolVersion:1,requestId:q.data.requestId,status:'Synced'})});}};
}
function test(name,body){body();groups++;console.log(name+' PASS');}
test('PB01-init-only-on-call-binding-and-success',()=>{const r=rig();assert.equal(r.calls.init,0);r.start();assert.equal(r.calls.init,1);assert.equal(r.calls.requests[0].config.env,'fixture-env');assert.equal(r.calls.requests[0].name,'hotpotProfileSync');r.success();assert.equal(r.calls.events[0][1],0);assert.equal(r.timers.size,0);assert.equal(Object.keys(r.h.operations).length,0);});
test('PB02-cancel-late-success-and-new-generation',()=>{const r=rig();r.start();r.h.cancel(1);r.start(2);r.success(0);assert.equal(r.calls.events.length,0);r.success(1);assert.equal(r.calls.events[0][0],2);assert.equal(r.calls.init,1);assert.equal(r.timers.size,0);});
test('PB03-duplicate-fail-and-completion',()=>{const r=rig();r.start();r.success();r.calls.requests[0].fail({errMsg:'private'});r.success();assert.equal(r.calls.events.length,1);});
test('PB04-timeout-no-claim-of-server-abort',()=>{const r=rig();r.start();Array.from(r.timers.values())[0]();assert.equal(r.calls.events[0][1],3);r.success();assert.equal(r.calls.events.length,1);assert.equal(r.timers.size,0);});
test('PB05-init-network-and-call-errors-sanitized',()=>{const a=rig({initFailure:true});a.start();assert.equal(a.calls.events[0][1],1);assert.equal(a.calls.requests.length,0);const b=rig({callThrow:true});b.start();assert.equal(b.calls.events[0][1],2);const c=rig();c.start();c.calls.requests[0].fail({errMsg:'private credential'});assert.equal(c.calls.events[0][1],2);assert.ok(!JSON.stringify(c.calls.events).includes('private'));});
test('PB06-malformed-oversized-correlation-string-result',()=>{for(const body of ['{',JSON.stringify({protocolVersion:2,requestId:'a'.repeat(32)}),'x'.repeat(4194305)]){const r=rig();r.start(1,body);assert.equal(r.calls.events[0][1],4);assert.equal(r.calls.init,0);}const r=rig();r.start();r.success(0,x=>({...x,requestId:'b'.repeat(32)}));assert.equal(r.calls.events[0][1],4);const string=rig();string.start();string.success(0,x=>JSON.stringify(x));assert.equal(string.calls.events[0][1],0);});
test('PB07-twenty-cycles-no-callback-timer-accumulation',()=>{const r=rig();for(let i=0;i<20;i++){r.start(i+1);if(i%2)r.success(i);else r.h.cancel(i+1);}assert.equal(r.calls.init,1);assert.equal(r.timers.size,0);assert.equal(Object.keys(r.h.operations).length,0);});
console.log('CLOUD_PROFILE_BRIDGE_PASS groups='+groups+' externalRequests=0');
