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
    /// This conversion always happens by multiplying or dividing by powers of 10.
    /// </remarks>
    public static class RawCoinConverter
    {
        /// <param name="rawAmount">String containing numbers that represent amount of a coin in its atomic native units.</param>
        /// <param name="decimals">Number of decimal places allowed by the coin. Usually fetched from <see cref="CurrencyInfo.Decimals"/>.</param>
        /// <returns>User-friendly coin amount that can have up to <paramref name="decimals"/> decimal places of precision.</returns>
        public static decimal FromRaw(string rawAmount, int decimals)
        {
            if (!BigInteger.TryParse(rawAmount, out var bigIntWei))
                throw new ArgumentException($"Invalid amount string: {rawAmount}", nameof(rawAmount));

            var divisor = BigInteger.Pow(10, decimals);
            var wholePart = BigInteger.DivRem(bigIntWei, divisor, out var remainder);

            var fractionalPart = (decimal)remainder / (decimal)divisor;
            return (decimal)wholePart + fractionalPart;
        }

        /// <param name="amount">User-friendly coin amount that can have up to <paramref name="decimals"/> decimal places of precision.</param>
        /// <param name="decimals">Number of decimal places allowed by the coin. Usually fetched from <see cref="CurrencyInfo.Decimals"/>.</param>
        /// <returns>String containing numbers that represent the <paramref name="amount"/> in coin's atomic native units.</returns>
        /// <remarks>If <paramref name="amount"/> has more decimal places than <paramref name="decimals"/>, the extra decimal places will be discarded before conversion.</remarks>
        public static string ToRaw(decimal amount, int decimals)
        {
            var wholePart = (BigInteger)amount;
            var fraction = amount - (decimal)wholePart;
            var multiplier = BigInteger.Pow(10, decimals);
            var result = BigInteger.Multiply(wholePart, multiplier);
            result += (BigInteger)(fraction * (decimal)multiplier);

            return result.ToString();
        }
    }
}
