using System;
using System.Collections.Generic;

namespace HotpotSort.Presentation
{
    public static class PresentationThemeValidation
    {
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static bool Unit(float n)=>Finite(n)&&n>=0&&n<=1;
        public static string[] Validate(PresentationTheme theme,string root)
        {
            var errors=new List<string>();
            if(theme==null){errors.Add("Missing presentation theme");return errors.ToArray();}
            if(!PresentationAssets.IsThemeRoot(root)||theme.resourceRoot!=root)errors.Add("Theme/root mismatch");
            if(theme.schemaVersion!="presentation_theme_v7"&&theme.schemaVersion!="presentation_theme_v10")errors.Add("Unsupported theme schema");
            if(root==PresentationAssets.CandidateRoot)for(int i=0;i<AssetKey.BrothKeys.Length;i++)if(theme.Resolve(AssetKey.BrothKeys[i])!=AssetKey.BrothResources[i])errors.Add("Invalid local broth asset: "+AssetKey.BrothKeys[i]);
            var keys=new HashSet<string>();
            foreach(var a in theme.assets??Array.Empty<ThemeAsset>())
            {
                if(a==null||string.IsNullOrEmpty(a.key)||!keys.Add(a.key)){errors.Add("Invalid/duplicate asset key");continue;}
                if(string.IsNullOrEmpty(a.resourceAddress)||!a.resourceAddress.StartsWith(root+"/",StringComparison.Ordinal)||a.resourceAddress.Contains("..")||a.resourceAddress.Contains("\\"))errors.Add("Invalid theme address: "+a.key);
            }
            foreach(string key in new[]{AssetKey.Table,AssetKey.EdgeCloth,AssetKey.Plate,AssetKey.BufferDish,AssetKey.PotBody,AssetKey.PotUnlit,AssetKey.PotBroth,AssetKey.PotRim})
                if(!keys.Contains(key))errors.Add("Missing theme key: "+key);
            int foodCount=root==PresentationAssets.CandidateRoot?32:16;
            for(int i=0;i<foodCount;i++)if(!keys.Contains(AssetKey.Food(i)))errors.Add("Missing theme food: "+i);
            var ids=new HashSet<int>();
            foreach(var uv in theme.foodUvs??Array.Empty<ThemeFoodUv>())
                if(uv==null||uv.id<0||uv.id>=foodCount||!ids.Add(uv.id)||!Unit(uv.x)||!Unit(uv.y)||!Unit(uv.width)||!Unit(uv.height)||uv.width<=0||uv.height<=0||uv.x+uv.width>1.00001f||uv.y+uv.height>1.00001f)errors.Add("Invalid/duplicate food UV");
            if(root!=V7Art.Root&&ids.Count!=foodCount)errors.Add("Versioned theme requires all food UVs");
            var sliceKeys=new HashSet<string>();
            foreach(var s in theme.slices??Array.Empty<ThemeSlice>())
            {
                if(s==null||string.IsNullOrEmpty(s.key)||!keys.Contains(s.key)||!sliceKeys.Add(s.key)){errors.Add("Invalid/duplicate slice key");continue;}
                if(!Finite(s.x)||!Finite(s.y)||!Finite(s.width)||!Finite(s.height)||!Finite(s.left)||!Finite(s.bottom)||!Finite(s.right)||!Finite(s.top)||s.x<0||s.y<0||s.width<=0||s.height<=0||s.left<0||s.bottom<0||s.right<0||s.top<0||s.left+s.right>s.width||s.bottom+s.top>s.height)errors.Add("Invalid slice: "+s.key);
            }
            var colorKeys=new HashSet<string>();
            foreach(var c in theme.colors??Array.Empty<ThemeColor>())
                if(c==null||string.IsNullOrEmpty(c.key)||!colorKeys.Add(c.key)||!Unit(c.r)||!Unit(c.g)||!Unit(c.b)||!Unit(c.a))errors.Add("Invalid/duplicate theme color");
            if(theme.anchors==null||theme.anchors.pots==null||theme.anchors.pots.Length!=4||theme.anchors.bufferSlots==null||theme.anchors.bufferSlots.Length!=5)errors.Add("Theme requires four pot and five buffer anchors");
            return errors.ToArray();
        }
    }
}
