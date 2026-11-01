using System;
using System.IO;
using System.Linq;
using GameUp.App;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    // All artwork, controls, navigation and listeners are saved in Menu.unity, never built at runtime.
    public static class DocumentMenuAuthoring
    {
        internal const string ArtPath = "Assets/Art/Menu/Documents/";
        private const string ButtonPath = ArtPath + "DocumentButton.asset";
        private static readonly Color DocumentInk = new Color(.025f, .02f, .015f);
        private static readonly Color SelectedInk = new Color(.28f, .08f, .02f);

        [MenuItem("GameUP32/Authoring/Refresh document main menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring the menu.");
            CreateScene();
            Capture();
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("DOCUMENT_MENU_OK / four transparent documents / native controls / explicit navigation");
        }

        internal static void CreateScene()
        {
            ImportArtwork();
            var backdrop = MenuEnvironmentAuthoring.ReadSource();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MenuEnvironmentAuthoring.Place(backdrop);
            var canvas = Canvas("MenuCanvas");
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Overlay("Atmosphere", canvas.transform, new Color(.025f, .055f, .05f, .42f)).GetComponent<Image>().raycastTarget = false;
            var controller = new GameObject("MenuController").AddComponent<MenuController>();
            var home = Rect("Home", canvas.transform, 0, 0, 1920, 1080); Stretch(home);
            var clipboard = Paper("Clipboard", home, "ClipboardPaperV2", -500, 0, 690, 1035);
            DocumentText("Authority", clipboard, "T.C. ÇEVRE VE GIDA DENETİMİ\nCOP31 ANTALYA", 512, 397, 810, 155, 32, TextAnchor.MiddleCenter);
            Legible(Text("Title", home, "Antalya\nDenetim Masası", 430, 35, 780, 300, 68, PaperColor, TextAnchor.MiddleCenter, true));
            Legible(Text("Subtitle", home, "Bir doğru karar, daha temiz bir kıyı.", 430, -150, 760, 65, 29, PaperColor, TextAnchor.MiddleCenter, true));
            Legible(Text("Dates", home, "9–20 KASIM 2026  •  ANTALYA, TÜRKİYE", 430, -245, 760, 50, 23, PaperColor, TextAnchor.MiddleCenter));
            Image("LogoPaper", home, 430, 325, 340, 150, new Color(.98f, .97f, .94f));
            var logo = Image("OfficialCOP31", home, 430, 325, 470, 264.375f, Color.white,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Menu/COP31Horizontal.png")); logo.preserveAspect = true;
            var buttons = new[] {
                PaperButton("Start", home, "GÖREVE BAŞLA", -500, 82, 470, 76, controller.StartGame),
                PaperButton("Settings", home, "AYARLAR", -500, -25, 470, 76, controller.ShowSettings),
                PaperButton("Help", home, "NASIL OYNANIR?", -500, -132, 470, 76, controller.ShowHelp),
                PaperButton("About", home, "COP31 & EKİP", -500, -239, 470, 76, controller.ShowAbout),
                PaperButton("Quit", home, "ÇIKIŞ", -500, -346, 470, 76, controller.Quit)
            };
            Navigate(buttons);
            var settings = CreateSettings(canvas.transform);

            var help = Overlay("Help", canvas.transform, new Color(.015f, .025f, .02f, .82f));
            var guide = Paper("Document", help.transform, "ClipboardPaperV2", 0, 0, 700, 1050);
            DocumentText("GuideTitle", guide, "DENETÇİ REHBERİ", 512, 392, 810, 120, 43, TextAnchor.MiddleCenter);
            var instructions = DocumentText("HelpBody", guide,
                "Her sabah gazeteyi oku; o günün kurallarıyla çalış.\n\nE / Sol tık — Ürünü incele.\nFare hareketi — Yalnız ürünü döndür.\nTekerlek — Yaklaş / uzaklaş.\nE / Sağ tık — Ürünü bırak.\n\nB — El kitapçığı\nF — Laboratuvar analizi\nW — Onay: piyasaya gönder\nR — Red: gıdayı imha et\n\nRedden sonra ambalajı kâğıt, cam, plastik veya kontamine atık kutusuna gönder.\n\nAcil faksı onaylayınca yeni kural sonraki ürünlere uygulanır.\nAynı gün üç yanlış vardiyayı bitirir; sayaç her sabah sıfırlanır.\n\nEsc — Belgeyi kapat / Duraklat\nZorunlu gazete ve faksı devam düğmesiyle onayla.",
                512, 906, 810, 880, 26, TextAnchor.UpperLeft);
            instructions.resizeTextForBestFit = true; instructions.resizeTextMinSize = 20; instructions.resizeTextMaxSize = 26;
            var backPoint = Position(guide, 512, 1398);
            var helpBack = PaperButton("Back", guide, "Menüye dön", backPoint.x, backPoint.y, 350, 66, controller.ShowHome);
            Navigate(new Selectable[] { helpBack }); help.SetActive(false);

            var about = Overlay("About", canvas.transform, new Color(.015f, .025f, .02f, .82f));
            var credits = Paper("Document", about.transform, "CreditsPaperV2", 0, 0, 700, 1050);
            Credits(credits);
            var site = PrintedButton("OfficialSite", credits, "COP31 resmî sitesi", 338, 1367, 467, 106, controller.OpenCOP31);
            var aboutBack = PrintedButton("Back", credits, "Menüye dön", 764, 1367, 326, 106, controller.ShowHome);
            NavigateHorizontal(site, aboutBack); about.SetActive(false);
            Set(controller, "home", home.gameObject); Set(controller, "help", help); Set(controller, "about", about);
            Set(controller, "settings", settings); Set(controller, "startButton", buttons[0]);
            Set(controller, "helpBack", helpBack); Set(controller, "aboutBack", aboutBack);
            WorkstationUIAuthoring.AddButtonSounds(canvas.gameObject);
            EditorSceneManager.SaveScene(scene, ProductionProjectSetup.MenuPath);
            AssetDatabase.SaveAssets();
        }

        private static Color PaperColor => new Color(.97f, .93f, .82f);
        private static void Legible(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .8f); shadow.effectDistance = new Vector2(2, -3);
            if (text.preferredHeight > text.rectTransform.sizeDelta.y)
                throw new InvalidOperationException("Menu text exceeds its authored height: " + text.name);
        }

        internal static SettingsPanel CreateSettings(Transform parent)
        {
            var root = Overlay("Settings", parent, new Color(.015f, .025f, .02f, .82f));
            var panel = root.AddComponent<SettingsPanel>();
            var document = Paper("Document", root.transform, "SettingsPaperV2", 0, 0, 680, 1020);
            DocumentText("SettingsTitle", document, "AYARLAR", 520, 305, 650, 120, 52, TextAnchor.MiddleCenter);
            DocumentText("VolumeLabel", document, "Ana ses", 450, 475, 460, 90, 34, TextAnchor.MiddleLeft);
            DocumentText("SensitivityLabel", document, "Fare hassasiyeti", 480, 662, 520, 90, 34, TextAnchor.MiddleLeft);
            DocumentText("FovLabel", document, "Görüş açısı", 450, 860, 460, 90, 34, TextAnchor.MiddleLeft);
            DocumentText("FullscreenLabel", document, "Tam ekran", 510, 1040, 430, 90, 34, TextAnchor.MiddleLeft);
            var volume = DocumentSlider(document, "Volume", 565, 0, 1, out var volumeValue);
            var sensitivity = DocumentSlider(document, "Sensitivity", 755, .03f, .3f, out var sensitivityValue);
            var fov = DocumentSlider(document, "FieldOfView", 955, 45, 80, out var fovValue);
            var hit = At("Fullscreen", document, 500, 1040, 560, 85);
            var hitImage = hit.gameObject.AddComponent<Image>(); hitImage.color = Color.clear;
            var toggle = hit.gameObject.AddComponent<Toggle>();
            var square = Image("BoxInterior", hit, (251 - 500) * 680f / 1024, 0, 27, 27, new Color(.83f, .74f, .56f));
            var check = Text("Check", square.transform, "×", 0, 0, 35, 35, 28, DocumentInk, TextAnchor.MiddleCenter);
            toggle.targetGraphic = square; toggle.graphic = check;
            var toggleColors = toggle.colors; toggleColors.highlightedColor = new Color(1, .86f, .45f);
            toggleColors.selectedColor = toggleColors.highlightedColor; toggle.colors = toggleColors;
            var backPosition = Position(document, 555, 1260);
            var back = PaperButton("Back", document, "Kaydet ve dön", backPosition.x, backPosition.y, 365, 76, panel.Close);
            Set(panel, "volume", volume); Set(panel, "sensitivity", sensitivity); Set(panel, "fieldOfView", fov);
            Set(panel, "fullscreen", toggle); Set(panel, "volumeValue", volumeValue);
            Set(panel, "sensitivityValue", sensitivityValue); Set(panel, "fieldOfViewValue", fovValue);
            UnityEventTools.AddPersistentListener(volume.onValueChanged, panel.SetVolume);
            UnityEventTools.AddPersistentListener(sensitivity.onValueChanged, panel.SetSensitivity);
            UnityEventTools.AddPersistentListener(fov.onValueChanged, panel.SetFieldOfView);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, panel.SetFullscreen);
            Navigate(new Selectable[] { volume, sensitivity, fov, toggle, back });
            root.SetActive(false); return panel;
        }

        private static Slider DocumentSlider(RectTransform document, string name, float sourceY, float min, float max, out Text value)
        {
            var rect = At(name, document, 545, sourceY, 635, 70);
            var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var slider = rect.gameObject.AddComponent<Slider>();
            Image("Track", rect, 0, 0, rect.sizeDelta.x, 4, new Color(.27f, .23f, .17f));
            var handleArea = Rect("HandleArea", rect, 0, 0, rect.sizeDelta.x - 12, 28);
            var handle = Image("Handle", handleArea, 0, 0, 12, 0, Color.white); handle.raycastTarget = true;
            slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            var colors = slider.colors; colors.normalColor = DocumentInk; colors.highlightedColor = SelectedInk;
            colors.selectedColor = SelectedInk; colors.pressedColor = new Color(.65f, .38f, .09f); slider.colors = colors;
            var valuePosition = Position(document, 800, sourceY - 35);
            value = Text(name + "Value", document, "", valuePosition.x, valuePosition.y, 76, 35, 20, DocumentInk, TextAnchor.MiddleRight, true);
            return slider;
        }

        private static RectTransform Paper(string name, Transform parent, string asset, float x, float y, float w, float h)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + asset + ".png");
            if (!sprite) throw new FileNotFoundException("Missing document sprite: " + asset);
            var image = Image(name, parent, x, y, w, h, Color.white, sprite); image.preserveAspect = true;
            return image.rectTransform;
        }

        // Coordinates refer to the 1024 x 1536 source documents. Native controls retain the same alignment at every resolution.
        private static Vector2 Position(RectTransform document, float x, float y) =>
            new Vector2((x / 1024 - .5f) * document.sizeDelta.x, (.5f - y / 1536) * document.sizeDelta.y);
        private static RectTransform At(string name, RectTransform document, float x, float y, float w, float h)
        {
            var p = Position(document, x, y);
            return Rect(name, document, p.x, p.y, w * document.sizeDelta.x / 1024, h * document.sizeDelta.y / 1536);
        }

        internal static Button PaperButton(string name, Transform parent, string label, float x, float y, float w, float h, UnityAction action)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            if (!sprite) throw new FileNotFoundException(ButtonPath);
            var image = Image(name, parent, x, y, w, h, Color.white, sprite);
            image.type = UnityEngine.UI.Image.Type.Sliced; image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .87f, .6f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.79f, .59f, .33f); button.colors = colors;
            Text("Label", image.transform, label, 0, 0, w - 24, h - 10, 30, DocumentInk, TextAnchor.MiddleCenter, true);
            UnityEventTools.AddPersistentListener(button.onClick, action); return button;
        }

        private static Button PrintedButton(string name, RectTransform document, string label, float x, float y, float w, float h, UnityAction action)
        {
            var rect = At(name, document, x, y, w, h);
            var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            var text = Text("Label", rect, label, 0, 0, rect.sizeDelta.x - 12, rect.sizeDelta.y - 8, 27,
                Color.white, TextAnchor.MiddleCenter, true);
            button.targetGraphic = text;
            var colors = button.colors; colors.normalColor = DocumentInk;
            colors.highlightedColor = SelectedInk; colors.selectedColor = SelectedInk;
            colors.pressedColor = new Color(.65f, .38f, .09f); button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action); return button;
        }

        private static void Navigate(Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
                items[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i + items.Length - 1) % items.Length], selectOnDown = items[(i + 1) % items.Length] };
        }
        private static void NavigateHorizontal(Selectable left, Selectable right)
        {
            left.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = right, selectOnRight = right };
            right.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = left, selectOnRight = left };
        }

        internal static void ImportArtwork()
        {
            AssetDatabase.Refresh();
            foreach (var name in new[] { "ClipboardPaperV2", "SettingsPaperV2", "CreditsPaperV2", "InspectorGuide" })
            {
                string path = ArtPath + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) throw new FileNotFoundException(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false; importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.wrapMode = TextureWrapMode.Clamp;
                var platform = importer.GetPlatformTextureSettings("Standalone"); platform.name = "Standalone";
                platform.overridden = true; platform.maxTextureSize = 2048; platform.format = TextureImporterFormat.BC7;
                importer.SetPlatformTextureSettings(platform); importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + "InspectorGuide.png");
            var sprite = Sprite.Create(texture, new UnityEngine.Rect(566, 74, 344, 132), new Vector2(.5f, .5f), 100,
                0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20)); sprite.name = "DocumentButton";
            var saved = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            if (saved) { EditorUtility.CopySerialized(sprite, saved); EditorUtility.SetDirty(saved); UnityEngine.Object.DestroyImmediate(sprite); }
            else AssetDatabase.CreateAsset(sprite, ButtonPath);
            AssetDatabase.SaveAssets();
        }

        private static Text DocumentText(string name, RectTransform document, string value, float x, float y, float w, float h, int size, TextAnchor alignment)
        {
            var rect = At(name, document, x, y, w, h);
            var text = Text(name + "Ink", rect, value, 0, 0, rect.sizeDelta.x, rect.sizeDelta.y, size, DocumentInk, alignment, true);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = size;
            return text;
        }
        private static void Credits(RectTransform document)
        {
            DocumentText("Heading", document, "ORTAK\nGELECEK İÇİN", 520, 235, 500, 210, 45, TextAnchor.MiddleCenter);
            DocumentText("Conference", document, "COP31 • 9–20 Kasım 2026 • Antalya", 512, 397, 800, 65, 26, TextAnchor.MiddleCenter);
            DocumentText("Mission", document, "Denizleri, caretta carettaların yaşam alanlarını ve gıda güvenliğini korumak; atığı kaynağında azaltmakla başlar. Bu oyunda COP31 hazırlıklarında görevli bir denetçisin.", 512, 566, 775, 205, 28, TextAnchor.MiddleLeft);
            DocumentText("Quote", document, "“İklim krizi yalnızca çevresel değil, aynı zamanda ahlaki bir sorumluluk alanıdır.”\nEmine Erdoğan • cop31.tr", 512, 777, 775, 170, 27, TextAnchor.MiddleCenter);
            DocumentText("Team", document, "Ekip: Esmanur: 3D Artist / Özgür: etkileşim / Faruk: veri ve kurallar / Hasan: akış ve entegrasyon", 512, 935, 775, 105, 27, TextAnchor.MiddleLeft);
            DocumentText("Credits", document, "Görseller: yapay zekâ ile üretilmiştir.\nMenü öğeleri: Feathered Fortune proje arşivi.\nYazı tipi: Patrick Hand / SIL OFL.\nKitap animasyonu: Özgür / Book-Page Curl.\nOyundaki haberler ve ürün partileri kurmacadır.", 512, 1150, 775, 240, 25, TextAnchor.MiddleLeft);
            DocumentText("Stamp1", document, "KAYIT", 848, 238, 200, 65, 31, TextAnchor.MiddleCenter).transform.localEulerAngles = new Vector3(0, 0, 14);
            DocumentText("Stamp2", document, "ONAY", 885, 394, 150, 65, 29, TextAnchor.MiddleCenter).transform.localEulerAngles = new Vector3(0, 0, 14);
        }
        internal static void CreatePause(GameObject canvas, PauseMenu pause)
        {
            var old = canvas.transform.Find("PauseOverlay"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay = Overlay("PauseOverlay", canvas.transform, new Color(.01f, .025f, .02f, .76f));
            var home = Rect("PauseHome", overlay.transform, 0, 0, 1920, 1080); Stretch(home);
            var paper = Paper("Clipboard", home, "ClipboardPaperV2", 0, 0, 620, 930);
            DocumentText("PauseTitle", paper, "VARDİYA MOLASI", 512, 395, 800, 135, 42, TextAnchor.MiddleCenter);
            var resume = PaperButton("Resume", home, "Devam et", 0, 15, 430, 76, pause.Resume);
            var settings = PaperButton("Settings", home, "Ayarlar", 0, -92, 430, 76, pause.ShowSettings);
            var menu = PaperButton("Menu", home, "Ana menü", 0, -199, 430, 76, pause.ReturnToMenu);
            Text("PauseHint", home, "Analiz ve ürün hareketi,\ndevam ettiğinde sürer.", 0, -320, 440, 95, 26, DocumentInk, TextAnchor.MiddleCenter, true);
            Navigate(new[] { resume, settings, menu });
            Set(pause, "overlay", overlay); Set(pause, "home", home.gameObject); Set(pause, "resume", resume);
            Set(pause, "settings", CreateSettings(overlay.transform)); WorkstationUIAuthoring.AddButtonSounds(overlay);
            overlay.SetActive(false);
        }

        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.MenuPath);
            var canvas = scene.GetRootGameObjects().Single(r => r.name == "MenuCanvas").GetComponent<Canvas>();
            foreach (string name in new[] { "Home", "Settings", "Help", "About" })
            {
                foreach (string other in new[] { "Home", "Settings", "Help", "About" })
                    canvas.transform.Find(other).gameObject.SetActive(name == other);
                if (name == "Settings")
                {
                    var document = canvas.transform.Find("Settings/Document");
                    document.Find("Volume").GetComponent<Slider>().SetValueWithoutNotify(.8f);
                    document.Find("Sensitivity").GetComponent<Slider>().SetValueWithoutNotify(.15f);
                    document.Find("FieldOfView").GetComponent<Slider>().SetValueWithoutNotify(65);
                    document.Find("VolumeValue").GetComponent<Text>().text = "80%";
                    document.Find("SensitivityValue").GetComponent<Text>().text = "0.15";
                    document.Find("FieldOfViewValue").GetComponent<Text>().text = "65°";
                }
                var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
                ProductionSnapshot.Capture(canvas, camera, "Docs/Previews/Menu/" + name + ".png", 1920, 1080);
                if (name == "Home")
                {
                    ProductionSnapshot.Capture(canvas, camera, "Docs/Previews/Menu/Home4x3.png", 1280, 960);
                    ProductionSnapshot.Capture(canvas, camera, "Docs/Previews/Menu/HomeUltrawide.png", 2560, 1080);
                }
            }
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
        }
    }
}
