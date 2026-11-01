using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class ProductLabelAuthoring
    {
        internal const string Root="Assets/Art/ProductLabels";
        private const int Grid=12;
        private struct Triangle { public Vector3 A,B,C; }

        internal static void RefreshCampaign(bool milkOnly=false,bool rebuildTextures=true)
        {
            if(!AssetDatabase.IsValidFolder(Root)){Directory.CreateDirectory(Root);AssetDatabase.Refresh();}
            foreach(var definition in ProductionProjectSetup.Days().SelectMany(d=>d.Products).Where(d=>!milkOnly||d.ModelSource.name.StartsWith("milk",StringComparison.Ordinal)))
            {
                string path=AssetDatabase.GetAssetPath(definition.Prefab);var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var model=prefab.transform.Cast<Transform>().Single(t=>t.name.StartsWith("Model_",StringComparison.Ordinal)).gameObject;
                    Configure(prefab,model,definition,rebuildTextures);PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();Debug.Log("PRODUCT_LABELS_OK / 31 products / 62 baked textures / no runtime text canvas");
        }

        internal static void Configure(GameObject root,GameObject model,ProductDefinition definition,bool rebuildTextures=true)
        {
            if(!AssetDatabase.IsValidFolder(Root)){Directory.CreateDirectory(Root);AssetDatabase.Refresh();}
            foreach(var name in new[]{"Front","Back","DateFront","DateBack"}){var old=root.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var triangles=new List<Triangle>();var bounds=new Bounds();bool initialized=false;
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var renderer=filter.GetComponent<MeshRenderer>();
                var material=renderer.sharedMaterials[0];
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=vertices[i];
                    if(material.shader.name=="GameUP/FoodCondition")
                    {
                        var min=(Vector3)material.GetVector("_BoundsMin");var size=(Vector3)material.GetVector("_BoundsSize");
                        var u=new Vector3((p.x-min.x)/Mathf.Max(size.x,.0001f),(p.y-min.y)/Mathf.Max(size.y,.0001f),(p.z-min.z)/Mathf.Max(size.z,.0001f));
                        float swell=material.GetFloat("_Swell")*material.GetFloat("_Spoilage");float bulge=Mathf.Sin(Mathf.Clamp01(u.y)*Mathf.PI)*swell;
                        p.x+=(u.x-.5f)*size.x*bulge;p.z+=(u.z-.5f)*size.z*bulge;p.y+=Mathf.Pow(Mathf.Clamp01(u.y),5)*size.y*swell*.35f;
                    }
                    vertices[i]=root.transform.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if(!initialized){bounds=new Bounds(vertices[i],Vector3.zero);initialized=true;}else bounds.Encapsulate(vertices[i]);
                }
                var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)triangles.Add(new Triangle{A=vertices[indices[i]],B=vertices[indices[i+1]],C=vertices[indices[i+2]]});
            }
            if(!initialized)throw new InvalidOperationException("Product mesh missing: "+definition.DefinitionId);
            bool flat=bounds.size.y<Mathf.Max(bounds.size.x,bounds.size.z)*.45f;
            float width=bounds.size.x*.64f,height=(flat?bounds.size.z:bounds.size.y)*.56f;
            Vector3 center=bounds.center;
            if(definition.Category==ProductCategory.Fruit){center.y=bounds.min.y+bounds.size.y*.34f;height=bounds.size.y*.38f;}
            var front=Surface(root,"Front",bounds,triangles,flat?Vector3.up:Vector3.back,width,height,
                LabelMaterial(definition,"Front",definition.GetFormattedFrontLabel(),width,height,rebuildTextures),center);
            var back=Surface(root,"Back",bounds,triangles,flat?Vector3.down:Vector3.forward,width,height,
                LabelMaterial(definition,"Back",definition.GetFormattedBackLabel(),width,height,rebuildTextures),center);
            var binder=root.GetComponent<ProductWorldLabelBinder>();if(!binder)binder=root.AddComponent<ProductWorldLabelBinder>();
            binder.Configure(definition,front,back);
            if(definition.ModelSource.name.StartsWith("milk",StringComparison.Ordinal))
            {
                var sealCenter=bounds.center;sealCenter.y=bounds.max.y-.05f;
                float sealWidth=bounds.size.x*.8f,sealHeight=.08f;
                var seal=ProductAuthoring.Material("ProductDateMask",Paper);
                Surface(root,"DateFront",bounds,triangles,Vector3.back,sealWidth,sealHeight,seal,sealCenter,false);
                Surface(root,"DateBack",bounds,triangles,Vector3.forward,sealWidth,sealHeight,seal,sealCenter,false);
            }
        }

        private static Material LabelMaterial(ProductDefinition definition,string face,string text,float width,float height,bool rebuild)
        {
            if(rebuild)return Bake(definition,face,text,width,height);
            var material=AssetDatabase.LoadAssetAtPath<Material>($"{Root}/{definition.DefinitionId}_{face}.mat");
            return material?material:throw new InvalidOperationException("Bake label textures before refreshing surfaces: "+definition.DefinitionId);
        }

        private static Material Bake(ProductDefinition definition,string face,string text,float width,float height)
        {
            text=Regex.Replace(text.Trim(),@"\n\s*\n","\n");
            int pixelsHigh=Mathf.Clamp(Mathf.RoundToInt(1024*height/width),256,1536);
            var canvas=Canvas("__LabelBake");
            try
            {
                canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1024,pixelsHigh);
                Image("ReservedPrintArea",canvas.transform,0,0,1024,pixelsHigh,new Color(.965f,.95f,.89f));
                var ink=Text("ProductInformation",canvas.transform,text,0,0,1000,pixelsHigh-24,256,Ink,TextAnchor.MiddleCenter);
                ink.lineSpacing=1;ink.resizeTextForBestFit=true;ink.resizeTextMinSize=16;ink.resizeTextMaxSize=256;
                string path=$"{Root}/{definition.DefinitionId}_{face}.png";
                ProductionSnapshot.Capture(canvas,null,path,1024,pixelsHigh);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.anisoLevel=4;
                importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                var platform=importer.GetPlatformTextureSettings("Standalone");platform.name="Standalone";platform.overridden=true;
                platform.maxTextureSize=2048;platform.format=TextureImporterFormat.BC7;platform.compressionQuality=100;
                importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
                string materialPath=$"{Root}/{definition.DefinitionId}_{face}.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.color=Color.white;material.SetFloat("_Smoothness",.12f);
                EditorUtility.SetDirty(material);return material;
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
        }

        private static MeshRenderer Surface(GameObject root,string name,Bounds bounds,List<Triangle> triangles,Vector3 normal,float width,float height,Material material,Vector3? printCenter=null,bool planar=true)
        {
            Vector3 right=Vector3.right,up=Mathf.Abs(normal.y)>.5f?(normal.y>0?Vector3.forward:Vector3.back):Vector3.up;
            if(normal.z>0)right=Vector3.left;
            var vertices=new Vector3[(Grid+1)*(Grid+1)];var uv=new Vector2[vertices.Length];var valid=new bool[vertices.Length];var indices=new List<int>();
            float distance=bounds.size.magnitude+1;
            float nearestPlane=float.MaxValue;
            for(int y=0;y<=Grid;y++)for(int x=0;x<=Grid;x++)
            {
                int i=y*(Grid+1)+x;float u=(float)x/Grid,v=(float)y/Grid;
                Vector3 origin=(printCenter??bounds.center)+right*((u-.5f)*width)+up*((v-.5f)*height)+normal*distance;
                float nearest=float.MaxValue;
                foreach(var triangle in triangles)if(Intersect(origin,-normal,triangle,out float hit)&&hit<nearest)nearest=hit;
                nearestPlane=Mathf.Min(nearestPlane,nearest);
                valid[i]=nearest<float.MaxValue;vertices[i]=origin-normal*(nearest<float.MaxValue?nearest:distance)+normal*.003f;uv[i]=new Vector2(u,v);
            }
            if(planar&&nearestPlane<float.MaxValue)
                for(int i=0;i<vertices.Length;i++)
                {
                    float u=uv[i].x,v=uv[i].y;
                    vertices[i]=(printCenter??bounds.center)+right*((u-.5f)*width)+up*((v-.5f)*height)+normal*(distance-nearestPlane+.003f);
                    valid[i]=true;
                }
            for(int y=0;y<Grid;y++)for(int x=0;x<Grid;x++)
            {
                int a=y*(Grid+1)+x,b=a+1,c=a+Grid+1,d=c+1;
                if(!valid[a]||!valid[b]||!valid[c]||!valid[d])continue;
                bool reverse=Vector3.Dot(Vector3.Cross(right,up),normal)<0;
                indices.Add(a);indices.Add(reverse?c:b);indices.Add(reverse?b:c);
                indices.Add(b);indices.Add(reverse?c:d);indices.Add(reverse?d:c);
            }
            if(indices.Count<30)throw new InvalidOperationException("Print area does not fit product: "+root.name+"/"+name);
            var mesh=new Mesh{name=root.name+"_"+name+"_PrintSurface"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            string meshPath=$"{Root}/{root.name}_{name}.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else {AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}
            EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.layer=8;
            go.GetComponent<MeshFilter>().sharedMesh=saved;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;return renderer;
        }

        private static bool Intersect(Vector3 origin,Vector3 direction,Triangle t,out float distance)
        {
            var e1=t.B-t.A;var e2=t.C-t.A;var p=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,p);
            distance=0;if(Mathf.Abs(determinant)<.0000001f)return false;
            float inverse=1/determinant;var s=origin-t.A;float u=Vector3.Dot(s,p)*inverse;if(u<0||u>1)return false;
            var q=Vector3.Cross(s,e1);float v=Vector3.Dot(direction,q)*inverse;if(v<0||u+v>1)return false;
            distance=Vector3.Dot(e2,q)*inverse;return distance>0;
        }
    }
}
