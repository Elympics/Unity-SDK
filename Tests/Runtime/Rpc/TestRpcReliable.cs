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
    public class TestRpcReliable
    {
        private GameObject _elympicsObject;
        private GameObject _rpcHolderObject;

        private ElympicsBaseTest _elympicsBase;
        private ElympicsBehaviour _elympicsBehaviour;
        private RpcHolderReliable _rpcHolder;

        #region Setup and teardown

        [OneTimeSetUp]
        public void PrepareScene()
        {
            const int maxPlayers = 1;
            ElympicsWorld.Current = new ElympicsWorld(maxPlayers);

            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(RpcHolderReliable));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_rpcHolder = _rpcHolderObject.GetComponent<RpcHolderReliable>());
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

        public record ReliableTestCase(
            ElympicsStatus Status,
            Action<RpcHolderReliable> Call,
            Func<RpcHolderReliable, bool> WasExecuted);

        private static List<ReliableTestCase> reliableTestCase =
            new()
            {
                new ReliableTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PlayerToServerMethodReliable(),
                    rpcHolder => rpcHolder.PlayerToServerReliableMethodCalled),
                new ReliableTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.ServerToPlayersMethodReliable(),
                    rpcHolder => rpcHolder.ServerToPlayersReliableMethodCalled),
            };

        [Test]
        public void ReliableRpcShouldBeSentUsingReliableQueues([ValueSource(nameof(reliableTestCase))] ReliableTestCase testCase)
        {
            _elympicsBase.SetElympicsStatus(testCase.Status);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            testCase.Call(_rpcHolder);

            const int messageCount = 1;

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.EqualTo(messageCount));
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.True);
        }

        private static List<ReliableTestCase> unreliableTestCases =
            new()
            {
                new ReliableTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PlayerToServerMethodUnreliable(),
                    rpcHolder => rpcHolder.PlayerToServerUnreliableMethodCalled),
                new ReliableTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.ServerToPlayersMethodUnreliable(),
                    rpcHolder => rpcHolder.ServerToPlayersUnreliableMethodCalled),
            };

        [Test]
        public void UnreliableRpcShouldBeSentUsingUnreliableQueues([ValueSource(nameof(unreliableTestCases))] ReliableTestCase testCase)
        {
            _elympicsBase.SetElympicsStatus(testCase.Status);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            testCase.Call(_rpcHolder);

            const int messageCount = 1;

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.EqualTo(messageCount));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSendReliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToSendUnreliable.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.True);
        }
    }
}
