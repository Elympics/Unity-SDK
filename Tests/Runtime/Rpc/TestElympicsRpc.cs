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
        private TestRpcHolder _testRpcHolder;

        [OneTimeSetUp]
        public void PrepareScene()
        {
            _elympicsObject = new GameObject("Elympics Systems", typeof(ElympicsBaseTest),
                typeof(ElympicsBehavioursManager), typeof(ElympicsFactory));
            _rpcHolderObject = new GameObject("RPC Holder", typeof(ElympicsBehaviour), typeof(TestRpcHolder));

            Assert.NotNull(_elympicsBase = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_testRpcHolder = _rpcHolderObject.GetComponent<TestRpcHolder>());
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
        public void AllRpcMethodsShouldBeCorrectlyRegisteredInRpcMethodMap()
        {
            var allMethodInfos = new[]
            {
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingServerToPlayers)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongPlayerToServer)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingPlayerToServer)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongServerToPlayers)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethod)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethodWithArgs)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethod)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethodWithArgs)),
                _testRpcHolder.PlayerToServerMethodPrivateInfo,
            };
            var expectedRpcMethods = allMethodInfos.Select(methodInfo => new RpcMethod(methodInfo, _testRpcHolder)).ToArray();

            foreach (var expectedRpcMethod in expectedRpcMethods)
                Assert.DoesNotThrow(() => _elympicsBehaviour.RpcMethods.GetIdOf(expectedRpcMethod));
        }

        [Test]
        public void RpcMethodMapShouldBeSortedAlphabetically()
        {
            var sortedMethodInfos = new[]
            {
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingPlayerToServer)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingServerToPlayers)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethod)),
                _testRpcHolder.PlayerToServerMethodPrivateInfo,
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PlayerToServerMethodWithArgs)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongPlayerToServer)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PongServerToPlayers)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethod)),
                typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.ServerToPlayersMethodWithArgs)),
            };
            var sortedRpcMethods = sortedMethodInfos.Select(methodInfo => new RpcMethod(methodInfo, _testRpcHolder)).ToArray();

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
            TestDelegate schedulingAction = () => _testRpcHolder.ServerToPlayersMethod();

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
                ElympicsRpcDirection.PlayerToServer => _testRpcHolder.PlayerToServerMethod,
                ElympicsRpcDirection.ServerToPlayers => _testRpcHolder.ServerToPlayersMethod,
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

            _testRpcHolder.PlayerToServerMethod();

            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void PlayerToServerRpcScheduledOnLocalPlayerWithBotsShouldBeInvokedWithoutAddingToQueues()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);

            _testRpcHolder.PlayerToServerMethod();

            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void ServerToPlayersRpcScheduledOnLocalPlayerWithBotsShouldBeInvokedWithoutAddingToQueues()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);

            _testRpcHolder.ServerToPlayersMethod();

            Assert.IsTrue(_testRpcHolder.ServerToPlayersMethodCalled);
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
            TestDelegate methodToCall = testCase.Direction switch
            {
                ElympicsRpcDirection.PlayerToServer => _testRpcHolder.PlayerToServerMethod,
                ElympicsRpcDirection.ServerToPlayers => _testRpcHolder.ServerToPlayersMethod,
                _ => throw new ArgumentOutOfRangeException(nameof(testCase.Direction)),
            };
            var rpcMethod = new RpcMethod(methodToCall.Method, _testRpcHolder);
            var expectedMethodId = _elympicsBehaviour.RpcMethods.GetIdOf(rpcMethod);

            methodToCall();

            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodCalled);

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
            _testRpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodCalled);
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

            _testRpcHolder.PlayerToServerMethod();

            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        [Test]
        public void ServerToPlayersRpcShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);

            _testRpcHolder.ServerToPlayersMethod();

            Assert.IsFalse(_testRpcHolder.ServerToPlayersMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_testRpcHolder.ServerToPlayersMethodCalled);
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

            _testRpcHolder.PlayerToServerMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );

            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodLastCallArguments.HasValue);
            var actualArgs = _testRpcHolder.PlayerToServerMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
        }

        [Test]
        public void PrivateMethodRpcShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);

            _testRpcHolder.CallPlayerToServerMethodPrivate();

            Assert.IsFalse(_testRpcHolder.PlayerToServerMethodPrivateCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_testRpcHolder.PlayerToServerMethodPrivateCalled);
        }

        [Test]
        public void ServerToPlayersRpcWithArgsShouldBeInvokedCorrectlyAfterBeingScheduledSentAndReceived()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.ElympicsUpdate;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneServer);
            var expectedArgs = (true, byte.MaxValue, sbyte.MaxValue, ushort.MaxValue, short.MaxValue, uint.MaxValue,
                int.MaxValue, ulong.MaxValue, long.MaxValue, float.MaxValue, double.MaxValue, char.MaxValue, "Some test string");

            _testRpcHolder.ServerToPlayersMethodWithArgs(expectedArgs.Item1, expectedArgs.Item2, expectedArgs.Item3,
                expectedArgs.Item4, expectedArgs.Item5, expectedArgs.Item6, expectedArgs.Item7, expectedArgs.Item8,
                expectedArgs.Item9, expectedArgs.Item10, expectedArgs.Item11, expectedArgs.Item12, expectedArgs.Item13
            );

            Assert.IsFalse(_testRpcHolder.ServerToPlayersMethodCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);

            _elympicsBase.SendQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToInvoke[0].Messages.Count);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
            Assert.IsTrue(_testRpcHolder.ServerToPlayersMethodLastCallArguments.HasValue);
            var actualArgs = _testRpcHolder.ServerToPlayersMethodLastCallArguments.Value;
            Assert.AreEqual(expectedArgs, actualArgs);
        }

        [Test]
        public void RpcOfOppositeDirectionShouldBeScheduledCorrectlyInsideCurrentlyInvokedServerToPlayersRpc()
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.RpcInvoking;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.StandaloneClient);
            var calledMethodInfo = typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingServerToPlayers));
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _testRpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_testRpcHolder.PingServerToPlayersCalled);
            Assert.IsFalse(_testRpcHolder.PongPlayerToServerCalled);
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
            var calledMethodInfo = typeof(TestRpcHolder).GetMethod(nameof(TestRpcHolder.PingPlayerToServer));
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _testRpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(_testRpcHolder.PingPlayerToServerCalled);
            Assert.IsFalse(_testRpcHolder.PongServerToPlayersCalled);
            Assert.AreEqual(1, _elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }

        public record ChainingInLocalModeTestCase(string MethodName, Func<TestRpcHolder, bool> WasPingCalled, Func<TestRpcHolder, bool> WasPongCalled);

        private static IEnumerable<ChainingInLocalModeTestCase> ChainingInLocalModeTestCases =>
            new ChainingInLocalModeTestCase[]
            {
                new(nameof(TestRpcHolder.PingServerToPlayers),
                    holder => holder.PingServerToPlayersCalled,
                    holder => holder.PongPlayerToServerCalled),
                new(nameof(TestRpcHolder.PingPlayerToServer),
                    holder => holder.PingPlayerToServerCalled,
                    holder => holder.PongServerToPlayersCalled),
            };

        [Test]
        public void RpcOfOppositeDirectionShouldBeInvokedInstantlyInsideCurrentlyInvokedRpcInLocalMode([ValueSource(nameof(ChainingInLocalModeTestCases))] ChainingInLocalModeTestCase testCase)
        {
            _elympicsBase.CurrentCallContext = ElympicsBase.CallContext.RpcInvoking;
            _elympicsBase.SetElympicsStatus(ElympicsStatus.LocalPlayerWithBots);
            var calledMethodInfo = typeof(TestRpcHolder).GetMethod(testCase.MethodName);
            var calledRpcMethod = new RpcMethod(calledMethodInfo, _testRpcHolder);
            var rpcMessage = new ElympicsRpcMessage
            {
                NetworkId = _elympicsBehaviour.NetworkId,
                MethodId = _elympicsBehaviour.RpcMethods.GetIdOf(calledRpcMethod),
                Arguments = Array.Empty<object>(),
            };
            var rpcMessageList = new ElympicsRpcMessageList { Messages = new List<ElympicsRpcMessage> { rpcMessage } };
            _elympicsBase.RpcMessagesToInvoke.Add(rpcMessageList);

            _elympicsBase.InvokeQueuedRpcMessages();

            Assert.IsTrue(testCase.WasPingCalled(_testRpcHolder));
            Assert.IsTrue(testCase.WasPongCalled(_testRpcHolder));
            Assert.Zero(_elympicsBase.RpcMessagesToSend.Messages.Count);
            Assert.Zero(_elympicsBase.RpcMessagesToInvoke.Count);
        }
    }
}
