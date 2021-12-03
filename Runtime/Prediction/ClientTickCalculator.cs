using System;
using UnityEngine;

namespace Elympics
{
	public class ClientTickCalculator
	{
		private const long MaxTicksDiffSumToWaitBeforeCatchUp = 30;

		private readonly IRoundTripTimeCalculator _roundTripTimeCalculator;
		private readonly ElympicsGameConfig       _config;

		public ClientTickCalculator(IRoundTripTimeCalculator roundTripTimeCalculator)
		{
			_roundTripTimeCalculator = roundTripTimeCalculator;
			_config = ElympicsConfig.LoadCurrentElympicsGameConfig();
		}

		private long _lastLatestSnapshotTick;
		private long _ticksDiffSumSinceWaitingBeforeCatchUp;

		public double DelayedInputTickExact { get; private set; }
		public long   DelayedInputTick      { get; private set; }
		public long   PredictionTick        { get; private set; }

		public void CalculateNextTick(long latestSnapshotTick)
		{
			var lastLatestSnapshotTick = _lastLatestSnapshotTick;
			_lastLatestSnapshotTick = latestSnapshotTick;

			// Did not received new snapshot, lag happened - continue ticking by 1
			if (latestSnapshotTick == lastLatestSnapshotTick)
			{
				UseNextTick();
				return;
			}

			var ticksTotalDelay = CalculateTotalDelayInTicks();

			var delayedInputTickExact = latestSnapshotTick + ticksTotalDelay;
			// Latency stable or increase - jump to new predicted tick
			if (TryUpdateTickIfBiggerThanCurrent(delayedInputTickExact))
				return;

			// Latency stable but oscillations around full tick - continue ticking with slightly increased predicted tick
			var slightlyIncreasedPredictedTick = delayedInputTickExact + 0.5;
			if (TryUpdateTickIfBiggerThanCurrent(slightlyIncreasedPredictedTick))
				return;

			// Latency decrease, predicted tick not greater than current - try to catch up with new predicted tick if continuous decrease
			if (TryUpdateTickIfLowerThanCurrentForSomeTime(delayedInputTickExact))
				return;

			// Not continuous decrease - continue ticking
			UseNextTick();
		}

		private double CalculateTotalDelayInTicks()
		{
			var tickTime = _config.TickDuration;
			var averageTicksRtt = _roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds / tickTime;
			var ticksTotalDelay = averageTicksRtt + _config.InputLagTicks;
			return ticksTotalDelay;
		}

		private bool TryUpdateTickIfBiggerThanCurrent(double delayedInputTickExact)
		{
			var delayedInputTick = (long) Math.Ceiling(delayedInputTickExact);
			if (delayedInputTick > DelayedInputTick)
			{
				ForceUpdateNextTick(delayedInputTickExact, delayedInputTick);
				return true;
			}

			return false;
		}

		private bool TryUpdateTickIfLowerThanCurrentForSomeTime(double delayedInputTickExact)
		{
			var delayedInputTick = (long) Math.Ceiling(delayedInputTickExact);
			var nextTick = DelayedInputTick + 1;
			var ticksDiff = nextTick - delayedInputTick;
			// Wait shorter when bigger decrease
			_ticksDiffSumSinceWaitingBeforeCatchUp += ticksDiff;

			if (_ticksDiffSumSinceWaitingBeforeCatchUp >= MaxTicksDiffSumToWaitBeforeCatchUp)
			{
				ForceUpdateNextTick(delayedInputTickExact, delayedInputTick);
				return true;
			}

			return false;
		}

		private void ForceUpdateNextTick(double delayedInputTickExact, long delayedInputTick)
		{
			DelayedInputTickExact = delayedInputTickExact;
			DelayedInputTick = delayedInputTick;
			PredictionTick = GetLimitedPredictionTick(delayedInputTick);
			_ticksDiffSumSinceWaitingBeforeCatchUp = 0;
		}

		private long GetLimitedPredictionTick(long delayedInputTick) => Math.Min(_lastLatestSnapshotTick + _config.TotalPredictionLimitInTicks, delayedInputTick);

		private void UseNextTick()
		{
			DelayedInputTickExact += 1;
			DelayedInputTick += 1;
			PredictionTick += 1;
			PredictionTick = GetLimitedPredictionTick(PredictionTick);
		}
	}
}
