using System.Collections;
using System.Linq;
using Elympics.Util;
using NUnit.Framework;

namespace Elympics.Tests
{
    public class RawCoinConverterTests
    {
        private static readonly (decimal amount, string rawAmount, int decimals)[] TestData = new[]
        {
            (0m, "0", 6),
            (0m, "0", 9),
            (0m, "0", 18),
            (1m, "1000000", 6),
            (1m, "1000000000", 9),
            (1m, "1000000000000000000", 18),
            (1.5m, "1500000", 6),
            (1.5m, "1500000000", 9),
            (1.5m, "1500000000000000000", 18),
            (0.331645m, "331645", 6),
            (0.331645859m, "331645859", 9),
            (0.331645859154352681m, "331645859154352681", 18),
            (95000000000m, "95000000000000000", 6),
            (95000000000m, "95000000000000000000", 9),
            (95000000000m, "95000000000000000000000000000", 18),
            (79228162514264337593543950335m, "79228162514264337593543950335000000", 6),
            (79228162514264337593543950335m, "79228162514264337593543950335000000000", 9),
            (79228162514264337593543950335m, "79228162514264337593543950335000000000000000000", 18),
            (0.000001m, "1", 6),
            (0.000000001m, "1", 9),
            (0.000000000000000001m, "1", 18),
            (1697551125.331645m, "1697551125331645", 6),
            (1697551125.331645859m, "1697551125331645859", 9),
            (1697551125.331645859154352681m, "1697551125331645859154352681", 18),
        };

        private static IEnumerable FromRawTestCases => TestData.Select(data => new TestCaseData(data.rawAmount, data.decimals).Returns(data.amount));
        private static IEnumerable ToRawTestCases()
        {
            foreach (var data in TestData)
                yield return new TestCaseData(data.amount, data.decimals).Returns(data.rawAmount);

            //Trim too many decimal places
            yield return new TestCaseData(0.3316458591543526816975511258m, 6).Returns("331645");
            yield return new TestCaseData(0.3316458591543526816975511258m, 9).Returns("331645859");
            yield return new TestCaseData(0.3316458591543526816975511258m, 18).Returns("331645859154352681");
            yield return new TestCaseData(1697551125.3316458591543526811m, 6).Returns("1697551125331645");
            yield return new TestCaseData(1697551125.3316458591543526811m, 9).Returns("1697551125331645859");
            yield return new TestCaseData(1697551125.3316458591543526811m, 18).Returns("1697551125331645859154352681");
        }

        [TestCaseSource(nameof(FromRawTestCases))]
        public decimal FromRaw(string rawAmount, int decimals) => RawCoinConverter.FromRaw(rawAmount, decimals);

        [TestCaseSource(nameof(ToRawTestCases))]
        public string ToRaw(decimal amount, int decimals) => RawCoinConverter.ToRaw(amount, decimals);
    }
}
