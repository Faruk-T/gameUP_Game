using System;
using System.IO;
using System.Linq;
using GameUp.Contracts;
using GameUp.Gameplay;
using GameUp.Lead;
using GameUP.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class InspectionVisualReview
    {
        private const string Output = "Docs/Previews/InspectionAudit";
        public static void CaptureRecyclingStation()
        {
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots = scene.GetRootGameObjects();
            foreach (var c in roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)) c.gameObject.SetActive(false);
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera")); camera.gameObject.SetActive(true);
            var capture = Canvas("__RecyclingStationCapture");
            try
            {
                View(camera, new Vector3(0, 2.1f, -3.6f), new Vector3(0, .9f, -7), 70);
                ProductionSnapshot.Capture(capture, camera, Output + "/RecyclingBins.png", 1920, 1080);
            }
            finally { UnityEngine.Object.DestroyImmediate(capture.gameObject); EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath); }
            Debug.Log("ADJOINING_RECYCLING_BINS_CAPTURE_OK");
        }
        [MenuItem("GameUP32/QA/Capture product inspection visuals")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before capturing.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Output); Directory.CreateDirectory("Logs/InspectionAudit/Products");
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                foreach (var c in roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)) c.gameObject.SetActive(false);
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera")); camera.gameObject.SetActive(true);
                var capture = Canvas("__InspectionCapture");
                try
                {
                    View(camera, new Vector3(0, 2.1f, -3.6f), new Vector3(0, .9f, -7), 70);
                    ProductionSnapshot.Capture(capture, camera, Output + "/RecyclingBins.png", 1920, 1080);
                    var office = roots.Single(r => r.name == "InspectionOffice").transform;
                    var buttons = new[] { office.Find("Approve"), office.Find("Reject") };
                    Vector3 buttonCenter = (buttons[0].position + buttons[1].position) * .5f;
                    View(camera, buttonCenter + new Vector3(0, 1.1f, -1.3f), buttonCenter, 46);
                    ProductionSnapshot.Capture(capture, camera, Output + "/DecisionButtons.png", 1920, 1080);
                    foreach (var product in ProductionProjectSetup.Days().SelectMany(d => d.Products))
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(product.Prefab);
                        try
                        {
                            View(camera, new Vector3(0, 2.35f, -3.2f), new Vector3(0, 2.35f, 0), 52);
                            var labels = instance.GetComponent<ProductWorldLabelBinder>(); instance.transform.localScale *= labels.InspectionMultiplier;
                            foreach (var side in new[] { "Front", "Back" })
                            {
                                instance.transform.rotation = camera.transform.rotation * (side == "Back" ? Quaternion.Euler(0, 180, 0) : Quaternion.identity) * labels.InspectionRotation;
                                instance.transform.position = camera.transform.TransformPoint(new Vector3(0, 0, 1.12f)) - instance.transform.TransformVector(labels.LocalBounds.center);
                                string file = product.DefinitionId + "_" + side + ".png";
                                ProductionSnapshot.Capture(capture, camera, "Logs/InspectionAudit/Products/" + file, 960, 720);
                                if (new[] { "D1-01", "D1-02", "D1-03", "D1-04", "D3-12" }.Contains(product.DefinitionId))
                                    File.Copy("Logs/InspectionAudit/Products/" + file, Output + "/" + file, true);
                            }
                            if (product.LaboratoryProfile == GameUP.Contracts.LaboratoryProfile.Yogurt)
                            {
                                instance.transform.rotation = camera.transform.rotation * Quaternion.Euler(-90, 0, 0);
                                instance.transform.position = camera.transform.TransformPoint(new Vector3(0, 0, 1.12f)) - instance.transform.TransformVector(labels.LocalBounds.center);
                                ProductionSnapshot.Capture(capture, camera, Output + "/" + product.DefinitionId + "_Lid.png", 960, 720);
                            }
                        }
                        finally { UnityEngine.Object.DestroyImmediate(instance); }
                    }
                    capture.gameObject.SetActive(false);
                    var canvas = roots.Single(r => r.name == "GameCanvas").GetComponent<Canvas>(); canvas.gameObject.SetActive(true);
                    var presenter = canvas.GetComponent<DocumentPresenter>();
                    var documents = new DocumentQueue();
                    foreach (var id in new[] { "D1-01", "D1-02", "D1-03", "D1-04", "D1-06", "D2-04", "D3-04" })
                    {
                        var day = ProductionProjectSetup.Days().Single(d => d.Products.Any(p => p.DefinitionId == id));
                        var product = day.Products.Single(p => p.DefinitionId == id);
                        var report = new ProductInspectionReport(product, CampaignInspectionAudit.Snapshot(day, day.Products.ToList().IndexOf(product)));
                        documents.Enqueue(new DocumentRequest(DocumentKind.Analysis, "Analiz raporu / " + product.DisplayName, report.Text));
                        presenter.Show(documents.Current, day, Array.Empty<RuleInfo>());
                        var body = (Text)new SerializedObject(presenter).FindProperty("body").objectReferenceValue;
                        if (body.text != report.Text) throw new InvalidOperationException("Fax did not render current product data: " + id);
                        ProductionSnapshot.Capture(canvas, camera, Output + "/" + id + "_Fax.png", 1920, 1080);
                        var scroll = (ScrollRect)new SerializedObject(presenter).FindProperty("scroll").objectReferenceValue;
                        scroll.verticalNormalizedPosition = 0;
                        ProductionSnapshot.Capture(canvas, camera, Output + "/" + id + "_FaxSource.png", 1920, 1080);
                        documents.Confirm(documents.Current.Token);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(capture.gameObject); }
                ContactSheet("Front"); ContactSheet("Back");
            }
            finally { EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath); }
            CampaignInspectionAudit.RunBatch();
            Debug.Log("INSPECTION_VISUALS_OK / all 31 product fronts and backs / 3 yogurt lids / 7 actual fax documents / source bins and buttons");
        }
        private static void ContactSheet(string side)
        {
            var canvas = Canvas("__ProductContactSheet"); var textures = new System.Collections.Generic.List<Texture2D>();
            try
            {
                canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(3840, 3240);
                Image("Background", canvas.transform, 0, 0, 3840, 3240, Paper);
                int index = 0;
                foreach (var product in ProductionProjectSetup.Days().SelectMany(d => d.Products))
                {
                    float x = -1600 + index % 6 * 640, y = 1340 - index / 6 * 540;
                    var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes("Logs/InspectionAudit/Products/" + product.DefinitionId + "_" + side + ".png")); textures.Add(texture);
                    var image = Rect(product.DefinitionId, canvas.transform, x, y, 620, 465).gameObject.AddComponent<RawImage>(); image.texture = texture;
                    Text("Caption", canvas.transform, product.DefinitionId + " / " + product.DisplayName + "\nSKT " + product.ExpiryDate, x, y - 255, 620, 60, 20, Ink, TextAnchor.MiddleCenter);
                    index++;
                }
                ProductionSnapshot.Capture(canvas, null, Output + "/AllProducts_" + side + ".png", 3840, 3240);
            }
            finally { foreach (var t in textures) UnityEngine.Object.DestroyImmediate(t); UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
        }
        private static void View(Camera camera, Vector3 position, Vector3 target, float fov)
        { camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position)); camera.fieldOfView = fov; }
    }
}
