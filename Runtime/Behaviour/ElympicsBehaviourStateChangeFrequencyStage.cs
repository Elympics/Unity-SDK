using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Elympics
{
	[System.Serializable]
	public class ElympicsBehaviourStateChangeFrequencyStage
	{
		[SerializeField] private int stageDurationInMiliseconds = -1;
		[SerializeField] private int frequencyInMiliseconds = -1;

		[SerializeField] private int maxStageDurationInMiliseconds = 5000;

		public ElympicsBehaviourStateChangeFrequencyStage(int stageDurationInMiliseconds, int frequencyInMiliseconds)
		{
			this.stageDurationInMiliseconds = stageDurationInMiliseconds;
			this.frequencyInMiliseconds = frequencyInMiliseconds;
		}

		public int StageDurationInMiliseconds => stageDurationInMiliseconds;
		public int FrequencyInMiliseconds => frequencyInMiliseconds;
	}
}
