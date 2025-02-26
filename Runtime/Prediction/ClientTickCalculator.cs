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

        public void CalculateNextTick(long lastReceivedTick, long previousTick, long lastDelayInputTick, DateTime receivedTickStartUtc, DateTime clientTickStartUtc)
        {
            Results.Reset();
            var lastReceivedTickStart = receivedTickStartUtc.ToLocalTime();
            var clientTickStart = clientTickStartUtc.ToLocalTime();

            var calculatedNextTickExact = CalculateTotalDelayInTicks(lastReceivedTick, lastReceivedTickStart, clientTickStart);

            var expectedPredictionTick = previousTick + 1;
            var exactToExpectedTickDiff = calculatedNextTickExact - expectedPredictionTick;

            long newTick;
            bool canPredict;

            if (DoesClientNeedsToForceJumpToTheFuture(exactToExpectedTickDiff, previousTick, lastReceivedTick, out var ticksToCatchup))
            {
                canPredict = TrySetNextTick(lastReceivedTick, previousTick, out newTick, ticksToCatchup);
                if (canPredict)
                {
                    Results.WasTickJumpForced = true;
                    Results.TicksToCatchup = ticksToCatchup;
                    ElympicsLogger.LogWarning($"Client was unable to maintain required simulation speed. Forcing tick jump, jumping {ticksToCatchup} ticks. Last received tick: {lastReceivedTick} Last predicted tick: {previousTick} New prediction tick: {newTick}.");
                }
            }
            else
            {
                canPredict = TrySetNextTick(lastReceivedTick, previousTick, out newTick);
            }

            Results.LastReceivedTick = lastReceivedTick;
            Results.ExactTickCalculated = calculatedNextTickExact;
            Results.CanPredict = canPredict;
            Results.PreviousTick = previousTick;
            Results.LastInputTick = lastDelayInputTick;
            Results.NewTickFromCalculations = newTick;
            if (canPredict)
            {
                Results.CurrentTick = newTick;
                Results.DelayedInputTick = newTick;
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
            var diffInTicks = Results.ExactTickCalculated - Results.CurrentTick;
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

        private bool DoesClientNeedsToForceJumpToTheFuture(double calculatedAndExpectedTickDiff, long previousTick, long lastReceivedTick, out long ticksToCatchup)
        {
            var tickDiff = (long)Math.Floor(calculatedAndExpectedTickDiff);
            ticksToCatchup = 0;

            if (tickDiff > _config.ForceJumpThresholdInTicks)
            {
                ticksToCatchup = tickDiff;

                if (previousTick + ticksToCatchup < lastReceivedTick)
                {
                    ticksToCatchup = lastReceivedTick - previousTick;
                }

                return true;
            }

            return false;
        }

        private bool TrySetNextTick(long lastReceivedTick, long previousTick, out long newTick, long offset = 0)
        {
            newTick = previousTick + 1 + offset;
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
