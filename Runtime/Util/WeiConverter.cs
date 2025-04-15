using System;
using System.Numerics;

public static class WeiConverter
{
    public static decimal FromWei(string wei, int decimalPlacesToUnit)
    {
        if (!BigInteger.TryParse(wei, out var bigIntWei))
            throw new ArgumentException("Invalid wei amount string");
        var divisor = BigInteger.Pow(10, decimalPlacesToUnit);
        var wholePart = BigInteger.DivRem(bigIntWei, divisor, out var remainder);

        var fractionalPart = (decimal)remainder / (decimal)divisor;
        return (decimal)wholePart + fractionalPart;
    }

    public static string ToWei(decimal amount, int decimalPlacesToUnit)
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
