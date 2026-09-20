using System;
using System.Collections.Generic;
using System.Linq;
namespace HotpotSort.Core
{
    [Serializable] public sealed class PlateBody { public int id; public double r,x,y,vx,vy; }
    public struct BoardPoint { public double x,y,r; public BoardPoint(double x,double y,double r=0){this.x=x;this.y=y;this.r=r;} }
    public sealed class PlateHit { public Plate plate; public Token item; public BoardPoint point; }
    /// <summary>Fixed-step circular packing; parameters intentionally approximate the original layout.</summary>
    public sealed class PlateWorld
    {
        public readonly Rules Rules; public readonly XorShift32 Random;
        public readonly List<PlateBody> Bodies=new List<PlateBody>();
        public int Spawned {get;private set;} public int Steps {get;private set;} public bool Blocked {get;private set;}
        private double timer;
        public PlateWorld(Rules rules,uint seed){Rules=rules;Random=new XorShift32(seed^0x9e3779b9u);}
        private static double Distance(double x,double y){return Math.Sqrt(x*x+y*y);}
        public void Sync(GameSession game){var ids=new HashSet<int>(game.Active.Select(p=>p.id));Bodies.RemoveAll(b=>!ids.Contains(b.id));}
        public PlateBody Proposed(GameSession game)
        {
            if(game.Pending.Count==0)return null;var p=game.Pending[0];double r=Rules.plateRadii[p.capacity];double[] anchors={86,334,210};
            return new PlateBody{id=p.id,r=r,x=anchors[Spawned%3],y=Rules.playTop+r+2};
        }
        public bool CanSpawn(GameSession game)
        {
            var c=Proposed(game);return c!=null&&game.Status=="playing"&&!Bodies.Any(b=>b.y<Rules.spawnGate)&&!Bodies.Any(b=>Distance(b.x-c.x,b.y-c.y)<b.r+c.r+3);
        }
        public bool Spawn(GameSession game)
        {
            if(!CanSpawn(game))return false;var c=Proposed(game);if(game.Spawn()==null)return false;
            double jitter=(Random.Next()-.5)*10;if(!Bodies.Any(b=>Distance(b.x-c.x-jitter,b.y-c.y)<b.r+c.r+2))c.x+=jitter;
            c.vx=(Random.Next()-.5)*8;c.vy=24;Bodies.Add(c);Spawned++;return true;
        }
        public void Step(GameSession game,double dt)
        {
            if(game.Status!="playing")return;dt=Math.Max(0,Math.Min(dt,1.0/30));Steps++;Sync(game);timer+=dt;
            if(timer>=Rules.spawnInterval){Blocked=!CanSpawn(game);if(!Blocked&&Spawn(game))timer=0;else timer=Rules.spawnInterval;}
            foreach(var b in Bodies){b.vy+=Rules.gravity*dt;b.vx*=Math.Pow(.994,dt*120);b.x+=b.vx*dt;b.y+=b.vy*dt;}
            for(int iteration=0;iteration<9;iteration++)
            {
                foreach(var b in Bodies)
                {
                    if(b.x-b.r<12){b.x=12+b.r;b.vx=Math.Abs(b.vx)*.04;}
                    if(b.x+b.r>Rules.width-12){b.x=Rules.width-12-b.r;b.vx=-Math.Abs(b.vx)*.04;}
                    if(b.y+b.r>Rules.playBottom){b.y=Rules.playBottom-b.r;if(b.vy>0)b.vy*=-.015;b.vx*=.90;}
                    if(b.y-b.r<Rules.playTop){b.y=Rules.playTop+b.r;if(b.vy<0)b.vy=0;}
                }
                for(int i=0;i<Bodies.Count;i++)for(int j=i+1;j<Bodies.Count;j++)
                {
                    var a=Bodies[i];var b=Bodies[j];double dx=b.x-a.x,dy=b.y-a.y,dist=Distance(dx,dy),limit=a.r+b.r+1;
                    if(dist>=limit)continue;if(dist<.0001){dx=.0001;dy=0;dist=.0001;}
                    double nx=dx/dist,ny=dy/dist,penetration=limit-dist,invA=1/(a.r*a.r),invB=1/(b.r*b.r),ra=invA/(invA+invB),rb=1-ra;
                    a.x-=nx*penetration*ra;a.y-=ny*penetration*ra;b.x+=nx*penetration*rb;b.y+=ny*penetration*rb;
                    double vn=(b.vx-a.vx)*nx+(b.vy-a.vy)*ny;
                    if(vn<0){double impulse=-vn*1.02;a.vx-=nx*impulse*ra;a.vy-=ny*impulse*ra;b.vx+=nx*impulse*rb;b.vy+=ny*impulse*rb;}
                    double tangent=(b.vx-a.vx)*(-ny)+(b.vy-a.vy)*nx;
                    a.vx+=(-ny)*tangent*.03;a.vy+=nx*tangent*.03;b.vx-=(-ny)*tangent*.03;b.vy-=nx*tangent*.03;
                }
            }
        }
        public static BoardPoint[] Offsets(int n,double r)
        {
            double[,] xy=n==1?new double[,]{{0,0}}:n==2?new double[,]{{-.46,0},{.46,0}}:n==3?new double[,]{{0,-.46},{-.43,.30},{.43,.30}}:n==4?new double[,]{{-.38,-.38},{.38,-.38},{-.38,.38},{.38,.38}}:new double[,]{{0,0},{-.49,-.40},{.49,-.40},{-.49,.40},{.49,.40}};
            var result=new BoardPoint[n];for(int i=0;i<n;i++)result[i]=new BoardPoint(xy[i,0]*r,xy[i,1]*r);return result;
        }
        public BoardPoint Position(Plate p,Token token)
        {
            var b=Bodies.FirstOrDefault(b=>b.id==p.id);if(b==null)throw new InvalidOperationException("Missing plate body");var o=Offsets(p.capacity,b.r)[token.slot];return new BoardPoint(b.x+o.x,b.y+o.y,p.capacity==5?15.5:17);
        }
        public PlateHit Hit(GameSession game,double x,double y)
        {
            if(y<Rules.playTop||y>Rules.playBottom)return null;
            for(int i=Bodies.Count-1;i>=0;i--)
            {
                var b=Bodies[i];if(Distance(x-b.x,y-b.y)>b.r)continue;var p=game.Active.FirstOrDefault(p=>p.id==b.id);if(p==null)continue;
                for(int j=p.items.Count-1;j>=0;j--){var t=p.items[j];var q=Position(p,t);if(Distance(x-q.x,y-q.y)<=q.r+3)return new PlateHit{plate=p,item=t,point=q};}return null;
            }
            return null;
        }
        public void Audit(GameSession game)
        {
            Sync(game);if(Bodies.Count!=game.Active.Count)throw new InvalidOperationException("Physics/model mismatch");
            foreach(var b in Bodies)if(new[]{b.x,b.y,b.vx,b.vy}.Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidOperationException("Non-finite physics");
        }
    }
}
