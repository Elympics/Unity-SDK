using System;
using System.Text;
using UnityEngine;

namespace Elympics
{
    internal class ClientTickCalculatorNetworkDetails
    {
        public bool CanPredict;
        public long DelayedInputTick;
        public double ElympicsUpdateTickRate;
        public double ExactTickCalculated;
        public int InputLagTicks;
        public long LastInputTick;
        public long PreviousTick;
        public long LastReceivedTick;
        public double LcoTicks;
        public long CurrentTick;
        public bool ReconciliationPerformed;
        public double RttTicks;
        public bool WasTickJumpForced;
        public long PredictionLimit;
        public double DefaultTickRate;
        public double TicksToCatchup;
        public long NewTickFromCalculations;

        public ClientTickCalculatorNetworkDetails(ElympicsGameConfig config) =>
            ElympicsUpdateTickRate = config.TickDuration;

        public override string ToString()
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine("###Tick Calculation Summary###");
            if (!CanPredict)
                _ = sb.AppendLine(NewTickFromCalculations > LastReceivedTick + PredictionLimit ?
                    $"Prediction limit achieved. Max Prediction Tick {LastReceivedTick + PredictionLimit} for Prediction limit {PredictionLimit}" :
                    "Prediction was blocked.");

            if (WasTickJumpForced)
                _ = sb.AppendLine($"Tick Jump was forced by {TicksToCatchup} Ticks");

            _ = sb.AppendLine($"Latest Tick from server {LastReceivedTick}")
                .AppendLine($"Exact Tick Calculated {ExactTickCalculated}")
                .AppendLine($"Client input Tick {DelayedInputTick}. Last input Tick was {LastInputTick}");


            if (ReconciliationPerformed)
            {
                _ = sb.AppendLine("Reconciliation was performed")
                    .AppendLine(LastReceivedTick > PreviousTick ?
                        $"No localSnapshot for Tick {PreviousTick}." :
                        $"LocalSnapshot was different than serverSnapshot for {LastReceivedTick} Tick.");

                if ((LastReceivedTick + 1) <= (PredictionLimit + 1))
                    _ = sb.AppendLine($"Resimulation was performed for [{LastReceivedTick + 1}, {CurrentTick - 1}]");
            }

            return sb.AppendLine($"Client Current Tick {CurrentTick}. Last Tick was {PreviousTick}. Calculated Tick {NewTickFromCalculations}")
                .AppendLine($"ExactCalculated - ExpectedTick = {ExactTickCalculated - (PreviousTick + 1)}")
                .AppendLine($"ExactCalculated - Current = {ExactTickCalculated - NewTickFromCalculations}")
                .AppendLine($"New Tick Rate {ElympicsUpdateTickRate:F2} which is {ElympicsUpdateTickRate / DefaultTickRate * 100:F2}% of DefaultTickRate")
                .AppendLine("###Network conditions###")
                .AppendLine($"Input lag - {InputLagTicks:F} ticks")
                .AppendLine($"Round trip time - {RttTicks:F} ticks")
                .AppendLine($"Local clock offset - {LcoTicks:F} ticks")
                .ToString();
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(new NetworkConditions
            {
                canPredict = CanPredict,
                delayedInputTick = DelayedInputTick,
                elympicsUpdateTickRate = ElympicsUpdateTickRate,
                exactTickCalculated = ExactTickCalculated,
                inputLagTicks = InputLagTicks,
                lastInputTick = LastInputTick,
                previousTick = PreviousTick,
                lastReceivedTick = LastReceivedTick,
                lcoTicks = LcoTicks,
                currentTick = CurrentTick,
                reconciliationPerformed = ReconciliationPerformed,
                rttTicks = RttTicks,
                wasTickJumpForced = WasTickJumpForced,
                predictionLimit = PredictionLimit,
                defaultTickRate = DefaultTickRate,
                ticksToCatchup = TicksToCatchup,
                newTickFromCalculations = NewTickFromCalculations,
            });
        }

        [Serializable]
        private struct NetworkConditions
        {
            public bool canPredict;
            public long delayedInputTick;
            public double elympicsUpdateTickRate;
            public double exactTickCalculated;
            public int inputLagTicks;
            public long lastInputTick;
            public long previousTick;
            public long lastReceivedTick;
            public double lcoTicks;
            public long currentTick;
            public bool reconciliationPerformed;
            public double rttTicks;
            public bool wasTickJumpForced;
            public long predictionLimit;
            public double defaultTickRate;
            public double ticksToCatchup;
            public long newTickFromCalculations;
        }

        public void Reset()
        {
            ReconciliationPerformed = false;
            WasTickJumpForced = false;
            CanPredict = false;
            TicksToCatchup = 0;
        }
    }
}
