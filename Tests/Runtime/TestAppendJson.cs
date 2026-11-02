using System.Text;
using Elympics.Core.Utils;
using NUnit.Framework;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("Utils")]
    public class TestAppendJson
    {
        [TestCase(double.NaN, ExpectedResult = "\"key\":\"NaN\"")]
        [TestCase(double.PositiveInfinity, ExpectedResult = "\"key\":\"Infinity\"")]
        [TestCase(double.NegativeInfinity, ExpectedResult = "\"key\":\"-Infinity\"")]
        [TestCase(0d, ExpectedResult = "\"key\":0")]
        [TestCase(1d, ExpectedResult = "\"key\":1")]
        [TestCase(-100d, ExpectedResult = "\"key\":-100")]
        [TestCase(10000d, ExpectedResult = "\"key\":10000")]
        [TestCase(1.5d, ExpectedResult = "\"key\":1.5")]
        [TestCase(12.345d, ExpectedResult = "\"key\":12.345")]
        [TestCase(1000.345d, ExpectedResult = "\"key\":1000.345")]
        [TestCase(1e20, ExpectedResult = "\"key\":1E+20")]
        [TestCase(1e-20, ExpectedResult = "\"key\":1E-20")]
        public string AppendJsonPropertyWorksCorrectlyForDouble(double numberToSerialize) =>
            new StringBuilder().AppendJsonProperty("key", numberToSerialize, true).ToString();
    }
}
