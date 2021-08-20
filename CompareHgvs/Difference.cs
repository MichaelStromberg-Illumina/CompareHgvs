namespace CompareHgvs
{
    public record Difference(string Vid, string HgvsGenomic, string Key, string Expected, string Actual)
    {
        public override string ToString() => $"VID: {Vid} ({HgvsGenomic}), key: {Key}, expected: {Expected}, actual: {Actual}";
    }
}