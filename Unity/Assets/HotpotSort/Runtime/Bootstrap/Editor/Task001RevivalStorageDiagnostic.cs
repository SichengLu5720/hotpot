#if UNITY_EDITOR
using System;
using HotpotSort.Contracts;
using HotpotSort.Session;
using UnityEngine;

namespace HotpotSort.Bootstrap
{
    public static class Task001RevivalStorageDiagnostic
    {
        static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        public static void Verify()
        {
            IntegrationDiagnostic.VerifyBoot();
            string key="HotpotSort.Task001.v7.IsolatedQA."+Guid.NewGuid().ToString("N");
            try
            {
                PlayerPrefs.SetString(key,"{\"wins\":[\"20260920\"],\"claims\":[\"old-request\"],\"quotas\":[{\"day\":\"20260922\",\"used\":1}],\"music\":false,\"effects\":true,\"musicVolume\":0.25,\"effectsVolume\":0.7}");
                var utc=DateTimeOffset.Parse("2026-09-22T00:00:00Z");
                var store=new LocalDevelopmentServices(key);
                Assert(store.SharesUsed("20260922")==1&&store.ReadShareAvailability("20260922",utc).Available,"legacy used/timestamp migration");
                Assert(!store.TryReserveShare("20260922","old-request",utc),"legacy claimed request reused");
                Assert(store.TryReserveShare("20260922","new-effective",utc)&&store.TryCommitShare("20260922","new-effective",utc),"legacy next share");
                var reload=new LocalDevelopmentServices(key);
                Assert(reload.SharesUsed("20260922")==2&&!reload.ReadShareAvailability("20260922",utc.AddSeconds(899.999)).Available&&reload.ReadShareAvailability("20260922",utc.AddSeconds(900)).Available,"durable 15 minute edge");
                Assert(reload.TotalFirstWins==1&&!reload.LoadSettings().MusicEnabled&&Math.Abs(reload.LoadSettings().EffectsVolume-.7f)<.0001,"profile/settings regression");
                Assert(!reload.TryReserveShare("20260923","new-effective",utc),"committed ID not durable");
                Assert(reload.ReadShareAvailability("20260923",utc).Available,"new bucket inherited cooldown");
                Debug.Log("TASK001_V7_STORAGE_OK: real PlayerPrefs/JsonUtility legacy used-only migration, durable timestamp/request ID, 899.999/900s boundary, wins/settings and independent day buckets. Isolated QA key only.");
            }
            finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
#endif
