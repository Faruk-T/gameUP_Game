using System;
using System.IO;
using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static GameUp.Editor.ProductAuthoring;

namespace GameUp.Editor
{
    internal static class CoastalEnvironmentAuthoring
    {
        private const string Buildings = "Assets/PolygonOffice/Prefabs/Buildings/";
        internal static void Interior(Transform parent)
        {
            var office = new GameObject("PolygonOfficeArchitecture").transform; office.SetParent(parent,false);
            for(int x=0;x<4;x++) for(int z=0;z<4;z++)
                Fitted(Buildings+"SM_Bld_Floor_Concrete_01.prefab",office,new Vector3(-6+x*4,-.04f,-5.9375f+z*4.125f),new Vector3(4,.08f,4.125f));
            for(int i=0;i<5;i++)
            {
                Wall(office,new Vector3(-6.4f+i*3.2f,2.6f,-8),3.2f,5.2f,0);
                Wall(office,new Vector3(8,2.6f,-6.35f+i*3.3f),3.3f,5.2f,90);
            }
            Wall(office,new Vector3(-8,2.6f,-7),2,5.2f,90);
            for(int i=0;i<3;i++) Wall(office,new Vector3(-8,2.6f,-.25f+i*3.5f),3.5f,5.2f,90);
            Wall(office,new Vector3(-8,4.5f,-4),4,1.4f,90);
            // The portal stays open along the cinematic route; no camera passes through a closed door.
            var ceiling = Material("FacilityCeiling",new Color(.88f,.9f,.87f));
            Shape("FacilityCeiling",office,PrimitiveType.Cube,new Vector3(0,5.25f,.25f),new Vector3(16,.15f,16.5f),ceiling);
            Corridor(office);
            foreach(float x in new[]{-5f,0,5f})
            {
                var lamp=Prop("SM_Prop_Light_Fluorescent_01",office,new Vector3(x,4.9f,.5f),1.8f);
                var light=new GameObject("CeilingFill",typeof(Light)).GetComponent<Light>();light.transform.SetParent(lamp.transform,false);
                light.type=LightType.Point; light.range=9;light.intensity=1.2f;light.color=new Color(.85f,.94f,1);light.shadows=LightShadows.None;
            }
            Prop("SM_Prop_Plant_03",office,new Vector3(6.4f,0,6.5f),2.1f);
            Prop("SM_Prop_Plant_05",office,new Vector3(-6.4f,0,6.5f),1.8f);
            Prop("SM_Prop_Benchseat_01",office,new Vector3(5.8f,0,-5.8f),2.1f,90);
        }
        private static void Corridor(Transform parent)
        {
            var corridor = new GameObject("ArrivalCorridor").transform;corridor.SetParent(parent,false);
            for(int i=0;i<3;i++)
            {
                Fitted(Buildings+"SM_Bld_Floor_Concrete_01.prefab",corridor,new Vector3(-18+i*4,-.04f,-4),new Vector3(4,.08f,4));
                Wall(corridor,new Vector3(-18+i*4,1.9f,-2),4,3.8f,0);
                Wall(corridor,new Vector3(-18+i*4,1.9f,-6),4,3.8f,180);
            }
            Shape("CorridorCeiling",corridor,PrimitiveType.Cube,new Vector3(-14,3.85f,-4),new Vector3(12,.12f,4),Material("FacilityCeiling",new Color(.88f,.9f,.87f)));
            RoomAuthoring.Poster(corridor,"WaterFoodClimatePoster",new Vector3(-15.3f,1.95f,-2.13f),0,"SUYU KORU\nGIDAYI KORU","Gıda • Su • İklim\nAynı geleceğin parçaları.","Assets/Art/Documents/Day2Products.png");
            RoomAuthoring.Poster(corridor,"ColdChainPoster",new Vector3(-11.1f,1.95f,-5.87f),180,"SOĞUK ZİNCİR\nGÜVENLİ SOFRA","Kaynağı izle. Etiketi oku.\nRiskli ürünü ayır.","Assets/Art/Documents/Day1Products.png");
            corridor.Find("WaterFoodClimatePoster").localScale=Vector3.one*.72f;
            corridor.Find("ColdChainPoster").localScale=Vector3.one*.72f;
            foreach(float x in new[]{-18f,-13f,-9f})
            {
                Prop("SM_Prop_Light_Fluorescent_01",corridor,new Vector3(x,3.5f,-4),1.3f);
                var light=new GameObject("CorridorFill",typeof(Light)).GetComponent<Light>();light.transform.SetParent(corridor,false);light.transform.localPosition=new Vector3(x,3,-4);
                light.type=LightType.Point;light.range=6;light.intensity=2;light.color=new Color(.88f,.95f,1);light.shadows=LightShadows.None;
            }
            Sign(corridor,"InspectionDirection",new Vector3(-8.2f,3.25f,-4),90,2.8f,.5f,"ÜRÜN KONTROL  →");
            Prop("SM_Prop_Plant_04",corridor,new Vector3(-18.5f,0,-5.25f),1.2f);
            // Two open Polygon Office doors frame the entrance from the promenade.
            var entrance = new GameObject("FacilityEntrance").transform;entrance.SetParent(parent,false);
            Wall(entrance,new Vector3(-20,3.6f,-4),4,.6f,90);
            Fitted(Buildings+"SM_Bld_Door_Large_01_L.prefab",entrance,new Vector3(-20.15f,1.5f,-1.25f),new Vector3(1.6f,3,.12f),0);
            Fitted(Buildings+"SM_Bld_Door_Large_01_R.prefab",entrance,new Vector3(-20.15f,1.5f,-6.75f),new Vector3(1.6f,3,.12f),0);
            Sign(entrance,"FacilityName",new Vector3(-20.22f,3.65f,-4),90,5.2f,.7f,"ANTALYA  /  GIDA GÜVENLİĞİ TESİSİ");
            Sign(entrance,"FacilityCOP31",new Vector3(-20.25f,4.3f,-4),90,4.6f,.45f,"COP31  •  KIYIDAN SOFRAYA");
        }
        internal static void Outside(Transform parent)
        {
            var coast=new GameObject("AntalyaCoast").transform;coast.SetParent(parent,false);
            var sand=Material("AntalyaSand",new Color(.78f,.68f,.47f));
            var stone=Material("AntalyaLimestone",new Color(.77f,.75f,.65f));
            var timber=Material("PromenadeTimber",new Color(.36f,.26f,.17f));
            Shape("CoastalGround",coast,PrimitiveType.Cube,new Vector3(-10,-.65f,4.5f),new Vector3(100,1,33),sand);
            // A sloped, saved mesh gives water depth and an actual shoreline instead of intersecting cubes.
            var shoreMesh=new Mesh{name="AntalyaShoreSlope"};
            shoreMesh.vertices=new[]{new Vector3(-70,-.15f,21),new Vector3(70,-.15f,21),new Vector3(-70,-1.6f,36),new Vector3(70,-1.6f,36),new Vector3(-70,-4,180),new Vector3(70,-4,180)};
            shoreMesh.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5};shoreMesh.RecalculateNormals();shoreMesh.RecalculateBounds();
            SaveMesh(shoreMesh,CoastalAssetSetup.Root+"/ShoreSlope.asset");
            var slope=new GameObject("BeachSlope",typeof(MeshFilter),typeof(MeshRenderer));slope.transform.SetParent(coast,false);
            slope.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(CoastalAssetSetup.Root+"/ShoreSlope.asset");slope.GetComponent<MeshRenderer>().sharedMaterial=sand;
            var waterSource=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StylizedWaterShader/WaterPlane.prefab");
            var sea=new GameObject("MediterraneanSea",typeof(MeshFilter),typeof(MeshRenderer));sea.transform.SetParent(coast,false);
            var mesh=waterSource.GetComponent<MeshFilter>().sharedMesh; sea.GetComponent<MeshFilter>().sharedMesh=mesh;
            var b=mesh.bounds;sea.transform.localScale=new Vector3(260/b.size.x,1,210/b.size.z);
            sea.transform.localPosition=new Vector3(-b.center.x*sea.transform.localScale.x,-.58f-b.center.y,125-b.center.z*sea.transform.localScale.z);
            sea.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(CoastalAssetSetup.Root+"/MediterraneanWater.mat");
            sea.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Shape("PromenadeFoundation",coast,PrimitiveType.Cube,new Vector3(-23,-.15f,8),new Vector3(5,.25f,29),stone);
            for(int i=0;i<58;i++) Shape("BoardwalkPlank",coast,PrimitiveType.Cube,new Vector3(-23,.015f,-6.25f+i*.5f),new Vector3(4.9f,.08f,.46f),timber);
            Shape("BeachAccess",coast,PrimitiveType.Cube,new Vector3(-18.6f,-.05f,21),new Vector3(10.5f,.15f,3.2f),timber);
            for(int i=0;i<5;i++)
            {
                float z=-3+i*5.6f;
                Shape("PromenadePost",coast,PrimitiveType.Cube,new Vector3(-25.4f,.65f,z),new Vector3(.16f,1.2f,.16f),timber);
                Shape("PromenadeRail",coast,PrimitiveType.Cube,new Vector3(-25.4f,.95f,z+2.8f),new Vector3(.1f,.12f,5.6f),timber);
            }
            Prop("SM_Prop_Outdoor_Seat_01",coast,new Vector3(-23.7f,0,13),2.2f,90);
            for(int i=0;i<4;i++) Palm(coast,new Vector3(-27-i%2*3,0,-2+i*7),i*41);
            for(int i=0;i<8;i++)
            {
                var rock=Shape("LimestoneRock",coast,PrimitiveType.Sphere,new Vector3(10+i*3,-.1f,23+Mathf.Sin(i)*2),new Vector3(2.8f,1.2f,2.2f),stone);
                rock.transform.localRotation=Quaternion.Euler(i*23,i*47,0);
            }
            Sign(coast,"HabitatSign",new Vector3(-15.6f,1.15f,23.1f),0,2.7f,1,"YAŞAM ALANINI KORU\nMesafeni koru • Işık tutma\nAtığını kıyıda bırakma");
            Turtles(coast);
        }
        private static void Turtles(Transform parent)
        {
            string controllerPath="Assets/Data/Cinematic/Caretta.controller";Directory.CreateDirectory("Assets/Data/Cinematic");AssetDatabase.Refresh();
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if(controller.layers.Length==0)controller.AddLayer("Caretta motion");
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Outside/Mesh/caretta caretta.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.Select(s=>s.state).SingleOrDefault(s=>s.name=="Esmanur / caretta swim");
            if(!state)state=machine.AddState("Esmanur / caretta swim");
            state.motion=clip;machine.defaultState=state;
            EditorUtility.SetDirty(state);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);
            for(int i=0;i<5;i++)
            {
                var turtle=new GameObject("Caretta_"+(i+1));turtle.transform.SetParent(parent,false);
                var visual=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Outside/caretta caretta.prefab"),turtle.transform,Vector3.zero,.85f+i*.035f);
                var skeleton=visual.GetComponentsInChildren<Transform>();
                var head=skeleton.Single(t=>t.name=="head");var tail=skeleton.Single(t=>t.name=="tail1");
                var nativeForward=Vector3.ProjectOnPlane(head.position-tail.position,Vector3.up).normalized;
                visual.transform.localRotation=Quaternion.Euler(0,Vector3.SignedAngle(nativeForward,Vector3.forward,Vector3.up),0);
                var animator=visual.transform.GetChild(0).gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=.8f+i*.09f;
                var mover=turtle.AddComponent<ShorelineTurtle>();var so=new SerializedObject(mover);
                var start=new Vector3(-13.2f+i*1.25f,-.15f,20+i%2*.6f);
                var entry=new Vector3(start.x+.6f,-.82f,32.6f+i%2*.6f);
                turtle.transform.localPosition=start;
                so.FindProperty("beachStart").vector3Value=start;so.FindProperty("waterEntry").vector3Value=entry;
                so.FindProperty("swimCenter").vector3Value=entry+new Vector3(3.5f+i*.25f,0,0);
                so.FindProperty("phaseOffset").floatValue=i*3-18;
                so.FindProperty("crawlSeconds").floatValue=26;so.FindProperty("swimSpeed").floatValue=.08f+i*.008f;
                so.ApplyModifiedPropertiesWithoutUndo();CoastalAssetSetup.Outline(turtle);
            }
        }
        private static void Palm(Transform parent,Vector3 position,float yaw)
        {
            var palm=new GameObject("MediterraneanPalm").transform;palm.SetParent(parent,false);palm.localPosition=position;palm.localRotation=Quaternion.Euler(0,yaw,0);
            var bark=Material("PalmBark",new Color(.4f,.32f,.2f));var leaf=Material("PalmLeaves",new Color(.17f,.36f,.19f));
            Shape("Trunk",palm,PrimitiveType.Cylinder,new Vector3(0,2.3f,0),new Vector3(.3f,2.3f,.3f),bark);
            for(int i=0;i<7;i++)
            {
                float angle=i*Mathf.PI*2/7;var frond=Shape("Frond",palm,PrimitiveType.Sphere,new Vector3(Mathf.Sin(angle)*1.05f,4.35f,Mathf.Cos(angle)*1.05f),new Vector3(.52f,.14f,2.6f),leaf);
                frond.transform.localRotation=Quaternion.Euler(18,i*360f/7,0);
            }
        }
        internal static GameObject Prop(string name,Transform parent,Vector3 position,float size,float yaw=0)
        {
            var path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/PolygonOffice/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Single(p=>Path.GetFileNameWithoutExtension(p)==name);
            var prop=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent,position,size,yaw);
            CoastalAssetSetup.Outline(prop);return prop;
        }
        private static void Wall(Transform parent,Vector3 center,float width,float height,float yaw)
            =>Fitted(Buildings+"SM_Bld_Wall_Blank_01.prefab",parent,center,new Vector3(width,height,.18f),yaw);
        private static GameObject Fitted(string path,Transform parent,Vector3 center,Vector3 size,float yaw=0)
        {
            var root=new GameObject(Path.GetFileNameWithoutExtension(path));root.transform.SetParent(parent,false);root.transform.localPosition=center;root.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new FileNotFoundException(path);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].localBounds;
            // Building meshes are authored directly on the root, with stable local pivots.
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.localBounds);
            if(path.Contains("Wall"))
            {
                string materialPath=CoastalAssetSetup.Root+"/FacilityWallDoubleSided.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(renderers[0].sharedMaterial);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,materialPath);}
                foreach(var renderer in renderers)renderer.sharedMaterial=material;
            }
            var scale=new Vector3(size.x/Mathf.Max(bounds.size.x,.01f),size.y/Mathf.Max(bounds.size.y,.01f),size.z/Mathf.Max(bounds.size.z,.01f));
            model.transform.localScale=scale;model.transform.localPosition=-Vector3.Scale(bounds.center,scale);
            return root;
        }
        internal static void Sign(Transform parent,string name,Vector3 position,float yaw,float width,float height,string message)
        {
            var panel=new GameObject(name,typeof(RectTransform),typeof(Canvas)).GetComponent<RectTransform>();panel.SetParent(parent,false);
            panel.localPosition=position;panel.localRotation=Quaternion.Euler(0,yaw,0);panel.localScale=Vector3.one*(width/1200);panel.sizeDelta=new Vector2(1200,height/width*1200);
            panel.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            UIAuthoring.Image("Panel",panel,0,0,1200,panel.sizeDelta.y,new Color(.025f,.17f,.2f));
            UIAuthoring.Text("Message",panel,message,0,0,1120,panel.sizeDelta.y-20,42,UIAuthoring.Paper,TextAnchor.MiddleCenter);
        }
        private static void SaveMesh(Mesh mesh,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);
        }
    }
}
