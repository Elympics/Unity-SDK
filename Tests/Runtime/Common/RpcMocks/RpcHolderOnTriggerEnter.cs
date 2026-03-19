using Elympics.Replication;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderOnTriggerEnter : RpcHolderInContext
    {
        private GameObject _trigger;

        private void OnTriggerEnter(Collider _) => CallRpc();

        public override void Setup(ElympicsBaseTest elympicsInstance)
        {
            elympicsInstance.ElympicsBehavioursManager.InitializeInternal(elympicsInstance, ElympicsWorld.Current.MaxPlayers);
            CreateTriggerCube();
            elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();
        }

        public override void Act(ElympicsBaseTest elympicsInstance)
        {
            _trigger!.GetComponent<Rigidbody>().position = new Vector3(0, 0, 0);
            elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();
        }

        private void CreateTriggerCube()
        {
            if (_trigger != null)
                DestroyImmediate(_trigger);
            _trigger = new GameObject("Trigger cube", typeof(BoxCollider), typeof(Rigidbody));
            _trigger.GetComponent<BoxCollider>().isTrigger = true;
            var rigidbody = _trigger.GetComponent<Rigidbody>();
            Assert.NotNull(rigidbody);
            rigidbody.useGravity = false;
            rigidbody.position = new Vector3(5, 5, 5);
        }
    }
}

