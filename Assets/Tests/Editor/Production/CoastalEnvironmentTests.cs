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
                var center=so.FindProperty("swimCenter").vector3Value;
                Assert.That(center.z-so.FindProperty("swimRadiusZ").floatValue,Is.GreaterThan(28),"Swim loop must stay seaward of the shoreline.");
            }
        }
        [Test] public void SavedOpeningRouteAvoidsWallsAndEndsAtTheWorkstationCamera()
        {
            var intro=all.Select(t=>t.GetComponent<IntroCinematic>()).Single(c=>c);var so=new SerializedObject(intro);
            var cameraObject=(GameObject)so.FindProperty("cinematicCamera").objectReferenceValue;
            var gameplay=(GameObject)so.FindProperty("gameplayCamera").objectReferenceValue;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Data/Cinematic/ArrivalCamera.anim");
            Assert.That(intro.GetComponent<PlayableDirector>().playableAsset,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(OpeningTiming.End).Within(.01));
            Vector3 previous=Vector3.zero;
            for(float t=19;t<OpeningTiming.End;t+=.2f)
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
            clip.SampleAnimation(cameraObject,OpeningTiming.End);
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
        [Test] public void ArtistEnvironmentAndPackageSignsAreActualSceneGeometry()
        {
            var meshes=all.Select(t=>t.GetComponent<MeshFilter>()).Where(f=>f).Select(f=>f.sharedMesh).ToArray();
            var artist=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Mesh/Environment.fbx").GetComponentsInChildren<MeshFilter>();
            Assert.That(artist.Length,Is.EqualTo(16));
            foreach(var part in artist)Assert.That(meshes,Does.Contain(part.sharedMesh),"Artist object absent: "+part.name);
            foreach(var name in new[]{"HabitatSign","InspectionDirection","FacilityName","FacilityCOP31","BeltInstructions"})
            {
                var sign=all.Single(t=>t.name==name);
                Assert.That(sign.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/PolygonOffice/",StringComparison.Ordinal)),Is.True,name);
                foreach(var text in sign.GetComponentsInChildren<Text>())Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),name+" lettering overflow");
            }
            foreach(var name in new[]{"FoodSecurityPoster","ClimateActionPoster","WasteSortingPoster","WaterFoodClimatePoster","ColdChainPoster"})
                Assert.That(all.Single(t=>t.name==name).GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SM_Prop_Poster_01")),Is.True,name+" must use the package poster prop");
            Assert.That(all.Any(t=>t.name=="TaurusMountains"||t.name=="DistantTaurusRidge"),Is.False);
            var terrain=all.Single(t=>t.name=="TaurusTerrain").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(terrain.vertexCount,Is.GreaterThan(2000));Assert.That(terrain.bounds.size.z,Is.GreaterThan(500));Assert.That(terrain.bounds.size.y,Is.GreaterThan(35));Assert.That(terrain.bounds.min.y,Is.LessThan(0));
            Assert.That(terrain.vertices.All(v=>!float.IsNaN(v.y)&&!float.IsInfinity(v.y)),Is.True);
        }
        [Test] public void SwimmingIsCurvedAndVisibleBeforeLogoThenFireworksThenWalk()
        {
            var intro=all.Select(t=>t.GetComponent<IntroCinematic>()).Single(c=>c);
            var source=new SerializedObject(intro);var cameraObject=(GameObject)source.FindProperty("cinematicCamera").objectReferenceValue;cameraObject.SetActive(true);
            var director=intro.GetComponent<PlayableDirector>();director.RebuildGraph();director.Play();
            var logo=all.Single(t=>t.name=="COP31RisingLogo");var group=logo.GetComponent<CanvasGroup>();
            var turtles=all.Select(t=>t.GetComponent<ShorelineTurtle>()).Where(t=>t).ToArray();
            intro.SampleAt(21);Assert.That(turtles.Any(t=>!t.IsSwimming),Is.True);
            intro.SampleAt(OpeningTiming.SwimReveal);Assert.That(turtles.All(t=>t.IsSwimming),Is.True);Assert.That(group.alpha,Is.LessThan(.01));
            intro.SampleAt(OpeningTiming.LogoStart);Assert.That(turtles.All(t=>t.IsCruising),Is.True);float initialY=logo.localPosition.y;
            var direction=turtles[0].transform.forward;intro.SampleAt(OpeningTiming.LogoRaised);
            Assert.That(Vector3.Angle(direction,turtles[0].transform.forward),Is.GreaterThan(35));
            Assert.That(logo.localPosition.y-initialY,Is.GreaterThan(15));Assert.That(group.alpha,Is.GreaterThan(.99));
            var viewport=cameraObject.GetComponent<Camera>().WorldToViewportPoint(logo.position);Assert.That(viewport.z,Is.GreaterThan(0));Assert.That(viewport.x,Is.InRange(.1f,.9f));Assert.That(viewport.y,Is.InRange(.1f,.9f));
            intro.SampleAt(46);
            var fireworks=all.Where(t=>t.name.StartsWith("COP31Firework_",StringComparison.Ordinal)).Select(t=>t.GetComponent<ParticleSystem>()).ToArray();
            Assert.That(fireworks.Length,Is.EqualTo(5));Assert.That(fireworks.Where(p=>p.gameObject.activeInHierarchy).Sum(p=>p.particleCount),Is.GreaterThan(0));
            intro.SampleAt(OpeningTiming.WalkStart+.2f);Assert.That(group.alpha,Is.LessThan(.01));Assert.That(fireworks.All(p=>!p.gameObject.activeInHierarchy),Is.True,"No frozen fireworks after the celebration.");
            Assert.That(cameraObject.transform.position.z,Is.GreaterThan(20),"Corridor walk must follow the logo reveal.");
            intro.SampleAt(76);Assert.That(cameraObject.transform.position.x,Is.LessThan(-12));Assert.That(cameraObject.transform.position.z,Is.InRange(-5,-3));
            director.Stop();
        }
        [Test] public void SwimLoopHasDepthBankingAndCannotReturnOntoTheBeach()
        {
            foreach(var turtle in all.Select(t=>t.GetComponent<ShorelineTurtle>()).Where(t=>t))
            {
                float low=100,high=-100;Vector3 first=Vector3.zero;float maxTurn=0;
                for(int i=0;i<81;i++)
                {
                    turtle.SamplePose(40+i*.25f);var p=turtle.transform.localPosition;
                    Assert.That(p.z,Is.GreaterThan(28));low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);
                    if(i==0)first=turtle.transform.forward;else maxTurn=Mathf.Max(maxTurn,Vector3.Angle(first,turtle.transform.forward));
                }
                Assert.That(high-low,Is.GreaterThan(.1));Assert.That(maxTurn,Is.GreaterThan(90));
            }
        }
    }
}
