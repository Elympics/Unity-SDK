using System.Text;
using MatchTcpLibrary;
using UnityEngine;

namespace Elympics
{
	public class GameServerJsonSerializer : IGameServerSerializer
	{
		public byte[] Serialize(object obj)
		{
			return Encoding.ASCII.GetBytes(JsonUtility.ToJson(obj));
		}

		public T Deserialize<T>(byte[] data)
		{
			return JsonUtility.FromJson<T>(Encoding.ASCII.GetString(data));
		}
	}
}
