namespace Elympics
{
	internal partial class ElympicsGameConfigEditor
	{
		private const string Label_BotsInsideServerSummary = "Defines if server should run bots internally instead of independent processes. " +
		                                                     "Running bots independently requires a lot more resources, but allows to change internal state without consequences.";

		private const string Label_WebClientWarning = "Be careful using WebSocket and WebRTC with other types of build. It is working fine with Editor and WebGL build, " +
		                                              "but other platforms weren't tested and it is known that IL2CPP cannot serialize some methods used in native webrtc library wrapper.";
	}
}
