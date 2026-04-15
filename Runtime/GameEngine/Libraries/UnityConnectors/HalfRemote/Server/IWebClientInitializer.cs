using System.Threading.Tasks;

namespace UnityConnectors.HalfRemote.Server
{
	public interface IWebClientInitializer
	{
		Task<string> InitClientAndCreateAnswer(string offer);
	}
}
