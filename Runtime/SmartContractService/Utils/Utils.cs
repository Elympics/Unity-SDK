using System.Text.RegularExpressions;
using SCS.InternalModels.Player;
using UnityEngine;

namespace SCS
{
    public static class Utils
    {
        private static readonly Regex EthTransactionRegex = new("^(0x)?[0-9a-f]{64}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void ThrowIfAllowanceNotSigned(string allowanceResult)
        {
            if (EthTransactionRegex.IsMatch(allowanceResult))
                return;

            var deserialized = JsonUtility.FromJson<SetAllowanceErrorResult>(allowanceResult);
            throw new SmartContractServiceException($"Allowance has not been set. Code: {deserialized.code}\n"
                + $"Message: {deserialized.message}");
        }
    }
}
