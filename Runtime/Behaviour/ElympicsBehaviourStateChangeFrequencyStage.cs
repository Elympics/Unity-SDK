using UnityEngine;

namespace Elympics
{
    [System.Serializable]
    public class ElympicsBehaviourStateChangeFrequencyStage
    {
        [SerializeField] private int stageDurationInMiliseconds = -1;
        [SerializeField] private int frequencyInMiliseconds = -1;

#pragma warning disable CS0414
        [SerializeField] private int maxStageDurationInMiliseconds = 5000;
#pragma warning restore CS0414
        public ElympicsBehaviourStateChangeFrequencyStage(int stageDurationInMiliseconds, int frequencyInMiliseconds)
        {
            this.stageDurationInMiliseconds = stageDurationInMiliseconds;
            this.frequencyInMiliseconds = frequencyInMiliseconds;
        }

        public int StageDurationInMiliseconds => stageDurationInMiliseconds;
        public int FrequencyInMiliseconds => frequencyInMiliseconds;
    }
}
