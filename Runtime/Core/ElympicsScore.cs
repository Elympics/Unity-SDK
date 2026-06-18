#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Elympics.Core
{
    public class ElympicsScore
    {
        internal event Action<(int PlayerIndex, float Score)>? PlayerScoreUpdated;

        private readonly bool _enabled;
        private readonly float[] _score;

        internal ElympicsScore(int playerCount, bool enabled)
        {
            _enabled = enabled;
            _score = new float[playerCount];
        }

        public float this[int playerIndex]
        {
            get => _score[playerIndex];
            set
            {
                if (_score[playerIndex] == value)
                    return;
                _score[playerIndex] = value;
                if (_enabled)
                    PlayerScoreUpdated?.Invoke((playerIndex, _score[playerIndex]));
            }
        }
    }
}
