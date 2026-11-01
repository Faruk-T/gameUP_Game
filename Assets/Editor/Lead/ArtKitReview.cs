using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class ArtKitReview
    {
        public static void CompareFrames()
        {
            EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var root=new GameObject("__FrameComparison");root.transform.position=new Vector3(5000,0,0);
            var canvas=Canvas("__FrameComparisonLabels");
            var cameraObject=new GameObject("__FrameComparisonCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.transform.position=new Vector3(5000,0,-16);camera.orthographic=true;camera.orthographicSize=4;camera.aspect=16f/9;
            camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.08f,.1f);
            try
            {
                var names=new[]{"SM_Prop_CorkBoard_01","SM_Prop_CorkBoard_02","SM_Prop_Blackboard_01","SM_Prop_Darkboard_01","SM_Prop_Poster_04","SM_Prop_Poster_03","SM_Prop_Poster_Stocks_01","SM_Prop_Child_Art_01"};
                for(int i=0;i<names.Length;i++)
                {
                    var model=ProductAuthoring.PlaceModel(FacilityPropAuthoring.Package(names[i]),root.transform,new Vector3(-5.3f+i%4*3.53f,i<4?1.9f:-1.9f,0),2.75f,180);
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
                    model.transform.localPosition-=Vector3.up*bounds.size.y*.5f;
                    foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                    Text("Candidate_"+i,canvas.transform,names[i],-716+i%4*477,i<4?52:-460,460,48,23,Color.white,TextAnchor.MiddleCenter);
                    Debug.Log("FRAME_CANDIDATE "+names[i]+" size="+bounds.size+" vertices="+model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount)+" materials="+string.Join(",",renderers.Select(r=>r.sharedMaterial.name)));
                }
                ProductionSnapshot.Capture(canvas,camera,"Docs/Previews/Coastal/FrameCandidates.png",1920,1080);
            }
            finally
            {
                Object.DestroyImmediate(root);Object.DestroyImmediate(canvas.gameObject);Object.DestroyImmediate(cameraObject);
                EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            }
            Debug.Log("FRAME_COMPARISON_OK");
        }
        public static void CaptureKit()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var kit=scene.GetRootGameObjects().Single(r=>r.name==ArtistGameKitAuthoring.RootName);
            var cameraObject=new GameObject("__KitCaptureCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.cullingMask=1<<30;camera.farClipPlane=250;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.17f,.18f);
            var canvas=Canvas("__KitCaptureOverlay");
            Image("Header",canvas.transform,0,485,1920,110,new Color(.025f,.08f,.09f,.94f));
            var title=Text("Title",canvas.transform,"ESMANUR  /  GAME KIT",0,485,1800,70,35,Paper,TextAnchor.MiddleCenter);
            try
            {
                camera.orthographic=true;camera.orthographicSize=16;
                camera.transform.SetPositionAndRotation(new Vector3(28,30,-1),Quaternion.LookRotation(new Vector3(0,.5f,-27)-new Vector3(28,30,-1)));
                ProductionSnapshot.Capture(canvas,camera,"Docs/Previews/Coastal/GameKitOverview.png",1920,1080);
                camera.orthographic=false;camera.fieldOfView=60;
                title.text="ESMANUR  /  31 KAYNAK ÜRÜN MODELİ";
                camera.transform.SetPositionAndRotation(new Vector3(-10,13,-7),Quaternion.LookRotation(new Vector3(-11,.8f,-26)-new Vector3(-10,13,-7)));
                ProductionSnapshot.Capture(canvas,camera,"Docs/Previews/Coastal/GameKitProducts.png",1920,1080);
                title.text="ESMANUR  /  ÇEVRE PARÇALARI VE CARETTA";
                camera.transform.SetPositionAndRotation(new Vector3(21,14,-9),Quaternion.LookRotation(new Vector3(8,1,-27)-new Vector3(21,14,-9)));
                ProductionSnapshot.Capture(canvas,camera,"Docs/Previews/Coastal/GameKitEnvironment.png",1920,1080);
                Debug.Log("ARTIST_KIT_VISUALS_OK root="+kit.name+" tag="+kit.tag);
            }
            finally
            {
                Object.DestroyImmediate(canvas.gameObject);Object.DestroyImmediate(cameraObject);
                EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            }
        }
    }
}
