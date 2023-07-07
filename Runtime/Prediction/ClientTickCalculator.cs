using System;
using UnityEngine;

namespace Elympics
{
	public class ClientTickCalculator
	{
		private const    float                              TimeThresholdToHoldPredictionDownSeconds = 0.3f;
		private const    float                              TimeThresholdToForceJumpSeconds          = 0.2f;
		private const    double                             MaxTickAheadWithNoChange                 = 1.0d;
		private const    float                              LerpRatio                                = 0.35f;
		private readonly ElympicsGameConfig                 _config;
		private readonly IRoundTripTimeCalculator           _roundTripTimeCalculator;
		private readonly ClientTickCalculatorNetworkDetails _tickCalculationResults;
		public           ClientTickCalculatorNetworkDetails Results => _tickCalculationResults;

		public ClientTickCalculator(IRoundTripTimeCalculator roundTripTimeCalculator, ElympicsGameConfig config)
		{
			_tickCalculationResults = new ClientTickCalculatorNetworkDetails(config);
			_roundTripTimeCalculator = roundTripTimeCalculator;
			_config = config;
		}

		public void CalculateNextTick(long lastReceivedTick, long lastPredictedTick, long lastDelayInputTick, DateTime receivedTickStartUtc, DateTime clientTickStartUtc)
		{
			_tickCalculationResults.Reset();
			var canPredict = true;
			var lastReceivedTickStart = receivedTickStartUtc.ToLocalTime();
			var clientTickStart = clientTickStartUtc.ToLocalTime();

			var calculatedNextTickExact = CalculateTotalDelayInTicks(lastReceivedTick, lastReceivedTickStart, clientTickStart);

			var expectedPredictionTick = lastPredictedTick + 1;
			var exactToExpectedTickDiff = calculatedNextTickExact - expectedPredictionTick;

			long newPredictionTick;
			var tickDiffInSec = exactToExpectedTickDiff * _config.TickDuration;
			if (DoesClientNeedToHoldPrediction(tickDiffInSec))
			{
				newPredictionTick = lastPredictedTick;
				canPredict = false;
			}
			else if (DoesClientNeedsToForceJumpToTheFuture(exactToExpectedTickDiff, tickDiffInSec, lastPredictedTick, lastReceivedTick, out var tickToCatchup))
			{
				canPredict = TrySetNextTick(lastReceivedTick, lastPredictedTick, out newPredictionTick, tickToCatchup);
				if (canPredict)
				{
					_tickCalculationResults.WasTickJumpForced = true;
					_tickCalculationResults.TicksToCatchup = tickToCatchup;
				}
			}
			else
			{
				canPredict = TrySetNextTick(lastReceivedTick, lastPredictedTick, out newPredictionTick);
			}

			_tickCalculationResults.LastReceivedTick = lastReceivedTick;
			_tickCalculationResults.ExactTickCalculated = calculatedNextTickExact;
			_tickCalculationResults.CanPredict = canPredict;
			_tickCalculationResults.LastPredictionTick = lastPredictedTick;
			_tickCalculationResults.LastInputTick = lastDelayInputTick;
			_tickCalculationResults.NewPredictedTickFromCalculations = newPredictionTick;
			if (canPredict)
			{
				_tickCalculationResults.PredictionTick = newPredictionTick;
				_tickCalculationResults.DelayedInputTick = newPredictionTick;
			}

			_tickCalculationResults.PredictionLimit = _config.TotalPredictionLimitInTicks;
			_tickCalculationResults.DefaultTickRate = _config.TicksPerSecond;
			_tickCalculationResults.InputLagTicks = _config.InputLagTicks;
			_tickCalculationResults.RttTicks = _roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds * _config.TicksPerSecond;
			_tickCalculationResults.LcoTicks = _roundTripTimeCalculator.AverageLocalClockOffset.TotalSeconds * _config.TicksPerSecond;

			UpdateElympicsUpdateInterval();
		}

		private void UpdateElympicsUpdateInterval()
		{
			var diffInTicks = _tickCalculationResults.ExactTickCalculated - _tickCalculationResults.PredictionTick;
			var isClientTooFast = diffInTicks < 0;
			// We don't want to slow down client ASAP due to the fact slowing down, can lead to lack of inputs on server as client will not be able to deliver inputs on time.
			var needsUpdateIntervalAdjustment = diffInTicks <= -MaxTickAheadWithNoChange;

			if (!_tickCalculationResults.CanPredict || (isClientTooFast && !needsUpdateIntervalAdjustment))
			{
				_tickCalculationResults.ElympicsUpdateTickRate = _config.TicksPerSecond;
				return;
			}

			var previousTickRate = _tickCalculationResults.ElympicsUpdateTickRate;
			var newTickRate = _config.TicksPerSecond + diffInTicks;

			newTickRate = (1 - LerpRatio) * previousTickRate + LerpRatio * newTickRate;
			newTickRate = newTickRate.Clamp(_config.MinTickRate, _config.MaxTickRate);

			_tickCalculationResults.ElympicsUpdateTickRate = newTickRate;
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

		private bool DoesClientNeedToHoldPrediction(double calculatedAndExpectedTickDiffInSec)
		{
			return calculatedAndExpectedTickDiffInSec < -TimeThresholdToHoldPredictionDownSeconds;
		}
	}
}