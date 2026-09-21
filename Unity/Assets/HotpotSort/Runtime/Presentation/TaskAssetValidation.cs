using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotpotSort.Presentation
{
    public static class TaskAssetValidation
    {
        public static string[] Validate(string root)
        {
            var errors=new List<string>();if(string.IsNullOrWhiteSpace(root)){errors.Add("No formal visual batch selected");return errors.ToArray();}
            var ids=new List<string>{"containers/plate_main","containers/dish_buffer","pots/pot_body","pots/pot_unlit","pots/pot_broth","pots/pot_rim","background/table","share/share_theme"};
            for(int i=0;i<16;i++)ids.Add("food/food_"+i.ToString("00"));
            foreach(string name in new[]{"panel","button","button_round","order_plate","order_pointer","progress_track","progress_fill","timer_plate"})ids.Add("ui/"+name);
            foreach(string name in new[]{"hint","clear","shuffle","pause","settings","close","share","play","retry","home","music","sound","ad","leaderboard","lock"})ids.Add("icons/"+name);
            foreach(string name in new[]{"splash","ripple","steam","fire","gold","hint"})ids.Add("fx/"+name);
            foreach(string id in ids){var texture=Resources.Load<Texture2D>(root+"/"+id);if(!texture)errors.Add("Missing "+id);else if(id.StartsWith("food/")&&!texture.isReadable)errors.Add("Food alpha hit data unreadable: "+id);}
            foreach(string name in new[]{"display","readable"})if(!Resources.Load<Font>(root+"/fonts/"+name))errors.Add("Missing embedded font "+name);
            return errors.ToArray();
        }
    }
}
