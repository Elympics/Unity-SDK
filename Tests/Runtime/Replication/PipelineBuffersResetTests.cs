using NUnit.Framework;

namespace Elympics.Tests.Runtime.Replication
{
    /// <summary>
    /// PipelineBuffers.ResetForReconnect was removed — ReplicationPipeline is server-only
    /// and the server never reconnects. These tests are intentionally empty.
    /// The file is kept to avoid meta file churn.
    /// </summary>
    [TestFixture]
    [Category("Replication")]
    public class PipelineBuffersResetTests
    {
    }
}
