using System;
using System.Collections.Generic;
using UnityEngine;
using HotpotSort.Contracts.RemoteAssets;
using System.Threading.Tasks;

namespace HotpotSort.Presentation
{
    // One installed provider for every presentation consumer. No remote Resources fallback.
    public static class PresentationAssets
    {
        static IPresentationAssetProvider provider;
        public static IPresentationAssetProvider Provider
        {
            get
            {
#if UNITY_EDITOR || !UNITY_WEBGL
                if(provider==null)provider=new LocalPresentationAssetProvider();
#endif
                return provider??throw new AssetFailure(AssetError.NotConfigured);
            }
        }
        public static void Install(IPresentationAssetProvider value){provider=value??throw new ArgumentNullException(nameof(value));}
        public static void Clear(IPresentationAssetProvider expected){if(ReferenceEquals(provider,expected))provider=null;}
        public static bool IsRemote(string path)
        {
            if(path==null)return false;
            var parts=path.Split('/');
            if(parts.Length<5||!IsThemeRoot(string.Join("/",parts,0,4)))return false;
            string relative=string.Join("/",parts,4,parts.Length-4);
            return relative.StartsWith("food/")||relative.StartsWith("containers/")||relative.StartsWith("pots/")||relative.StartsWith("fx/")||relative=="hero/win";
        }
        public const string CandidateRoot="Hotpot/TASK001/v10/r001";
        public static bool IsThemeRoot(string root)
        {
            if(string.IsNullOrEmpty(root))return false;
            var p=root.Split('/');
            return p.Length==4&&p[0]=="Hotpot"&&p[1]=="TASK001"&&VersionPart(p[2],'v')&&VersionPart(p[3],'r');
        }
        static bool VersionPart(string value,char prefix)
        {if(value.Length<2||value[0]!=prefix)return false;for(int i=1;i<value.Length;i++)if(value[i]<'0'||value[i]>'9')return false;return true;}
        public static T Load<T>(string path) where T:UnityEngine.Object=>IsRemote(path)?Provider.GetComplete<T>(path):Provider.GetLocal<T>(path);
    }
    public sealed class LocalPresentationAssetProvider:IPresentationAssetProvider
    {
        bool disposed;
        public AssetPreparationSnapshot Snapshot {get;private set;}=new AssetPreparationSnapshot(0,"local-complete",AssetReadiness.Ready,AssetError.None,0,0,0);
        public event Action<AssetPreparationSnapshot> Changed {add{}remove{}}
        public Task<AssetPreparationSnapshot> PrepareAsync()=>Task.FromResult(Snapshot);
        public Task<AssetPreparationSnapshot> RetryAsync()=>PrepareAsync();
        public void Cancel(){}
        public T GetLocal<T>(string path) where T:class
        {if(PresentationAssets.IsRemote(path))throw new AssetFailure(AssetError.MissingAsset);return Load<T>(path);}
        T Load<T>(string path) where T:class
        {
            if(disposed||string.IsNullOrEmpty(path)||!path.StartsWith("Hotpot/",StringComparison.Ordinal)||path.Contains(".."))throw new AssetFailure(AssetError.MissingAsset);
            var value=Resources.Load(path,typeof(T));if(!value||!(value is T typed))throw new AssetFailure(AssetError.MissingAsset);return typed;
        }
        public T GetComplete<T>(string path) where T:class=>Load<T>(path);
        public void Dispose(){disposed=true;Snapshot=new AssetPreparationSnapshot(0,"local-complete",AssetReadiness.Cancelled,AssetError.None,0,0,0);}
    }
    // Versioned visual decisions. No core/data decisions belong here.
    public sealed class V7Art
    {
        public const string Root="Hotpot/TASK001/v7/r001";
        public readonly PresentationTheme Theme;
        public static readonly Color Ivory=new Color32(255,245,229,255),Ink=new Color32(81,51,32,255),Muted=new Color32(139,112,88,255),Red=new Color32(181,55,37,255);
        public const float LandingSeconds=.1f,RevivalSeconds=.8f;
        // Normalize source transparent gutters, not physics or food placement. The hit test uses these identical UVs.
        static readonly Rect[] foodUV={
            new Rect(0.197374f,0.174913f,0.622830f,0.622830f),
            new Rect(0.183702f,0.154405f,0.661892f,0.661892f),
            new Rect(0.180990f,0.150716f,0.677083f,0.677083f),
            new Rect(0.110677f,0.134115f,0.716146f,0.716146f),
            new Rect(0.195747f,0.171332f,0.655382f,0.655382f),
            new Rect(0.199002f,0.141385f,0.707465f,0.707465f),
            new Rect(0.149848f,0.143012f,0.713976f,0.713976f),
            new Rect(0.219401f,0.212565f,0.572917f,0.572917f),
            new Rect(0.161241f,0.151476f,0.681424f,0.681424f),
            new Rect(0.183160f,0.183160f,0.633681f,0.633681f),
            new Rect(0.187500f,0.183594f,0.625000f,0.625000f),
            new Rect(0.191840f,0.184028f,0.616319f,0.616319f),
            new Rect(0.113064f,0.104275f,0.785590f,0.785590f),
            new Rect(0.143446f,0.129774f,0.724826f,0.724826f),
            new Rect(0.084635f,0.076823f,0.846354f,0.846354f),
            new Rect(0.116536f,0.117513f,0.774740f,0.774740f),
        };
        public static Rect FoodUV(int id)=>foodUV[Mathf.Clamp(id,0,15)];
        public Rect FoodUv(int id)
        {
            foreach(var uv in Theme.foodUvs??Array.Empty<ThemeFoodUv>())if(uv.id==id)return new Rect(uv.x,uv.y,uv.width,uv.height);
            if(Theme.resourceRoot==Root)return FoodUV(id);
            throw new InvalidOperationException("Missing theme food UV: "+id);
        }
        public Color ColorFor(string key,Color fallback)
        {foreach(var c in Theme.colors??Array.Empty<ThemeColor>())if(c.key==key)return new Color(c.r,c.g,c.b,c.a);return fallback;}
        readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly HashSet<string> generatedSprites=new HashSet<string>();
        struct Cut {public Rect rect;public Vector4 border;public Cut(Rect r,Vector4 b){rect=r;border=b;}}
        static readonly Dictionary<string,Cut> cuts=new Dictionary<string,Cut>{
            { "bottom_bar", new Cut(new Rect(9,60,1006,135),new Vector4(61,61,61,61)) },
            { "button_primary", new Cut(new Rect(9,76,494,105),new Vector4(48,48,48,48)) },
            { "button_secondary", new Cut(new Rect(9,76,494,105),new Vector4(48,48,48,48)) },
            { "icon_disc", new Cut(new Rect(8,10,240,236),new Vector4(0,0,0,0)) },
            { "order_card", new Cut(new Rect(9,26,495,205),new Vector4(39,22,40,23)) },
            { "order_pointer", new Cut(new Rect(8,50,240,156),new Vector4(0,0,0,0)) },
            { "panel", new Cut(new Rect(10,14,1003,998),new Vector4(118,114,117,116)) },
            { "progress_track", new Cut(new Rect(9,112,494,31),new Vector4(14,14,14,14)) },
            { "timer", new Cut(new Rect(9,62,494,131),new Vector4(59,59,59,59)) },
        };
        public V7Art(string root)
        {
            var data=PresentationAssets.Load<TextAsset>(root+"/presentation-theme");
            if(!data)throw new InvalidOperationException("Missing presentation theme: "+root);
            Theme=JsonUtility.FromJson<PresentationTheme>(data.text);
            var errors=PresentationThemeValidation.Validate(Theme,root);
            if(errors.Length>0)throw new InvalidOperationException(string.Join("; ",errors));
            // Preserve approved v7 bitmap/theme files; v8 geometry is the shared authority.
            Theme.anchors.supplyGate=PlateSupplyGeometry.Gate;
        }
        public Texture2D Texture(string key)
        {
            Texture2D value;if(textures.TryGetValue(key,out value))return value;
            var path=Theme.Resolve(key);if(string.IsNullOrEmpty(path))throw new AssetFailure(AssetError.MissingAsset);value=PresentationAssets.Load<Texture2D>(path);textures[key]=value;return value;
        }
        public Sprite Skin(string key)
        {
            Sprite value;if(sprites.TryGetValue(key,out value))return value;
            var texture=Texture(key);if(!texture)return null;Cut cut;
            if(TryCut(key,out cut))
            {
                if(cut.rect.xMin<0||cut.rect.yMin<0||cut.rect.xMax>texture.width||cut.rect.yMax>texture.height)throw new InvalidOperationException("Theme slice exceeds texture: "+key);
                value=Sprite.Create(texture,cut.rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,cut.border);generatedSprites.Add(key);
            }
            else value=PresentationAssets.Load<Sprite>(Theme.Resolve(key));
            sprites[key]=value;return value;
        }
        bool TryCut(string key,out Cut cut)
        {
            foreach(var s in Theme.slices??Array.Empty<ThemeSlice>())if(s.key==key){cut=new Cut(new Rect(s.x,s.y,s.width,s.height),new Vector4(s.left,s.bottom,s.right,s.top));return true;}
            if(Theme.resourceRoot==Root&&key.StartsWith("ui.",StringComparison.Ordinal))return cuts.TryGetValue(key.Substring(3),out cut);
            cut=default;return false;
        }
        public Vector2 Pot(int slot){var p=Theme.anchors.pots[Mathf.Clamp(slot,0,3)];return new Vector2(p.x,p.y);}
        public Vector2 Buffer(int slot){var p=Theme.anchors.bufferSlots[Mathf.Clamp(slot,0,4)];return new Vector2(p.x,p.y);}
        public void Dispose(){foreach(var pair in sprites)if(pair.Value&&generatedSprites.Contains(pair.Key))UnityEngine.Object.Destroy(pair.Value);sprites.Clear();generatedSprites.Clear();textures.Clear();}
    }
}
