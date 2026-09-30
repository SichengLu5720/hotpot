#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task059ToolLimitDiagnostic
    {
        const string Key="Task059.ToolLimitDiagnostic";
        static Task059ToolLimitDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);Debug.Log("TASK059 PASS "+message);}
        static void Invoke(GameplayView view,string name,params object[] args)=>typeof(GameplayView).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,args);
        static async void Execute()
        {
            int exit=0;GameplayView view=null;string root=Path.GetFullPath("../.harness/qa/TASK-059/view");
            try
            {
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-visualEvidence");if(index>=0&&index+1<args.Length)root=args[index+1];Directory.CreateDirectory(root);
                view=new GameObject("Task059ToolLimitFixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();state.thirdPotThreshold=31;state.fourthPotThreshold=0;var port=new Port(state);view.Bind(port);view.SetForeground(true);view.World.SetSimulating(false);
                int requests=0;view.RewardRequested+=(k,r)=>requests++;view.SwapCreditRequested+=r=>requests++;
                var kinds=new[]{RewardKind.SwapOrder,RewardKind.ClearBuffer,RewardKind.Shuffle};
                foreach(int mask in new[]{0,1,2,4,7})
                {
                    state.usedToolMask=mask;port.Emit();await Task.Delay(30);
                    for(int i=0;i<kinds.Length;i++)
                    {
                        var button=view.GetComponentsInChildren<Button>().Single(b=>b.name=="Tool_"+kinds[i]);bool used=(mask&(1<<i))!=0;
                        Check(button.interactable==!used,"mask "+mask+" button "+kinds[i]);
                        Check(Math.Abs(button.GetComponent<CanvasGroup>().alpha-(used?.35f:1f))<.001f,"mask "+mask+" opacity "+kinds[i]);
                        if(used){button.onClick.Invoke();Invoke(view,"ChooseReward",kinds[i]);Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="SwapOrderIntro"||n.name.StartsWith("ToolDemo_")),"used tool cannot open intro");}
                    }
                    Check(requests==0,"used callbacks emitted no requests");
                    Capture.Capture(view,1080,1920,Path.Combine(root,"mask-"+mask+".png"));
                }
                state.usedToolMask=0;state.sessionId+="-new";state.sessionGeneration++;port.Emit();
                Check(view.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Tool_")).All(b=>b.interactable&&b.GetComponent<CanvasGroup>().alpha==1),"new session restores all tools");
                Invoke(view,"ChooseReward",RewardKind.ClearBuffer);await Task.Delay(30);
                var stale=view.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_分享领取");
                state.usedToolMask=2;port.Emit();stale.onClick.Invoke();Check(requests==0,"stale intro cannot emit reward after tool used");
                await Task.Delay(30);state.usedToolMask=0;port.Emit();Invoke(view,"ChooseReward",RewardKind.Shuffle);await Task.Delay(30);
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_分享领取").onClick.Invoke();Check(requests==1,"unused tool retains single share-first reward action");
                File.WriteAllText(Path.Combine(root,"result.txt"),"PASS mask 0/1/2/4/7 gray and input; callbacks blocked; new-session reset; stale intro; unused share-first reward. Real styles captured.\n");
            }
            catch(Exception e){exit=1;Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"error.txt"),e.ToString());Debug.LogException(e);}
            finally{if(view){view.World.Clear();view.Bind(null);}SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
        sealed class Port:IPresentationPort,IRevivalPresentationPort
        {
            readonly ViewSnapshot state;
            public int Used;
            public bool VideoAvailable=true;
            public Port(ViewSnapshot state){this.state=state;}
            public event Action<ViewUpdate> Updated;
            public ViewSnapshot Read()=>state;
            public void Emit(){state.revision++;Updated?.Invoke(new ViewUpdate{snapshot=state,events=Array.Empty<ViewEvent>()});}
            public void SessionAction(ViewAction action){}
            public void Tap(ViewTap command){}
            public void ObserveSupply(ViewSupplyObservation observation){}
            public RevivalOfferView ReadRevivalOffer()=>new RevivalOfferView
            {
                sessionId=state.sessionId,generation=state.sessionGeneration,rewardedVideoAvailable=VideoAvailable,
                shareAvailability=new ShareAvailability("fixture",Used,null,false,DateTimeOffset.UtcNow)
            };
            public Task<RewardApplicationResult> RequestRevivalAsync()=>Task.FromResult(RewardApplicationResult.Unavailable);
            public void DeclineRevival(){}
            public bool CompleteRevivalTransfer(RevivalCompletionToken token)=>false;
        }
    }
}
#endif
