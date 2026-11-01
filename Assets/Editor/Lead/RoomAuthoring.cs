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
            CoastalEnvironmentAuthoring.Interior(room);
            Shape("FrontSill",room,PrimitiveType.Cube,new Vector3(0,.6f,8.5f),new Vector3(16,1.2f,.25f),wall);
            Shape("WindowHeader",room,PrimitiveType.Cube,new Vector3(0,4.8f,8.5f),new Vector3(16,.8f,.25f),wall);
            foreach(float x in new[]{-7.8f,-4f,0f,4f,7.8f})
                Shape("WindowMullion",room,PrimitiveType.Cube,new Vector3(x,2.8f,8.4f),new Vector3(.14f,3.3f,.2f),trim);
            foreach(float y in new[]{1.2f,4.4f})
                Shape("WindowRail",room,PrimitiveType.Cube,new Vector3(0,y,8.35f),new Vector3(16,.12f,.25f),trim);
            foreach(float x in new[]{-7.83f,7.83f})
                Shape("Skirting",room,PrimitiveType.Cube,new Vector3(x,.12f,.2f),new Vector3(.1f,.22f,16),trim);
            Coast(room);
            Poster(room,"FoodSecurityPoster",new Vector3(-7.82f,2.9f,1.6f),-90,"SOFRAYI\nKORU","Yerel üretime değer ver.\nGıdayı israf etme.","Assets/Art/Documents/Day1Products.png");
            Poster(room,"ClimateActionPoster",new Vector3(7.82f,2.9f,1.6f),90,"ORTAK\nGELECEK","İklim için birlikte.\nAntalya'dan dünyaya.",null);
            Poster(room,"WasteSortingPoster",new Vector3(0,2.6f,-7.82f),180,"HER ATIĞIN\nBİR YERİ VAR","Temiz ambalajı ayır.\nKontamine ambalajı ayrı tut.",null);
            foreach(var t in room.GetComponentsInChildren<Transform>())
                if(!t.GetComponentInParent<GameUp.Gameplay.ShorelineTurtle>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
        }
        private static void Coast(Transform parent)
        {
            CoastalEnvironmentAuthoring.Outside(parent);
            FacilityPropAuthoring.Mountains(parent);
        }
        internal static Transform[] RecyclingStation(Transform parent)
        {
            var station=new GameObject("RecyclingStation").transform;station.SetParent(parent,false);
            string[] names={"KÂĞIT","CAM","PLASTİK","KONTAMİNE"};
            var colors=new[]{new Color(.16f,.37f,.7f),new Color(.14f,.5f,.35f),new Color(.8f,.63f,.12f),new Color(.65f,.18f,.16f)};
            var points=new Transform[4];
            for(int i=0;i<4;i++)
            {
                var bin=new GameObject("Waste_"+i).transform;bin.SetParent(station,false);bin.localPosition=new Vector3(-2.4f+i*1.6f,.64f,-7);bin.localRotation=Quaternion.Euler(0,180,0);
                string[] sources={"trashbin-paper","trashbin-glass","trashbin-plastic","trashbin-general"};
                FacilityPropAuthoring.Esma(sources[i],bin,Vector3.zero,new Vector3(1.1f,1.2f,.88f));
                var collider=bin.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(1.2f,1.3f,1);
                SetAction(bin.gameObject.AddComponent<GameUP.Interaction.WorkstationTarget>(),(GameUP.Interaction.WorkstationAction)((int)GameUP.Interaction.WorkstationAction.Paper+i));
                var label=WorldCanvas("BinLabel",bin,new Vector3(0,.08f,-.49f),0,.98f,.55f);
                Image("CategoryBand",label,0,0,890,460,colors[i]);
                Text("Category",label,names[i],0,55,820,180,86,Paper,TextAnchor.MiddleCenter);
                Text("Instruction",label,i==3?"AYRI İMHA":"TEMİZ AMBALAJ",0,-140,820,120,48,Paper,TextAnchor.MiddleCenter);
                CoastalAssetSetup.Outline(bin.gameObject);
                var point=new GameObject("WastePoint_"+i).transform;point.SetParent(station,false);point.position=bin.position+Vector3.up*.73f;points[i]=point;
            }
            return points;
        }
        internal static void Poster(Transform parent,string name,Vector3 position,float yaw,string headline,string message,string photograph)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            FacilityPropAuthoring.PosterBacking(root);
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
