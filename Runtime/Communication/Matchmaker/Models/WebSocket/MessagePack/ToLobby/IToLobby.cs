using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
	[Union(0, typeof(Ping))]
	[Union(1, typeof(Pong))]
	[Union(2, typeof(GameData))]
	[Union(3, typeof(JoinMatchmaker))]
	public interface IToLobby
	{
	}
}
