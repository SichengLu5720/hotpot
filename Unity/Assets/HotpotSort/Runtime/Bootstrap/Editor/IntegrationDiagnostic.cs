using System;
using HotpotSort.Contracts;
using HotpotSort.Core;
using HotpotSort.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    // Minimal integration diagnostic, not the release QA suite or device validation.
    public static class IntegrationDiagnostic
    {
        public static void Verify()
        {
            EditorSceneManager.OpenScene("Assets/HotpotSort/Scenes/Boot.unity");
            var boot=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();
            if(!boot)throw new InvalidOperationException("Boot component missing");
            var composition=new SerializedObject(boot).FindProperty("composition").objectReferenceValue as DailyProductionComposition;
            if(!composition || composition.UsesDevelopmentDoubles)throw new InvalidOperationException("Production composition missing");
            if(composition.Read().phase!=ViewPhase.Entry)throw new InvalidOperationException("Expected entry before player start");
            // Exercise the same real factory selected by production; no fixture or seeded prototype.
            var context=new ChallengeContext("20260920",composition.ContentVersion,composition.ConfigurationDigest,"integration-diagnostic",0);
            using(var session=(DailySession)composition.CoreFactory.CreateSession(context))
            {
                if(session.Snapshot.Status!=GameStatus.Running)throw new InvalidOperationException("Core failed to initialize");
                var result=session.Supply(new SupplyObservation(1,1,true,true));
                if(!result.Accepted)throw new InvalidOperationException("Supply mapping: "+result.Reason);
                var contentAsset=new SerializedObject(composition).FindProperty("dailyContent").objectReferenceValue as TextAsset;
                var content=DailyContent.Load(contentAsset.text,"e6612cb54b548b81aabef9bc376441682ff0157b556b5a6258cba4c7057e5d08");
                var mapped=DailyViewMapper.Map(result.Snapshot,result.Events,content,0);
                if(mapped.snapshot.plates.Length!=1 || mapped.snapshot.buffer.Length!=5 || mapped.snapshot.orders.Length!=4)
                    throw new InvalidOperationException("Mapped view shape mismatch");
                int id=int.Parse(mapped.snapshot.plates[0].items[0].itemId);
                var tap=session.Tap(new TapCommand(id,1,2,true));
                if(!tap.Accepted)throw new InvalidOperationException("Tap mapping: "+tap.Reason);
                DailyViewMapper.Map(tap.Snapshot,tap.Events,content,0);
            }
            HotpotSort.Build.WeChatBuild.CompilePlayerDiagnostic();
            Debug.Log("HOTPOT_RELEASE_INTEGRATION_OK: Boot binding, entry state, real factory, supply/tap snapshot mapping; WebGL scripts compiled. Not full QA.");
        }
    }
}
