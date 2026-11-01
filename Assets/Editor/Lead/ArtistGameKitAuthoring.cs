using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class ArtistGameKitAuthoring
    {
        public const string RootName="Esmanur_GameKit";
        private const float Tabletop=.72f;
        [MenuItem("GameUP32/Art/Refresh art frames and Esmanur game kit")]
        public static void Refresh()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var office=scene.GetRootGameObjects().Single(r=>r.name=="InspectionOffice");
            int frames=0;
            var artworks=office.GetComponentsInChildren<Transform>(true).Where(t=>
                (t.name.EndsWith("Portrait",StringComparison.Ordinal)&&t.Find("PortraitPrint"))||
                (t.name.EndsWith("Poster",StringComparison.Ordinal)&&t.Find("Print"))).ToArray();
            foreach(var root in artworks)
            {
                var print=root.Find("PortraitPrint")??root.Find("Print");
                if(!print||!(print is RectTransform rect))continue;
                if(root.name.EndsWith("Portrait",StringComparison.Ordinal)||root.name.EndsWith("Poster",StringComparison.Ordinal))
                {WallArtFrameAuthoring.Refresh(root,rect);frames++;}
            }
            var old=scene.GetRootGameObjects().SingleOrDefault(r=>r.name==RootName);if(old)UnityEngine.Object.DestroyImmediate(old);
            CreateKit();
            foreach(var camera in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)))camera.cullingMask&=~(1<<30);
            FacilityPropAuthoring.Inventory();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("ART_FRAMES_AND_KIT_OK frames="+frames+" products=31 environment=16 caretta=1 / EditorOnly");
        }
        private static void CreateKit()
        {
            var root=new GameObject(RootName).transform;root.position=new Vector3(0,0,-27);
            FacilityPropAuthoring.Pack("SM_Bld_Floor_Concrete_01",root,new Vector3(0,-.04f,-1.5f),new Vector3(40,.12f,31));
            var products=new GameObject("Products_31").transform;products.SetParent(root,false);
            var sources=AssetDatabase.FindAssets("t:Prefab",new[]{RoomAuthoring.ArtistRoot}).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).OrderBy(g=>g.name,StringComparer.Ordinal).ToArray();
            if(sources.Length!=31)throw new InvalidOperationException("The review kit expects the verified 31 product sources.");
            var groups=new[]{
                (name:"Süt",items:sources.Where(g=>g.name.StartsWith("milk",StringComparison.Ordinal)).ToArray()),
                (name:"Peynir",items:sources.Where(g=>g.name.StartsWith("cheese",StringComparison.Ordinal)).ToArray()),
                (name:"Yoğurt ve yumurta",items:sources.Where(g=>g.name.StartsWith("Yoghurt",StringComparison.Ordinal)||g.name=="Eggs").ToArray()),
                (name:"Et",items:sources.Where(g=>g.name.StartsWith("meat",StringComparison.Ordinal)).ToArray()),
                (name:"Meyve",items:sources.Where(g=>CampaignAuthoring.Category(g.name)==GameUP.Contracts.ProductCategory.Fruit).ToArray())};
            int row=0,count=0;
            foreach(var group in groups)
            {
                var category=new GameObject(group.name).transform;category.SetParent(products,false);
                for(int i=0;i<group.items.Length;i++)
                {
                    var position=new Vector3(-17+i%6*2.2f,0,10-(row+i/6)*3.4f);
                    DisplayTable(category,group.items[i].name,position,new Vector2(1.8f,1.5f));
                    var model=ProductAuthoring.PlaceModel(group.items[i],category,position+Vector3.up*Tabletop,.9f);
                    StripColliders(model);Label(category,group.items[i].name,position+new Vector3(0,.5f,.78f),1.9f,.2f,32);count++;
                }
                row+=Mathf.CeilToInt(group.items.Length/6f);
            }
            if(count!=sources.Length)throw new InvalidOperationException("Unclassified artist product in the game kit.");
            var environment=new GameObject("Environment_16").transform;environment.SetParent(root,false);
            var parts=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityPropAuthoring.Artist).GetComponentsInChildren<MeshFilter>().OrderBy(f=>f.name,StringComparer.Ordinal).ToArray();
            for(int i=0;i<parts.Length;i++)
            {
                var position=new Vector3(1+i%4*4.2f,0,10-i/4*4.8f);string name=parts[i].name;
                DisplayTable(environment,name,position,new Vector2(3.4f,2.3f));
                var size=parts[i].sharedMesh.bounds.size;
                float maximum=name=="desk"?2.8f:name.StartsWith("donen",StringComparison.Ordinal)?2.5f:name.EndsWith("signal",StringComparison.Ordinal)?.4f:1.25f;
                float scale=maximum/Mathf.Max(size.x,Mathf.Max(size.y,size.z));
                var fitted=new Vector3(Mathf.Max(.005f,size.x*scale),Mathf.Max(.005f,size.y*scale),Mathf.Max(.005f,size.z*scale));
                FacilityPropAuthoring.Esma(name,environment,position+Vector3.up*(Tabletop+fitted.y*.5f),fitted);
                Label(environment,name,position+new Vector3(0,.5f,1.18f),3.2f,.24f,34);
            }
            var bins=new GameObject("UpdatedBins_4").transform;bins.SetParent(root,false);
            for(int i=0;i<4;i++)
            {
                var position=new Vector3(1+i*4.2f,0,-14);DisplayTable(bins,RecyclingBinAuthoring.Names[i],position,new Vector2(3.4f,2.3f));
                RecyclingBinAuthoring.Place(i,bins,position+Vector3.up*(Tabletop+.65f),new Vector3(.68f,1.3f,.65f));
                Label(bins,RecyclingBinAuthoring.Names[i],position+new Vector3(0,.5f,1.18f),3.2f,.24f,32);
            }
            var carettaGroup=new GameObject("Caretta_1").transform;carettaGroup.SetParent(root,false);var carettaPosition=new Vector3(9.4f,0,-9.4f);
            DisplayTable(carettaGroup,"Caretta caretta",carettaPosition,new Vector2(3.4f,2.3f));
            var caretta=ProductAuthoring.PlaceModel(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Outside/caretta caretta.prefab"),carettaGroup,carettaPosition+Vector3.up*Tabletop,1.65f);
            StripColliders(caretta);
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Outside/Mesh/caretta caretta.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal));
            clip.SampleAnimation(caretta.transform.GetChild(0).gameObject,.45f);
            var bones=caretta.GetComponentsInChildren<Transform>();var forward=Vector3.ProjectOnPlane(bones.Single(t=>t.name=="head").position-bones.Single(t=>t.name=="tail1").position,Vector3.up).normalized;
            caretta.transform.localRotation=Quaternion.Euler(0,Vector3.SignedAngle(forward,Vector3.forward,Vector3.up),0);
            Label(carettaGroup,"caretta caretta / örnek poz",carettaPosition+new Vector3(0,.5f,1.18f),3.2f,.24f,32);
            Label(root,"ESMANUR  /  GAME KIT\n31 ürün  •  16 çevre parçası  •  4 yeni kutu  •  caretta",new Vector3(-3.5f,2.3f,13),13,1.6f,28);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.tag="EditorOnly";t.gameObject.layer=30;
                if(!t.GetComponentInParent<SkinnedMeshRenderer>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            Debug.Log("ARTIST_KIT_CONTENTS products="+count+" environment="+parts.Length+" caretta=1 position="+root.position);
        }
        private static void DisplayTable(Transform parent,string name,Vector3 position,Vector2 size)
        {
            var table=FacilityPropAuthoring.Pack("SM_Prop_Table_01",parent,position+Vector3.up*.37f,new Vector3(size.x,.7f,size.y));table.name="Display_"+name;
        }
        private static void StripColliders(GameObject model)
        {foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);}
        private static void Label(Transform parent,string text,Vector3 position,float width,float height,int size)
        {
            var canvas=Rect("Label_"+text,parent,0,0,600,height/width*600);canvas.localPosition=position;canvas.localScale=Vector3.one*(width/600);canvas.localRotation=Quaternion.Euler(0,180,0);
            canvas.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            Image("Background",canvas,0,0,600,canvas.sizeDelta.y,new Color(.025f,.12f,.14f));
            Text("ModelName",canvas,text,0,0,580,canvas.sizeDelta.y,size,Paper,TextAnchor.MiddleCenter);
        }
        [MenuItem("GameUP32/Art/Focus Esmanur game kit")]
        public static void Focus()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ProductionProjectSetup.GamePath)throw new InvalidOperationException("Open the saved Game scene to focus the kit.");
            var root=scene.GetRootGameObjects().Single(r=>r.name==RootName);var renderers=root.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            bounds.Expand(3);Selection.activeGameObject=root;var view=SceneView.GetWindow<SceneView>();view.rotation=Quaternion.Euler(45,-135,0);view.Frame(bounds,false);
        }
        public static void RefreshAndCapture(){Refresh();ArtKitReview.CaptureKit();CoastalReview.Capture();}
    }
}
