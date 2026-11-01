using System;
using System.IO;
using System.Linq;
using GameUp.App;
using GameUp.Gameplay;
using GameUP.Contracts;
using GameUP.Interaction;
using GameUP.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class ProductionProjectSetup
    {
        public const string FirstPath="Assets/Scenes/First.unity", MenuPath="Assets/Scenes/Menu.unity", GamePath="Assets/Scenes/Game.unity";
        internal const string Art="Assets/Art/Workstation", Products="Assets/Prefabs/Products";
        internal static DayDefinition[] Days()
        {
            var campaign=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(CampaignAuthoring.CatalogPath);
            if(!campaign||campaign.Days.Count==0)throw new InvalidOperationException("Assign the saved category campaign.");
            return campaign.Days.ToArray();
        }

        [InitializeOnLoadMethod]
        private static void ConfigureEditorStart() => EditorApplication.delayCall += () =>
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstPath);

        [MenuItem("GameUP32/Authoring/Rebuild First, Menu and Game")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop play mode before authoring.");
            foreach(var dir in new[]{Art,Products,"Assets/Data/Application","Assets/Audio"}) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            foreach(var file in new[]{"AntalyaHarbour","CarettaCoast","AntalyaHeat","COP31Horizontal","SharedButtonLight"})
                ImportSprite("Assets/Art/Menu/"+file+".png",file=="AntalyaHarbour"?2048:file=="COP31Horizontal"?1024:1024);
            ImportSprite("Assets/Art/Documents/Newsprint.png",2048);
            CampaignAuthoring.Create(); CreateNotification(); ProductAuthoring.Create(Days());
            var defaults=AssetDatabase.LoadAssetAtPath<UserSettingsDefaults>("Assets/Data/Application/SettingsDefaults.asset");
            if(!defaults){defaults=ScriptableObject.CreateInstance<UserSettingsDefaults>();AssetDatabase.CreateAsset(defaults,"Assets/Data/Application/SettingsDefaults.asset");}
            AssetDatabase.SaveAssets(); First(); Menu(); WorkstationAuthoring.Game();
            EditorBuildSettings.scenes=new[]{FirstPath,MenuPath,GamePath}.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstPath);
            PlayerSettings.companyName="GameUP"; PlayerSettings.productName="Antalya Denetim Masası";
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=false;
            AssetDatabase.SaveAssets(); ArtistModelAudit.Generate(); EditorSceneManager.OpenScene(FirstPath); Debug.Log("PRODUCTION_AUTHORING_OK First -> Menu -> Game");
        }
        [MenuItem("GameUP32/Authoring/Rebuild Game room and newspaper")]
        public static void AuthorGame()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop play mode before authoring.");
            ImportSprite("Assets/Art/Documents/Newsprint.png",2048);
            CampaignAuthoring.Create();ProductAuthoring.Create(Days());
            WorkstationAuthoring.Game();AssetDatabase.SaveAssets();ArtistModelAudit.Generate();
            EditorSceneManager.OpenScene(FirstPath);Debug.Log("GAME_AUTHORING_OK room, newspaper and artist model references");
        }
        private static void ImportSprite(string path,int size)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(!importer)throw new FileNotFoundException(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=size;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.alphaIsTransparency=true; importer.SaveAndReimport();
        }
        private static void First()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var defaults=AssetDatabase.LoadAssetAtPath<UserSettingsDefaults>("Assets/Data/Application/SettingsDefaults.asset");
            var root=new GameObject("Application");var app=root.AddComponent<ApplicationRoot>();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(root.transform);
            var module=events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
            var loading=Canvas("Loading",root.transform,100);var group=loading.gameObject.AddComponent<CanvasGroup>();
            Overlay("Background",loading.transform,Ink);
            Text("Title",loading.transform,"Antalya\nDenetim Masası",0,65,1000,250,80,Paper,TextAnchor.MiddleCenter,true);
            Text("LoadingLabel",loading.transform,"COP31  •  Görev hazırlanıyor…",0,-150,1000,90,30,Paper,TextAnchor.MiddleCenter);
            Set(app,"defaults",defaults);Set(app,"loadingScreen",group);AudioAuthoring.Configure(root.transform); EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),FirstPath);
        }
        private static void Menu() => DocumentMenuAuthoring.CreateScene();
        private static void CreateNotification()
        {
            string path="Assets/Audio/FaxSignal.wav";const int sampleRate=22050,count=5512;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(sampleRate);writer.Write(sampleRate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++){float t=(float)i/sampleRate;float envelope=Mathf.Sin(Mathf.PI*i/count);writer.Write((short)(Mathf.Sin(t*880*2*Mathf.PI)*envelope*4000));}
            }
            AssetDatabase.ImportAsset(path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var sample=importer.defaultSampleSettings;
            sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
        }
        [MenuItem("GameUP32/Build/Windows")]
        public static void BuildWindows()
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FirstPath,MenuPath,GamePath},
                locationPathName="Builds/Windows/AntalyaDenetimMasasi.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed.");
            Debug.Log("PRODUCTION_BUILD_OK "+report.summary.totalSize);
        }
    }
}
