using System;
using System.Linq;
using System.Reflection;
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
    internal class TestRpcInContext
    {
        private readonly GameObject _elympicsObject;
        private readonly GameObject _rpcHolderObject;

        private readonly ElympicsBaseTest _elympicsInstance;
        private readonly ElympicsBehaviour _elympicsBehaviour;
        private readonly RpcHolderInContext _rpcHolder;

        private readonly TestDelegate _act;
        private readonly TestDelegate _setup;

        private GameObject _trigger;

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

            _elympicsInstance.InitializeInternal(ScriptableObject.CreateInstance<ElympicsGameConfig>(), _elympicsObject.GetComponent<ElympicsBehavioursManager>());

            _act = _rpcHolder switch
            {
                RpcHolderInitializable => RunInitialization,
                RpcHolderUpdatable => RunUpdate,
                RpcHolderOnTriggerEnter => MoveTriggerAndRunUpdate,
                _ => throw new ArgumentOutOfRangeException(nameof(rpcHolderType)),
            };

            _setup = _rpcHolder switch
            {
                RpcHolderInitializable => SetupBeforeInitialization,
                RpcHolderUpdatable => SetupBeforeUpdate,
                RpcHolderOnTriggerEnter => SetupBeforePhysicsUpdate,
                _ => throw new ArgumentOutOfRangeException(nameof(rpcHolderType)),
            };
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

        #region Setup methods

        private void SetupBeforeInitialization()
        {
            _elympicsBehaviour.RpcMethods.Clear();
            CreateBehavioursManager();
        }

        private void SetupBeforeUpdate()
        {
            SetupBeforeInitialization();
            _elympicsInstance.ElympicsBehavioursManager.InitializeInternal(_elympicsInstance, 100);
        }

        private void SetupBeforePhysicsUpdate()
        {
            SetupBeforeUpdate();
            CreateTriggerCube();
            _elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();
        }

        private void CreateBehavioursManager()
        {
            foreach (var behaviourManager in _elympicsObject.GetComponents<ElympicsBehavioursManager>())
                Object.DestroyImmediate(behaviourManager);
            var behavioursManager = _elympicsObject.AddComponent<ElympicsBehavioursManager>();
            Assert.NotNull(behavioursManager);
            var factory = _elympicsObject.GetComponent<ElympicsFactory>();
            Assert.NotNull(factory);
            behavioursManager.factory = factory;
            _elympicsInstance.ElympicsBehavioursManager = behavioursManager;
        }

        private void CreateTriggerCube()
        {
            if (_trigger != null)
                Object.DestroyImmediate(_trigger);
            _trigger = new GameObject("Trigger cube", typeof(BoxCollider), typeof(Rigidbody));
            _trigger.GetComponent<BoxCollider>().isTrigger = true;
            var rigidbody = _trigger.GetComponent<Rigidbody>();
            Assert.NotNull(rigidbody);
            rigidbody.useGravity = false;
            rigidbody.position = new Vector3(5, 5, 5);
        }

        #endregion Setup methods

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
            return _elympicsInstance.RpcMessagesToSend.Messages.Any(message => message.MethodId == expectedId);
        }

        #region Act methods

        private void RunInitialization() =>
            _elympicsInstance.ElympicsBehavioursManager.InitializeInternal(_elympicsInstance, 100);

        private void RunUpdate() =>
            _elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();

        private void MoveTriggerAndRunUpdate()
        {
            _trigger!.GetComponent<Rigidbody>().position = new Vector3(0, 0, 0);
            _elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();
        }

        #endregion Act methods

        [OneTimeTearDown]
        public void CleanScene()
        {
            Object.Destroy(_elympicsObject);
            Object.Destroy(_rpcHolderObject);
        }
    }
}
