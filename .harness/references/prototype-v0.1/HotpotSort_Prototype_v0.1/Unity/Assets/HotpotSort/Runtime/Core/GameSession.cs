using System;
using System.Collections.Generic;
using System.Linq;
namespace HotpotSort.Core
{
    /// <summary>Pure C# domain model. A tap is one atomic settlement transaction.
    /// Motion/animations never own gameplay tokens. Not the APK's recovered source.</summary>
    public sealed class GameSession
    {
        public const int KindCount=16;
        public readonly GameData Data; public readonly Rules Rules; public readonly LevelDef Level;
        public readonly uint Seed; public readonly XorShift32 Random;
        public string Status {get;private set;}="playing";
        public string FailureReason {get;private set;}="";
        public int Moves {get;private set;} public int Completed {get;private set;} public int Total {get;private set;}
        public readonly List<Plate> Pending=new List<Plate>(), Active=new List<Plate>();
        public readonly Token[] Buffer; public readonly Order[] Orders;
        public readonly int[] InitialByKind, CompletedByKind=new int[KindCount];
        public readonly List<Command> Commands=new List<Command>(); public readonly List<GameEvent> Log=new List<GameEvent>();
        private readonly List<GameEvent> events=new List<GameEvent>(); private int sequence;
        public GameSession(GameData data,string levelId="A",uint seed=7,bool? failOnFull=null)
        {
            if(data==null||data.schemaVersion!=1)throw new ArgumentException("Unsupported data schema");
            Data=data;Rules=data.rules.Copy();if(failOnFull.HasValue)Rules.failOnFull=failOnFull.Value;
            if(Rules.bufferCapacity<1||Rules.orderSize!=3||Rules.openOrderSlots<1||Rules.openOrderSlots>Rules.orderSlots)throw new ArgumentException("Invalid rules");
            Level=data.levels.FirstOrDefault(l=>l.id==levelId);if(Level==null)throw new ArgumentException("Unknown level: "+levelId);
            Seed=seed==0?1:seed;Random=new XorShift32(Seed);Buffer=new Token[Rules.bufferCapacity];
            Orders=Enumerable.Range(0,Rules.orderSlots).Select(i=>new Order{slot=i,open=i<Rules.openOrderSlots}).ToArray();
            int next=1;
            for(int i=0;i<Level.plates.Length;i++)
            {
                string str=Level.plates[i];if(string.IsNullOrEmpty(str)||str.Length>5||str.Any(c=>c<'A'||c>'P'))throw new ArgumentException("Bad plate "+i);
                var plate=new Plate{id=i+1,capacity=str.Length};for(int j=0;j<str.Length;j++)plate.items.Add(new Token(next++,str[j]-'A',j));Pending.Add(plate);
            }
            InitialByKind=Count(Pending.SelectMany(p=>p.items));Total=InitialByKind.Sum();
            if(Total==0||InitialByKind.Any(n=>n%3!=0))throw new ArgumentException("Each ingredient count must be a multiple of 3");
            Emit(new GameEvent{type="start",level=levelId,seed=Seed,total=Total,failOnFull=Rules.failOnFull});
            foreach(var order in Orders)if(order.open)Assign(order,true);Audit();
        }
        public static int[] Count(IEnumerable<Token> tokens){var c=new int[KindCount];foreach(var t in tokens)if(t!=null)c[t.kind]++;return c;}
        public IEnumerable<Token> AllLoose(){return Pending.Concat(Active).SelectMany(p=>p.items).Concat(Buffer.Where(t=>t!=null));}
        public int[] Demand(int except=-1){var c=new int[KindCount];foreach(var o in Orders)if(o.open&&o.kind>=0&&o.slot!=except)c[o.kind]+=Rules.orderSize-o.items.Count;return c;}
        public int[] RemainingAvailable(int except=-1){var c=Count(AllLoose());var d=Demand(except);for(int i=0;i<KindCount;i++)c[i]-=d[i];return c;}
        private void Emit(GameEvent e){e.seq=sequence++;Log.Add(e);events.Add(e);}
        public GameEvent[] DrainEvents(){var result=events.ToArray();events.Clear();return result;}
        public Selection PickOpening(int slot)
        {
            int[] d=Demand(slot),available=RemainingAvailable(slot),c=Count(Pending.Take(Rules.openingLookahead).SelectMany(p=>p.items));
            for(int i=0;i<KindCount;i++)c[i]-=d[i];
            var candidates=Enumerable.Range(0,KindCount).Where(k=>c[k]>0&&available[k]>=3).Select(k=>new Candidate{kind=k,count=c[k]}).ToList();
            if(candidates.Count==0){c=Count(AllLoose());for(int i=0;i<KindCount;i++)c[i]-=d[i];candidates=Enumerable.Range(0,KindCount).Where(k=>c[k]>0&&available[k]>=3).Select(k=>new Candidate{kind=k,count=c[k]}).ToList();}
            var other=new HashSet<int>(Orders.Where(o=>o.open&&o.kind>=0&&o.slot!=slot).Select(o=>o.kind));
            var distinct=candidates.Where(x=>!other.Contains(x.kind)).ToList();if(distinct.Count>0)candidates=distinct;
            candidates=candidates.OrderByDescending(x=>x.count).ThenBy(x=>x.kind).ToList();
            return new Selection{kind=candidates.Count>0?candidates[0].kind:-1,reason="opening-prefix-max",candidates=candidates.ToArray()};
        }
        public DifficultyRow SelectWeightRow()
        {
            double progress=(Completed+Orders.Sum(o=>o.items.Count))/(double)Total;int temp=Math.Min(4,Buffer.Count(t=>t!=null));
            var rows=Data.difficultyRows.Where(r=>r.difficulty==Level.difficulty&&r.temp==temp).OrderBy(r=>r.progress).ToArray();
            return rows.FirstOrDefault(r=>progress<=r.progress+1e-8)??rows.LastOrDefault()??new DifficultyRow{progress=1,temp=temp,weights=new double[]{0,1,0,0,0}};
        }
        /// <summary>Documented proxy cost. Active means spawned, not a geometric accessibility proof.</summary>
        public List<Candidate> CandidateCosts(int slot)
        {
            var demand=Demand(slot);var available=RemainingAvailable(slot);var buffer=Count(Buffer);var active=Count(Active.SelectMany(p=>p.items));
            int free=Buffer.Count(t=>t==null);var candidates=new List<Candidate>();
            for(int kind=0;kind<KindCount;kind++)
            {
                if(available[kind]<3)continue;
                int reserve=demand[kind],usableB=Math.Max(0,buffer[kind]-reserve);reserve=Math.Max(0,reserve-buffer[kind]);
                int usableA=Math.Max(0,active[kind]-reserve);reserve=Math.Max(0,reserve-active[kind]);int cost;
                if(usableB+usableA>=3)cost=-Math.Min(3,usableB);
                else
                {
                    int need=3-usableB-usableA,foreign=0;bool done=false;
                    foreach(var p in Pending){foreach(var t in p.items){if(t.kind==kind){if(reserve>0)reserve--;else need--;if(need==0){done=true;break;}}else foreign++;}if(done)break;}
                    cost=Math.Max(1,Math.Min(free+1,foreign));
                }
                candidates.Add(new Candidate{kind=kind,cost=cost,buffer=usableB,active=usableA,groups=new[]{cost<0,cost==0,cost>=1&&cost<free-1,cost==free-1,cost==free||cost==free+1}});
            }
            var other=new HashSet<int>(Orders.Where(o=>o.open&&o.kind>=0&&o.slot!=slot).Select(o=>o.kind));
            var distinct=candidates.Where(c=>!other.Contains(c.kind)).ToList();return distinct.Count>0?distinct:candidates;
        }
        public Selection PickRefill(int slot)
        {
            var candidates=CandidateCosts(slot);if(candidates.Count==0)return new Selection{kind=-1,reason="no-unreserved-triple"};
            var row=SelectWeightRow();double roll=Random.Next()*row.weights.Sum();int group=row.weights.Length-1;
            for(int i=0;i<row.weights.Length;i++){roll-=row.weights[i];if(roll<0){group=i;break;}}
            var pool=candidates.Where(c=>c.groups[group]).ToList();bool fallback=pool.Count==0;
            if(fallback){int low=candidates.Min(c=>c.cost);pool=candidates.Where(c=>c.cost==low).ToList();}
            pool=pool.OrderBy(c=>c.kind).ToList();var selected=pool[Random.Int(pool.Count)];
            return new Selection{kind=selected.kind,reason=fallback?"category-empty-min-cost":"weighted-category",requestedGroup=group+1,cost=selected.cost,threshold=row.progress,temp=row.temp,candidates=candidates.ToArray()};
        }
        private void Assign(Order order,bool opening=false)
        {
            var p=opening?PickOpening(order.slot):PickRefill(order.slot);order.kind=p.kind;
            Emit(new GameEvent{type="target",slot=order.slot,kind=p.kind,reason=p.reason,requestedGroup=p.requestedGroup,cost=p.cost,temp=p.temp,threshold=p.threshold,candidates=p.candidates});
        }
        public Plate Spawn()
        {
            if(Status!="playing"||Pending.Count==0)return null;
            var p=Pending[0];Pending.RemoveAt(0);Active.Add(p);Commands.Add(new Command("spawn"));Emit(new GameEvent{type="spawn",plate=p.id});Audit();return p;
        }
        public Order FindTarget(int kind){return Orders.Where(o=>o.open&&o.kind==kind&&o.items.Count<Rules.orderSize).OrderByDescending(o=>o.items.Count).ThenBy(o=>o.slot).FirstOrDefault();}
        public bool Tap(int id)
        {
            if(Status!="playing")return false;
            var plate=Active.FirstOrDefault(p=>p.items.Any(t=>t.id==id));if(plate==null)return false;
            var item=plate.items.First(t=>t.id==id);var target=FindTarget(item.kind);int empty=Array.FindIndex(Buffer,t=>t==null);
            if(target==null&&empty<0)
            {
                if(!Rules.failOnFull){Commands.Add(new Command("tap",id));Status="lost";FailureReason="buffer-overflow-attempt";Emit(new GameEvent{type="lost",reason=FailureReason});}
                return false;
            }
            Commands.Add(new Command("tap",id));Moves++;plate.items.Remove(item);
            if(target!=null){target.items.Add(item);Emit(new GameEvent{type="move",id=id,kind=item.kind,from="plate",plate=plate.id,to="order",slot=target.slot});}
            else{Buffer[empty]=item;Emit(new GameEvent{type="move",id=id,kind=item.kind,from="plate",plate=plate.id,to="buffer",slot=empty});}
            if(plate.items.Count==0){Active.Remove(plate);Emit(new GameEvent{type="plate-empty",plate=plate.id});}
            Settle();Audit();return true;
        }
        public void Settle()
        {
            int guard=0;bool changed=true;
            while(changed)
            {
                if(++guard>Total*4+32)throw new InvalidOperationException("Settlement did not converge");changed=false;
                foreach(var o in Orders)if(o.open&&o.items.Count==Rules.orderSize)
                {
                    int kind=o.kind;Completed+=o.items.Count;CompletedByKind[kind]+=o.items.Count;
                    Emit(new GameEvent{type="complete",slot=o.slot,kind=kind,ids=o.items.Select(t=>t.id).ToArray()});o.items.Clear();o.kind=-1;Assign(o);changed=true;
                }
                for(int i=0;i<Buffer.Length;i++)
                {
                    var token=Buffer[i];if(token==null)continue;var target=FindTarget(token.kind);if(target==null)continue;
                    Buffer[i]=null;target.items.Add(token);Emit(new GameEvent{type="move",id=token.id,kind=token.kind,from="buffer",fromSlot=i,to="order",slot=target.slot});changed=true;
                }
            }
            if(Completed==Total){Status="won";Emit(new GameEvent{type="won",moves=Moves});}
            else if(Rules.failOnFull&&Buffer.All(t=>t!=null)){Status="lost";FailureReason="buffer-full-after-settlement";Emit(new GameEvent{type="lost",reason=FailureReason});}
        }
        public void Audit()
        {
            var loose=AllLoose().ToArray();var live=loose.Concat(Orders.SelectMany(o=>o.items)).ToArray();var c=Count(live);
            if(live.Select(t=>t.id).Distinct().Count()!=live.Length)throw new InvalidOperationException("Duplicate token");
            for(int k=0;k<KindCount;k++)if(c[k]+CompletedByKind[k]!=InitialByKind[k])throw new InvalidOperationException("Conservation failure, kind "+k);
            if(Buffer.Length!=Rules.bufferCapacity||Orders.Count(o=>o.open)!=Rules.openOrderSlots)throw new InvalidOperationException("Capacity changed");
            foreach(var o in Orders)if(o.items.Count>Rules.orderSize||o.items.Any(t=>t.kind!=o.kind)||(!o.open&&(o.kind>=0||o.items.Count>0)))throw new InvalidOperationException("Invalid order");
            var supply=Count(loose);var demand=Demand();for(int k=0;k<KindCount;k++)if(supply[k]<demand[k])throw new InvalidOperationException("Impossible reservation "+k);
            if(Status=="won"&&(live.Length>0||Completed!=Total))throw new InvalidOperationException("Premature victory");
        }
        public GameSnapshot Snapshot()
        {
            return new GameSnapshot{level=Level.id,seed=Seed,rng=Random.State,status=Status,reason=FailureReason,moves=Moves,total=Total,completed=Completed,
                pending=Pending.Select(p=>new PlateSnapshot{id=p.id,items=p.items.Select(t=>t.id).ToArray()}).ToArray(),
                active=Active.Select(p=>new PlateSnapshot{id=p.id,items=p.items.Select(t=>t.id).ToArray()}).ToArray(),
                buffer=Buffer.Select(t=>t==null?0:t.id).ToArray(),completedByKind=(int[])CompletedByKind.Clone(),
                orders=Orders.Select(o=>new OrderSnapshot{slot=o.slot,open=o.open,kind=o.kind,ids=o.items.Select(t=>t.id).ToArray()}).ToArray()};
        }
        public ReplayRecord ExportReplay(){return new ReplayRecord{level=Level.id,seed=Seed,failOnFull=Rules.failOnFull,commands=Commands.ToArray(),final=Snapshot(),log=Log.ToArray()};}
        public static GameSession Replay(GameData data,ReplayRecord record)
        {
            if(record==null||record.schemaVersion!=1||record.commands==null||record.commands.Length>100000)throw new ArgumentException("Invalid replay");
            var game=new GameSession(data,record.level,record.seed,record.failOnFull);
            foreach(var c in record.commands){if(c.type=="spawn"){if(game.Spawn()==null)throw new InvalidOperationException("Invalid spawn");}else if(c.type=="tap"){if(!game.Tap(c.id)&&game.Status!="lost")throw new InvalidOperationException("Invalid tap");}else throw new InvalidOperationException("Unknown command");}
            game.Audit();return game;
        }
    }
}
