using System;
using System.Linq;
using GameUp.App;
using GameUp.Gameplay;
using GameUP.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class WorkstationUpgradeAuthoring
    {
        public static void InspectSources()
        {
            RecyclingBinAuthoring.RemapSource();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Mesh/Trashbin.fbx");
            foreach(var mesh in source.GetComponentsInChildren<MeshFilter>())Debug.Log("NEW_BIN_SOURCE name="+mesh.name+" size="+mesh.sharedMesh.bounds.size+" materials="+string.Join(",",mesh.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name)));
            foreach(var path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio"}).Select(AssetDatabase.GUIDToAssetPath))
            {var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);Debug.Log("AUDIO_SOURCE "+path+" seconds="+clip.length);}
        }
        [MenuItem("GameUP32/Authoring/Upgrade workstation, labels and audio")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
            var first=EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            AudioAuthoring.Configure(first.GetRootGameObjects().Single(g=>g.GetComponent<ApplicationRoot>()).transform);
            EditorSceneManager.MarkSceneDirty(first);EditorSceneManager.SaveScene(first);
            CampaignAuthoring.Create();
            ProductLabelAuthoring.RefreshCampaign();
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots=scene.GetRootGameObjects();var office=roots.Single(g=>g.name=="InspectionOffice").transform;
            foreach(var listener in roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)))UnityEngine.Object.DestroyImmediate(listener);
            var session=roots.SelectMany(g=>g.GetComponentsInChildren<GameSessionController>(true)).Single();
            var presentation=session.GetComponent<ProductPresentation>();
            ConveyorAuthoring.Apply(office,presentation);FaxAuthoring.Apply(office,session);
            RecyclingBinAuthoring.Apply(office);
            Number(session.Backend,"analysisSeconds",AudioAuthoring.Clip("analyzer_full.mp3").length);
            WorkstationUIAuthoring.Apply(roots.Single(g=>g.name=="GameCanvas"));
            FacilityPropAuthoring.Inventory();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var menu=EditorSceneManager.OpenScene(ProductionProjectSetup.MenuPath);
            foreach(var root in menu.GetRootGameObjects())WorkstationUIAuthoring.AddButtonSounds(root);
            var help=menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Text>(true)).Single(t=>t.name=="HelpBody");
            if(!help.text.Contains("Üç yanlış"))help.text+="\n\nÜç yanlış işlem aynı gün vardiyayı bitirir. Sayaç her sabah sıfırlanır. İncelemede kamera sabittir; ürünün ön ve arka etiketini çevirerek okuyun.";
            EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);AssetDatabase.SaveAssets();
            ArtistGameKitAuthoring.Refresh();
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("WORKSTATION_UPGRADE_OK / physical ministry fax / daily 3 mistakes / right-hand 32m conveyor / 31 baked labels / authored AudioManager");
        }
        public static void ApplyAndReview(){Apply();WorkstationReview.Capture();}
        public static void RefreshLabelSurfacesAndReview(){ProductLabelAuthoring.RefreshCampaign(false,false);WorkstationReview.Capture();}
        public static void FinalizeArtAndReview()
        {
            ProductLabelAuthoring.RefreshCampaign();
            ApplyFinishingDetails();
            WorkstationReview.Capture();
        }
        private static void ApplyFinishingDetails()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots=scene.GetRootGameObjects();var office=roots.Single(g=>g.name=="InspectionOffice").transform;
            var session=roots.SelectMany(r=>r.GetComponentsInChildren<GameSessionController>(true)).Single();
            RecyclingBinAuthoring.Apply(office);FaxAuthoring.Apply(office,session);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
