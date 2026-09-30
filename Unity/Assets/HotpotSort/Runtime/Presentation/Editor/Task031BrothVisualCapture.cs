#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task031BrothVisualCapture
    {
        const string Key="Task031.BrothCapture";
        static readonly List<string> checks=new List<string>();
        sealed class Store:ICollectionStore
        {
            public CollectionDocument doc=new CollectionDocument();public event Action CollectionChanged;
            public Store(){for(int i=0;i<16;i++){doc.unlocked.Add(CollectionCatalog.Id(i));doc.selected.Add(CollectionCatalog.Id(i));}}
            public CollectionDocument ReadCollection()=>doc;
            public List<string> BeginSelectionDraft()=>new List<string>(doc.selected);
            public bool TrySaveSelection(IEnumerable<string> ids){var list=ids.ToList();if(list.Count<16)return false;doc.selected=list;return true;}
            public string[] CreateSessionSelection()=>doc.selected.ToArray();
            public CollectionReward RecordWin(string session,DateTimeOffset utc,bool local)=>null;
            public bool TryConsumeTool(RewardKind kind,string operation)=>false;
            public bool TryExchangeDuplicate(string operation,string offered,string received)=>false;
            public void ApplyAuthoritativeSnapshot(CollectionDocument value){doc=value;CollectionChanged?.Invoke();}
            public void Notify()=>CollectionChanged?.Invoke();
        }
        static Task031BrothVisualCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run()
        {
            SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Check(bool pass,string message){if(!pass)throw new Exception("TASK031 "+message);checks.Add(message);Debug.Log("TASK031_VIS_PASS "+message);}
        static T Named<T>(GameplayView view,string name)where T:Component=>view.GetComponentsInChildren<T>().Single(v=>v.name==name);
        static void Click(GameplayView view,string title)=>Named<Button>(view,"BrothAction_"+title).onClick.Invoke();
        static bool Has(GameplayView view,string name)=>view.GetComponentsInChildren<RectTransform>().Any(n=>n.name==name);
        static string Words(GameplayView view)=>string.Join("\n",view.GetComponentsInChildren<Text>().Select(t=>t.text));
        static void Execute()
        {
            int exit=0;string output="";
            try
            {
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-visualEvidence");if(index<0)throw new ArgumentException("-visualEvidence required");output=args[index+1];Directory.CreateDirectory(output);
                var view=new GameObject("Task031VisualFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var store=new Store();view.ConfigureCollection(store);view.UpdateBrothPresentation(store.doc);
                var state=Capture.Fixture();state.phase=ViewPhase.Entry;view.Bind(new Capture.CapturePort(state));view.SetForeground(true);view.World.SetSimulating(false);
                Action<string> shot=name=>{Capture.Capture(view,1080,1920,Path.Combine(output,name+".png"));Check(view.GetComponentsInChildren<RawImage>().All(i=>i.texture!=null),name+" no missing textures");};
                shot("home-before-first-win");Check(!Has(view,"BrothActivityEntry"),"before-first-win no activity node");
                store.doc.entryUnlocked=true;store.Notify();shot("home-after-first-win");
                var ingredient=Named<RectTransform>(view,"CollectionEntry");var broth=Named<RectTransform>(view,"BrothActivityEntry");
                float ingredientX=ingredient.anchoredPosition.x+ingredient.rect.width/2,brothX=broth.anchoredPosition.x+broth.rect.width/2;
                Check(Mathf.Abs(ingredientX-brothX)<.001f,"same logical vertical axis "+ingredientX);
                float pixelAxis=(1080-420*(1920f/900))*.5f+ingredientX*(1920f/900);Check(Mathf.Abs(pixelAxis-102)<.01f,"both screen axes X="+pixelAxis);
                Check(!broth.GetComponent<Image>()&&broth.GetComponent<Button>().interactable,"no entry backplate; colored interactive state");
                Check(!broth.Find("ClaimReminder").gameObject.activeSelf,"unqualified no red dot");
                view.OpenBrothCollection();shot("collection-broth");Check(!Words(view).Contains("助力")&&!Words(view).Contains("邀请")&&!Words(view).Contains("活动"),"broth collection has no activity content");
                Click(view,"食材");Check(Has(view,"CollectionGrid")&&view.GetComponentsInChildren<RectTransform>().Count(n=>n.name.StartsWith("food_"))==32,"ingredient 32-tile inventory preserved");shot("collection-ingredients");view.TryCloseCollection();
                int shares=0,confirms=0,claims=0;string claimed=null;view.BrothInvitationShareRequested+=()=>shares++;view.BrothAssistConfirmRequested+=id=>confirms++;view.BrothClaimRequested+=id=>{claims++;claimed=id;};
                view.OpenBrothActivity();shot("activity-zero");Check(Words(view).Contains("0 / 1"),"initial progress zero");Click(view,"邀请好友助力");Check(shares==1&&!store.doc.brothActivityQualified,"share intent does not qualify");
                Click(view,"活动详情 ﹀");shot("activity-detail");Check(Words(view).Contains("每日最多帮助 3")&&Words(view).Contains("不可更改"),"detail limit and immutable choice text");view.HandleBrothBack();view.CloseBrothActivity();
                store.doc.brothActivityQualified=true;store.Notify();shot("home-after-first-win-alert");Check(Named<RectTransform>(view,"ClaimReminder").gameObject.activeSelf,"qualification red dot");
                view.OpenBrothActivity();shot("activity-qualified");Check(Named<RectTransform>(view,"ClaimReminder").gameObject.activeSelf,"opening activity does not clear reminder");Click(view,"选番茄");shot("choice-confirmation");Check(claims==0&&Has(view,"BrothChoiceConfirmation"),"first selection only opens second confirmation");
                view.SetBrothOperationState(true,"正在确认…");Check(!Named<Button>(view,"BrothAction_确认选择").interactable,"busy confirm disabled");view.SetBrothOperationState(false);Click(view,"确认选择");Check(claims==1&&claimed==BrothCatalog.Tomato&&string.IsNullOrEmpty(store.doc.brothActivityChoice),"confirm emits intent without optimistic grant");
                store.doc.brothActivityChoice=BrothCatalog.Tomato;store.doc.ownedBroths.Add(BrothCatalog.Tomato);store.Notify();shot("activity-claimed");Check(!Has(view,"BrothChoiceConfirmation")&&!Has(view,"BrothAction_选番茄"),"claimed activity immutable");view.CloseBrothActivity();Check(!Has(view,"ClaimReminder"),"claim clears dot");
                view.OpenBrothCollection();shot("collection-owned-switch");int selects=0;view.BrothSelectRequested+=id=>{selects++;Check(id==BrothCatalog.Tomato,"switch emits owned stable id");};Click(view,"切换使用");Check(selects==1&&store.doc.currentBroth==BrothCatalog.Red,"switch intent does not mutate document");view.TryCloseCollection();
                store.doc.entryUnlocked=false;store.Notify();view.OpenBrothAssistConfirmation(new BrothInvitation{invitationId="diagnostic-invitation",canAssist=true});shot("friend-assist-confirm");Check(confirms==0&&!Has(view,"BrothActivityEntry"),"friend landing before first win; opening never assists");
                Check(view.GetComponentsInChildren<Text>().Count(t=>t.text=="× 1")==3,"exact three rewards one each");Click(view,"确认助力");Check(confirms==1,"explicit friend confirm emits intent");view.ShowBrothAssistSuccess("other-invitation");Check(!Words(view).Contains("奖励已到账"),"wrong invitation success ignored");view.ShowBrothAssistSuccess("diagnostic-invitation");shot("friend-assist-success");view.CloseBrothActivity();
                view.OpenBrothAssistConfirmation(new BrothInvitation{invitationId="development",canAssist=true},true);shot("friend-assist-development");Check(Words(view).Contains("Development 模拟"),"development clearly marked");view.CloseBrothActivity();
                view.OpenBrothAssistConfirmation(new BrothInvitation{invitationId="self",canAssist=false,isInitiator=true});Check(!Named<Button>(view,"BrothAction_确认助力").interactable,"self assist disabled");view.CloseBrothActivity();
                foreach(bool warmup in new[]{false,true})foreach(string id in new[]{BrothCatalog.Red,BrothCatalog.Clear,BrothCatalog.Tomato,BrothCatalog.Mushroom})
                {
                    store.doc.currentBroth=id;store.Notify();state=Capture.Fixture();state.isWarmup=warmup;view.Bind(new Capture.CapturePort(state));view.SetForeground(true);view.World.SetSimulating(false);shot((warmup?"warmup-":"formal-")+id);
                    var soups=view.GetComponentsInChildren<RawImage>().Where(i=>i.name=="Broth").ToArray();Check(soups.Length==2&&soups.Select(i=>i.texture).Distinct().Count()==1,"two active identical soups, two unlit: "+warmup+" "+id);
                    Check(view.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="PotPlus")==2,"two locked controls preserved "+id);
                }
                foreach(var order in state.orders)order.enabled=true;view.RefreshOrderPresentation();shot("four-active-mushroom");Check(view.GetComponentsInChildren<RawImage>().Count(i=>i.name=="Broth")==4,"four active soups");
                store.doc.currentBroth=BrothCatalog.Clear;store.Notify();var all=view.GetComponentsInChildren<RawImage>().Where(i=>i.name=="Broth").ToArray();Check(all.Length==4&&all.All(i=>i.texture.name=="clear"),"all four soups refresh without rebuilding orders");
                File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);File.WriteAllText(Path.Combine(output,"scope.txt"),"Actual Unity PlayMode rendering at 1080x1920 with deterministic state inputs. Not live cloud, share, physical gameplay or device verification.\nIngredient/activity logical center X="+ingredientX+"; screen center X="+pixelAxis+". Production service wiring is the Code integrator's responsibility.");
                view.Bind(null);view.World.Clear();view.gameObject.SetActive(false);
                Debug.Log("TASK031_VISUAL_CAPTURE_OK "+output);
            }
            catch(Exception e){exit=1;Debug.LogException(e);if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
