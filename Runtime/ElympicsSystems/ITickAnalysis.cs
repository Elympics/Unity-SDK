using System;
using System.Collections.Generic;

namespace Elympics
{
	public interface ITickAnalysis
	{
		bool Paused { get; }

		void Attach(Action<ElympicsSnapshot> snapshotApplier, bool[] isBots = null);
		void Detach();
		void AddSnapshotToAnalysis(ElympicsSnapshotWithMetadata snapshot, List<ElympicsSnapshotWithMetadata> reconciliationSnapshots, ClientTickCalculatorNetworkDetails networkDetails);
	}
}
