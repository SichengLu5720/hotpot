using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotpotSort.Presentation
{
    public static class TaskAssetValidation
    {
        public const string ModernFontResource = "Hotpot/TASK002/v9/r001/fonts/modern-sans";
        public static Font RequireModernFont(Font font)
        {
            if (!font) throw new InvalidOperationException("Required font unavailable: " + ModernFontResource);
            return font;
        }
        public static Font LoadModernFont() => RequireModernFont(PresentationAssets.Load<Font>(ModernFontResource));
        public static string[] ValidateModernFont()
        {
            var errors = new List<string>();
            var font = PresentationAssets.Load<Font>(ModernFontResource);
            var glyphs = PresentationAssets.Load<TextAsset>("Hotpot/TASK002/v9/r001/fonts/glyphs");
            if (!font) errors.Add("Missing v9 modern sans font");
            if (!glyphs) errors.Add("Missing v9 glyph inventory");
            if (font && glyphs) foreach (char ch in glyphs.text)
                if (!char.IsControl(ch) && !font.HasCharacter(ch)) errors.Add("Missing v9 glyph U+" + ((int)ch).ToString("X4"));
            return errors.ToArray();
        }
        public static string[] Validate(string root,bool requireComplete=false)
        {
            var errors=new List<string>();if(string.IsNullOrWhiteSpace(root)){errors.Add("No formal visual batch selected");return errors.ToArray();}
            if(PresentationAssets.IsThemeRoot(root))
            {
                try
                {
                var themeFile=PresentationAssets.Load<TextAsset>(root+"/presentation-theme");
                if(!themeFile){errors.Add("Missing v7 theme");return errors.ToArray();}
                var theme=JsonUtility.FromJson<PresentationTheme>(themeFile.text);
                errors.AddRange(PresentationThemeValidation.Validate(theme,root));
                if(root==PresentationAssets.CandidateRoot)foreach(string address in AssetKey.BrothResources)if(!PresentationAssets.Load<Texture2D>(address))errors.Add("Missing local broth resource: "+address);
                if(errors.Count>0)return errors.ToArray();
                foreach(var asset in theme.assets)
                {
                    if(!requireComplete&&PresentationAssets.IsRemote(asset.resourceAddress)&&PresentationAssets.Provider.Snapshot.State!=HotpotSort.Contracts.RemoteAssets.AssetReadiness.Ready)continue;
                    var texture=PresentationAssets.Load<Texture2D>(asset.resourceAddress);
                    if(!texture)errors.Add("Missing "+asset.key);
                    else if(asset.key.StartsWith("food.")&&!texture.isReadable)errors.Add("Food alpha hit data unreadable: "+asset.key);
                    if(texture)foreach(var slice in theme.slices??Array.Empty<ThemeSlice>())if(slice.key==asset.key&&(slice.x+slice.width>texture.width||slice.y+slice.height>texture.height))errors.Add("Theme slice exceeds texture: "+asset.key);
                }
                errors.AddRange(ValidateModernFont());
                }
                catch(Exception e){errors.Add("Theme load failed: "+root+": "+e.Message);}
                return errors.ToArray();
            }
            var ids=new List<string>{"containers/plate_main","containers/dish_buffer","pots/pot_body","pots/pot_unlit","pots/pot_broth","pots/pot_rim","background/table","share/share_theme"};
            for(int i=0;i<16;i++)ids.Add("food/food_"+i.ToString("00"));
            foreach(string name in new[]{"panel","button","button_round","order_plate","order_pointer","progress_track","timer_plate"})ids.Add("ui/"+name);
            foreach(string name in new[]{"hint","clear","shuffle","pause","settings","close","share","play","retry","home","music","sound","ad","leaderboard","lock"})ids.Add("icons/"+name);
            foreach(string name in new[]{"splash","ripple","steam","fire","gold","hint"})ids.Add("fx/"+name);
            foreach(string id in ids){var texture=PresentationAssets.Load<Texture2D>(root+"/"+id);if(!texture)errors.Add("Missing "+id);else if(id.StartsWith("food/")&&!texture.isReadable)errors.Add("Food alpha hit data unreadable: "+id);}
            errors.AddRange(ValidateModernFont());
            return errors.ToArray();
        }
    }
}
