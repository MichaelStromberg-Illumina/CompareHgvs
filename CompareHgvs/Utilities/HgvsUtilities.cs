using System;
using System.IO;

namespace CompareHgvs.Utilities
{
    public static class HgvsUtilities
    {
        public static string GetAccession(string hgvs)
        {
            if (hgvs is null or "non-coding") return null;
            ReadOnlySpan<char> hgvsSpan = hgvs.AsSpan();
            int                colonPos = hgvsSpan.IndexOf(':');
            if (colonPos == -1) throw new InvalidDataException($"Could not find the colon in [{hgvs}]");
            return hgvsSpan.Slice(0, colonPos).ToString();
        }
    }
}