using System;
using System.Linq;
using HotpotSort.Core;
using HotpotSort.Determinism;

namespace HotpotSort.Bootstrap
{
    // Independent oracle transcribed from TASK-001 v5 Designer handoff, not the importer/provider.
    public static class FixedCGolden
    {
        public const string Sequence="ABCDE|BDFGG|EHICC|EJD|AAECH|HHB|BGHCC|CJFD|KHBC|HGB|EEBC|DDJF|DJIL|JELCI|JDICC|FEKL|CJHIG|FGJA|DBBG|HHI|KJHGC|JJJHG|CK|MMC|CF|KFJN|KHC|EAHI|LOIC|ENI|HEI|PKECG|BHM|II|LJEP|DAFI|DJP|PC|NPO|NNC|FOM|EAMH|CCA|P|JN|MH|CB|CCEAI|KLGJJ|GDIKB";
        public static void Verify(DailyContent content,DailySession session)
        {
            var plates=Sequence.Split('|');
            if(content.Plates.Count!=50 || string.Join("|",content.Plates.Select(p=>string.Concat(p.Kinds)))!=Sequence)
                throw new InvalidOperationException("C01.content: golden plate sequence mismatch");
            var state=CanonicalJson.Map(CanonicalJson.Parse(session.Snapshot.CanonicalStateJson));
            var items=CanonicalJson.Array(state["items"]).Select(CanonicalJson.Map).ToArray();int index=0;
            if(items.Length!=183)throw new InvalidOperationException("C01.items: expected 183");
            for(int plate=0;plate<50;plate++)
            {
                if(content.Plates[plate].PlateId!=plate+1)throw new InvalidOperationException("C01.plateId");
                for(int source=0;source<plates[plate].Length;source++,index++)
                {
                    var item=items[index];
                    if(CanonicalJson.Int(item["itemId"])!=index+1 || CanonicalJson.Int(item["plateId"])!=plate+1
                        || CanonicalJson.Int(item["sourceIndex"])!=source || (string)item["kind"]!=plates[plate][source].ToString())
                        throw new InvalidOperationException("C01.item: mismatch at "+(index+1));
                }
            }
        }
    }
}
