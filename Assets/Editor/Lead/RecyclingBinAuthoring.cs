using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    internal static class RecyclingBinAuthoring
    {
        internal const string Source="Assets/Art/Environment/Mesh/Trashbin.fbx";
        private static readonly Vector3 BinSize=new Vector3(.72f,1.38f,.68f);
        internal static readonly string[] Names={"trashbin-paper","trashbin-glass","trashbin-plastic","Trashbin"};
        internal static void Apply(Transform office)
        {
            RemapSource();
            var station=office.Find("RecyclingStation");
            float center=Enumerable.Range(0,4).Average(i=>station.Find("Waste_"+i).localPosition.x);
            var colors=new[]{new Color(.16f,.37f,.7f),new Color(.14f,.5f,.35f),new Color(.8f,.63f,.12f),new Color(.65f,.18f,.16f)};
            for(int i=0;i<4;i++)
            {
                var bin=station.Find("Waste_"+i);
                bin.localPosition=new Vector3(center+(i-1.5f)*BinSize.x,bin.localPosition.y,bin.localPosition.z);
                foreach(var child in bin.Cast<Transform>().Where(t=>t.GetComponentsInChildren<MeshFilter>(true).Length>0).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                var model=Place(i,bin,new Vector3(0,.05f,0),BinSize);
                model.transform.localRotation=Quaternion.Euler(0,180,0);
                var label=bin.Find("BinLabel");label.localScale=Vector3.one*(.68f/900);label.localPosition=new Vector3(0,.03f,-.36f);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>CategoryMaterial(m,i,colors[i])).ToArray();
                CoastalAssetSetup.Outline(model);
                var collider=bin.GetComponent<BoxCollider>();collider.center=new Vector3(0,.05f,0);collider.size=BinSize;
                station.Find("WastePoint_"+i).position=bin.position+Vector3.up*.73f;
            }
            Debug.Log("NEW_RECYCLING_BINS_OK / supplied FBX / 4 adjoining containers / non-overlapping targets / aligned sorting points");
        }
        internal static GameObject Place(int category,Transform parent,Vector3 center,Vector3 size)
            =>FacilityPropAuthoring.ArtistPart(Source,Names[category],parent,center,size);
        private static Material CategoryMaterial(Material source,int index,Color color)
        {
            if(!source.mainTexture)throw new InvalidOperationException("Bin texture missing: "+source.name);
            string path="Assets/Art/Workstation/UpdatedBin_"+index+"_"+source.name.Replace(' ','_')+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("GameUP/Facility/RecyclingBin"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("GameUP/Facility/RecyclingBin");material.SetTexture("_BaseMap",source.mainTexture);material.SetColor("_ContainerTint",color);
            bool front=source.name=="Bin (front)";
            material.SetTextureScale("_BaseMap",front?new Vector2(1,-1):Vector2.one);
            material.SetTextureOffset("_BaseMap",front?new Vector2(0,1):Vector2.zero);
            EditorUtility.SetDirty(material);return material;
        }
        internal static void RemapSource()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Source);
            foreach(var pair in new[]{("Bin (front).001","Bin (front).mat"),("Bin (rear).001","Bin (rear).mat"),("Bin (side).001","Bin (side).mat"),("Bin (top) diffuse.001","Bin (top) diffuse 1.mat")})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Material/"+pair.Item2);
                if(!material)throw new InvalidOperationException("Missing bin material: "+pair.Item2);
                string file=pair.Item1=="Bin (front).001"?"Bin (front) diffuse-Bin (front) opacity_3.png":pair.Item1=="Bin (rear).001"?"Bin (rear) diffuse_0.jpeg":pair.Item1=="Bin (side).001"?"Bin (side) diffuse_1.jpeg":"Bin (top) diffuse_2.jpeg";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Textures/"+file);
                if(!texture)throw new InvalidOperationException("Missing bin source texture: "+file);
                material.SetTexture("_BaseMap",texture);material.SetTexture("_MainTex",texture);EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Item1),material);
            }
            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
        }
    }
}
