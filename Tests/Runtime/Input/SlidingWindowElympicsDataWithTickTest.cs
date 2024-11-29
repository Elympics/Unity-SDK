using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    public class SlidingWindowElympicsDataWithTickTest : MonoBehaviour
    {
        private RingBufferElympicsDataWithTick<ElympicsInput> _buffer;
        private ElympicsInput[] _inputBuffer;
        [Test]
        public void TestInputBuffer()
        {
            var bufferSize = 10;
            _buffer = new RingBufferElympicsDataWithTick<ElympicsInput>(bufferSize);
            _inputBuffer = new ElympicsInput[bufferSize];

            for (var i = 0; i < bufferSize; i++)
            {
                _ = _buffer.TryAddData(new ElympicsInput
                {
                    Tick = i,
                    Player = default,
                    Data = null
                });

            }

            var tickExpected = 0;
            _buffer.GetInputListNonAlloc(_inputBuffer, out var size);
            var counter = 0;
            foreach (var input in _inputBuffer)
            {
                if (input == null)
                    break;

                Assert.AreEqual(tickExpected, input.Tick);
                counter++;
                tickExpected++;
            }
            Assert.AreEqual(size, counter);
        }

        [Test]
        public void TestInputBuffer_AddMoreThanSize()
        {
            var bufferSize = 10;
            _buffer = new RingBufferElympicsDataWithTick<ElympicsInput>(bufferSize);
            _inputBuffer = new ElympicsInput[bufferSize];

            for (var i = 0; i < bufferSize * 2; i++)
            {
                _ = _buffer.TryAddData(new ElympicsInput
                {
                    Tick = i,
                    Player = default,
                    Data = null
                });

            }

            var tickExpected = 10;
            _buffer.GetInputListNonAlloc(_inputBuffer, out var size);
            var counter = 0;
            foreach (var input in _inputBuffer)
            {
                if (input == null)
                    break;

                Assert.AreEqual(tickExpected, input.Tick);
                counter++;
                tickExpected++;
            }
            Assert.AreEqual(10, _buffer.MinTick());
            Assert.AreEqual(size, counter);
        }

        [Test]
        [TestCase(10, 0)]
        [TestCase(10, 5)]
        [TestCase(10, 9)]
        [TestCase(100, 99)]
        public void TestInputBuffer_SetupMinTick_LessThanMaxTick(int initialMaxTick, long newMinTick)
        {
            Assert.Less(newMinTick, initialMaxTick);
            _buffer = new RingBufferElympicsDataWithTick<ElympicsInput>(initialMaxTick);
            _inputBuffer = new ElympicsInput[initialMaxTick];

            for (var i = 0; i < initialMaxTick; i++)
            {
                _ = _buffer.TryAddData(new ElympicsInput
                {
                    Tick = i,
                    Player = default,
                    Data = null
                });

            }


            _buffer.UpdateMinTick(newMinTick);

            var tickExpected = newMinTick;
            _buffer.GetInputListNonAlloc(_inputBuffer, out var size);
            var counter = 0;
            foreach (var input in _inputBuffer)
            {
                if (input == null)
                    break;

                Assert.AreEqual(tickExpected, input.Tick);
                counter++;
                tickExpected++;
            }
            Assert.AreEqual(newMinTick + initialMaxTick - 1, _buffer.MaxTick());
            Assert.AreEqual(initialMaxTick - newMinTick, _buffer.Count());
            Assert.AreEqual(size, counter);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(10, 100)]
        public void TestInputBuffer_SetupMinTick_GreaterOrEqualThanMaxTick(int initialMaxTick, long newMinTick)
        {
            Assert.GreaterOrEqual(newMinTick, initialMaxTick);

            _buffer = new RingBufferElympicsDataWithTick<ElympicsInput>(initialMaxTick);
            _inputBuffer = new ElympicsInput[initialMaxTick];

            for (var i = 0; i < initialMaxTick; i++)
            {
                _ = _buffer.TryAddData(new ElympicsInput
                {
                    Tick = i,
                    Player = default,
                    Data = null
                });

            }

            _buffer.UpdateMinTick(newMinTick);

            _buffer.GetInputListNonAlloc(_inputBuffer, out var size);

            foreach (var input in _inputBuffer)
                Assert.IsNull(input);

            Assert.AreEqual(0, _buffer.Count());
            Assert.AreEqual(newMinTick, _buffer.MinTick());
            Assert.AreEqual(0, size);
        }

        [TearDown]
        public void Cleanup()
        {
            _buffer.Dispose();
            _buffer = null;
        }
    }
}
