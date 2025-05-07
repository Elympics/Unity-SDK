namespace Elympics
{
    public interface IRenderer
    {
        /// <summary>
        /// Called each Unity Update, guaranteed to be called after ElympicsUpdate. Called on <see cref="ElympicsClient"/> only.
        /// </summary>
        /// <param name="renderData">Current render frame data <see cref="RenderData"/>></param>
        void Render(in RenderData renderData);
    }

    public readonly struct RenderData
    {
        /// <summary>Normalized time value between when current tick should have started and when next tick should start.</summary>
        /// <remarks>
        /// When <see cref="FirstFrame"/> is true, this value will usually be close to 0.
        /// It will then grow larger with each call to <see cref="IRenderer.Render"/> until it approaches 1,
        /// at which point new tick is processed, and the process starts over.
        /// </remarks>

        public float Alpha { get; init; }

        /// <summary>
        /// True if it is the first callback after ElympicsUpdate.
        /// </summary>
        public bool FirstFrame { get; init; }
    }
}
