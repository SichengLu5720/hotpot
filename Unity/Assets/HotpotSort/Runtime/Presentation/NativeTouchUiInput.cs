using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HotpotSort.Presentation
{
    // Feed the existing UGUI module one native edge per frame. A press/release
    // pair received between frames must not collapse into "no touch".
    public sealed class NativeTouchUiInput : BaseInput
    {
        readonly Queue<Touch> edges=new Queue<Touch>();
        StandaloneInputModule module;
        BaseInput previous;
        Touch current;
        int frame=-1;
        bool present;
        public static NativeTouchUiInput Install()
        {
            var system=EventSystem.current;
            var target=system?system.GetComponent<StandaloneInputModule>():null;
            if(!target)return null;
            var input=target.gameObject.AddComponent<NativeTouchUiInput>();
            input.module=target;input.previous=target.inputOverride;
            target.DeactivateModule();target.inputOverride=input;
            return input;
        }
        public void Push(int id,Vector2 position,TouchPhase phase)
        {
            if(phase==TouchPhase.Canceled){Cancel();return;}
            edges.Enqueue(new Touch{fingerId=id,position=position,rawPosition=position,phase=phase,type=TouchType.Direct,tapCount=1});
        }
        public void Cancel()
        {
            edges.Clear();
            // UGUI treats cancellation as release; move it off every UI target
            // so a focus loss cannot be mistaken for a successful button click.
            if(present&&current.phase!=TouchPhase.Ended&&current.phase!=TouchPhase.Canceled)
                edges.Enqueue(new Touch{fingerId=current.fingerId,position=new Vector2(-100000,-100000),phase=TouchPhase.Canceled,type=TouchType.Direct});
        }
        void Advance()
        {
            if(frame==Time.frameCount)return;
            frame=Time.frameCount;
            if(edges.Count>0){var next=edges.Dequeue();next.deltaPosition=next.phase==TouchPhase.Began?Vector2.zero:next.position-current.position;current=next;present=true;}
            else if(present&&(current.phase==TouchPhase.Ended||current.phase==TouchPhase.Canceled))present=false;
            else if(present){current.phase=TouchPhase.Stationary;current.deltaPosition=Vector2.zero;}
        }
        public override bool touchSupported=>true;
        public override bool mousePresent=>false;
        public override Vector2 mousePosition=>Vector2.zero;
        public override Vector2 mouseScrollDelta=>Vector2.zero;
        public override bool GetMouseButton(int button)=>false;
        public override bool GetMouseButtonDown(int button)=>false;
        public override bool GetMouseButtonUp(int button)=>false;
        public override float GetAxisRaw(string name)=>0;
        public override bool GetButtonDown(string name)=>false;
        public override int touchCount{get{Advance();return present?1:0;}}
        public override Touch GetTouch(int index){Advance();return current;}
        protected override void OnDisable()
        {
            edges.Clear();present=false;
            if(module&&module.inputOverride==this){module.DeactivateModule();module.inputOverride=previous;}
            base.OnDisable();
        }
    }
}
