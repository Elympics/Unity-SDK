#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Elympics.Core
{
    public class ElympicsScore : IEnumerable<float[]>
    {
        internal event Action<(int PlayerIndex, float[] Score)>? PlayerScoreUpdated;

        private readonly bool _enabled;
        private readonly float[][] _score;

        internal ElympicsScore(int playerCount, int scoreWidth, bool enabled)
        {
            _enabled = enabled;
            _score = new float[playerCount][];
            for (var i = 0; i < playerCount; i++)
                _score[i] = new float[scoreWidth];
        }

        public float this[int playerIndex, int scoreIndex]
        {
            get => _score[playerIndex][scoreIndex];
            set
            {
                if (_score[playerIndex][scoreIndex] == value)
                    return;
                _score[playerIndex][scoreIndex] = value;
                if (_enabled)
                    PlayerScoreUpdated?.Invoke((playerIndex, _score[playerIndex]));
            }
        }

        public IEnumerator<float[]> GetEnumerator() => ((IEnumerable<float[]>)_score).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
