using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using HotpotSort.Contracts;
using HotpotSort.Platform;
using HotpotSort.Platform.Editor;

namespace UnityEngine
{
    public class Object { public bool destroyed; public static void Destroy(Object value) { value.destroyed=true; } }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Texture { public static readonly List<Texture2D> Instances=new List<Texture2D>(); public string name; public Texture2D(int w,int h,TextureFormat f,bool mipmap) { Instances.Add(this); } }
    public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d) {x=a;y=b;width=c;height=d;} }
}

public static class WeChatFriendBoardQa
{
    private sealed class Transport : IWeChatFriendBoardTransport
    {
        public readonly List<string> Events=new List<string>(), Messages=new List<string>();
        public readonly List<FriendBoardViewport> Viewports=new List<FriendBoardViewport>();
        public bool FailShow, FailPost;
        public void PostMessage(string json) { if (FailPost) throw new IOException("injected post failure"); Events.Add("post");Messages.Add(json); }
        public void Show(UnityEngine.Texture texture,FriendBoardViewport viewport) { if (FailShow) throw new IOException("injected show failure");Events.Add("show");Viewports.Add(viewport); }
        public void Hide() { Events.Add("hide"); }
    }
    private static readonly List<object> results=new List<object>();
    private static readonly List<string> fixtures=new List<string>();
    private static int failed;
    private static readonly FriendBoardViewport View=new FriendBoardViewport(12,48,720,1280,2);
    private static void Assert(bool value,string reason="assertion") {if(!value)throw new Exception(reason);}
    private static void Throws<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static JsonElement Read(string value)=>JsonDocument.Parse(value).RootElement;
    private static void Test(string name,Action action)
    {try{action();results.Add(new{name,passed=true});}catch(Exception e){failed++;results.Add(new{name,passed=false,error=e.ToString()});}}
    public static int Main(string[] args)
    {
        string reportRoot=Path.GetFullPath(args[0]),source=Path.GetFullPath(args[1]);Directory.CreateDirectory(reportRoot);
        Test("contract has no friend record return channel",()=>{
            var methods=typeof(IWeChatFriendBoardSurface).GetMethods();Assert(methods.Length==5);Assert(methods.All(m=>m.ReturnType==typeof(void)));
            Assert(methods.Select(m=>m.Name).OrderBy(x=>x).SequenceEqual(new[]{"Close","Open","PublishOwnScore","Refresh","UpdateViewport"}));
        });
        Test("monotonic epoch IDs, correct show/post/close order and texture lifetime",()=>{
            var t=new Transport();var s=new WeChatFriendBoardSurface(t);s.PublishOwnScore(5,"device_marker",DateTimeOffset.Parse("2026-09-22T01:00:00Z"));
            s.Open(View);var texture=s.SharedTexture;s.Refresh();s.UpdateViewport(new FriendBoardViewport(0,90,1080,1920,3));s.Close();s.Close();
            Assert(texture.destroyed);Assert(s.SharedTexture==null);s.Open(View);s.Dispose();
            Assert(t.Messages.Count==7);Assert(Read(t.Messages[0]).GetProperty("viewEpoch").GetInt32()==0);
            Assert(Read(t.Messages[5]).GetProperty("viewEpoch").GetInt32()==2);
            for(int i=0;i<t.Messages.Count;i++)Assert(Read(t.Messages[i]).GetProperty("requestId").GetInt32()==i+1);
            Assert(t.Events.SequenceEqual(new[]{"post","show","post","post","show","post","post","hide","show","post","post","hide"}));
            fixtures.AddRange(t.Messages);Throws<ObjectDisposedException>(()=>s.Refresh());
        });
        Test("repeated open refreshes rather than allocating a second surface",()=>{
            var t=new Transport();using(var s=new WeChatFriendBoardSurface(t)){s.Open(View);var texture=s.SharedTexture;s.Open(View);Assert(ReferenceEquals(texture,s.SharedTexture));Assert(Read(t.Messages[1]).GetProperty("type").GetString()=="refresh");}
        });
        Test("closed refresh and resize do not send or reopen",()=>{
            var t=new Transport();using(var s=new WeChatFriendBoardSurface(t)){s.Refresh();s.UpdateViewport(View);Assert(t.Messages.Count==0);}
        });
        Test("invalid viewport, marker and score fail before SDK",()=>{
            var t=new Transport();using(var s=new WeChatFriendBoardSurface(t)){
                Throws<ArgumentOutOfRangeException>(()=>s.Open(default));Throws<ArgumentOutOfRangeException>(()=>new FriendBoardViewport(0,0,4000,8192,2));
                Throws<ArgumentOutOfRangeException>(()=>new FriendBoardViewport(0,0,720,1280,double.NaN));
                Throws<ArgumentOutOfRangeException>(()=>s.PublishOwnScore(-1,"valid",DateTimeOffset.UtcNow));
                Throws<ArgumentException>(()=>s.PublishOwnScore(1,"bad\"marker",DateTimeOffset.UtcNow));Assert(t.Messages.Count==0);
            }
        });
        Test("invariant decimal serialization and UTC timestamp",()=>{
            var old=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");var t=new Transport();using(var s=new WeChatFriendBoardSurface(t)){
                s.Open(new FriendBoardViewport(0,0,720,1280,2.75));Assert(Read(t.Messages[0]).GetProperty("payload").GetProperty("dpr").GetDouble()==2.75);
                s.PublishOwnScore(1,"owner",DateTimeOffset.Parse("2026-09-22T09:00:00+08:00"));Assert(Read(t.Messages[1]).GetProperty("payload").GetProperty("updatedAtUtc").GetString()=="2026-09-22T01:00:00.000Z");
            }}finally{CultureInfo.CurrentCulture=old;}
        });
        Test("failed show or post releases resources and permits recovery",()=>{
            foreach(bool failShow in new[]{true,false}){
                var t=new Transport{FailShow=failShow,FailPost=!failShow};var s=new WeChatFriendBoardSurface(t);Throws<IOException>(()=>s.Open(View));Assert(s.SharedTexture==null);Assert(t.Events.Last()=="hide");
                t.FailShow=t.FailPost=false;s.Open(View);Assert(Read(t.Messages.Last()).GetProperty("viewEpoch").GetInt32()>1);s.Dispose();
            }
        });
        Test("replacement adapter preserves monotonic protocol identity",()=>{
            var first=new Transport();using(var a=new WeChatFriendBoardSurface(first)){a.Open(View);}
            var second=new Transport();using(var b=new WeChatFriendBoardSurface(second)){b.Open(View);}
            Assert(Read(second.Messages[0]).GetProperty("viewEpoch").GetInt32()>Read(first.Messages[0]).GetProperty("viewEpoch").GetInt32());
            Assert(Read(second.Messages[0]).GetProperty("requestId").GetInt32()>Read(first.Messages.Last()).GetProperty("requestId").GetInt32());
        });
        Test("UV contract matches SDK flipped shared texture",()=>{var uv=WeChatFriendBoardSurface.SharedTextureUv;Assert(uv.x==0&&uv.y==1&&uv.width==1&&uv.height==-1);});
        Test("DPR 1 2 3 and nonzero offsets are preserved in actual transport and serialized fixtures",()=>{
            var t=new Transport();using(var s=new WeChatFriendBoardSurface(t)){
                foreach(int dpr in new[]{1,2,3}){
                    var viewport=new FriendBoardViewport(23*dpr,61*dpr,360*dpr,640*dpr,dpr);s.Open(viewport);s.Refresh();s.UpdateViewport(viewport);s.Close();
                    var actual=t.Viewports.Last();Assert(actual.X==23*dpr&&actual.Y==61*dpr&&actual.Width==360*dpr&&actual.Height==640*dpr&&actual.DevicePixelRatio==dpr);
                    foreach(string json in t.Messages.Skip(t.Messages.Count-4).Take(3)){
                        var payload=Read(json).GetProperty("payload");Assert(payload.GetProperty("x").GetInt32()==23*dpr);Assert(payload.GetProperty("y").GetInt32()==61*dpr);
                        Assert(payload.GetProperty("width").GetInt32()==360*dpr);Assert(payload.GetProperty("height").GetInt32()==640*dpr);Assert(payload.GetProperty("dpr").GetInt32()==dpr);
                    }
                }
            }fixtures.AddRange(t.Messages);
        });
        Test("20 lifecycle cycles release all textures and retain monotonic commands",()=>{
            var t=new Transport();int before=UnityEngine.Texture2D.Instances.Count;using(var s=new WeChatFriendBoardSurface(t)){
                int lastEpoch=0,lastId=0;
                for(int cycle=0;cycle<20;cycle++){
                    s.Open(View);var texture=s.SharedTexture;s.Refresh();s.Close();Assert(texture.destroyed&&s.SharedTexture==null);s.Refresh();s.UpdateViewport(View);s.Close();
                    Assert(t.Messages.Count==(cycle+1)*3);var open=Read(t.Messages[cycle*3]);Assert(open.GetProperty("viewEpoch").GetInt32()>lastEpoch);lastEpoch=open.GetProperty("viewEpoch").GetInt32();
                    foreach(string json in t.Messages.Skip(cycle*3)) {var m=Read(json);int next=m.GetProperty("requestId").GetInt32();Assert(next>lastId);lastId=next;Assert(m.GetProperty("viewEpoch").GetInt32()==lastEpoch);}
                }
            }
            Assert(UnityEngine.Texture2D.Instances.Count-before==20);Assert(UnityEngine.Texture2D.Instances.Skip(before).All(x=>x.destroyed));
            Assert(t.Events.Count(x=>x=="show")==20&&t.Events.Count(x=>x=="hide")==20);fixtures.AddRange(t.Messages);
        });
        Test("production source and complete export static guard",()=>{
            WeChatFriendBoardExportGuard.ValidateSource(source);string package=Path.Combine(reportRoot,"export-fixture");Directory.CreateDirectory(Path.Combine(package,"open-data"));
            File.Copy(Path.Combine(source,"index.js"),Path.Combine(package,"open-data","index.js"),true);File.WriteAllText(Path.Combine(package,"game.json"),"{\"openDataContext\":\"open-data\"}");
            WeChatFriendBoardExportGuard.ValidateExport(package,source);
            File.AppendAllText(Path.Combine(package,"open-data","index.js"),"\nMath.random();");Throws<InvalidDataException>(()=>WeChatFriendBoardExportGuard.ValidateExport(package,source));
            File.Copy(Path.Combine(source,"index.js"),Path.Combine(package,"open-data","index.js"),true);File.WriteAllText(Path.Combine(package,"game.json"),"{}");Throws<InvalidDataException>(()=>WeChatFriendBoardExportGuard.ValidateExport(package,source));
            File.WriteAllText(Path.Combine(package,"game.json"),"{\"openDataContext\":\"open-data\"}");WeChatFriendBoardExportGuard.ValidateExport(package,source);
        });
        File.WriteAllText(Path.Combine(reportRoot,"csharp-protocol-fixtures.json"),JsonSerializer.Serialize(fixtures));
        string report=JsonSerializer.Serialize(new{task="TASK-008",version=1,passed=results.Count-failed,total=results.Count,results},new JsonSerializerOptions{WriteIndented=true});
        File.WriteAllText(Path.Combine(reportRoot,"csharp-report.json"),report);Console.WriteLine(report);return failed==0?0:1;
    }
}
