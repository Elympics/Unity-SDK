#nullable enable

using System;

namespace Elympics
{
    public class RunningAvg
    {
        private readonly RingBuffer<double> _samples;

        public RunningAvg(int size) => _samples = new(size);

        /// <summary>Add new value to the data set.</summary>
        public void Add(double sample) => _samples.PushBack(sample);

        /// <summary>Average of last <see cref="_samples"/>.<see cref="RingBuffer{T}.Count"/> samples added with <see cref="Add(double)"/>.</summary>
        public double Average
        {
            get
            {
                if (_samples.Count <= 0)
                    throw new InvalidOperationException("Can't calculate average on empty data set.");

                var newRttAvg = 0.0;
                for (var i = 0; i < _samples.Count; i++)
                    newRttAvg += _samples[i];

                newRttAvg /= _samples.Count;

                return newRttAvg;
            }
        }

        /// <summary>Standard deviation of last <see cref="_samples"/>.<see cref="RingBuffer{T}.Count"/> samples added with <see cref="Add(double)"/>.</summary>

        public double StdDev => GetStandardDeviation(Average);

        /// <summary>Use this method to get values of both <see cref="Average"/> and <see cref="StdDev"/> and avoid evaluating <see cref="Average"/> twice.</summary>
        public (double Average, double StdDev) GetAvgAndStdDev()
        {
            var avg = Average;
            var stdDev = GetStandardDeviation(avg);

            return (avg, stdDev);
        }


        private double GetStandardDeviation(double average)
        {
            var variance = 0.0;

            for (var i = 0; i < _samples.Count; i++)
                variance += Math.Pow(average - _samples[i], 2);

            variance /= _samples.Count;

            return Math.Sqrt(variance);
        }
    }
}
