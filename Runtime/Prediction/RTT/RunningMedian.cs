using System;

namespace Elympics
{
	public class RunningMedian
	{
		private readonly double[] _samples;
		private readonly double[] _sortedSamples;
		private          int _samplesNumber;
		private          int _samplesIndex;

		public RunningMedian(int size)
		{
			_samples = new double[size];
			_sortedSamples = new double[size];
		}

		public double AddAndGetMedian(double newVal)
		{
			_samples[_samplesIndex] = newVal;

			_samplesNumber = Math.Min(_samplesNumber + 1, _samples.Length);
			_samplesIndex = (_samplesIndex + 1) % _samples.Length;

			for (var i = 0; i < _samplesNumber; i++)
				_sortedSamples[i] = _samples[i];
			Array.Sort(_sortedSamples, 0, _samplesNumber);

			return 0.5 * (_sortedSamples[(_samplesNumber - 1) / 2] + _sortedSamples[_samplesNumber / 2]);
		}
	}
}
