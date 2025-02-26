namespace Elympics
{
    public enum TerminationOption
    {
        /// <summary>
        /// Do not auto-terminate server, regardless of how many players leave the game.
        /// </summary>
        None = 0,

        /// <summary>
        /// Auto-terminate server if any player leaves the game.
        /// </summary>
        Any = 1,

        /// <summary>
        /// Auto-terminate server if all players leave the game.
        /// </summary>
        All = 2,
    }
}
