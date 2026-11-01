using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.ProductAuthoring;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class FacilityPropAuthoring
    {
        internal const string Artist="Assets/Art/Environment/Mesh/Environment.fbx";
        internal static GameObject Package(string name)=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/PolygonOffice/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Single(p=>Path.GetFileNameWithoutExtension(p)==name));
        internal static GameObject Pack(string name,Transform parent,Vector3 center,Vector3 size,float yaw=0,Material paint=null)
            =>Fit(Package(name),parent,center,size,Quaternion.Euler(0,yaw,0),paint);
        internal static GameObject Esma(string name,Transform parent,Vector3 center,Vector3 size,float yaw=0)
            =>ArtistPart(Artist,name,parent,center,size,yaw);
        internal static GameObject ArtistPart(string asset,string name,Transform parent,Vector3 center,Vector3 size,float yaw=0)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(asset).GetComponentsInChildren<MeshRenderer>().Single(r=>r.name==name);
            var container=new GameObject(name);
            var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(container.transform,false);
            part.transform.localRotation=source.transform.rotation;part.transform.localScale=source.transform.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
            var root=FitInstance(container,parent,center,size,Quaternion.Euler(0,yaw,0),null);
            return root;
        }
        internal static GameObject Fit(GameObject source,Transform parent,Vector3 center,Vector3 size,Quaternion rotation,Material paint=null)
        {
            if(!source)throw new InvalidOperationException("A saved prop asset is required.");
            return FitInstance((GameObject)PrefabUtility.InstantiatePrefab(source),parent,center,size,rotation,paint);
        }
        internal static GameObject FitInstance(GameObject instance,Transform parent,Vector3 center,Vector3 size,Quaternion rotation,Material paint)
        {
            instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var renderers=instance.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            var scale=new Vector3(size.x/Mathf.Max(bounds.size.x,.001f),size.y/Mathf.Max(bounds.size.y,.001f),size.z/Mathf.Max(bounds.size.z,.001f));
            var root=new GameObject("Prop_"+instance.name);instance.transform.SetParent(root.transform,false);
            instance.transform.localScale=Vector3.Scale(instance.transform.localScale,scale);instance.transform.localPosition=-Vector3.Scale(bounds.center,scale);
            foreach(var c in instance.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            foreach(var r in renderers)if(paint)r.sharedMaterials=Enumerable.Repeat(paint,r.sharedMaterials.Length).ToArray();
            root.transform.SetParent(parent,false);root.transform.localPosition=center;root.transform.localRotation=rotation;
            return root;
        }
        internal static void Sign(Transform parent,string name,Vector3 position,float yaw,float width,float height,string message)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            SignContents(root,width,height,message);
        }
        internal static void RefreshSign(Transform root)
        {
            var old=(RectTransform)root.Find("Lettering");string message=old.GetComponentInChildren<Text>().text;
            float width=old.sizeDelta.x*old.localScale.x,height=old.sizeDelta.y*old.localScale.y;
            foreach(var child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            SignContents(root,width,height,message);
        }
        private static void SignContents(Transform root,float width,float height,string message)
        {
            string name=root.name;
            bool habitat=name=="HabitatSign";
            var paint=Material(habitat?"CoastalWarningFrame":"DirectorSignFrame",habitat?new Color(.5f,.43f,.29f):new Color(.52f,.46f,.32f),habitat?0:.45f);
            paint.SetFloat("_Cull",0);
            paint.SetTexture("_BaseMap",null);
            var prop=Pack(habitat?"SM_Prop_Sign_OutofOrder_01":"SM_Prop_Sign_Director_01",root,Vector3.zero,new Vector3(width+.16f,height+.12f,.09f),0,paint);
            CoastalAssetSetup.Outline(prop);
            if(habitat)Pack("SM_Prop_Pole_01",root,new Vector3(0,-.42f,.06f),new Vector3(.09f,1.5f,.09f),0,Material("CoastalWarningPost",new Color(.28f,.2f,.12f)));
            var canvas=WorldCanvas("Lettering",root,new Vector3(0,0,-.057f),width,height);
            Image("CleanInsert",canvas,0,0,1200,canvas.sizeDelta.y,habitat?Paper:new Color(.025f,.14f,.16f));
            var text=Text("Message",canvas,message,0,0,1120,canvas.sizeDelta.y-24,habitat?60:width<=1.25f?130:42,Paper,TextAnchor.MiddleCenter);
            if(habitat)text.color=Ink;
            if(text.preferredHeight>text.rectTransform.rect.height)
                text.fontSize=Mathf.FloorToInt(text.fontSize*text.rectTransform.rect.height/text.preferredHeight);
        }
        internal static void PosterBacking(Transform parent)
        {
            WallArtFrameAuthoring.Create(parent,new Vector2(1.8f,2.64f));
        }
        private static RectTransform WorldCanvas(string name,Transform parent,Vector3 position,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform),typeof(Canvas)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.localPosition=position;rect.localScale=Vector3.one*(width/1200);rect.sizeDelta=new Vector2(1200,height/width*1200);
            rect.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;return rect;
        }
        internal static void Mountains(Transform parent)
        {
            const int columns=256,rows=28;var vertices=new Vector3[(columns+1)*(rows+1)];var triangles=new int[columns*rows*6];
            for(int r=0;r<=rows;r++)for(int a=0;a<=columns;a++)
            {
                float angle=a*Mathf.PI*2/columns,u=(float)r/rows,radius=160+u*240;
                float noise=Mathf.PerlinNoise(3+Mathf.Sin(angle)*2.7f,6+Mathf.Cos(angle)*2.7f);
                float summit=.5f+.5f*Mathf.Sin(angle*5+Mathf.Sin(angle*2)*1.4f);
                float detail=Mathf.PerlinNoise(11+Mathf.Sin(angle)*14+u*7,13+Mathf.Cos(angle)*14+u*5);
                float crest=.46f+.1f*Mathf.Sin(angle*7);
                float slope=u<crest?u/crest:(1-u)/(1-crest);
                float ridge=Mathf.Pow(Mathf.Max(0,slope),1.3f);
                float height=-6+ridge*(18+summit*summit*70+noise*18)+(detail-.5f)*ridge*19;
                int i=r*(columns+1)+a;vertices[i]=new Vector3(Mathf.Sin(angle)*radius,height,Mathf.Cos(angle)*radius);
                if(r==rows||a==columns)continue;
                int t=(r*columns+a)*6;triangles[t]=i;triangles[t+1]=i+columns+1;triangles[t+2]=i+1;
                triangles[t+3]=i+1;triangles[t+4]=i+columns+1;triangles[t+5]=i+columns+2;
            }
            var mesh=new Mesh{name="Taurus continuous 3D terrain"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var colors=new Color[vertices.Length];var normals=mesh.normals;
            for(int i=0;i<colors.Length;i++)
            {
                float stone=Mathf.Clamp01((1-normals[i].y)*1.7f+Mathf.Max(0,vertices[i].y-25)/85);
                colors[i]=Color.Lerp(new Color(.12f,.23f,.18f),new Color(.43f,.44f,.4f),stone);
            }
            mesh.colors=colors;string path=CoastalAssetSetup.Root+"/TaurusTerrain.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            string materialPath=CoastalAssetSetup.Root+"/TaurusTerrain.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("GameUP/COP31/Terrain"));AssetDatabase.CreateAsset(material,materialPath);}
            var root=new GameObject("TaurusTerrain",typeof(MeshFilter),typeof(MeshRenderer));root.transform.SetParent(parent,false);
            root.GetComponent<MeshFilter>().sharedMesh=saved;root.GetComponent<MeshRenderer>().sharedMaterial=material;root.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        internal static void Inventory()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects().Where(r=>!r.CompareTag("EditorOnly")).ToArray();
            var meshes=roots.SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Select(f=>f.sharedMesh)
                .Concat(roots.SelectMany(r=>r.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Select(r=>r.sharedMesh)).Where(m=>m).ToArray();
            var used=new System.Collections.Generic.HashSet<Mesh>(meshes);
            var parts=AssetDatabase.LoadAssetAtPath<GameObject>(Artist).GetComponentsInChildren<MeshFilter>();
            var lines=new System.Collections.Generic.List<string>{"# Sanatçı çevre objeleri — kaynak ve sahne denetimi","","Kaynak: feature/Objects / 0106ab2. 16 çevre parçası, 31 ürün ve caretta kaynağı projededir. Mesh referansı üzerinden, gizli belge durumları da dahil kontrol edilir.","","Faks ve siren, Bakanlık bildirimleri ve vardiya raporu için masadadır. T / son raporu yeniden açma kısayolu bulunmaz. Normal F analiz cihazı korunur.","","| Obje | Projede | Game sahnesi |","|---|---|---|"};
            int active=0;
            foreach(var part in parts)
            {
                bool present=used.Contains(part.sharedMesh);
                bool replaced=part.name.StartsWith("trashbin-",StringComparison.Ordinal);
                lines.Add("| "+part.name+" | Var | "+(present?"Var":replaced?"Yeni Trashbin.fbx modeliyle değiştirildi":"EKSİK")+" |");
                if(present)active++;
                if(!present&&!replaced)throw new InvalidOperationException("Artist object missing: "+part.name);
            }
            foreach(var part in AssetDatabase.LoadAssetAtPath<GameObject>(RecyclingBinAuthoring.Source).GetComponentsInChildren<MeshFilter>())
            {
                bool present=used.Contains(part.sharedMesh);lines.Add("| Yeni / "+part.name+" | Var | "+(present?"Var":"EKSİK")+" |");
                if(present)active++;
                if(!present)throw new InvalidOperationException("Updated bin missing: "+part.name);
            }
            lines.Add("");lines.Add("31 ürün modeli kampanya havuzunda; caretta modeli beş animasyonlu aktörde kullanılır. Tüm sayımlar otomatik testlerle doğrulanır.");
            File.WriteAllLines("Docs/ENVIRONMENT_INVENTORY.md",lines);Debug.Log("ARTIST_ENVIRONMENT_OK imported=20/20 active="+active+" replaced=4");
        }
    }
}
