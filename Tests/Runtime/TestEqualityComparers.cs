using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    public class TestEqualityComparers
    {
        [Test]
        public void TestNegativeToleranceBehavesLikeZeroTolerance()
        {
            var comparer = new ElympicsFloatEqualityComparer(-10f);
            Assert.IsTrue(comparer.Equals(0f, 0f));
            Assert.IsFalse(comparer.Equals(0f, float.Epsilon));
        }

        #region ElympicsIntEqualityComparer tests

        private static IEnumerable<EqualityComparerTestCase<int>> IntTestCasesWithToleranceTooLow => new List<EqualityComparerTestCase<int>>
        {
            new (0, 1, 0f),
            new (int.MinValue, 0, 10e5f),
            new (0, int.MinValue, 10e5f),
            new (int.MinValue, int.MaxValue, 10e5f)
        };

        private static IEnumerable<EqualityComparerTestCase<int>> IntTestCasesWithToleranceSufficient => new List<EqualityComparerTestCase<int>>
        {
            new (0, 0, 0f),
            new (0, 0, 100f),  // (much) greater than needed
			new (0, 1, 1f),
            new (int.MinValue, 0, -(float)int.MinValue),
            new (0, int.MinValue, -(float)int.MinValue),
            new (int.MinValue, int.MaxValue, -(float)int.MinValue * 2)
        };

        [Test]
        public void TestElympicsIntEqualityComparerWithToleranceTooLow([ValueSource(nameof(IntTestCasesWithToleranceTooLow))] EqualityComparerTestCase<int> testCase)
        {
            var comparer = new ElympicsIntEqualityComparer(testCase.Tolerance);
            Assert.IsFalse(comparer.Equals(testCase.Left, testCase.Right));
        }

        [Test]
        public void TestElympicsIntEqualityComparerWithToleranceSufficient([ValueSource(nameof(IntTestCasesWithToleranceSufficient))] EqualityComparerTestCase<int> testCase)
        {
            var comparer = new ElympicsIntEqualityComparer(testCase.Tolerance);
            Assert.IsTrue(comparer.Equals(testCase.Left, testCase.Right));
        }

        #endregion ElympicsIntEqualityComparer tests

        #region ElympicsVector2EqualityComparer tests

        private static IEnumerable<EqualityComparerTestCase<Vector2>> Vector2TestCasesWithToleranceTooLow => new List<EqualityComparerTestCase<Vector2>>
        {
            // Small differences
            new (Vector2.zero, new Vector2(1f, 0f), 0.99f),
            new (Vector2.zero, new Vector2(0f, 1f), 0.99f),
            new (Vector2.zero, new Vector2(0.1f, 0f), 0.099f),
            new (Vector2.zero, new Vector2(3f, 4f), 4.99f),
            new (Vector2.zero, new Vector2(5f, 0f), 4.99f),

            // Diagonal vectors
            new (Vector2.zero, new Vector2(1f, 1f), 1.41f),

            // Non-zero starting points
            new (new Vector2(1f, 1f), new Vector2(2f, 2f), 1.41f),
            new (new Vector2(10f, 10f), new Vector2(13f, 14f), 4.99f),
            
            // Very small vectors (epsilon range)
            new (Vector2.zero, new Vector2(float.Epsilon, 0f), float.Epsilon * float.Epsilon * 0.9f),
            new (Vector2.zero, new Vector2(0.001f, 0.001f), 0.000001f),

            // Large magnitude vectors
            new (Vector2.zero, new Vector2(1000f, 0f), 999f),
            new (new Vector2(1000f, 1000f), new Vector2(2000f, 2000f), 1414.20f),
            new (Vector2.zero, new Vector2(10000f, 10000f), 14142f),

            // Negative coordinates
            new (Vector2.zero, new Vector2(-1f, 0f), 0.99f),
            new (new Vector2(-5f, -5f), new Vector2(0f, 0f), 7f),
            new (new Vector2(-100f, -100f), new Vector2(100f, 100f), 282.84f),

            // Mixed components
            new (Vector2.zero, new Vector2(0.1f, 100f), 100f),
            new (new Vector2(0.001f, 0.001f), new Vector2(1000f, 1000f), 1414.20f)
        };

        private static IEnumerable<EqualityComparerTestCase<Vector2>> Vector2TestCasesWithToleranceSufficient => new List<EqualityComparerTestCase<Vector2>>
        {
            // Exact matches
            new (Vector2.zero, Vector2.zero, 0f),
            new (Vector2.one, Vector2.one, 0f),
            new (new Vector2(123.456f, 789.012f), new Vector2(123.456f, 789.012f), 0f),
            
            // Zero tolerance edge cases
            new (Vector2.zero, Vector2.zero, 100f), // Much greater than needed

            // Small differences with matching tolerance
            new (Vector2.zero, new Vector2(1f, 0f), 1f),
            new (Vector2.zero, new Vector2(0f, 1f), 1f),
            new (Vector2.zero, new Vector2(0.1f, 0f), 0.1f),
            new (Vector2.zero, new Vector2(3f, 4f), 25f),
            new (Vector2.zero, new Vector2(5f, 0f), 25f),
            new (Vector2.zero, new Vector2(5f, 12f), 169f),

            // Diagonal vectors
            new (Vector2.zero, new Vector2(1f, 1f), 2f),

            // Non-zero starting points
            new (new Vector2(1f, 1f), new Vector2(2f, 2f), 2f),
            new (new Vector2(10f, 10f), new Vector2(13f, 14f), 25f),
            new (new Vector2(100f, 200f), new Vector2(103f, 204f), 25f),

            // Very small vectors (epsilon range)
            new (Vector2.zero, new Vector2(0.001f, 0.001f), 0.002f),
            new (new Vector2(0.0001f, 0.0001f), new Vector2(0.0002f, 0.0002f), 0.0004f),

            // Large magnitude vectors
            new (Vector2.zero, new Vector2(1000f, 0f), 1000000f),
            new (new Vector2(1000f, 1000f), new Vector2(2000f, 2000f), 2000000f),
            new (Vector2.zero, new Vector2(10000f, 10000f), 200000000f),
            new (Vector2.zero, new Vector2(50000f, 0f), 2500000000f),

            // Negative coordinates
            new (Vector2.zero, new Vector2(-1f, 0f), 1f),
            new (Vector2.zero, new Vector2(-3f, -4f), 25f),
            new (new Vector2(-5f, -5f), new Vector2(0f, 0f), 50f),
            new (new Vector2(-100f, -100f), new Vector2(100f, 100f), 80000f),

            // Mixed positive and negative
            new (new Vector2(-10f, 10f), new Vector2(10f, -10f), 800f),
            new (new Vector2(-1f, 1f), new Vector2(1f, -1f), 8f),

            // Mixed components
            new (Vector2.zero, new Vector2(0.01f, 1000f), 1000.00005f),
            new (new Vector2(0.001f, 0.001f), new Vector2(1000f, 1000f), 1414.21357f),

            // Tolerance much greater than needed
            new (Vector2.zero, new Vector2(1f, 0f), 1000f),
            new (new Vector2(10f, 20f), new Vector2(11f, 21f), 100f)
        };

        [Test]
        public void TestElympicsVector2EqualityComparerWithToleranceTooLow([ValueSource(nameof(Vector2TestCasesWithToleranceTooLow))] EqualityComparerTestCase<Vector2> testCase)
        {
            var comparer = new ElympicsVector2EqualityComparer(testCase.Tolerance);
            Assert.IsFalse(comparer.Equals(testCase.Left, testCase.Right));
        }

        [Test]
        public void TestElympicsVector2EqualityComparerWithToleranceSufficient([ValueSource(nameof(Vector2TestCasesWithToleranceSufficient))] EqualityComparerTestCase<Vector2> testCase)
        {
            var comparer = new ElympicsVector2EqualityComparer(testCase.Tolerance);
            Assert.IsTrue(comparer.Equals(testCase.Left, testCase.Right));
        }

        #endregion ElympicsVector2EqualityComparer tests

        #region ElympicsVector3EqualityComparer tests

        private static IEnumerable<EqualityComparerTestCase<Vector3>> Vector3TestCasesWithToleranceTooLow => new List<EqualityComparerTestCase<Vector3>>
        {
            // Small differences - single axis
            new (Vector3.zero, new Vector3(1f, 0f, 0f), 0.99f),
            new (Vector3.zero, new Vector3(0f, 1f, 0f), 0.99f),
            new (Vector3.zero, new Vector3(0f, 0f, 1f), 0.99f),
            new (Vector3.zero, new Vector3(0.1f, 0f, 0f), 0.099f),

            // Two-axis combinations
            new (Vector3.zero, new Vector3(3f, 4f, 0f), 4.99f),
            new (Vector3.zero, new Vector3(3f, 0f, 4f), 4.99f),
            new (Vector3.zero, new Vector3(0f, 3f, 4f), 4.99f),
            new (Vector3.zero, new Vector3(5f, 0f, 0f), 4.99f),

            // Three-axis combinations
            new (Vector3.zero, new Vector3(1f, 1f, 1f), 1.73f),
            new (Vector3.zero, new Vector3(2f, 3f, 6f), 6.99f),

            // Non-zero starting points
            new (new Vector3(1f, 1f, 1f), new Vector3(2f, 2f, 2f), 1.73f),
            new (new Vector3(10f, 10f, 10f), new Vector3(13f, 14f, 10f), 4.99f),
            
            // Very small vectors (epsilon range)
            new (Vector3.zero, new Vector3(float.Epsilon, 0f, 0f), float.Epsilon * float.Epsilon * 0.9f),
            new (Vector3.zero, new Vector3(0.001f, 0.001f, 0.001f), 0.000001f),

            // Large magnitude vectors
            new (Vector3.zero, new Vector3(1000f, 0f, 0f), 999f),
            new (new Vector3(1000f, 1000f, 1000f), new Vector3(2000f, 2000f, 2000f), 1732.05f),
            new (Vector3.zero, new Vector3(10000f, 10000f, 10000f), 17320f),

            // Negative coordinates
            new (Vector3.zero, new Vector3(-1f, 0f, 0f), 0.99f),
            new (new Vector3(-5f, -5f, -5f), new Vector3(0f, 0f, 0f), 8.66f),
            new (new Vector3(-100f, -100f, -100f), new Vector3(100f, 100f, 100f), 346.41f),

            // Mixed components
            new (Vector3.zero, new Vector3(0.1f, 100f, 0.1f), 100f),
            new (new Vector3(0.1f, 0.1f, 0.1f), new Vector3(1000f, 1000f, 1000f), 1731f)
        };

        private static IEnumerable<EqualityComparerTestCase<Vector3>> Vector3TestCasesWithToleranceSufficient => new List<EqualityComparerTestCase<Vector3>>
        {
            // Exact matches
            new (Vector3.zero, Vector3.zero, 0f),
            new (Vector3.one, Vector3.one, 0f),
            new (new Vector3(123.456f, 789.012f, 345.678f), new Vector3(123.456f, 789.012f, 345.678f), 0f),
            
            // Zero tolerance edge cases
            new (Vector3.zero, Vector3.zero, 100f), // Much greater than needed

            // Small differences with matching tolerance - single axis
            new (Vector3.zero, new Vector3(1f, 0f, 0f), 1f),
            new (Vector3.zero, new Vector3(0f, 1f, 0f), 1f),
            new (Vector3.zero, new Vector3(0f, 0f, 1f), 1f),
            new (Vector3.zero, new Vector3(0.1f, 0f, 0f), 0.1f),

            // Two-axis combinations
            new (Vector3.zero, new Vector3(3f, 4f, 0f), 25f),
            new (Vector3.zero, new Vector3(3f, 0f, 4f), 25f),
            new (Vector3.zero, new Vector3(0f, 3f, 4f), 25f),
            new (Vector3.zero, new Vector3(5f, 0f, 0f), 25f),
            new (Vector3.zero, new Vector3(5f, 12f, 0f), 169f),
            new (Vector3.zero, new Vector3(0f, 5f, 12f), 169f),

            // Three-axis combinations
            new (Vector3.zero, new Vector3(1f, 1f, 1f), 3f),
            new (Vector3.zero, new Vector3(2f, 3f, 6f), 49f),
            new (Vector3.zero, new Vector3(1f, 2f, 2f), 9f),

            // Non-zero starting points
            new (new Vector3(1f, 1f, 1f), new Vector3(2f, 2f, 2f), 3f),
            new (new Vector3(10f, 10f, 10f), new Vector3(13f, 14f, 10f), 25f),
            new (new Vector3(100f, 200f, 300f), new Vector3(103f, 204f, 300f), 25f),

            // Very small vectors (epsilon range)
            new (Vector3.zero, new Vector3(0.001f, 0.001f, 0.001f), 0.003f),
            new (new Vector3(0.0001f, 0.0001f, 0.0001f), new Vector3(0.0002f, 0.0002f, 0.0002f), 0.0006f),

            // Large magnitude vectors
            new (Vector3.zero, new Vector3(1000f, 0f, 0f), 1000000f),
            new (new Vector3(1000f, 1000f, 1000f), new Vector3(2000f, 2000f, 2000f), 3000000f),
            new (Vector3.zero, new Vector3(10000f, 10000f, 10000f), 300000000f),
            new (Vector3.zero, new Vector3(50000f, 0f, 0f), 2500000000f),

            // Negative coordinates - single axis
            new (Vector3.zero, new Vector3(-1f, 0f, 0f), 1f),
            new (Vector3.zero, new Vector3(0f, -1f, 0f), 1f),
            new (Vector3.zero, new Vector3(0f, 0f, -1f), 1f),

            // Negative coordinates - multiple axes
            new (Vector3.zero, new Vector3(-3f, -4f, 0f), 25f),
            new (Vector3.zero, new Vector3(-3f, 0f, -4f), 25f),
            new (Vector3.zero, new Vector3(0f, -3f, -4f), 25f),
            new (new Vector3(-5f, -5f, -5f), new Vector3(0f, 0f, 0f), 75f),
            new (new Vector3(-100f, -100f, -100f), new Vector3(100f, 100f, 100f), 120000f),

            // Mixed positive and negative
            new (new Vector3(-10f, 10f, 0f), new Vector3(10f, -10f, 0f), 800f),
            new (new Vector3(-1f, 1f, -1f), new Vector3(1f, -1f, 1f), 12f),
            new (new Vector3(-5f, 5f, -5f), new Vector3(5f, -5f, 5f), 300f),

            // Mixed components
            new (Vector3.zero, new Vector3(0.01f, 1000f, 0.01f), 1000.0001f),
            new (new Vector3(0.001f, 0.001f, 0.001f), new Vector3(1000f, 1000f, 1000f), 1732.05081f),

            // Tolerance much greater than needed
            new (Vector3.zero, new Vector3(1f, 0f, 0f), 1000f),
            new (new Vector3(10f, 20f, 30f), new Vector3(11f, 21f, 31f), 100f)
        };

        [Test]
        public void TestElympicsVector3EqualityComparerWithToleranceTooLow([ValueSource(nameof(Vector3TestCasesWithToleranceTooLow))] EqualityComparerTestCase<Vector3> testCase)
        {
            var comparer = new ElympicsVector3EqualityComparer(testCase.Tolerance);
            Assert.IsFalse(comparer.Equals(testCase.Left, testCase.Right));
        }

        [Test]
        public void TestElympicsVector3EqualityComparerWithToleranceSufficient([ValueSource(nameof(Vector3TestCasesWithToleranceSufficient))] EqualityComparerTestCase<Vector3> testCase)
        {
            var comparer = new ElympicsVector3EqualityComparer(testCase.Tolerance);
            Assert.IsTrue(comparer.Equals(testCase.Left, testCase.Right));
        }

        #endregion ElympicsVector3EqualityComparer tests

        public class EqualityComparerTestCase<T>
        {
            public T Left;
            public T Right;
            public float Tolerance;

            public EqualityComparerTestCase(T a, T b, float tolerance)
            {
                (Left, Right, Tolerance) = (a, b, tolerance);
            }

            public override string ToString() => $"({nameof(Left)}:{Left}, {nameof(Right)}:{Right}, {nameof(Tolerance)}:{Tolerance})";
        }
    }
}
