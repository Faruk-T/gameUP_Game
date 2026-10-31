using System;
using System.Linq;
using GameUP.Contracts;
using GameUP.Interaction;
using GameUP.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class ProductAuthoring
    {
        internal static Material Material(string name,Color color,float metallic=0)
        {
            string path=ProductionProjectSetup.Art+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(material);return material;
        }
        internal static GameObject Shape(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collider=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        internal static void Create(DayDefinition[] days)
        {
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Interactable";tags.ApplyModifiedPropertiesWithoutUndo();
            CreateFish();
            string[] models={"Yoghurt_01","Yoghurt_01","cheese1","cheese3","Yoghurt_03","Yoghurt_02"};int index=0;
            foreach(var definition in days.SelectMany(d=>d.Products))
            {
                var root=new GameObject(definition.DefinitionId);root.layer=8;
                var source=definition.Category==ProductCategory.Fish
                    ?AssetDatabase.LoadAssetAtPath<GameObject>(ProductionProjectSetup.Products+"/FishTray.prefab")
                    :AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Urunler/Mesh/"+models[index]+".prefab");
                if(!source)throw new InvalidOperationException("Missing explicit product model: "+definition.DefinitionId);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.transform.SetParent(root.transform,false);
                visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
                var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("Model has no renderer.");
                var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
                float scale=.65f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                visual.transform.localScale*=scale;visual.transform.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
                foreach(var collider in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
                var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.33f,0);box.size=new Vector3(.8f,.72f,.8f);
                var target=root.AddComponent<WorkstationTarget>();SetAction(target,WorkstationAction.Product);
                var front=Face(root.transform,"Front",new Vector3(0,.2f,-.28f),Quaternion.identity);
                var back=Face(root.transform,"Back",new Vector3(0,.2f,.28f),Quaternion.Euler(0,180,0));
                var labels=root.AddComponent<ProductWorldLabelBinder>();labels.Configure(definition,front,back);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,ProductionProjectSetup.Products+"/"+definition.DefinitionId+".prefab");
                Set(definition,"prefab",prefab);UnityEngine.Object.DestroyImmediate(root);index++;
            }
        }
        internal static void SetAction(WorkstationTarget target,WorkstationAction action)
        {var so=new SerializedObject(target);so.FindProperty("action").enumValueIndex=(int)action;so.ApplyModifiedPropertiesWithoutUndo();target.gameObject.layer=8;}
        private static Text Face(Transform parent,string name,Vector3 position,Quaternion rotation)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localRotation=rotation;go.transform.localScale=Vector3.one*.00045f;
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(1000,420);go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var card=Image("LabelPaper",rect,0,0,1000,420,Paper);
            var text=Text("LabelText",card.transform,"",0,0,940,380,42,Ink,TextAnchor.MiddleCenter);
            text.resizeTextForBestFit=true;text.resizeTextMinSize=26;text.resizeTextMaxSize=42;return text;
        }
        private static void CreateFish()
        {
            var root=new GameObject("FishTray");
            var silver=Material("FishSilver",new Color(.42f,.64f,.67f),.4f);var dark=Material("FishEye",Ink);
            Shape("Tray",root.transform,PrimitiveType.Cube,new Vector3(0,.04f,0),new Vector3(1,.08f,.48f),Material("Tray",new Color(.78f,.87f,.83f)));
            Shape("Fish",root.transform,PrimitiveType.Sphere,new Vector3(.05f,.15f,0),new Vector3(.63f,.22f,.3f),silver);
            Shape("Eye",root.transform,PrimitiveType.Sphere,new Vector3(.29f,.19f,-.13f),Vector3.one*.045f,dark);
            var mesh=new Mesh{name="FishTail"};mesh.vertices=new[]{new Vector3(-.2f,.15f,0),new Vector3(-.47f,.3f,0),new Vector3(-.47f,.02f,0),
                new Vector3(-.2f,.15f,.03f),new Vector3(-.47f,.3f,.03f),new Vector3(-.47f,.02f,.03f)};
            mesh.triangles=new[]{0,1,2,3,5,4,0,3,1,1,3,4,1,4,2,2,4,5,2,5,0,0,5,3};mesh.RecalculateNormals();
            string path=ProductionProjectSetup.Art+"/FishTail.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
            var tail=new GameObject("Tail",typeof(MeshFilter),typeof(MeshRenderer));tail.transform.SetParent(root.transform,false);
            tail.GetComponent<MeshFilter>().sharedMesh=mesh;tail.GetComponent<MeshRenderer>().sharedMaterial=silver;
            PrefabUtility.SaveAsPrefabAsset(root,ProductionProjectSetup.Products+"/FishTray.prefab");UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
