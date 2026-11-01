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
        private const string EnvRoot = "Assets/Prefabs/Environment/";

        internal static void Game()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var days=ProductionProjectSetup.Days();
            var systems=new GameObject("GameSystems");var backend=systems.AddComponent<ProductionInspectionBackend>();backend.SetConfiguredDays(days);
            var session=systems.AddComponent<GameSessionController>();Set(session,"backend",backend);
            var presentation=systems.AddComponent<ProductPresentation>();Set(session,"presentationSource",presentation);
            var architecture=new GameObject("InspectionOffice").transform;
            RoomAuthoring.Create(architecture);
            var wood=Material("Wood",new Color(.43f,.28f,.16f));var pale=Material("Linen",Paper);
            var teal=Material("Teal",new Color(.08f,.28f,.29f));var metal=Material("Metal",new Color(.22f,.3f,.31f),.3f);
            var green=Material("Accept",new Color(.08f,.55f,.36f));var red=Material("Reject",new Color(.65f,.17f,.12f));
            var handbookCover=Material("HandbookCover",new Color(.56f,.35f,.15f));

            var deskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "desk.prefab");
            if (deskPrefab) PlaceFittedBox(deskPrefab, architecture, "Desk", new Vector3(0, 0f, .1f), new Vector3(6.6f, 1.03f, 2.6f));
            else
            {
                Shape("Desk",architecture,PrimitiveType.Cube,new Vector3(0,.92f,.1f),new Vector3(7,.22f,2.7f),wood);
                foreach(float x in new[]{-3f,3f})Shape("DeskLeg",architecture,PrimitiveType.Cube,new Vector3(x,.4f,.7f),new Vector3(.2f,1,.2f),teal);
            }

            var conveyorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "donen alet yanları.prefab");
            if (conveyorPrefab) PlaceFittedBox(conveyorPrefab, architecture, "ConveyorBelt3D", new Vector3(0, 1.01f, .25f), new Vector3(3.6f, .11f, 1.05f));
            else
            {
                Shape("Conveyor",architecture,PrimitiveType.Cube,new Vector3(0,1.06f,.25f),new Vector3(3.6f,.08f,1.05f),metal);
                for(int i=-6;i<=6;i++)Shape("BeltSeam",architecture,PrimitiveType.Cube,new Vector3(i*.27f,1.105f,.25f),new Vector3(.012f,.008f,1),teal);
            }

            var arrival=Point("Arrival",new Vector3(-1.5f,1.12f,.25f));var desk=Point("InspectionDesk",new Vector3(0,1.12f,.25f));
            var approved=Point("MarketExit",new Vector3(2.1f,1.12f,.25f));var disposal=Point("FoodDisposal",new Vector3(3.15f,.65f,-.5f));
            Set(presentation,"arrivalPoint",arrival);Set(presentation,"deskPoint",desk);Set(presentation,"approvedExitPoint",approved);Set(presentation,"foodDisposalPoint",disposal);
            Target("Approve",architecture,new Vector3(.95f,1.14f,-.75f),new Vector3(.58f,.18f,.42f),green,WorkstationAction.Approve,"W / ONAY");
            Target("Reject",architecture,new Vector3(1.68f,1.14f,-.75f),new Vector3(.58f,.18f,.42f),red,WorkstationAction.Reject,"R / RED");

            var xrayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "xray.prefab");
            TargetModel("Analyzer",architecture,new Vector3(-2.12f,1.37f,.35f),new Vector3(1,.68f,.9f),xrayPrefab,.88f,90f,teal,WorkstationAction.Analyzer,"F / ANALİZ",new Vector3(0,.42f,-.38f));

            var openBookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "openBook.prefab");
            var closedBookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "closedBook.prefab");
            var closedNewsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "Closed_NewsPaper.prefab");
            var handbookObject = TargetModel("Handbook",architecture,new Vector3(-1.18f,1.12f,-.75f),new Vector3(.68f,.22f,.48f),openBookPrefab,.56f,0f,handbookCover,WorkstationAction.Handbook,"B / KİTAPÇIK & REHBER",new Vector3(.28f,.17f,-.24f));
            if (closedBookPrefab)
            {
                var closedBook = PlaceModel(closedBookPrefab, handbookObject.transform, new Vector3(.63f, -.085f, -.07f), .38f, 12f);
                closedBook.layer = 8;
                var bookCollider = closedBook.AddComponent<BoxCollider>();
                bookCollider.center = new Vector3(0, .06f, 0);
                bookCollider.size = new Vector3(.42f, .18f, .42f);
            }
            if (closedNewsPrefab)
            {
                var foldedNews = PlaceModel(closedNewsPrefab, handbookObject.transform, new Vector3(3.43f, -.085f, 1.2f), .46f, -18f);
                foldedNews.layer = 8;
                var newsCollider = foldedNews.AddComponent<BoxCollider>();
                newsCollider.center = new Vector3(0, .05f, 0);
                newsCollider.size = new Vector3(.48f, .14f, .48f);
            }

            var faxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "fax.prefab");
            var faxObject=TargetModel("Fax",architecture,new Vector3(-2.05f,1.22f,-.78f),new Vector3(.85f,.38f,.5f),faxPrefab,.68f,0f,metal,WorkstationAction.Report,"T / SON RAPOR",new Vector3(0,.26f,-.24f));
            var fax=faxObject.AddComponent<FaxPresenter>();
            var paperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvRoot + "paper.prefab");
            var paper = paperPrefab
                ? PlaceModel(paperPrefab, architecture, new Vector3(-2.05f, 1.15f, -.74f), .28f, 0f)
                : Shape("FaxPaper",architecture,PrimitiveType.Cube,new Vector3(-2.05f,1.25f,-.78f),new Vector3(.42f,.015f,.3f),pale);
            paper.name = "FaxPaper";
            var indicator=new GameObject("FaxIndicator",typeof(Light)).GetComponent<Light>();indicator.transform.SetParent(faxObject.transform,false);indicator.transform.localPosition=new Vector3(0,1,-.8f);indicator.type=LightType.Point;indicator.range=1;indicator.intensity=0;
            var audio=faxObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
            Set(fax,"indicator",indicator);Set(fax,"paper",paper.transform);Set(fax,"speaker",audio);Set(fax,"notification",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/FaxSignal.wav"));Set(session,"fax",fax);

            var bins=RoomAuthoring.RecyclingStation(architecture);
            var ps=new SerializedObject(presentation);var points=ps.FindProperty("wastePoints");points.arraySize=4;for(int i=0;i<4;i++)points.GetArrayElementAtIndex(i).objectReferenceValue=bins[i];
            var definitions=days.SelectMany(d=>d.Products).ToArray();var pool=ps.FindProperty("visuals");pool.arraySize=definitions.Length;
            var productRoot=new GameObject("ProductPool").transform;
            for(int i=0;i<definitions.Length;i++)
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(definitions[i].Prefab);visual.transform.SetParent(productRoot,false);visual.SetActive(false);
                var entry=pool.GetArrayElementAtIndex(i);entry.FindPropertyRelative("Definition").objectReferenceValue=definitions[i];
                entry.FindPropertyRelative("Visual").objectReferenceValue=visual;entry.FindPropertyRelative("Labels").objectReferenceValue=visual.GetComponent<ProductWorldLabelBinder>();
            }
            ps.ApplyModifiedPropertiesWithoutUndo();
            var packaging=Shape("EmptyPackaging",productRoot,PrimitiveType.Cube,Vector3.zero,new Vector3(.45f,.16f,.45f),pale);packaging.SetActive(false);Set(presentation,"packagingVisual",packaging);
            var player=new GameObject("PlayerView").transform;player.position=new Vector3(0,2.35f,-3.2f);
            var camera=new GameObject("PlayerCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.transform.SetParent(player,false);camera.transform.localRotation=Quaternion.Euler(18,0,0);
            camera.tag="MainCamera";camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.67f,.8f,.82f);
            var anchor=new GameObject("InspectionAnchor").transform;anchor.SetParent(camera.transform,false);anchor.localPosition=new Vector3(-.22f,-.23f,1.15f);
            var input=player.gameObject.AddComponent<WorkstationInteractionController>();Set(input,"session",session);Set(input,"presentation",presentation);Set(input,"playerCamera",camera);Set(input,"cameraYawRoot",player);Set(input,"inspectionAnchor",anchor);
            var inputSo=new SerializedObject(input);inputSo.FindProperty("interactableLayers").intValue=1<<8;inputSo.FindProperty("rayDistance").floatValue=8;inputSo.ApplyModifiedPropertiesWithoutUndo();
            var sun=new GameObject("MediterraneanSun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.05f;sun.color=new Color(1,.94f,.83f);sun.transform.rotation=Quaternion.Euler(48,-25,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.61f,.68f,.68f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            GameInterfaceAuthoring.Create(session);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ProductionProjectSetup.GamePath);
        }
        private static Transform Point(string name,Vector3 position){var t=new GameObject(name).transform;t.position=position;return t;}
        private static GameObject Target(string name,Transform parent,Vector3 position,Vector3 scale,Material material,WorkstationAction action,string label)
        {
            var go=Shape(name,parent,PrimitiveType.Cube,position,scale,material,true);SetAction(go.AddComponent<WorkstationTarget>(),action);
            WorldText(name+"Label",parent,label,position+new Vector3(0,.12f,-scale.z*.5f-.02f),Mathf.Max(scale.x,.72f),.18f,Paper,42,false);return go;
        }
        private static GameObject TargetModel(string name,Transform parent,Vector3 position,Vector3 colliderSize,GameObject modelPrefab,float modelMaxSize,float yaw,Material fallbackMaterial,WorkstationAction action,string label,Vector3 labelOffset)
        {
            if (!modelPrefab) return Target(name, parent, position, colliderSize, fallbackMaterial, action, label);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var box = go.AddComponent<BoxCollider>();
            box.size = colliderSize;
            SetAction(go.AddComponent<WorkstationTarget>(), action);
            PlaceModel(modelPrefab, go.transform, new Vector3(0, -colliderSize.y * .5f, 0), modelMaxSize, yaw);
            WorldText(name+"Label",parent,label,position+labelOffset,Mathf.Max(.62f,label.Length*.044f),.15f,Paper,36,true);
            return go;
        }
        private static GameObject PlaceFittedBox(GameObject source, Transform parent, string name, Vector3 bottomCenter, Vector3 targetSize)
        {
            var container = new GameObject(name);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.transform.SetParent(container.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                if (bounds.size.z > bounds.size.x * 1.2f)
                {
                    visual.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                    bounds = renderers[0].bounds;
                    foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                }
                Vector3 scale = new Vector3(
                    targetSize.x / Mathf.Max(bounds.size.x, .01f),
                    targetSize.y / Mathf.Max(bounds.size.y, .01f),
                    targetSize.z / Mathf.Max(bounds.size.z, .01f));
                visual.transform.localScale = Vector3.Scale(visual.transform.localScale, scale);
                bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                visual.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            foreach (var c in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (container.scene != parent.gameObject.scene) SceneManager.MoveGameObjectToScene(container, parent.gameObject.scene);
            container.transform.SetParent(parent, false);
            container.transform.localPosition = bottomCenter;
            return container;
        }
        private static void WorldText(string name,Transform parent,string value,Vector3 position,float width,float height,Color color,int fontSize,bool badge)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localScale=Vector3.one*.002f;go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta=new Vector2(width*500,height*500);
            if(badge)Image("Badge",go.transform,0,0,width*500,height*500,new Color(.03f,.11f,.13f,.92f));
            Text("Text",go.transform,value,0,0,width*500,height*500,fontSize,color,TextAnchor.MiddleCenter);
        }
    }
}
