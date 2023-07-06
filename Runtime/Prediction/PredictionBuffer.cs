
namespace Elympics
{
    public class PredictionBuffer
    {
        private readonly ElympicsDataWithTickBuffer<ElympicsInput> _inputs;
        private readonly ElympicsDataWithTickBuffer<ElympicsSnapshot> _snapshots;

        public PredictionBuffer(ElympicsGameConfig elympicsGameConfig)
        {
            _inputs = new ElympicsDataWithTickBuffer<ElympicsInput>(elympicsGameConfig.PredictionBufferSize);
            _snapshots = new ElympicsDataWithTickBuffer<ElympicsSnapshot>(elympicsGameConfig.PredictionBufferSize);
        }

        public void UpdateMinTick(long tick)
        {
            _inputs.UpdateMinTick(tick);
            _snapshots.UpdateMinTick(tick);
        }

        public bool AddInputToBuffer(ElympicsInput input) =>
            _inputs.TryAddData(input);

        public bool AddSnapshotToBuffer(ElympicsSnapshot snapshot) =>
            _snapshots.TryAddData(snapshot);

        public bool AddOrReplaceSnapshotInBuffer(ElympicsSnapshot snapshot) =>
            _snapshots.TryAddOrReplaceData(snapshot);

        public bool TryGetInputFromBuffer(long tick, out ElympicsInput input) =>
            _inputs.TryGetDataForTick(tick, out input);

        public bool TryGetSnapshotFromBuffer(long tick, out ElympicsSnapshot snapshot) =>
            _snapshots.TryGetDataForTick(tick, out snapshot);
    }
}
