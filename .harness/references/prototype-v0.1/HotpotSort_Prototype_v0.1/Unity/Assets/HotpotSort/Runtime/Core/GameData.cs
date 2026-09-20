using System;
using System.Collections.Generic;
namespace HotpotSort.Core
{
    [Serializable] public sealed class GameData
    {
        public int schemaVersion; public string title; public string prototypeVersion;
        public Rules rules; public IngredientDef[] ingredients; public LevelDef[] levels; public DifficultyRow[] difficultyRows;
    }
    [Serializable] public sealed class Rules
    {
        public int bufferCapacity = 5, orderSlots = 4, openOrderSlots = 2, orderSize = 3, openingLookahead = 10;
        public bool failOnFull = true;
        public double width = 420, height = 900, playTop = 304, playBottom = 828, spawnGate = 377, spawnInterval = .2, gravity = 960, physicsStep = 1.0/120;
        public double[] plateRadii = {0,33,40,46,52,57};
        public Rules Copy() { var r=(Rules)MemberwiseClone();r.plateRadii=(double[])plateRadii.Clone();return r; }
    }
    [Serializable] public sealed class IngredientDef { public int id; public string symbol, name, color; }
    [Serializable] public sealed class LevelDef { public string id, name; public int difficulty, sourceLevel; public string[] plates; }
    [Serializable] public sealed class DifficultyRow { public int difficulty, temp; public double progress; public double[] weights; }
    [Serializable] public sealed class Token { public int id, kind, slot; public Token(int id,int kind,int slot){this.id=id;this.kind=kind;this.slot=slot;} }
    [Serializable] public sealed class Plate { public int id, capacity; public List<Token> items=new List<Token>(); }
    [Serializable] public sealed class Order { public int slot,kind=-1; public bool open; public List<Token> items=new List<Token>(); }
    [Serializable] public sealed class Candidate { public int kind, count, cost, buffer, active; public bool[] groups; }
    [Serializable] public sealed class Selection { public int kind=-1, requestedGroup, cost, temp; public double threshold; public string reason; public Candidate[] candidates; }
    [Serializable] public sealed class GameEvent
    {
        public int seq, id, kind=-1, slot, plate, fromSlot, total, moves, requestedGroup, cost, temp;
        public uint seed; public string type, level, from, to, reason; public bool failOnFull;
        public int[] ids; public Candidate[] candidates; public double threshold;
    }
    [Serializable] public sealed class Command { public string type; public int id; public Command(string type,int id=0){this.type=type;this.id=id;} }
    [Serializable] public sealed class PlateSnapshot { public int id; public int[] items; }
    [Serializable] public sealed class OrderSnapshot { public int slot,kind; public bool open; public int[] ids; }
    [Serializable] public sealed class GameSnapshot
    {
        public string level,status,reason; public uint seed,rng; public int moves,total,completed;
        public PlateSnapshot[] pending,active; public int[] buffer,completedByKind; public OrderSnapshot[] orders;
        // The Unity snapshot uses 0 for an empty buffer cell (token IDs start at 1).
    }
    [Serializable] public sealed class ReplayRecord
    {
        public int schemaVersion=1; public string version="0.1.0",level; public uint seed; public bool failOnFull;
        public Command[] commands; public GameSnapshot final; public GameEvent[] log;
    }
    public sealed class XorShift32
    {
        public uint State { get; private set; }
        public XorShift32(uint seed){State=seed==0?1:seed;}
        public double Next(){uint x=State;unchecked{x^=x<<13;x^=x>>17;x^=x<<5;}State=x;return x/4294967296.0;}
        public int Int(int n){if(n<=0)throw new ArgumentOutOfRangeException(nameof(n));return (int)Math.Floor(Next()*n);}
    }
}
