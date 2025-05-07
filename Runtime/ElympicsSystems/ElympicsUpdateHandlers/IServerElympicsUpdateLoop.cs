namespace Elympics.ElympicsSystems
{
    internal interface IServerElympicsUpdateLoop
    {
        public long Tick { get; }
        public ElympicsSnapshot GenerateSnapshot();
        public void FinalizeTick(ElympicsSnapshot snapshot);

        public void HandleRenderFrame(in RenderData renderData);
    }
}
