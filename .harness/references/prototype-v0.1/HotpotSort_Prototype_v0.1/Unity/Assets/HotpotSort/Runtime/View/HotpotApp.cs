using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using HotpotSort.Core;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
namespace HotpotSort.View
{
    /// <summary>Complete playable prototype entry. Scene-independent, no prefab or plugin setup.</summary>
    public sealed class HotpotApp : MonoBehaviour
    {
        public string initialLevel="A"; public int initialSeed=7;
        public GameSession Session {get;private set;} public PlateWorld World {get;private set;}
        private GameData data; private PrimitiveArt art; private bool paused,debug,shownEnd;
        private double clock,accumulator,busyUntil;private string error="",notice="";private double noticeUntil;
        private string chosenLevel="A";private uint chosenSeed=7;private bool chosenFail=true;
        private Order[] uiOrders;private Token[] uiBuffer;
        private sealed class Task {public double at;public Action apply;}
        private sealed class Flight {public int kind;public BoardPoint from,to;public double start,end;}
        private readonly List<Task> tasks=new List<Task>();private readonly List<Flight> flights=new List<Flight>();
        private static Color C(string hex){return PrimitiveArt.ColorOf(hex);}private static readonly Color Ink=C("#2F5048"),Muted=C("#86927C"),Coral=C("#CB6956");
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HotpotDownload(string filename,string contents);
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureApp(){if(FindObjectOfType<HotpotApp>()==null)new GameObject("HotpotApp").AddComponent<HotpotApp>();}
        private void Awake()
        {
            try
            {
                Application.targetFrameRate=60;Screen.orientation=ScreenOrientation.Portrait;
                if(Camera.main==null){var camera=new GameObject("BackgroundCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=C("#E7E9DC");camera.cullingMask=0;}
                var resource=Resources.Load<TextAsset>("Hotpot/game-data");if(resource==null)throw new FileNotFoundException("Resources/Hotpot/game-data.json is missing");
                data=JsonUtility.FromJson<GameData>(resource.text);art=new PrimitiveArt();ResetGame(initialLevel,(uint)Math.Max(1,initialSeed),data.rules.failOnFull);
            }
            catch(Exception e){error=e.ToString();Debug.LogException(e);}
        }
        public void ResetGame(string level,uint seed,bool failOnFull)
        {
            Session=new GameSession(data,level,seed,failOnFull);World=new PlateWorld(Session.Rules,Session.Seed);
            clock=accumulator=busyUntil=0;paused=shownEnd=false;tasks.Clear();flights.Clear();CopyUI();Session.DrainEvents();
            chosenLevel=level;chosenSeed=Session.Seed;chosenFail=failOnFull;
            for(int i=0;i<480;i++)World.Step(Session,1.0/120);Session.DrainEvents();World.Audit(Session);
        }
        private void CopyUI(){uiOrders=Session.Orders.Select(o=>new Order{slot=o.slot,kind=o.kind,open=o.open,items=new List<Token>(o.items)}).ToArray();uiBuffer=(Token[])Session.Buffer.Clone();}
        private static BoardPoint OrderPoint(int slot){return new BoardPoint(61+slot*98,151);}
        private static BoardPoint BufferPoint(int slot){return new BoardPoint(105+slot*63,277);}
        private void Schedule(GameEvent[] events,BoardPoint source)
        {
            double cursor=clock;
            foreach(var e in events)
            {
                if(e.type=="move")
                {
                    if(e.from=="buffer")tasks.Add(new Task{at=cursor,apply=()=>uiBuffer[e.fromSlot]=null});
                    flights.Add(new Flight{kind=e.kind,from=e.from=="plate"?source:BufferPoint(e.fromSlot),to=e.to=="order"?OrderPoint(e.slot):BufferPoint(e.slot),start=cursor,end=cursor+.19});cursor+=.19;
                    tasks.Add(new Task{at=cursor,apply=()=>{var t=new Token(e.id,e.kind,0);if(e.to=="order")uiOrders[e.slot].items.Add(t);else uiBuffer[e.slot]=t;}});
                }
                else if(e.type=="complete"){tasks.Add(new Task{at=cursor+.03,apply=()=>{uiOrders[e.slot].items.Clear();uiOrders[e.slot].kind=-1;}});cursor+=.08;}
                else if(e.type=="target")tasks.Add(new Task{at=cursor,apply=()=>uiOrders[e.slot].kind=e.kind});
            }
            tasks.Sort((a,b)=>a.at.CompareTo(b.at));busyUntil=cursor+.03;
        }
        private void Pause(){paused=true;chosenLevel=Session.Level.id;chosenSeed=Session.Seed;chosenFail=Session.Rules.failOnFull;}
        private void OnApplicationFocus(bool focused){if(!focused&&Session!=null&&Session.Status=="playing")Pause();}
        private void Update()
        {
            if(Session==null||error.Length>0)return;
            try
            {
                if(Input.GetKeyDown(KeyCode.Escape)){if(paused&&Session.Status=="playing")paused=false;else Pause();}
                if(Input.GetKeyDown(KeyCode.R))ResetGame(Session.Level.id,Session.Seed,Session.Rules.failOnFull);
                if(Input.GetKeyDown(KeyCode.D))debug=!debug;
                if(Input.touchCount>0){var t=Input.GetTouch(0);if(t.phase==TouchPhase.Began)HandlePoint(ToBoard(t.position));}
                else if(Input.GetMouseButtonDown(0))HandlePoint(ToBoard(Input.mousePosition));
                if(paused)return;
                double dt=Math.Min(.05,Time.unscaledDeltaTime);clock+=dt;
                while(tasks.Count>0&&tasks[0].at<=clock){var task=tasks[0];tasks.RemoveAt(0);task.apply();}flights.RemoveAll(f=>f.end<clock);
                accumulator+=dt;while(accumulator>=Session.Rules.physicsStep){World.Step(Session,Session.Rules.physicsStep);accumulator-=Session.Rules.physicsStep;}
                Session.DrainEvents();if(Session.Status!="playing"&&clock>=busyUntil&&!shownEnd){shownEnd=true;CopyUI();Pause();}
            }
            catch(Exception e){error=e.ToString();Debug.LogException(e);}
        }
        private Rect Viewport()
        {
            Rect safe=Screen.safeArea;if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
            float scale=Mathf.Min(safe.width/420f,safe.height/900f);
            return new Rect(safe.x+(safe.width-420*scale)/2,Screen.height-safe.yMax+(safe.height-900*scale)/2,420*scale,900*scale);
        }
        private Vector2 ToBoard(Vector2 screen){var v=Viewport();return new Vector2((screen.x-v.x)/(v.width/420),(Screen.height-screen.y-v.y)/(v.height/900));}
        private void HandlePoint(Vector2 point)
        {
            float x=point.x,y=point.y;if(x<0||x>420||y<0||y>900)return;
            if(paused)
            {
                for(int i=0;i<4;i++)if(new Rect(45+i*84,404,76,39).Contains(point)){chosenLevel=new[]{"A","B","C","P"}[i];return;}
                if(new Rect(45,456,65,36).Contains(point)){chosenSeed=chosenSeed<=1?1:chosenSeed-1;return;}
                if(new Rect(310,456,65,36).Contains(point)){chosenSeed=chosenSeed==uint.MaxValue?1:chosenSeed+1;return;}
                if(new Rect(45,507,330,36).Contains(point)){chosenFail=!chosenFail;return;}
                if(new Rect(45,559,330,40).Contains(point)){if(Session.Status=="playing")paused=false;return;}
                if(new Rect(45,610,330,40).Contains(point)){ResetGame(chosenLevel,chosenSeed,chosenFail);return;}
                if(new Rect(45,662,330,34).Contains(point)){ExportLog();return;}
                if(new Rect(45,707,330,26).Contains(point)){debug=!debug;return;}return;
            }
            if(y<78&&x>346){Pause();return;}
            if(y>104&&y<221&&x>204){Notify("预留订单位：本版不开放扩展");return;}
            if(clock<busyUntil||Session.Status!="playing")return;
            var hit=World.Hit(Session,x,y);if(hit==null)return;
            bool accepted=Session.Tap(hit.item.id);var events=Session.DrainEvents();World.Sync(Session);
            if(accepted)Schedule(events,hit.point);else if(Session.Status=="lost")busyUntil=clock+.1;
        }
        private void Notify(string message){notice=message;noticeUntil=Time.realtimeSinceStartupAsDouble+3;}
        private void ExportLog()
        {
            string name="hotpot-"+Session.Level.id+"-seed"+Session.Seed+".json",json=JsonUtility.ToJson(Session.ExportReplay(),true);
#if UNITY_WEBGL && !UNITY_EDITOR
            HotpotDownload(name,json);Notify("已导出逻辑日志");
#else
            string path=Path.Combine(Application.persistentDataPath,name);File.WriteAllText(path,json);Debug.Log("Replay saved: "+path);Notify("日志已写入 persistentDataPath，路径见 Console");
#endif
        }
        private void Label(string s,float x,float y,float w,float h,int size,Color color,TextAnchor a=TextAnchor.MiddleLeft,bool bold=false){art.Text(s,new Rect(x,y,w,h),size,color,a,bold);}
        private void Panel(float x,float y,float w,float h,float radius,string fill,string border="#D5DDC8"){art.Panel(new Rect(x,y,w,h),radius,C(fill),C(border));}
        private void DrawOrder(Order order,int i)
        {
            float x=16+i*98,cx=61+i*98;Panel(x,104,90,117,16,order.open?"#FFFBEF":"#E7EBDB");
            if(!order.open){Label("+",x,118,90,52,37,C("#A6B5A0"),TextAnchor.MiddleCenter);Label("待开放",x,177,90,22,10,Muted,TextAnchor.MiddleCenter);return;}
            art.Rounded(new Rect(cx-39,150,78,13),5,C("#C1A785"));art.Rounded(new Rect(cx-31,135,62,35),16,C("#C58E66"));art.Rounded(new Rect(cx-32,123,64,39),18,C("#E7C697"));art.Rounded(new Rect(cx-27,128,54,28),14,C("#EFE2B7"));
            if(order.kind>=0){art.Food(order.kind,cx,146,21);Label(data.ingredients[order.kind].name,x,174,90,19,11,Ink,TextAnchor.MiddleCenter,true);}else Label("已出齐",x,168,90,28,11,Muted,TextAnchor.MiddleCenter);
            for(int j=0;j<3;j++)art.Circle(cx+(j-1)*11,203,3.2f,j<order.items.Count?Coral:C("#DEDCC6"));Label(order.items.Count+"/3",x+61,107,23,13,8,Muted,TextAnchor.MiddleCenter);
        }
        private void DrawGame()
        {
            art.Rect(new Rect(0,0,420,900),C("#D8E2CB"));Panel(14,305,392,531,20,"#E6EAD7","#CBD7BE");
            foreach(var body in World.Bodies)
            {
                var plate=Session.Active.FirstOrDefault(p=>p.id==body.id);if(plate==null)continue;art.Plate((float)body.x,(float)body.y,(float)body.r,body.id,debug);
                foreach(var token in plate.items){var p=World.Position(plate,token);art.Food(token.kind,(float)p.x,(float)p.y,(float)p.r,true);}
            }
            art.Rect(new Rect(0,0,420,304),C("#F8F4E3"));Panel(20,23,36,37,11,"#CB6956","#CB6956");Label("锅",20,22,36,38,22,C("#FFF4DD"),TextAnchor.MiddleCenter,true);
            Label("开锅啦",68,18,180,38,25,Ink,TextAnchor.MiddleLeft,true);Label("HOT POT SORT",70,51,150,17,9,Muted);
            Panel(271,29,72,25,12,"#E3E9D7");Label(Session.Level.name,271,29,72,25,11,C("#667F66"),TextAnchor.MiddleCenter,true);
            art.Circle(381,42,18,C("#FCFAED"));Label("II",361,21,40,40,20,C("#6D8975"),TextAnchor.MiddleCenter);
            Label("今日订单",19,74,100,25,11,Muted,TextAnchor.MiddleLeft,true);Label("同类 3 件 · 自动出锅",234,74,167,25,10,Muted,TextAnchor.MiddleRight);
            for(int i=0;i<uiOrders.Length;i++)DrawOrder(uiOrders[i],i);
            Label("暂存食材",20,230,150,23,10,Muted,TextAnchor.MiddleLeft,true);Label(uiBuffer.Count(t=>t!=null)+" / 5",340,230,60,23,10,uiBuffer.Count(t=>t!=null)>=4?Coral:Muted,TextAnchor.MiddleRight);
            Panel(16,255,50,46,10,"#DFE6D2");Label("已出锅",16,259,50,16,8,Muted,TextAnchor.MiddleCenter);Label(Session.Completed+"/"+Session.Total,16,276,50,20,11,Ink,TextAnchor.MiddleCenter,true);
            for(int i=0;i<5;i++){var p=BufferPoint(i);Panel((float)p.x-28,255,56,46,9,"#FEFBEF","#C7D5BD");if(uiBuffer[i]!=null)art.Food(uiBuffer[i].kind,(float)p.x,(float)p.y,17,true);}
            Panel(14,842,392,47,15,"#F6F4E1");Label("点一件食材，同类凑满 3 件出锅",14,846,392,23,11,C("#52735C"),TextAnchor.MiddleCenter,true);
            Label(World.Spawned+" / "+Session.Level.plates.Length+" 盘已上桌  ·  "+(Session.Completed/3)+" 份已完成  ·  种子 "+Session.Seed,14,869,392,17,9,Muted,TextAnchor.MiddleCenter);
            foreach(var f in flights){if(clock<f.start||clock>f.end)continue;double v=(clock-f.start)/(f.end-f.start),t=1-Math.Pow(1-v,3);art.Food(f.kind,(float)(f.from.x+(f.to.x-f.from.x)*t),(float)(f.from.y+(f.to.y-f.from.y)*t-Math.Sin(v*Math.PI)*20),19,true);}
            if(debug)
            {
                art.Rect(new Rect(20,(float)Session.Rules.spawnGate,380,1),Coral);Panel(24,311,280,42,8,"#315549","#315549");
                Label("active="+Session.Active.Count+" pending="+Session.Pending.Count+" rng="+Session.Random.State,31,316,270,16,9,C("#F0F2DD"));Label("供给 "+(World.Blocked?"暂停":"可继续")+" | commands="+Session.Commands.Count+" | "+Session.Status,31,333,270,16,9,C("#F0F2DD"));
            }
        }
        private void Button(string label,Rect r,bool primary=false){art.Rounded(r,10,primary?Ink:C("#EFF0DF"));art.Text(label,r,13,primary?C("#FFF9E8"):Ink,TextAnchor.MiddleCenter,true);}
        private void DrawMenu()
        {
            art.Rect(new Rect(0,0,420,900),new Color(.14f,.24f,.2f,.76f));Panel(25,205,370,556,22,"#FFF8E9");
            Label("HOT POT SORT / PROTOTYPE 0.1",45,226,330,25,9,Coral,TextAnchor.MiddleLeft,true);
            string title=Session.Status=="won"?"全部出锅！":Session.Status=="lost"?"暂存区满了":"歇口气，再开锅";
            Label(title,45,265,330,45,25,Ink,TextAnchor.MiddleLeft,true);
            Label("已完成 "+Session.Completed+" / "+Session.Total+" 件 · 点击 "+Session.Moves+" 次",45,319,330,23,12,Muted);
            Label("选择测试骨架；重开后使用新的配置",45,363,330,27,11,Muted);
            for(int i=0;i<4;i++){string id=new[]{"A","B","C","P"}[i];Button(id=="P"?"练习":"骨架 "+id,new Rect(45+i*84,404,76,39),chosenLevel==id);}
            Button("−",new Rect(45,456,65,36));Label("随机种子 "+chosenSeed,113,456,194,36,13,Ink,TextAnchor.MiddleCenter);Button("+",new Rect(310,456,65,36));
            Button(chosenFail?"失败：第 5 格占满并结算后":"失败：下一件无暂存空位时",new Rect(45,507,330,36));
            Button(Session.Status=="playing"?"继续本局":"本局已结束",new Rect(45,559,330,40),Session.Status=="playing");Button("按以上配置重开",new Rect(45,610,330,40));Button("导出本局逻辑日志",new Rect(45,662,330,34));
            Label("调试信息："+(debug?"开":"关")+"（点击切换）",45,707,330,26,10,Muted,TextAnchor.MiddleCenter);
        }
        private void OnGUI()
        {
            if(error.Length>0){GUI.Label(new Rect(20,20,Screen.width-40,Screen.height-40),error);return;}if(art==null||Session==null)return;
            var old=GUI.matrix;Rect view=Viewport();GUI.matrix=Matrix4x4.TRS(new Vector3(view.x,view.y,0),Quaternion.identity,new Vector3(view.width/420,view.height/900,1));
            DrawGame();if(paused)DrawMenu();if(notice.Length>0&&Time.realtimeSinceStartupAsDouble<noticeUntil){Panel(22,779,376,39,9,"#315549","#315549");Label(notice,30,782,360,32,10,C("#FFF9E8"),TextAnchor.MiddleCenter);}GUI.matrix=old;
        }
        private void OnDestroy(){if(art!=null)art.Dispose();}
    }
}
