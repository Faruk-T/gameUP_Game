using System;
using System.IO;
using GameUp.Lead.Prototype;
using GameUp.Lead.Unity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameUp.Lead.Editor
{
    public static class LeadProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/LeadPrototype.unity";
        private static Font font;
        private static readonly Color Ink = new Color32(25, 45, 49, 255);
        private static readonly Color Cream = new Color32(244, 237, 217, 255);
        private static readonly Color Teal = new Color32(25, 104, 100, 255);

        [MenuItem("GameUP/Lead/Open or create integration scene")]
        public static void EnsureScene()
        {
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            CreateScene();
        }

        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Art/LeadPrototype");
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LeadPrototype";
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.68f);
            var cameraObject = new GameObject("WorkstationCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 1.7f, -2.8f);
            camera.transform.LookAt(new Vector3(0, 1.05f, 0.45f));
            camera.fieldOfView = 52;
            camera.backgroundColor = new Color(0.57f, 0.77f, 0.81f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var sun = new GameObject("AfternoonLight", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.transform.rotation = Quaternion.Euler(45, -30, 0);
            Box("Floor", new Vector3(0, -0.1f, 0), new Vector3(10, 0.15f, 10), new Color(0.5f, 0.56f, 0.53f));
            Box("BackWall", new Vector3(0, 1.8f, 2), new Vector3(8, 3.6f, 0.15f), Cream);
            Box("Window", new Vector3(0, 2.1f, 1.9f), new Vector3(3.3f, 1.5f, 0.04f), new Color(0.36f, 0.7f, 0.8f));
            Box("Desk", new Vector3(0, 0.78f, 0.4f), new Vector3(2.7f, 0.15f, 1.6f), new Color(0.48f, 0.32f, 0.19f));
            Box("Conveyor", new Vector3(0, 0.9f, 0.08f), new Vector3(3.6f, 0.08f, 0.5f), new Color(0.13f, 0.2f, 0.2f));
            var item = Box("CurrentProduct", Vector3.zero, Vector3.one * 0.28f, new Color(0.9f, 0.64f, 0.28f));
            item.SetActive(false);
            var arrival = Point("ArrivalPoint", new Vector3(-1.8f, 1.12f, 0.08f));
            var desk = Point("DeskPoint", new Vector3(0, 1.12f, 0.08f));
            var exit = Point("ApprovedExitPoint", new Vector3(1.8f, 1.12f, 0.08f));
            var disposal = Point("FoodDisposalPoint", new Vector3(-1.6f, 0.45f, -0.2f));
            Box("Analyzer", new Vector3(-0.85f, 1.1f, 0.83f), new Vector3(0.48f, 0.48f, 0.45f), Teal);
            var faxBody = Box("Fax", new Vector3(0.85f, 1.04f, 0.8f), new Vector3(0.48f, 0.32f, 0.4f), new Color(0.22f, 0.27f, 0.29f));
            var paper = Box("FaxPaper", new Vector3(0.85f, 0.91f, 0.58f), new Vector3(0.27f, 0.008f, 0.34f), Color.white).transform;
            paper.SetParent(faxBody.transform, true);
            var indicator = new GameObject("FaxIndicator", typeof(Light)).GetComponent<Light>();
            indicator.transform.position = new Vector3(0.85f, 1.28f, 0.68f); indicator.range = 1; indicator.intensity = 0;
            var speaker = faxBody.AddComponent<AudioSource>(); speaker.playOnAwake = false; speaker.spatialBlend = 0;
            var printer = faxBody.AddComponent<FaxPresenter>(); printer.Configure(indicator, paper, speaker);
            var bins = new Transform[4];
            Color[] colors = { new Color(0.3f, 0.6f, 0.8f), new Color(0.3f, 0.65f, 0.4f), new Color(0.9f, 0.7f, 0.25f), new Color(0.8f, 0.28f, 0.25f) };
            for (int i = 0; i < bins.Length; i++) bins[i] = Box("Waste_" + i, new Vector3(1.65f + i * 0.4f, 0.35f, 0.8f), new Vector3(0.33f, 0.7f, 0.4f), colors[i]).transform;

            var systems = new GameObject("LeadSystems");
            var runtime = systems.AddComponent<LeadRuntime>();
            var backend = systems.AddComponent<PrototypeInspectionBackend>();
            var presentation = systems.AddComponent<PrototypeWorkstation>();
            presentation.Configure(item.transform, arrival, desk, exit, disposal, bins, camera, runtime);
            var view = CreateUI();
            runtime.Configure(backend, backend, presentation, view, printer);
            systems.AddComponent<LeadSmokeRunner>().Configure(runtime, view);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LEAD_SCENE_READY " + ScenePath);
        }

        private static LeadView CreateUI()
        {
            var root = new GameObject("LeadCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var view = root.AddComponent<LeadView>();
            var w = new LeadViewWidgets();
            var header = Panel(root.transform, "Header", 32, 24, 1856, 112, Ink);
            w.Header = TextAt(header, "Title", "", 26, 14, 1100, 38, 30, Cream);
            w.Status = TextAt(header, "Status", "", 28, 61, 1350, 28, 20, Cream);
            TextAt(header, "Location", "COP31 / ANTALYA", 1490, 38, 330, 32, 22, new Color(0.65f, 0.82f, 0.77f));
            var card = Panel(root.transform, "ProductCard", 32, 164, 465, 640, Cream);
            TextAt(card, "Eyebrow", "SEVKİYAT / ÜRÜN ETİKETİ", 26, 22, 410, 32, 19, Teal);
            w.ProductTitle = TextAt(card, "ProductName", "", 26, 73, 410, 100, 28, Ink);
            w.ProductBody = TextAt(card, "Label", "", 26, 195, 410, 310, 23, Ink);
            w.Inspect = ButtonAt(card, "Inspect", "E / Etiketi incele", 26, 546, 410, 62, Teal, Cream);
            var bottom = Panel(root.transform, "Actions", 520, 819, 1368, 230, Ink);
            w.Tutorial = TextAt(bottom, "Hint", "", 24, 15, 1320, 45, 21, Cream);
            w.Approve = ButtonAt(bottom, "Approve", "W / ONAY", 24, 75, 210, 62, Teal, Cream);
            w.Reject = ButtonAt(bottom, "Reject", "R / RED", 250, 75, 190, 62, new Color(0.65f, 0.28f, 0.23f), Cream);
            w.Analyze = ButtonAt(bottom, "Analyze", "F / ANALİZ", 456, 75, 210, 62, new Color(0.26f, 0.4f, 0.49f), Cream);
            w.Handbook = ButtonAt(bottom, "Handbook", "B / KİTAPÇIK", 682, 75, 270, 62, Cream, Ink);
            w.Report = ButtonAt(bottom, "Report", "SON RAPOR", 968, 75, 365, 62, Cream, Ink);
            w.WasteButtons = new Button[4];
            string[] labels = { "KÂĞIT", "CAM", "PLASTİK", "KONTAMİNE" };
            for (int i = 0; i < 4; i++) w.WasteButtons[i] = ButtonAt(bottom, "Waste" + i, labels[i], 24 + i * 335, 155, 314, 52, Cream, Ink);
            var feedback = Panel(root.transform, "FeedbackPanel", 32, 827, 465, 222, Cream);
            w.Feedback = TextAt(feedback, "Feedback", "", 22, 22, 420, 180, 20, Ink);
            TextAt(root.transform, "CameraHint", "Sağ fare: çevreye bak  •  İnceledikten sonra sol fare: ürünü döndür", 550, 748, 1280, 40, 20, Ink);

            w.SummaryPanel = FullPanel(root.transform, "SummaryOverlay", new Color(0.04f, 0.1f, 0.11f, 0.9f));
            var summary = Panel(w.SummaryPanel.transform, "ReportPaper", 480, 100, 960, 860, Cream);
            w.SummaryTitle = TextAt(summary, "Title", "", 56, 45, 850, 95, 34, Ink);
            w.SummaryBody = TextAt(summary, "Body", "", 56, 190, 850, 430, 28, Ink);
            w.SummaryContinue = ButtonAt(summary, "Continue", "Devam", 56, 710, 848, 72, Teal, Cream);
            w.SummaryButtonLabel = w.SummaryContinue.GetComponentInChildren<Text>();
            w.Restart = ButtonAt(summary, "Restart", "Tekrar oyna", 56, 710, 410, 72, Teal, Cream);
            w.Quit = ButtonAt(summary, "Quit", "Çıkış", 490, 710, 414, 72, Ink, Cream);

            w.DocumentPanel = FullPanel(root.transform, "DocumentOverlay", new Color(0.04f, 0.1f, 0.11f, 0.87f));
            var document = Panel(w.DocumentPanel.transform, "DocumentPaper", 380, 80, 1160, 920, Cream);
            w.DocumentKind = TextAt(document, "Kind", "", 60, 34, 1040, 34, 20, Teal);
            w.DocumentTitle = TextAt(document, "Title", "", 60, 92, 1040, 95, 36, Ink);
            w.DocumentBody = ScrollText(document, 60, 215, 1040, 540);
            w.DocumentScroll = w.DocumentBody.GetComponentInParent<ScrollRect>();
            w.DocumentConfirm = ButtonAt(document, "Confirm", "Kapat", 60, 805, 1040, 72, Teal, Cream);
            w.DocumentButtonLabel = w.DocumentConfirm.GetComponentInChildren<Text>();
            w.DocumentPanel.SetActive(false); w.SummaryPanel.SetActive(false);
            view.Configure(w);
            return view;
        }

        private static Text ScrollText(Transform parent, float x, float y, float width, float height)
        {
            var scroll = Rect(parent, "ScrollableBody", x, y, width, height).gameObject.AddComponent<ScrollRect>();
            var viewport = Rect(scroll.transform, "Viewport", 0, 0, width, height);
            viewport.gameObject.AddComponent<RectMask2D>();
            var image = viewport.gameObject.AddComponent<Image>(); image.color = new Color(1, 1, 1, 0.01f);
            var text = TextAt(viewport, "Body", "", 0, 0, width, height, 25, Ink);
            var fitter = text.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = text.rectTransform; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        { var rect = Rect(parent, name, x, y, width, height); rect.gameObject.AddComponent<Image>().color = color; return rect; }
        private static GameObject FullPanel(Transform parent, string name, Color color)
        {
            var rect = Rect(parent, name, 0, 0, 0, 0); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.gameObject.AddComponent<Image>().color = color; return rect.gameObject;
        }
        private static Text TextAt(Transform parent, string name, string value, float x, float y, float width, float height, int size, Color color)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.color = color; text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        private static Button ButtonAt(Transform parent, string name, string value, float x, float y, float width, float height, Color background, Color foreground)
        {
            var panel = Panel(parent, name, x, y, width, height, background);
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            var colors = button.colors; colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.55f); button.colors = colors;
            var label = TextAt(panel, "Label", value, 10, 0, width - 20, height, 22, foreground); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        private static Transform Point(string name, Vector3 position) { var item = new GameObject(name).transform; item.position = position; return item; }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.position = position; item.transform.localScale = scale;
            string path = "Assets/Art/LeadPrototype/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
                AssetDatabase.CreateAsset(material, path);
            }
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        [MenuItem("GameUP/Lead/Build Windows prototype")]
        public static void BuildWindows()
        {
            EnsureScene();
            string output = Path.GetFullPath("Builds/LeadPrototype/GameUP32.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows prototype build failed.");
            Debug.Log($"LEAD_BUILD_READY {output} ({report.summary.totalSize} bytes)");
        }
    }
}
