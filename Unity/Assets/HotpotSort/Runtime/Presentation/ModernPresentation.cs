using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HotpotSort.Presentation
{
    // r017 presentation-only styling. Existing food UVs, transforms and hit geometry stay authoritative.
    public static class ModernPalette
    {
        public static readonly Color Paper=new Color32(255,251,242,255), Ink=new Color32(64,57,49,255),
            Muted=new Color32(119,111,101,255), Coral=new Color32(204,76,56,255),
            Tea=new Color32(72,113,99,255), Line=new Color32(225,213,195,255),
            Soft=new Color32(245,236,220,255);
    }

    // Code-native, resolution-independent control symbols; no artwork or text is baked into a texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ModernIconGraphic : MaskableGraphic
    {
        public string symbol;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(symbol)
            {
                case "pause": Stroke(vh,.36f,.28f,.36f,.72f,5);Stroke(vh,.64f,.28f,.64f,.72f,5);break;
                case "hint": Ring(vh,.43f,.57f,.24f,3);Stroke(vh,.61f,.39f,.82f,.18f,3);break;
                case "clear_buffer":
                    Arc(vh,.5f,.43f,.30f,.11f,180,360,2.8f);Arc(vh,.5f,.29f,.30f,.11f,180,360,2.8f);
                    Stroke(vh,.5f,.40f,.5f,.84f,3);Stroke(vh,.5f,.84f,.35f,.67f,3);Stroke(vh,.5f,.84f,.65f,.67f,3);break;
                case "shuffle":
                    Stroke(vh,.19f,.73f,.34f,.73f,3);Stroke(vh,.34f,.73f,.66f,.27f,3);Stroke(vh,.66f,.27f,.82f,.27f,3);
                    Stroke(vh,.19f,.27f,.34f,.27f,3);Stroke(vh,.34f,.27f,.66f,.73f,3);Stroke(vh,.66f,.73f,.82f,.73f,3);
                    Stroke(vh,.70f,.85f,.82f,.73f,3);Stroke(vh,.70f,.61f,.82f,.73f,3);Stroke(vh,.70f,.39f,.82f,.27f,3);Stroke(vh,.70f,.15f,.82f,.27f,3);break;
                case "close":Stroke(vh,.3f,.3f,.7f,.7f,3);Stroke(vh,.3f,.7f,.7f,.3f,3);break;
                case "friends":
                    Ring(vh,.40f,.67f,.14f,2.4f);Arc(vh,.40f,.27f,.27f,.22f,0,180,2.4f);
                    Arc(vh,.70f,.65f,.10f,.12f,-70,85,2.4f);Arc(vh,.67f,.28f,.22f,.21f,0,85,2.4f);break;
                case "lock":
                    Arc(vh,.5f,.57f,.18f,.23f,0,180,2.8f);Stroke(vh,.32f,.38f,.32f,.59f,2.8f);Stroke(vh,.68f,.38f,.68f,.59f,2.8f);
                    Stroke(vh,.26f,.42f,.74f,.42f,3);Stroke(vh,.26f,.42f,.26f,.20f,3);Stroke(vh,.74f,.42f,.74f,.20f,3);Stroke(vh,.26f,.20f,.74f,.20f,3);break;
                case "music_on":case "music_off":
                    Stroke(vh,.35f,.28f,.35f,.73f,3);Stroke(vh,.35f,.73f,.72f,.84f,3);Stroke(vh,.72f,.84f,.72f,.39f,3);
                    Ring(vh,.25f,.23f,.11f,3);Ring(vh,.62f,.34f,.11f,3);if(symbol=="music_off")Stroke(vh,.20f,.84f,.84f,.20f,2);break;
                case "effects_on":case "effects_off":
                    Stroke(vh,.20f,.40f,.35f,.40f,3);Stroke(vh,.20f,.60f,.35f,.60f,3);Stroke(vh,.20f,.40f,.20f,.60f,3);
                    Stroke(vh,.35f,.60f,.55f,.79f,3);Stroke(vh,.35f,.40f,.55f,.21f,3);Stroke(vh,.55f,.21f,.55f,.79f,3);
                    if(symbol=="effects_on"){Arc(vh,.58f,.5f,.14f,.19f,-65,65,2.5f);Arc(vh,.58f,.5f,.26f,.31f,-65,65,2.5f);}else{Stroke(vh,.69f,.37f,.85f,.63f,2.5f);Stroke(vh,.69f,.63f,.85f,.37f,2.5f);}break;
                default: Ring(vh,.5f,.5f,.27f,2.5f);break;
            }
        }
        Vector2 Point(float x,float y){var r=rectTransform.rect;return new Vector2(r.x+x*r.width,r.y+y*r.height);}
        void Stroke(VertexHelper vh,float ax,float ay,float bx,float by,float weight)
        {
            Vector2 a=Point(ax,ay),b=Point(bx,by),v=b-a;if(v.sqrMagnitude<.0001f)return;
            float thickness=weight*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/40f;
            Vector2 side=new Vector2(-v.y,v.x).normalized*thickness*.5f;int start=vh.currentVertCount;
            vh.AddVert(a-side,color,Vector2.zero);vh.AddVert(a+side,color,Vector2.zero);vh.AddVert(b+side,color,Vector2.zero);vh.AddVert(b-side,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        void Ring(VertexHelper vh,float x,float y,float radius,float weight)=>Arc(vh,x,y,radius,radius,0,360,weight);
        void Arc(VertexHelper vh,float x,float y,float rx,float ry,float from,float to,float weight)
        {
            int count=Mathf.CeilToInt(Mathf.Abs(to-from)/10f);
            for(int i=0;i<count;i++){float a=Mathf.Lerp(from,to,i/(float)count)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(i+1f)/count)*Mathf.Deg2Rad;Stroke(vh,x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry,weight);}
        }
    }

    public sealed class PresentationPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        bool pressed;float amount=1;
        public void OnPointerDown(PointerEventData e){var s=GetComponent<Selectable>();pressed=s&&s.IsInteractable();}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnPointerExit(PointerEventData e){pressed=false;}
        void Update()
        {
            float next=Mathf.MoveTowards(amount,pressed?.975f:1,Time.unscaledDeltaTime*.8f);
            if(Mathf.Abs(next-amount)<.000001f)return;
            amount=next;transform.localScale=Vector3.one*amount;
        }
        void OnDisable(){pressed=false;amount=1;transform.localScale=Vector3.one;}
    }

    public sealed partial class GameplayView
    {
        RectTransform ModernSkin(Transform parent,string name,Rect rect,string key)
        {
            if(UnifiedTheme)
            {
                var node=Node(parent,name,rect);var skin=node.gameObject.AddComponent<Image>();
                skin.sprite=art.Skin(key);skin.color=Color.white;skin.raycastTarget=false;
                skin.type=key=="ui.icon_disc"||key=="ui.order_pointer"?Image.Type.Simple:Image.Type.Sliced;
                if(skin.sprite)skin.pixelsPerUnitMultiplier=Mathf.Max(.1f,Mathf.Max(skin.sprite.rect.width/Mathf.Max(1,rect.width),skin.sprite.rect.height/Mathf.Max(1,rect.height)));
                return node;
            }
            var n=Node(parent,name,rect);var image=n.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.raycastTarget=false;
            image.pixelsPerUnitMultiplier=key=="ui.panel"?1.1f:key=="ui.icon_disc"?.7f:1.8f;
            image.color=key=="ui.button_primary"?ModernPalette.Coral:key=="ui.progress_track"?ModernPalette.Soft:ModernPalette.Paper;
            if(key=="ui.button_secondary")image.color=ModernPalette.Soft;
            if(key=="ui.panel"||key=="ui.bottom_bar"||key=="ui.button_primary")
            {var shadow=n.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.10f,.06f,.03f,.16f);shadow.effectDistance=new Vector2(0,-2);shadow.useGraphicAlpha=true;}
            return n;
        }
        void ModernIcon(Transform parent,string name,string symbol,Rect rect,Color tint)
        {
            if(UnifiedTheme){PictureContain(parent,art.Texture("icon."+symbol),rect,name);return;}
            var n=Node(parent,name,rect);var icon=n.gameObject.AddComponent<ModernIconGraphic>();icon.symbol=symbol;icon.color=tint;icon.raycastTarget=false;icon.SetVerticesDirty();
        }
        void StyleButton(Button button)
        {
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;
            colors.pressedColor=new Color(.90f,.90f,.90f,1);colors.disabledColor=new Color(.8f,.8f,.8f,.65f);colors.fadeDuration=.09f;button.colors=colors;
            button.gameObject.AddComponent<PresentationPress>();
        }
    }
}
