mergeInto(LibraryManager.library, {
  $HotpotProfileFunctions: {
    operations:{}, initialized:{}, maxBytes:4194304,
    finish:function(op,kind,json) {
      if(HotpotProfileFunctions.operations[op.id]!==op)return;
      delete HotpotProfileFunctions.operations[op.id];clearTimeout(op.timer);
      var callback=op.callback,pointer=stringToNewUTF8(json||'');
      try { {{{ makeDynCall('viii','callback') }}}(op.id,kind,pointer); }
      finally {_free(pointer);}
    },
    cancel:function(id) {
      var op=HotpotProfileFunctions.operations[id];if(!op)return;
      delete HotpotProfileFunctions.operations[id];clearTimeout(op.timer);
      // callFunction has no guaranteed physical cancellation. Retire the callback;
      // an already-running server transaction may commit and remains idempotent.
      try{if(op.task&&typeof op.task.abort==='function')op.task.abort();}catch(_){}
    },
    start:function(id,env,name,json,callback) {
      if(HotpotProfileFunctions.operations[id])return;
      var op={id:id,callback:callback,timer:0,task:null};HotpotProfileFunctions.operations[id]=op;
      var data;
      try{
        if(!/^[A-Za-z0-9_-]{1,128}$/.test(env)||!/^[A-Za-z0-9_-]{1,128}$/.test(name)||lengthBytesUTF8(json)>HotpotProfileFunctions.maxBytes)throw 0;
        data=JSON.parse(json);if(!data||data.protocolVersion!==1||!/^[a-f0-9]{32}$/.test(data.requestId))throw 0;
      }catch(_){HotpotProfileFunctions.finish(op,4);return;}
      try{
        if(typeof wx==='undefined'||!wx.cloud||typeof wx.cloud.init!=='function'||typeof wx.cloud.callFunction!=='function')throw 0;
        if(!Object.prototype.hasOwnProperty.call(HotpotProfileFunctions.initialized,env)){wx.cloud.init({env:env,traceUser:false});HotpotProfileFunctions.initialized[env]=true;}
      }catch(_){HotpotProfileFunctions.finish(op,1);return;}
      op.timer=setTimeout(function(){HotpotProfileFunctions.finish(op,3);},10000);
      try{
        op.task=wx.cloud.callFunction({name:name,data:data,config:{env:env},
          success:function(value){
            if(HotpotProfileFunctions.operations[id]!==op)return;
            try{
              var result=value&&value.result;if(typeof result==='string')result=JSON.parse(result);
              if(!result||result.protocolVersion!==1||result.requestId!==data.requestId)throw 0;
              var response=JSON.stringify(result);if(lengthBytesUTF8(response)>HotpotProfileFunctions.maxBytes)throw 0;
              HotpotProfileFunctions.finish(op,0,response);
            }catch(_){HotpotProfileFunctions.finish(op,4);}
          },fail:function(){HotpotProfileFunctions.finish(op,2);}
        });
        // Some base-library versions also return a Promise for callback-style calls.
        // Consume rejections without exposing SDK details or producing duplicate results.
        if(op.task&&typeof op.task.catch==='function')op.task.catch(function(){HotpotProfileFunctions.finish(op,2);});
      }catch(_){HotpotProfileFunctions.finish(op,2);}
    }
  },
  HotpotProfileFunction_Start__deps:['$HotpotProfileFunctions','$UTF8ToString','$stringToNewUTF8','$lengthBytesUTF8','free'],
  HotpotProfileFunction_Start:function(id,env,name,json,callback){HotpotProfileFunctions.start(id,UTF8ToString(env),UTF8ToString(name),UTF8ToString(json),callback);},
  HotpotProfileFunction_Cancel__deps:['$HotpotProfileFunctions'],
  HotpotProfileFunction_Cancel:function(id){HotpotProfileFunctions.cancel(id);}
});
