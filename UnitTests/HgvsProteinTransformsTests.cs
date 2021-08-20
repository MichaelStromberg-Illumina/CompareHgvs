using CompareHgvs;
using Xunit;

namespace UnitTests
{
    public sealed class HgvsProteinTransformsTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "NP_001268422.1:p.(Ter1059Ter)", "NP_001268422.1:p.(Ter1059Ter)")]
        [InlineData("NM_003820.3:c.516C>G(p.(Pro172=))", "NP_003811.2:p.(Pro172=)", "NM_003820.3:c.516C>G(p.(Pro172=))")]
        [InlineData("NM_001281493.1:c.3176_3177insAG(p.(Ter1059=))", "NP_001268422.1:p.(Ter1059Ter)", "NM_001281493.1:c.3176_3177insAG(p.(Ter1059=))")]
        public void TransformBiocommons_ExpectedResults(string nirvana, string biocommons, string expectedBiocommons)
        {
            string actualBiocommons = HgvsProteinTransforms.TransformBiocommons(nirvana, biocommons);
            Assert.Equal(expectedBiocommons, actualBiocommons);
        }
    }
}