/* Hotpot Sort - pure deterministic domain model. No DOM, physics, or network. */
(function(root,factory){const x=factory();if(typeof module==='object'&&module.exports)module.exports=x;else root.HotpotCore=x;})(typeof globalThis!=='undefined'?globalThis:this,function(){
'use strict';
const KIND_COUNT=16;
class Rng {
  constructor(seed){this.state=(Number(seed)>>>0)||1;}
  next(){let x=this.state;x^=x<<13;x^=x>>>17;x^=x<<5;this.state=x>>>0;return this.state/4294967296;}
  int(n){if(!Number.isInteger(n)||n<=0)throw Error('Invalid random range');return Math.floor(this.next()*n);}
}
const counts=items=>{const c=Array(KIND_COUNT).fill(0);for(const t of items)c[t.kind]++;return c;};
const sum=a=>a.reduce((s,v)=>s+v,0);
class Game {
  constructor(data,levelId='A',seed=7,overrides={}){
    if(!data||data.schemaVersion!==1)throw Error('Unsupported data schema');
    this.data=data;this.rules=Object.assign({},data.rules,overrides);
    if(this.rules.bufferCapacity<1||this.rules.orderSize!==3||this.rules.openOrderSlots<1||this.rules.openOrderSlots>this.rules.orderSlots)throw Error('Invalid rules');
    this.level=data.levels.find(x=>x.id===levelId);if(!this.level)throw Error('Unknown level: '+levelId);
    this.seed=(Number(seed)>>>0)||1;this.rng=new Rng(this.seed);
    this.status='playing';this.failureReason='';this.moves=0;this.completed=0;this.sequence=0;
    this.log=[];this.commands=[];this.events=[];this.completedByKind=Array(KIND_COUNT).fill(0);
    let next=1;
    this.pending=this.level.plates.map((str,index)=>{
      if(typeof str!=='string'||str.length<1||str.length>5||!/^[A-P]+$/.test(str))throw Error('Bad plate '+index);
      const items=[...str].map((ch,slot)=>({id:next++,kind:ch.charCodeAt(0)-65,slot}));
      return {id:index+1,capacity:items.length,items};
    });
    this.active=[];this.buffer=Array(this.rules.bufferCapacity).fill(null);
    this.orders=Array.from({length:this.rules.orderSlots},(_,i)=>({slot:i,open:i<this.rules.openOrderSlots,kind:-1,items:[]}));
    this.initialByKind=counts(this.pending.flatMap(p=>p.items));this.total=sum(this.initialByKind);
    if(!this.total||this.initialByKind.some(x=>x%3!==0))throw Error('Each ingredient count must be a multiple of 3');
    this.emit('start',{level:levelId,seed:this.seed,total:this.total,failOnFull:this.rules.failOnFull});
    for(const o of this.orders)if(o.open)this.assign(o,true);
    this.audit();
  }
  emit(type,payload={}){const e=Object.assign({seq:this.sequence++,type},payload);this.log.push(e);this.events.push(e);return e;}
  drainEvents(){const e=this.events;this.events=[];return e;}
  allLoose(){return this.pending.concat(this.active).flatMap(p=>p.items).concat(this.buffer.filter(Boolean));}
  demand(except=-1){const c=Array(KIND_COUNT).fill(0);for(const o of this.orders)if(o.open&&o.kind>=0&&o.slot!==except)c[o.kind]+=this.rules.orderSize-o.items.length;return c;}
  remainingAvailable(except=-1){const c=counts(this.allLoose()),d=this.demand(except);return c.map((v,i)=>v-d[i]);}
  pickOpening(slot){
    const d=this.demand(slot),other=new Set(this.orders.filter(o=>o.open&&o.kind>=0&&o.slot!==slot).map(o=>o.kind));
    const available=this.remainingAvailable(slot);
    let c=counts(this.pending.slice(0,this.rules.openingLookahead).flatMap(p=>p.items));
    c=c.map((v,i)=>v-d[i]);
    let candidates=c.map((v,kind)=>({kind,count:v})).filter(x=>x.count>0&&available[x.kind]>=3);
    if(!candidates.length){c=counts(this.allLoose()).map((v,i)=>v-d[i]);candidates=c.map((v,kind)=>({kind,count:v})).filter(x=>x.count>0&&available[x.kind]>=3);}
    const distinct=candidates.filter(x=>!other.has(x.kind));if(distinct.length)candidates=distinct;
    // Explicit prototype tie-break: lower abstract ID. Not native Dictionary ordering.
    candidates.sort((a,b)=>b.count-a.count||a.kind-b.kind);
    return {kind:candidates.length?candidates[0].kind:-1,reason:'opening-prefix-max',candidates};
  }
  selectWeightRow(){
    const progress=(this.completed+sum(this.orders.map(o=>o.items.length)))/this.total;
    const temp=Math.min(4,this.buffer.filter(Boolean).length);
    const rows=this.data.difficultyRows.filter(r=>r.difficulty===this.level.difficulty&&r.temp===temp).sort((a,b)=>a.progress-b.progress);
    return rows.find(r=>progress<=r.progress+1e-8)||rows[rows.length-1]||{weights:[0,1,0,0,0],progress:1,temp};
  }
  // This is deliberately a documented approximation, not the recovered native cost.
  candidateCosts(slot){
    const d=this.demand(slot),available=this.remainingAvailable(slot),b=counts(this.buffer.filter(Boolean)),a=counts(this.active.flatMap(p=>p.items));
    const free=this.buffer.filter(t=>!t).length,candidates=[];
    for(let kind=0;kind<KIND_COUNT;kind++){
      if(available[kind]<3)continue;
      let reserve=d[kind],usableB=Math.max(0,b[kind]-reserve);reserve=Math.max(0,reserve-b[kind]);
      const usableA=Math.max(0,a[kind]-reserve);reserve=Math.max(0,reserve-a[kind]);
      let cost;
      if(usableB+usableA>=3)cost=-Math.min(3,usableB);
      else{
        let need=3-usableB-usableA,foreign=0;
        outer:for(const p of this.pending)for(const t of p.items){
          if(t.kind===kind){if(reserve>0)reserve--;else need--;if(need===0)break outer;}
          else foreign++;
        }
        cost=Math.max(1,Math.min(free+1,foreign));
      }
      // Preserve the category predicates; with very little free space they overlap.
      const groups=[cost<0,cost===0,cost>=1&&cost<free-1,cost===free-1,cost===free||cost===free+1];
      candidates.push({kind,cost,groups,buffer:usableB,active:usableA});
    }
    const other=new Set(this.orders.filter(o=>o.open&&o.kind>=0&&o.slot!==slot).map(o=>o.kind));
    const distinct=candidates.filter(x=>!other.has(x.kind));return distinct.length?distinct:candidates;
  }
  pickRefill(slot){
    const candidates=this.candidateCosts(slot);if(!candidates.length)return {kind:-1,reason:'no-unreserved-triple'};
    const row=this.selectWeightRow(),weights=row.weights;const total=sum(weights);
    let roll=this.rng.next()*total,group=weights.length-1;
    for(let i=0;i<weights.length;i++){roll-=weights[i];if(roll<0){group=i;break;}}
    let pool=candidates.filter(c=>c.groups[group]);const fallback=pool.length===0;
    if(fallback){const low=Math.min(...candidates.map(x=>x.cost));pool=candidates.filter(x=>x.cost===low);}
    pool.sort((a,b)=>a.kind-b.kind);
    const selected=pool[this.rng.int(pool.length)];
    return {kind:selected.kind,reason:fallback?'category-empty-min-cost':'weighted-category',requestedGroup:group+1,cost:selected.cost,threshold:row.progress,temp:row.temp,candidates};
  }
  assign(order,opening=false){
    const pick=opening?this.pickOpening(order.slot):this.pickRefill(order.slot);order.kind=pick.kind;
    this.emit('target',Object.assign({slot:order.slot},pick));
  }
  spawn(){
    if(this.status!=='playing'||!this.pending.length)return null;
    const p=this.pending.shift();this.active.push(p);this.commands.push({type:'spawn'});
    this.emit('spawn',{plate:p.id});this.audit();return p;
  }
  findTarget(kind){
    return this.orders.filter(o=>o.open&&o.kind===kind&&o.items.length<this.rules.orderSize)
      .sort((a,b)=>b.items.length-a.items.length||a.slot-b.slot)[0]||null;
  }
  tap(id){
    if(this.status!=='playing')return {accepted:false,reason:'not-playing'};
    const plate=this.active.find(p=>p.items.some(t=>t.id===id));if(!plate)return {accepted:false,reason:'not-active'};
    const item=plate.items.find(t=>t.id===id),target=this.findTarget(item.kind),empty=this.buffer.findIndex(t=>!t);
    if(!target&&empty<0){
      if(!this.rules.failOnFull){this.commands.push({type:'tap',id});this.status='lost';this.failureReason='buffer-overflow-attempt';this.emit('lost',{reason:this.failureReason});}
      return {accepted:false,reason:'buffer-full'};
    }
    this.commands.push({type:'tap',id});this.moves++;plate.items=plate.items.filter(t=>t.id!==id);
    if(target){target.items.push(item);this.emit('move',{id,kind:item.kind,from:'plate',plate:plate.id,to:'order',slot:target.slot});}
    else{this.buffer[empty]=item;this.emit('move',{id,kind:item.kind,from:'plate',plate:plate.id,to:'buffer',slot:empty});}
    if(!plate.items.length){this.active=this.active.filter(p=>p!==plate);this.emit('plate-empty',{plate:plate.id});}
    this.settle();this.audit();return {accepted:true};
  }
  settle(){
    // A bounded atomic transaction; render animations do not own gameplay tokens.
    let guard=0,changed=true;
    while(changed){
      if(++guard>this.total*4+32)throw Error('Settlement did not converge');changed=false;
      for(const o of this.orders)if(o.open&&o.items.length===this.rules.orderSize){
        const kind=o.kind;this.completed+=o.items.length;this.completedByKind[kind]+=o.items.length;
        this.emit('complete',{slot:o.slot,kind,ids:o.items.map(t=>t.id)});o.items=[];o.kind=-1;this.assign(o);changed=true;
      }
      for(let i=0;i<this.buffer.length;i++){
        const t=this.buffer[i];if(!t)continue;const target=this.findTarget(t.kind);if(!target)continue;
        this.buffer[i]=null;target.items.push(t);this.emit('move',{id:t.id,kind:t.kind,from:'buffer',fromSlot:i,to:'order',slot:target.slot});changed=true;
      }
    }
    if(this.completed===this.total){this.status='won';this.emit('won',{moves:this.moves});}
    else if(this.rules.failOnFull&&this.buffer.every(Boolean)){this.status='lost';this.failureReason='buffer-full-after-settlement';this.emit('lost',{reason:this.failureReason});}
  }
  audit(){
    const loose=this.allLoose(),held=this.orders.flatMap(o=>o.items),live=loose.concat(held),c=counts(live),ids=new Set();
    for(const t of live){if(ids.has(t.id))throw Error('Duplicate token '+t.id);ids.add(t.id);}
    for(let k=0;k<KIND_COUNT;k++)if(c[k]+this.completedByKind[k]!==this.initialByKind[k])throw Error('Conservation failure for kind '+k);
    if(this.buffer.length!==this.rules.bufferCapacity)throw Error('Buffer shape changed');
    if(this.orders.filter(o=>o.open).length!==this.rules.openOrderSlots)throw Error('Slot count changed');
    for(const o of this.orders){if(o.items.length>this.rules.orderSize||o.items.some(t=>t.kind!==o.kind))throw Error('Bad order content');if(!o.open&&(o.kind>=0||o.items.length))throw Error('Locked slot received items');}
    const remaining=counts(loose),demand=this.demand();for(let k=0;k<KIND_COUNT;k++)if(remaining[k]<demand[k])throw Error('Impossible target reservation '+k);
    if(this.status==='won'&&(live.length||this.completed!==this.total))throw Error('Premature victory');
    return true;
  }
  snapshot(){return {level:this.level.id,seed:this.seed,rng:this.rng.state,status:this.status,reason:this.failureReason,moves:this.moves,total:this.total,completed:this.completed,
    pending:this.pending.map(p=>({id:p.id,items:p.items.map(t=>t.id)})),active:this.active.map(p=>({id:p.id,items:p.items.map(t=>t.id)})),
    buffer:this.buffer.map(t=>t?t.id:null),orders:this.orders.map(o=>({slot:o.slot,open:o.open,kind:o.kind,ids:o.items.map(t=>t.id)})),completedByKind:this.completedByKind.slice()};}
  exportReplay(){return {schemaVersion:1,version:'0.1.0',level:this.level.id,seed:this.seed,failOnFull:this.rules.failOnFull,commands:this.commands.slice(),final:this.snapshot(),log:this.log.slice()};}
  static replay(data,record){
    if(!record||record.schemaVersion!==1||!Array.isArray(record.commands)||record.commands.length>100000)throw Error('Invalid replay');
    const g=new Game(data,record.level,record.seed,{failOnFull:record.failOnFull!==false});
    for(const c of record.commands){if(c.type==='spawn'){if(!g.spawn())throw Error('Invalid replay spawn');}else if(c.type==='tap'){const result=g.tap(c.id);if(!result.accepted&&result.reason!=='buffer-full')throw Error('Invalid replay tap');}else throw Error('Unknown replay command');}
    g.audit();return g;
  }
}
return {Game,Rng,counts,KIND_COUNT};
});
