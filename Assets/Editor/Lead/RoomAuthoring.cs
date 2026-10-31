using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;
using static GameUp.Editor.ProductAuthoring;

namespace GameUp.Editor
{
    internal static class RoomAuthoring
    {
        internal const string ArtistRoot="Assets/Art/Urunler/Mesh/";

        internal static void Create(Transform parent)
        {
            var room=new GameObject("Room").transform;room.SetParent(parent,false);
            var wall=Material("Plaster",new Color(.83f,.85f,.8f));var trim=Material("RoomTrim",new Color(.2f,.35f,.34f));
            var floor=Material("Floor",new Color(.62f,.57f,.47f));
            Shape("RoomFloor",room,PrimitiveType.Cube,new Vector3(0,-.16f,.25f),new Vector3(16,.3f,16.5f),floor);
            Shape("LeftWall",room,PrimitiveType.Cube,new Vector3(-8,2.6f,.25f),new Vector3(.25f,5.2f,16.5f),wall);
            Shape("RightWall",room,PrimitiveType.Cube,new Vector3(8,2.6f,.25f),new Vector3(.25f,5.2f,16.5f),wall);
            Shape("RearWall",room,PrimitiveType.Cube,new Vector3(0,2.6f,-8),new Vector3(16,5.2f,.25f),wall);
            Shape("FrontSill",room,PrimitiveType.Cube,new Vector3(0,.6f,8.5f),new Vector3(16,1.2f,.25f),wall);
            Shape("WindowHeader",room,PrimitiveType.Cube,new Vector3(0,4.8f,8.5f),new Vector3(16,.8f,.25f),wall);
            foreach(float x in new[]{-7.8f,-4f,0f,4f,7.8f})
                Shape("WindowMullion",room,PrimitiveType.Cube,new Vector3(x,2.8f,8.4f),new Vector3(.14f,3.3f,.2f),trim);
            foreach(float y in new[]{1.2f,4.4f})
                Shape("WindowRail",room,PrimitiveType.Cube,new Vector3(0,y,8.35f),new Vector3(16,.12f,.25f),trim);
            foreach(float x in new[]{-7.83f,7.83f})
                Shape("Skirting",room,PrimitiveType.Cube,new Vector3(x,.12f,.2f),new Vector3(.1f,.22f,16),trim);
            for(int i=-3;i<=3;i++)
                Shape("FloorJoint",room,PrimitiveType.Cube,new Vector3(i*2,.003f,.2f),new Vector3(.012f,.004f,16),Material("Grout",new Color(.48f,.46f,.39f)));
            Coast(room);
            Poster(room,"FoodSecurityPoster",new Vector3(-7.82f,2.9f,1.6f),-90,"SOFRAYI\nKORU","Yerel üretime değer ver.\nGıdayı israf etme.","Assets/Art/Documents/Day1Products.png");
            Poster(room,"ClimateActionPoster",new Vector3(7.82f,2.9f,1.6f),90,"ORTAK\nGELECEK","İklim için birlikte.\nAntalya'dan dünyaya.",null);
            Poster(room,"WasteSortingPoster",new Vector3(0,2.6f,-7.82f),180,"HER ATIĞIN\nBİR YERİ VAR","Temiz ambalajı ayır.\nKontamine ambalajı ayrı tut.",null);
            foreach(var t in room.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
        }
        private static void Coast(Transform parent)
        {
            Shape("MediterraneanSea",parent,PrimitiveType.Cube,new Vector3(0,-.4f,30),new Vector3(120,.1f,50),Material("Sea",new Color(.14f,.56f,.61f)));
            var shore=Material("DistantCoast",new Color(.36f,.49f,.53f));
            var mesh=new Mesh{name="TaurusSilhouette"};
            float[] peaks={2,7,4,11,6,13,8,5,9,3,7,2};
            var vertices=new Vector3[peaks.Length*2];var indices=new int[(peaks.Length-1)*6];
            for(int i=0;i<peaks.Length;i++)
            {
                vertices[i*2]=new Vector3(-55+i*10,0,0);vertices[i*2+1]=new Vector3(-55+i*10,peaks[i],0);
                if(i==peaks.Length-1)continue;int p=i*6,n=i*2;
                indices[p]=n;indices[p+1]=n+1;indices[p+2]=n+3;indices[p+3]=n;indices[p+4]=n+3;indices[p+5]=n+2;
            }
            mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();
            string path=ProductionProjectSetup.Art+"/TaurusSilhouette.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var mountains=new GameObject("TaurusMountains",typeof(MeshFilter),typeof(MeshRenderer));mountains.transform.SetParent(parent,false);mountains.transform.localPosition=new Vector3(0,-.4f,49);
            mountains.GetComponent<MeshFilter>().sharedMesh=mesh;mountains.GetComponent<MeshRenderer>().sharedMaterial=shore;
        }
        internal static Transform[] RecyclingStation(Transform parent)
        {
            var station=new GameObject("RecyclingStation").transform;station.SetParent(parent,false);
            string[] names={"KÂĞIT","CAM","PLASTİK","KONTAMİNE"};
            var colors=new[]{new Color(.16f,.37f,.7f),new Color(.14f,.5f,.35f),new Color(.8f,.63f,.12f),new Color(.65f,.18f,.16f)};
            var points=new Transform[4];var metal=Material("BinRim",new Color(.19f,.23f,.22f),.4f);
            for(int i=0;i<4;i++)
            {
                var bin=new GameObject("Waste_"+i).transform;bin.SetParent(station,false);bin.localPosition=new Vector3(-2.4f+i*1.6f,.64f,-7);bin.localRotation=Quaternion.Euler(0,180,0);
                var color=Material("Waste_"+i,colors[i]);
                Shape("Front",bin,PrimitiveType.Cube,new Vector3(0,0,-.44f),new Vector3(1.12f,1.2f,.08f),color);
                Shape("Back",bin,PrimitiveType.Cube,new Vector3(0,0,.44f),new Vector3(1.12f,1.2f,.08f),color);
                foreach(float x in new[]{-.52f,.52f})Shape("Side",bin,PrimitiveType.Cube,new Vector3(x,0,0),new Vector3(.08f,1.2f,.86f),color);
                Shape("Base",bin,PrimitiveType.Cube,new Vector3(0,-.56f,0),new Vector3(1.04f,.08f,.86f),metal);
                foreach(float x in new[]{-.56f,.56f})Shape("Rim",bin,PrimitiveType.Cube,new Vector3(x,.6f,0),new Vector3(.08f,.08f,1),metal);
                foreach(float z in new[]{-.48f,.48f})Shape("Rim",bin,PrimitiveType.Cube,new Vector3(0,.6f,z),new Vector3(1.2f,.08f,.08f),metal);
                var collider=bin.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(1.2f,1.3f,1);
                SetAction(bin.gameObject.AddComponent<GameUP.Interaction.WorkstationTarget>(),(GameUP.Interaction.WorkstationAction)((int)GameUP.Interaction.WorkstationAction.Paper+i));
                var label=WorldCanvas("BinLabel",bin,new Vector3(0,.08f,-.49f),0,.98f,.55f);
                Text("Category",label,names[i],0,55,820,180,86,Paper,TextAnchor.MiddleCenter);
                Text("Instruction",label,i==3?"AYRI İMHA":"TEMİZ AMBALAJ",0,-140,820,120,48,Paper,TextAnchor.MiddleCenter);
                var point=new GameObject("WastePoint_"+i).transform;point.SetParent(station,false);point.position=bin.position+Vector3.up*.73f;points[i]=point;
            }
            return points;
        }
        private static void Poster(Transform parent,string name,Vector3 position,float yaw,string headline,string message,string photograph)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            Shape("Frame",root,PrimitiveType.Cube,Vector3.zero,new Vector3(1.92f,2.76f,.07f),Material("PosterFrame",new Color(.21f,.27f,.25f)));
            var canvas=WorldCanvas("Print",root,new Vector3(0,0,-.05f),0,1.8f,2.64f);
            Image("Paper",canvas,0,0,900,1320,Paper);
            var logo=Image("COP31Official",canvas,0,445,840,472,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/COP31Horizontal.png"));logo.preserveAspect=true;
            Text("Headline",canvas,headline,0,160,780,280,76,Ink,TextAnchor.MiddleCenter,true);
            if(photograph!=null)
            {
                var picture=Image("Photo",canvas,0,-160,800,320,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(photograph));picture.preserveAspect=true;
            }
            else
            {
                var marks=new[]{new Color(.16f,.37f,.7f),new Color(.14f,.5f,.35f),new Color(.8f,.63f,.12f)};
                for(int i=0;i<3;i++)Image("ClimateMark",canvas,(i-1)*205,-155,150,150,marks[i]);
                Text("ActionLine",canvas,"AZALT   •   YENİDEN KULLAN   •   AYIR",0,-285,820,80,33,Ink,TextAnchor.MiddleCenter);
            }
            Text("Message",canvas,message,0,-420,800,180,48,Ink,TextAnchor.MiddleCenter);
            Text("Dates",canvas,"9–20 KASIM 2026  /  ANTALYA\ncop31.tr",0,-590,800,100,30,Ink,TextAnchor.MiddleCenter);
        }
        private static RectTransform WorldCanvas(string name,Transform parent,Vector3 position,float yaw,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(900,height/width*900);rect.localScale=Vector3.one*(width/900);
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;return rect;
        }
    }
}
