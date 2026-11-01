using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameUp.Editor
{
    public static class InspectionUpgradeAuthoring
    {
        [MenuItem("GameUP32/Authoring/Update product inspection and supplied models")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            foreach (var product in ProductionProjectSetup.Days().SelectMany(d => d.Products))
            { ProductLaboratoryAuthoring.Configure(product); EditorUtility.SetDirty(product); }
            AssetDatabase.SaveAssets();
            ProductLabelAuthoring.RefreshCampaign();
            ApplySceneModels();
            CampaignInspectionAudit.RunBatch();
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("INSPECTION_UPGRADE_OK / current bins and buttons / data-driven fax / 31 UV labels / saved scene preserved");
        }
        public static void ApplySceneModels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var roots = scene.GetRootGameObjects(); var office = roots.Single(r => r.name == "InspectionOffice").transform;
            RecyclingBinAuthoring.Apply(office); DecisionButtonAuthoring.Apply(office);
            var kit = roots.Single(r => r.name == ArtistGameKitAuthoring.RootName).transform.Find("UpdatedBins_4");
            foreach (var old in kit.Cast<Transform>().Where(t => t.name.StartsWith("Prop_trashbin", StringComparison.OrdinalIgnoreCase)).ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            for (int i = 0; i < 4; i++) RecyclingBinAuthoring.Place(i, kit, new Vector3(1 + i * 4.2f, 1.37f, -14), new Vector3(.68f, 1.3f, .65f));
            ArtistGameKitAuthoring.Isolate(kit);
            // Keep all gameplay target actions/colliders and user-authored workstation transforms.
            foreach (var text in office.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.font = UIAuthoring.Body;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            MenuEnvironmentAuthoring.ReadSource();
        }
        public static void ApplyAndReview() { Apply(); InspectionVisualReview.Capture(); }
    }
}
