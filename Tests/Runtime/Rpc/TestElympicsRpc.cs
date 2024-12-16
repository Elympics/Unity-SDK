using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Tests.RpcMocks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [TestFixture]
    [Category("RPC")]
    internal class TestElympicsRpc
    {
        private GameObject _elympicsObject;
        private GameObject _rpcHolderObject;

        private ElympicsBaseTest _elympicsBase;
        private ElympicsBehaviour _elympicsBehaviour;
        private RpcHolderComplex _rpcHolder;

        [OneTimeSetUp]
        public void PrepareScene()
        {
            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(RpcHolderComplex));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_rpcHolder = _rpcHolderObject.GetComponent<RpcHolderComplex>());
            Assert.NotNull(_elympicsBehaviour = _rpcHolder.ElympicsBehaviour);
            var behavioursManager = _elympicsObject.GetComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = _elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);

            _elympicsBase.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>(), behavioursManager);
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
            _rpcHolder.Reset();
        }

        [OneTimeTearDown]
        public void CleanScene()
        {
            Object.Destroy(_elympicsObject);
            Object.Destroy(_rpcHolderObject);
        }

        [Test]
        public void AllRpcMethodsShouldBeCorrectlyRegisteredInRpcMethodMap()
        {
            var allMethodInfos = new[]
            {
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingServerToPlayers)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongPlayerToServer)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingPlayerToServer)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongServerToPlayers)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethod)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethodWithArgs)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethod)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethodWithArgs)),
                _rpcHolder.PlayerToServerMethodPrivateInfo,
            };
            var expectedRpcMethods = allMethodInfos.Select(methodInfo => new RpcMethod(methodInfo, _rpcHolder)).ToArray();

            foreach (var expectedRpcMethod in expectedRpcMethods)
                Assert.DoesNotThrow(() => _elympicsBehaviour.RpcMethods.GetIdOf(expectedRpcMethod));
        }

        [Test]
        public void RpcMethodMapShouldBeSortedAlphabetically()
        {
            var sortedMethodInfos = new[]
            {
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingPlayerToServer)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingServerToPlayers)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethod)),
                _rpcHolder.PlayerToServerMethodPrivateInfo,
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PlayerToServerMethodWithArgs)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongPlayerToServer)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PongServerToPlayers)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethod)),
                typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.ServerToPlayersMethodWithArgs)),
            };
            var sortedRpcMethods = sortedMethodInfos.Select(methodInfo => new RpcMethod(methodInfo, _rpcHolder)).ToArray();

            for (ushort methodId = 0; methodId < sortedRpcMethods.Length; methodId++)
                Assert.AreEqual(sortedRpcMethods[methodId], _elympicsBehaviour.RpcMethods[methodId]);
        }

        [Test]
        public void SchedulingRpcShouldThrowIfCalledInWrongContext([Values] ElympicsBase.CallContext context)
        {
            var allowedContexts = new[]
            {
                ElympicsBase.CallContext.ElympicsUpdate,
                ElympicsBase.CallContext.Initialize,
                ElympicsBase.CallContext.RpcInvoking,
            };
            TestDelegate schedulingAction = () => _rpcHolder.ServerToPlayersMethod();

            _elympicsBase.CurrentCallContext = context;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);

            if (allowedContexts.Contains(context))
                Assert.DoesNotThrow(schedulingAction);
            else
                _ = Assert.Throws<ElympicsException>(schedulingAction);
        }

        public record DirectionTestCase(ElympicsStatus Status, ElympicsRpcDirection Direction, bool ShouldThrow);

        private static IEnumerable<DirectionTestCase> DirectionTestCases =>
            new DirectionTestCase[]
            {
                new(ElympicsStatus.StandaloneClient, ElympicsRpcDirection.ServerToPlayers, true),
                new(ElympicsStatus.StandaloneClient, ElympicsRpcDirection.PlayerToServer, false),
                new(ElympicsStatus.StandaloneServer, ElympicsRpcDirection.ServerToPlayers, false),
                new(ElympicsStatus.StandaloneServer, ElympicsRpcDirection.PlayerToServer, true),
                new(ElympicsStatus.StandaloneBot, ElympicsRpcDirection.ServerToPlayers, true),
                new(ElympicsStatus.StandaloneBot, ElympicsRpcDirection.PlayerToServer, false),
                new(ElympicsStatus.ServerWithBots, ElympicsRpcDirection.ServerToPlayers, false),
                new(ElympicsStatus.ServerWithBots, ElympicsRpcDirection.PlayerToServer, false),
                new(ElympicsStatus.LocalPlayerWithBots, ElympicsRpcDirection.ServerToPlayers, false),
                new(ElympicsStatus.LocalPlayerWithBots, ElympicsRpcDirection.PlayerToServer, false),
            };

        [Test]
        public void SchedulingRpcShouldThrowIfCalledWithInvalidDirection([ValueSource(nameof(DirectionTestCases))] DirectionTestCase testCase)
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(testCase.Status);
            TestDelegate methodToCall = testCase.Direction switch
            {
                ElympicsRpcDirection.PlayerToServer => _rpcHolder.PlayerToServerMethod,
                ElympicsRpcDirection.ServerToPlayers => _rpcHolder.ServerToPlayersMethod,
                _ => throw new ArgumentOutOfRangeException(nameof(testCase.Direction)),
            };

            if (testCase.ShouldThrow)
                _ = Assert.Throws<RpcDirectionMismatchException>(methodToCall);
            else
                Assert.DoesNotThrow(methodToCall);
        }

        [Test]
        public void PlayerToServerRpcScheduledOnServerWithBotsShouldBeInvokedWithoutAddingToQueues()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.ServerWithBots);

            _rpcHolder.PlayerToServerMethod();

            Assert.IsTrue(_rpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void PlayerToServerRpcScheduledOnLocalPlayerWithBotsShouldBeInvokedWithoutAddingToQueues()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);

            _rpcHolder.PlayerToServerMethod();

            Assert.IsTrue(_rpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void ServerToPlayersRpcScheduledOnLocalPlayerWithBotsShouldBeInvokedWithoutAddingToQueues()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);

            _rpcHolder.ServerToPlayersMethod();

            Assert.IsTrue(_rpcHolder.ServerToPlayersMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        public record SendingQueueTestCase(ElympicsStatus Status, ElympicsRpcDirection Direction);

        private static IEnumerable<SendingQueueTestCase> SendingQueueTestCases =>
            new SendingQueueTestCase[]
            {
                new(ElympicsStatus.StandaloneClient, ElympicsRpcDirection.PlayerToServer),
                new(ElympicsStatus.StandaloneServer, ElympicsRpcDirection.ServerToPlayers),
                new(ElympicsStatus.StandaloneBot, ElympicsRpcDirection.PlayerToServer),
                new(ElympicsStatus.ServerWithBots, ElympicsRpcDirection.ServerToPlayers),
            };

        [Test]
        public void ScheduledRpcShouldBeCorrectlyQueuedToBeSentWithoutBeingInvoked([ValueSource(nameof(SendingQueueTestCases))] SendingQueueTestCase testCase)
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(testCase.Status);
            var methodInfo = testCase.Direction switch
            {
                ElympicsRpcDirection.PlayerToServer => _rpcHolder.GetType().GetMethod(nameof(_rpcHolder.PlayerToServerMethod)),
                ElympicsRpcDirection.ServerToPlayers => _rpcHolder.GetType().GetMethod(nameof(_rpcHolder.ServerToPlayersMethod)),
                _ => throw new ArgumentOutOfRangeException(nameof(testCase.Direction)),
            };
            var rpcMethod = new RpcMethod(methodInfo, _rpcHolder);
            var expectedMethodId = _elympicsBehaviour.RpcMethods.GetIdOf(rpcMethod);

            _ = methodInfo!.Invoke(_rpcHolder, Array.Empty<object>());

            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsFalse(_rpcHolder.PlayerToServerMethodCalled);

            var queuedRpc = _elympicsBase.RpcMessagesToSend.Messages[0];
            Assert.AreEqual(expectedMethodId, queuedRpc.MethodId);
            Assert.AreEqual(_elympicsBehaviour.NetworkId, queuedRpc.NetworkId);
            Assert.Zero(queuedRpc.Arguments.Length);
        }

        [Test]
        public void PlayerToServerRpcShouldNotBeScheduledDuringReconciliation()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);

            _elympicsBehaviour.OnPreReconcile();
            _rpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_rpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void ServerToPlayersRpcShouldNotBeInvokedNorDequeuedDuringReconciliation() =>
            Assert.Pass($"RPCs from {nameof(ElympicsBase.RpcMessagesToInvoke)} are neither invoked nor dequeued "
                + $"during reconciliation as {nameof(ElympicsClient)} never calls "
                + $"{nameof(ElympicsBase.InvokeQueuedRpcMessages)} in reconciliation context.");

        [Test]
        public void PlayerToServerRpcShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);

            _rpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_rpcHolder.PlayerToServerMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_rpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void ServerToPlayersRpcShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);

            _rpcHolder.ServerToPlayersMethod();

            Assert.IsFalse(_rpcHolder.ServerToPlayersMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_rpcHolder.ServerToPlayersMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void PlayerToServerRpcWithArgsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);
            var expectedArgs = (false, byte.MinValue, sbyte.MinValue, ushort.MinValue, short.MinValue, uint.MinValue,
                int.MinValue, ulong.MinValue, long.MinValue, float.MinValue, double.MinValue, char.MinValue, "");

            _rpcHolder.PlayerToServerMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );

            Assert.IsFalse(_rpcHolder.PlayerToServerMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_rpcHolder.PlayerToServerMethodLastCallArguments.HasValue);
            var actualArgs = _rpcHolder.PlayerToServerMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
        }

        [Test]
        public void PrivateMethodRpcShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);

            _rpcHolder.CallPlayerToServerMethodPrivate();

            Assert.IsFalse(_rpcHolder.PlayerToServerMethodPrivateCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_rpcHolder.PlayerToServerMethodPrivateCalled);
        }

        [Test]
        public void ServerToPlayersRpcWithArgsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);
            var expectedArgs = (true, byte.MaxValue, sbyte.MaxValue, ushort.MaxValue, short.MaxValue, uint.MaxValue,
                int.MaxValue, ulong.MaxValue, long.MaxValue, float.MaxValue, double.MaxValue, char.MaxValue, "Some test string");

            _rpcHolder.ServerToPlayersMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );

            Assert.IsFalse(_rpcHolder.ServerToPlayersMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_rpcHolder.ServerToPlayersMethodLastCallArguments.HasValue);
            var actualArgs = _rpcHolder.ServerToPlayersMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
        }

        [Test]
        public void RpcOfOppositeDirectionShouldBeScheduledCorrectlyInsideCurrentlyInvokedServerToPlayersRpc()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.RpcInvoking;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);
            var calledMethodInfo = typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingServerToPlayers));
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _rpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_rpcHolder.PingServerToPlayersCalled);
            Assert.IsFalse(_rpcHolder.PongPlayerToServerCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        private static ElympicsStatus[] ServerTypes =>
            new[]
            {
                ElympicsStatus.StandaloneServer,
                ElympicsStatus.ServerWithBots,
            };

        [Test]
        public void RpcOfOppositeDirectionShouldBeScheduledCorrectlyInsideCurrentlyInvokedPlayerToServerRpc([ValueSource(nameof(ServerTypes))] ElympicsStatus serverType)
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.RpcInvoking;
            _elympicsBase.SetElympicsStatus(serverType);
            var calledMethodInfo = typeof(RpcHolderComplex).GetMethod(nameof(RpcHolderComplex.PingPlayerToServer));
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _rpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_rpcHolder.PingPlayerToServerCalled);
            Assert.IsFalse(_rpcHolder.PongServerToPlayersCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        public record ChainingInLocalModeTestCase(string MethodName, Func<RpcHolderComplex, bool> WasPingCalled, Func<RpcHolderComplex, bool> WasPongCalled);

        private static IEnumerable<ChainingInLocalModeTestCase> ChainingInLocalModeTestCases =>
            new ChainingInLocalModeTestCase[]
            {
                new(nameof(RpcHolderComplex.PingServerToPlayers),
                    holder => holder.PingServerToPlayersCalled,
                    holder => holder.PongPlayerToServerCalled),
                new(nameof(RpcHolderComplex.PingPlayerToServer),
                    holder => holder.PingPlayerToServerCalled,
                    holder => holder.PongServerToPlayersCalled),
            };

        [Test]
        public void RpcOfOppositeDirectionShouldBeInvokedInstantlyInsideCurrentlyInvokedRpcInLocalMode([ValueSource(nameof(ChainingInLocalModeTestCases))] ChainingInLocalModeTestCase testCase)
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.RpcInvoking;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);
            var calledMethodInfo = typeof(RpcHolderComplex).GetMethod(testCase.MethodName);
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _rpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(testCase.WasPingCalled(_rpcHolder));
            Assert.IsTrue(testCase.WasPongCalled(_rpcHolder));
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }
    }
}
