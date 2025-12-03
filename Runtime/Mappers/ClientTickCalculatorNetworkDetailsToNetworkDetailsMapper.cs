using Elympics.Public;
namespace Elympics.Mappers
{
    public static class ClientTickCalculatorNetworkDetailsToNetworkDetailsMapper
    {
        public static NetworkCondition MapToNetworkNetworkCondition(this ClientTickCalculatorNetworkDetails source)
        {
            return new NetworkCondition()
            {
                ElympicsUpdateTickRate = source.ElympicsUpdateTickRate,
                ExactTickCalculated = source.ExactTickCalculated,
                InputLagTicks = source.InputLagTicks,
                PreviousTick = source.PreviousTick,
                LastReceivedTick = source.LastReceivedTick,
                LcoTicks = source.LcoTicks,
                ReconciliationPerformed = source.ReconciliationPerformed,
                RttTicks = source.RttTicks,
                WasTickJumpForced = source.WasTickJumpForced,
                PredictionLimit = source.PredictionLimit,
                DefaultTickRate = source.DefaultTickRate,
                TicksToCatchup = source.TicksToCatchup,
                NewTickFromCalculations = source.NewTickFromCalculations
            };
        }
    }
}
