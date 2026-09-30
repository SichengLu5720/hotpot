// Project-owned bridge. Callback protocol: id, kind, amount, temporaryPath.
// kind: 0 progress, 1 success, 2 init/unavailable, 3 permission, 4 download,
// 5 timeout. No cloud identifiers, SDK errors or credentials are logged.
mergeInto(LibraryManager.library, {
  HotpotCloudAsset_FileSize__deps: ['$UTF8ToString'],
  HotpotCloudAsset_FileSize: function(path) {
    try {
      var info = wx.getFileSystemManager().statSync(UTF8ToString(path), false);
      var size = info && info.size;
      return Number.isSafeInteger(size) && size >= 0 && size <= 2147483647 ? size : -1;
    } catch (_) { return -1; }
  },
  $HotpotCloudAssets: {
    operations: {}, initialized: {},
    removeFile: function(path) {
      if (!path) return;
      try { wx.getFileSystemManager().unlinkSync(path); } catch (_) {}
    },
    detach: function(op) {
      if (op.timer) { clearTimeout(op.timer); op.timer = 0; }
      if (op.progressAttached) {
        op.progressAttached = false;
        try { if (op.task && op.task.offProgressUpdate) op.task.offProgressUpdate(op.progress); } catch (_) {}
      }
    },
    emit: function(op, kind, amount, path) {
      var callback = op.callback;
      var pointer = stringToNewUTF8(path || '');
      try { {{{ makeDynCall('viidi', 'callback') }}}(op.id, kind, amount || 0, pointer); }
      finally { _free(pointer); }
    },
    finish: function(op, kind, path) {
      if (op.finished || HotpotCloudAssets.operations[op.id] !== op) {
        if (path && path !== op.path) HotpotCloudAssets.removeFile(path);
        return;
      }
      op.finished = true;
      op.path = path || '';
      HotpotCloudAssets.detach(op);
      // C# consumes synchronously during emit; always release even on a receiver failure.
      try { HotpotCloudAssets.emit(op, kind, 0, op.path); }
      finally { HotpotCloudAssets.release(op.id); }
    },
    release: function(id) {
      var op = HotpotCloudAssets.operations[id];
      if (!op) return;
      delete HotpotCloudAssets.operations[id];
      HotpotCloudAssets.detach(op);
      HotpotCloudAssets.removeFile(op.path);
      op.path = '';
    },
    abort: function(id) {
      var op = HotpotCloudAssets.operations[id];
      if (!op) return;
      op.finished = true;
      // Real platform cancellation precedes releasing the managed waiter.
      try { if (op.task) op.task.abort(); } catch (_) {}
      HotpotCloudAssets.release(id);
    },
    start: function(id, env, fileID, timeout, callback) {
      if (HotpotCloudAssets.operations[id]) return;
      var op = { id:id, callback:callback, finished:false, task:null, path:'', timer:0 };
      HotpotCloudAssets.operations[id] = op;
      try {
        if (typeof wx === 'undefined' || !wx.cloud || typeof wx.cloud.init !== 'function' || typeof wx.cloud.downloadFile !== 'function') throw 0;
        if (!Object.prototype.hasOwnProperty.call(HotpotCloudAssets.initialized, env)) {
          wx.cloud.init({ env:env, traceUser:false });
          HotpotCloudAssets.initialized[env] = true;
        }
      } catch (_) { HotpotCloudAssets.finish(op, 2); return; }
      op.progress = function(result) {
        if (!op.finished && HotpotCloudAssets.operations[id] === op && result && Number.isFinite(result.totalBytesWritten) && result.totalBytesWritten >= 0)
          HotpotCloudAssets.emit(op, 0, result.totalBytesWritten);
      };
      try {
        op.task = wx.cloud.downloadFile({
          fileID:fileID, config:{ env:env },
          success:function(result) {
            if (!result || !result.tempFilePath || (result.statusCode !== undefined && result.statusCode !== 200)) {
              if (result && result.tempFilePath) HotpotCloudAssets.removeFile(result.tempFilePath);
              HotpotCloudAssets.finish(op, result && result.statusCode === 403 ? 3 : 4);return;
            }
            HotpotCloudAssets.finish(op, 1, result.tempFilePath);
          },
          fail:function(error) {
            // Used only for failure classification; never sent to Unity/logs.
            var denied = error && (error.errCode === -502003 || /permission|denied|unauthori[sz]ed|access.?reject/i.test(error.errMsg || ''));
            HotpotCloudAssets.finish(op, denied ? 3 : 4);
          }
        });
        if (op.finished) { HotpotCloudAssets.detach(op); return; }
        if (!op.task || typeof op.task.abort !== 'function') { HotpotCloudAssets.finish(op, 2); return; }
        if (typeof op.task.onProgressUpdate === 'function') {
          // Older platforms can omit/decline progress without weakening completion integrity.
          try { op.task.onProgressUpdate(op.progress); op.progressAttached = true; } catch (_) {}
        }
        op.timer = setTimeout(function() {
          if (op.finished) return;
          // Abort may synchronously invoke fail; classify timeout first and suppress that callback.
          op.finished = true;
          try { op.task.abort(); } catch (_) {}
          op.finished = false;
          HotpotCloudAssets.finish(op, 5);
        }, Math.min(60000, Math.max(1, timeout)));
      } catch (_) { HotpotCloudAssets.finish(op, 4); }
    }
  },
  HotpotCloudAsset_Start__deps: ['$HotpotCloudAssets', '$stringToNewUTF8', '$UTF8ToString', 'free'],
  HotpotCloudAsset_Start: function(id, env, fileID, timeout, callback) {
    HotpotCloudAssets.start(id, UTF8ToString(env), UTF8ToString(fileID), timeout, callback);
  },
  HotpotCloudAsset_Abort__deps: ['$HotpotCloudAssets'],
  HotpotCloudAsset_Abort: function(id) { HotpotCloudAssets.abort(id); },
  HotpotCloudAsset_Release__deps: ['$HotpotCloudAssets'],
  HotpotCloudAsset_Release: function(id) { HotpotCloudAssets.release(id); }
});
