#nullable enable

using System;
using Unity.WebRTC;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal static class WebRtcNative
    {
        internal static RTCPeerConnection CreatePeerConnection()
        {
            EditorWebRtcContext.EnsureInitialized();
            return new RTCPeerConnection();
        }

        internal static RTCPeerConnection CreatePeerConnection(ref RTCConfiguration configuration)
        {
            EditorWebRtcContext.EnsureInitialized();
            return new RTCPeerConnection(ref configuration);
        }

        // On Application.quitting, com.unity.webrtc disposes every native object, in an order relative to ours that isn't guaranteed.
        // A flag set by us can't tell whether that has already happened, so the call itself has to be guarded.
        internal static void IgnoreDisposed(Action action)
        {
            try
            {
                action();
            }
            catch (ObjectDisposedException)
            { }
        }
    }
}
