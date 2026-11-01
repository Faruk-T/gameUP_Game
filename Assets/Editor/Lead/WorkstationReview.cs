using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class WorkstationReview
    {
        public static void Capture()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots=scene.GetRootGameObjects();
            foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Where(c=>c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace))canvas.gameObject.SetActive(false);
            var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single(c=>c.CompareTag("MainCamera"));camera.gameObject.SetActive(true);
            var capture=Canvas("__WorkstationReview");
            try
            {
                View(camera,new Vector3(-1.5f,3.4f,-5.8f),new Vector3(3.7f,1.05f,4),65);
                ProductionSnapshot.Capture(capture,camera,"Docs/Previews/Workstation/RightConveyor.png",1920,1080);
                View(camera,new Vector3(0,2.1f,-3.6f),new Vector3(0,.9f,-7),70);
                ProductionSnapshot.Capture(capture,camera,"Docs/Previews/Workstation/UpdatedBins.png",1920,1080);
                View(camera,new Vector3(-1.1f,1.8f,-1.9f),new Vector3(-.25f,1.35f,.18f),50);
                ProductionSnapshot.Capture(capture,camera,"Docs/Previews/Workstation/MinistryFax.png",1920,1080);
                var catalog=AssetDatabase.LoadAssetAtPath<GameUP.Contracts.CampaignDefinition>(CampaignAuthoring.CatalogPath);
                foreach(var sample in new[]{(id:"D1-01",angle:Vector3.zero,name:"MilkFront"),(id:"D1-01",angle:new Vector3(0,180,0),name:"MilkBack"),
                    (id:"D1-02",angle:new Vector3(-65,0,0),name:"MeatLabel"),(id:"D2-03",angle:Vector3.zero,name:"PineappleLabel")})
                {
                    var definition=catalog.Days.SelectMany(d=>d.Products).Single(p=>p.DefinitionId==sample.id);
                    var product=(GameObject)PrefabUtility.InstantiatePrefab(definition.Prefab);
                    View(camera,new Vector3(0,2.35f,-3.2f),new Vector3(0,2.35f,0),58);
                    product.transform.position=camera.transform.TransformPoint(new Vector3(0,-.23f,1.15f));product.transform.rotation=Quaternion.Euler(sample.angle);
                    ProductionSnapshot.Capture(capture,camera,"Docs/Previews/Workstation/"+sample.name+".png",1920,1080);UnityEngine.Object.DestroyImmediate(product);
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(capture.gameObject);EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);}
            Debug.Log("WORKSTATION_VISUALS_OK / right belt / new bins / fax / baked product surfaces");
        }
        private static void View(Camera camera,Vector3 position,Vector3 target,float fov)
        {camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.fieldOfView=fov;}
    }
}
