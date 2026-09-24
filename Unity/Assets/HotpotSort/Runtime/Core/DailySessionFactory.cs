using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotpotSort.Contracts;
using HotpotSort.Determinism;

namespace HotpotSort.Core
{
    public sealed class DailySessionFactory : IGameSessionFactory
    {
        public DailyContent Content { get; }
        public IReadOnlyList<IngredientEntry> Catalog { get; }
        public string CatalogDigest { get; }
        public string ConfigurationDigest { get; }
        public static DailySessionFactory FromProductionJson(string json)
        {
            var content=DailyContent.LoadProduction(json);
            var catalog=Enumerable.Range(0,16).Select(id=>new IngredientEntry("food_"+id.ToString("00"),"food/food_"+id.ToString("00"),"food_"+id.ToString("00"),"alpha-hit-radius-16-board-units","standard",content.ContentVersion));
            return new DailySessionFactory(content,catalog);
        }
        public DailySessionFactory(DailyContent content, IEnumerable<IngredientEntry> catalog)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Catalog = new ReadOnlyCollection<IngredientEntry>(new List<IngredientEntry>(catalog).OrderBy(x => x.IngredientId, StringComparer.Ordinal).ToList());
            if (Catalog.Count != 16 || Catalog.Any(x => x.ContentVersion != content.ContentVersion) || Catalog.Select(x => x.IngredientId).Distinct(StringComparer.Ordinal).Count() != 16)
                throw new ArgumentException("Catalog must contain exactly 16 distinct entries valid for this content version");
            CatalogDigest = CanonicalJson.Hash(CanonicalJson.Write(Catalog.Select(x => x.Json()).ToArray()));
            ConfigurationDigest = CanonicalJson.Hash(CanonicalJson.Write(CanonicalJson.Object("contentDigest", content.Digest, "catalogDigest", CatalogDigest)));
        }
        public IGameSession CreateSession(ChallengeContext context) => CreateDailySession(context);
        public DailySessionFactory ForReplayContent(string digest)
        {
            if(Content.Digest==digest)return this;
            var historical=KnownDailyProfiles.Resolve(Content,digest);
            // Catalog embeds contentVersion; never reuse the current catalog hash.
            return FromProductionJson(historical.CanonicalJsonText);
        }
        public DailySession CreateDailySession(ChallengeContext context, DailyRulesVersion rules = DailyRulesVersion.RevivalV3) => new DailySession(this, context, null, rules);
        public DailySession CreateFixtureSession(ChallengeContext context, DailyFixture fixture, DailyRulesVersion rules = DailyRulesVersion.RevivalV3) => new DailySession(this, context, fixture ?? throw new ArgumentNullException(nameof(fixture)), rules);
    }
}
