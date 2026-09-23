using System;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    public sealed partial class GameplayView
    {
        RectTransform friendOverlay,friendSafe;
        RawImage friendCanvas;
        Func<Texture> friendTexture;
        Action friendCloseRequested;
        Action<Rect> friendViewportChanged;
        Action<bool> friendModalChanged;
        Rect lastFriendViewport;
        public bool FriendBoardVisible=>friendOverlay&&friendOverlay.gameObject.activeInHierarchy;

        // Main-domain presentation consumes only a texture and neutral callbacks, never friend records.
        // Integrator pauses the session through modalChanged and binds its CloseFriendSurface through requestClose.
        // Returned viewport is physical pixels with top-left origin, excluding header and close control.
        public Rect FriendBoardViewportPixels
        {
            get
            {
                float s=Mathf.Min(viewport.width/420,viewport.height/900);
                float x=viewport.x+(viewport.width-420*s)*.5f;
                float y=(viewportScreenHeight>0?viewportScreenHeight:Screen.height)-viewport.yMax+(viewport.height-900*s)*.5f;
                return new Rect(x+48*s,y+122*s,324*s,572*s);
            }
        }
        public void ShowFriendBoard(Func<Texture> sharedTexture,Action requestClose,Action<Rect> viewportChanged,Action<bool> modalChanged)
        {
            if(sharedTexture==null||requestClose==null||viewportChanged==null||modalChanged==null)throw new ArgumentNullException("Friend board presentation callbacks are required");
            HideFriendBoard();CloseModal();
            friendTexture=sharedTexture;friendCloseRequested=requestClose;friendViewportChanged=viewportChanged;friendModalChanged=modalChanged;
            friendOverlay=Node(canvasRoot,"FriendBoardOverlay",new Rect(0,0,canvasRoot.rect.width,canvasRoot.rect.height));
            var shade=friendOverlay.gameObject.AddComponent<Image>();shade.color=new Color(.09f,.08f,.065f,.76f);shade.raycastTarget=true;
            friendSafe=Node(friendOverlay,"FriendSafeArea",new Rect());
            var frame=LogicalFrame(friendSafe,"FriendFrame");
            var card=ModernSkin(frame,"FriendCard",new Rect(30,98,360,706),"ui.panel");card.GetComponent<Image>().raycastTarget=true;
            // The open-data renderer owns its own title. Do not duplicate it in the host shell.
            var canvas=Node(card,"SharedCanvas",new Rect(18,24,324,572));
            friendCanvas=canvas.gameObject.AddComponent<RawImage>();friendCanvas.uvRect=new Rect(0,1,1,-1);friendCanvas.raycastTarget=true;
            VButton(card,"关闭",new Rect(40,632,280,50),CloseFriendBoardView,false,20);
            friendModalChanged(true);UpdateSimulation();UpdateFriendBoardPresentation(true);
        }
        public void CloseFriendBoardView()
        {
            var close=friendCloseRequested;friendCloseRequested=null;
            try{close?.Invoke();}finally{HideFriendBoard();}
        }
        // Called when platform closes/disposes the surface; never calls back into platform Close again.
        public void HideFriendBoard()
        {
            var changed=friendModalChanged;friendModalChanged=null;friendCloseRequested=null;friendViewportChanged=null;friendTexture=null;friendCanvas=null;friendSafe=null;
            if(friendOverlay){friendOverlay.gameObject.SetActive(false);Destroy(friendOverlay.gameObject);friendOverlay=null;}
            changed?.Invoke(false);if(World&&LastSnapshot!=null)UpdateSimulation();
        }
        void UpdateFriendBoardPresentation(bool force=false)
        {
            if(!FriendBoardVisible)return;
            Place(friendOverlay,new Rect(0,0,canvasRoot.rect.width,canvasRoot.rect.height));
            Place(friendSafe,new Rect(viewport.x,(viewportScreenHeight>0?viewportScreenHeight:Screen.height)-viewport.yMax,viewport.width,viewport.height));
            var frame=friendSafe.Find("FriendFrame") as RectTransform;float scale=Mathf.Min(viewport.width/420,viewport.height/900);
            frame.localScale=Vector3.one*scale;frame.anchoredPosition=new Vector2((viewport.width-420*scale)*.5f,-(viewport.height-900*scale)*.5f);
            var texture=friendTexture?.Invoke();if(texture)texture.wrapMode=TextureWrapMode.Clamp;
            friendCanvas.texture=texture;friendCanvas.color=texture?Color.white:Color.clear;
            Rect pixels=FriendBoardViewportPixels;
            if(force||pixels!=lastFriendViewport){lastFriendViewport=pixels;friendViewportChanged?.Invoke(pixels);}
            friendOverlay.SetAsLastSibling();
        }
    }
}
