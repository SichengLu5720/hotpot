'use strict';
const fs=require('fs'),vm=require('vm'),assert=require('assert');
const path=require('path');
const source=fs.readFileSync(path.join(__dirname,'../../Platform/WeChat/HotpotCloudAssetBridge.jslib'),'utf8');
// Execute the actual project bridge, substituting only Emscripten's compile-time indirect call.
const executable=source.replace("{{{ makeDynCall('viidi', 'callback') }}}",'callback');
let groups=0;
function rig(options={}) {
  const calls={init:0,downloads:[],events:[],deleted:[],aborts:0,off:0};const timers=new Map();let clock=0;
  const context={LibraryManager:{library:{}},mergeInto:(target,value)=>Object.assign(target,value),stringToNewUTF8:x=>x,_free:()=>{},UTF8ToString:x=>x,
    setTimeout:fn=>{timers.set(++clock,fn);return clock;},clearTimeout:id=>timers.delete(id),
    wx:{cloud:{init:()=>{calls.init++;if(options.initFailure)throw Error();},downloadFile:config=>{
      calls.downloads.push(config);if(options.downloadThrow)throw Error();
      const task={abort:()=>{calls.aborts++;config.fail({errMsg:'abort'});},onProgressUpdate:fn=>{task.progress=fn;},offProgressUpdate:fn=>{assert.strictEqual(fn,task.progress);calls.off++;task.progress=null;}};
      if(options.noProgress){delete task.onProgressUpdate;delete task.offProgressUpdate;}
      calls.task=task;return task;
    }},getFileSystemManager:()=>({unlinkSync:file=>calls.deleted.push(file)})}};
  vm.runInNewContext(executable,context);context.HotpotCloudAssets=context.LibraryManager.library.$HotpotCloudAssets;
  const h=context.HotpotCloudAssets;
  return {calls,timers,h,start:(id=1)=>h.start(id,'fixture-env','cloud://fixture-env.fixture-bucket/release/bundle',60000,(...event)=>calls.events.push(event))};
}
function test(name,fn){fn();groups++;console.log(name+' PASS');}
test('J01-native-init-exact-file-and-progress',()=>{const r=rig();assert.equal(r.calls.init,0);r.start();assert.equal(r.calls.init,1);assert.equal(r.calls.downloads[0].fileID,'cloud://fixture-env.fixture-bucket/release/bundle');assert.equal(r.calls.downloads[0].config.env,'fixture-env');r.calls.task.progress({totalBytesWritten:9});assert.equal(r.calls.events[0][1],0);assert.equal(r.calls.events[0][2],9);});
test('J02-success-cleans-listeners-timers-temp',()=>{const r=rig();r.start();const lateProgress=r.calls.task.progress;r.calls.downloads[0].success({statusCode:200,tempFilePath:'temp-a'});assert.equal(r.calls.events[0][1],1);assert.deepEqual(r.calls.deleted,['temp-a']);assert.equal(r.timers.size,0);assert.equal(Object.keys(r.h.operations).length,0);assert.equal(r.calls.off,1);lateProgress({totalBytesWritten:10});assert.equal(r.calls.events.length,1);});
test('J03-real-abort-and-late-success-cleanup',()=>{const r=rig();r.start();const old=r.calls.downloads[0];r.h.abort(1);assert.equal(r.calls.aborts,1);assert.equal(r.calls.events.length,0);assert.equal(r.timers.size,0);r.start(2);old.success({tempFilePath:'stale-temp',statusCode:200});assert.deepEqual(r.calls.deleted,['stale-temp']);assert.equal(r.calls.events.length,0);r.calls.downloads[1].success({tempFilePath:'new-temp',statusCode:200});assert.equal(r.calls.events[0][0],2);});
test('J04-permission-init-download-failure',()=>{for(const kind of ['permission denied','offline']){const r=rig();r.start();r.calls.downloads[0].fail({errMsg:kind});assert.equal(r.calls.events[0][1],kind==='offline'?4:3);assert.equal(r.calls.downloads.length,1);assert.equal(r.timers.size,0);}const r=rig({initFailure:true});r.start();assert.equal(r.calls.events[0][1],2);assert.equal(r.calls.downloads.length,0);const thrown=rig({downloadThrow:true});thrown.start();assert.equal(thrown.calls.events[0][1],4);});
test('J05-duplicate-completion-and-no-progress-support',()=>{const r=rig({noProgress:true});r.start();const request=r.calls.downloads[0];request.success({tempFilePath:'temp-a'});request.success({tempFilePath:'temp-b'});request.fail({errMsg:'offline'});assert.equal(r.calls.events.length,1);assert.deepEqual(r.calls.deleted,['temp-a','temp-b']);});
test('J06-timeout-aborts-before-notification',()=>{const r=rig();r.start();Array.from(r.timers.values())[0]();assert.equal(r.calls.aborts,1);assert.equal(r.calls.events.length,1);assert.equal(r.calls.events[0][1],5);assert.equal(r.timers.size,0);});
test('J07-twenty-cycles-no-retained-native-operations',()=>{const r=rig();for(let id=1;id<=20;id++){r.start(id);if(id%2)r.h.abort(id);else r.calls.downloads.at(-1).success({tempFilePath:'temp-'+id});}assert.equal(r.calls.init,1);assert.equal(r.calls.downloads.length,20);assert.equal(r.timers.size,0);assert.equal(Object.keys(r.h.operations).length,0);assert.equal(r.calls.off,20);});
console.log('CLOUD_ASSET_NATIVE_BRIDGE_PASS groups='+groups+' externalRequests=0');
