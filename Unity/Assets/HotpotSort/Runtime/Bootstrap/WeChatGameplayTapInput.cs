using System.Collections.Generic;
using HotpotSort.Presentation;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace HotpotSort.Bootstrap
{
    // Installed after SDK initialization. A short native press must not disappear
    // when both its start and end arrive between two Unity Update calls.
    public sealed class WeChatGameplayTapInput : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private GameplayView view;
        private NativeTouchUiInput ui;
        private int? primaryId;
        private readonly HashSet<int> pressed = new HashSet<int>();
        private void OnEnable()
        {
            view=GetComponent<GameplayView>();
            if(!view)return;
            ui=NativeTouchUiInput.Install();
            WX.OnTouchStart(OnStart);
            WX.OnTouchMove(OnMove);
            WX.OnTouchEnd(OnEnd);
            WX.OnTouchCancel(OnCancel);
            view.UsesExternalTapInput=true;
        }
        private void OnStart(OnTouchStartListenerResult value)
        {
            if(!view || value.changedTouches==null)return;
            foreach(var touch in value.changedTouches)
            {
                bool primary=pressed.Count==0;
                if(!pressed.Add(touch.identifier) || !primary)continue;
                primaryId=touch.identifier;
                ui?.Push(touch.identifier,new Vector2(touch.clientX,touch.clientY),TouchPhase.Began);
                // The installed WX SDK's formatTouchEvent already converts CSS
                // coordinates to physical pixels, with a bottom-left origin.
                view.SubmitScreenTap(new Vector2(touch.clientX,touch.clientY),touch.identifier);
            }
        }
        private void OnEnd(OnTouchStartListenerResult value)
        {
            if(value.changedTouches==null)return;
            foreach(var touch in value.changedTouches)
            {
                if(primaryId==touch.identifier){ui?.Push(touch.identifier,new Vector2(touch.clientX,touch.clientY),TouchPhase.Ended);primaryId=null;}
                pressed.Remove(touch.identifier);
            }
        }
        private void OnMove(OnTouchStartListenerResult value)
        {
            if(value.changedTouches==null)return;
            foreach(var touch in value.changedTouches)if(primaryId==touch.identifier)
                ui?.Push(touch.identifier,new Vector2(touch.clientX,touch.clientY),TouchPhase.Moved);
        }
        private void OnCancel(OnTouchStartListenerResult value){Cancel();}
        private void Cancel(){pressed.Clear();primaryId=null;ui?.Cancel();}
        private void OnApplicationFocus(bool focused){if(!focused)Cancel();}
        private void OnApplicationPause(bool paused){if(paused)Cancel();}
        private void OnDisable()
        {
            WX.OffTouchStart(OnStart);
            WX.OffTouchMove(OnMove);
            WX.OffTouchEnd(OnEnd);
            WX.OffTouchCancel(OnCancel);
            Cancel();if(ui){ui.enabled=false;Destroy(ui);ui=null;}
            if(view)view.UsesExternalTapInput=false;
        }
#endif
    }
}
