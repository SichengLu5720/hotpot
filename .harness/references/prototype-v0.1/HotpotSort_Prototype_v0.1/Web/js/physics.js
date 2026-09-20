/* Small fixed-step circle packing simulation. Deliberately not a Unity/Box2D clone. */
(function(root,factory){if(typeof module==='object'&&module.exports)module.exports=factory(require('./core'));else root.HotpotPhysics=factory(root.HotpotCore);})(typeof globalThis!=='undefined'?globalThis:this,function(Core){
'use strict';
function offsets(n,r){
 const xy=n===1?[[0,0]]:n===2?[[-.46,0],[.46,0]]:n===3?[[0,-.46],[-.43,.30],[.43,.30]]:n===4?[[-.38,-.38],[.38,-.38],[-.38,.38],[.38,.38]]:[[0,0],[-.49,-.40],[.49,-.40],[-.49,.40],[.49,.40]];
 return xy.map(v=>({x:v[0]*r,y:v[1]*r}));
}
class World {
 constructor(rules,seed){this.rules=rules;this.rng=new Core.Rng((seed^0x9e3779b9)>>>0);this.bodies=[];this.timer=0;this.spawned=0;this.blocked=false;this.steps=0;this.lastMotion=0;}
 sync(game){const ids=new Set(game.active.map(p=>p.id));this.bodies=this.bodies.filter(b=>ids.has(b.id));}
 proposed(game){if(!game.pending.length)return null;const p=game.pending[0],r=this.rules.plateRadii[p.capacity],anchors=[86,334,210];return {id:p.id,r,x:anchors[this.spawned%3],y:this.rules.playTop+r+2};}
 canSpawn(game){
   const c=this.proposed(game);if(!c||game.status!=='playing')return false;
   if(this.bodies.some(b=>b.y<this.rules.spawnGate))return false;
   return !this.bodies.some(b=>Math.hypot(b.x-c.x,b.y-c.y)<b.r+c.r+3);
 }
 spawn(game){
   if(!this.canSpawn(game))return false;
   const c=this.proposed(game),p=game.spawn();if(!p)return false;
   // Jitter is applied only when safe; no off-screen or overlapping insertion.
   const jitter=(this.rng.next()-.5)*10;
   if(!this.bodies.some(b=>Math.hypot(b.x-c.x-jitter,b.y-c.y)<b.r+c.r+2))c.x+=jitter;
   this.bodies.push(Object.assign(c,{vx:(this.rng.next()-.5)*8,vy:24}));this.spawned++;return true;
 }
 step(game,dt){
   if(game.status!=='playing')return;dt=Math.max(0,Math.min(dt,1/30));this.steps++;this.sync(game);
   this.timer+=dt;if(this.timer>=this.rules.spawnInterval){this.blocked=!this.canSpawn(game);if(!this.blocked&&this.spawn(game))this.timer=0;else this.timer=this.rules.spawnInterval;}
   const bs=this.bodies,R=this.rules;
   for(const b of bs){b.vy+=R.gravity*dt;b.vx*=Math.pow(.994,dt*120);b.x+=b.vx*dt;b.y+=b.vy*dt;}
   for(let iteration=0;iteration<9;iteration++){
     for(const b of bs){
       if(b.x-b.r<12){b.x=12+b.r;b.vx=Math.abs(b.vx)*.04;}
       if(b.x+b.r>R.width-12){b.x=R.width-12-b.r;b.vx=-Math.abs(b.vx)*.04;}
       if(b.y+b.r>R.playBottom){b.y=R.playBottom-b.r;if(b.vy>0)b.vy*=-.015;b.vx*=.90;}
       if(b.y-b.r<R.playTop){b.y=R.playTop+b.r;if(b.vy<0)b.vy=0;}
     }
     for(let i=0;i<bs.length;i++)for(let j=i+1;j<bs.length;j++){
       const a=bs[i],b=bs[j];let dx=b.x-a.x,dy=b.y-a.y,dist=Math.hypot(dx,dy),limit=a.r+b.r+1;
       if(dist>=limit)continue;if(dist<.0001){dx=.0001;dy=0;dist=.0001;}
       const nx=dx/dist,ny=dy/dist,penetration=limit-dist;
       const invA=1/(a.r*a.r),invB=1/(b.r*b.r),ratioA=invA/(invA+invB),ratioB=1-ratioA;
       a.x-=nx*penetration*ratioA;a.y-=ny*penetration*ratioA;b.x+=nx*penetration*ratioB;b.y+=ny*penetration*ratioB;
       const vn=(b.vx-a.vx)*nx+(b.vy-a.vy)*ny;
       if(vn<0){const impulse=-vn*1.02;a.vx-=nx*impulse*ratioA;a.vy-=ny*impulse*ratioA;b.vx+=nx*impulse*ratioB;b.vy+=ny*impulse*ratioB;}
       const tangent=(b.vx-a.vx)*(-ny)+(b.vy-a.vy)*nx;
       a.vx+=(-ny)*tangent*.03;a.vy+=nx*tangent*.03;b.vx-=(-ny)*tangent*.03;b.vy-=nx*tangent*.03;
     }
   }
   this.lastMotion=bs.reduce((s,b)=>Math.max(s,Math.hypot(b.vx,b.vy)),0);
 }
 position(plate,token){const b=this.bodies.find(b=>b.id===plate.id);if(!b)return null;const o=offsets(plate.capacity,b.r)[token.slot];return {x:b.x+o.x,y:b.y+o.y,r:plate.capacity===5?15.5:17};}
 hit(game,x,y){
   if(y<this.rules.playTop||y>this.rules.playBottom)return null;
   // Reverse drawing order: only the topmost containing plate can be picked.
   for(let i=this.bodies.length-1;i>=0;i--){const b=this.bodies[i];if(Math.hypot(x-b.x,y-b.y)>b.r)continue;
     const p=game.active.find(p=>p.id===b.id);if(!p)continue;
     for(let j=p.items.length-1;j>=0;j--){const t=p.items[j],q=this.position(p,t);if(Math.hypot(x-q.x,y-q.y)<=q.r+3)return {plate:p,item:t,point:q};}
     return null;
   }
   return null;
 }
 audit(game){
  this.sync(game);for(const b of this.bodies)if(![b.x,b.y,b.vx,b.vy].every(Number.isFinite))throw Error('Non-finite physics state');
  if(this.bodies.length!==game.active.length)throw Error('Physics/model count mismatch');return true;
 }
}
return {World,offsets};
});
