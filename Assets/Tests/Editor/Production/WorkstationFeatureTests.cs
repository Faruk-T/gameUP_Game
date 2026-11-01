using System.Linq;
using GameUp.App;
using GameUp.Editor;
using GameUp.Gameplay;
using GameUP.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Production.Tests
{
    public sealed class WorkstationFeatureTests
    {
        [Test] public void FirstOwnsOneAudioManagerAndLongAudioStreams()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath,OpenSceneMode.Additive);
            try
            {
                var managers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioManager>(true)).ToArray();
                Assert.That(managers.Length,Is.EqualTo(1));
                Assert.That(managers[0].GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(7));
                Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).Count(),Is.EqualTo(1));
                var so=new SerializedObject(managers[0]);var cues=so.FindProperty("cues");Assert.That(cues.arraySize,Is.EqualTo(9));
                var analyzer=(AudioSource)so.FindProperty("analyzer").objectReferenceValue;Assert.That(analyzer.volume,Is.EqualTo(.95f));
                foreach(string name in new[]{"analyzer","effects","printer","narration"})Assert.That(((AudioSource)so.FindProperty(name).objectReferenceValue).loop,Is.False,name+" must play once");
                var narration=(AudioSource)so.FindProperty("narration").objectReferenceValue;
                Assert.That(AssetDatabase.GetAssetPath(narration.clip),Is.EqualTo("Assets/Audio/Cinematic/sinema_senkron_ses.mp3"));
                Assert.That(narration.clip.length,Is.LessThanOrEqualTo(OpeningTiming.End));
                Assert.That(((AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(narration.clip))).defaultSampleSettings.loadType,Is.EqualTo(AudioClipLoadType.Streaming));
                Assert.That(so.FindProperty("workstationOceanVolume").floatValue,Is.GreaterThanOrEqualTo(.2f));
                for(int i=0;i<cues.arraySize;i++)Assert.That(cues.GetArrayElementAtIndex(i).FindPropertyRelative("Clip").objectReferenceValue,Is.Not.Null);
                foreach(var path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio/Music"}).Select(AssetDatabase.GUIDToAssetPath))
                    Assert.That(((AudioImporter)AssetImporter.GetAtPath(path)).defaultSampleSettings.loadType,Is.EqualTo(AudioClipLoadType.Streaming));
            }
            finally {EditorSceneManager.CloseScene(scene,true);}
        }

        [Test] public void EveryProductHasBakedFrontAndBackInformationAndNoFloatingText()
        {
            var campaign=AssetDatabase.LoadAssetAtPath<GameUP.Contracts.CampaignDefinition>(CampaignAuthoring.CatalogPath);
            foreach(var product in campaign.Days.SelectMany(d=>d.Products))
            {
                var binder=product.Prefab.GetComponent<GameUP.Rules.ProductWorldLabelBinder>();
                Assert.That(binder.ProductDefinition,Is.SameAs(product));Assert.That(product.Prefab.GetComponentsInChildren<Canvas>(true),Is.Empty);
                Assert.That(binder.PrintedSurfaces,Is.EquivalentTo(product.Prefab.GetComponentsInChildren<MeshRenderer>(true)));
                Assert.That(product.Prefab.transform.Find("Front"),Is.Null);
                Assert.That(product.Prefab.transform.Find("Back"),Is.Null);
                var bounds=binder.LocalBounds;
                Assert.That(binder.InspectionMultiplier*Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z),Is.EqualTo(.86f).Within(.001f));
                foreach(var surface in binder.PrintedSurfaces)
                {
                    Assert.That(surface,Is.Not.Null,product.DefinitionId);
                    foreach(var material in surface.sharedMaterials)
                    {
                        Assert.That(AssetDatabase.GetAssetPath(material.mainTexture),Does.StartWith("Assets/Art/ProductPrints/"+product.DefinitionId+"_"));
                        Assert.That(material.mainTexture.width,Is.EqualTo(2048));
                    }
                    Assert.That(surface.GetComponent<MeshFilter>().sharedMesh.triangles.Length,Is.GreaterThan(30));
                    Assert.That(AssetDatabase.GetAssetPath(surface.GetComponent<MeshFilter>().sharedMesh),Does.StartWith("Assets/Art/ProductPrints/"));
                }
            }
        }

        [Test] public void ConveyorPassesRightOfDeskAndThroughClearWallOpenings()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();var office=roots.Single(r=>r.name=="InspectionOffice");
                var conveyor=office.transform.Find("AirportConveyor");Assert.That(conveyor,Is.Not.Null);
                var presentation=roots.SelectMany(r=>r.GetComponentsInChildren<ProductPresentation>(true)).Single();var so=new SerializedObject(presentation);
                var arrival=(Transform)so.FindProperty("arrivalPoint").objectReferenceValue;var checkpoint=(Transform)so.FindProperty("deskPoint").objectReferenceValue;
                var exit=(Transform)so.FindProperty("approvedExitPoint").objectReferenceValue;
                var path=so.FindProperty("intakePath");Assert.That(path.arraySize,Is.EqualTo(2));
                var corner=(Transform)path.GetArrayElementAtIndex(0).objectReferenceValue;
                var player=roots.Single(r=>r.name=="PlayerView").transform;
                Assert.That(arrival.position.x,Is.GreaterThan(12));Assert.That(exit.position.z,Is.LessThan(-10));
                Assert.That(Vector3.Distance(arrival.position,corner.position)+Vector3.Distance(corner.position,exit.position),Is.GreaterThanOrEqualTo(30));
                Assert.That(checkpoint.position.x-player.position.x,Is.InRange(1.8f,2.3f));
                Assert.That(checkpoint.position.z,Is.EqualTo(player.position.z).Within(.01));
                Assert.That(arrival.position.z,Is.EqualTo(3.55f).Within(.01));
                var desk=office.transform.Find("Prop_desk").GetComponentsInChildren<Renderer>();var bounds=desk[0].bounds;
                foreach(var renderer in desk.Skip(1))bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.max.x,Is.LessThan(checkpoint.position.x-.65f),"Conveyor must not overlap the desk.");
                Assert.That(office.transform.Find("Room/PolygonOfficeArchitecture/ConveyorPortalHeader"),Is.Not.Null);
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)),Is.Empty,"Audio listener lives in First.");
            }
            finally {EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
