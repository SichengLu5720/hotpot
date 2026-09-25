#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Capture=HotpotSort.Task001V7.VisualCapture;
namespace HotpotSort.Presentation
{
    [InitializeOnLoad] public static class Task026LockedPotCapture
    {
        const string Key="Task026.Capture";
        static Task026LockedPotCapture(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("TASK026 PASS "+message);}
        static void Execute()
        {
            int exit=0;
            try
            {
                var args=Environment.GetCommandLineArgs();string path=args[Array.IndexOf(args,"-visualEvidence")+1];Directory.CreateDirectory(path);
                var view=new GameObject("Task026Fixture").AddComponent<GameplayView>();view.ConfigureAssets(PresentationAssets.CandidateRoot);
                var state=Capture.Fixture();var port=new Capture.CapturePort(state);view.Bind(port);view.SetForeground(true);view.World.SetSimulating(false);
                Check(view.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="UnlockProgress")==2,"two capsule tracks");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="AdBadge"||n.name=="Locked"||n.name=="Icon_lock"),"no ad badges or lock icons");
                Check(view.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="PotPlus")==2,"two pot plus symbols");
                var toolBadges=view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="ToolPlus").ToArray();
                Check(toolBadges.Length==3,"three tool plus badges");
                foreach(var badge in toolBadges)
                {
                    var parent=(RectTransform)badge.parent;float left=badge.anchoredPosition.x,top=-badge.anchoredPosition.y;
                    Check(left>78&&top>=10&&left+badge.rect.width<parent.rect.width&&top+badge.rect.height<36,"tool badge inside safe area "+parent.name);
                    Check(badge.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),"badge preserves click routing "+parent.name);
                }
                Check(!view.GetComponentsInChildren<RectTransform>().Where(n=>n.name=="UnlockProgress").Any(n=>n.GetComponentInChildren<Text>()),"capsules have no text");
                Capture.Capture(view,1080,1920,path+"/locked-pots.png");
                var fills=view.GetComponentsInChildren<Image>().Where(i=>i.name=="Fill").OrderBy(i=>i.transform.parent.parent.name).ToArray();
                Check(fills.Length==2&&fills.All(i=>i.fillAmount==0),"zero progress hidden");
                var gradient=fills[0].sprite;
                Check(fills.All(i=>i.sprite==gradient&&i.type==Image.Type.Filled&&i.fillMethod==Image.FillMethod.Horizontal),"shared fixed full width gradient clipping");
                Color32[] stops={new Color32(120,28,20,255),new Color32(181,38,24,255),new Color32(228,59,31,255),new Color32(255,118,38,255)};
                for(int i=0;i<4;i++)Check(((Color32)GameplayView.FireProgressColor(i/3f)).Equals(stops[i]),"exact fire stop "+i);
                state.completedOrders=1;view.RefreshOrderPresentation();
                Check(fills.All(i=>i&&i.sprite==gradient),"progress updates retain same image and texture");
                Check(Mathf.Abs(fills[0].fillAmount-1f/31)<.0001f&&Mathf.Abs(fills[1].fillAmount-1f/49)<.0001f,"low progress uses order ratios");
                Check(fills.All(i=>i.GetComponents<MonoBehaviour>().Length==1),"fill has no animation behaviours");
                Capture.Capture(view,1080,1920,path+"/fire-low.png");
                fills[0].fillAmount=.75f;fills[1].fillAmount=.45f;
                // Independent presentation ratios cannot arise from a single integer order count.
                // Temporarily detach refresh references for this explicitly labelled visual fixture.
                var bindings=(Image[])typeof(GameplayView).GetField("fireFills",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(view);
                bindings[2]=bindings[3]=null;
                Capture.Capture(view,1080,1920,path+"/fire-75-45-fixture.png");
                bindings[2]=fills[0];bindings[3]=fills[1];
                foreach(int count in new[]{30,31,48,49})
                {
                    state.completedOrders=count;view.RefreshOrderPresentation();
                    Check(Mathf.Abs(fills[0].fillAmount-Mathf.Clamp01(count/31f))<.0001f&&Mathf.Abs(fills[1].fillAmount-Mathf.Clamp01(count/49f))<.0001f,"threshold clipping "+count);
                }
                state.completedOrders=0;view.RefreshOrderPresentation();
                RewardKind? requested=null;RewardRoute? route=null;view.RewardRequested+=(k,r)=>{requested=k;route=r;};
                foreach(int slot in new[]{2,3})
                {
                    view.GetComponentsInChildren<Button>().Single(b=>b.name=="LockedPotHit"&&b.transform.parent.name=="Order_"+slot).onClick.Invoke();
                    var modal=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="LockedPotUnlock");
                    Check(modal.GetComponentsInChildren<Button>().Length==2,"one reward button and close");
                    Check(!modal.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("分享")),"no sharing");
                    Check(modal.GetComponentsInChildren<Text>().Any(t=>t.text=="再完成 "+(slot==2?31:49)+" 锅订单即可解锁"),"remaining count slot "+slot);
                    Capture.Capture(view,1080,1920,path+"/unlock-slot-"+slot+".png");
                    modal.GetComponentsInChildren<Button>().Single(b=>b.name=="Action_看视频解锁").onClick.Invoke();
                    Check(requested==(slot==2?RewardKind.ThirdPot:RewardKind.FourthPot)&&route==RewardRoute.SimulatedAd,"targeted rewarded route slot "+slot);
                }
                state.completedOrders=15;view.RefreshOrderPresentation();
                foreach(int slot in new[]{2,3})
                {
                    var track=view.GetComponentsInChildren<RectTransform>().Single(n=>n.name=="UnlockProgress"&&n.parent.name=="Order_"+slot);
                    var fill=track.Find("Fill").GetComponent<Image>();
                    Check(Mathf.Abs(fill.rectTransform.rect.width-76)<.01f&&Mathf.Abs(fill.fillAmount-15f/(slot==2?31:49))<.0001f,"fixed width progress refresh slot "+slot);
                }
                Capture.Capture(view,1080,1920,path+"/progress-15-orders.png");
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="LockedPotHit"&&b.transform.parent.name=="Order_2").onClick.Invoke();
                Check(view.GetComponentsInChildren<Text>().Any(t=>t.text=="再完成 16 锅订单即可解锁"),"remaining count after progress update");
                foreach(var order in state.orders)order.enabled=true;view.RefreshOrderPresentation();
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="UnlockProgress"||n.name=="AdBadge"||n.name=="LockedPotHit"),"unlocked controls removed");
                Check(!view.GetComponentsInChildren<RectTransform>().Any(n=>n.name=="LockedPotUnlock"),"unlock closes corresponding dialog");
                view.Bind(null);
                view.gameObject.SetActive(false);
                var legacy=new GameObject("Task026LegacyFixture").AddComponent<GameplayView>();
                legacy.Bind(new Capture.CapturePort(Capture.Fixture()));legacy.SetForeground(true);legacy.World.SetSimulating(false);
                Check(legacy.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="PotPlus")==2,"legacy two pot plus symbols");
                Check(legacy.GetComponentsInChildren<RectTransform>().Count(n=>n.name=="ToolPlus")==3,"legacy three tool plus badges");
                Capture.Capture(legacy,1080,1920,path+"/legacy-plus.png");legacy.Bind(null);
                File.WriteAllText(path+"/result.txt","PASS: actual Unity PlayMode fixture, locked pot UI, remaining counts, reward routing, unlocked cleanup. Platform ad playback and device feel not covered.");
            }
            catch(Exception e){exit=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(exit);}
        }
    }
}
#endif
