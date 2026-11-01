using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GameUp.Editor
{
    internal static class MenuEnvironmentAuthoring
    {
        private const string Path = "Assets/Prefabs/Environment/MenuFacility.prefab";
        internal struct Settings
        {
            internal Color Ambient, Fog, Background;
            internal bool FogEnabled;
            internal float Density, Fov, Far;
            internal Vector3 Position;
            internal Quaternion Rotation;
        }
        internal static Settings ReadSource()
        {
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots = scene.GetRootGameObjects();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
            var settings = new Settings { Ambient = RenderSettings.ambientLight, Fog = RenderSettings.fogColor,
                FogEnabled = RenderSettings.fog, Density = RenderSettings.fogDensity, Position = camera.transform.position,
                Rotation = camera.transform.rotation, Fov = camera.fieldOfView, Far = camera.farClipPlane, Background = camera.backgroundColor };
            var art = new GameObject("MenuFacility");
            foreach (var source in roots.Where(r => r.name == "InspectionOffice" || r.name == "MediterraneanSun"))
            {
                var copy = UnityEngine.Object.Instantiate(source, art.transform); copy.name = source.name;
                foreach (var script in copy.GetComponentsInChildren<MonoBehaviour>(true))
                    if (script && !(script is Graphic) && !(script is CanvasScaler)) UnityEngine.Object.DestroyImmediate(script);
                foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var sound in copy.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(sound);
                foreach (var listener in copy.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
                foreach (var text in copy.GetComponentsInChildren<Text>(true)) text.font = UIAuthoring.Body;
            }
            PrefabUtility.SaveAsPrefabAsset(art, Path); UnityEngine.Object.DestroyImmediate(art);
            return settings;
        }
        internal static void Place(Settings settings)
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            var camera = new GameObject("MenuWorldCamera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(settings.Position, settings.Rotation);
            camera.fieldOfView = settings.Fov; camera.nearClipPlane = .05f; camera.farClipPlane = settings.Far;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = settings.Background; camera.cullingMask &= ~(1 << 30);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = settings.Ambient;
            RenderSettings.fog = settings.FogEnabled; RenderSettings.fogColor = settings.Fog; RenderSettings.fogDensity = settings.Density;
        }
    }
}
