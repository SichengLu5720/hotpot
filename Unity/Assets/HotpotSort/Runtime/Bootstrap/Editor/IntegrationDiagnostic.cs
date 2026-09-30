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
            VerifyBoot();
            HotpotSort.Build.WeChatBuild.CompilePlayerDiagnostic();
        }
        public static void VerifyBoot()
        {
            HotpotSort.ContentImport.DailyContentBuildGuard.Validate();
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
                var content=composition.ProductionContent;
                FixedCGolden.Verify(content,session);
                var asset=new SerializedObject(composition).FindProperty("dailyContent").objectReferenceValue;
                if(AssetDatabase.GetAssetPath(asset)!="Assets/HotpotSort/Content/Daily/"+DailyContent.RuntimeFileName)
                    throw new InvalidOperationException("C08: Boot must select the canonical project content asset");
                var mapped=DailyViewMapper.Map(result.Snapshot,result.Events,content,0);
                if(mapped.snapshot.plates.Length!=1 || mapped.snapshot.buffer.Length!=5 || mapped.snapshot.orders.Length!=4)
                    throw new InvalidOperationException("Mapped view shape mismatch");
                int id=int.Parse(mapped.snapshot.plates[0].items[0].itemId);
                var tap=session.Tap(new TapCommand(id,1,2,true));
                if(!tap.Accepted)throw new InvalidOperationException("Tap mapping: "+tap.Reason);
                DailyViewMapper.Map(tap.Snapshot,tap.Events,content,0);
            }
            Debug.Log("TASK001_V5_BOOT_INTEGRATION_OK: "+composition.ContentVersion+" content="+composition.ProductionContent.Digest+" configuration="+composition.ConfigurationDigest+"; C01 golden inventory, C08 actual Boot binding, real factory and supply/tap mapping. Editor diagnostic only; not WebGL or full QA.");
        }
    }
}
