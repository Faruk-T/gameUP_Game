using System.Linq;
using GameUP.Rules;
using GameUp.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameUP.Interaction.Editor
{
    public sealed class InteractionSceneEditModeTests
    {
        [Test]
        public void GameSceneHasOneProductionCompositionAndEveryAction()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.That(all.Count(t => t.GetComponent<GameSessionController>()), Is.EqualTo(1));
                Assert.That(all.Count(t => t.GetComponent<ProductionInspectionBackend>()), Is.EqualTo(1));
                Assert.That(all.Count(t => t.GetComponent<ProductPresentation>()), Is.EqualTo(1));
                Assert.That(all.Count(t => t.GetComponent<WorkstationInteractionController>()), Is.EqualTo(1));
                var actions = all.Select(t => t.GetComponent<WorkstationTarget>()).Where(t => t).Select(t => t.Action).ToArray();
                foreach (WorkstationAction action in System.Enum.GetValues(typeof(WorkstationAction)))
                    Assert.That(actions, Does.Contain(action));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
