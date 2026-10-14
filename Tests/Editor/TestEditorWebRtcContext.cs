#if ELYMPICS_LAZY_WEBRTC_CONTEXT
using Elympics.GameEngine.Libraries.WebRtc;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    public class TestEditorWebRtcContext
    {
        [Test]
        public void TheInstalledWebRtcPackageHasTheInternalsTheLazyContextUses() => Assert.That(EditorWebRtcContext.IsSupported, Is.True);

        [Test]
        public void TheWebRtcContextIsStoppedUntilElympicsNeedsIt() => Assert.That(EditorWebRtcContext.IsInitialized, Is.False);
    }
}
#endif
