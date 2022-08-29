using System;
using System.Collections;
using System.Threading;

namespace Elympics
{
	public interface IElympics
	{
		/// <value>Current player identifier.</value>
		/// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), their ID is only available during input gathering and game
		/// initialization.</remarks>
		ElympicsPlayer Player { get; }

		/// <value>Is game instance a server?</value>
		/// <remarks>If client or bot is handled in server (e.g. in "Local Player And Bots" mode), the property is always truthy for them.</remarks>
		bool IsServer { get; }

		/// <value>Is game instance a client?</value>
		/// <remarks>If client is handled in server (in "Local Player And Bots" mode), the property is only meaningful during input gathering and game
		/// initialization.</remarks>
		bool IsClient { get; }

		/// <value>Is game instance a bot?</value>
		/// <remarks>If bot is handled in server (in "Local Player And Bots" mode or with "Bots inside server" option checked), the property is only meaningful
		/// during input gathering and game initialization.</remarks>
		bool IsBot { get; }

		/// <value>The interval in seconds at which network synchronization occurs. It is equal to <see cref="UnityEngine.Time.fixedDeltaTime"/> and calculated as 1/<see cref="TicksPerSecond"/>.</value>
		float TickDuration { get; }

		/// <value>The total number of ticks per seconds.</value>
		/// <seealso cref="TickDuration"/>
		int TicksPerSecond { get; }

		/// <value>Number of current tick</value>
		long Tick { get; }

		bool TryGetBehaviour(int networkId, out ElympicsBehaviour elympicsBehaviour);

		#region Client

		IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct);
		IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct);
		void        Disconnect();

		#endregion

		#region Server

		void EndGame(ResultMatchPlayerDatas result = null);

		#endregion
	}
}
