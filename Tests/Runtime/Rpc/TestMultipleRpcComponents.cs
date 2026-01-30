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
        private RpcHolderComplex _rpcHolder;
        private RpcHolderSimple _anotherRpcHolder;

        [OneTimeSetUp]
        public void PrepareScene()
        {
            _elympicsObject = new GameObject("Elympics Systems",
                typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager),
                typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder",
                typeof(ElympicsBehaviour),
                typeof(RpcHolderComplex),
                typeof(RpcHolderSimple));

            _rpcHolderObject.GetComponent<ElympicsBehaviour>().NetworkId = 1;
            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_rpcHolder = _rpcHolderObject.GetComponent<RpcHolderComplex>());
            Assert.NotNull(_anotherRpcHolder = _rpcHolderObject.GetComponent<RpcHolderSimple>());
            Assert.NotNull(_elympicsBehaviour = _rpcHolder.ElympicsBehaviour);
            var behavioursManager = _elympicsObject.GetComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = _elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);

            _elympicsBase.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>(), behavioursManager);
            behavioursManager.factory = factory;

            behavioursManager.InitializeInternal(_elympicsBase, 100);
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

        [Test]
        public void RpcMethodMapShouldBeCorrectlyRegisteredAndSortedByComponentIndexAndByMethodNameAlphabetically()
        {
            var sortedRpcMethods = new RpcMethod[]
            {
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingPlayerToServer)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingServerToPlayers)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethod)), _rpcHolder),
                new(_rpcHolder.PlayerToServerMethodPrivateInfo, _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethodWithArgs)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongPlayerToServer)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongServerToPlayers)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethod)), _rpcHolder),
                new(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethodWithArgs)), _rpcHolder),
                new(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.PlayerToServerMethod)), _anotherRpcHolder),
                new(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.PlayerToServerMethodWithArgs)), _anotherRpcHolder),
                new(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.ServerToPlayersMethod)), _anotherRpcHolder),
                new(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.ServerToPlayersMethodWithArgs)), _anotherRpcHolder),
            };

            for (ushort methodId = 0; methodId < sortedRpcMethods.Length; methodId++)
                Assert.AreEqual(sortedRpcMethods[methodId], _elympicsBehaviour.RpcMethods[methodId]);
        }

        [Test]
        public void MultiplePlayerToServerRpcsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            using var tmpContext = _rpcHolder.ElympicsBehaviour.ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);
            var expectedArgs = (false, byte.MinValue, sbyte.MinValue, ushort.MinValue, short.MinValue, uint.MinValue,
                int.MinValue, ulong.MinValue, long.MinValue, float.MinValue, double.MinValue, char.MinValue, "");
            var firstRpcMethod = new RpcMethod(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethodWithArgs)), _rpcHolder);
            var secondRpcMethod = new RpcMethod(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.PlayerToServerMethod)), _anotherRpcHolder);

            _rpcHolder.PlayerToServerMethodWithArgs(expectedArgs.Item1,
                expectedArgs.Item2,
                expectedArgs.Item3,
                expectedArgs.Item4,
                expectedArgs.Item5,
                expectedArgs.Item6,
                expectedArgs.Item7,
                expectedArgs.Item8,
                expectedArgs.Item9,
                expectedArgs.Item10,
                expectedArgs.Item11,
                expectedArgs.Item12,
                expectedArgs.Item13
            );
            _anotherRpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_rpcHolder.PlayerToServerMethodCalled);
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
            Assert.IsTrue(_rpcHolder.PlayerToServerMethodLastCallArguments.HasValue);
            var actualArgs = _rpcHolder.PlayerToServerMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
            Assert.IsTrue(_anotherRpcHolder.PlayerToServerMethodCalled);
        }

        [Test]
        public void MultipleServerToPlayersRpcsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            using var tmpContext = _rpcHolder.ElympicsBehaviour.ElympicsBase.SetTemporaryCallContext(ElympicsBase.CallContext.ElympicsUpdate);
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);
            var expectedArgs = (true, byte.MaxValue, sbyte.MaxValue, ushort.MaxValue, short.MaxValue, uint.MaxValue,
                int.MaxValue, ulong.MaxValue, long.MaxValue, float.MaxValue, double.MaxValue, char.MaxValue, "Some test string");
            var firstRpcMethod = new RpcMethod(typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethodWithArgs)), _rpcHolder);
            var secondRpcMethod = new RpcMethod(typeof(RpcHolderSimple).GetMethod(nameof(RpcHolderSimple.ServerToPlayersMethod)), _anotherRpcHolder);

            _rpcHolder.ServerToPlayersMethodWithArgs(expectedArgs.Item1,
                expectedArgs.Item2,
                expectedArgs.Item3,
                expectedArgs.Item4,
                expectedArgs.Item5,
                expectedArgs.Item6,
                expectedArgs.Item7,
                expectedArgs.Item8,
                expectedArgs.Item9,
                expectedArgs.Item10,
                expectedArgs.Item11,
                expectedArgs.Item12,
                expectedArgs.Item13
            );
            _anotherRpcHolder.ServerToPlayersMethod();

            Assert.IsFalse(_rpcHolder.ServerToPlayersMethodCalled);
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
            Assert.IsTrue(_rpcHolder.ServerToPlayersMethodLastCallArguments.HasValue);
            var actualArgs = _rpcHolder.ServerToPlayersMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
            Assert.IsTrue(_anotherRpcHolder.ServerToPlayersMethodCalled);
        }
    }
}
