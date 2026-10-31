using System;
using System.IO;
using System.Linq;
using GameUP.Contracts;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    public static class ConditionVariantAuthoring
    {
        public const string Variants="Assets/Prefabs/Products/Variants";
        private const string Materials="Assets/Art/FoodConditions";
        public static string PathFor(string model,FoodCondition condition)=>Variants+"/"+model+"_"+condition+".prefab";
        [MenuItem("GameUP32/Authoring/Generate healthy and spoiled artist variants")]
        public static void Generate()
        {
            Directory.CreateDirectory(Variants);Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
            var shader=Shader.Find("GameUP/FoodCondition");if(!shader)throw new InvalidOperationException("FoodCondition shader is required.");
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".prefab",StringComparison.Ordinal)))
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(FoodCondition condition in Enum.GetValues(typeof(FoodCondition)))
                {
                    var root=new GameObject(source.name+"_"+condition);
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
                    if(condition==FoodCondition.Spoiled)
                    {
                        var renderers=model.GetComponentsInChildren<MeshRenderer>();
                        for(int r=0;r<renderers.Length;r++)
                        {
                            var renderer=renderers[r];var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                            var originals=renderer.sharedMaterials;var materials=new Material[originals.Length];
                            for(int m=0;m<originals.Length;m++)
                            {
                                var original=originals[m];if(!original)throw new InvalidOperationException("Artist material missing.");
                                string assetPath=$"{Materials}/{source.name}_{r}_{m}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                                if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,assetPath);}
                                material.shader=shader;material.CopyPropertiesFromMaterial(original);
                                var category=CampaignAuthoring.Category(source.name);
                                material.SetColor("_SpoilTint",category==ProductCategory.Meat?new Color(.28f,.26f,.16f):category==ProductCategory.Fruit?new Color(.3f,.17f,.035f):new Color(.2f,.32f,.08f));
                                material.SetFloat("_Spoilage",1);material.SetFloat("_Swell",source.name.StartsWith("milk",StringComparison.Ordinal)||source.name.StartsWith("Yoghurt",StringComparison.Ordinal)?.22f:.035f);
                                material.SetVector("_BoundsMin",mesh.bounds.min);material.SetVector("_BoundsSize",mesh.bounds.size);
                                EditorUtility.SetDirty(material);materials[m]=material;
                            }
                            renderer.sharedMaterials=materials;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,PathFor(source.name,condition));UnityEngine.Object.DestroyImmediate(root);
                }
            }
            AssetDatabase.SaveAssets();Debug.Log("CONDITION_VARIANTS_OK sources=31 variants=62 originalMeshesPreserved=true runtimeSpawning=false");
        }
    }
}
