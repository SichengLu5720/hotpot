using System;
using System.Collections.Generic;
using HotpotSort.Contracts;
using UnityEngine;

namespace HotpotSort.Presentation
{
    public enum AudioCue { Music, Shop, Boiling, FoodPress, Flight, PlateArrival, PotArrival, OrderComplete, Serve, NewPot, Win, Lose }

    // TASK009 A+ and the accompanying cues were approved on 2026-09-24.
    public sealed class GameplayAudio : MonoBehaviour
    {
        readonly AudioClip[] clips=new AudioClip[Enum.GetValues(typeof(AudioCue)).Length];
        readonly double[] lastPlayed=new double[Enum.GetValues(typeof(AudioCue)).Length];
        readonly AudioSource[] loops=new AudioSource[3],voices=new AudioSource[12];
        readonly AudioCue[] voiceCues=new AudioCue[12];
        readonly double[] voiceStartReservations=new double[12];
        PlayerSettings settings=new PlayerSettings();
        bool foreground=true,applicationPaused,focused=true,suspended,ducked,settingsOpen,inGame,terminal;
        ViewPauseReasons pauses;
        float resumeGain=1;
        readonly Queue<AudioCue> terminalCues=new Queue<AudioCue>();
        AudioSource terminalVoice;
        string session;
        long generation;
        float terminalWait;
        bool terminalQueued;
        public event Action<AudioCue> TerminalCueAdvanced;
        public int PendingTerminalCues=>terminalCues.Count;
        public bool Suspended=>suspended;
        public const string ApprovedResourceRoot="HotpotSort/Audio/TASK009/";
        public string ResourceError {get;private set;}
        public AudioClip ClipFor(AudioCue cue)=>clips[(int)cue];
        public bool LoadApprovedResources(Func<string,AudioClip> loader=null)
        {
            var missing=new List<string>();
            foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                string path=ApprovedResourceRoot+cue;
                var clip=loader!=null?loader(path):Resources.Load<AudioClip>(path);
                if(!clip||clip.length<=0||clip.loadState==AudioDataLoadState.Failed)
                {missing.Add(path);clip=null;}
                Bind(cue,clip);
            }
            ResourceError=missing.Count==0?null:"TASK009_AUDIO_RESOURCE_MISSING: "+string.Join(", ",missing);
            if(ResourceError!=null)Debug.LogError(ResourceError,this);
            return ResourceError==null;
        }
        public void Bind(AudioCue cue,AudioClip clip)
        {
            if(clips[(int)cue]==clip)return; // Rebinding identical approved assets must not restart music.
            clips[(int)cue]=clip;
            if((int)cue<3){loops[(int)cue].Stop();loops[(int)cue].clip=clip;if(clip&&!suspended)loops[(int)cue].Play();}
        }
        public void SetSettings(PlayerSettings value){settings=value??new PlayerSettings();ApplyVolumes(0);}
        public void SetSettingsOpen(bool value){settingsOpen=value;}
        public void SetForeground(bool value){foreground=value;RefreshSuspension();}
        public void SetState(ViewSnapshot state)
        {
            if(state==null)return;
            if(session!=state.sessionId||generation!=state.sessionGeneration||state.phase==ViewPhase.Entry||state.phase==ViewPhase.Aborted)
                ResetTransient();
            session=state.sessionId;generation=state.sessionGeneration;
            pauses=state.pauseReasons;
            inGame=state.phase==ViewPhase.Running||state.phase==ViewPhase.Paused;
            terminal=state.phase==ViewPhase.Won||state.phase==ViewPhase.Overflow;
            ducked=state.phase==ViewPhase.Paused||(pauses&ViewPauseReasons.User)!=0;
            RefreshSuspension();
        }
        public void ResetTransient()
        {
            terminalCues.Clear();terminalWait=0;terminalQueued=false;if(terminalVoice)terminalVoice.Stop();
            foreach(var voice in voices)if(voice)voice.Stop();
            Array.Clear(voiceStartReservations,0,voiceStartReservations.Length);
            for(int i=0;i<lastPlayed.Length;i++)lastPlayed[i]=double.NegativeInfinity;
        }
        // Terminal visuals intentionally close immediately. This independent sequence consumes
        // only accepted pending presentation events and never changes their visual timing.
        public void BeginTerminal(string expectedSession,long expectedGeneration,bool potArrival,bool completion,bool serve,AudioCue result)
        {
            if(suspended||!terminal||terminalQueued||session!=expectedSession||generation!=expectedGeneration)return;
            terminalQueued=true;
            if(potArrival)terminalCues.Enqueue(AudioCue.PotArrival);
            if(completion)terminalCues.Enqueue(AudioCue.OrderComplete);
            if(serve)terminalCues.Enqueue(AudioCue.Serve);
            terminalCues.Enqueue(result);
            AdvanceTerminal(0);
        }
        public void AdvanceTerminal(float delta)
        {
            if(suspended||!isActiveAndEnabled)return;
            terminalWait-=Mathf.Max(0,delta);
            if(terminalWait>0||terminalCues.Count==0)return;
            var cue=terminalCues.Dequeue();var clip=clips[(int)cue];
            TerminalCueAdvanced?.Invoke(cue);
            terminalWait=clip?clip.length:.12f;
            if(clip&&settings.EffectsEnabled&&settings.EffectsVolume>0)
            {terminalVoice.clip=clip;terminalVoice.volume=Mathf.Clamp01(settings.EffectsVolume)*.65f*resumeGain;terminalVoice.Play();}
        }
        public void Play(AudioCue cue)
        {
            if((int)cue<3||suspended||!settings.EffectsEnabled||settings.EffectsVolume<=0||!clips[(int)cue])return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(now-lastPlayed[(int)cue]<.055)return;
            int same=0,free=-1;
            // Some audio backends report isPlaying only after their next DSP update.
            // Reserve a newly assigned source so two cues in one event frame cannot overwrite it.
            for(int i=0;i<voices.Length;i++){if(!voices[i].isPlaying&&now>=voiceStartReservations[i]){if(free<0)free=i;}else if(voiceCues[i]==cue)same++;}
            if(same>=2||free<0)return;
            lastPlayed[(int)cue]=now;voiceCues[free]=cue;
            voiceStartReservations[free]=now+.1;
            voices[free].clip=clips[(int)cue];voices[free].volume=Mathf.Clamp01(settings.EffectsVolume)*.65f;voices[free].Play();
        }
        void Awake()
        {
            if(!FindFirstObjectByType<AudioListener>())gameObject.AddComponent<AudioListener>();
            for(int i=0;i<loops.Length;i++)loops[i]=Source(true);
            for(int i=0;i<voices.Length;i++)voices[i]=Source(false);
            terminalVoice=Source(false);
            ResetTransient();
            LoadApprovedResources();
        }
        AudioSource Source(bool loop){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.volume=0;return source;}
        void OnApplicationPause(bool value){applicationPaused=value;RefreshSuspension();}
        void OnApplicationFocus(bool value){focused=value;RefreshSuspension();}
        void OnDisable(){foreach(var source in loops)if(source)source.Pause();foreach(var source in voices)if(source)source.Pause();if(terminalVoice)terminalVoice.Pause();suspended=true;}
        void OnEnable(){RefreshSuspension();}
        void RefreshSuspension()
        {
            bool next=!isActiveAndEnabled||!foreground||applicationPaused||!focused||(pauses&(ViewPauseReasons.Background|ViewPauseReasons.Reward))!=0;
            if(next==suspended)return;
            suspended=next;
            if(next){foreach(var source in loops)if(source)source.Pause();foreach(var source in voices)if(source)source.Pause();if(terminalVoice)terminalVoice.Pause();}
            else
            {
                resumeGain=0;
                foreach(var source in loops)if(source&&source.clip){source.volume=0;source.UnPause();if(!source.isPlaying)source.Play();}
                foreach(var source in voices)if(source){source.volume=0;source.UnPause();}
                if(terminalVoice){terminalVoice.volume=0;terminalVoice.UnPause();}
            }
        }
        void Update(){if(!suspended){ApplyVolumes(Time.unscaledDeltaTime);AdvanceTerminal(Time.unscaledDeltaTime);}}
        void ApplyVolumes(float dt)
        {
            resumeGain=Mathf.MoveTowards(resumeGain,1,dt*4);
            float duck=(ducked||settingsOpen) ? .3f : 1;
            float music=settings.MusicEnabled?Mathf.Clamp01(settings.MusicVolume):0;
            float effects=settings.EffectsEnabled?Mathf.Clamp01(settings.EffectsVolume):0;
            for(int i=0;i<loops.Length;i++)
            {
                if(!loops[i])continue;
                float target=(i==0?(terminal?0:music):effects*(i==1?.16f:inGame?.22f:0))*duck*resumeGain;
                if((i==0&&!settings.MusicEnabled)||(i>0&&!settings.EffectsEnabled))loops[i].volume=0;
                else loops[i].volume=Mathf.MoveTowards(loops[i].volume,target,dt*2);
            }
            foreach(var voice in voices)if(voice)voice.volume=effects*.65f*resumeGain;
            if(terminalVoice)terminalVoice.volume=effects*.65f*resumeGain;
        }
    }
}
