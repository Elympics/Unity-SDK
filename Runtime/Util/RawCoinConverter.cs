#nullable enable

using System;
using System.Numerics;

namespace Elympics.Util
{
    /// <summary>Provides methods for conversion between user-friendly coin amounts and raw values in atomic units native to blockchain.</summary>
    /// <remarks>
    /// Raw values are expressed in atomic native units of coins.
    /// Different coins have different scales for conversion between regular and raw units and use different names to refer to them.
    /// For example in case of ETH that raw unit is called Wei and 0.01 ETH equals 10000000000000000 Wei.
    /// </remarks>
    public static class RawCoinConverter
    {
        /// <param name="decimalPlacesToUnit">Usually fetched from <see cref="Elympics.CurrencyInfo.Decimals"/>.</param>
        public static decimal FromRaw(string rawAmount, int decimalPlacesToUnit)
        {
            if (!BigInteger.TryParse(rawAmount, out var bigIntWei))
                throw new ArgumentException($"Invalid amount string: {rawAmount}",nameof(rawAmount));

            var divisor = BigInteger.Pow(10, decimalPlacesToUnit);
            var wholePart = BigInteger.DivRem(bigIntWei, divisor, out var remainder);

            var fractionalPart = (decimal)remainder / (decimal)divisor;
            return (decimal)wholePart + fractionalPart;
        }

        /// <param name="decimalPlacesToUnit">Usually fetched from <see cref="Elympics.CurrencyInfo.Decimals"/>.</param>
        public static string ToRaw(decimal amount, int decimalPlacesToUnit)
        {
            var mantissa = (BigInteger)amount;
            var exponent = 0;
            for (var num = 1M; (decimal)mantissa != amount * num; mantissa = (BigInteger)(amount * num))
            {
                --exponent;
                num *= 10M;
            }
            var multiplier = BigInteger.Pow(10, decimalPlacesToUnit + exponent);
            var wei = BigInteger.Multiply(mantissa, multiplier);
            return wei.ToString();
        }
    }
}
