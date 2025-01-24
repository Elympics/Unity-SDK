using System;

namespace Elympics
{
    public class ClientTickCalculator
    {
        private const float TimeThresholdToForceJumpSeconds = 0.2f;
        private const double MaxTickAheadWithNoChange = 1.0d;
        private const float LerpRatio = 0.35f;
        private readonly ElympicsGameConfig _config;
        private readonly IRoundTripTimeCalculator _roundTripTimeCalculator;

        public ClientTickCalculatorNetworkDetails Results { get; }

        public ClientTickCalculator(IRoundTripTimeCalculator roundTripTimeCalculator, ElympicsGameConfig config)
        {
            Results = new ClientTickCalculatorNetworkDetails(config);
            _roundTripTimeCalculator = roundTripTimeCalculator;
            _config = config;
        }

        public void CalculateNextTick(long lastReceivedTick, long lastPredictedTick, long lastDelayInputTick, DateTime receivedTickStartUtc, DateTime clientTickStartUtc)
        {
            Results.Reset();
            var lastReceivedTickStart = receivedTickStartUtc.ToLocalTime();
            var clientTickStart = clientTickStartUtc.ToLocalTime();

            var calculatedNextTickExact = CalculateTotalDelayInTicks(lastReceivedTick, lastReceivedTickStart, clientTickStart);

            var expectedPredictionTick = lastPredictedTick + 1;
            var exactToExpectedTickDiff = calculatedNextTickExact - expectedPredictionTick;

            long newPredictionTick;
            var tickDiffInSec = exactToExpectedTickDiff * _config.TickDuration;
            bool canPredict;

            if (DoesClientNeedsToForceJumpToTheFuture(exactToExpectedTickDiff, tickDiffInSec, lastPredictedTick, lastReceivedTick, out var ticksToCatchup))
            {
                canPredict = TrySetNextTick(lastReceivedTick, lastPredictedTick, out newPredictionTick, ticksToCatchup);
                if (canPredict)
                {
                    Results.WasTickJumpForced = true;
                    Results.TicksToCatchup = ticksToCatchup;
                    ElympicsLogger.LogWarning($"Client was unable to maintain required simulation speed. Forcing tick jump, jumping {ticksToCatchup} ticks. Last received tick: {lastReceivedTick} Last predicted tick: {lastPredictedTick} New prediction tick: {newPredictionTick}.");
                }
            }
            else
            {
                canPredict = TrySetNextTick(lastReceivedTick, lastPredictedTick, out newPredictionTick);
            }

            Results.LastReceivedTick = lastReceivedTick;
            Results.ExactTickCalculated = calculatedNextTickExact;
            Results.CanPredict = canPredict;
            Results.LastPredictionTick = lastPredictedTick;
            Results.LastInputTick = lastDelayInputTick;
            Results.NewPredictedTickFromCalculations = newPredictionTick;
            if (canPredict)
            {
                Results.PredictionTick = newPredictionTick;
                Results.DelayedInputTick = newPredictionTick;
            }

            Results.PredictionLimit = _config.TotalPredictionLimitInTicks;
            Results.DefaultTickRate = _config.TicksPerSecond;
            Results.InputLagTicks = _config.InputLagTicks;
            Results.RttTicks = _roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds * _config.TicksPerSecond;
            Results.LcoTicks = _roundTripTimeCalculator.AverageLocalClockOffset.TotalSeconds * _config.TicksPerSecond;

            UpdateElympicsUpdateInterval();
        }

        private void UpdateElympicsUpdateInterval()
        {
            var diffInTicks = Results.ExactTickCalculated - Results.PredictionTick;
            var isClientTooFast = diffInTicks < 0;
            // We don't want to slow down client ASAP due to the fact slowing down, can lead to lack of inputs on server as client will not be able to deliver inputs on time.
            var needsUpdateIntervalAdjustment = diffInTicks <= -MaxTickAheadWithNoChange;

            if (!Results.CanPredict || (isClientTooFast && !needsUpdateIntervalAdjustment))
            {
                Results.ElympicsUpdateTickRate = _config.TicksPerSecond;
                return;
            }

            var previousTickRate = Results.ElympicsUpdateTickRate;
            var newTickRate = _config.TicksPerSecond + diffInTicks;

            newTickRate = (1 - LerpRatio) * previousTickRate + LerpRatio * newTickRate;
            newTickRate = newTickRate.Clamp(_config.MinTickRate, _config.MaxTickRate);

            Results.ElympicsUpdateTickRate = newTickRate;
        }

        private bool DoesClientNeedsToForceJumpToTheFuture(double calculatedAndExpectedTickDiff, double calculatedAndExpectedTickDiffInSec, long lastPredictionTick, long lastReceivedTick, out long ticksToCatchup)
        {
            ticksToCatchup = 0;
            if (calculatedAndExpectedTickDiffInSec > TimeThresholdToForceJumpSeconds)
            {
                ticksToCatchup = (long)Math.Floor(calculatedAndExpectedTickDiff);

                if (lastPredictionTick + ticksToCatchup < lastReceivedTick)
                {
                    ticksToCatchup = lastReceivedTick - lastPredictionTick;
                }

                return true;
            }

            return false;
        }

        private bool TrySetNextTick(long lastReceivedTick, long lastPredictedTick, out long newTick, long offset = 0)
        {
            newTick = lastPredictedTick + 1 + offset;
            return newTick <= lastReceivedTick + _config.TotalPredictionLimitInTicks;
        }

        private double CalculateTotalDelayInTicks(long receivedTick, DateTime receivedTickStartLocalTime, DateTime clientTickStartLocalTime)
        {
            var receivedTickStartLocal = receivedTickStartLocalTime.Subtract(_roundTripTimeCalculator.AverageLocalClockOffset);
            var clientTickStartDelayVsReceived = clientTickStartLocalTime - receivedTickStartLocal;
            var tickTotal = (double)_config.InputLagTicks;
            tickTotal += (clientTickStartDelayVsReceived + _roundTripTimeCalculator.AverageRoundTripTime).TotalSeconds * _config.TicksPerSecond;
            tickTotal += receivedTick;
            return tickTotal;
        }
    }
}
