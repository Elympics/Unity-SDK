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
		private double    _calculatedNextTickExact;

		private double _clientTickingAberrationSumTicks;
		private double _serverTickingAberrationSumTicks;

		private long _ticksDiffSumSinceWaitingBeforeCatchUp;

		public long LastPredictionTick   { get; private set; }
		public long LastDelayedInputTick { get; private set; }

		private long _predictionTick;
		private long _delayedInputTick;
		private long _calculatedNextTickExactRoundedUp;

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

		public ClientTickCalculatorNetworkDetails CalculateNextTick(long receivedTick, DateTime receivedTickStartUtc, DateTime clientTickStartUtc)
		{
			var receivedTickStart = receivedTickStartUtc.ToLocalTime();
			var clientTickStart = clientTickStartUtc.ToLocalTime();

			CalculateTickingAberrations(receivedTick, clientTickStart, receivedTickStart);

			_calculatedNextTickExact = CalculateTotalDelayInTicks(receivedTick, receivedTickStart, clientTickStart);
			_calculatedNextTickExactRoundedUp = (long)Math.Ceiling(_calculatedNextTickExact);
			if (IsNextTick(_calculatedNextTickExact))
			{
				UseNextTick();
				ResetTicksDiffSum();
				var details = CreateClientTickCalculateNetworkDetails(true, false);
				return details;
			}

			if (IsBiggerThanNextTick(_calculatedNextTickExact) || IsLowerThanNextTick(_calculatedNextTickExact))
			{
				UpdateTicksDiffSum(_calculatedNextTickExactRoundedUp);

				if (IsTicksDiffSumBiggerThanMaxBeforeCatchupForward() || IsTicksDiffSumLowerThanMinBeforeCatchupBackwards())
				{
					ForceUpdateNextTick(_calculatedNextTickExactRoundedUp);
					var details = CreateClientTickCalculateNetworkDetails(false, true);
					ResetTicksDiffSum();
					return details;
				}
			}

			UseNextTick();
			return CreateClientTickCalculateNetworkDetails(false, false);
		}

		private long GetLimitedPredictionTick(long delayedInputTick)
		{
			return _lastReceivedTick.HasValue ? Math.Min(_lastReceivedTick.Value + _config.TotalPredictionLimitInTicks, delayedInputTick) : delayedInputTick;
		}

		private void UseNextTick()
		{
			DelayedInputTick += 1;
			PredictionTick = GetLimitedPredictionTick(DelayedInputTick);
		}

		private void CalculateTickingAberrations(long receivedTick, DateTime clientTickStart, DateTime receivedTickStart)
		{
			if (_lastReceivedTick.HasValue && _lastReceivedTickStart.HasValue && _lastClientTickStart.HasValue)
			{
				var receivedTicksStartDiff = receivedTickStart - _lastReceivedTickStart.Value;
				var receivedTicksStartDiffTicks = receivedTicksStartDiff.TotalSeconds * _config.TicksPerSecond;
				var receivedTicksDiff = receivedTick - _lastReceivedTick.Value;
				var serverTickingAberrationTicks = receivedTicksStartDiffTicks - receivedTicksDiff;
				_serverTickingAberrationSumTicks += serverTickingAberrationTicks;

				var clientTicksStartDiff = clientTickStart - _lastClientTickStart.Value;
				var clientTicksStartDiffTicks = clientTicksStartDiff.TotalSeconds * _config.TicksPerSecond;
				const double clientTicksDiff = 1;
				var clientTickingAberrationTicks = clientTicksStartDiffTicks - clientTicksDiff;
				_clientTickingAberrationSumTicks += clientTickingAberrationTicks;
			}

			_lastReceivedTick = receivedTick;
			_lastReceivedTickStart = receivedTickStart;
			_lastClientTickStart = clientTickStart;
		}

		private double CalculateTotalDelayInTicks(long receivedTick, DateTime receivedTickStart, DateTime clientTickStart)
		{
			var receivedTickStartLocal = receivedTickStart.Subtract(_roundTripTimeCalculator.AverageLocalClockOffset);
			var clientTickStartDelayVsReceived = clientTickStart - receivedTickStartLocal;
			var tickTotal = (double)_config.InputLagTicks;
			tickTotal += (clientTickStartDelayVsReceived + _roundTripTimeCalculator.AverageRoundTripTime).TotalSeconds * _config.TicksPerSecond;
			tickTotal += receivedTick;
			return tickTotal;
		}

		private bool IsNextTick(double calculatedNextTickExact) => (long)Math.Ceiling(calculatedNextTickExact) == DelayedInputTick + 1 || (long)Math.Ceiling(calculatedNextTickExact + 0.5) == DelayedInputTick + 1;

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

		private ClientTickCalculatorNetworkDetails CreateClientTickCalculateNetworkDetails(bool correctTicking, bool forcedTickJump)
		{
			var tickJumpStart = LastPredictionTick + 1;
			var exactTick = _calculatedNextTickExact;
			var tickJumpEnd = PredictionTick;
			var inputJumpStart = LastDelayedInputTick + 1;
			var inputJumpEnd = DelayedInputTick;
			var ticksDiffSumBeforeCatchUp = _ticksDiffSumSinceWaitingBeforeCatchUp;
			var inputLagTicks = _config.InputLagTicks;
			var rttTicks = _roundTripTimeCalculator.AverageRoundTripTime.TotalSeconds * _config.TicksPerSecond;
			var lcoTicks = _roundTripTimeCalculator.AverageLocalClockOffset.TotalSeconds * _config.TicksPerSecond;
			var ctasTicks = _clientTickingAberrationSumTicks;
			var stasTicks = _serverTickingAberrationSumTicks;
			var tickDiff = 1L;
			if (!correctTicking)
			{
				tickDiff = _calculatedNextTickExactRoundedUp - DelayedInputTick;
			}

			return new ClientTickCalculatorNetworkDetails
			{
				CorrectTicking = correctTicking,
				ExactTickCalculated = exactTick,
				Diff = tickDiff,
				ForcedTickJump = forcedTickJump,
				TicksDiffSumBeforeCatchup = ticksDiffSumBeforeCatchUp,
				TickJumpStart = tickJumpStart,
				TickJumpEnd = tickJumpEnd,
				InputTickJumpStart = inputJumpStart,
				InputTickJumpEnd = inputJumpEnd,
				InputLagTicks = inputLagTicks,
				RttTicks = rttTicks,
				LcoTicks = lcoTicks,
				CtasTicks = ctasTicks,
				StasTicks = stasTicks
			};
		}
	}
}