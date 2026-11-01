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
        private static void ConfigureEditorStart()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool("GameUP32_EnvModels_v7", false))
                {
                    SessionState.SetBool("GameUP32_EnvModels_v7", true);
                    AuthorGame();
                }
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstPath);
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && !SessionState.GetBool("GameUP32_EnvModels_v7", false))
                {
                    SessionState.SetBool("GameUP32_EnvModels_v7", true);
                    AuthorGame();
                }
            };
        }

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
            AssetDatabase.Refresh();
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
            Set(app,"defaults",defaults);Set(app,"loadingScreen",group); EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),FirstPath);
        }
        private static void Menu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var canvas=Canvas("MenuCanvas");
            var bg=Image("Antalya",canvas.transform,0,0,1920,1080,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/AntalyaHarbour.png"));
            var fit=bg.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
            var shade=Image("MenuShade",canvas.transform,-570,0,780,1080,new Color(.025f,.09f,.1f,.88f));
            var controller=new GameObject("MenuController").AddComponent<MenuController>();
            var home=Rect("Home",canvas.transform,0,0,1920,1080);Stretch(home);
            Text("Eyebrow",home,"COP31 / ANTALYA",-570,425,600,60,26,new Color(.45f,.83f,.73f));
            Text("Title",home,"Antalya\nDenetim Masası",-570,285,600,220,70,Paper,TextAnchor.UpperLeft,true);
            Text("Subtitle",home,"Bir doğru karar, daha temiz bir kıyı.",-570,135,600,70,27,Paper);
            var start=Button("Start",home,"Göreve başla",-570,20,550,controller.StartGame);
            Button("Settings",home,"Ayarlar",-570,-64,550,controller.ShowSettings);
            Button("Help",home,"Nasıl oynanır?",-570,-148,550,controller.ShowHelp);
            Button("About",home,"COP31 & ekip",-570,-232,550,controller.ShowAbout);
            Button("Quit",home,"Çıkış",-570,-316,550,controller.Quit);
            Text("Dates",home,"9–20 KASIM 2026  •  ANTALYA, TÜRKİYE",-570,-460,600,50,23,Paper);
            // The original official logo keeps its aspect ratio and colors, including its white clear space.
            Image("LogoPaper",home,650,350,330,150,Color.white);
            var logo=Image("OfficialCOP31",home,650,350,570,321,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/COP31Horizontal.png"));logo.preserveAspect=true;
            var settings=Settings(canvas.transform);
            var help=Overlay("Help",canvas.transform,new Color(.025f,.08f,.09f,.97f));
            Text("HelpTitle",help.transform,"Denetçi rehberi",0,380,1250,100,64,Paper,TextAnchor.MiddleLeft,true);
            Text("HelpBody",help.transform,"Her sabah gazeteyi oku; o günün kurallarıyla çalış.\n\nE / Sol tık  —  Ürünü incele. Sürükleyerek döndür; tekerlekle yaklaş.\nB  —  El kitapçığı       F  —  Laboratuvar analizi       T  —  Son rapor\nW  —  Onay: ürün piyasaya gider       R  —  Red: gıda imha edilir\n\nRedden sonra ambalajı uygun hedefe gönder: kâğıt, cam, plastik veya kontamine atık.\nBakanlık acil faksını onayladığında yeni kural yalnızca sonraki ürünlere uygulanır.\n\nEsc  —  Belgeyi kapat / Duraklat       Fare  —  Etrafa bak\nZorunlu gazete ve acil faksı, devam düğmesiyle onayla.",0,40,1250,520,30,Paper);
            var helpBack=Button("Back",help.transform,"Menüye dön",0,-380,440,controller.ShowHome);help.SetActive(false);
            var about=Overlay("About",canvas.transform,new Color(.025f,.08f,.09f,.97f));
            Text("AboutTitle",about.transform,"Ortak gelecek için",0,380,1250,100,64,Paper,TextAnchor.MiddleLeft,true);
            Text("AboutBody",about.transform,"COP31 • 9–20 Kasım 2026 • Antalya\n\nDenizleri, caretta carettaların yaşam alanlarını ve gıda güvenliğini korumak; atığı kaynağında azaltmakla başlar. Bu oyunda COP31 hazırlıklarında görevli bir denetçisin.\n\n“İklim krizi yalnızca çevresel değil, aynı zamanda ahlaki bir sorumluluk alanıdır.”\nEmine Erdoğan • cop31.tr\n\nEkip: 3D sanat / Özgür: etkileşim / Faruk: veri ve kurallar / Hasan: akış ve entegrasyon\nGörseller: yapay zekâ ile üretilmiş illüstrasyonlar. Logo: COP31 resmî kurumsal kimliği.\nMenü öğeleri: Feathered Fortune proje arşivi. Yazı tipi: Kurale / SIL OFL.\nOyundaki haberler ve ürün partileri kurmacadır.",0,30,1250,540,28,Paper);
            Button("OfficialSite",about.transform,"COP31 resmî sitesi",-250,-380,440,controller.OpenCOP31);
            var aboutBack=Button("Back",about.transform,"Menüye dön",250,-380,440,controller.ShowHome);about.SetActive(false);
            Set(controller,"home",home.gameObject);Set(controller,"help",help);Set(controller,"about",about);Set(controller,"settings",settings);
            Set(controller,"startButton",start);Set(controller,"helpBack",helpBack);Set(controller,"aboutBack",aboutBack);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),MenuPath);
        }
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
