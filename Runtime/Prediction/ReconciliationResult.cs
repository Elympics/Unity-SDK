namespace Elympics
{
    internal readonly struct ReconciliationResult
    {
        internal enum Outcome
        {
            None,
            Replayed,
            Reanchored,
        }

        public readonly Outcome Kind;
        public readonly long AnchoredTick;

        public bool Performed => Kind != Outcome.None;
        public bool WasReanchored => Kind == Outcome.Reanchored;

        public static ReconciliationResult None => default;
        public static ReconciliationResult Replayed() => new(Outcome.Replayed, 0);
        public static ReconciliationResult Reanchored(long anchoredTick) => new(Outcome.Reanchored, anchoredTick);

        private ReconciliationResult(Outcome kind, long anchoredTick)
        {
            Kind = kind;
            AnchoredTick = anchoredTick;
        }
    }
}
