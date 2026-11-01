using System;
using System.Linq;
using GameUP.Contracts;
using GameUP.Interaction;
using GameUP.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
            ConditionVariantAuthoring.Generate();
            foreach(var definition in days.SelectMany(d=>d.Products))
            {
                var root=new GameObject(definition.DefinitionId);root.layer=8;
                if(!definition.ModelSource)throw new InvalidOperationException("Assign an existing artist model: "+definition.DefinitionId);
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(ConditionVariantAuthoring.PathFor(definition.ModelSource.name,definition.Condition));
                if(!source)throw new InvalidOperationException("Generate condition variants: "+definition.DefinitionId);
                var model=PlaceModel(source,root.transform,Vector3.zero,.65f);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
                var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.33f,0);box.size=new Vector3(.8f,.72f,.8f);
                var target=root.AddComponent<WorkstationTarget>();SetAction(target,WorkstationAction.Product);
                ProductLabelAuthoring.Configure(root,model,definition);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,ProductionProjectSetup.Products+"/"+definition.DefinitionId+".prefab");
                Set(definition,"prefab",prefab);UnityEngine.Object.DestroyImmediate(root);
            }
        }
        internal static void SetAction(WorkstationTarget target,WorkstationAction action)
        {var so=new SerializedObject(target);so.FindProperty("action").enumValueIndex=(int)action;so.ApplyModifiedPropertiesWithoutUndo();target.gameObject.layer=8;}
        internal static GameObject PlaceModel(GameObject source,Transform parent,Vector3 position,float maximumSize,float yaw=0)
        {
            if(!source)throw new InvalidOperationException("An explicit artist prefab is required.");
            // Measure at the origin before parenting, so shelf rotations never change normalization.
            var container=new GameObject("Model_"+source.name);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(source);visual.transform.SetParent(container.transform,false);
            visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
            var renderers=visual.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)throw new InvalidOperationException("Model has no renderer: "+source.name);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            float scale=maximumSize/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            visual.transform.localScale*=scale;
            visual.transform.localPosition=-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            foreach(var collider in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            if(container.scene!=parent.gameObject.scene)SceneManager.MoveGameObjectToScene(container,parent.gameObject.scene);
            container.transform.SetParent(parent,false);container.transform.localPosition=position;container.transform.localRotation=Quaternion.Euler(0,yaw,0);
            return container;
        }
    }
}
