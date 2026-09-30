using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        bool UnifiedTheme=>art!=null&&art.Theme.schemaVersion=="presentation_theme_v10";
        Color ThemeInk=>UnifiedTheme?art.ColorFor("ink",V7Art.Ink):ModernPalette.Ink;
        Color ThemeIvory=>UnifiedTheme?art.ColorFor("ivory",V7Art.Ivory):ModernPalette.Paper;
        readonly List<RawImage> themeEdges=new List<RawImage>();

        static void SubtractRect(List<Rect> regions,Rect exclusion)
        {
            for(int i=regions.Count-1;i>=0;i--)
            {
                var a=regions[i];if(!a.Overlaps(exclusion))continue;regions.RemoveAt(i);
                float x0=Mathf.Max(a.xMin,exclusion.xMin),x1=Mathf.Min(a.xMax,exclusion.xMax);
                float y0=Mathf.Max(a.yMin,exclusion.yMin),y1=Mathf.Min(a.yMax,exclusion.yMax);
                if(x0>a.xMin)regions.Add(new Rect(a.xMin,a.yMin,x0-a.xMin,a.height));
                if(x1<a.xMax)regions.Add(new Rect(x1,a.yMin,a.xMax-x1,a.height));
                if(y0>a.yMin)regions.Add(new Rect(x0,a.yMin,x1-x0,y0-a.yMin));
                if(y1<a.yMax)regions.Add(new Rect(x0,y1,x1-x0,a.yMax-y1));
            }
        }

        // Background-only table frame. Insets remain visible when the logical board fills a narrow
        // phone; content is always later in the canvas hierarchy and owns every interactive pixel.
        void LayoutThemeEdges()
        {
            foreach(var edge in themeEdges)if(edge)edge.enabled=false;
            if(!UnifiedTheme||!edgeImage||!edgeImage.texture||viewport.width<=0||viewport.height<=0)return;
            if(LastSnapshot.phase!=ViewPhase.Entry)
            {
                // Gameplay uses the existing whole-image layer, outside every gameplay mask.
                // Keep its full-stretch transform and UVs; retain the visible bands' original tint.
                edgeImage.transform.SetSiblingIndex(backgroundImage.transform.GetSiblingIndex()+1);
                edgeImage.color=Color.white;edgeImage.raycastTarget=false;edgeImage.enabled=true;
                return;
            }
            float width=canvasRoot.rect.width,height=canvasRoot.rect.height;
            if(width<=0||height<=0)return;
            float screenHeight=viewportScreenHeight>0?viewportScreenHeight:Screen.height;
            var safe=new Rect(viewport.x,screenHeight-viewport.yMax,viewport.width,viewport.height);
            float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            float band=Mathf.Min(safe.width*.14f,(LastSnapshot.phase==ViewPhase.Entry?58:38)*scale);
            var regions=new List<Rect>{new Rect(safe.x,safe.y,band,safe.height),new Rect(safe.xMax-band,safe.y,band,safe.height)};
            if(menuButtonPixels.width>0&&menuButtonPixels.height>0)
                SubtractRect(regions,new Rect(menuButtonPixels.x-12*scale,screenHeight-menuButtonPixels.yMax-12*scale,menuButtonPixels.width+24*scale,menuButtonPixels.height+24*scale));
            int index=0;
            foreach(var region in regions)
            {
                if(region.width<1||region.height<1)continue;
                if(index>=themeEdges.Count)
                {
                    var node=Node(canvasRoot,"ThemeEdgeGutter_"+index,region);node.SetSiblingIndex(2);
                    var edge=node.gameObject.AddComponent<RawImage>();edge.raycastTarget=false;themeEdges.Add(edge);
                }
                var image=themeEdges[index++];Place(image.rectTransform,region);
                image.texture=edgeImage.texture;image.color=Color.white;
                // UVs are relative to the actual safe portrait surface, never the desktop canvas.
                image.uvRect=new Rect((region.x-safe.x)/safe.width,1-(region.yMax-safe.y)/safe.height,region.width/safe.width,region.height/safe.height);image.enabled=true;
            }
        }

        static void PictureContain(Transform parent,Texture texture,Rect bounds,string name)
        {
            if(!texture)return;
            float scale=Mathf.Min(bounds.width/texture.width,bounds.height/texture.height);
            var size=new Vector2(texture.width*scale,texture.height*scale);
            Picture(parent,texture,new Rect(bounds.position+(bounds.size-size)*.5f,size),name);
        }
    }

}
