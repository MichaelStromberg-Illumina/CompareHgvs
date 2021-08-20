using System.Text.RegularExpressions;

namespace CompareHgvs
{
    public static class HgvsProteinTransforms
    {
        private static readonly Regex SilentRegex    = new(@"\(\w{3}\d+=\)", RegexOptions.Compiled);
        private static readonly Regex TerNumTerRegex = new(@"(\(Ter\d+)Ter\)", RegexOptions.Compiled);

        public static string TransformBiocommons(string nirvana, string biocommons)
        {
            if (nirvana == null) return biocommons;

            Match nirvanaMatch = SilentRegex.Match(nirvana);
            if (!nirvanaMatch.Success) return biocommons;

            // (Ter1059=)
            string nirvanaString = nirvanaMatch.Groups[0].Value;

            // transform TerNumTer mutations
            Match biocommonsMatch = TerNumTerRegex.Match(biocommons);
            if (biocommonsMatch.Success)
            {
                var biocommonsString = $"{biocommonsMatch.Groups[1].Value}=)";
                if (nirvanaString == biocommonsString) return nirvana;
            }

            // transform silent mutations
            biocommonsMatch = SilentRegex.Match(biocommons);
            if (biocommonsMatch.Success)
            {
                var biocommonsString = $"{biocommonsMatch.Groups[0].Value}";
                if (nirvanaString == biocommonsString) return nirvana;
            }

            return biocommons;
        }
    }
}