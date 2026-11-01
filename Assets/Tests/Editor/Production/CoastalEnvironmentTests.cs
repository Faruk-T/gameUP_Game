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
            all=scene.GetRootGameObjects().Where(r=>!r.CompareTag("EditorOnly")).SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        }
        [TearDown] public void Close(){if(scene.IsValid())EditorSceneManager.CloseScene(scene,true);}
        private IntroCinematic BeginOpening()
        {
            var intro=all.Select(t=>t.GetComponent<IntroCinematic>()).Single(c=>c);var source=new SerializedObject(intro);
            ((GameObject)source.FindProperty("cinematicCamera").objectReferenceValue).SetActive(true);
            ((CanvasGroup)source.FindProperty("overlay").objectReferenceValue).gameObject.SetActive(true);
            var director=intro.GetComponent<PlayableDirector>();director.RebuildGraph();director.Play();return intro;
        }

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
            var intro=BeginOpening();var so=new SerializedObject(intro);
            var cameraObject=(GameObject)so.FindProperty("cinematicCamera").objectReferenceValue;
            var gameplay=(GameObject)so.FindProperty("gameplayCamera").objectReferenceValue;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Data/Cinematic/ArrivalCamera.anim");
            Assert.That(intro.GetComponent<PlayableDirector>().playableAsset,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(OpeningTiming.End).Within(.01));
            Vector3 previous=Vector3.zero;
            for(float t=19;t<OpeningTiming.End;t+=.2f)
            {
                intro.SampleAt(t);Physics.SyncTransforms();
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
            intro.SampleAt(OpeningTiming.End);
            Assert.That(Vector3.Distance(cameraObject.transform.position,gameplay.transform.position),Is.LessThan(.005));
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation,gameplay.transform.rotation),Is.LessThan(.1));
            intro.GetComponent<PlayableDirector>().Stop();
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
            foreach(var part in artist)
            {
                bool retired=part.name.StartsWith("trashbin-",StringComparison.Ordinal);
                if(retired)Assert.That(meshes.Contains(part.sharedMesh),Is.False,"Old bins are replaced by Trashbin.fbx.");
                else Assert.That(meshes,Does.Contain(part.sharedMesh),"Artist object absent: "+part.name);
            }
            foreach(var part in AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Mesh/Trashbin.fbx").GetComponentsInChildren<MeshFilter>())
                Assert.That(meshes,Does.Contain(part.sharedMesh),"Updated artist bin absent: "+part.name);
            foreach(var sign in all.Where(t=>t.Find("Lettering")&&t.Find("Lettering").GetComponent<Canvas>()))
            {
                string concept=sign.name=="HabitatSign"?"SM_Prop_Sign_OutofOrder_01":"SM_Prop_Sign_Director_01";
                Assert.That(sign.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains(concept)),Is.True,sign.name);
                Assert.That(sign.Find("Lettering/CleanInsert"),Is.Not.Null);
                foreach(var text in sign.GetComponentsInChildren<Text>())Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),sign.name+" lettering overflow");
            }
            Assert.That(all.Single(t=>t.name=="HabitatSign").GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SM_Prop_Pole_01")),Is.True);
            Assert.That(all.Any(t=>t.name=="WaterFoodClimatePoster")&&all.Any(t=>t.name=="ColdChainPoster"),Is.True,"The two corridor story posters stay authored.");
            foreach(var poster in all.Where(t=>t.name.EndsWith("Poster",StringComparison.Ordinal)&&t.Find("Print")))
                Assert.That(poster.GetComponentsInChildren<MeshFilter>(true).Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SM_Prop_Blackboard_01")),Is.True,poster.name+" must use the wooden package frame");
            Assert.That(all.Any(t=>t.name=="TaurusMountains"||t.name=="DistantTaurusRidge"),Is.False);
            var terrain=all.Single(t=>t.name=="TaurusTerrain").GetComponent<MeshFilter>().sharedMesh;
            Assert.That(terrain.vertexCount,Is.GreaterThan(2000));Assert.That(terrain.bounds.size.z,Is.GreaterThan(500));Assert.That(terrain.bounds.size.y,Is.GreaterThan(35));Assert.That(terrain.bounds.min.y,Is.LessThan(0));
            Assert.That(terrain.vertices.All(v=>!float.IsNaN(v.y)&&!float.IsInfinity(v.y)),Is.True);
        }
        [Test] public void SwimmingIsCurvedAndVisibleBeforeLogoThenFireworksThenWalk()
        {
            var intro=BeginOpening();
            var source=new SerializedObject(intro);var cameraObject=(GameObject)source.FindProperty("cinematicCamera").objectReferenceValue;cameraObject.SetActive(true);
            var director=intro.GetComponent<PlayableDirector>();
            var logo=all.Single(t=>t.name=="COP31RisingLogo");var group=logo.GetComponent<CanvasGroup>();
            var turtles=all.Select(t=>t.GetComponent<ShorelineTurtle>()).Where(t=>t).ToArray();
            intro.SampleAt(21);Assert.That(turtles.Any(t=>!t.IsSwimming),Is.True);
            intro.SampleAt(OpeningTiming.SwimReveal);Assert.That(turtles.All(t=>t.IsSwimming),Is.True);Assert.That(group.alpha,Is.LessThan(.01));
            intro.SampleAt(OpeningTiming.LogoStart);Assert.That(turtles.All(t=>t.IsCruising),Is.True);float initialY=logo.localPosition.y;
            var direction=turtles[0].transform.forward;intro.SampleAt(OpeningTiming.LogoRaised);
            Assert.That(Vector3.Angle(direction,turtles[0].transform.forward),Is.GreaterThan(35));
            Assert.That(logo.localPosition.y-initialY,Is.GreaterThan(15));Assert.That(group.alpha,Is.GreaterThan(.99));
            Assert.That(logo.GetComponent<RectTransform>().anchoredPosition.x,Is.EqualTo(0).Within(.01));
            intro.SampleAt(46);
            var fireworks=all.Where(t=>t.name.StartsWith("COP31Firework_",StringComparison.Ordinal)).Select(t=>t.GetComponent<ParticleSystem>()).ToArray();
            Assert.That(fireworks.Length,Is.GreaterThanOrEqualTo(20));Assert.That(fireworks.Where(p=>p.gameObject.activeInHierarchy).Sum(p=>p.particleCount),Is.GreaterThan(0));
            intro.SampleAt(OpeningTiming.WalkStart+.2f);Assert.That(group.alpha,Is.LessThan(.01));Assert.That(fireworks.All(p=>!p.gameObject.activeInHierarchy),Is.True,"No frozen fireworks after the celebration.");
            Assert.That(cameraObject.transform.position.z,Is.GreaterThan(20),"Corridor walk must follow the logo reveal.");
            intro.SampleAt(76);Assert.That(cameraObject.transform.position.x,Is.LessThan(-12));Assert.That(cameraObject.transform.position.z,Is.InRange(-5,-3));
            director.Stop();
        }
        [Test] public void LogoRemainsCenteredThroughoutTheRiseAndTheShowHasLargeDistinctColors()
        {
            var intro=BeginOpening();var logo=all.Single(t=>t.name=="COP31RisingLogo").GetComponent<RectTransform>();
            var canvas=logo.GetComponentInParent<Canvas>();Assert.That(canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            var canvasRect=(RectTransform)canvas.transform;
            var camera=(GameObject)new SerializedObject(intro).FindProperty("cinematicCamera").objectReferenceValue;
            intro.SampleAt(OpeningTiming.LogoRaised);var heldRotation=camera.transform.rotation;
            for(float time=OpeningTiming.LogoStart;time<OpeningTiming.WalkStart;time+=.2f)
            {
                intro.SampleAt(time);Canvas.ForceUpdateCanvases();
                var center=RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect,logo).center;
                Assert.That(center.x,Is.EqualTo(0).Within(.1),"Visible logo must not drift left when the camera tilts at "+time);
                Assert.That(Quaternion.Angle(logo.localRotation,Quaternion.identity),Is.LessThan(.01));
                if(time>=OpeningTiming.LogoRaised)Assert.That(Quaternion.Angle(camera.transform.rotation,heldRotation),Is.LessThan(.1),"Firework framing must stay still until the walk begins.");
            }
            var fireworks=all.Where(t=>t.name.StartsWith("COP31Firework_",StringComparison.Ordinal)).Select(t=>t.GetComponent<ParticleSystem>()).ToArray();
            Assert.That(fireworks.Length,Is.GreaterThanOrEqualTo(20));
            foreach(var firework in fireworks){Assert.That(firework.main.startSize.constantMin,Is.GreaterThan(.22));Assert.That(firework.emission.GetBurst(0).count.constant,Is.GreaterThan(100));}
            var colors=fireworks.Select(p=>p.colorOverLifetime.color.gradient.colorKeys[1].color).Distinct().ToArray();
            Assert.That(colors.Length,Is.GreaterThanOrEqualTo(6));
            foreach(var color in colors){Color.RGBToHSV(color,out _,out float saturation,out _);Assert.That(saturation,Is.GreaterThan(.65));}
            intro.GetComponent<PlayableDirector>().Stop();
        }
        [Test] public void EntranceDoorsCloseThePortalThenOpenOnTheirHingesBeforeTheCameraCrosses()
        {
            var intro=BeginOpening();var root=all.Single(t=>t.name=="FacilityEntranceDoors");
            var north=root.Find("NorthHinge");var south=root.Find("SouthHinge");
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())Assert.That(renderer.gameObject.isStatic,Is.False,"Animated doors cannot be statically batched.");
            intro.SampleAt(68);Physics.SyncTransforms();
            Assert.That(Mathf.DeltaAngle(north.localEulerAngles.y,90),Is.EqualTo(0).Within(.1));
            Assert.That(Physics.SphereCast(new Vector3(-23,1.7f,-4),.12f,Vector3.right,out var hit,4,1,QueryTriggerInteraction.Ignore),Is.True,"Closed entrance must fill the actual portal.");
            Assert.That(hit.collider.transform.IsChildOf(root),Is.True);
            intro.SampleAt((OpeningTiming.DoorsOpenStart+OpeningTiming.DoorsOpenEnd)*.5f);
            Assert.That(Mathf.DeltaAngle(north.localEulerAngles.y,90),Is.InRange(20,70));
            intro.SampleAt(OpeningTiming.DoorsOpenEnd);Physics.SyncTransforms();
            Assert.That(Mathf.DeltaAngle(north.localEulerAngles.y,0),Is.EqualTo(0).Within(.1));
            Assert.That(Mathf.DeltaAngle(south.localEulerAngles.y,180),Is.EqualTo(0).Within(.1));
            Assert.That(Physics.SphereCast(new Vector3(-23,1.7f,-4),.12f,Vector3.right,out _,4,1,QueryTriggerInteraction.Ignore),Is.False,"Opened leaves must clear the camera path.");
            intro.GetComponent<PlayableDirector>().Stop();
        }
        [Test] public void RearWallHasRealPortraitsAndBothCornersHaveThreeDimensionalStandingFlags()
        {
            foreach(var name in new[]{"AtaturkPortrait","PresidentPortrait"})
            {
                var root=all.Single(t=>t.name==name);var image=root.GetComponentsInChildren<Image>().Single(i=>i.name=="Photograph");
                Assert.That(image.sprite,Is.Not.Null);Assert.That(AssetDatabase.GetAssetPath(image.sprite),Does.StartWith("Assets/Art/OfficeIdentity/"));
                Assert.That(root.position.z,Is.LessThan(-7.5));Assert.That(root.position.y,Is.GreaterThan(2));Assert.That(image.preserveAspect,Is.True);
                var viewport=(RectTransform)image.transform.parent;
                Assert.That(viewport.GetComponent<RectMask2D>(),Is.Not.Null);
                Assert.That(image.rectTransform.sizeDelta.x,Is.GreaterThanOrEqualTo(viewport.sizeDelta.x));Assert.That(image.rectTransform.sizeDelta.y,Is.GreaterThanOrEqualTo(viewport.sizeDelta.y));
                Assert.That(image.rectTransform.sizeDelta.x/image.rectTransform.sizeDelta.y,Is.EqualTo(image.sprite.rect.width/image.sprite.rect.height).Within(.001));
                Assert.That(root.GetComponentsInChildren<Text>(true),Is.Empty);
                Assert.That(root.Find("ArtworkFrame").GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SM_Prop_Blackboard_01")),Is.True);
            }
            foreach(var name in new[]{"TurkiyeStandingFlag","UNStandingFlag"})
            {
                var root=all.Single(t=>t.name==name);Assert.That(Mathf.Abs(root.position.x),Is.GreaterThan(5.5));Assert.That(root.position.z,Is.LessThan(-6.5));
                var cloth=root.Find("DrapedCloth").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(cloth.vertexCount,Is.GreaterThan(500));Assert.That(cloth.bounds.size.z,Is.GreaterThan(.1));
                Assert.That(root.Find("WeightedBase").GetComponent<MeshFilter>().sharedMesh.bounds.min.y,Is.EqualTo(0).Within(.001));
                Assert.That(root.Find("CrescentFinial"),Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/PolygonOffice/",StringComparison.Ordinal)),Is.True);
            }
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
