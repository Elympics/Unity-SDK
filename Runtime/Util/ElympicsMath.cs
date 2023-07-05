using System;
using System.Data;


namespace Elympics
{
	internal static class ElympicsMath
	{
		/// <summary>
		/// .NET 4 (on which Unity works) lacks Math.Clamp method. It is introduced in .NET 6
		/// </summary>
		internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (min.CompareTo(max) > 0)
			{
				throw new NotSupportedException("Min value cannot be greater than max.");
			}

			if (val.CompareTo(min) < 0) return min;
			if (val.CompareTo(max) > 0) return max;
			return val;
		}
	}
}