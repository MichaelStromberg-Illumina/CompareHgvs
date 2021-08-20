using System.Collections.Generic;

namespace CompareHgvs.DataStructures
{
    public record Variant(string VID, bool RefAlleleHasNs, string HgvsGenomic,
        Dictionary<string, Transcript> IdToTranscript);
}