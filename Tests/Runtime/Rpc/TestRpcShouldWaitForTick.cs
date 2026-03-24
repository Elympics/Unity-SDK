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
    public class TestRpcShouldWaitForTick
    {
        private GameObject _elympicsObject;
        private GameObject _rpcHolderObject;

        private ElympicsBaseTest _elympicsBase;
        private ElympicsBehaviour _elympicsBehaviour;
        private RpcHolderWaitForTick _rpcHolder;

        #region Setup and teardown

        [OneTimeSetUp]
        public void PrepareScene()
        {
            const int maxPlayers = 1;
            ElympicsWorld.Current = new ElympicsWorld(maxPlayers);

            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(RpcHolderWaitForTick));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_rpcHolder = _rpcHolderObject.GetComponent<RpcHolderWaitForTick>());
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

        public record WaitingTestCase(
            ElympicsStatus Status,
            Action<RpcHolderWaitForTick> Call,
            Func<RpcHolderWaitForTick, bool> WasExecuted);

        private static List<WaitingTestCase> waitingTestCases =
            new()
            {
                new WaitingTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PlayerToServerMethodWaiting(),
                    rpcHolder => rpcHolder.PlayerToServerWaitingMethodCalled),
                new WaitingTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.ServerToPlayersMethodWaiting(),
                    rpcHolder => rpcHolder.ServerToPlayersWaitingMethodCalled),
            };

        [Test]
        public void RpcWithWaitForTickSetToTrueShouldNotBeExecutedInTickPriorToSendingTick([ValueSource(nameof(waitingTestCases))] WaitingTestCase testCase)
        {
            _elympicsBase.SetTick(5);
            _elympicsBase.SetElympicsStatus(testCase.Status);

            testCase.Call(_rpcHolder);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            const int messageCount = 1;

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.EqualTo(messageCount));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));

            _elympicsBase.SetTick(4);
            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            _elympicsBase.SetTick(5);
            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.True);
        }

        private static List<WaitingTestCase> notWaitingTestCases =
            new()
            {
                new WaitingTestCase(
                    ElympicsStatus.StandaloneClient,
                    rpcHolder => rpcHolder.PlayerToServerMethodNotWaiting(),
                    rpcHolder => rpcHolder.PlayerToServerNotWaitingMethodCalled),
                new WaitingTestCase(
                    ElympicsStatus.StandaloneServer,
                    rpcHolder => rpcHolder.ServerToPlayersMethodNotWaiting(),
                    rpcHolder => rpcHolder.ServerToPlayersNotWaitingMethodCalled),
            };

        [Test]
        public void RpcWithWaitForTickSetToFalseShouldNotBeExecutedAsSoonAsPossible([ValueSource(nameof(notWaitingTestCases))] WaitingTestCase testCase)
        {
            _elympicsBase.SetTick(5);
            _elympicsBase.SetElympicsStatus(testCase.Status);

            testCase.Call(_rpcHolder);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.False);

            const int messageCount = 1;

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.EqualTo(messageCount));
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.EqualTo(messageCount));

            _elympicsBase.SetTick(4);
            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.That(_elympicsBase.RpcMessagesToSend.Count, Is.Zero);
            Assert.That(_elympicsBase.RpcMessagesToInvoke.Count, Is.Zero);
            Assert.That(testCase.WasExecuted(_rpcHolder), Is.True);
        }
    }
}
