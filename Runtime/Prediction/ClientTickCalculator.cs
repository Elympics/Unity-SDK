using System;
using UnityEngine;

namespace Elympics
{
	public class ClientTickCalculator
	{
		private readonly long _maxTicksToJumpBackwards;

		private readonly long _maxTicksDiffSumToWaitBeforeCatchUpForward;
		private readonly long _maxTicksDiffSumToWaitBeforeCatchUpBackwards;

		private readonly IRoundTripTimeCalculator _roundTripTimeCalculator;
		private readonly ElympicsGameConfig       _config;

		public ClientTickCalculator(IRoundTripTimeCalculator roundTripTimeCalculator)
		{
			_roundTripTimeCalculator = roundTripTimeCalculator;
			_config = ElympicsConfig.LoadCurrentElympicsGameConfig();

			_maxTicksToJumpBackwards = _config.TicksPerSecond;

			_maxTicksDiffSumToWaitBeforeCatchUpForward = _config.TicksPerSecond / 2;
			_maxTicksDiffSumToWaitBeforeCatchUpBackwards = -_config.TicksPerSecond;
		}

		private long?     _lastReceivedTick;
		private DateTime? _lastReceivedTickStart;
		private DateTime? _lastClientTickStart;

		public double ClientTickingAberrationSumTicks { get; private set; }
		public double ServerTickingAberrationSumTicks { get; private set; }

		private long _ticksDiffSumSinceWaitingBeforeCatchUp;

		public long LastPredictionTick   { get; private set; }
		public long LastDelayedInputTick { get; private set; }

		#region ForceUpdateNextTick comparison

		private TimeSpan _lastAverageRoundTripTime;
		private TimeSpan _lastAverageLocalClockOffset;
		private double   _lastClientTickingAberrationSumTicks;
		private double   _lastServerTickingAberrationSumTicks;

		#endregion

		private long _predictionTick;
		private long _delayedInputTick;

		public long PredictionTick
		{
			get => _predictionTick;
			private set
			{
				if (IsBiggerThanLast(_predictionTick, LastPredictionTick) || IsLessEnoughForBackwardsJump(_predictionTick, LastPredictionTick))
					LastPredictionTick = _predictionTick;

				_predictionTick = value;
			}
		}

		private bool IsLessEnoughForBackwardsJump(long tick, long lastTick) => tick < lastTick && lastTick - tick > _maxTicksToJumpBackwards;
		private bool IsBiggerThanLast(long tick, long lastTick)             => tick > lastTick;

		public long DelayedInputTick
		{
			get => _delayedInputTick;
			private set
			{
				if (IsBiggerThanLast(_delayedInputTick, LastDelayedInputTick) || IsLessEnoughForBackwardsJump(_delayedInputTick, LastDelayedInputTick))
					LastDelayedInputTick = _delayedInputTick;

				_delayedInputTick = value;
			}
		}

		public void CalculateNextTick(long receivedTick, DateTime receivedTickStartUtc, DateTime clientTickStart)
		{
			var receivedTickStart = receivedTickStartUtc.ToLocalTime();

			CalculateTickingAberrations(receivedTick, clientTickStart, receivedTickStart);

			var calculatedNextTickExact = CalculateTotalDelayInTicks(receivedTick, receivedTickStart, clientTickStart);

			if (IsNextTick(calculatedNextTickExact))
			{
				UseNextTick();
				ResetTicksDiffSum();
				return;
			}

			if (IsBiggerThanNextTick(calculatedNextTickExact) || IsLowerThanNextTick(calculatedNextTickExact))
			{
				var calculatedNextTick = (long) Math.Ceiling(calculatedNextTickExact);
				UpdateTicksDiffSum(calculatedNextTick);

				if (IsTicksDiffSumBiggerThanMaxBeforeCatchupForward() || IsTicksDiffSumLowerThanMinBeforeCatchupBackwards())
				{
					ForceUpdateNextTick(calculatedNextTick);
					ResetTicksDiffSum();
					LogForceUpdatesAndPrintCalculateNetworkConditionsChange();
					return;
				}
			}

			UseNextTick();
		}

		private long GetLimitedPredictionTick(long delayedInputTick)
		{
			return _lastReceivedTick.HasValue
				? Math.Min(_lastReceivedTick.Value + _config.TotalPredictionLimitInTicks, delayedInputTick)
				: delayedInputTick;
		}

		private void UseNextTick()
		{
			DelayedInputTick += 1;
			PredictionTick = GetLimitedPredictionTick(PredictionTick + 1);
		}

		private void CalculateTickingAberrations(long receivedTick, DateTime clientTickStart, DateTime receivedTickStart)
		{
			if (_lastReceivedTick.HasValue && _lastReceivedTickStart.HasValue && _lastClientTickStart.HasValue)
			{
				var receivedTicksStartDiff = receivedTickStart - _lastReceivedTickStart.Value;
				var receivedTicksStartDiffTicks = receivedTicksStartDiff.TotalSeconds * _config.TicksPerSecond;
				var receivedTicksDiff = receivedTick - _lastReceivedTick.Value;
				var serverTickingAberrationTicks = receivedTicksStartDiffTicks - receivedTicksDiff;
				ServerTickingAberrationSumTicks += serverTickingAberrationTicks;

				var clientTicksStartDiff = clientTickStart - _lastClientTickStart.Value;
				var clientTicksStartDiffTicks = clientTicksStartDiff.TotalSeconds * _config.TicksPerSecond;
				const double clientTicksDiff = 1;
				var clientTickingAberrationTicks = clientTicksStartDiffTicks - clientTicksDiff;
				ClientTickingAberrationSumTicks += clientTickingAberrationTicks;
			}

			_lastReceivedTick = receivedTick;
			_lastReceivedTickStart = receivedTickStart;
			_lastClientTickStart = clientTickStart;
		}

		private double CalculateTotalDelayInTicks(long receivedTick, DateTime receivedTickStart, DateTime clientTickStart)
		{
			var receivedTickStartLocal = receivedTickStart.Subtract(_roundTripTimeCalculator.AverageLocalClockOffset);
			var clientTickStartDelayVsReceived = clientTickStart - receivedTickStartLocal;
			var tickTotal = (double) _config.InputLagTicks;
			tickTotal += (clientTickStartDelayVsReceived + _roundTripTimeCalculator.AverageRoundTripTime).TotalSeconds * _config.TicksPerSecond;
			tickTotal += receivedTick;
			return tickTotal;
		}

		private bool IsNextTick(double calculatedNextTickExact) => (long) Math.Ceiling(calculatedNextTickExact) == DelayedInputTick + 1 ||
		                                                           (long) Math.Ceiling(calculatedNextTickExact + 0.5) == DelayedInputTick + 1;

		private bool IsBiggerThanNextTick(double calculatedNextTickExact) => calculatedNextTickExact + 0.5 > DelayedInputTick + 1;
		private bool IsLowerThanNextTick(double calculatedNextTickExact)  => calculatedNextTickExact < DelayedInputTick + 1;

		private void UpdateTicksDiffSum(long calculatedNextTick)
		{
			var nextTick = DelayedInputTick + 1;
			var ticksDiff = calculatedNextTick - nextTick;
			_ticksDiffSumSinceWaitingBeforeCatchUp += ticksDiff;
		}

		private bool IsTicksDiffSumBiggerThanMaxBeforeCatchupForward()  => _ticksDiffSumSinceWaitingBeforeCatchUp >= _maxTicksDiffSumToWaitBeforeCatchUpForward;
		private bool IsTicksDiffSumLowerThanMinBeforeCatchupBackwards() => _ticksDiffSumSinceWaitingBeforeCatchUp <= _maxTicksDiffSumToWaitBeforeCatchUpBackwards;

		private void ForceUpdateNextTick(long delayedInputTick)
		{
			DelayedInputTick = delayedInputTick;
			PredictionTick = GetLimitedPredictionTick(delayedInputTick);
		}

		private void ResetTicksDiffSum() => _ticksDiffSumSinceWaitingBeforeCatchUp = 0;

		private void LogForceUpdatesAndPrintCalculateNetworkConditionsChange()
		{
			var averageRoundTripTime = _roundTripTimeCalculator.AverageRoundTripTime;
			var averageLocalClockOffset = _roundTripTimeCalculator.AverageLocalClockOffset;

			Debug.LogError($"[Elympics] Forcing update next prediction tick with difference {PredictionTick - (LastPredictionTick + 1)} from {LastPredictionTick + 1} to {PredictionTick}\n" +
			               $"Network conditions:\n" +
			               $"Input lag - {_config.InputLagTicks:F} ticks\n" +
			               $"RTT - {_roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds * _config.TicksPerSecond:F} ticks; change - {(_lastAverageRoundTripTime - averageRoundTripTime).TotalSeconds * _config.TicksPerSecond} ticks\n" +
			               $"Local time offset - {_roundTripTimeCalculator.AverageLocalClockOffset:G}; change - {(_lastAverageLocalClockOffset - averageLocalClockOffset).TotalSeconds * _config.TicksPerSecond} ticks\n" +
			               $"Client ticking aberration sum - {ClientTickingAberrationSumTicks:F} ticks; change - {ClientTickingAberrationSumTicks - _lastClientTickingAberrationSumTicks} ticks\n" +
			               $"Server ticking aberration sum - {ServerTickingAberrationSumTicks:F} ticks; change - {ServerTickingAberrationSumTicks - _lastServerTickingAberrationSumTicks} ticks\n");

			_lastAverageRoundTripTime = averageRoundTripTime;
			_lastAverageLocalClockOffset = averageLocalClockOffset;
			_lastClientTickingAberrationSumTicks = ClientTickingAberrationSumTicks;
			_lastServerTickingAberrationSumTicks = ServerTickingAberrationSumTicks;
		}

		public void LogNetworkConditions()
		{
			Debug.Log($"[Elympics] Network conditions:\n" +
			          $"Input lag - {_config.InputLagTicks:F} ticks\n" +
			          $"RTT - {_roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds * _config.TicksPerSecond:F} ticks\n" +
			          $"Local time offset - {_roundTripTimeCalculator.AverageLocalClockOffset:G}\n" +
			          $"Client ticking aberration sum - {ClientTickingAberrationSumTicks:F} ticks\n" +
			          $"Server ticking aberration sum - {ServerTickingAberrationSumTicks:F} ticks\n");
		}
	}
}
