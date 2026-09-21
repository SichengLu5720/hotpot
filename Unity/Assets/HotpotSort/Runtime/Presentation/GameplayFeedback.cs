using System;
using System.Collections;
using System.Collections.Generic;
using HotpotSort.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace HotpotSort.Presentation
{
    // Event-driven presentation only. Missing licensed audio stays silent.
    public sealed class GameplayFeedback : MonoBehaviour
    {
        readonly HashSet<string> consumed=new HashSet<string>();
        readonly List<GameObject> effects=new List<GameObject>();
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        RectTransform layer,plateEffects;
        AudioSource sfx,music,ambience;
        Texture2D[] foods;
        string sessionId,artRoot;
        float nextSteam;
        bool running;
        PlayerSettings settings=new PlayerSettings();
        readonly Queue<ViewEvent>[] serveQueues={new Queue<ViewEvent>(),new Queue<ViewEvent>(),new Queue<ViewEvent>(),new Queue<ViewEvent>()};
        readonly bool[] serving=new bool[4];
        public void Initialize(Transform parent,Canvas canvas)
        {
            sfx=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();ambience=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{sfx,music,ambience}){source.playOnAwake=false;source.spatialBlend=0;}
            music.loop=ambience.loop=true;
        }
        public void SetBoard(RectTransform board,RectTransform clippedPlates=null)
        {
            plateEffects=clippedPlates;
            if(!layer){var go=new GameObject("FeedbackLayer",typeof(RectTransform));layer=(RectTransform)go.transform;}
            layer.SetParent(board,false);layer.anchorMin=layer.anchorMax=layer.pivot=new Vector2(0,1);layer.anchoredPosition=Vector2.zero;layer.sizeDelta=new Vector2(420,900);layer.SetAsLastSibling();
        }
        public void ConfigureAssets(string root,Texture2D[] foodTextures){artRoot=root;foods=foodTextures;}
        // Only call after the independent audio batch and licence evidence are accepted.
        public void ConfigureAudio(string approvedAudioRoot)
        {
            clips.Clear();
            foreach(string name in new[]{"ceramic","fly","drop","sizzle","order","serve","button","win","lose"})clips[name]=Resources.Load<AudioClip>(approvedAudioRoot+"/sfx_"+name);
            music.clip=Resources.Load<AudioClip>(approvedAudioRoot+"/bgm_main");ambience.clip=Resources.Load<AudioClip>(approvedAudioRoot+"/amb_boiling");
            SetAudioSettings(settings);
        }
        public void SetAudioSettings(PlayerSettings value)
        {
            settings=value;sfx.volume=value.EffectsEnabled?value.EffectsVolume:0;music.volume=value.MusicEnabled?value.MusicVolume:0;ambience.volume=value.EffectsEnabled?value.EffectsVolume*.25f:0;
        }
        public void ResetFeedback(){StopAllCoroutines();foreach(var go in effects)if(go)Destroy(go);effects.Clear();for(int i=0;i<4;i++){serveQueues[i].Clear();serving[i]=false;GetComponent<GameplayView>()?.SetOrderServing(i,false);}consumed.Clear();sessionId=null;sfx.Stop();music.Stop();ambience.Stop();}
        public void Apply(ViewUpdate update,ViewSnapshot previous,Func<string,Vector2> sourcePosition)
        {
            if(sessionId!=update.snapshot.sessionId){ResetFeedback();sessionId=update.snapshot.sessionId;}
            foreach(var evt in update.events??new ViewEvent[0])
            {
                if(!consumed.Add(evt.sequence+":"+evt.transactionId))continue;
                Vector2 target=evt.targetContainer=="Buffer"?new Vector2(75+evt.targetSlot*67,259):new Vector2(56+evt.targetSlot*103,166);
                switch(evt.kind)
                {
                    case "TapAccepted":Play("ceramic");break;
                    case "ItemRoutedToOrder":case "ItemRoutedToBuffer":case "BufferAutoAbsorbed":
                        var from=evt.sourceContainer=="Buffer"?new Vector2(75+evt.sourceSlot*67,259):sourcePosition(evt.itemId);
                        StartCoroutine(Flight(evt,from,target));break;
                    case "OrderCompleted":serveQueues[evt.slot].Enqueue(evt);if(!serving[evt.slot])StartCoroutine(Serve(evt.slot));Play("order");break;
                    case "PotUnlocked":Pulse(new Vector2(56+evt.slot*103,166),"fire",.45f);Play("sizzle");break;
                    case "ChallengeWon":Play("win");break;
                    case "ChallengeFailed":Play("lose");break;
                }
            }
            running=update.snapshot.phase==ViewPhase.Running;
            if(running){if(music.clip&&!music.isPlaying)music.Play();if(ambience.clip&&!ambience.isPlaying)ambience.Play();}
            else {music.Pause();ambience.Pause();}
        }
        void Update()
        {
            if(!running||!layer||string.IsNullOrEmpty(artRoot)||Time.unscaledTime<nextSteam)return;
            nextSteam=Time.unscaledTime+1.2f;
            foreach(var order in GetComponent<GameplayView>().LastSnapshot.orders)
                if(order.enabled&&!serving[order.slot])Pulse(new Vector2(56+order.slot*103,145),"steam",.8f);
        }
        IEnumerator Flight(ViewEvent evt,Vector2 from,Vector2 to)
        {
            if(!layer)yield break;
            int id=-1;if(evt.ingredientId!=null)int.TryParse(evt.ingredientId.Substring(5),out id);
            var node=Effect("FlyingItem",from,32,id>=0&&foods!=null&&id<foods.Length?foods[id]:null,new Color(1,1,1,.95f));
            Play("fly");float elapsed=0;
            while(elapsed<.22f && node){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/.22f);var p=Vector2.Lerp(from,to,t);p.y-=Mathf.Sin(t*Mathf.PI)*25;node.anchoredPosition=new Vector2(p.x,-p.y);yield return null;}
            if(node){effects.Remove(node.gameObject);Destroy(node.gameObject);}Pulse(to,evt.targetContainer=="Order"?"splash":"ripple",.22f);Play("drop");
        }
        IEnumerator Serve(int slot)
        {
            if(!layer)yield break;
            var view=GetComponent<GameplayView>();serving[slot]=true;view.SetOrderServing(slot,true);
            while(serveQueues[slot].Count>0)
            {
                var evt=serveQueues[slot].Dequeue();int food=-1;if(evt.ingredientId!=null)int.TryParse(evt.ingredientId.Substring(5),out food);
                var node=view.CreateServingPot(layer,slot,food);effects.Add(node.gameObject);
                yield return new WaitForSecondsRealtime(.24f);
                Pulse(new Vector2(56+slot*103,160),"gold",.22f);Play("serve");
                float elapsed=0;while(elapsed<.32f && node){elapsed+=Time.unscaledDeltaTime;node.anchoredPosition+=Vector2.up*(Time.unscaledDeltaTime*440);node.localScale=Vector3.one*(1-elapsed*.5f);yield return null;}
                if(node){effects.Remove(node.gameObject);Destroy(node.gameObject);}
            }
            serving[slot]=false;view.SetOrderServing(slot,false);
        }
        Texture Load(string name)=>string.IsNullOrEmpty(artRoot)?null:Resources.Load<Texture2D>(artRoot+"/fx/"+name);
        RectTransform Effect(string name,Vector2 point,float size,Texture texture,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(layer,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(point.x,-point.y);rt.sizeDelta=Vector2.one*size;
            var image=go.GetComponent<RawImage>();image.texture=texture;image.color=color;image.raycastTarget=false;effects.Add(go);return rt;
        }
        void Pulse(Vector2 point,string name,float duration){if(!layer)return;var node=Effect(name,point,46,Load(name),new Color(1,.75f,.35f,.6f));StartCoroutine(Fade(node,duration));}
        IEnumerator Fade(RectTransform node,float duration){float t=0;while(t<duration&&node){t+=Time.unscaledDeltaTime;var im=node.GetComponent<RawImage>();im.color=new Color(im.color.r,im.color.g,im.color.b,1-t/duration);node.localScale=Vector3.one*(1+t);yield return null;}if(node){effects.Remove(node.gameObject);Destroy(node.gameObject);}}
        public void Highlight(Func<Vector2> position){if(layer)StartCoroutine(TrackHint(position));}
        IEnumerator TrackHint(Func<Vector2> position)
        {
            var node=Effect("hint",position(),42,Load("hint"),new Color(1,.85f,.2f,.75f));float time=0;
            if(plateEffects)node.SetParent(plateEffects,false);
            while(time<1.5f && node){var point=position();if(point==Vector2.zero)break;node.anchoredPosition=new Vector2(point.x,-point.y);time+=Time.unscaledDeltaTime;yield return null;}
            if(node){effects.Remove(node.gameObject);Destroy(node.gameObject);}
        }
        public void Button()=>Play("button");
        void Play(string name){AudioClip clip;if(clips.TryGetValue(name,out clip)&&clip&&sfx)sfx.PlayOneShot(clip);}
    }
}
