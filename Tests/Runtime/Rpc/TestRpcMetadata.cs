using System;
using System.Collections.Generic;
using Elympics.Replication;
using Elympics.Tests.RpcMocks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("RPC")]
    public class TestRpcMetadata
    {
        private GameObject _elympicsObject;
        private GameObject _rpcHolderObject;

        private ElympicsBaseTest _elympicsBase;
        private ElympicsBehaviour _elympicsBehaviour;
        private RpcHolderWithMetadata _rpcHolder;

        #region Setup and teardown

        [OneTimeSetUp]
        public void PrepareScene()
        {
            const int maxPlayers = 1;
            ElympicsWorld.Current = new ElympicsWorld(maxPlayers);

            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(RpcHolderWithMetadata));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_rpcHolder = _rpcHolderObject.GetComponent<RpcHolderWithMetadata>());
            Assert.NotNull(_elympicsBehaviour = _rpcHolder.ElympicsBehaviour);
            _elympicsBehaviour.AutoAssignNetworkId = true;
            _elympicsBehaviour.networkId = 1001;
            var behavioursManager = _elympicsObject.GetComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = _elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);

            _elympicsBase.SetElympicsStatus(new ElympicsStatus(false, true, false));
            _elympicsBase.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>(), behavioursManager);
            behavioursManager.factory = factory;

            behavioursManager.InitializeInternal(_elympicsBase, maxPlayers);
        }

        [SetUp]
        public void ResetSut()
        {
            _elympicsBase.SetElympicsStatus(new ElympicsStatus(false, false, false));
            _elympicsBase.SetPermanentCallContext(ElympicsBase.CallContext.None);
            _elympicsBase.ClearRpcQueues();
            _elympicsBehaviour.OnPostReconcile();
            _rpcHolder.Reset();
        }

        [OneTimeTearDown]
        public void CleanScene()
        {
            Object.Destroy(_elympicsObject);
            Object.Destroy(_rpcHolderObject);
        }

        #endregion

        [Test]
        public void AccessingPropertiesOfUninitializedRpcMetadataShouldThrow()
        {
            var rpcMetadata = new RpcMetadata();

            _ = Assert.Throws<InvalidOperationException>(() => _ = rpcMetadata.Sender);
        }

        [Test]
        public void EqualityOperatorsShouldWorkCorrectly()
        {
            var uninitialized = new RpcMetadata();
            RpcMetadata defaultValue = default;
            var defaultArgument = new RpcMetadata(default);
            var nonDefaultArgument = new RpcMetadata(ElympicsPlayer.World);

            Assert.True(uninitialized == uninitialized);
            Assert.True(defaultValue == defaultValue);
            Assert.True(uninitialized.Equals((object)defaultValue));
            Assert.True(defaultArgument == defaultArgument);
            Assert.True(nonDefaultArgument == nonDefaultArgument);

            Assert.True(uninitialized != defaultArgument);
            Assert.True(uninitialized != nonDefaultArgument);
            Assert.True(defaultValue != defaultArgument);
            Assert.True(defaultValue != nonDefaultArgument);
        }

        public record OmittedTestCase(
            ElympicsStatus Status,
            Action<RpcHolderWithMetadata> Call,
            Func<RpcHolderWithMetadata, RpcMetadata?> Args,
            ElympicsPlayer ExpectedSender);

        private static List<OmittedTestCase> omittedTestCases =
            new()
            {
                new OmittedTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataUsingDefault(),
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs,
                    ElympicsPlayer.FromIndex(0)),
                new OmittedTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataUsingNew(),
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs,
                    ElympicsPlayer.FromIndex(0)),
                new OmittedTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.StpWithOptionalMetadataUsingDefault(),
                    rpcHolder => rpcHolder.StpWithOptionalMetadataArgs,
                    ElympicsPlayer.World),
                new OmittedTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.StpWithOptionalMetadataUsingNew(),
                    rpcHolder => rpcHolder.StpWithOptionalMetadataArgs,
                    ElympicsPlayer.World),
            };

        [Test]
        public void OmittedOptionalMetadataArgumentShouldBeSupplied([ValueSource(nameof(omittedTestCases))] OmittedTestCase testCase)
        {
            using var _ = _elympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(testCase.Status);
            testCase.Call(_rpcHolder);

            Assert.That(testCase.Args(_rpcHolder), Is.Null);

            AssertRpcIsQueuedAndInvoked();

            var args = testCase.Args(_rpcHolder);
            Assert.That(args.HasValue, Is.True);
            var rpcMetadata = args!.Value;
            Assert.That(rpcMetadata.Sender, Is.EqualTo(testCase.ExpectedSender));
        }

        public record SuppliedTestCase(
            ElympicsStatus Status,
            Action<RpcHolderWithMetadata, RpcMetadata> Call,
            Func<RpcHolderWithMetadata, RpcMetadata?> Args,
            ElympicsPlayer ExpectedSender);

        private static List<SuppliedTestCase> suppliedTestCases =
            new()
            {
                new SuppliedTestCase(
                    ElympicsStatus.StandaloneClient,
                    (rpcHolder, metadata) => rpcHolder.PtsWithOptionalMetadataUsingDefault(metadata),
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs,
                    ElympicsPlayer.FromIndex(0)),
                new SuppliedTestCase(
                    ElympicsStatus.StandaloneClient,
                    (rpcHolder, metadata) => rpcHolder.PtsWithOptionalMetadataUsingNew(metadata),
                    rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs,
                    ElympicsPlayer.FromIndex(0)),
                new SuppliedTestCase(
                    ElympicsStatus.StandaloneServer,
                    (rpcHolder, metadata) => rpcHolder.StpWithOptionalMetadataUsingDefault(metadata),
                    rpcHolder => rpcHolder.StpWithOptionalMetadataArgs,
                    ElympicsPlayer.World),
                new SuppliedTestCase(
                    ElympicsStatus.StandaloneServer,
                    (rpcHolder, metadata) => rpcHolder.StpWithOptionalMetadataUsingNew(metadata),
                    rpcHolder => rpcHolder.StpWithOptionalMetadataArgs,
                    ElympicsPlayer.World),
            };

        [Test]
        public void SuppliedOptionalMetadataArgumentShouldBeOverriden([ValueSource(nameof(suppliedTestCases))] SuppliedTestCase testCase)
        {
            using var _ = _elympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(testCase.Status);
            var expectedMetadata = new RpcMetadata(ElympicsPlayer.Invalid);

            testCase.Call(_rpcHolder, expectedMetadata);
            Assert.That(testCase.Args(_rpcHolder), Is.Null);

            AssertRpcIsQueuedAndInvoked();

            var args = testCase.Args(_rpcHolder);
            Assert.That(args, Is.Not.Null);
            var rpcMetadata = args!.Value;
            Assert.That(rpcMetadata.Sender, Is.EqualTo(testCase.ExpectedSender));
        }

        #region Helpers

        private void AssertRpcIsQueuedAndInvoked(int messageCount = 1)
        {
            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.EqualTo(messageCount));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
        }

        #endregion
    }
}
