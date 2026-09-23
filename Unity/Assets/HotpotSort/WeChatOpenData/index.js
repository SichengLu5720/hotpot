/* Project-owned, self-contained open-data entry. No dependencies or main-domain data egress. */
(function (root) {
  'use strict';
  const SCORE_KEY = 'hotpot_first_wins_v1';
  const integer = (v, min, max) => Number.isSafeInteger(v) && v >= min && v <= max;
  const object = v => v !== null && typeof v === 'object' && !Array.isArray(v);
  const keys = (v, list) => object(v) && Object.keys(v).length === list.length && list.every(k => Object.prototype.hasOwnProperty.call(v, k));
  const marker = v => typeof v === 'string' && /^[A-Za-z0-9_-]{1,128}$/.test(v);
  const timestamp = v => typeof v === 'string' && /^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{3}Z$/.test(v) && Number.isFinite(Date.parse(v)) && new Date(v).toISOString() === v;
  function viewport(p) {
    return keys(p, ['x','y','width','height','dpr']) && integer(p.x,0,16384) && integer(p.y,0,16384) &&
      integer(p.width,120,4096) && integer(p.height,160,8192) && p.width*p.height <= 16777216 &&
      typeof p.dpr === 'number' && Number.isFinite(p.dpr) && p.dpr >= .5 && p.dpr <= 8;
  }
  function parseMessage(raw) {
    if (typeof raw !== 'string' || raw.length > 4096) return null;
    let m; try { m=JSON.parse(raw); } catch (_) { return null; }
    if (!keys(m,['version','type','viewEpoch','requestId','payload']) || m.version !== 1 ||
        !integer(m.viewEpoch,0,2147483647) || !integer(m.requestId,1,2147483647)) return null;
    if (m.type === 'open' || m.type === 'refresh') return viewport(m.payload) && m.viewEpoch > 0 ? m : null;
    if (m.type === 'close') return keys(m.payload,[]) && m.viewEpoch > 0 ? m : null;
    if (m.type === 'publishScore' && keys(m.payload,['firstWins','ownerMarker','updatedAtUtc']) &&
        integer(m.payload.firstWins,0,2147483647) && marker(m.payload.ownerMarker) && timestamp(m.payload.updatedAtUtc)) return m;
    return null;
  }
  function score(raw) {
    if (typeof raw !== 'string' || raw.length > 1024) return null;
    let value; try { value=JSON.parse(raw); } catch (_) { return null; }
    if (!object(value) || Object.keys(value).some(k=>!['schema','firstWins','updatedAtUtc','ownerMarker'].includes(k)) || value.schema !== 1 ||
        !integer(value.firstWins,0,2147483647) || !timestamp(value.updatedAtUtc)) return null;
    // Missing identity markers do not invalidate genuine scores; they cannot establish ownership.
    return {schema:1,firstWins:value.firstWins,updatedAtUtc:value.updatedAtUtc,ownerMarker:marker(value.ownerMarker)?value.ownerMarker:''};
  }
  function stored(kvs) { return Array.isArray(kvs) ? kvs.find(v => object(v) && v.key === SCORE_KEY) : null; }
  function rankRows(records) {
    const rows=[];
    if (!Array.isArray(records)) return rows;
    records.forEach((record,index) => {
      if (!object(record)) return;
      const item=stored(record.KVDataList), s=item && score(item.value);
      if (!s) return;
      rows.push({ nickname: typeof record.nickname === 'string' ? record.nickname : '',
        avatarUrl: typeof record.avatarUrl === 'string' ? record.avatarUrl : '',
        firstWins:s.firstWins, ownerMarker:s.ownerMarker, platformIndex:index, isSelf:false });
    });
    rows.sort((a,b) => b.firstWins-a.firstWins || a.platformIndex-b.platformIndex);
    rows.forEach((row,index) => { row.rank=index > 0 && rows[index-1].firstWins === row.firstWins ? rows[index-1].rank : index+1; });
    return rows;
  }
  function errorState(error) {
    const message=String(error && error.errMsg || '');
    if (/announce.*privacy usage/i.test(message)) return 'privacy-declaration';
    return /auth|permission|deny|denied|scope/i.test(message) ? 'permission' : 'network';
  }
  function create(api, timers) {
    const canvas=api.getSharedCanvas(), ctx=canvas.getContext('2d');
    const schedule=timers && timers.setTimeout || setTimeout;
    const unschedule=timers && timers.clearTimeout || clearTimeout;
    let epoch=0, lastRequest=0, visible=false, generation=0, bounds=null, status='closed';
    let rows=[], self={ nickname:'',avatarUrl:'',firstWins:null,rank:null }, scroll=0, drag=null;
    let localMax=null, canonicalMarker='', remoteKnown=null, pendingPublish=null, retryPublish=null, writing=false;
    const pending=new Set(), images=new Map();
    function call(method,args,success,fail,scope) {
      let done=false, timer;
      const cancel=() => { done=true; unschedule(timer); pending.delete(cancel); };
      const settle=(fn,value) => { if (done) return; cancel(); if (scope === null || (visible && scope === generation)) fn(value); };
      timer=schedule(() => settle(fail,{errMsg:'timeout'}),12000);
      if (scope !== null) pending.add(cancel);
      try {
        if (typeof api[method] !== 'function') { settle(fail,{errMsg:'api unavailable'}); return; }
        api[method](Object.assign({},args,{success:v=>settle(success,v),fail:e=>settle(fail,e)}));
      } catch (_) { settle(fail,{errMsg:'api failed'}); }
    }
    function invalidate() {
      generation++; pending.forEach(cancel=>cancel()); pending.clear(); drag=null;
      images.forEach(entry => { if (entry.image) { entry.image.onload=null; entry.image.onerror=null; } });
      images.clear();
    }
    function load(retryFailedPublish) {
      // Retry only at an explicit open/refresh/tap boundary, never in a failure loop.
      if (retryFailedPublish && retryPublish && !writing && !pendingPublish) {
        pendingPublish=retryPublish; retryPublish=null; flushPublish();
      }
      invalidate(); const ticket=generation;
      rows=[]; self={ nickname:'',avatarUrl:'',firstWins:localMax,rank:null }; scroll=0; status='loading'; draw();
      let friendResult=null, ownDone=false, profileDone=false, friendDone=false, friendError=null, ownError=null;
      function complete() {
        if (!visible || ticket !== generation || !friendDone || !ownDone || !profileDone) return;
        if (friendError) { status=errorState(friendError); draw(); return; }
        rows=rankRows(friendResult);
        const matches=canonicalMarker ? rows.filter(r=>r.ownerMarker === canonicalMarker) : [];
        if (matches.length === 1) { matches[0].isSelf=true; self.rank=matches[0].rank; }
        const others=rows.filter(r=>!r.isSelf);
        status=rows.length === 0 ? 'no-score' : others.length === 0 ? 'self-only' : 'ready';
        // A failed own-score lookup is never represented as a zero score.
        if (ownError && self.firstWins === null && rows.length === 0) status=errorState(ownError);
        draw();
      }
      call('getFriendCloudStorage',{keyList:[SCORE_KEY]},value=>{
        if (!value || !Array.isArray(value.data)) friendError={errMsg:'invalid response'};
        else friendResult=value.data;
        friendDone=true; complete();
      },error=>{friendError=error;friendDone=true;complete();},ticket);
      call('getUserCloudStorage',{keyList:[SCORE_KEY]},value=>{
        const item=stored(value && value.KVDataList), own=item && score(item.value);
        if (!value || !Array.isArray(value.KVDataList) || (item && !own)) ownError={errMsg:'invalid own score'};
        else if (own) {
          remoteKnown=Math.max(remoteKnown || 0,own.firstWins); canonicalMarker=own.ownerMarker;
          self.firstWins=Math.max(localMax || 0,remoteKnown);
        }
        ownDone=true;complete();
      },error=>{ownError=error;ownDone=true;complete();},ticket);
      call('getUserInfo',{openIdList:['self']},value=>{
        const user=value && Array.isArray(value.data) && value.data[0];
        if (object(user)) { self.nickname=typeof user.nickName === 'string' ? user.nickName : typeof user.nickname === 'string' ? user.nickname : ''; self.avatarUrl=typeof user.avatarUrl === 'string' ? user.avatarUrl : ''; }
        profileDone=true;complete();
      },()=>{profileDone=true;complete();},ticket);
    }
    function publish(payload, requestEpoch) {
      localMax=Math.max(localMax || 0,payload.firstWins);
      retryPublish=null;
      pendingPublish={firstWins:localMax,ownerMarker:payload.ownerMarker,updatedAtUtc:payload.updatedAtUtc,epoch:requestEpoch};
      flushPublish();
    }
    function flushPublish() {
      if (writing || !pendingPublish) return;
      const job=pendingPublish; pendingPublish=null; writing=true;
      const finish=changed=>{
        writing=false;
        if (!changed && !pendingPublish) retryPublish=job;
        // Scores belong to the account, not the display epoch. Refresh whichever
        // view is currently open, including one opened while this write was in flight.
        if (changed && visible) load();
        if (pendingPublish) flushPublish();
      };
      // Read before EVERY write. A failed read cannot safely establish a remote maximum.
      call('getUserCloudStorage',{keyList:[SCORE_KEY]},value=>{
        if (!value || !Array.isArray(value.KVDataList)) { finish(false);return; }
        const item=stored(value.KVDataList), remote=item && score(item.value);
        if (item && !remote) { finish(false);return; }
        if (remote) { canonicalMarker=remote.ownerMarker; remoteKnown=Math.max(remoteKnown || 0,remote.firstWins); }
        const count=Math.max(job.firstWins,localMax || 0,remoteKnown || 0);
        if (remote && remote.firstWins >= count && remote.ownerMarker) { finish(true);return; }
        const write={schema:1,firstWins:count,updatedAtUtc:remote && remote.updatedAtUtc > job.updatedAtUtc ? remote.updatedAtUtc : job.updatedAtUtc,ownerMarker:remote && remote.ownerMarker || canonicalMarker || job.ownerMarker};
        call('setUserCloudStorage',{KVDataList:[{key:SCORE_KEY,value:JSON.stringify(write)}]},()=>{
          canonicalMarker=write.ownerMarker; remoteKnown=Math.max(remoteKnown || 0,count); finish(true);
        },()=>finish(false),null);
      },()=>finish(false),null);
    }
    function receive(raw) {
      const message=parseMessage(raw);
      if (!message || message.requestId <= lastRequest || message.viewEpoch < epoch) return false;
      if (message.type === 'open') {
        if (message.viewEpoch <= epoch) return false;
        epoch=message.viewEpoch; lastRequest=message.requestId; bounds=message.payload;visible=true;load(true);return true;
      }
      if (message.viewEpoch !== epoch) return false;
      if (message.type === 'publishScore') { lastRequest=message.requestId;publish(message.payload,epoch);return true; }
      if (!visible) return false;
      lastRequest=message.requestId;
      if (message.type === 'close') { visible=false;invalidate();status='closed';rows=[];ctx.clearRect(0,0,canvas.width,canvas.height);return true; }
      bounds=message.payload;load(true);return true;
    }
    function text(value,x,y,maxWidth) {
      const clean=String(value).replace(/[\x00-\x1f\x7f]/g,'');
      let glyphs=Array.from(clean.slice(0,512)), candidate=glyphs.join('');
      if (ctx.measureText(candidate).width > maxWidth) {
        while (glyphs.length && ctx.measureText(glyphs.join('')+'…').width > maxWidth) glyphs.pop();
        candidate=glyphs.join('')+'…';
      }
      ctx.fillText(candidate,x,y);
    }
    function avatar(url,x,y,size) {
      let entry=images.get(url);
      if (url && !entry && images.size < 128) {
        entry={image:null,ready:false};images.set(url,entry);const ticket=generation;
        try {
          entry.image=api.createImage();
          entry.image.onload=()=>{ if (visible && ticket === generation) {entry.ready=true;draw();} };
          entry.image.onerror=()=>{ entry.ready=false; };
          entry.image.src=url;
        } catch (_) { entry.ready=false; }
      }
      if (entry && entry.ready) { try {ctx.drawImage(entry.image,x,y,size,size);return;} catch (_) {} }
      // Neutral missing-avatar tile; never substitutes a fabricated face.
      ctx.fillStyle='#d9b88c';ctx.fillRect(x,y,size,size);
    }
    function drawRow(row,y,width,own) {
      ctx.fillStyle=own?'#f4dfbd':'#fff5e5';ctx.fillRect(0,y,width,54);
      ctx.fillStyle='#513320';ctx.font='14px sans-serif';
      text(row.rank === null || row.rank === undefined ? '—' : row.rank,10,y+32,30);
      avatar(row.avatarUrl,45,y+9,36);
      ctx.fillStyle='#513320';text(row.nickname || (own?'我':'昵称未提供'),91,y+24,Math.max(12,width-170));
      if (own || row.isSelf) {ctx.font='11px sans-serif';text('我',91,y+43,30);}
      ctx.font='14px sans-serif';text(row.firstWins === null ? '暂无成绩' : row.firstWins,width-73,y+32,70);
    }
    function draw() {
      if (!visible || !bounds) return;
      canvas.width=bounds.width;canvas.height=bounds.height;
      // Technical content renderer only; final visual skin remains owned by Visual.
      const scale=bounds.width/360,w=360,h=bounds.height/scale;
      ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,w,h);
      ctx.fillStyle='#fff5e5';ctx.fillRect(0,0,w,h);ctx.fillStyle='#513320';ctx.font='18px sans-serif';
      text('好友榜',14,29,210);ctx.font='12px sans-serif';text('累计每日首通',235,29,120);
      const messages={loading:'正在加载好友成绩',permission:'微信朋友信息暂不可用',network:'网络暂不可用，点击重试','privacy-declaration':'开发者尚未完成微信隐私声明','no-score':'暂无好友成绩','self-only':'当前只有你的成绩'};
      if (status !== 'ready') {ctx.font='14px sans-serif';text(messages[status] || '',14,75,332);}
      if (status === 'permission' || status === 'network' || status === 'privacy-declaration') text('点击此处重试',14,109,332);
      const top=status === 'ready'?46:124, bottom=Math.max(top,h-104);
      ctx.save();ctx.beginPath();ctx.rect(0,top,w,bottom-top);ctx.clip();
      scroll=Math.max(0,Math.min(scroll,Math.max(0,rows.length*54-(bottom-top))));
      rows.forEach((row,index)=>{const y=top+index*54-scroll;if(y+54>=top&&y<bottom)drawRow(row,y,w,false);});ctx.restore();
      if (status !== 'loading') drawRow(self,Math.max(top,h-99),w,true);
      if (status === 'self-only' || status === 'no-score') {ctx.fillStyle='#8b7058';ctx.font='12px sans-serif';text('通过微信右上角邀请好友',14,h-16,332);}
    }
    function point(event) {
      const t=event && event.touches && event.touches[0];
      if (!visible || !bounds || !t) return null;
      const x=t.clientX*bounds.dpr-bounds.x,y=t.clientY*bounds.dpr-bounds.y;
      return x>=0&&y>=0&&x<bounds.width&&y<bounds.height ? {x:x*360/bounds.width,y:y*360/bounds.width} : null;
    }
    api.onMessage(receive);
    if (typeof api.onTouchStart === 'function') api.onTouchStart(event=>{const p=point(event);if(!p)return;if((status==='network'||status==='permission'||status==='privacy-declaration')&&p.y>=45&&p.y<=124){load(true);return;}drag=p;});
    if (typeof api.onTouchMove === 'function') api.onTouchMove(event=>{const p=point(event);if(p&&drag){scroll+=drag.y-p.y;drag=p;draw();}});
    if (typeof api.onTouchEnd === 'function') api.onTouchEnd(()=>{drag=null;});
    // Diagnostics are module-local and never posted back to the main domain.
    return {receive, snapshot:()=>({epoch,visible,status,rows:rows.map(r=>Object.assign({},r)),self:Object.assign({},self),writing,pendingTimers:pending.size})};
  }
  if (typeof module !== 'undefined' && module.exports) module.exports={create,parseMessage,score,rankRows,SCORE_KEY};
  if (root && typeof root.onMessage === 'function') create(root);
})(typeof wx !== 'undefined' ? wx : null);
