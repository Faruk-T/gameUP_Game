using System.Linq;
using GameUp.Gameplay;
using GameUP.Contracts;
using GameUP.Interaction;
using GameUP.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;
using static GameUp.Editor.ProductAuthoring;

namespace GameUp.Editor
{
    internal static class WorkstationAuthoring
    {
        internal static void Game()
        {
            CoastalAssetSetup.Configure();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var days=ProductionProjectSetup.Days();
            var systems=new GameObject("GameSystems");var backend=systems.AddComponent<ProductionInspectionBackend>();backend.SetConfiguredDays(days);
            var session=systems.AddComponent<GameSessionController>();Set(session,"backend",backend);
            var presentation=systems.AddComponent<ProductPresentation>();Set(session,"presentationSource",presentation);
            var architecture=new GameObject("InspectionOffice").transform;
            RoomAuthoring.Create(architecture);
            var pale=Material("Linen",Paper);
            var teal=Material("Teal",new Color(.08f,.28f,.29f));var metal=Material("Metal",new Color(.22f,.3f,.31f),.3f);
            var green=Material("Accept",new Color(.08f,.55f,.36f));var red=Material("Reject",new Color(.65f,.17f,.12f));
            FacilityPropAuthoring.Esma("desk",architecture,new Vector3(0,.52f,.1f),new Vector3(7,1.04f,2.7f));
            FacilityPropAuthoring.Esma("donen alet yanları",architecture,new Vector3(0,1.06f,.25f),new Vector3(1.05f,.1f,3.6f),90);
            FacilityPropAuthoring.Esma("donen alet",architecture,new Vector3(0,1.065f,.25f),new Vector3(.9f,.09f,3.55f),90);
            WorldText("BeltInstructions",architecture,"GELEN ÜRÜN   →   DENETİM   →   PİYASA",new Vector3(0,1.4f,1.1f),3.1f,.3f,Paper);

            var arrival=Point("Arrival",new Vector3(-1.5f,1.12f,.25f));var desk=Point("InspectionDesk",new Vector3(0,1.12f,.25f));
            var approved=Point("MarketExit",new Vector3(2.1f,1.12f,.25f));var disposal=Point("FoodDisposal",new Vector3(3.15f,.65f,-.5f));
            Set(presentation,"arrivalPoint",arrival);Set(presentation,"deskPoint",desk);Set(presentation,"approvedExitPoint",approved);Set(presentation,"foodDisposalPoint",disposal);
            Target("Approve",architecture,new Vector3(1.25f,1.18f,-.75f),new Vector3(.6f,.23f,.45f),green,WorkstationAction.Approve,"W / ONAY");
            Target("Reject",architecture,new Vector3(2.1f,1.18f,-.75f),new Vector3(.6f,.23f,.45f),red,WorkstationAction.Reject,"R / RED");
            Target("Analyzer",architecture,new Vector3(-2.4f,1.37f,.3f),new Vector3(1,.6f,.9f),teal,WorkstationAction.Analyzer,"F / ANALİZ");
            Target("Handbook",architecture,new Vector3(-1.4f,1.15f,-.8f),new Vector3(.65f,.12f,.45f),pale,WorkstationAction.Handbook,"B / KİTAPÇIK");
            var faxObject=Target("Fax",architecture,new Vector3(-2.55f,1.28f,-.85f),new Vector3(.9f,.45f,.5f),metal,WorkstationAction.Report,"T / SON RAPOR");
            var fax=faxObject.AddComponent<FaxPresenter>();var paper=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/paper.prefab"),architecture,new Vector3(-2.55f,1.53f,-.85f),.65f);
            var indicator=new GameObject("FaxIndicator",typeof(Light)).GetComponent<Light>();indicator.transform.SetParent(faxObject.transform,false);indicator.transform.localPosition=new Vector3(0,1,-.8f);indicator.type=LightType.Point;indicator.range=1;indicator.intensity=0;
            var audio=faxObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
            Set(fax,"indicator",indicator);Set(fax,"paper",paper.transform);Set(fax,"speaker",audio);Set(fax,"notification",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/FaxSignal.wav"));Set(session,"fax",fax);
            var openBook=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/openBook.prefab"),architecture,new Vector3(-1.4f,1.1f,-.8f),.65f);
            var newspaper=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Closed_NewsPaper.prefab"),architecture,new Vector3(-.45f,1.06f,-.8f),.58f);
            var masthead=new GameObject("FoldedNewspaperPrint",typeof(RectTransform),typeof(Canvas)).GetComponent<RectTransform>();masthead.SetParent(newspaper.transform,false);
            masthead.localPosition=new Vector3(0,.046f,0);masthead.localRotation=Quaternion.Euler(90,0,0);masthead.localScale=Vector3.one*.00038f;masthead.sizeDelta=new Vector2(1000,1300);masthead.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            Text("Masthead",masthead,"AKDENİZ POSTASI",0,300,940,170,80,Ink,TextAnchor.MiddleCenter,true);
            Text("Edition",masthead,"GIDA  •  İKLİM  •  YAŞAM",0,100,940,120,48,Ink,TextAnchor.MiddleCenter);
            var physicalDocuments=systems.AddComponent<WorkstationDocuments>();Set(physicalDocuments,"session",session);
            Set(physicalDocuments,"closedBook",architecture.Find("Model_closedBook").gameObject);Set(physicalDocuments,"openBook",openBook);Set(physicalDocuments,"newspaper",newspaper);openBook.SetActive(false);

            var bins=RoomAuthoring.RecyclingStation(architecture);
            var ps=new SerializedObject(presentation);var points=ps.FindProperty("wastePoints");points.arraySize=4;for(int i=0;i<4;i++)points.GetArrayElementAtIndex(i).objectReferenceValue=bins[i];
            var definitions=days.SelectMany(d=>d.Products).ToArray();var pool=ps.FindProperty("visuals");pool.arraySize=definitions.Length;
            var productRoot=new GameObject("ProductPool").transform;
            for(int i=0;i<definitions.Length;i++)
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(definitions[i].Prefab);visual.transform.SetParent(productRoot,false);CoastalAssetSetup.Outline(visual);visual.SetActive(false);
                var entry=pool.GetArrayElementAtIndex(i);entry.FindPropertyRelative("Definition").objectReferenceValue=definitions[i];
                entry.FindPropertyRelative("Visual").objectReferenceValue=visual;entry.FindPropertyRelative("Labels").objectReferenceValue=visual.GetComponent<ProductWorldLabelBinder>();
            }
            ps.ApplyModifiedPropertiesWithoutUndo();
            var packaging=FacilityPropAuthoring.Pack("SM_Prop_CardboardBox_01",productRoot,Vector3.zero,new Vector3(.45f,.16f,.45f));packaging.name="EmptyPackaging";packaging.SetActive(false);Set(presentation,"packagingVisual",packaging);
            var player=new GameObject("PlayerView").transform;player.position=new Vector3(0,2.35f,-3.2f);
            var camera=new GameObject("PlayerCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.transform.SetParent(player,false);camera.transform.localRotation=Quaternion.Euler(18,0,0);
            camera.tag="MainCamera";camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.67f,.8f,.82f);
            var anchor=new GameObject("InspectionAnchor").transform;anchor.SetParent(camera.transform,false);anchor.localPosition=new Vector3(-.22f,-.23f,1.15f);
            var input=player.gameObject.AddComponent<WorkstationInteractionController>();Set(input,"session",session);Set(input,"presentation",presentation);Set(input,"playerCamera",camera);Set(input,"cameraYawRoot",player);Set(input,"inspectionAnchor",anchor);
            var inputSo=new SerializedObject(input);inputSo.FindProperty("interactableLayers").intValue=1<<8;inputSo.FindProperty("rayDistance").floatValue=8;inputSo.ApplyModifiedPropertiesWithoutUndo();
            var sun=new GameObject("MediterraneanSun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.05f;sun.color=new Color(1,.94f,.83f);sun.transform.rotation=Quaternion.Euler(48,-25,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.61f,.68f,.68f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.0015f;RenderSettings.fogColor=new Color(.62f,.79f,.82f);
            var gameCanvas=GameInterfaceAuthoring.Create(session);
            CinematicAuthoring.Create(session,camera,gameCanvas);
            SceneArtCorrections.Apply();
            indicator.transform.position=architecture.GetComponentsInChildren<Renderer>().Single(r=>r.name=="fax signal").bounds.center+Vector3.up*.02f;indicator.range=.3f;
            FacilityPropAuthoring.Inventory();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ProductionProjectSetup.GamePath);
        }
        private static Transform Point(string name,Vector3 position){var t=new GameObject(name).transform;t.position=position;return t;}
        private static GameObject Target(string name,Transform parent,Vector3 position,Vector3 scale,Material material,WorkstationAction action,string label)
        {
            var go=new GameObject(name,typeof(BoxCollider));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.GetComponent<BoxCollider>().size=scale;
            SetAction(go.AddComponent<WorkstationTarget>(),action);
            string artistPrefab=action==WorkstationAction.Analyzer?"xray":action==WorkstationAction.Handbook?"closedBook":action==WorkstationAction.Report?"fax":null;
            if(artistPrefab!=null)
            {
                var model=PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/"+artistPrefab+".prefab"),parent,position-Vector3.up*scale.y*.5f,Mathf.Max(scale.x,scale.z));
                CoastalAssetSetup.Outline(model);
            }
            else
            {
                var model=FacilityPropAuthoring.Pack("SM_Prop_Elevator_Button_01",parent,position,new Vector3(scale.x,scale.z,.12f),0,material);
                model.transform.localRotation=Quaternion.Euler(90,0,0);CoastalAssetSetup.Outline(model);
            }
            WorldText(name+"Label",parent,label,position+new Vector3(0,.15f,-scale.z*.5f-.02f),Mathf.Max(scale.x,.8f),.3f,Paper);return go;
        }
        private static void WorldText(string name,Transform parent,string value,Vector3 position,float width,float height,Color color)
        {
            if(name.EndsWith("Label",System.StringComparison.Ordinal))
            {
                position.y=.89f;position.z=-1.29f;height=.15f;width=.8f;
                if(name=="FaxLabel"){position.x=-3.17f;width=.52f;}
                if(name=="AnalyzerLabel")position.x=-2.32f;
                if(name=="HandbookLabel")position.x=-1.3f;
                if(name=="ApproveLabel"||name=="RejectLabel")width=.65f;
            }
            FacilityPropAuthoring.Sign(parent,name,position,0,width,height,value);
        }
    }
}
