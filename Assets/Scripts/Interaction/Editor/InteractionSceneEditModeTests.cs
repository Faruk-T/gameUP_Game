#if UNITY_EDITOR
using System.Linq;
using GameUP.Rules;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUP.Interaction.Editor
{
    public sealed class InteractionSceneEditModeTests
    {
        [Test]
        public void InteractionTestSceneHasWiredRuntimeAndEveryP0Target()
        {
            const string path = "Assets/Scenes/Tests_Interaction.unity";
            Scene previousScene = SceneManager.GetActiveScene();
            Scene testScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = testScene.GetRootGameObjects();
                InspectionService service = roots.SelectMany(root => root.GetComponentsInChildren<InspectionService>(true)).FirstOrDefault();
                ProductPresentation presentation = roots.SelectMany(root => root.GetComponentsInChildren<ProductPresentation>(true)).FirstOrDefault();
                WorkstationInteractionController controller = roots.SelectMany(root => root.GetComponentsInChildren<WorkstationInteractionController>(true)).FirstOrDefault();
                InteractionTestBootstrap bootstrap = roots.SelectMany(root => root.GetComponentsInChildren<InteractionTestBootstrap>(true)).FirstOrDefault();

                Assert.That(service, Is.Not.Null, "InspectionService missing from test scene.");
                Assert.That(presentation, Is.Not.Null, "ProductPresentation missing from test scene.");
                Assert.That(controller, Is.Not.Null, "WorkstationInteractionController missing from test scene.");
                Assert.That(bootstrap, Is.Not.Null, "InteractionTestBootstrap missing from test scene.");
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Any(), Is.True, "Test camera missing.");

                WorkstationAction[] configuredActions = roots
                    .SelectMany(root => root.GetComponentsInChildren<WorkstationTarget>(true))
                    .Select(target => target.Action)
                    .ToArray();
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Analyzer));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Approve));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Reject));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Paper));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Glass));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Plastic));
                Assert.That(configuredActions, Does.Contain(WorkstationAction.Contaminated));
            }
            finally
            {
                EditorSceneManager.CloseScene(testScene, true);
                if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
            }
        }
    }
}
#endif
