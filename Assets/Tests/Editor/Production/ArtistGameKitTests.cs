using System.Linq;
using GameUp.Editor;
using GameUP.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameUp.Production.Tests
{
    public sealed class ArtistGameKitTests
    {
        [Test] public void BehindTheBuildingKitContainsAllArtistSourcesAndIsIsolatedFromGameplay()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath,OpenSceneMode.Additive);
            try
            {
                var kit=scene.GetRootGameObjects().Single(r=>r.name==ArtistGameKitAuthoring.RootName);
                Assert.That(kit.CompareTag("EditorOnly"),Is.True);Assert.That(kit.transform.position.z,Is.LessThan(-20));
                Assert.That(kit.GetComponentsInChildren<Transform>(true).All(t=>t.CompareTag("EditorOnly")&&t.gameObject.layer==30),Is.True);
                Assert.That(kit.GetComponentsInChildren<WorkstationTarget>(true),Is.Empty);
                Assert.That(kit.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(kit.GetComponentsInChildren<Camera>(true),Is.Empty);
                Assert.That(kit.GetComponentsInChildren<Animator>(true),Is.Empty);
                var meshes=kit.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)
                    .Concat(kit.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.sharedMesh)).ToArray();
                foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler/Mesh"}))
                    foreach(var source in AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponentsInChildren<MeshFilter>())
                        Assert.That(meshes.Contains(source.sharedMesh),Is.True,"Missing product source: "+source.name);
                foreach(var part in AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Mesh/Environment.fbx").GetComponentsInChildren<MeshFilter>())
                    Assert.That(meshes.Contains(part.sharedMesh),Is.True,"Missing environment part: "+part.name);
                var turtle=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Outside/caretta caretta.prefab").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                Assert.That(meshes.Contains(turtle),Is.True);
                foreach(var camera in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)))
                    Assert.That(camera.cullingMask&(1<<30),Is.Zero,"Review kit must not appear in gameplay/cinematic cameras.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
