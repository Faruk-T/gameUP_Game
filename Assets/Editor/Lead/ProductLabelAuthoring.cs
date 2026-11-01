using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    // Only copies are re-unwrapped. The original albedo and printing become one permanent UV atlas.
    internal static class ProductLabelAuthoring
    {
        internal const string Root = "Assets/Art/ProductPrints";
        internal static void RefreshCampaign()
        {
            Directory.CreateDirectory(Root); Directory.CreateDirectory("Logs/FacilityPolish/Prints"); AssetDatabase.Refresh();
            foreach (var definition in ProductionProjectSetup.Days().SelectMany(d => d.Products))
                RefreshProduct(definition);
            AssetDatabase.SaveAssets(); Debug.Log("PRODUCT_UV_PRINTS_OK / 31 artist models / no separate label geometry");
        }
        internal static void RefreshProduct(ProductDefinition definition)
        {
            string path = AssetDatabase.GetAssetPath(definition.Prefab);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var previous = prefab.transform.Cast<Transform>().Single(t => t.name.StartsWith("Model_", StringComparison.Ordinal));
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ConditionVariantAuthoring.PathFor(definition.ModelSource.name, definition.Condition));
                var model = ProductAuthoring.PlaceModel(source, prefab.transform, Vector3.zero, .65f);
                Configure(prefab, model, definition); PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        internal static void Configure(GameObject root, GameObject model, ProductDefinition definition)
        {
            Directory.CreateDirectory(Root); Directory.CreateDirectory("Logs/FacilityPolish/Prints"); AssetDatabase.Refresh();
            foreach (var name in new[] { "Front", "Back", "DateFront", "DateBack" })
            { var old = root.transform.Find(name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var bounds = new Bounds(); bool initialized = false;
            foreach (var filter in filters) foreach (var vertex in filter.sharedMesh.vertices)
            {
                var point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; } else bounds.Encapsulate(point);
            }
            if (!initialized) throw new InvalidOperationException("Missing product geometry: " + definition.DefinitionId);
            bool flat = ProductWorldLabelBinder.PrintsOnTop(bounds);
            float width = bounds.size.x * .76f, height = (flat ? bounds.size.z : bounds.size.y) * .65f;
            var center = bounds.center;
            if (definition.Category == ProductCategory.Fruit) { center.y = bounds.min.y + bounds.size.y * .34f; height = bounds.size.y * .42f; }
            if (definition.Category == ProductCategory.Eggs) { center.y = bounds.min.y + bounds.size.y * .72f; height = bounds.size.y * .45f; }
            var front = Print(definition, "Front", definition.GetFormattedFrontLabel(), width, height);
            var back = Print(definition, "Back", definition.GetFormattedBackLabel(), width, height);
            try { for (int i = 0; i < filters.Length; i++) BakeMesh(root, filters[i], definition, i, bounds, center, width, height, flat, front, back); }
            finally { UnityEngine.Object.DestroyImmediate(front); UnityEngine.Object.DestroyImmediate(back); }
            var binder = root.GetComponent<ProductWorldLabelBinder>(); if (!binder) binder = root.AddComponent<ProductWorldLabelBinder>();
            binder.Configure(definition, model.GetComponentsInChildren<MeshRenderer>(true), bounds);
            var collider = root.GetComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size + Vector3.one * .12f;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
        }
        private static Texture2D Print(ProductDefinition definition, string side, string text, float w, float h)
        {
            text = Regex.Replace(text.Trim(), @"\n\s*\n", "\n");
            int height = Mathf.Clamp(Mathf.RoundToInt(1024 * h / w), 384, 1536);
            var canvas = Canvas("__UVPrint");
            try
            {
                canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1024, height);
                Image("Paper", canvas.transform, 0, 0, 1024, height, new Color(.96f, .94f, .84f));
                var ink = Text("Ink", canvas.transform, text, 0, 0, 988, height - 30, 180, Color.black, TextAnchor.MiddleCenter);
                ink.resizeTextForBestFit = true; ink.resizeTextMinSize = 20; ink.resizeTextMaxSize = 180;
                string path = "Logs/FacilityPolish/Prints/" + definition.DefinitionId + side + ".png";
                ProductionSnapshot.Capture(canvas, null, path, 1024, height);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); texture.LoadImage(File.ReadAllBytes(path));
                texture.wrapMode = TextureWrapMode.Clamp; return texture;
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
        }
        private static void BakeMesh(GameObject root, MeshFilter filter, ProductDefinition definition, int index,
            Bounds bounds, Vector3 center, float width, float height, bool flat, Texture2D front, Texture2D back)
        {
            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = definition.DefinitionId + "_PrintedMesh_" + index;
            UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .006f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new InvalidOperationException("Unable to unwrap " + definition.DefinitionId);
            var renderer = filter.GetComponent<MeshRenderer>(); var originals = renderer.sharedMaterials;
            var baker = new Material(Shader.Find("Hidden/GameUP/ProductUVBake"));
            const int size = 2048;
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active; bool srgb = GL.sRGBWrite;
            var pixels = new Texture2D(size, size, TextureFormat.RGB24, false);
            string prefix = Root + "/" + definition.DefinitionId + "_" + index;
            try
            {
                RenderTexture.active = target; GL.Clear(false, true, Color.black);
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                var transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                baker.SetMatrix("_ToProduct", transform); baker.SetMatrix("_NormalToProduct", transform.inverse.transpose);
                baker.SetVector("_PrintCenter", center); baker.SetVector("_PrintSize", new Vector4(width, height, flat ? 1 : 0, 0));
                baker.SetTexture("_FrontPrint", front); baker.SetTexture("_BackPrint", back);
                baker.SetFloat("_Milk", definition.ModelSource.name.StartsWith("milk", StringComparison.Ordinal) ? 1 : 0); baker.SetFloat("_Top", bounds.max.y);
                baker.SetFloat("_Yogurt", definition.LaboratoryProfile == LaboratoryProfile.Yogurt ? 1 : 0);
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var original = originals[sub];
                    baker.SetFloat("_PrintAllowed", definition.Category != ProductCategory.Eggs || original.name == "Paper" ? 1 : 0);
                    baker.SetTexture("_OriginalTex", original.mainTexture ? original.mainTexture : Texture2D.whiteTexture);
                    baker.SetColor("_OriginalColor", original.color);
                    baker.SetVector("_OriginalST", new Vector4(original.mainTextureScale.x, original.mainTextureScale.y, original.mainTextureOffset.x, original.mainTextureOffset.y));
                    if (!baker.SetPass(0) || ShaderUtil.ShaderHasError(baker.shader)) throw new InvalidOperationException("UV bake shader did not compile.");
                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity, sub);
                }
                pixels.ReadPixels(new UnityEngine.Rect(0, 0, size, size), 0, 0); pixels.Apply();
                var colors = pixels.GetPixels32(); int painted = 0;
                for (int c = 0; c < colors.Length; c += 97) if (colors[c].r > 60 || colors[c].g > 60 || colors[c].b > 60) painted++;
                if (painted < colors.Length / 97 * .02f) throw new InvalidOperationException("UV atlas render is empty: " + definition.DefinitionId);
                File.WriteAllBytes(prefix + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; GL.sRGBWrite = srgb; RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(baker);
            }
            AssetDatabase.ImportAsset(prefix + ".png", ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(prefix + ".png");
            importer.mipmapEnabled = true; importer.npotScale = TextureImporterNPOTScale.None; importer.isReadable = false;
            importer.maxTextureSize = size; importer.anisoLevel = 4; importer.wrapMode = TextureWrapMode.Clamp;
            var platform = importer.GetPlatformTextureSettings("Standalone"); platform.name = "Standalone";
            platform.overridden = true; platform.maxTextureSize = size; platform.format = TextureImporterFormat.BC7;
            importer.SetPlatformTextureSettings(platform); importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + ".png"); var materials = new Material[originals.Length];
            for (int m = 0; m < materials.Length; m++)
            {
                string materialPath = prefix + "_" + m + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material) { material = new Material(originals[m]); AssetDatabase.CreateAsset(material, materialPath); }
                else { material.shader = originals[m].shader; material.CopyPropertiesFromMaterial(originals[m]); }
                material.mainTexture = atlas; material.color = Color.white; material.mainTextureScale = Vector2.one; material.mainTextureOffset = Vector2.zero;
                EditorUtility.SetDirty(material); materials[m] = material;
            }
            mesh.uv = mesh.uv2; mesh.uv2 = Array.Empty<Vector2>(); string meshPath = prefix + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved) { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
            else { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
            filter.sharedMesh = saved; renderer.sharedMaterials = materials;
        }
    }
}
