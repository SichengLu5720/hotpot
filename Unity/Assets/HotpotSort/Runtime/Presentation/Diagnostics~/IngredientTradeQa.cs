using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using HotpotSort.Collection;
using HotpotSort.Platform;
namespace UnityEngine {public static class JsonUtility {static readonly JsonSerializerOptions options=new JsonSerializerOptions{IncludeFields=true};public static string ToJson(object value)=>JsonSerializer.Serialize(value,options);public static T FromJson<T>(string value)=>JsonSerializer.Deserialize<T>(value,options);}}
namespace HotpotSort.Contracts {public enum RewardKind {Hint,ClearBuffer,Shuffle}}
namespace HotpotSort.Platform {
 public enum WeChatCapabilityState {Ready,NotConfigured}
 public sealed class WeChatRuntimeConfig {public string environment="development",cloudEnvironmentId="env";public WeChatCapabilityState CloudState=>WeChatCapabilityState.Ready;}
 public interface IWeChatCloudFunctionBridge {void Start(int id,string environment,string functionName,string json,Action<int,string> completion);void Cancel(int id);}
}
sealed class Persistence:ICollectionPersistence {CollectionDocument state;public CollectionDocument Load()=>state;public void Save(CollectionDocument value){state=value;}}
sealed class Bridge:IWeChatCloudFunctionBridge {
 public readonly List<Action<int,string>> callbacks=new List<Action<int,string>>();public readonly List<WeChatIngredientTradeService.Command> commands=new List<WeChatIngredientTradeService.Command>();
 public void Start(int id,string env,string name,string json,Action<int,string> callback){if(name!="hotpotIngredientTrade")throw new Exception();commands.Add(UnityEngine.JsonUtility.FromJson<WeChatIngredientTradeService.Command>(json));callbacks.Add(callback);}
 public void Cancel(int id){}
 public void Complete(int index,long revision,string account="a") {var p=CollectionStore.NewDocument("development",account);p.serverRevision=revision;p.tools[0]=(int)revision;callbacks[index](0,UnityEngine.JsonUtility.ToJson(new WeChatIngredientTradeService.Response{protocolVersion=1,requestId=commands[index].requestId,status="Synced",accountId=account,snapshot=p}));}
}
static class Program {
 static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
 static async Task Main(){
  var store=new CollectionStore("development","a",new Persistence());var bridge=new Bridge();var service=new WeChatIngredientTradeService(new WeChatRuntimeConfig(),"a",store.ApplyAuthoritativeSnapshot,bridge);
  var old=service.ListAsync();var newer=service.ListAsync();bridge.Complete(1,3);Check((await newer).Succeeded);bridge.Complete(0,2);Check((await old).Succeeded);Check(store.ReadCollection().serverRevision==3&&store.ReadCollection().tools[0]==3);
  bridge.Complete(1,99);Check(store.ReadCollection().serverRevision==3);
  var spoof=service.ListAsync();bridge.Complete(2,4,"b");Check(!(await spoof).Succeeded&&store.ReadCollection().serverRevision==3);
  Check((await service.CreateAsync("food_00","food_01","op")).failure==IngredientTradeFailure.Unavailable);
  Console.WriteLine("PASS: service compile, authoritative snapshot, old revision ignored, duplicate callback ignored, identity mismatch rejected, missing friend target unavailable");
 }
}
