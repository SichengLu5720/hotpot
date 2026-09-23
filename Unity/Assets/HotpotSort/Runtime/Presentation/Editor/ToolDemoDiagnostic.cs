#if UNITY_EDITOR
using System;
using System.Linq;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public static class ToolDemoDiagnostic
    {
        static int checks;
        static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Debug.Log("TOOL_DEMO_PASS "+label);}
        static void Tick(ToolDemoLoop demo,float seconds){while(seconds>0){float dt=Mathf.Min(.05f,seconds);demo.Advance(dt);seconds-=dt;}}
        static float Spread(RectTransform[] nodes)
        {var center=nodes.Aggregate(Vector2.zero,(sum,n)=>sum+n.anchoredPosition)/nodes.Length;return nodes.Average(n=>Vector2.Distance(n.anchoredPosition,center));}
        public static void Run()
        {
            int exit=0;var roots=new GameObject[3];V7Art art=null;
            try
            {
                art=new V7Art(V7Art.Root);
                var foods=Enumerable.Range(0,16).Select(i=>art.Texture(AssetKey.Food(i))).ToArray();
                var uvs=Enumerable.Range(0,16).Select(art.FoodUv).ToArray();
                var kinds=new[]{RewardKind.Hint,RewardKind.ClearBuffer,RewardKind.Shuffle};
                for(int i=0;i<kinds.Length;i++)
                {
                    roots[i]=new GameObject("ToolDemoFixture_"+kinds[i],typeof(RectTransform));var rt=(RectTransform)roots[i].transform;rt.sizeDelta=new Vector2(310,270);
                    var demo=roots[i].AddComponent<ToolDemoLoop>();demo.Initialize(kinds[i],art.Texture(AssetKey.Plate),art.Texture(AssetKey.BufferDish),art.Texture("fx.hint"),foods,uvs);
                    Check(demo.Generation==1&&Math.Abs(demo.CycleDuration-4)<.001,"one-second hold plus three-second motion cycle "+kinds[i]);
                    var names=roots[i].GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
                    float advanced=0;var initial=roots[i].GetComponentsInChildren<RectTransform>(true).Where(n=>n.name.StartsWith("HintHighlight")||n.name.StartsWith("GatherFood_")||n.name.StartsWith("ShufflePlate_")).Select(n=>n.anchoredPosition).ToArray();
                    Tick(demo,.95f);advanced=.95f;var held=roots[i].GetComponentsInChildren<RectTransform>(true).Where(n=>n.name.StartsWith("HintHighlight")||n.name.StartsWith("GatherFood_")||n.name.StartsWith("ShufflePlate_")).Select(n=>n.anchoredPosition).ToArray();
                    Check(initial.SequenceEqual(held),"first second holds still "+kinds[i]);
                    if(kinds[i]==RewardKind.Hint)
                    {
                        var glow=roots[i].GetComponentsInChildren<RectTransform>(true).First(n=>n.name=="HintHighlight");
                        Check(names.Contains("HintHighlight")&&!names.Any(n=>n.IndexOf("Line",StringComparison.OrdinalIgnoreCase)>=0)&&glow.sizeDelta.x<=45,"hint tightly highlights one food only");
                    }
                    if(kinds[i]==RewardKind.ClearBuffer)
                    {
                        var gathered=roots[i].GetComponentsInChildren<RectTransform>(true).Where(n=>n.name.StartsWith("GatherFood_")).ToArray();
                        Check(names.Count(n=>n.StartsWith("BufferDish_"))==5&&gathered.Length==5&&names.Contains("GatherPlate")&&gathered.All(n=>n.parent.name=="GatherTransport"),"clear demo gathers five foods into one transport group");
                        Tick(demo,1.95f);advanced+=1.95f;Check(gathered.Select(n=>n.anchoredPosition).Distinct().Count()==5&&gathered.Any(n=>Math.Abs(n.localEulerAngles.z)>1),"clear foods settle in a mixed non-grid arrangement");
                    }
                    if(kinds[i]==RewardKind.Shuffle)Check(names.Count(n=>n.StartsWith("ShufflePlate_"))==4&&names.Count(n=>n=="FoodA"||n=="FoodB")==8,"shuffle keeps food under whole plates");
                    Check(roots[i].GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"demo does not intercept input "+kinds[i]);
                    Check(roots[i].GetComponent<RectMask2D>()!=null,"demo clips all out-of-frame visuals "+kinds[i]);
                    if(kinds[i]==RewardKind.Shuffle)
                    {
                        var plates=roots[i].GetComponentsInChildren<RectTransform>(true).Where(n=>n.name.StartsWith("ShufflePlate_")).ToArray();float before=Spread(plates);
                        Tick(demo,.77f);advanced+=.77f;Check(Spread(plates)<before*.55f,"shuffle first gathers plates centrally");
                        Tick(demo,.45f);advanced+=.45f;Check(plates.Any(n=>Math.Abs(n.localEulerAngles.z)>1),"shuffle mixes gathered plates briefly");
                    }
                    Tick(demo,3.8f-advanced);var group=roots[i].GetComponentInChildren<CanvasGroup>();Check(group&&group.alpha<.8f,"cycle fades before rebuild "+kinds[i]);
                    Tick(demo,.25f);Check(demo.Generation==2&&roots[i].GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("DemoCycle_")&&t.gameObject.activeSelf)==1,"cycle destroys and rebuilds "+kinds[i]);
                }
                Debug.Log("TOOL_DEMO_RUNTIME_PASS checks="+checks);
            }
            catch(Exception ex){exit=1;Debug.LogException(ex);}
            finally
            {
                foreach(var root in roots)if(root)UnityEngine.Object.DestroyImmediate(root);
                art?.Dispose();EditorApplication.Exit(exit);
            }
        }
    }
}
#endif
