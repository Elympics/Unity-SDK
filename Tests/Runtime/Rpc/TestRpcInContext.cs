using System;
using System.Linq;
using System.Reflection;
using Elympics.Replication;
using Elympics.Tests.RpcMocks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [Category("RPC")]
    [TestFixture(typeof(RpcHolderInitializable))]
    [TestFixture(typeof(RpcHolderUpdatable))]
    [TestFixture(typeof(RpcHolderOnTriggerEnter))]
    [TestFixture(typeof(RpcHolderValueChanged))]
    [TestFixture(typeof(RpcHolderNoContext))]
    internal class TestRpcInContext
    {
        private readonly GameObject _elympicsObject;
        private readonly GameObject _rpcHolderObject;

        private readonly ElympicsBaseTest _elympicsInstance;
        private readonly ElympicsBehaviour _elympicsBehaviour;
        private readonly RpcHolderInContext _rpcHolder;

        private readonly TestDelegate _act;
        private readonly TestDelegate _setup;

        #region Setup and teardown

        public TestRpcInContext(Type rpcHolderType)
        {
            var elympicsComponents = new[] { typeof(ElympicsBaseTest), typeof(ElympicsFactory), typeof(ElympicsBehavioursManager), typeof(ElympicsBehaviour) };
            var rpcComponents = new[] { typeof(ElympicsBehaviour), typeof(BoxCollider), rpcHolderType };

            if (rpcHolderType == typeof(RpcHolderOnTriggerEnter))
                elympicsComponents = elympicsComponents.Append(typeof(ElympicsUnityPhysicsSimulator)).ToArray();

            _elympicsObject = new GameObject("Elympics Systems", elympicsComponents);
            _rpcHolderObject = new GameObject("RPC Holder", rpcComponents);

            Assert.NotNull(_elympicsInstance = _elympicsObject.GetComponent<ElympicsBaseTest>());
            Assert.NotNull(_elympicsBehaviour = _rpcHolderObject.GetComponent<ElympicsBehaviour>());
            Assert.NotNull(_rpcHolder = (RpcHolderInContext)_rpcHolderObject.GetComponent(rpcHolderType));

            _elympicsBehaviour.PredictableFor = ElympicsPlayer.All;
            _elympicsObject.GetComponent<ElympicsBehaviour>().PredictableFor = ElympicsPlayer.All;
            _elympicsObject.GetComponent<ElympicsBehaviour>().NetworkId = 1;
            _rpcHolderObject.GetComponent<ElympicsBehaviour>().NetworkId = 2;

            const int maxPlayers = 2;
            ElympicsWorld.Current = new ElympicsWorld(maxPlayers);

            _elympicsInstance.SetElympicsStatus(new ElympicsStatus(false, true, false));
            _elympicsInstance.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>(), _elympicsObject.GetComponent<ElympicsBehavioursManager>());

            _setup = () =>
            {
                _elympicsBehaviour.RpcMethods.Clear();
                CreateBehavioursManager(_elympicsObject, _elympicsInstance);
                _rpcHolder.Setup(_elympicsInstance);
            };
            _act = () => _rpcHolder.Act(_elympicsInstance);
        }

        [SetUp]
        public void SetupSut()
        {
            _setup();
            _elympicsInstance.SetElympicsStatus(new ElympicsStatus(false, false, false));
            _elympicsInstance.SetPermanentCallContext(ElympicsBase.CallContext.None);
            _elympicsInstance.ClearRpcQueues();
            _rpcHolder.Reset();
        }

        [OneTimeTearDown]
        public void CleanScene()
        {
            Object.Destroy(_elympicsObject);
            Object.Destroy(_rpcHolderObject);
            ElympicsWorld.Current?.Dispose();
            ElympicsWorld.Current = null;
        }

        #endregion

        public record ContextTestCase(ElympicsStatus Status, bool ShouldCallPlayerToServer, bool ShouldCallServerToPlayer);

        private static ContextTestCase[] testCases =
        {
            new(ElympicsStatus.StandaloneServer, false, true),
            new(ElympicsStatus.StandaloneClient, true, false),
            new(ElympicsStatus.StandaloneBot, true, false),
            new(ElympicsStatus.ServerWithBots, true, true),
            new(ElympicsStatus.LocalPlayerWithBots, true, true),
        };

        [Test]
        public void RpcShouldBeExecutedCorrectlyInContext([ValueSource(nameof(testCases))] ContextTestCase testCase)
        {
            _elympicsInstance.SetElympicsStatus(testCase.Status);
            _rpcHolder.ShouldCallPlayerToServerMethod = testCase.ShouldCallPlayerToServer;
            _rpcHolder.ShouldCallServerToPlayerMethod = testCase.ShouldCallServerToPlayer;

            _act();

            Assert.AreEqual(testCase.ShouldCallPlayerToServer, WasPlayerToServerRpcInvokedOrScheduled());
            Assert.AreEqual(testCase.ShouldCallServerToPlayer, WasServerToPlayersRpcInvokedOrScheduled());
        }

        #region Helpers

        private bool WasPlayerToServerRpcInvokedOrScheduled() =>
            _rpcHolder.PlayerToServerMethodCalled || WasPlayerToServerRpcScheduled();

        private bool WasServerToPlayersRpcInvokedOrScheduled() =>
            _rpcHolder.ServerToPlayersMethodCalled || WasServerToPlayersRpcScheduled();

        private bool WasPlayerToServerRpcScheduled() =>
            WasRpcScheduled(_rpcHolder.GetType().GetMethod(nameof(RpcHolderInContext.PlayerToServerMethod)));

        private bool WasServerToPlayersRpcScheduled() =>
            WasRpcScheduled(_rpcHolder.GetType().GetMethod(nameof(RpcHolderInContext.ServerToPlayersMethod)));

        private bool WasRpcScheduled(MethodInfo methodInfo)
        {
            var rpcMethod = new RpcMethod(methodInfo, _rpcHolder);
            var expectedId = _elympicsBehaviour.RpcMethods.GetIdOf(rpcMethod);
            return _elympicsInstance.RpcMessagesToSend.Messages
                .Concat(_elympicsInstance.RpcMessagesToInvoke.SelectMany(x => x.Messages))
                .Any(message => message.MethodId == expectedId);
        }

        protected static void CreateBehavioursManager(GameObject elympicsObject, ElympicsBaseTest elympicsInstance)
        {
            foreach (var behaviourManager in elympicsObject.GetComponents<ElympicsBehavioursManager>())
                Object.DestroyImmediate(behaviourManager); // OnDestroy nulls ElympicsWorld.Current
            ElympicsWorld.Current = new ElympicsWorld(2);
            var behavioursManager = elympicsObject.AddComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);
            behavioursManager.factory = factory;
            elympicsInstance.ElympicsBehavioursManager = behavioursManager;
        }

        #endregion
    }
}
