using System;

namespace Elympics
{
    internal class ElympicsBehaviourStateChangeFrequencyCalculator
    {
        public class ElympicsBehaviourStateChangeFrequencyStageInTicks
        {
            public int StateDurationInTicks { get; private set; }
            public int UpdateFrequencyInTicks { get; private set; }

            public ElympicsBehaviourStateChangeFrequencyStageInTicks(int stateDurationInTicks, int updateFrequencyInTicks)
            {
                StateDurationInTicks = stateDurationInTicks;
                UpdateFrequencyInTicks = updateFrequencyInTicks;
            }
        }

        private ElympicsBehaviourStateChangeFrequencyStageInTicks currentStateUpdateFrequencyStage;
        private ElympicsBehaviourStateChangeFrequencyStageInTicks[] stateUpdateFrequencyStages;
        private int currentStageIndex = 0;
        private int currentSendingSnapshotCalls = 0;
        private byte[] previousState = null;

        private Func<byte[], byte[], bool> areStatesEqualsFunc = null;

        public ElympicsBehaviourStateChangeFrequencyCalculator(ElympicsBehaviourStateChangeFrequencyStage[] stateUpdateFrequencyStages, Func<byte[], byte[], bool> areStatesEqualsFunc)
        {
            this.areStatesEqualsFunc = areStatesEqualsFunc;

            CreateStateUpdateFrequencyStagesInTicks(stateUpdateFrequencyStages, ElympicsConfig.LoadCurrentElympicsGameConfig().TicksPerSecond);
        }

        private bool CanSkipStateSynchronizingInCurrentSnapshotCall()
        {
            if (currentStateUpdateFrequencyStage.UpdateFrequencyInTicks >= 1)
                return currentSendingSnapshotCalls % currentStateUpdateFrequencyStage.UpdateFrequencyInTicks != 0;
            else
                return false;
        }

        private void IncreaseSendingSnapshotCalls()
        {
            currentSendingSnapshotCalls++;

            TryToSetNextStateUpdateFrequencyStage();
        }

        private void TryToSetNextStateUpdateFrequencyStage()
        {
            if (currentSendingSnapshotCalls < currentStateUpdateFrequencyStage.StateDurationInTicks)
                return;

            if (currentStageIndex + 1 < stateUpdateFrequencyStages.Length)
                currentStageIndex++;

            currentStateUpdateFrequencyStage = stateUpdateFrequencyStages[currentStageIndex];
            currentSendingSnapshotCalls = 0;
        }

        internal void ResetStateUpdateFrequencyStage()
        {
            currentStageIndex = 0;
            currentStateUpdateFrequencyStage = stateUpdateFrequencyStages[currentStageIndex];
            currentSendingSnapshotCalls = 0;
        }

        internal bool UpdateNextStateAndCheckIfSendCanBeSkipped(byte[] currentState)
        {
            IncreaseSendingSnapshotCalls();

            var statesEqual = previousState != null && areStatesEqualsFunc(currentState, previousState);
            var canSkipStateSynchronization = CanSkipStateSynchronizingInCurrentSnapshotCall();

            if (!statesEqual)
                ResetStateUpdateFrequencyStage();

            var sendCanBeSkipped = statesEqual && canSkipStateSynchronization;

            if (!sendCanBeSkipped)
                SetPreviousState(currentState);

            return sendCanBeSkipped;
        }

        private void SetPreviousState(byte[] currentState)
        {
            previousState = currentState;
        }

        private void CreateStateUpdateFrequencyStagesInTicks(ElympicsBehaviourStateChangeFrequencyStage[] stateUpdateFrequencyStagesInMiliseconds, int ticksPerSecond)
        {
            stateUpdateFrequencyStages = new ElympicsBehaviourStateChangeFrequencyStageInTicks[stateUpdateFrequencyStagesInMiliseconds.Length];

            for (var i = 0; i < stateUpdateFrequencyStagesInMiliseconds.Length; i++)
            {
                stateUpdateFrequencyStages[i] = new ElympicsBehaviourStateChangeFrequencyStageInTicks(
                    MsToTicks(stateUpdateFrequencyStagesInMiliseconds[i].StageDurationInMiliseconds, ticksPerSecond),
                    MsToTicks(stateUpdateFrequencyStagesInMiliseconds[i].FrequencyInMiliseconds, ticksPerSecond));
            }

            ResetStateUpdateFrequencyStage();
        }

        private int MsToTicks(int milliseconds, int ticksPerSecond)
        {
            return (int)Math.Round(ticksPerSecond * milliseconds / 1000.0);
        }

    }
}
