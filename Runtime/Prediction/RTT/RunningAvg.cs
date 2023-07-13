using System;

namespace Elympics
{
    public class RunningAvg
    {
        private readonly double[] _samples;
        private int _samplesNumber;
        private int _samplesIndex;

        public RunningAvg(int size)
        {
            _samples = new double[size];
        }

        public double AddAndGetAvg(double newVal)
        {
            _samples[_samplesIndex] = newVal;

            _samplesNumber = Math.Min(_samplesNumber + 1, _samples.Length);
            _samplesIndex = (_samplesIndex + 1) % _samples.Length;

            var newRttAvg = 0.0;
            for (var i = 0; i < _samplesNumber; i++)
                newRttAvg += _samples[i];

            newRttAvg /= _samplesNumber;

            return newRttAvg;
        }
    }
}
