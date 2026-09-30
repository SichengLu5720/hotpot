using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HotpotSort.Core;
using HotpotSort.Determinism;
static class Task057NativeQa
{
 static int checks;static void Check(bool yes,string name){checks++;if(!yes)throw new Exception(name);}
 static CoreItem I(int id,string k,string loc,int p=1)=>new CoreItem{Id=id,Kind=k,Location=loc,PlateId=p,SourceIndex=id};
 static void Main(string[] args)
 {
  var content=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0])).Content;var d=new NativeD1Director(content);
  var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true},new CoreOrder(),new CoreOrder()};var buffer=new int?[5];
  var xs=new List<CoreItem>{I(1,"A","Buffer"),I(2,"A","ActiveAvailable"),I(3,"B","ActiveAvailable"),I(4,"B","ActiveAvailable"),I(5,"C","ActiveAvailable",2),I(6,"A","Pending",3)};buffer[0]=1;
  var above=new HashSet<int>{5};var caps=new Dictionary<int,int>{{1,3},{2,1}};
  var c=d.Costs(0,xs,orders,buffer,new[]{1,2},above,caps).Single(x=>x.Kind=="A");Check(c.D==1&&c.Missing==2,"temp -1 ordinary zero missing2");
  orders[1].Kind="B";c=d.Costs(0,xs,orders,buffer,new[]{1,2},above,caps).Single(x=>x.Kind=="A");Check(c.D==-1&&c.Missing==0,"existing demand lowers residue");orders[1].Kind=null;
  xs.RemoveAll(x=>x.Id==3||x.Id==4);c=d.Costs(0,xs,orders,buffer,new[]{1,2},above,caps).Single(x=>x.Kind=="A");Check(c.Missing==0,"original type preserved after depletion");
  c=d.Costs(0,xs,orders,buffer,new[]{1,2},new HashSet<int>(),caps).Single(x=>x.Kind=="A");Check(c.D==4&&c.Missing==5,"no above => fixed missing5");
  orders[1].Kind="A";Check(d.Costs(0,xs,orders,buffer,new[]{1,2},above,caps).All(x=>x.Kind!="A"),"reservation temp then ordinary deletes zero keys");orders[1].Kind=null;
  for(int s=0;s<=5;s++)for(int cost=-3;cost<=15;cost++)for(int cat=0;cat<5;cat++)Check(NativeD1Director.Matches(cat,cost,s)==new[]{cost<0,cost==0,cost>=1&&cost<s-1,cost==s-1,cost==s||cost==s+1}[cat],"category boundary");
  Check(NativeD1Director.Matches(1,0,1)&&NativeD1Director.Matches(3,0,1),"categories overlap S1");
  Check(NativeD1Director.Fallback(1,.4f).SequenceEqual(new[]{1,0})&&NativeD1Director.Fallback(1,.40001f).SequenceEqual(new[]{1,2,3,4}),"fallback progress boundary");
  Check(NativeD1Director.Sample(new[]{20,20,20,20,20},.2f)==1&&NativeD1Director.Sample(new[]{20,20,20,20,20},1f)==0,"float strict less and end fallback");
  var rng=new Pcg32(23);d.ArmPriority(1,0,rng);string state=CanonicalJson.Write(d.StateJson());ulong index=rng.DrawIndex;
  var preview=d.Choose(0,xs,orders,buffer,new[]{3},new[]{1,2},above,rng,.5f,select:false,plateCapacities:caps);Check(index==rng.DrawIndex&&state==CanonicalJson.Write(d.StateJson())&&preview.HasCandidates,"query purity");
  var allowed=new HashSet<string>{"A"};for(int i=0;i<30;i++)Check(d.Choose(0,xs,orders,buffer,new[]{3},new[]{1,2},above,rng,.5f,allowed,true,caps).Kind=="A","allowed pool cannot be escaped");
  var counts=xs.GroupBy(x=>x.Kind).ToDictionary(g=>g.Key,g=>g.Count());var swaps=NativeD1Director.Exchange("A",0,xs,orders,buffer,new[]{3},new[]{1,2},above);Check(swaps.Count==1&&xs.Single(x=>x.Id==5).Kind=="A"&&xs.Single(x=>x.Id==6).Kind=="C","exchange first above/pending");Check(counts.All(kv=>xs.Count(x=>x.Kind==kv.Key)==kv.Value),"exchange conserves inventory");
  orders[1].Kind="C";xs.Single(x=>x.Id==5).Kind="C";xs.Single(x=>x.Id==6).Kind="A";Check(NativeD1Director.Exchange("A",0,xs,orders,buffer,new[]{3},new[]{1,2},above).Count==0,"exchange excludes other orders");
  NativeD1Director priority=null;Pcg32 prng=null;
  for(ulong seed=1;seed<1000;seed++){var trial=new NativeD1Director(content);var tr=new Pcg32(seed);trial.ArmPriority(1,0,tr);var st=CanonicalJson.Map(trial.StateJson());if((bool)st["enabled"]&&CanonicalJson.Int(st["limit"])==2){priority=trial;prng=tr;break;}}
  Check(priority!=null,"deterministic enabled two-quota fixture");
  xs=new List<CoreItem>{I(1,"B","Buffer"),I(2,"B","Buffer"),I(3,"B","Buffer")};buffer=new int?[]{1,2,3,null,null};orders[0].Kind="A";orders[1].Kind="C";
  priority.ObservePriority(xs,orders,buffer,new HashSet<int>(),.5f,prng,2);var ps=CanonicalJson.Map(priority.StateJson());Check((bool)ps["latched"]&&CanonicalJson.Int(ps["used"])==1,"observe consumes quota once");
  ulong pi=prng.DrawIndex;priority.ObservePriority(xs,orders,buffer,new HashSet<int>(),.5f,prng,2);Check(prng.DrawIndex==pi&&CanonicalJson.Int(CanonicalJson.Map(priority.StateJson())["used"])==1,"latch prevents repeated quota");
  priority.Choose(0,xs,orders,buffer,Array.Empty<int>(),Array.Empty<int>(),new HashSet<int>(),prng,.5f);ps=CanonicalJson.Map(priority.StateJson());Check(!(bool)ps["latched"]&&CanonicalJson.Int(ps["used"])==1,"selection resets latch without consuming quota");
  string oldState=CanonicalJson.Write(priority.StateJson());pi=prng.DrawIndex;priority.ArmPriority(2,.9f,prng,false);Check(oldState==CanonicalJson.Write(priority.StateJson())&&pi==prng.DrawIndex,"non-a no writes or draws");
  priority.ArmPriority(2,.60001f,prng);Check(!(bool)CanonicalJson.Map(priority.StateJson())["enabled"]&&pi==prng.DrawIndex,"late rearm disables without draw");
  // Reference-generated source/cost/selection cases, ordinary-only and no priority.
  foreach(var raw in CanonicalJson.Array(CanonicalJson.Parse(File.ReadAllText(args[1]))))
  {
   var f=CanonicalJson.Map(raw);xs=new List<CoreItem>();buffer=new int?[5];orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true},new CoreOrder(),new CoreOrder()};above=new HashSet<int>();var active=new List<int>();var pending=new List<int>();caps=new Dictionary<int,int>();int id=0,p=0;
   foreach(var b in CanonicalJson.Array(f["buffer"]))if(b!=null){int slot=Array.FindIndex(buffer,x=>!x.HasValue);buffer[slot]=++id;xs.Add(I(id,(string)b,"Buffer",0));}
   foreach(var pr in CanonicalJson.Array(f["plates"])) {p++;active.Add(p);var a=CanonicalJson.Array(pr);caps[p]=a.Count;foreach(var v in a){var z=CanonicalJson.Map(v);xs.Add(I(++id,(string)z["kind"],"ActiveAvailable",p));if((bool)z["above"])above.Add(id);}}
   foreach(var pr in CanonicalJson.Array(f["pending"])) {p++;pending.Add(p);foreach(var v in CanonicalJson.Array(pr))xs.Add(I(++id,(string)v,"Pending",p));}
   if(f["target"]!=null)orders[1].Kind=(string)f["target"];
   var costs=d.Costs(0,xs,orders,buffer,active,above,caps);var expected=CanonicalJson.Map(f["costs"]);Check(costs.Count==expected.Count&&costs.All(x=>expected.ContainsKey(x.Kind)&&CanonicalJson.Int(expected[x.Kind])==x.D),"reference costs");
   rng=new Pcg32((ulong)CanonicalJson.Int(f["seed"]));var clean=new NativeD1Director(content);var decision=clean.Choose(0,xs,orders,buffer,pending,active,above,rng,Convert.ToSingle(f["progress"]),plateCapacities:caps);Check(decision.Kind==(string)f["kind"],"reference selection");
  }
  Console.WriteLine("PASS "+checks+" native D1 checks");
 }
}


