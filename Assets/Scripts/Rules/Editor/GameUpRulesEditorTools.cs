#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GameUP.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUP.Rules.Editor
{
    /// <summary>
    /// Faruk (Veri &amp; Kurallar) için Unity Editor otomasyon menüsü:
    /// 1) 12 ürün, 4 P0 + 3 P1 kuralı, gazeteler, acil faks ve 2 günü Assets/Data/ altına .asset olarak üretir.
    /// 2) T01–T17 kabul testlerini tek tıkla çalıştırır.
    /// 3) Ana sahneye dokunmadan Assets/Scenes/Tests_Rules.unity izole test sahnesini kurar.
    /// </summary>
    public static class GameUpRulesEditorTools
    {
        private const string DataRoot = "Assets/Data";
        private const string RulesDir = "Assets/Data/Rules";
        private const string DocumentsDir = "Assets/Data/Documents";
        private const string ProductsDir = "Assets/Data/Products";
        private const string DaysDir = "Assets/Data/Days";
        private const string TestScenePath = "Assets/Scenes/Tests_Rules.unity";

        [InitializeOnLoadMethod]
        private static void AutoBootstrapDataAndVerifyOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;

                bool dataExists = AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_01_Dairy.asset") != null &&
                                  AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_02_Fish.asset") != null;

                if (!dataExists)
                {
                    GenerateAllDataAssets();
                    ExecuteAcceptanceTestsInEditor();
                }
            };
        }

        [MenuItem("GameUP32/Faruk (Rules & Data)/1. Tüm Veri Assetlerini Oluştur (Assets/Data)", false, 10)]
        public static void GenerateAllDataAssets()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataRoot, "Rules");
            EnsureFolder(DataRoot, "Documents");
            EnsureFolder(DataRoot, "Products");
            EnsureFolder(DataRoot, "Days");

            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();

            // 1. Kuralları kaydet
            var ruleExpiry = SaveOrUpdateAsset(catalog.RuleExpiry, $"{RulesDir}/{catalog.RuleExpiry.name}.asset");
            var ruleIdentity = SaveOrUpdateAsset(catalog.RuleIdentity, $"{RulesDir}/{catalog.RuleIdentity.name}.asset");
            var rulePollution = SaveOrUpdateAsset(catalog.RulePollution, $"{RulesDir}/{catalog.RulePollution.name}.asset");
            var ruleSourceBan = SaveOrUpdateAsset(catalog.RuleSourceBan, $"{RulesDir}/{catalog.RuleSourceBan.name}.asset");
            SaveOrUpdateAsset(catalog.RuleWeight, $"{RulesDir}/{catalog.RuleWeight.name}.asset");
            SaveOrUpdateAsset(catalog.RuleLength, $"{RulesDir}/{catalog.RuleLength.name}.asset");
            SaveOrUpdateAsset(catalog.RuleIngredientPct, $"{RulesDir}/{catalog.RuleIngredientPct.name}.asset");

            // 2. Belgeleri kaydet
            var docDay1News = SaveOrUpdateAsset(catalog.Day1Newspaper, $"{DocumentsDir}/{catalog.Day1Newspaper.name}.asset");
            var docDay2News = SaveOrUpdateAsset(catalog.Day2Newspaper, $"{DocumentsDir}/{catalog.Day2Newspaper.name}.asset");
            var docDay2Fax = SaveOrUpdateAsset(catalog.Day2EmergencyFax, $"{DocumentsDir}/{catalog.Day2EmergencyFax.name}.asset");

            // 3. Ürünleri kaydet (Gün 1 + Gün 2 + P1)
            var savedDay1Products = new List<ProductDefinition>();
            foreach (var p in catalog.Day1Products)
            {
                savedDay1Products.Add(SaveOrUpdateAsset(p, $"{ProductsDir}/{p.name}.asset"));
            }

            var savedDay2Products = new List<ProductDefinition>();
            foreach (var p in catalog.Day2Products)
            {
                savedDay2Products.Add(SaveOrUpdateAsset(p, $"{ProductsDir}/{p.name}.asset"));
            }

            foreach (var p in catalog.P1SampleProducts)
            {
                SaveOrUpdateAsset(p, $"{ProductsDir}/{p.name}.asset");
            }

            // 4. Günleri kalıcı asset referanslarıyla bağlayıp kaydet
            catalog.Day1.InitializeDay(
                id: "DAY_01",
                number: 1,
                title: "1. Gün — Süt Ürünleri Denetimi",
                date: new GameDate(1, 11, 2026),
                category: ProductCategory.Dairy,
                newspaper: docDay1News,
                startRules: new List<RuleDefinition> { ruleExpiry, ruleIdentity },
                dayProducts: savedDay1Products,
                emergency: new EmergencyDefinition(false, 0, "", null, null)
            );
            SaveOrUpdateAsset(catalog.Day1, $"{DaysDir}/{catalog.Day1.name}.asset");

            catalog.Day2.InitializeDay(
                id: "DAY_02",
                number: 2,
                title: "2. Gün — Deniz Ürünleri ve Acil Kriz Denetimi",
                date: new GameDate(2, 11, 2026),
                category: ProductCategory.Fish,
                newspaper: docDay2News,
                startRules: new List<RuleDefinition> { ruleExpiry, rulePollution },
                dayProducts: savedDay2Products,
                emergency: new EmergencyDefinition(
                    enabled: true,
                    triggerCount: 3,
                    clockTime: "12.00",
                    rule: ruleSourceBan,
                    document: docDay2Fax
                )
            );
            SaveOrUpdateAsset(catalog.Day2, $"{DaysDir}/{catalog.Day2.name}.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GameUP32 - Faruk] Tüm Rules, Documents, Products (12 P0 + 1 P1) ve Days asset'leri Assets/Data/ altında başarıyla oluşturuldu!");
        }

        [MenuItem("GameUP32/Faruk (Rules & Data)/2. T01–T17 Kabul Testlerini Çalıştır", false, 20)]
        public static void ExecuteAcceptanceTestsInEditor()
        {
            RulesAcceptanceSuite.RunAllTests(out bool allPassed, out string report);
            if (allPassed)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        [MenuItem("GameUP32/Faruk (Rules & Data)/3. Faruk Test Sahnesini Kur ve Aç (Tests_Rules.unity)", false, 30)]
        public static void CreateAndOpenRulesTestScene()
        {
            GenerateAllDataAssets();

            EnsureFolder("Assets", "Scenes");
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject systemsRoot = new GameObject("RulesSystems_TestRig");
            var inspection = systemsRoot.AddComponent<InspectionService>();
            var analysis = systemsRoot.AddComponent<AnalysisService>();
            var results = systemsRoot.AddComponent<RunResults>();
            var productionBackend = systemsRoot.AddComponent<ProductionInspectionBackend>();
            var harness = systemsRoot.AddComponent<RulesTestHarness>();

            var day1Asset = AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_01_Dairy.asset");
            var day2Asset = AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_02_Fish.asset");

            if (day1Asset != null && day2Asset != null)
            {
                AssignConfiguredDays(inspection, day1Asset, day2Asset);
                AssignConfiguredDays(productionBackend, day1Asset, day2Asset);
            }

            EditorSceneManager.SaveScene(testScene, TestScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[GameUP32 - Faruk] İzole test sahnesi oluşturuldu ve kaydedildi: {TestScenePath}. Play tuşuna basarak tüm akışı test edebilirsiniz!");
        }

        [MenuItem("GameUP32/Faruk (Rules & Data)/4. Hepsini Kur ve Doğrula (Tek Tıkla Tam Kurulum)", false, 40)]
        public static void RunAllSetupAndVerify()
        {
            CreateAndOpenRulesTestScene();
            RulesAcceptanceSuite.RunAllTests(out bool allPassed, out string report);
            Debug.Log(report);
            if (!allPassed)
            {
                throw new System.Exception("Kabul testlerinden bazıları başarısız oldu:\n" + report);
            }
        }

        [MenuItem("GameUP32/Faruk (Rules & Data)/5. Açık Sahnedeki LeadRuntime'a Faruk Backend'ini Bağla", false, 50)]
        public static void BindProductionBackendToLeadRuntimeInActiveScene()
        {
            GenerateAllDataAssets();

            var day1Asset = AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_01_Dairy.asset");
            var day2Asset = AssetDatabase.LoadAssetAtPath<DayDefinition>($"{DaysDir}/Day_02_Fish.asset");

            MonoBehaviour leadRuntime = null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "LeadRuntime")
                {
                    leadRuntime = mb;
                    break;
                }
            }

            if (leadRuntime == null)
            {
                Debug.LogWarning("[GameUP32 - Faruk] Aktif sahnede 'LeadRuntime' bileşeni bulunamadı. Önce LeadPrototype sahnesini açın.");
                return;
            }

            var prodBackend = leadRuntime.gameObject.GetComponent<ProductionInspectionBackend>();
            if (prodBackend == null)
            {
                prodBackend = Undo.AddComponent<ProductionInspectionBackend>(leadRuntime.gameObject);
            }

            if (day1Asset != null && day2Asset != null)
            {
                AssignConfiguredDays(prodBackend, day1Asset, day2Asset);
            }

            SerializedObject runtimeSo = new SerializedObject(leadRuntime);
            SerializedProperty campaignSourceProp = runtimeSo.FindProperty("campaignSource");
            SerializedProperty inspectionBackendProp = runtimeSo.FindProperty("inspectionBackend");

            if (campaignSourceProp != null) campaignSourceProp.objectReferenceValue = prodBackend;
            if (inspectionBackendProp != null) inspectionBackendProp.objectReferenceValue = prodBackend;
            runtimeSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(leadRuntime);
            EditorSceneManager.MarkSceneDirty(leadRuntime.gameObject.scene);

            Debug.Log("[GameUP32 - Faruk] ProductionInspectionBackend başarıyla LeadRuntime (campaignSource & inspectionBackend) alanlarına bağlandı!");
        }

        private static void AssignConfiguredDays(Object targetComponent, DayDefinition day1, DayDefinition day2)
        {
            SerializedObject so = new SerializedObject(targetComponent);
            SerializedProperty daysProp = so.FindProperty("configuredDays");
            if (daysProp != null)
            {
                daysProp.ClearArray();
                daysProp.InsertArrayElementAtIndex(0);
                daysProp.GetArrayElementAtIndex(0).objectReferenceValue = day1;
                daysProp.InsertArrayElementAtIndex(1);
                daysProp.GetArrayElementAtIndex(1).objectReferenceValue = day2;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetComponent);
            }
        }

        private static T SaveOrUpdateAsset<T>(T memoryInstance, string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(memoryInstance, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(memoryInstance);
                return existing;
            }
            else
            {
                AssetDatabase.CreateAsset(memoryInstance, path);
                EditorUtility.SetDirty(memoryInstance);
                return memoryInstance;
            }
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string fullPath = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
#endif
