using System.Text;

namespace Elympics
{
    public class ClientTickCalculatorNetworkDetails
    {
        public bool CanPredict;
        public long DelayedInputTick;
        public double ElympicsUpdateTickRate;
        public double ExactTickCalculated;
        public int InputLagTicks;
        public long LastInputTick;
        public long LastPredictionTick;
        public long LastReceivedTick;
        public double LcoTicks;
        public long PredictionTick;
        public bool ReconciliationPerformed;
        public double RttTicks;
        public bool WasTickJumpForced;
        public long PredictionLimit;
        public double DefaultTickRate;
        public double TicksToCatchup;
        public long NewPredictedTickFromCalculations;

        public ClientTickCalculatorNetworkDetails(ElympicsGameConfig config)
        {
            ElympicsUpdateTickRate = config.TickDuration;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            _ = sb.AppendLine("###Tick Calculation Summary###");
            if (!CanPredict)
            {
                _ = sb.AppendLine(NewPredictedTickFromCalculations > LastReceivedTick + PredictionLimit ?
                    $"Prediction limit achieved. Max Prediction Tick {LastReceivedTick + PredictionLimit} for Prediction limit {PredictionLimit}" :
                    "Prediction was blocked.");
            }

            if (WasTickJumpForced)
                _ = sb.AppendLine($"Tick Jump was forced by {TicksToCatchup} Ticks");

            _ = sb.AppendLine($"Latest Tick from server {LastReceivedTick}")
                .AppendLine($"Exact Tick Calculated {ExactTickCalculated}")
                .AppendLine($"Client input Tick {DelayedInputTick}. Last input Tick was {LastInputTick}");


            if (ReconciliationPerformed)
            {
                _ = sb.AppendLine("Reconciliation was performed")
                    .AppendLine(LastReceivedTick > LastPredictionTick ?
                        $"No localSnapshot for Tick {LastPredictionTick}." :
                        $"LocalSnapshot was different than serverSnapshot for {LastReceivedTick} Tick.");

                if ((LastReceivedTick + 1) <= (PredictionLimit + 1))
                    _ = sb.AppendLine($"Resimulation was performed for [{LastReceivedTick + 1}, {PredictionTick - 1}]");
            }

            return sb.AppendLine($"Client predicted Tick {PredictionTick}. Last Prediction Tick was {LastPredictionTick}. Calculated Tick {NewPredictedTickFromCalculations}")
                .AppendLine($"ExactCalculated - ExpectedTick = {ExactTickCalculated - (LastPredictionTick + 1)}")
                .AppendLine($"ExactCalculated - Predicted = {ExactTickCalculated - NewPredictedTickFromCalculations}")
                .AppendLine($"New Tick Rate {ElympicsUpdateTickRate:F2} which is {ElympicsUpdateTickRate / DefaultTickRate * 100:F2}% of DefaultTickRate")
                .AppendLine("###Network conditions###")
                .AppendLine($"Input lag - {InputLagTicks:F} ticks")
                .AppendLine($"Round trip time - {RttTicks:F} ticks")
                .AppendLine($"Local clock offset - {LcoTicks:F} ticks")
                .ToString();
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
