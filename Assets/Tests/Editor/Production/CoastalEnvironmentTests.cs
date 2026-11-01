using System;
using System.Linq;
using EPOOutline;
using GameUp.Editor;
using GameUp.Gameplay;
using GameUP.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace GameUp.Production.Tests
{
    public sealed class CoastalEnvironmentTests
    {
        private Scene scene;
        private Transform[] all;
        [SetUp] public void Open()
        {
            scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath,OpenSceneMode.Additive);
            all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        }
        [TearDown] public void Close(){if(scene.IsValid())EditorSceneManager.CloseScene(scene,true);}

        [Test] public void FiveArtistTurtlesHaveSavedAnimationAndFaceTheirTravelDirection()
        {
            var turtles=all.Select(t=>t.GetComponent<ShorelineTurtle>()).Where(t=>t).ToArray();
            Assert.That(turtles.Length,Is.EqualTo(5));
            foreach(var turtle in turtles)
            {
                var animator=turtle.GetComponentInChildren<Animator>();
                Assert.That(animator,Is.Not.Null);Assert.That(animator.runtimeAnimatorController,Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController.animationClips.Any(c=>c.isLooping),Is.True);
                Assert.That(animator.applyRootMotion,Is.False);
                var bones=turtle.GetComponentsInChildren<Transform>();
                var forward=Vector3.ProjectOnPlane(bones.Single(t=>t.name=="head").position-bones.Single(t=>t.name=="tail1").position,Vector3.up).normalized;
                Assert.That(Vector3.Dot(forward,turtle.transform.forward),Is.GreaterThan(.98f));
                Assert.That(Vector3.Dot(turtle.transform.GetChild(0).up,turtle.transform.up),Is.GreaterThan(.99f),"Facing correction must not roll the animal upside down.");
                Assert.That(turtle.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>!r.gameObject.isStatic),Is.True);
                var so=new SerializedObject(turtle);
                var entry=so.FindProperty("waterEntry").vector3Value;var center=so.FindProperty("swimCenter").vector3Value;
                Assert.That(center.z-Vector3.Distance(entry,center),Is.GreaterThan(28),"Swim loop must stay seaward of the shoreline.");
            }
        }
        [Test] public void SavedOpeningRouteAvoidsWallsAndEndsAtTheWorkstationCamera()
        {
            var intro=all.Select(t=>t.GetComponent<IntroCinematic>()).Single(c=>c);var so=new SerializedObject(intro);
            var cameraObject=(GameObject)so.FindProperty("cinematicCamera").objectReferenceValue;
            var gameplay=(GameObject)so.FindProperty("gameplayCamera").objectReferenceValue;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Data/Cinematic/ArrivalCamera.anim");
            Assert.That(intro.GetComponent<PlayableDirector>().playableAsset,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(72).Within(.01));
            Vector3 previous=Vector3.zero;
            for(float t=19;t<72;t+=.2f)
            {
                clip.SampleAnimation(cameraObject,t);Physics.SyncTransforms();
                var hits=Physics.OverlapSphere(cameraObject.transform.position,.12f,1,QueryTriggerInteraction.Ignore);
                Assert.That(hits,Is.Empty,"Camera intersects geometry at "+t+": "+string.Join(",",hits.Select(h=>h.name)));
                var position=cameraObject.transform.position;
                if(t>19)
                {
                    var delta=position-previous;
                    if(delta.sqrMagnitude>.000001f)
                        Assert.That(Physics.SphereCast(previous,.12f,delta.normalized,out var hit,delta.magnitude,1,QueryTriggerInteraction.Ignore),Is.False,"Camera crosses geometry at "+t+": "+(hit.collider?hit.collider.name:""));
                }
                previous=position;
            }
            clip.SampleAnimation(cameraObject,72);
            Assert.That(Vector3.Distance(cameraObject.transform.position,gameplay.transform.position),Is.LessThan(.005));
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation,gameplay.transform.rotation),Is.LessThan(.1));
        }
        [Test] public void ThinContoursAreVisibleOnlyAndEveryInteractionProductIsConfigured()
        {
            foreach(var camera in all.Select(t=>t.GetComponent<Camera>()).Where(c=>c))
            {
                var outliner=camera.GetComponent<Outliner>();Assert.That(outliner,Is.Not.Null);
                var data=camera.GetUniversalAdditionalCameraData();
                Assert.That(data.requiresDepthOption,Is.EqualTo(CameraOverrideOption.On));
                Assert.That(data.requiresColorOption,Is.EqualTo(CameraOverrideOption.On));
                Assert.That(outliner.BlurIterations,Is.Zero);Assert.That(outliner.DilateIterations,Is.EqualTo(1));
                Assert.That(outliner.PrimaryBufferSizeMode,Is.EqualTo(BufferSizeMode.Native));
            }
            foreach(var target in all.Select(t=>t.GetComponent<WorkstationTarget>()).Where(t=>t&&t.Action==WorkstationAction.Product))
            {
                var outline=target.GetComponent<Outlinable>();Assert.That(outline,Is.Not.Null);
                Assert.That(outline.RenderStyle,Is.EqualTo(RenderStyle.FrontBack));Assert.That(outline.BackParameters.Enabled,Is.False);
                Assert.That(outline.FrontParameters.Color.maxColorComponent,Is.LessThan(.03f));
            }
            foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
                Assert.That(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path).rendererFeatures.Count(f=>f is URPOutlineFeature),Is.EqualTo(1));
        }
        [Test] public void OpeningQuoteIsReadableAndWaterUsesTheImportedAssetResources()
        {
            var text=all.Single(t=>t.name=="QuoteText").GetComponent<Text>();
            Assert.That(text.text,Does.StartWith("İklim krizi yalnızca çevresel değil"));Assert.That(text.text,Does.EndWith("gerektirmektedir."));
            Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height));
            Assert.That(all.Single(t=>t.name=="BlackBackground").GetComponent<Image>().color,Is.EqualTo(Color.black));
            var sea=all.Single(t=>t.name=="MediterraneanSea");var material=sea.GetComponent<Renderer>().sharedMaterial;
            Assert.That(material.shader.name,Is.EqualTo("GameUP/COP31/StylizedWaterURP"));
            Assert.That(AssetDatabase.GetAssetPath(sea.GetComponent<MeshFilter>().sharedMesh),Does.StartWith("Assets/StylizedWaterShader/"));
            Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_FoamTexture")),Does.StartWith("Assets/StylizedWaterShader/"));
            Assert.That(sea.GetComponents<MonoBehaviour>(),Is.Empty,"No legacy reflection camera or Built-in water component.");
        }
    }
}
