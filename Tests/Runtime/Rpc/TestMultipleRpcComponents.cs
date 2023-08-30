using Elympics.Tests.RpcMocks;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("RPC")]
    internal class TestMultipleRpcComponents
    {
        private GameObject _elympicsObject;
        private GameObject _rpcHolderObject;

        private ElympicsBaseTest _elympicsBase;
        private ElympicsBehaviour _elympicsBehaviour;
        private TestRpcHolder _testRpcHolder;
        private AnotherTestRpcHolder _anotherTestRpcHolder;

        [OneTimeSetUp]
        public void PrepareScene()
        {
            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(TestRpcHolder),
                typeof(AnotherTestRpcHolder));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_testRpcHolder = _rpcHolderObject.GetComponent<TestRpcHolder>());
            Assert.NotNull(_anotherTestRpcHolder = _rpcHolderObject.GetComponent<AnotherTestRpcHolder>());
            Assert.NotNull(_elympicsBehaviour = _testRpcHolder.ElympicsBehaviour);
            var behavioursManager = _elympicsObject.GetComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = _elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);

            _elympicsBase.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>());
            _elympicsBase.elympicsBehavioursManager = behavioursManager;
            behavioursManager.factory = factory;

            behavioursManager.InitializeInternal(_elympicsBase);
        }

        [SetUp]
        public void ResetSut()
        {
            _elympicsBase.SetElympicsStatus(new ElympicsStatus(false, false, false));
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.None;
            _elympicsBase.ClearRpcQueues();
            _elympicsBehaviour.OnPostReconcile();
            _testRpcHolder.Reset();
        }

        [OneTimeTearDown]
        public void CleanScene()
        {
            Object.Destroy(_elympicsObject);
            Object.Destroy(_rpcHolderObject);
        }

        [Test]
        public void RpcMethodMapShouldBeCorrectlyRegisteredAndSortedByComponentIndexAndByMethodNameAlphabetically()
        {
            var sortedRpcMethods = new RpcMethod[]
            {
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingPlayerToServer)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingServerToPlayers)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethod)), _testRpcHolder),
                new(_testRpcHolder.PlayerToServerMethodPrivateInfo, _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethodWithArgs)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongPlayerToServer)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongServerToPlayers)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethod)), _testRpcHolder),
                new(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethodWithArgs)), _testRpcHolder),
                new(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.PlayerToServerMethod)), _anotherTestRpcHolder),
                new(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.PlayerToServerMethodWithArgs)), _anotherTestRpcHolder),
                new(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.ServerToPlayersMethod)), _anotherTestRpcHolder),
                new(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.ServerToPlayersMethodWithArgs)), _anotherTestRpcHolder),
            };

            for (ushort methodId = 0; methodId < sortedRpcMethods.Length; methodId++)
                Assert.AreEqual(sortedRpcMethods[methodId], _elympicsBehaviour.RpcMethods[methodId]);
        }

        [Test]
        public void MultiplePlayerToServerRpcsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _testRpcHolder.ElympicsBehaviour.ElympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);
            var expectedArgs = (false, byte.MinValue, sbyte.MinValue, ushort.MinValue, short.MinValue, uint.MinValue,
                int.MinValue, ulong.MinValue, long.MinValue, float.MinValue, double.MinValue, char.MinValue, "");
            var firstRpcMethod = new RpcMethod(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethodWithArgs)), _testRpcHolder);
            var secondRpcMethod = new RpcMethod(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.PlayerToServerMethod)), _anotherTestRpcHolder);

            _testRpcHolder.PlayerToServerMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );
            _anotherTestRpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.AreEqual(2, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(2, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);
            var receivedFirstRpcMethodId = _elympicsBase.RpcMessagesToInvoke[0].Messages[0].MethodId;
            var receivedSecondRpcMethodId = _elympicsBase.RpcMessagesToInvoke[0].Messages[1].MethodId;
            Assert.AreEqual(firstRpcMethod, _elympicsBehaviour.RpcMethods[receivedFirstRpcMethodId]);
            Assert.AreEqual(secondRpcMethod, _elympicsBehaviour.RpcMethods[receivedSecondRpcMethodId]);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodLastCallArguments.HasValue);
            var actualArgs = _testRpcHolder.PlayerToServerMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
            Assert.IsTrue(_anotherTestRpcHolder.PlayerToServerMethodCalled);
        }

        [Test]
        public void MultipleServerToPlayersRpcsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _testRpcHolder.ElympicsBehaviour.ElympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);
            var expectedArgs = (true, byte.MaxValue, sbyte.MaxValue, ushort.MaxValue, short.MaxValue, uint.MaxValue,
                int.MaxValue, ulong.MaxValue, long.MaxValue, float.MaxValue, double.MaxValue, char.MaxValue, "Some test string");
            var firstRpcMethod = new RpcMethod(typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethodWithArgs)), _testRpcHolder);
            var secondRpcMethod = new RpcMethod(typeof(AnotherTestRpcHolder).GetMethod(nameof(AnotherTestRpcHolder.ServerToPlayersMethod)), _anotherTestRpcHolder);

            _testRpcHolder.ServerToPlayersMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );
            _anotherTestRpcHolder.ServerToPlayersMethod();

            Assert.IsFalse(_testRpcHolder.ServerToPlayersMethodCalled);
            Assert.AreEqual(2, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(2, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);
            var receivedFirstRpcMethodId = _elympicsBase.RpcMessagesToInvoke[0].Messages[0].MethodId;
            var receivedSecondRpcMethodId = _elympicsBase.RpcMessagesToInvoke[0].Messages[1].MethodId;
            Assert.AreEqual(firstRpcMethod, _elympicsBehaviour.RpcMethods[receivedFirstRpcMethodId]);
            Assert.AreEqual(secondRpcMethod, _elympicsBehaviour.RpcMethods[receivedSecondRpcMethodId]);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_testRpcHolder.ServerToPlayersMethodLastCallArguments.HasValue);
            var actualArgs = _testRpcHolder.ServerToPlayersMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
            Assert.IsTrue(_anotherTestRpcHolder.ServerToPlayersMethodCalled);
        }
    }
}
