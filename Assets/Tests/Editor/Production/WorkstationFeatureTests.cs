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
                Assert.That(managers[0].GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(5));
                Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).Count(),Is.EqualTo(1));
                var so=new SerializedObject(managers[0]);var cues=so.FindProperty("cues");Assert.That(cues.arraySize,Is.EqualTo(9));
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
                foreach(var surface in new[]{binder.FrontLabelSurface,binder.BackLabelSurface})
                {
                    Assert.That(surface,Is.Not.Null,product.DefinitionId);
                    Assert.That(AssetDatabase.GetAssetPath(surface.sharedMaterial.mainTexture),Does.StartWith("Assets/Art/ProductLabels/"+product.DefinitionId+"_"));
                    Assert.That(surface.sharedMaterial.mainTexture.width,Is.EqualTo(1024));
                    Assert.That(surface.GetComponent<MeshFilter>().sharedMesh.triangles.Length,Is.GreaterThan(30));
                    var size=surface.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                    Assert.That(Mathf.Min(size.x,Mathf.Min(size.y,size.z)),Is.LessThan(.00001f),"Saved print surfaces must remain planar so text is not occluded: "+product.DefinitionId);
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
                Assert.That(arrival.position.z,Is.GreaterThan(12));Assert.That(exit.position.z,Is.LessThan(-10));
                Assert.That(Vector3.Distance(arrival.position,exit.position),Is.GreaterThanOrEqualTo(30));
                Assert.That(checkpoint.position.x,Is.GreaterThan(3));Assert.That(checkpoint.position.x,Is.EqualTo(arrival.position.x).Within(.001));
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
