using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using HotpotSort.Contracts;
using HotpotSort.Collection;
using HotpotSort.Platform;
namespace UnityEngine {
 public static class PlayerPrefs {static readonly Dictionary<string,string> values=new Dictionary<string,string>();public static string GetString(string key,string fallback)=>values.TryGetValue(key,out var value)?value:fallback;public static void SetString(string key,string value)=>values[key]=value;public static void DeleteKey(string key)=>values.Remove(key);public static void Save(){} }
 public static class JsonUtility {static readonly JsonSerializerOptions options=new JsonSerializerOptions{IncludeFields=true};public static string ToJson(object value)=>JsonSerializer.Serialize(value,options);public static T FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,options);}}
namespace HotpotSort.Platform {
 public enum WeChatCapabilityState {Ready,NotConfigured}
 public sealed class WeChatRuntimeConfig {public string environment="development",cloudEnvironmentId="env",shareTitle="锅底活动";public bool enabled=true;public WeChatCapabilityState CloudState=>enabled?WeChatCapabilityState.Ready:WeChatCapabilityState.NotConfigured;public WeChatCapabilityState ShareState=>CloudState;}
 public interface IWeChatCloudFunctionBridge {void Start(int id,string environment,string functionName,string json,Action<int,string> completion);void Cancel(int id);}
}
class Task031BrothPlatformQa
{
 const string Account="wx_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
 const string Invite="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
 sealed class Memory:ICollectionPersistence {public CollectionDocument Load()=>null;public void Save(CollectionDocument d){} }
 sealed class Bridge:IWeChatCloudFunctionBridge
 {
  public readonly List<WeChatBrothActivityService.Command> Commands=new List<WeChatBrothActivityService.Command>();
  readonly List<Action<int,string>> callbacks=new List<Action<int,string>>();
  public void Start(int id,string env,string name,string json,Action<int,string> callback){if(name!="hotpotIngredientTrade")throw new Exception();Commands.Add(UnityEngine.JsonUtility.FromJson<WeChatBrothActivityService.Command>(json));callbacks.Add(callback);}
  public void Cancel(int id){}
  public void Complete(int i,long revision,Action<WeChatBrothActivityService.Response> edit=null){var d=CollectionStore.NewDocument("development",Account);d.serverRevision=revision;var response=new WeChatBrothActivityService.Response{protocolVersion=1,requestId=Commands[i].requestId,accountId=Account,status="Synced",snapshot=d};edit?.Invoke(response);callbacks[i](0,UnityEngine.JsonUtility.ToJson(response));}
 }
 static string Request()=>Guid.NewGuid().ToString("N");
 static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
 static async Task Main()
 {
  var config=new WeChatRuntimeConfig();var store=new CollectionStore("development",Account,new Memory());var bridge=new Bridge();
  var service=new WeChatBrothActivityService(config,Account,store.ReadCollection,store.ApplyAuthoritativeSnapshot,bridge,TimeSpan.FromMilliseconds(80));
  string shared=null;var links=new WeChatActivityLinkService(config,q=>shared=q);
  links.ReceiveQuery(new Dictionary<string,string>{{WeChatActivityLinkService.QueryKey,Invite}});
  Check(links.PendingInvitationId==Invite&&bridge.Commands.Count==0,"cold query retained before login without assist");
  links.ReceiveQuery(new Dictionary<string,string>());
  Check(links.PendingInvitationId==Invite&&bridge.Commands.Count==0,"ordinary show return never assists");
  Check(links.RequestShare(Invite)&&shared=="brothInvitation="+Invite&&bridge.Commands.Count==0,"share sends query without reward action");
  var inspect=service.InspectInvitationAsync(Request(),links.PendingInvitationId);bridge.Complete(0,1,r=>r.invitation=new BrothInvitation{invitationId=Invite,canAssist=true});
  Check((await inspect).Succeeded&&!store.ReadCollection().entryUnlocked,"login-ready inspect allows invited first-time player");
  var old=service.ReadAsync(Request());var newer=service.ReadAsync(Request());bridge.Complete(2,3);Check((await newer).Succeeded,"new revision accepted");bridge.Complete(1,2);Check((await old).failure==BrothFailure.Stale&&store.ReadCollection().serverRevision==3,"old callback rejected");
  bridge.Complete(2,90);Check(store.ReadCollection().serverRevision==3,"duplicate callback retired");
  var wrong=service.ReadAsync(Request());bridge.Complete(3,4,r=>r.accountId="other");Check(!(await wrong).Succeeded,"wrong account rejected");
  wrong=service.ReadAsync(Request());bridge.Complete(4,4,r=>r.snapshot.environment="production");Check(!(await wrong).Succeeded,"wrong environment rejected");
  wrong=service.ReadAsync(Request());bridge.Complete(5,4,r=>r.requestId=Request());Check(!(await wrong).Succeeded,"wrong request rejected");
  var timed=service.SelectAsync(Request(),"red","stable");Check((await timed).failure==BrothFailure.Offline,"timeout does not fabricate success");
  var refresh=service.ReadAsync(Request());bridge.Complete(7,4);Check((await refresh).Succeeded,"refresh recovers after uncertain timeout");
  bridge.Complete(6,91);Check(store.ReadCollection().serverRevision==4,"late timeout callback ignored");
  var retry=service.SelectAsync(Request(),"red","stable");Check(bridge.Commands[8].expectedRevision==3,"retry retains original expected revision");bridge.Complete(8,4);Check((await retry).Succeeded,"idempotent retry can return latest authority");
  Check((await service.SelectAsync(Request(),"clear","stable")).failure==BrothFailure.OperationConflict,"retry argument change rejected locally");
  config.enabled=false;Check(!(await service.ReadAsync(Request())).Succeeded&&!links.RequestShare(Invite),"disabled configuration fails closed");config.enabled=true;
  var pending=service.ReadAsync(Request());service.Dispose();bridge.Complete(9,99);Check(!(await pending).Succeeded&&store.ReadCollection().serverRevision==4,"retired account service ignores callback");
  links.Dismiss(Invite);links.ReceiveQuery(new Dictionary<string,string>{{WeChatActivityLinkService.QueryKey,Invite}});Check(links.PendingInvitationId==Invite,"hot query reopens pending context");
  Check(!service.IsDevelopmentSimulation,"production service marked native");
  var restart=new WeChatBrothActivityService(config,Account,store.ReadCollection,store.ApplyAuthoritativeSnapshot,bridge,TimeSpan.FromMilliseconds(50));
  Check((await restart.SelectAsync(Request(),"red","restart-op")).failure==BrothFailure.Offline,"pending mutation saved before timeout");restart.Dispose();
  var advanced=store.ReadCollection();advanced.serverRevision=5;store.ApplyAuthoritativeSnapshot(advanced);
  restart=new WeChatBrothActivityService(config,Account,store.ReadCollection,store.ApplyAuthoritativeSnapshot,bridge);
  var resumed=restart.SelectAsync(Request(),"red","restart-op");Check(bridge.Commands[11].expectedRevision==4,"recreated service retains uncertain mutation revision");bridge.Complete(11,5);Check((await resumed).Succeeded,"recreated service retries pending operation");restart.Dispose();
  links.Dispose();Console.WriteLine("TASK031 PLATFORM CHECKS PASSED");
 }
}
