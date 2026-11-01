using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GameUp.Gameplay;
using GameUP.Interaction;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class FacilityPolishAuthoring
    {
        [MenuItem("GameUP32/Authoring/Apply facility, UV printing and guide book")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            ProductLabelAuthoring.RefreshCampaign();
            ApplyScenes();
        }
        public static void ApplyScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            DocumentMenuAuthoring.ImportArtwork();
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots = scene.GetRootGameObjects();
            var office = roots.Single(r => r.name == "InspectionOffice").transform;
            var presentation = roots.SelectMany(r => r.GetComponentsInChildren<ProductPresentation>(true)).Single();
            ConveyorAuthoring.Apply(office, presentation);
            var input = roots.SelectMany(r => r.GetComponentsInChildren<WorkstationInteractionController>(true)).Single();
            var anchor = (Transform)new SerializedObject(input).FindProperty("inspectionAnchor").objectReferenceValue;
            anchor.localPosition = new Vector3(0, -.02f, 1.35f);
            var canvas = roots.Single(r => r.name == "GameCanvas");
            DocumentMenuAuthoring.CreatePause(canvas, canvas.GetComponent<PauseMenu>());
            GuideBookAuthoring.Apply(canvas);
            SetFonts(scene.GetRootGameObjects());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            DocumentMenuAuthoring.CreateScene();
            var first = EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath); SetFonts(first.GetRootGameObjects());
            EditorSceneManager.MarkSceneDirty(first); EditorSceneManager.SaveScene(first);
            NewspaperV2Authoring.Apply(); DocumentMenuAuthoring.Capture();
            Debug.Log("FACILITY_POLISH_OK / live menu / themed pause / side intake / UV atlas labels / data-driven curling guide / handwritten UI");
        }
        private static void SetFonts(GameObject[] roots)
        {
            var texts = roots.SelectMany(r => r.GetComponentsInChildren<Text>(true)).ToArray();
            foreach (var text in texts) text.font = Body;
            var hint = texts.SingleOrDefault(t => t.name == "Target");
            if (hint)
            {
                hint.rectTransform.anchoredPosition = new Vector2(0, -350);
                hint.rectTransform.sizeDelta = new Vector2(1600, 60); hint.fontSize = 24;
            }
            UnityEngine.Canvas.ForceUpdateCanvases();
            foreach (var text in texts)
            {
                if (text.resizeTextForBestFit || text.verticalOverflow == VerticalWrapMode.Overflow || string.IsNullOrEmpty(text.text)) continue;
                while (text.preferredHeight > text.rectTransform.rect.height + .5f && text.fontSize > 12) text.fontSize--;
            }
        }
        public static void Inspect()
        {
            Directory.CreateDirectory("Logs/FacilityPolish");
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var output = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects()) output.AppendLine("ROOT " + root.name + " " + root.transform.position);
            foreach (var t in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            {
                if (new[] { "ConveyorDirection", "PlayerView", "PlayerCamera", "Arrival", "InspectionDesk", "MarketExit", "Prop_desk", "InspectionAnchor" }.Contains(t.name))
                    output.AppendLine("POINT " + t.name + " world=" + t.position + " rotation=" + t.eulerAngles + " local=" + t.localPosition);
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PatrickHand-Regular.ttf");
            foreach (char letter in "ÇĞİÖŞÜçğıöşü")
                if (!font.HasCharacter(letter)) throw new InvalidOperationException("Missing Turkish glyph: " + letter);
            foreach (var product in ProductionProjectSetup.Days().SelectMany(d => d.Products))
            {
                output.AppendLine("PRODUCT " + product.DefinitionId + " " + product.ModelSource.name);
                foreach (var filter in product.ModelSource.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    output.AppendLine("  MESH " + filter.name + " vertices=" + mesh.vertexCount + " submeshes=" + mesh.subMeshCount + " uv=" + mesh.uv.Length);
                    foreach (var mat in filter.GetComponent<Renderer>().sharedMaterials)
                        output.AppendLine("  MAT " + mat.name + " shader=" + mat.shader.name + " texture=" + AssetDatabase.GetAssetPath(mat.mainTexture) + " normal=" + (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap")));
                }
            }
            File.WriteAllText("Logs/FacilityPolish/source-audit.txt", output.ToString());
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("FACILITY_SOURCE_AUDIT_OK");
        }
    }
}
