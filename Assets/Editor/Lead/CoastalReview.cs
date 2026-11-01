using System;
using System.IO;
using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;

namespace GameUp.Editor
{
    public static class CoastalReview
    {
        public static void AuthorAndCapture(){CinematicAuthoring.Author();Capture();}
        public static void Diagnose()
        {
            EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="MediterraneanSea"||r.name=="BeachSlope"||r.name=="SM_Bld_Wall_Blank_01"))
                Debug.Log("GEOMETRY "+renderer.name+" localBounds="+renderer.localBounds+" world="+renderer.bounds+" scale="+renderer.transform.lossyScale+" material="+renderer.sharedMaterial.name+" cull="+(renderer.sharedMaterial.HasProperty("_Cull")?renderer.sharedMaterial.GetFloat("_Cull"):-1));
            var mesh=GameObject.Find("MediterraneanSea").GetComponent<MeshFilter>().sharedMesh;
            Debug.Log("WATER_MESH "+mesh.name+" "+mesh.bounds+" normals="+mesh.normals[0]+" vertices="+mesh.vertexCount+" min="+mesh.vertices.Min(v=>v.y)+" max="+mesh.vertices.Max(v=>v.y));
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var intro=UnityEngine.Object.FindFirstObjectByType<IntroCinematic>();var so=new SerializedObject(intro);
            var cameraObject=(GameObject)so.FindProperty("cinematicCamera").objectReferenceValue;
            var camera=cameraObject.GetComponent<Camera>();
            var gameCamera=(GameObject)so.FindProperty("gameplayCamera").objectReferenceValue;gameCamera.SetActive(false);
            var ui=(GameObject)so.FindProperty("gameplayInterface").objectReferenceValue;ui.SetActive(false);
            var overlay=((CanvasGroup)so.FindProperty("overlay").objectReferenceValue).gameObject;overlay.SetActive(true);
            var black=(CanvasGroup)so.FindProperty("blackout").objectReferenceValue;
            var quote=(CanvasGroup)so.FindProperty("quote").objectReferenceValue;
            cameraObject.SetActive(true);
            var director=intro.GetComponent<PlayableDirector>();director.RebuildGraph();director.Play();
            Directory.CreateDirectory("Docs/Previews/Coastal");
            intro.SampleAt(5);
            ProductionSnapshot.Capture(overlay.GetComponent<Canvas>(),null,"Docs/Previews/Coastal/OpeningQuote.png",1920,1080);
            ProductionSnapshot.Capture(overlay.GetComponent<Canvas>(),null,"Docs/Previews/Coastal/OpeningQuote720.png",1280,720);
            black.alpha=0;quote.alpha=0;
            foreach(var item in new[]{(time:23d,name:"Beach"),(time:37d,name:"Swimming"),(time:42d,name:"LogoRising"),(time:46d,name:"LogoFireworks"),(time:64d,name:"Entrance"),(time:76d,name:"CorridorWater"),(time:80d,name:"CorridorColdChain"),(time:88.5d,name:"Workstation")})
            {
                intro.SampleAt(item.time);
                Debug.Log("CAMERA_POSE "+item.name+" "+camera.transform.position+" forward="+camera.transform.forward);
                PoseTurtles((float)item.time);
                ProductionSnapshot.Capture(overlay.GetComponent<Canvas>(),camera,"Docs/Previews/Coastal/"+item.name+".png",1920,1080);
            }
            director.Stop();
            var shaders=AssetDatabase.GetDependencies(ProductionProjectSetup.GamePath,true).Where(p=>p.EndsWith(".shader")).Select(AssetDatabase.LoadAssetAtPath<Shader>).ToArray();
            foreach(var shader in shaders)if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader error: "+shader.name);
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("COASTAL_VISUALS_OK");
        }
        private static void PoseTurtles(float t)
        {
            var animation=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Outside/Mesh/caretta caretta.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            foreach(var turtle in UnityEngine.Object.FindObjectsByType<ShorelineTurtle>(FindObjectsSortMode.None))
            {
                var animator=turtle.GetComponentInChildren<Animator>();animation.SampleAnimation(animator.gameObject,t%animation.length);
            }
        }
    }
}
