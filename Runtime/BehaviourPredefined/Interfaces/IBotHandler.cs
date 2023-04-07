using System;

namespace Elympics
{
	[Obsolete("Use " + nameof(IBotHandlerGuid) + " instead")]
	public interface IBotHandler : IObservable
	{
		/// <summary>
		/// Called on standalone bot initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="initialMatchPlayerData">Initialization data to be used by the bot.</param>
		void OnStandaloneBotInit(InitialMatchPlayerData initialMatchPlayerData);

		/// <summary>
		/// Called on bots-in-server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="initialMatchPlayerDatas">Initialization data of all bots included in the server.</param>
		/// <remarks>Used in "Local Player And Bots" development mode or if "Bots inside server" option is checked.</remarks>
		void OnBotsOnServerInit(InitialMatchPlayerDatas initialMatchPlayerDatas);
	}
}
