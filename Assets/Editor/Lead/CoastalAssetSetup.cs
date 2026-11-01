using System;
using System.IO;
using System.Linq;
using EPOOutline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameUp.Editor
{
    public static class CoastalAssetSetup
    {
        internal const string Root = "Assets/Art/CoastalFacility";
        public static void Audit()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            foreach (var path in new[]{"Assets/Art/Outside/Mesh/caretta caretta.fbx", "Assets/Art/Environment/Mesh/Environment.fbx",
                "Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Wall_Blank_01.prefab",
                "Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Wall_Blank_Door_Large_01.prefab",
                "Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Wall_Glass_Large_01.prefab",
                "Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Floor_Concrete_01.prefab", "Assets/StylizedWaterShader/WaterPlane.prefab"})
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!source) throw new FileNotFoundException(path);
                var instance = UnityEngine.Object.Instantiate(source);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                Debug.Log("COASTAL_ASSET " + path + " bounds=" + bounds + " renderers=" + renderers.Length + " children=" + string.Join(",",instance.GetComponentsInChildren<Transform>().Select(t=>t.name)));
                UnityEngine.Object.DestroyImmediate(instance);
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    Debug.Log("COASTAL_ANIMATION " + clip.name + " duration=" + clip.length + " loop=" + clip.isLooping);
            }
            Configure();
            Debug.Log("COASTAL_ASSETS_READY");
        }
        internal static void Configure()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var logoImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Menu/COP31Horizontal.png");
            if(logoImporter.maxTextureSize!=2048){logoImporter.maxTextureSize=2048;logoImporter.SaveAndReimport();}
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var folder in new[]{"Assets/PolygonOffice", "Assets/Art/Outside", "Assets/Art/Environment"})
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[]{folder}))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader == lit) continue;
                var map = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                bool transparent = material.HasProperty("_Mode") && material.GetFloat("_Mode") >= 2;
                material.shader = lit;
                material.SetTexture("_BaseMap", map); material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", .25f);
                if (transparent)
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                    material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                }
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
            foreach (var path in new[]{"Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset"})
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (!renderer.rendererFeatures.Any(f=>f is URPOutlineFeature))
                {
                    var feature = ScriptableObject.CreateInstance<URPOutlineFeature>(); feature.name = "EPO thin black contours";
                    AssetDatabase.AddObjectToAsset(feature, renderer); renderer.rendererFeatures.Add(feature);
                }
                renderer.SetDirty(); EditorUtility.SetDirty(renderer);
            }
            var waterPath = Root + "/MediterraneanWater.mat";
            var water = AssetDatabase.LoadAssetAtPath<Material>(waterPath);
            if (!water) { water = new Material(Shader.Find("GameUP/COP31/StylizedWaterURP")); AssetDatabase.CreateAsset(water, waterPath); }
            water.SetTexture("_DistortionTexture", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/StylizedWaterShader/Textures/T_WaterDistortion.png"));
            water.SetTexture("_FoamTexture", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/StylizedWaterShader/Textures/T_FoamTexture.png"));
            water.enableInstancing = true; EditorUtility.SetDirty(water);
            var newspaper=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Material/Newspaper_Texture.mat");
            newspaper.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Documents/Newsprint.png"));
            newspaper.SetColor("_BaseColor",new Color(.92f,.89f,.79f));newspaper.SetFloat("_Smoothness",.06f);newspaper.SetFloat("_BumpScale",.14f);EditorUtility.SetDirty(newspaper);
            AssetDatabase.SaveAssets();
        }
        internal static void ConfigureCamera(Camera camera)
        {
            var data=camera.GetUniversalAdditionalCameraData();
            data.requiresDepthOption=CameraOverrideOption.On;
            data.requiresColorOption=CameraOverrideOption.On;
            var outliner = camera.gameObject.AddComponent<Outliner>();
            outliner.PrimaryBufferSizeMode = BufferSizeMode.Native;
            outliner.PrimaryRendererScale = 1;
            outliner.DilateIterations = 1; outliner.DilateShift = .65f;
            outliner.BlurIterations = 0; outliner.BlurShift = 0;
        }
        internal static void Outline(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var outline = root.GetComponent<Outlinable>();
            if (!outline) outline = root.AddComponent<Outlinable>();
            outline.AddAllChildRenderersToRenderingList(RenderersAddingMode.MeshRenderer | RenderersAddingMode.SkinnedMeshRenderer);
            outline.OutlineParameters.Color = new Color(.015f,.02f,.023f,1);
            outline.OutlineParameters.DilateShift = 1;
            outline.OutlineParameters.BlurShift = 0;
            outline.RenderStyle = RenderStyle.FrontBack;
            outline.BackParameters.Enabled = false;
            outline.FrontParameters.Color = new Color(.015f,.02f,.023f,1);
            outline.FrontParameters.DilateShift = 1;
            outline.FrontParameters.BlurShift = 0;
            foreach (var renderer in renderers)
                if (!renderer.GetComponent<TargetStateListener>()) renderer.gameObject.AddComponent<TargetStateListener>();
        }
    }
}
