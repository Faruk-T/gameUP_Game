using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class OfficeIdentityAuthoring
    {
        private const string Root="Assets/Art/OfficeIdentity/";
        internal static void Create(Transform room)
        {
            foreach(var name in new[]{"Ataturk.jpg","President.jpg","Turkiye.png","UnitedNations.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Root+name);
                importer.textureType=name.EndsWith(".jpg")?TextureImporterType.Sprite:TextureImporterType.Default;
                importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=1024;
                importer.mipmapEnabled=true;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            }
            var root=new GameObject("OfficeIdentity").transform;root.SetParent(room,false);
            Portrait(root,"AtaturkPortrait",-2.35f,"Ataturk.jpg");
            Portrait(root,"PresidentPortrait",2.35f,"President.jpg");
            Flag(root,"TurkiyeStandingFlag",new Vector3(-6.45f,0,-7),25,"Turkiye.png");
            Flag(root,"UNStandingFlag",new Vector3(6.15f,0,-7),-25,"UnitedNations.png");
        }
        private static Material Surface(string name,Color color,float metallic=0,Texture texture=null)
        {
            string path=Root+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",metallic>0?.7f:.18f);
            material.SetFloat("_Cull",0);material.SetTexture("_BaseMap",texture);material.enableInstancing=true;
            EditorUtility.SetDirty(material);return material;
        }
        private static void Portrait(Transform parent,string name,float x,string file)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=new Vector3(x,3.2f,-7.78f);root.localRotation=Quaternion.Euler(0,180,0);
            WallArtFrameAuthoring.Create(root,new Vector2(1.4f,1.96f));
            var canvas=Rect("PortraitPrint",root,0,0,900,1260);canvas.localPosition=new Vector3(0,0,-.052f);canvas.localScale=Vector3.one*(1.4f/900);
            canvas.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var picture=Image("Photograph",canvas,0,0,900,1260,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(Root+file));picture.preserveAspect=true;
            FillPortrait(canvas,picture);
        }
        internal static void FillPortrait(RectTransform canvas,Image picture)
        {
            if(!canvas.GetComponent<RectMask2D>())canvas.gameObject.AddComponent<RectMask2D>();
            float aspect=picture.sprite.rect.width/picture.sprite.rect.height;
            var viewport=canvas.sizeDelta;
            picture.rectTransform.anchoredPosition=Vector2.zero;
            picture.rectTransform.sizeDelta=aspect>viewport.x/viewport.y?new Vector2(viewport.y*aspect,viewport.y):new Vector2(viewport.x,viewport.x/aspect);
            picture.preserveAspect=true;
        }
        private static void Flag(Transform parent,string name,Vector3 position,float yaw,string texture)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            var brass=Surface("FlagBrass",new Color(.69f,.48f,.16f),.8f);
            FacilityPropAuthoring.Pack("SM_Prop_Pole_01",root,new Vector3(0,1.48f,0),new Vector3(.037f,2.78f,.037f),0,brass);
            Geometry(root,"WeightedBase",BaseMesh(),brass);
            var cloth=Geometry(root,"DrapedCloth",ClothMesh(),Surface(name+"Cloth",Color.white,0,AssetDatabase.LoadAssetAtPath<Texture2D>(Root+texture)));
            cloth.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            Geometry(root,"CrescentFinial",FinialMesh(),brass);
        }
        private static GameObject Geometry(Transform parent,string name,Mesh mesh,Material material)
        {
            string path=Root+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        private static Mesh ClothMesh()
        {
            const int columns=32,rows=24;var vertices=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[vertices.Length];var indices=new int[columns*rows*6];
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=(float)x/columns,v=(float)y/rows;int i=y*(columns+1)+x;
                vertices[i]=new Vector3(.025f+u*(.73f+(1-v)*.3f),2.75f-(1-v)*1.25f-u*.65f,.05f+Mathf.Sin(u*Mathf.PI*3.3f+(1-v)*.7f)*u*.15f);
                uv[i]=new Vector2(1-u,v);
                if(x==columns||y==rows)continue;int t=(y*columns+x)*6;
                indices[t]=i;indices[t+1]=i+1;indices[t+2]=i+columns+1;indices[t+3]=i+1;indices[t+4]=i+columns+2;indices[t+5]=i+columns+1;
            }
            var mesh=new Mesh{name="MakamFlagCloth",vertices=vertices,uv=uv,triangles=indices};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh BaseMesh()
        {
            var profile=new[]{new Vector2(0,0),new Vector2(.24f,0),new Vector2(.28f,.025f),new Vector2(.28f,.055f),new Vector2(.23f,.08f),new Vector2(.12f,.11f),new Vector2(.045f,.19f),new Vector2(.019f,.24f)};
            const int sides=48;var vertices=new Vector3[profile.Length*(sides+1)];var indices=new List<int>();
            for(int r=0;r<profile.Length;r++)for(int a=0;a<=sides;a++)
            {
                float angle=a*Mathf.PI*2/sides;int i=r*(sides+1)+a;vertices[i]=new Vector3(Mathf.Cos(angle)*profile[r].x,profile[r].y,Mathf.Sin(angle)*profile[r].x);
                if(r==profile.Length-1||a==sides)continue;indices.AddRange(new[]{i,i+sides+1,i+1,i+1,i+sides+1,i+sides+2});
            }
            var mesh=new Mesh{name="MakamFlagBase",vertices=vertices,triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh FinialMesh()
        {
            var vertices=new List<Vector3>();var indices=new List<int>();const int segments=32;
            for(int i=0;i<=segments;i++)
            {
                float angle=Mathf.Lerp(45,315,(float)i/segments)*Mathf.Deg2Rad;
                vertices.Add(new Vector3(Mathf.Cos(angle)*.12f,2.99f+Mathf.Sin(angle)*.12f,0));
                vertices.Add(new Vector3(.035f+Mathf.Cos(angle)*.09f,2.99f+Mathf.Sin(angle)*.09f,0));
                if(i<segments){int a=i*2;indices.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
            }
            int center=vertices.Count;vertices.Add(new Vector3(.13f,2.99f,0));
            for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad;float radius=i%2==0?.048f:.022f;vertices.Add(new Vector3(.13f+Mathf.Cos(angle)*radius,2.99f+Mathf.Sin(angle)*radius,0));}
            for(int i=0;i<10;i++)indices.AddRange(new[]{center,center+1+i,center+1+(i+1)%10});
            var mesh=new Mesh{name="MakamFlagFinial",vertices=vertices.ToArray(),triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
