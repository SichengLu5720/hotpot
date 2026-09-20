using System;
using UnityEngine;
namespace HotpotSort.View
{
    /// <summary>Immediate-mode prototype UI. The full gameplay is independent of this renderer.</summary>
    public sealed class PrimitiveArt : IDisposable
    {
        private readonly Texture2D circle; private readonly Texture2D[] foods=new Texture2D[16]; private readonly Texture2D plate;
        private Font font; private bool ownsFont; private GUIStyle textStyle;
        public static Color ColorOf(string s){Color c;return ColorUtility.TryParseHtmlString(s,out c)?c:Color.white;}
        public PrimitiveArt()
        {
            circle=new Texture2D(64,64,TextureFormat.RGBA32,false);circle.name="RuntimeCircle";circle.filterMode=FilterMode.Bilinear;
            var pixels=new Color32[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));byte a=(byte)(Mathf.Clamp01(32-d)*255);pixels[y*64+x]=new Color32(255,255,255,a);}circle.SetPixels32(pixels);circle.Apply();
            for(int i=0;i<16;i++)foods[i]=Resources.Load<Texture2D>("Hotpot/food_"+i.ToString("D2"));plate=Resources.Load<Texture2D>("Hotpot/plate");
            // OS font lookup only. No font asset is redistributed in the project.
            if(Application.platform!=RuntimePlatform.WebGLPlayer)
            {
                try{font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","PingFang SC","Noto Sans CJK SC","Arial"},16);ownsFont=font!=null;}catch{font=null;}
            }
        }
        public void Rect(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public void Circle(float x,float y,float radius,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x-radius,y-radius,radius*2,radius*2),circle);GUI.color=old;}
        public void Rounded(Rect r,float radius,Color color)
        {
            radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)/2);Rect(new Rect(r.x+radius,r.y,r.width-radius*2,r.height),color);Rect(new Rect(r.x,r.y+radius,r.width,r.height-radius*2),color);
            Circle(r.x+radius,r.y+radius,radius,color);Circle(r.xMax-radius,r.y+radius,radius,color);Circle(r.x+radius,r.yMax-radius,radius,color);Circle(r.xMax-radius,r.yMax-radius,radius,color);
        }
        public void Panel(Rect r,float radius,Color fill,Color border){Rounded(r,radius,border);Rounded(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),Mathf.Max(0,radius-1),fill);}
        public void Text(string s,Rect r,int size,Color color,TextAnchor anchor=TextAnchor.MiddleLeft,bool bold=false)
        {
            if(textStyle==null)textStyle=new GUIStyle(GUI.skin.label);
            textStyle.font=font!=null?font:GUI.skin.font;textStyle.fontSize=size;textStyle.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;textStyle.alignment=anchor;textStyle.normal.textColor=color;textStyle.wordWrap=false;GUI.Label(r,s,textStyle);
        }
        public void Food(int kind,float x,float y,float size,bool letter=false)
        {
            if(kind<0||kind>=foods.Length)return;
            // Exports use size 48 at a 128x128 canvas with center (64,60).
            float dim=size*128/48;
            if(foods[kind]!=null)GUI.DrawTexture(new Rect(x-dim/2,y-size*60/48,dim,dim),foods[kind],ScaleMode.ScaleToFit,true);
            else Circle(x,y,size,ColorOf("#C89766"));
            if(letter){Circle(x+size*.73f,y+size*.69f,6.5f,ColorOf("#FFFAEE"));Text(((char)('A'+kind)).ToString(),new Rect(x+size*.73f-6.5f,y+size*.69f-7,13,14),8,ColorOf("#47635D"),TextAnchor.MiddleCenter,true);}
        }
        public void Plate(float x,float y,float radius,int id,bool debug)
        {
            if(plate!=null){float d=radius*128/58;GUI.DrawTexture(new Rect(x-d/2,y-radius*62/58,d,d),plate,ScaleMode.ScaleToFit,true);}else Circle(x,y,radius,ColorOf("#FFFBEF"));
            if(debug)Text("#"+id,new Rect(x-20,y-radius+6,40,13),8,ColorOf("#8CA185"),TextAnchor.MiddleCenter);
        }
        public void Dispose(){if(circle!=null)UnityEngine.Object.Destroy(circle);if(ownsFont&&font!=null)UnityEngine.Object.Destroy(font);}
    }
}
