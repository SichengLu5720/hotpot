using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using HotpotSort.Contracts;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed class DailySessionFactory : IGameSessionFactory
    {
        readonly object attemptGate = new object();
        string activeAttemptKey, activeSessionSeed;
        public DailyContent Content { get; }
        readonly DailyContent replayBaseContent;
        public DailyLayoutCatalog LayoutCatalog { get; }
        public DailyLayoutPair Layout { get; }
        public IReadOnlyList<IngredientEntry> Catalog { get; }
        public string CatalogDigest { get; }
        public string ConfigurationDigest { get; }
        public static DailySessionFactory FromProductionJson(string json,string layoutCatalogJson=null)
        {
            var content=DailyContent.LoadProduction(json);
            var catalog=Enumerable.Range(0,16).Select(id=>new IngredientEntry("food_"+id.ToString("00"),"food/food_"+id.ToString("00"),"food_"+id.ToString("00"),"alpha-hit-radius-16-board-units","standard",content.ContentVersion));
            var layouts=string.IsNullOrWhiteSpace(layoutCatalogJson)?null:DailyLayoutCatalog.Load(layoutCatalogJson,CanonicalJson.Hash(layoutCatalogJson.TrimEnd('\r','\n')));
            return new DailySessionFactory(content,catalog,layouts,null,content);
        }
        public DailySessionFactory(DailyContent content, IEnumerable<IngredientEntry> catalog)
            :this(content,catalog,null,null,content)
        {
        }
        DailySessionFactory(DailyContent content, IEnumerable<IngredientEntry> catalog,DailyLayoutCatalog layoutCatalog,DailyLayoutPair layout,DailyContent replayBaseContent)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Catalog = new ReadOnlyCollection<IngredientEntry>(new List<IngredientEntry>(catalog).OrderBy(x => x.IngredientId, StringComparer.Ordinal).ToList());
            if (Catalog.Count < 16 || Catalog.Count > CollectionCatalog.Count || Catalog.Any(x => x.ContentVersion != content.ContentVersion) || Catalog.Select(x => x.IngredientId).Distinct(StringComparer.Ordinal).Count() != Catalog.Count)
                throw new ArgumentException("Catalog must contain 16 to 32 distinct entries valid for this content version");
            CatalogDigest = CanonicalJson.Hash(CanonicalJson.Write(Catalog.Select(x => x.Json()).ToArray()));
            ConfigurationDigest = CanonicalJson.Hash(CanonicalJson.Write(CanonicalJson.Object("contentDigest", content.Digest, "catalogDigest", CatalogDigest)));
            LayoutCatalog=layoutCatalog;Layout=layout;this.replayBaseContent=replayBaseContent??content;
        }
        public IGameSession CreateSession(ChallengeContext context) => CreateDailySession(context);
        // Supply the complete player-selected pool. The attempt seed chooses the
        // actual 16 and their A..P bijection without consuming any gameplay RNG.
        public DailySessionFactory WithIngredientSelection(IEnumerable<string> ingredientIds)
        {
            if(ingredientIds==null)throw new ArgumentNullException(nameof(ingredientIds));
            var ids=ingredientIds.ToArray();
            if(ids.Length<16||ids.Length>CollectionCatalog.Count||ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length||ids.Any(x=>CollectionCatalog.Index(x)<0))throw new ArgumentException("16 to 32 distinct known ingredients required");
            return new DailySessionFactory(Content,ids.Select(id=>new IngredientEntry(id,"food/"+id,id,"alpha-hit-radius-16-board-units","standard",Content.ContentVersion)),LayoutCatalog,Layout,replayBaseContent);
        }
        public DailySessionFactory WithLayoutForChallenge(string challengeId)
        {
            if(LayoutCatalog==null)return this;
            DateTime day;
            if(!DateTime.TryParseExact(challengeId,new[]{"yyyyMMdd","yyyy-MM-dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out day))throw new ArgumentException("Challenge identity is not a challenge day",nameof(challengeId));
            int index=(int)(((day.Date-new DateTime(1970,1,1)).TotalDays%7+7)%7);
            var selected=LayoutCatalog.Layouts[index];if(Layout?.Index==selected.Index)return this;
            var content=new DailyContent(Content.ContentVersion,selected.FormalPlates,Content.Rows);
            var catalog=Catalog.Select(x=>new IngredientEntry(x.IngredientId,x.ResourceAddress,x.DisplayNameKey,x.ColliderSpec,x.SizeClass,content.ContentVersion));
            return new DailySessionFactory(content,catalog,LayoutCatalog,selected,replayBaseContent);
        }
        internal IReadOnlyList<PlateDefinition> WarmupPlates(string day,string version)=>Layout?.WarmupPlates??WarmupContent.Generate(day,version);
        internal IReadOnlyList<string> WarmupOpeningKinds=>Layout?.WarmupOpeningKinds;
        public DailySession CreateStage(ChallengeContext context, ChallengeStage stage, int inheritedPotMask=0)
            =>CreateStage(context,stage,inheritedPotMask,0);
        public DailySession CreateStage(ChallengeContext context, ChallengeStage stage, int inheritedPotMask,int consecutiveFormalFailuresAtStart)
            =>CreateStageInternal(context,stage,inheritedPotMask,consecutiveFormalFailuresAtStart,2,0);
        public DailySession CreateStageWithProtection(ChallengeContext context,ChallengeStage stage,int inheritedPotMask,int orderProtectionPercent,int inheritedToolUseMask=0)
            =>CreateStageInternal(context,stage,inheritedPotMask,0,3,orderProtectionPercent,2,1,inheritedToolUseMask);
        // Version 4 changes only the allowed frozen protection range; D1 stays identical.
        public DailySession CreateStageWithLifecycleProtection(ChallengeContext context,ChallengeStage stage,int inheritedPotMask,int orderProtectionPercent,int inheritedToolUseMask=0)
            =>CreateStageInternal(context,stage,inheritedPotMask,0,4,orderProtectionPercent,2,1,inheritedToolUseMask);
        DailySession CreateStageInternal(ChallengeContext context,ChallengeStage stage,int inheritedPotMask,int consecutiveFormalFailuresAtStart,int supportVersion,int protectionPercent,int potUnlockVersion=1,int toolLimitVersion=0,int inheritedToolUseMask=0)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            string key=context.ChallengeId+"\n"+context.ContentVersion+"\n"+context.ConfigurationDigest+"\n"+context.RetryIndex;
            string value=context.SessionSeed;
            lock(attemptGate)
            {
                if(value==null && stage==ChallengeStage.Formal && activeAttemptKey==key)value=activeSessionSeed;
                if(value==null)value=NewSessionSeed();
                if(stage==ChallengeStage.Warmup||activeAttemptKey!=key){activeAttemptKey=key;activeSessionSeed=value;}
            }
            return new DailySession(this,WithSeed(context,value),null,DailyRulesVersion.RevivalV3,stage,inheritedPotMask,true,consecutiveFormalFailuresAtStart,supportVersion,protectionPercent,potUnlockVersion,toolLimitVersion,inheritedToolUseMask);
        }
        public DailySessionFactory ForReplayContent(string digest)
        {
            if(Content.Digest==digest)return this;
            var historical=KnownDailyProfiles.Resolve(replayBaseContent,digest);
            // Catalog embeds contentVersion; never reuse the current catalog hash.
            return new DailySessionFactory(historical,Catalog.Select(x=>new IngredientEntry(x.IngredientId,x.ResourceAddress,x.DisplayNameKey,x.ColliderSpec,x.SizeClass,historical.ContentVersion)));
        }
        public DailySessionFactory ForReplayContent(string digest,string challengeId)
        {
            if(Content.Digest==digest)return this;
            var selected=WithLayoutForChallenge(challengeId);
            return selected.Content.Digest==digest?selected:selected.ForReplayContent(digest);
        }
        public DailySession CreateDailySession(ChallengeContext context, DailyRulesVersion rules = DailyRulesVersion.RevivalV3)
        {
            if(rules==DailyRulesVersion.LegacyV2)return new DailySession(this,context,null,rules,ChallengeStage.Legacy,0,false);
            var seeded=WithSeed(context,context.SessionSeed??NewSessionSeed());
            return new DailySession(this,seeded,null,rules,ChallengeStage.Legacy,0,true);
        }
        public DailySession CreateFixtureSession(ChallengeContext context, DailyFixture fixture, DailyRulesVersion rules = DailyRulesVersion.RevivalV3)
        {
            if(rules==DailyRulesVersion.LegacyV2)return new DailySession(this,context,fixture??throw new ArgumentNullException(nameof(fixture)),rules,ChallengeStage.Legacy,0,false);
            var seeded=WithSeed(context,context.SessionSeed??NewSessionSeed());
            return new DailySession(this,seeded,fixture??throw new ArgumentNullException(nameof(fixture)),rules,ChallengeStage.Legacy,0,true);
        }
        internal DailySession CreateHistoricalSession(ChallengeContext context, DailyFixture fixture, DailyRulesVersion rules, ChallengeStage stage, int inheritedPotMask)
            =>new DailySession(this,context,fixture,rules,stage,inheritedPotMask,false);
        internal DailySession CreateReplaySession(ChallengeContext context, DailyFixture fixture, ChallengeStage stage, int inheritedPotMask, int consecutiveFormalFailuresAtStart=0,int orderSupportVersion=1,int orderProtectionPercentAtStart=0,int potUnlockVersion=1,int toolLimitVersion=0,int inheritedToolUseMask=0)
            =>new DailySession(this,context,fixture,DailyRulesVersion.RevivalV3,stage,inheritedPotMask,true,consecutiveFormalFailuresAtStart,orderSupportVersion,orderProtectionPercentAtStart,potUnlockVersion,toolLimitVersion,inheritedToolUseMask);
        static ChallengeContext WithSeed(ChallengeContext context,string value)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            return new ChallengeContext(context.ChallengeId,context.ContentVersion,context.ConfigurationDigest,context.TimeSource,context.RetryIndex,value);
        }
        static string NewSessionSeed()=>BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(),0).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
