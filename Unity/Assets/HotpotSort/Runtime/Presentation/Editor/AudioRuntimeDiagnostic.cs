#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotpotSort.Bootstrap;
using HotpotSort.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PlayerSettings=HotpotSort.Contracts.PlayerSettings;

namespace HotpotSort.Presentation
{
    [InitializeOnLoad]
    public static class AudioRuntimeDiagnostic
    {
        const string Key="Hotpot.AudioRuntimeDiagnostic";
        static AudioRuntimeDiagnostic(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))Execute();};}
        public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");EditorApplication.EnterPlaymode();}
        static void Check(bool value,string label){if(!value)throw new Exception(label);Debug.Log("AUDIO_RUNTIME_PASS "+label);}
        static async void Execute()
        {
            int code=0;
            try
            {
                DailyProductionComposition composition=null;
                for(int i=0;i<200;i++){composition=UnityEngine.Object.FindFirstObjectByType<DailyProductionComposition>();if(composition&&composition.PlayerView&&composition.PlayerView.LastSnapshot!=null)break;await Task.Delay(50);}
                var view=composition.PlayerView;
                Check(view.LastSnapshot.phase==ViewPhase.Entry&&view.Audio,"real boot entry has audio runtime");
                var audio=view.Audio;
                Check(audio.ResourceError==null,"all approved audio resources loaded");
                foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))
                {
                    var clip=audio.ClipFor(cue);
                    if(cue==AudioCue.Music){Check(!clip,"current music removed");continue;}
                    Check(clip&&clip.name==cue.ToString()&&clip.length>0&&clip.channels==((int)cue<3?2:1),"approved clip imported "+cue);
                }
                Check(Enum.GetNames(typeof(AudioCue)).Length==12,"exactly twelve cues with no button or tool audio");
                var sources=view.GetComponents<AudioSource>();
                var music=sources.First(s=>s.loop&&!s.clip);
                var shop=sources.First(s=>s.clip==audio.ClipFor(AudioCue.Shop));
                var boiling=sources.First(s=>s.clip==audio.ClipFor(AudioCue.Boiling));
                Check(sources.Count(s=>s.loop)==3&&music.loop&&shop.loop&&boiling.loop,"only music and environment loop");
                audio.SetSettings(new PlayerSettings{MusicVolume=.6f,EffectsVolume=.8f});await Task.Delay(450);
                Check(!music.isPlaying&&shop.isPlaying&&shop.time>0&&boiling.volume==0,"home shop playing without BGM, boiling inaudible");
                float shopPosition=shop.time;audio.LoadApprovedResources();
                Check(shop.time>=shopPosition-.02f,"same resource rebind preserves shop position");
                int clicks=0;view.ButtonHapticRequested+=()=>clicks++;
                var settings=view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_设置");
                settings.interactable=false;settings.onClick.Invoke();Check(clicks==0,"disabled callback silent");settings.interactable=true;
                settings.onClick.Invoke();Check(clicks==1,"settings accepted click exactly once");
                await Task.Delay(350);Check(shop.volume<.05f,"settings ducks environment");
                var done=view.GetComponentsInChildren<Button>().First(b=>b.name=="Action_完成");done.onClick.Invoke();Check(clicks==2,"modal done click exactly once");
                view.SetForeground(false);settings.onClick.Invoke();Check(clicks==2&&view.Audio.Suspended,"background blocks clicks and suspends audio");view.SetForeground(true);
                await Task.Delay(300);shopPosition=shop.time;
                view.Audio.SetState(new ViewSnapshot{phase=ViewPhase.Running,pauseReasons=ViewPauseReasons.Reward});Check(view.Audio.Suspended,"reward suspends audio");
                await Task.Delay(220);Check(Math.Abs(shop.time-shopPosition)<.08f,"reward pauses source position");
                view.Audio.SetState(new ViewSnapshot{phase=ViewPhase.Running});Check(!view.Audio.Suspended,"reward return resumes audio");
                await Task.Delay(300);Check(shop.time>shopPosition&&boiling.volume>0,"resume advances shop and enables gameplay boiling");
                view.Audio.SetSettings(new PlayerSettings{EffectsEnabled=false,MusicEnabled=false});
                Check(sources.All(s=>s.volume==0),"both independent switches mute all sources");
                foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))view.Audio.Bind(cue,null);
                audio.ResetTransient();
                foreach(AudioCue cue in Enum.GetValues(typeof(AudioCue)))view.Audio.Play(cue);
                Check(view.GetComponents<AudioSource>().All(s=>!s.isPlaying),"missing assets remain silent");
                Check(audio.LoadApprovedResources(),"approved resources restored");
                view.SetAudioSettings(new PlayerSettings());
                await Task.Delay(300);shopPosition=shop.time;
                var boot=UnityEngine.Object.FindFirstObjectByType<HotpotSort.Bootstrap.Bootstrap>();
                composition.SessionAction(ViewAction.StartToday);if(boot.PendingStart!=null)await boot.PendingStart;
                Check(view.LastSnapshot.phase==ViewPhase.Running,"real gameplay entry");
                Check(shop.time>=shopPosition-.02f,"home to real gameplay keeps environment continuous");
                audio.SetSettings(new PlayerSettings{MusicVolume=.25f,EffectsVolume=.4f});await Task.Delay(400);
                Check(!music.clip&&!music.isPlaying&&shop.isPlaying&&boiling.isPlaying,"music settings retained without BGM; environment continues");
                audio.Play(AudioCue.OrderComplete);audio.Play(AudioCue.OrderComplete);
                await Task.Delay(40);
                Check(sources.Count(s=>!s.loop&&s.isPlaying&&s.clip==audio.ClipFor(AudioCue.OrderComplete))==1,"same-frame burst deduplicated");
                await Task.Delay(70);audio.Play(AudioCue.OrderComplete);await Task.Delay(70);audio.Play(AudioCue.OrderComplete);
                Check(sources.Count(s=>!s.loop&&s.isPlaying&&s.clip==audio.ClipFor(AudioCue.OrderComplete))==2,"same cue capped at two concurrent voices");
                Check(sources.Where(s=>!s.loop&&s.isPlaying).All(s=>Math.Abs(s.volume-.26f)<.02f),"effects volume applied independently");
                audio.ResetTransient();audio.Play(AudioCue.PotArrival);audio.Play(AudioCue.OrderComplete);await Task.Delay(40);
                Check(sources.Count(s=>!s.loop&&s.isPlaying)==2,"different cues in one event frame retain separate voices");
                var heard=new List<AudioCue>();view.Audio.TerminalCueAdvanced+=heard.Add;
                view.Audio.SetState(new ViewSnapshot{sessionId="audio-diagnostic",sessionGeneration=1,phase=ViewPhase.Won});
                view.Audio.BeginTerminal("stale",1,true,true,true,AudioCue.Win);Check(heard.Count==0,"stale terminal sequence rejected");
                view.Audio.BeginTerminal("audio-diagnostic",1,true,true,true,AudioCue.Win);
                Check(heard.SequenceEqual(new[]{AudioCue.PotArrival}),"terminal starts at pot arrival");
                view.Audio.SetForeground(false);view.Audio.AdvanceTerminal(10);Check(heard.Count==1,"background freezes terminal sequence");
                view.Audio.SetForeground(true);view.Audio.AdvanceTerminal(10);view.Audio.AdvanceTerminal(10);view.Audio.AdvanceTerminal(10);
                Check(heard.SequenceEqual(new[]{AudioCue.PotArrival,AudioCue.OrderComplete,AudioCue.Serve,AudioCue.Win}),"terminal cue order preserved");
                view.Audio.SetState(new ViewSnapshot{sessionId="next",sessionGeneration=2,phase=ViewPhase.Overflow});
                view.Audio.BeginTerminal("next",2,true,true,true,AudioCue.Lose);
                view.Audio.SetState(new ViewSnapshot{phase=ViewPhase.Entry});int before=heard.Count;
                view.Audio.AdvanceTerminal(10);Check(heard.Count==before&&view.Audio.PendingTerminalCues==0,"exit clears pending terminal sequence");
                view.Audio.SetState(new ViewSnapshot{sessionId="old",sessionGeneration=3,phase=ViewPhase.Won});
                view.Audio.BeginTerminal("old",3,true,true,true,AudioCue.Win);
                view.Audio.SetState(new ViewSnapshot{sessionId="new",sessionGeneration=4,phase=ViewPhase.Running});before=heard.Count;
                view.Audio.AdvanceTerminal(10);Check(heard.Count==before&&view.Audio.PendingTerminalCues==0,"new session clears pending terminal sequence");
                bool missingLogged=false;
                Check(audio.LoadApprovedResources(path=>{if(path.EndsWith("/Music"))throw new Exception("removed music must not be loaded");return Resources.Load<AudioClip>(path);}),"BGM is excluded from resource loading");
                Application.LogCallback onLog=(message,trace,type)=>{if(type==LogType.Error&&message=="TASK009_AUDIO_RESOURCE_MISSING: "+GameplayAudio.ApprovedResourceRoot+"Shop")missingLogged=true;};
                Application.logMessageReceived+=onLog;
                try{Check(!audio.LoadApprovedResources(path=>path.EndsWith("/Shop")?null:Resources.Load<AudioClip>(path)),"injected missing required environment returns failure");}
                finally{Application.logMessageReceived-=onLog;}
                Check(missingLogged&&audio.ResourceError!=null,"expected missing-resource failure includes exact path in error log");
                Check(audio.LoadApprovedResources()&&audio.ResourceError==null,"real resources recover after missing fixture");
                Debug.Log("AUDIO_RUNTIME_COMPLETE");
            }
            catch(Exception e){code=1;Debug.LogException(e);}
            finally{SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
        }
    }
}
#endif
