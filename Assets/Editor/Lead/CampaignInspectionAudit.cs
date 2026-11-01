using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameUp.Lead;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Data = GameUP.Contracts;
using FlowContracts = GameUp.Contracts;

namespace GameUp.Editor
{
    public sealed class CampaignAuditResult
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<CampaignAuditRow> Rows = new List<CampaignAuditRow>();
        public int Checks;
        internal void Check(bool valid, string message) { Checks++; if (!valid) Errors.Add(message); }
    }
    public sealed class CampaignAuditRow
    {
        public Data.DayDefinition Day;
        public Data.ProductDefinition Product;
        public ProductInspectionReport Report;
    }

    /// <summary>Read-only audit of actual saved campaign, reports, both decisions, every bin and full day flow.</summary>
    public static class CampaignInspectionAudit
    {
        public const string ReportPath = "Docs/Reports/PRODUCT_INSPECTION_AUDIT.md";
        public static Data.RuleSnapshot Snapshot(Data.DayDefinition day, int index, bool? emergency = null)
        {
            var rules = day.InitialRules.Select(r => r.ToSnapshotEntry()).ToList();
            bool active = emergency ?? day.EmergencyEvent.HasEmergency && index >= day.EmergencyEvent.TriggerAfterCompletedProducts;
            if (active && day.EmergencyEvent.HasEmergency) rules.Add(day.EmergencyEvent.NewRule.ToSnapshotEntry());
            return new Data.RuleSnapshot(active ? 2 : 1, day.GameDate, rules);
        }
        public static CampaignAuditResult Run(bool inspectScenes = true)
        {
            var result = new CampaignAuditResult();
            var campaign = AssetDatabase.LoadAssetAtPath<Data.CampaignDefinition>(CampaignAuthoring.CatalogPath);
            if (!campaign) throw new InvalidOperationException("Missing campaign asset.");
            var host = new GameObject("__CampaignInspectionAudit");
            try
            {
                var backend = host.AddComponent<ProductionInspectionBackend>(); backend.SetConfiguredDays(campaign.Days); backend.AnalysisSeconds = 0;
                var leadCampaign = backend.CreateCampaign();
                for (int d = 0; d < campaign.Days.Count; d++)
                for (int i = 0; i < campaign.Days[d].Products.Count; i++)
                {
                    var day = campaign.Days[d]; var product = day.Products[i]; var snapshot = Snapshot(day, i);
                    var report = new ProductInspectionReport(product, snapshot);
                    result.Rows.Add(new CampaignAuditRow { Day = day, Product = product, Report = report });
                    string id = product.DefinitionId;
                    var binder = product.Prefab.GetComponent<ProductWorldLabelBinder>();
                    result.Check(binder && binder.ProductDefinition == product, id + ": wrong label/product binding");
                    result.Check(binder && binder.PrintedLabelSignature == ProductWorldLabelBinder.LabelSignature(product), id + ": UV printing is stale; rebake labels");
                    result.Check(product.GetFormattedFrontLabel().Contains(product.ExpiryDate.ToString()) && product.GetFormattedBackLabel().Contains(product.ExpiryDate.ToString()), id + ": printed SKT differs");
                    result.Check(report.EnvironmentalPositive == product.IsPolluted && report.PhysicalSuitable == (product.Condition == Data.FoodCondition.Fresh), id + ": report contradicts sample");
                    result.Check(report.Expired == (product.ExpiryDate < day.GameDate), id + ": report date contradicts shift date");
                    foreach (var surface in binder.PrintedSurfaces)
                    foreach (var material in surface.sharedMaterials)
                    {
                        bool spoiled = material.HasProperty("_Spoilage") && material.GetFloat("_Spoilage") > .5f;
                        result.Check(spoiled == (product.Condition == Data.FoodCondition.Spoiled), id + ": visible spoilage contradicts data");
                        result.Check(AssetDatabase.GetAssetPath(material.mainTexture).StartsWith("Assets/Art/ProductPrints/" + id + "_", StringComparison.Ordinal), id + ": another product's texture");
                    }
                    var visible = leadCampaign.Days[d];
                    var infos = snapshot.ActiveRules.Select(r => new FlowContracts.RuleInfo(r.RuleId, r.PlayerVisibleDescription));
                    // An independent contract derived from the saved product facts, never an authoring Issue flag.
                    bool invalid = IndependentRejection(product, snapshot);
                    result.Check((report.Evaluation.ExpectedDecision == Data.ProductDecision.Reject) == invalid, id + ": scoring disagrees with product facts / active rules");
                    for (int decision = 0; decision < 2; decision++)
                    for (int bin = 0; bin < 4; bin++)
                    {
                        var run = Guid.NewGuid(); backend.BeginRun(run);
                        var session = new FlowContracts.ProductSession(new FlowContracts.ProductKey(run, id), visible.Id, visible.Products[i],
                            new FlowContracts.RuleSnapshot(snapshot.Version, visible.Date, infos));
                        FlowContracts.AnalysisReport first = null, second = null;
                        backend.RequestAnalysis(session, r => first = r); backend.RequestAnalysis(session, r => second = r);
                        result.Check(first != null && ReferenceEquals(first, second) && first.Key.Equals(session.Key) && first.Text == report.Text, id + ": report cache/identity changed");
                        var evaluation = backend.Evaluate(session, (FlowContracts.PlayerDecision)decision);
                        bool correct = (decision == (invalid ? 1 : 0));
                        result.Check(evaluation.Correct == correct, id + ": incorrect approval/rejection scoring");
                        FlowContracts.WasteCategory? waste = decision == 1 ? (FlowContracts.WasteCategory)bin : null;
                        var outcome = backend.CommitOutcome(session, evaluation, waste);
                        bool sorted = bin == (int)ProductRuleEvaluator.GetExpectedWasteBin(product) - 1;
                        int expectedScore = correct ? 10 : decision == 0 ? -15 : -10;
                        if (waste.HasValue) expectedScore += sorted ? 2 : -2;
                        result.Check(outcome.Score == expectedScore && outcome.SortingCorrect == (waste.HasValue ? sorted : (bool?)null), id + ": decision and sorting not accounted separately");
                        result.Check(ReferenceEquals(outcome, backend.CommitOutcome(session, evaluation, waste)) && backend.GetRunSummary().Total == 1, id + ": duplicate outcome was scored");
                    }
                }
                SimulateCampaign(backend, result, true); SimulateCampaign(backend, result, false);
                SimulateMistakes(backend, result);
                if (inspectScenes) InspectSceneBindings(result, campaign);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
            return result;
        }
        private static bool IndependentRejection(Data.ProductDefinition product, Data.RuleSnapshot snapshot)
        {
            foreach (var rule in snapshot.ActiveRules.Where(r => r.AppliesToCategory(product.Category)))
            {
                switch (rule.RuleType)
                {
                    case Data.RuleType.ExpiryDate: if (product.ExpiryDate < snapshot.GameDate) return true; break;
                    case Data.RuleType.VisibleSpoilage: if (product.Condition == Data.FoodCondition.Spoiled) return true; break;
                    case Data.RuleType.IdentityMismatch:
                        if (!string.Equals(product.DeclaredProductType.Trim(), product.ListedProductType.Trim(), StringComparison.OrdinalIgnoreCase)) return true; break;
                    case Data.RuleType.PollutionAnalysis: if (product.IsPolluted) return true; break;
                    case Data.RuleType.SourceRegionBan: if (rule.BannedSourceRegion != Data.SourceRegion.None && product.SourceRegion == rule.BannedSourceRegion) return true; break;
                    default: throw new InvalidOperationException("Extend the independent audit for active rule " + rule.RuleType);
                }
            }
            return false;
        }
        private static void SimulateCampaign(ProductionInspectionBackend backend, CampaignAuditResult result, bool analyze)
        {
            var flow = new DayFlowSession(backend.CreateCampaign(), backend);
            flow.StartNewRun();
            int completed = 0, guard = 0;
            while (flow.State != FlowContracts.FlowState.GameSummary && ++guard < 100)
            {
                if (flow.State == FlowContracts.FlowState.MorningBriefing) { result.Check(flow.DailyMistakes == 0, "Daily mistake count did not reset"); flow.AcknowledgeMorning(); }
                else if (flow.State == FlowContracts.FlowState.EmergencyBriefing) flow.AcknowledgeEmergency();
                else if (flow.State == FlowContracts.FlowState.DaySummary) flow.ContinueAfterSummary();
                else if (flow.State == FlowContracts.FlowState.Working)
                {
                    var current = flow.Current; var product = backend.GetProductDefinition(current.Product.Id);
                    var day = backend.ConfiguredDays[flow.DayIndex];
                    var snapshot = Snapshot(day, flow.ProductIndex);
                    flow.NotifyArrival(current.Key);
                    if (analyze) { flow.RequestAnalysis(current.Key); result.Check(flow.LastReport?.Key.Equals(current.Key) == true, "Flow returned another product's report"); }
                    var decision = IndependentRejection(product, snapshot) ? FlowContracts.PlayerDecision.Reject : FlowContracts.PlayerDecision.Approve;
                    flow.SubmitDecision(current.Key, decision); flow.NotifyDispositionFinished(current.Key);
                    if (decision == FlowContracts.PlayerDecision.Reject)
                    {
                        var waste = (FlowContracts.WasteCategory)((int)ProductRuleEvaluator.GetExpectedWasteBin(product) - 1);
                        flow.BeginSorting(current.Key, waste); flow.NotifySortingFinished(current.Key, waste);
                    }
                    completed++;
                    result.Check(flow.DailyMistakes == 0 && flow.LastOutcome.Decision.Correct, current.Product.Id + ": correct operation increased mistakes");
                    result.Check(!flow.NotifyDispositionFinished(current.Key), current.Product.Id + ": old animation callback accepted twice");
                }
                else { result.Check(false, "Perfect play unexpectedly stopped: " + flow.State); break; }
            }
            result.Check(flow.State == FlowContracts.FlowState.GameSummary && completed == 31 && flow.RunSummary.Score == 340,
                "Full campaign differs with analyze=" + analyze + "; completed=" + completed + "; score=" + flow.RunSummary.Score);
        }
        private static void Complete(ProductionInspectionBackend backend, DayFlowSession flow, FlowContracts.PlayerDecision decision, bool wrongBin = false)
        {
            var current = flow.Current;
            flow.NotifyArrival(current.Key); flow.SubmitDecision(current.Key, decision); flow.NotifyDispositionFinished(current.Key);
            if (decision == FlowContracts.PlayerDecision.Reject)
            {
                int bin = (int)ProductRuleEvaluator.GetExpectedWasteBin(backend.GetProductDefinition(current.Product.Id)) - 1;
                var waste = (FlowContracts.WasteCategory)(wrongBin ? (bin + 1) % 4 : bin);
                flow.BeginSorting(current.Key, waste); flow.NotifySortingFinished(current.Key, waste);
            }
        }
        private static void SimulateMistakes(ProductionInspectionBackend backend, CampaignAuditResult result)
        {
            var flow = new DayFlowSession(backend.CreateCampaign(), backend); flow.StartNewRun();
            int guard = 0, mistakes = 0;
            while (flow.State != FlowContracts.FlowState.GameSummary && ++guard < 100)
            {
                if (flow.State == FlowContracts.FlowState.MorningBriefing) { result.Check(flow.DailyMistakes == 0, "Mistakes were not reset next morning"); flow.AcknowledgeMorning(); }
                else if (flow.State == FlowContracts.FlowState.EmergencyBriefing) flow.AcknowledgeEmergency();
                else if (flow.State == FlowContracts.FlowState.DaySummary) flow.ContinueAfterSummary();
                else if (flow.State == FlowContracts.FlowState.MistakeBriefing)
                { result.Check(flow.DailyMistakes == 1, "One wrong decision counted multiple times"); mistakes++; flow.AcknowledgeMistake(); }
                else if (flow.State == FlowContracts.FlowState.Working)
                {
                    var current = flow.Current; var product = backend.GetProductDefinition(current.Product.Id);
                    bool invalid = IndependentRejection(product, Snapshot(backend.ConfiguredDays[flow.DayIndex], flow.ProductIndex));
                    var expected = invalid ? FlowContracts.PlayerDecision.Reject : FlowContracts.PlayerDecision.Approve;
                    Complete(backend, flow, flow.ProductIndex == 0 ? (expected == FlowContracts.PlayerDecision.Approve ? FlowContracts.PlayerDecision.Reject : FlowContracts.PlayerDecision.Approve) : expected);
                    result.Check(!flow.NotifyDispositionFinished(current.Key), "Duplicate callback changed daily mistakes");
                }
                else { result.Check(false, "Daily-reset simulation stopped: " + flow.State); break; }
            }
            result.Check(flow.State == FlowContracts.FlowState.GameSummary && mistakes == 3, "One mistake each day incorrectly lost the game");
            flow.StartNewRun(); flow.AcknowledgeMorning();
            for (int i = 0; i < 3; i++)
            {
                var current = flow.Current; flow.NotifyArrival(current.Key);
                var expected = backend.Evaluate(current, FlowContracts.PlayerDecision.Approve).Expected;
                Complete(backend, flow, expected == FlowContracts.PlayerDecision.Approve ? FlowContracts.PlayerDecision.Reject : FlowContracts.PlayerDecision.Approve);
                result.Check(flow.DailyMistakes == i + 1 && flow.State == FlowContracts.FlowState.MistakeBriefing, "Wrong decision did not produce exactly one mistake briefing");
                flow.AcknowledgeMistake();
            }
            result.Check(flow.State == FlowContracts.FlowState.GameOver && flow.DailyMistakes == 3, "Three daily mistakes did not lose the game");
            flow.StartNewRun(); result.Check(flow.DailyMistakes == 0, "Restart did not reset mistakes"); flow.AcknowledgeMorning();
            Complete(backend, flow, FlowContracts.PlayerDecision.Reject, true);
            result.Check(flow.DailyMistakes == 2 && !flow.LastOutcome.Decision.Correct && flow.LastOutcome.SortingCorrect == false,
                "Wrong decision and wrong sorting must count separately");
            flow.StartNewRun(); flow.AcknowledgeMorning(); Complete(backend, flow, FlowContracts.PlayerDecision.Approve);
            Complete(backend, flow, FlowContracts.PlayerDecision.Reject, true);
            result.Check(flow.DailyMistakes == 1 && flow.LastOutcome.Decision.Correct && flow.LastOutcome.SortingCorrect == false,
                "A correct rejection was wrongly counted as a decision mistake");
        }
        private static void InspectSceneBindings(CampaignAuditResult result, Data.CampaignDefinition campaign)
        {
            var existing = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ProductionProjectSetup.GamePath);
            bool owned = !existing.isLoaded;
            var scene = owned ? EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath, OpenSceneMode.Additive) : existing;
            try
            {
                var roots = scene.GetRootGameObjects();
                var presentation = roots.SelectMany(r => r.GetComponentsInChildren<GameUP.Interaction.ProductPresentation>(true)).Single();
                var entries = new SerializedObject(presentation).FindProperty("visuals");
                result.Check(entries.arraySize == 31, "Scene product pool count differs");
                var ids = new HashSet<string>();
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var product = (Data.ProductDefinition)entry.FindPropertyRelative("Definition").objectReferenceValue;
                    var visual = (GameObject)entry.FindPropertyRelative("Visual").objectReferenceValue;
                    var labels = (ProductWorldLabelBinder)entry.FindPropertyRelative("Labels").objectReferenceValue;
                    result.Check(product && ids.Add(product.DefinitionId) && visual && labels && labels.gameObject == visual && labels.ProductDefinition == product,
                        "Scene pool entry " + i + " uses the wrong visual/definition");
                    result.Check(product && labels && labels.PrintedLabelSignature == ProductWorldLabelBinder.LabelSignature(product), "Scene pool printing is stale: " + product?.name);
                }
                var backend = roots.SelectMany(r => r.GetComponentsInChildren<ProductionInspectionBackend>(true)).Single(b => b.GetComponent<GameUp.Gameplay.GameSessionController>());
                result.Check(backend.ConfiguredDays.SequenceEqual(campaign.Days), "Scene backend uses a different campaign");
            }
            finally { if (owned) EditorSceneManager.CloseScene(scene, true); }
        }
        public static void Export(CampaignAuditResult result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            var text = new StringBuilder("# Ürün / rapor / karar denetimi\n\n");
            text.AppendLine($"Kontrol: **{result.Checks}**. Hata: **{result.Errors.Count}**. Ürün: **{result.Rows.Count}**.\n");
            text.AppendLine("Unity: GameUP32 → QA → Product inspection audit. Araç kayıtlı veriyi okur; karar veya ürün assetini değiştirmez.\n");
            foreach (var error in result.Errors) text.AppendLine("- HATA: " + error);
            text.AppendLine("\n| Ürün | Vardiya / SKT | Kalıntı | Fiziksel | Tür | Karar | Atık |\n|---|---|---|---|---|---|---|");
            foreach (var row in result.Rows)
                text.AppendLine($"| {row.Product.DefinitionId} / {row.Product.DisplayName} | {row.Day.GameDate} / {row.Product.ExpiryDate} | {(row.Report.EnvironmentalPositive ? "Pozitif" : "Negatif")} | {(row.Report.PhysicalSuitable ? "Uygun" : "Uygunsuz")} | {(row.Report.IdentityVerified ? "Doğrulandı" : "Doğrulanmadı")} | {row.Report.Evaluation.ExpectedDecision.ToTurkishText()} | {ProductRuleEvaluator.GetExpectedWasteBin(row.Product).ToTurkishText()} |");
            foreach (var row in result.Rows) text.AppendLine("\n## " + row.Product.DefinitionId + "\n\n```text\n" + row.Report.Text + "\n```");
            File.WriteAllText(ReportPath, text.ToString(), new UTF8Encoding(false));
        }
        public static void RunBatch()
        {
            var result = Run(); Export(result);
            if (result.Errors.Count > 0) throw new InvalidOperationException(string.Join("\n", result.Errors));
            Debug.Log($"PRODUCT_INSPECTION_AUDIT_OK products={result.Rows.Count} checks={result.Checks} errors=0");
        }
    }

    public sealed class ProductInspectionAuditWindow : EditorWindow
    {
        private CampaignAuditResult result;
        private Vector2 scroll;
        private int selected;
        [MenuItem("GameUP32/QA/Product inspection audit")]
        public static void Open() => GetWindow<ProductInspectionAuditWindow>("Ürün denetimi");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Kayıtlı 31 ürünü; analiz, onay/red, dört atık kutusu, puanlama, UV etiketi, sahne bağlantıları ve analizli/analizsiz üç günlük akışla denetler. Assetleri değiştirmez.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Tüm denetimleri çalıştır"))
                {
                    try { result = CampaignInspectionAudit.Run(); selected = 0; }
                    catch (Exception e) { result = new CampaignAuditResult(); result.Check(false, e.Message); Debug.LogException(e); }
                }
            if (result == null) return;
            if (GUILayout.Button("Unity regresyon testleri / Test Runner")) EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            EditorGUILayout.LabelField($"{result.Rows.Count} ürün • {result.Checks} kontrol • {result.Errors.Count} hata", EditorStyles.boldLabel);
            if (GUILayout.Button("Markdown raporunu dışa aktar")) { CampaignInspectionAudit.Export(result); EditorUtility.RevealInFinder(CampaignInspectionAudit.ReportPath); }
            foreach (var error in result.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (result.Rows.Count == 0) return;
            selected = EditorGUILayout.Popup("Ürün", selected, result.Rows.Select(r => r.Product.DefinitionId + " / " + r.Product.DisplayName).ToArray());
            if (GUILayout.Button("Ürün assetini seç")) Selection.activeObject = result.Rows[selected].Product;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.SelectableLabel(result.Rows[selected].Report.Text, EditorStyles.wordWrappedLabel, GUILayout.Height(900));
            EditorGUILayout.EndScrollView();
        }
    }
}
