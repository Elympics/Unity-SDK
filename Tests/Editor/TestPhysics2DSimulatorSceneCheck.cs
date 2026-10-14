using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics.Editor.Tests
{
    public class TestPhysics2DSimulatorSceneCheck
    {
        private readonly List<GameObject> _roots = new();

        // DontSave objects leave the scene open in the Editor unmodified.
        private void CreateRoot(params Type[] components) =>
            _roots.Add(EditorUtility.CreateGameObjectWithHideFlags("Root", HideFlags.HideAndDontSave, components));

        [TearDown]
        public void TearDown()
        {
            foreach (var root in _roots)
                Object.DestroyImmediate(root);
            _roots.Clear();
        }

        [Test]
        public void ElympicsSceneWith2DPhysicsAndNoSimulatorIsReported()
        {
            CreateRoot(typeof(ElympicsBehavioursManager));
            CreateRoot(typeof(BoxCollider2D));

            Assert.That(Physics2DSimulatorSceneCheck.LacksPhysics2DSimulator(_roots), Is.True);
        }

        [Test]
        public void ElympicsSceneWith2DPhysicsAndSimulatorIsNotReported()
        {
            CreateRoot(typeof(ElympicsBehavioursManager));
            CreateRoot(typeof(Rigidbody2D));
            CreateRoot(typeof(ElympicsUnityPhysics2DSimulator));

            Assert.That(Physics2DSimulatorSceneCheck.LacksPhysics2DSimulator(_roots), Is.False);
        }

        [Test]
        public void ElympicsSceneWithout2DPhysicsIsNotReported()
        {
            CreateRoot(typeof(ElympicsBehavioursManager));
            CreateRoot(typeof(BoxCollider));

            Assert.That(Physics2DSimulatorSceneCheck.LacksPhysics2DSimulator(_roots), Is.False);
        }

        [Test]
        public void SceneWithoutElympicsIsNotReported()
        {
            CreateRoot(typeof(Rigidbody2D));

            Assert.That(Physics2DSimulatorSceneCheck.LacksPhysics2DSimulator(_roots), Is.False);
        }
    }
}
