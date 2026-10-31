using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static GameUp.Editor.ProductAuthoring;

namespace GameUp.Editor
{
    /// <summary>Renders press photographs directly from the artist's existing models, in an isolated preview scene.</summary>
    public static class CampaignPhotoAuthoring
    {
        [MenuItem("GameUP32/Authoring/Render campaign press photographs")]
        public static void Generate()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Press photographs require a graphics device.");
            Directory.CreateDirectory("Assets/Art/Documents");
            for(int d=0;d<CampaignAuthoring.PhotoModels.Length;d++)Render(CampaignAuthoring.PhotoModels[d],$"Assets/Art/Documents/Day{d+1}Products.png");
            AssetDatabase.Refresh();Debug.Log("CAMPAIGN_PHOTOS_OK existingArtistModelsOnly=true");
        }
        private static void Render(string[] names,string path)
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("PressPhoto");SceneManager.MoveGameObjectToScene(root,scene);
            var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
            Texture2D pixels=null;Material stage=null;
            try
            {
                stage=new Material(Shader.Find("Universal Render Pipeline/Lit"));stage.color=new Color(.52f,.46f,.34f);
                Shape("PhotoStage",root.transform,PrimitiveType.Cube,new Vector3(0,-.12f,0),new Vector3(20,.2f,20),stage);
                for(int i=0;i<names.Length;i++)
                {
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(RoomAuthoring.ArtistRoot+names[i]+".prefab");
                    PlaceModel(source,root.transform,new Vector3((i-(names.Length-1)*.5f)*1.05f,0,i%2*.28f),1.05f,i%2==0?-12:14);
                }
                var light=new GameObject("SoftWindowLight",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root.transform,false);
                light.type=LightType.Directional;light.intensity=1.25f;light.color=new Color(1,.95f,.86f);light.transform.rotation=Quaternion.Euler(45,-25,0);light.shadows=LightShadows.Soft;
                var camera=new GameObject("PressCamera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform,false);
                camera.enabled=false;camera.transform.position=new Vector3(.9f,3.3f,-6.8f);camera.transform.LookAt(new Vector3(0,.42f,.1f));
                camera.orthographic=true;camera.orthographicSize=(names.Length*1.05f+.65f)/(2*16f/9);camera.aspect=16f/9;camera.nearClipPlane=.1f;camera.farClipPlane=30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.75f,.77f,.7f);
                camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            finally
            {
                RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
                if(pixels)UnityEngine.Object.DestroyImmediate(pixels);EditorSceneManager.ClosePreviewScene(scene);if(stage)UnityEngine.Object.DestroyImmediate(stage);
            }
        }
    }
}
