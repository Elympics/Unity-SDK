namespace Elympics
{
	public class ElympicsGameObjectEqualityComparer : ElympicsVarEqualityComparer<ElympicsBehaviour>
	{
		public ElympicsGameObjectEqualityComparer() : base(1.0f)
		{
		}

		protected override float Distance(ElympicsBehaviour a, ElympicsBehaviour b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return 0f;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return float.PositiveInfinity;

			return a.networkId == b.networkId ? 0f : float.PositiveInfinity;
		}
	}
}
