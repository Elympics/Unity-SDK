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

        public record OmittedTestCase(ElympicsStatus Status, Action<RpcHolderWithMetadata> Call, Func<RpcHolderWithMetadata, ValueTuple<RpcMetadata>?> Args);

        private static List<OmittedTestCase> omittedTestCases =
            new()
            {
                new(ElympicsStatus.StandaloneClient, rpcHolder => rpcHolder.PtsWithOptionalMetadataUsingDefault(), rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneClient, rpcHolder => rpcHolder.PtsWithOptionalMetadataUsingNew(), rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneServer, rpcHolder => rpcHolder.StpWithOptionalMetadataUsingDefault(), rpcHolder => rpcHolder.StpWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneServer, rpcHolder => rpcHolder.StpWithOptionalMetadataUsingNew(), rpcHolder => rpcHolder.StpWithOptionalMetadataArgs),
            };

        [Test]
        public void OmittedOptionalMetadataArgumentShouldBeSupplied([ValueSource(nameof(omittedTestCases))] OmittedTestCase testCase)
        {
            using var _ = _elympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(testCase.Status);

            testCase.Call(_rpcHolder);

            Assert.That(testCase.Args(_rpcHolder), Is.Null);
            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.EqualTo(1));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(1));
            Assert.That(_elympicsBase.RpcMessagesToInvoke[0].Messages.Count, Is.EqualTo(1));

            _elympicsBase.InvokeQueuedRpcMessages();

            var args = testCase.Args(_rpcHolder);
            Assert.That(args, Is.Not.Null);
            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(args.Value.Item1, Is.EqualTo(new RpcMetadata(ElympicsPlayer.FromIndex(0))));
        }

        public record SuppliedTestCase(ElympicsStatus Status, Action<RpcHolderWithMetadata, RpcMetadata> Call, Func<RpcHolderWithMetadata, ValueTuple<RpcMetadata>?> Args);
        private static List<SuppliedTestCase> suppliedTestCases =
            new()
            {
                new(ElympicsStatus.StandaloneClient, (rpcHolder, metadata) => rpcHolder.PtsWithOptionalMetadataUsingDefault(metadata), rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneClient, (rpcHolder, metadata) => rpcHolder.PtsWithOptionalMetadataUsingNew(metadata), rpcHolder => rpcHolder.PtsWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneServer, (rpcHolder, metadata) => rpcHolder.StpWithOptionalMetadataUsingDefault(metadata), rpcHolder => rpcHolder.StpWithOptionalMetadataArgs),
                new(ElympicsStatus.StandaloneServer, (rpcHolder, metadata) => rpcHolder.StpWithOptionalMetadataUsingNew(metadata), rpcHolder => rpcHolder.StpWithOptionalMetadataArgs),
            };

        [Test]
        public void SuppliedOptionalMetadataArgumentShouldBeOverriden([ValueSource(nameof(suppliedTestCases))] SuppliedTestCase testCase)
        {
            using var _ = _elympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(testCase.Status);
            var expectedMetadata = new RpcMetadata(ElympicsPlayer.Invalid);

            testCase.Call(_rpcHolder, expectedMetadata);

            Assert.That(testCase.Args(_rpcHolder), Is.Null);
            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.EqualTo(1));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(1));
            Assert.That(_elympicsBase.RpcMessagesToInvoke[0].Messages.Count, Is.EqualTo(1));

            _elympicsBase.InvokeQueuedRpcMessages();

            var args = testCase.Args(_rpcHolder);
            Assert.That(args, Is.Not.Null);
            Assert.That(_elympicsBase.RpcMessagesToSend.Messages.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(args.Value.Item1, Is.EqualTo(new RpcMetadata(ElympicsPlayer.FromIndex(0))));
        }
    }
}
