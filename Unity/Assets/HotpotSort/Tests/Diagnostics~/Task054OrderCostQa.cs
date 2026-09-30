using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HotpotSort.Core;
using HotpotSort.Determinism;
static class Task054OrderCostQa
{
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static CoreItem I(int id,string kind,string loc,int plate=1)=>new CoreItem{Id=id,Kind=kind,Location=loc,PlateId=plate,SourceIndex=id,Slot=-1};
 static Dictionary<string,object> C(object d,string k)=>CanonicalJson.Array(CanonicalJson.Map(d)["candidates"]).Select(CanonicalJson.Map).Single(x=>(string)x["kind"]==k);
 static int N(Dictionary<string,object> c,string key)=>CanonicalJson.Int(c[key]);
 static void Main(string[] args)
 {
  var content=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0])).Content;
  var director=new DailyDirector(content);var orders=new[]{new CoreOrder{Enabled=true},new CoreOrder{Enabled=true},new CoreOrder(),new CoreOrder()};
  object Eval(List<CoreItem> xs,int occupied=0,int[] visible=null,int[] hidden=null,int[] next=null,int[] active=null,int[] pending=null)
  {return CanonicalJson.Parse(CanonicalJson.Write(director.Choose(0,xs,orders,Enumerable.Range(0,5).Select(i=>i<occupied?(int?)(900+i):null).ToArray(),(pending??Array.Empty<int>()).ToList(),new Pcg32(12),false,clickable:new HashSet<int>(visible??Array.Empty<int>()),policyVersion:6,nextLayer:new HashSet<int>(next??Array.Empty<int>()),offscreen:new HashSet<int>(hidden??Array.Empty<int>()),activePlateOrder:active??new[]{1},costVersion:1).Diagnostic));}
  var x=new List<CoreItem>{I(1,"A","ActiveAvailable"),I(2,"A","ActiveAvailable"),I(3,"A","ActiveAvailable"),I(4,"A","Buffer")};
  var c=C(Eval(x,1,new[]{1,2,3}),"A");Check(N(c,"category")==2&&N(c,"d")==0,"scene takes precedence over stored match");
  x.RemoveAt(2);c=C(Eval(x,1,new[]{1,2}),"A");Check(N(c,"category")==1&&N(c,"d")==-1,"buffer completion is ready");
  x=new List<CoreItem>{I(1,"A","ActiveAvailable"),I(2,"A","Buffer"),I(3,"B","Pending",2),I(4,"A","Pending",2),I(5,"C","Pending",2),I(6,"D","Pending",2)};
  c=C(Eval(x,1,new[]{1},pending:new[]{2}),"A");Check(N(c,"supplyItems")==4&&N(c,"d")==2&&N(c,"need")==1,"all foods including final plate tail count, buffer alone not ready");
  x=new List<CoreItem>{I(1,"A","ActiveAvailable"),I(2,"A","ActiveAvailable",2),I(3,"B","ActiveAvailable",2),I(4,"A","Pending",3),I(5,"C","Pending",3),I(6,"D","Pending",3)};
  c=C(Eval(x,0,new[]{1},new[]{2,3},active:new[]{1,2},pending:new[]{3}),"A");Check(N(c,"supplyItems")==5&&N(c,"d")==2,"offscreen active before pending, active is not scene");
  x=new List<CoreItem>{I(1,"A","ActiveAvailable"),I(2,"A","ActiveAvailable"),I(3,"A","ActiveAvailable"),I(4,"B","ActiveAvailable")};
  c=C(Eval(x,0,new[]{1,2,4},next:new[]{3}),"A");Check((bool)c["unresolved"]&&c["d"]==null,"unsafe prerequisite not scene and not mislabeled far");
  orders[1].Kind="B";c=C(Eval(x,0,new[]{1,2,4},next:new[]{3}),"A");Check(N(c,"category")==2,"existing order can receive prerequisite");orders[1].Kind=null;
  for(int occupied=0;occupied<5;occupied++)for(int distance=1;distance<=8;distance++)
  {
   x=new List<CoreItem>{I(1,"A","ActiveAvailable"),I(2,"A","ActiveAvailable")};
   for(int j=0;j<distance*3;j++)x.Add(I(j+3,j==distance*3-1?"A":"B","Pending",j+2));
   c=C(Eval(x,occupied,new[]{1,2},pending:Enumerable.Range(2,distance*3).ToArray()),"A");int n=5-occupied;int expected=distance<n-1?3:distance==n-1?4:distance==n||distance==n+1?5:0;
   Check(N(c,"d")==distance&& (c["category"]==null?0:N(c,"category"))==expected,"original pressure boundaries "+occupied+"/"+distance);
  }
  foreach(int progress in new[]{0,74,119,156})for(int occupied=0;occupied<5;occupied++)
  {
   x=Enumerable.Range(1,3).Select(id=>I(id,"A","ActiveAvailable")).ToList();for(int j=0;j<progress;j++)x.Add(I(100+j,"P","Completed"));
   var d=CanonicalJson.Map(Eval(x,occupied,new[]{1,2,3}));int band=DailyDirector.ProgressBand(progress);
   Check(CanonicalJson.Array(d["rawWeights"]).Select(CanonicalJson.Int).SequenceEqual(content.Rows[band*5+occupied].Weights),"original weights "+band+"/"+occupied);
  }
  orders[1].Kind="A";x=Enumerable.Range(1,6).Select(id=>I(id,"A","ActiveAvailable")).ToList();c=C(Eval(x,0,new[]{1,2,3}),"A");Check(N(c,"reserved")==3&&N(c,"v")==0,"reserved scene food not reused");
  var factory=DailySessionFactory.FromProductionJson(File.ReadAllText(args[0]));
  var context=new HotpotSort.Contracts.ChallengeContext("2026-09-29",factory.Content.ContentVersion,factory.ConfigurationDigest,"test",0);
  using(var session=factory.CreateStage(context,HotpotSort.Contracts.ChallengeStage.Formal,0,0))
  {
   ulong tick=1,seq=1;for(int i=0;i<60;i++)session.Supply(new SupplyObservation((ulong)i+1,tick++,true,true));
   for(int i=0;i<39;i++)
   {
    var state=CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
    var all=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).Where(t=>(string)t["location"]=="ActiveAvailable").ToArray();
    var kinds=CanonicalJson.Array(state["orders"]).Select(CanonicalJson.Map).Where(o=>o["kind"]!=null).Select(o=>(string)o["kind"]).ToArray();
    var target=all.FirstOrDefault(t=>kinds.Contains((string)t["kind"]));if(target==null)break;
    var hiddenIds=new HashSet<int>(all.Where(t=>(string)t["kind"]!=(string)target["kind"]&&CanonicalJson.Int(t["itemId"])%2==0).Select(t=>CanonicalJson.Int(t["itemId"])));
    var obs=new ClickableObservation(session.Snapshot.SessionId,session.Snapshot.TransactionId,all.Select(t=>CanonicalJson.Int(t["itemId"])).Where(id=>!hiddenIds.Contains(id)),offscreenItemIds:hiddenIds,activePlateOrder:CanonicalJson.Array(state["activePlateIds"]).Select(CanonicalJson.Int),costVersion:1);
    Check(session.Tap(new TapCommand(CanonicalJson.Int(target["itemId"]),seq++,tick++,true,obs)).Accepted,"new observation session tap");
   }
   Check(session.CoreEventsJson.Contains("director_supply_cost_v1"),"new evaluator reached through session");
   var replay=HotpotSort.Replay.DailyReplay.Run(factory,session.ExportReplay());Check(replay.Success&&replay.Session.StateHash==session.StateHash,"new observation exact replay "+replay.Error);
  }
  Console.WriteLine("PASS TASK054 checks="+checks);
 }
}
