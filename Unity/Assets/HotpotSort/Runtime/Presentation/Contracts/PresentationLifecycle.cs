using System;
using System.Collections.Generic;

namespace HotpotSort.Presentation
{
    // No layout or aesthetic defaults: Visual owns populated themes and anchor positions.
    [Serializable] public struct PresentationPoint { public float x,y; public PresentationPoint(float x,float y){this.x=x;this.y=y;} }
    [Serializable] public sealed class PresentationAnchors
    {
        public PresentationPoint[] pots=new PresentationPoint[4],bufferSlots=new PresentationPoint[5],gatherItemSlots=new PresentationPoint[5];
        public PresentationPoint revivalGatherPlate,supplyEntryVisual;
        public float boardWidth=420,boardHeight=900,clipTop=292,clipBottom=828,supplyGate=140;
    }
    public enum UIRole { Panel,Button,Order,Progress,Timer,BottomBar,Reward,Revival,Entry,Win,Failure,Settings,FriendBoard }
    [Serializable] public sealed class ThemeAsset { public string key,resourceAddress; }
    [Serializable] public sealed class ThemeFoodUv { public int id; public float x,y,width,height; }
    // Pixel rect, bottom-left origin; borders follow Unity left/bottom/right/top order.
    [Serializable] public sealed class ThemeSlice { public string key; public float x,y,width,height,left,bottom,right,top; }
    [Serializable] public sealed class ThemeColor { public string key; public float r,g,b,a; }
    [Serializable] public sealed class PresentationEffects
    {
        public string schemaVersion="presentation_effects_v1";
        public string[] steamKeys=new[]{"steam"};
        public float steamInterval=.85f,steamStagger=.19f,steamLifetime=1.05f,steamOpacity=.22f,steamDrift=10f,steamRise=30f,steamSize=76f;
        public float serveLift=.16f,serveHold=.10f,serveExit=.24f,serveSettle=.45f,serveExitDistance=360f;
        public int concurrentEffects=64;
    }
    // Pure presentation scheduler: deterministic variation never consumes the game's random stream.
    public sealed class PotAmbientSchedule
    {
        readonly double[] next=new double[4];
        readonly int[] sequence=new int[4];
        public void Reset(){Array.Clear(next,0,4);Array.Clear(sequence,0,4);}
        public bool TryEmit(int slot,double elapsed,bool enabled,bool serving,PresentationEffects settings,out int variant,out float drift)
        {
            variant=0;drift=0;if(slot<0||slot>=4)return false;
            // Half-life overlap pairs sin-squared envelopes into constant coverage.
            double interval=Math.Max(.05,settings.steamLifetime*.5);
            if(!enabled||serving){next[slot]=elapsed+interval+slot*Math.Max(.01,settings.steamStagger);return false;}
            if(next[slot]==0)next[slot]=interval+slot*Math.Max(.01,settings.steamStagger);
            if(elapsed<next[slot])return false;
            int step=sequence[slot]++;
            variant=(slot+step)%Math.Max(1,settings.steamKeys?.Length??0);
            drift=(((slot*7+step*11)%9)-4)/4f;
            // Preserve the deadline's fractional remainder; never accumulate frame drift.
            next[slot]+=interval;
            if(next[slot]<=elapsed)next[slot]=elapsed+interval;
            return true;
        }
    }
    [Serializable] public sealed class PresentationTheme
    {
        public string schemaVersion="presentation_theme_v7",resourceRoot;
        public ThemeAsset[] assets=new ThemeAsset[0];
        public ThemeFoodUv[] foodUvs=new ThemeFoodUv[0];
        public ThemeSlice[] slices=new ThemeSlice[0];
        public ThemeColor[] colors=new ThemeColor[0];
        public PresentationAnchors anchors=new PresentationAnchors();
        public PresentationEffects effects=new PresentationEffects();
        public string Resolve(string key)
        {
            foreach(var asset in assets)if(asset.key==key)return asset.resourceAddress;
            return null;
        }
    }
    public static class AssetKey
    {
        public const string Table="background.table",EdgeCloth="background.edge_cloth",Plate="plate.main",BufferDish="dish.buffer";
        public const string PotBody="pot.body",PotBroth="pot.broth",PotRim="pot.rim",PotUnlit="pot.unlit";
        public const string Panel="ui.panel",Button="ui.button",Order="ui.order",Progress="ui.progress",Timer="ui.timer",BottomBar="ui.bottom_bar";
        public const string Entry="hero.entry",Win="hero.win",Share="hero.share";
        public static string Food(int id){if(id<0||id>15)throw new ArgumentOutOfRangeException(nameof(id));return "food."+id.ToString("00",System.Globalization.CultureInfo.InvariantCulture);}
    }
    // The owner resets this for each session generation. Old callbacks cannot restart it.
    public sealed class VisualClock
    {
        string sessionId;
        long generation;
        bool cancelled=true;
        public double Elapsed { get; private set; }
        public void Reset(string sessionId,long generation){this.sessionId=sessionId;this.generation=generation;cancelled=false;Elapsed=0;}
        public void Cancel(){cancelled=true;}
        public double Advance(double seconds,ViewSnapshot snapshot,bool revivalTransfer=false)
        {
            if(cancelled||snapshot==null||snapshot.sessionId!=sessionId||snapshot.sessionGeneration!=generation||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return 0;
            bool allowed=revivalTransfer
                ?snapshot.revivalPending&&snapshot.revivalUsed&&snapshot.revivalTransfer!=null&&snapshot.pauseReasons==ViewPauseReasons.Revival
                :snapshot.phase==ViewPhase.Running&&snapshot.pauseReasons==ViewPauseReasons.None;
            if(!allowed)return 0;Elapsed+=seconds;return seconds;
        }
    }
    public sealed class PresentationEventCursor
    {
        string sessionId;
        long generation,lastSequence;
        public void Reset(string sessionId,long generation){this.sessionId=sessionId;this.generation=generation;lastSequence=0;}
        public bool TryAccept(ViewEvent value)
        {
            if(value==null||sessionId==null||value.sessionId!=sessionId||value.sessionGeneration!=generation||value.sequence<=lastSequence)return false;
            lastSequence=value.sequence;return true;
        }
        public void Cancel(){sessionId=null;}
    }
}
