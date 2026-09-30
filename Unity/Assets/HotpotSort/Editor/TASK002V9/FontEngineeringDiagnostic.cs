using System;
using System.IO;
using HotpotSort.Platform;
using UnityEditor;
using UnityEngine;

namespace HotpotSort.Build
{
    public static class FontEngineeringDiagnostic
    {
        // Explicit batch entry after integration; never changes scenes, imports or font bindings.
        public static void Run()
        {
            try
            {
                FontSubsetBuildGuard.Validate();
                var errors=HotpotSort.Presentation.TaskAssetValidation.ValidateModernFont();
                if(errors.Length!=0)throw new Exception(string.Join(";",errors));
                var config=WeChatRuntimeConfig.FromJson("{\"schemaVersion\":1,\"protocolVersion\":1,\"environment\":\"development\"}");
                if(config.CloudState!=WeChatCapabilityState.NotConfigured || config.RewardedVideoState!=WeChatCapabilityState.NotConfigured)throw new Exception("Missing identifiers must be NotConfigured");
                bool rejected=false;
                try{WeChatRuntimeConfig.FromJson("{}");}catch(FormatException){rejected=true;}
                if(!rejected)throw new Exception("Missing schema was accepted");
                string configuredReport=Environment.GetEnvironmentVariable("HOTPOT_TASK002_FONT_REPORT");
                string report=Path.GetFullPath(string.IsNullOrWhiteSpace(configuredReport) ? Path.Combine(Application.dataPath,"../../.harness/qa/TASK-002/v9/font-export-r001/unity-font-result.json") : configuredReport);
                Directory.CreateDirectory(Path.GetDirectoryName(report));
                File.WriteAllText(report,"{\"status\":\"PASS\",\"checks\":[\"imported-glyph-coverage\",\"font-data-embedded\",\"resource-load\",\"font-hashes\",\"configuration-json\"]}");
                Debug.Log("TASK002_UNITY_FONT_ENGINEERING_PASS");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception ex){Debug.LogError("TASK002_UNITY_FONT_ENGINEERING_FAILED "+ex.Message);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
    }
}
