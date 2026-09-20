'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const ROOT=path.resolve(__dirname,'..'),D=require('../Web/js/data'),{Game,Rng}=require('../Web/js/core'),{World}=require('../Web/js/physics');
let passed=0,failed=0;const results=[],golden=[];
function test(name,fn){try{fn();passed++;results.push({name,passed:true});console.log('PASS',name);}catch(e){failed++;results.push({name,passed:false,error:e.stack});console.error('FAIL',name,e.message);}}
function fixture(plates){return {...D,levels:[{id:'T',name:'Test',difficulty:1,plates}]};}
function spawnAll(g){while(g.pending.length)assert.ok(g.spawn());}
function tapKind(g,kind,n=1){for(let i=0;i<n;i++){const t=g.active.flatMap(p=>p.items).find(t=>t.kind===kind);assert.ok(t,'Missing kind '+kind);assert.ok(g.tap(t.id).accepted);}}
function winAllVisible(g){spawnAll(g);let guard=0;while(g.status==='playing'&&guard++<500){const t=g.active.flatMap(p=>p.items).find(t=>g.findTarget(t.kind));assert.ok(t,'No matching token');g.tap(t.id);}assert.equal(g.status,'won');g.audit();}
function saveGolden(name,g){let expected=JSON.parse(JSON.stringify(g.snapshot()));expected.buffer=expected.buffer.map(x=>x===null?0:x);golden.push({name,level:g.level.id,seed:g.seed,failOnFull:g.rules.failOnFull,commands:g.commands.slice(),expected});}

test('data: A/B/C/P cardinalities and 3-divisible species',()=>{const expects={A:[33,123,15],B:[44,162,15],C:[50,183,16],P:[9,27,3]};for(const id of Object.keys(expects)){const g=new Game(D,id);assert.deepEqual([g.pending.length,g.total,g.initialByKind.filter(Boolean).length],expects[id]);g.audit();}});
test('opening: A→A,F; B→A,G; C→C,H; P→A,B',()=>{for(const [id,types]of Object.entries({A:[0,5],B:[0,6],C:[2,7],P:[0,1]})){const g=new Game(D,id,7);assert.deepEqual(g.orders.filter(o=>o.open).map(o=>o.kind),types);saveGolden('opening-'+id,g);}});
test('invalid schema/unknown level/invalid plate/unbalanced counts rejected',()=>{assert.throws(()=>new Game({...D,schemaVersion:2}));assert.throws(()=>new Game(D,'Q'));assert.throws(()=>new Game(fixture(['AAX']),'T'));assert.throws(()=>new Game(fixture(['AA']),'T'));assert.throws(()=>new Game(fixture(['AAAAAA']),'T'));});
test('capacity: exactly 5 buffer, 2 active targets, 4 target positions',()=>{const g=new Game(D);assert.equal(g.buffer.length,5);assert.equal(g.orders.length,4);assert.equal(g.orders.filter(o=>o.open).length,2);});
test('spawn FIFO and no entire-board instantiation in constructor',()=>{const g=new Game(D);assert.equal(g.active.length,0);assert.equal(g.spawn().id,1);assert.equal(g.spawn().id,2);assert.equal(g.pending[0].id,3);});
test('input: unspawned token rejected without state mutation',()=>{const g=new Game(D),s=g.snapshot();assert.equal(g.tap(1).accepted,false);assert.deepEqual(g.snapshot(),s);});
test('matching token enters order rather than buffer',()=>{const g=new Game(D);g.spawn();assert.ok(g.tap(1).accepted);assert.equal(g.orders[0].items[0].id,1);assert.equal(g.buffer.filter(Boolean).length,0);saveGolden('first-matching-A',g);});
test('unmatched token enters first free buffer cell',()=>{const g=new Game(D);g.spawn();const t=g.active[0].items.find(t=>t.kind===1);g.tap(t.id);assert.equal(g.buffer[0].id,t.id);saveGolden('first-buffer-A',g);});
test('duplicate click cannot duplicate a token',()=>{const g=new Game(D);g.spawn();g.tap(1);const s=g.snapshot();assert.equal(g.tap(1).accepted,false);assert.deepEqual(g.snapshot(),s);});
test('plate item slots remain stable after removal',()=>{const g=new Game(D);g.spawn();g.tap(1);assert.deepEqual(g.active[0].items.map(t=>t.slot),[1,2,3,4]);});
test('empty plate recycled independently',()=>{const g=new Game(fixture(['AAA','BBB']),'T');spawnAll(g);tapKind(g,0,3);assert.equal(g.active.some(p=>p.id===1),false);assert.equal(g.active.some(p=>p.id===2),true);});
test('closest-to-full target wins even when not first slot',()=>{const g=new Game(fixture(['AAA','AAA','BBB']),'T');spawnAll(g);g.orders[0].kind=0;g.orders[1].kind=0;const p=g.active[0];g.orders[1].items.push(...p.items.splice(0,2));g.audit();const t=p.items[0];g.drainEvents();g.tap(t.id);assert.equal(g.drainEvents().find(e=>e.type==='move').slot,1);});
test('refill automatically absorbs matching buffered item',()=>{const g=new Game(fixture(['AAA','BBB','CCC']),'T');spawnAll(g);tapKind(g,2);tapKind(g,0,3);assert.equal(g.buffer.filter(Boolean).length,0);assert.ok(g.orders.some(o=>o.kind===2&&o.items.length===1));g.audit();});
test('buffered triple can cause multi-order cascade',()=>{const g=new Game(fixture(['AAA','BBB','CCC']),'T');spawnAll(g);tapKind(g,2,3);tapKind(g,0,3);assert.equal(g.completed,6);assert.equal(g.buffer.filter(Boolean).length,0);tapKind(g,1,3);assert.equal(g.status,'won');});
test('default fifth buffer token causes failure after settlement',()=>{const g=new Game(fixture(['AAA','BBB','CCC','DDD','EEE']),'T');spawnAll(g);tapKind(g,2,3);tapKind(g,3,2);assert.equal(g.status,'lost');assert.equal(g.buffer.filter(Boolean).length,5);assert.equal(g.failureReason,'buffer-full-after-settlement');g.audit();});
test('alternative overflow policy keeps fifth token; sixth attempt loses without removing it',()=>{const g=new Game(fixture(['AAA','BBB','CCC','DDD','EEE']),'T',7,{failOnFull:false});spawnAll(g);tapKind(g,2,3);tapKind(g,3,2);assert.equal(g.status,'playing');const t=g.active.flatMap(p=>p.items).find(t=>t.kind===3);const before=g.active.flatMap(p=>p.items).length;assert.equal(g.tap(t.id).accepted,false);assert.equal(g.status,'lost');assert.equal(g.active.flatMap(p=>p.items).length,before);g.audit();});
test('winning is not triggered while pending supply remains',()=>{const g=new Game(fixture(['AAA','BBB']),'T');g.spawn();tapKind(g,0,3);assert.equal(g.status,'playing');assert.equal(g.pending.length,1);});
test('one-species tail closes unused order rather than creating impossible demand',()=>{const g=new Game(fixture(['AAA']),'T');assert.equal(g.orders[1].kind,-1);winAllVisible(g);assert.ok(g.orders.filter(o=>o.open).every(o=>o.kind===-1));});
test('xorshift32 known vector / separate seed repeatability',()=>{const r=new Rng(7);const a=Array.from({length:5},()=>{r.next();return r.state;});assert.deepEqual(a,[1892583,470389255,3882205507,3069989445,2854842367]);assert.equal(new Rng(0).state,1);});
test('difficulty: 60 normalized rows; explicit early temp weights',()=>{assert.equal(D.difficultyRows.length,60);for(const row of D.difficultyRows)assert.ok(Math.abs(row.weights.reduce((a,b)=>a+b,0)-1)<1e-6);assert.deepEqual(D.difficultyRows.find(r=>r.difficulty===1&&r.progress===.4&&r.temp===4).weights,[.9,.1,0,0,0]);});
test('all-visible logical completion across 80 level/seed cases',()=>{for(const id of ['A','B','C','P'])for(let seed=1;seed<=20;seed++){const g=new Game(D,id,seed);winAllVisible(g);assert.equal(g.moves,g.total);if(seed===7)saveGolden('logical-win-'+id,g);}});
test('replay exactly reproduces logical state / invalid version rejected',()=>{const g=new Game(D,'B',19);spawnAll(g);for(let i=0;i<35;i++){const t=g.active.flatMap(p=>p.items).find(t=>g.findTarget(t.kind));g.tap(t.id);}assert.deepEqual(Game.replay(D,g.exportReplay()).snapshot(),g.snapshot());saveGolden('partial-B-35',g);assert.throws(()=>Game.replay(D,{schemaVersion:99,commands:[]}));});
test('fuzz: conservation and valid commitments across 180 seeded games',()=>{for(const id of ['A','B','C'])for(let seed=1;seed<=60;seed++){const g=new Game(D,id,seed),rng=new Rng(seed*41);spawnAll(g);for(let n=0;n<250&&g.status==='playing';n++){const items=g.active.flatMap(p=>p.items),matches=items.filter(t=>g.findTarget(t.kind));const pool=rng.next()<.85&&matches.length?matches:items;if(!pool.length)break;g.tap(pool[rng.int(pool.length)].id);g.audit();}const replay=Game.replay(D,g.exportReplay());assert.deepEqual(replay.snapshot(),g.snapshot());}});
test('logs include candidate, chosen category and fallback reason',()=>{const g=new Game(D);spawnAll(g);tapKind(g,0,3);const e=g.log.filter(e=>e.type==='target').at(-1);assert.ok(e.candidates.length);assert.ok(e.requestedGroup>=1&&e.requestedGroup<=5);assert.ok(e.reason);});
test('physics: finite fixed-step states and bounded initial supply',()=>{for(const id of ['A','B','C']){const g=new Game(D,id),w=new World(D.rules,7);for(let i=0;i<1800;i++)w.step(g,1/120);w.audit(g);g.audit();assert.ok(g.active.length>1&&g.active.length<g.level.plates.length);}});
test('height gate blocks supply; scene does not enforce a hard count limit',()=>{const g=new Game(D),w=new World(D.rules,7);assert.ok(w.spawn(g));const n=g.pending.length;assert.equal(w.canSpawn(g),false);assert.equal(w.spawn(g),false);assert.equal(g.pending.length,n);w.bodies[0].y=800;assert.equal(w.canSpawn(g),true);});
test('hit-test: token position, outside board, and removed token',()=>{const g=new Game(D),w=new World(D.rules,7);w.spawn(g);const p=g.active[0],t=p.items[0],q=w.position(p,t);assert.equal(w.hit(g,q.x,q.y).item.id,t.id);assert.equal(w.hit(g,210,180),null);g.tap(t.id);assert.notEqual(w.hit(g,q.x,q.y)?.item.id,t.id);});
test('physics does not continue after terminal state',()=>{const g=new Game(D),w=new World(D.rules,7);w.spawn(g);g.status='lost';const before=JSON.stringify(w.bodies);w.step(g,1);assert.equal(JSON.stringify(w.bodies),before);});
const physicalRuns=[];
test('physical greedy smoke: normal gated supply and actual hit tests, 12 runs',()=>{for(const id of ['A','B','C','P'])for(const seed of [7,13,19]){
 const g=new Game(D,id,seed),w=new World(D.rules,seed);let maxBuffer=0,bufferClicks=0;
 for(let i=0;i<480;i++)w.step(g,1/120);
 for(let turn=0;turn<300&&g.status==='playing';turn++){
  for(let s=0;s<160;s++)w.step(g,1/120);
  const ps=g.active.flatMap(p=>p.items.map(t=>({p,t,q:w.position(p,t)}))).filter(o=>w.hit(g,o.q.x,o.q.y)?.item.id===o.t.id);
  if(!ps.length)break;
  let pick=ps.filter(o=>g.findTarget(o.t.kind)).sort((a,b)=>a.p.items.length-b.p.items.length||a.p.id-b.p.id)[0];
  if(!pick){bufferClicks++;const bc=Array(16).fill(0);g.buffer.filter(Boolean).forEach(t=>bc[t.kind]++);pick=ps.sort((a,b)=>a.p.items.length-b.p.items.length||bc[b.t.kind]-bc[a.t.kind]||a.p.id-b.p.id)[0];}
  g.tap(pick.t.id);maxBuffer=Math.max(maxBuffer,g.buffer.filter(Boolean).length);
 }
 g.audit();w.sync(g);w.audit(g);physicalRuns.push({level:id,seed,status:g.status,completed:g.completed,total:g.total,bufferClicks,maxBuffer});assert.equal(g.status,'won',id+' '+seed);
}});
fs.mkdirSync(path.join(ROOT,'qa'),{recursive:true});
fs.writeFileSync(path.join(ROOT,'qa/core-tests.json'),JSON.stringify({runtime:process.version,passed,failed,results,physicalRuns,scope:'JavaScript model and custom circle simulation only. Does not execute Unity/C# and is not a solvability proof.'},null,2));
fs.writeFileSync(path.join(ROOT,'Unity/Assets/HotpotSort/Resources/Hotpot/golden-cases.json'),JSON.stringify({cases:golden},null,2));
console.log(`\n${passed} passed, ${failed} failed. ${golden.length} Unity golden fixtures generated (not run in Unity here).`);
if(failed)process.exitCode=1;
