#nullable enable

using System.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    [Category("Scene")]
    public class TestSceneNetworkIdAssigner
    {
        [Test]
        public void EveryAutoIdIsReassignedEvenWhenAnEarlierOneAlreadyMatches()
        {
            using var scene = new AdditiveTestScene();
            var behaviours = Enumerable.Range(0, 3).Select(i => scene.Create($"Object{i}").AddComponent<ElympicsBehaviour>()).ToArray();
            var first = NetworkIdConstants.PredefinedBehaviourCount;
            behaviours[0].NetworkId = first;
            behaviours[1].NetworkId = first;
            behaviours[2].NetworkId = ElympicsBehaviour.UndefinedNetworkId;

            SceneNetworkIdAssigner.ResetAllIds(scene.Scene);

            Assert.That(behaviours.Select(behaviour => behaviour.NetworkId), Is.EqualTo(new[] { first, first + 1, first + 2 }));
        }
    }
}
