using System;
using System.Collections;
using System.Collections.Generic;
using GameUp.Contracts;
using UnityEngine;
using Data = GameUP.Contracts;
using static GameUP.Contracts.GameUpTextFormatter;

namespace GameUP.Rules
{
    /// <summary>Configured assets and Faruk's evaluator are the only production source of judgment.</summary>
    public sealed class ProductionInspectionBackend : MonoBehaviour, IInspectionBackend, ICampaignSource
    {
        [SerializeField] private List<Data.DayDefinition> configuredDays = new List<Data.DayDefinition>();
        [SerializeField, Min(0)] private float analysisSeconds = 1.25f;
        private readonly Dictionary<string, Data.ProductDefinition> products = new Dictionary<string, Data.ProductDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Data.RuleDefinition> rules = new Dictionary<string, Data.RuleDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Data.RuleSnapshotEntry> ruleEntries = new Dictionary<string, Data.RuleSnapshotEntry>(StringComparer.Ordinal);
        private readonly Dictionary<ProductKey, AnalysisReport> reports = new Dictionary<ProductKey, AnalysisReport>();
        private readonly Dictionary<ProductKey, ProductOutcome> outcomes = new Dictionary<ProductKey, ProductOutcome>();
        private readonly Dictionary<string, InspectionSummary> dayResults = new Dictionary<string, InspectionSummary>(StringComparer.Ordinal);
        private InspectionSummary runResult = EmptySummary();
        private Guid activeRun;
        private bool indexed;

        public IReadOnlyList<Data.DayDefinition> ConfiguredDays => configuredDays;
        public float AnalysisSeconds { get => analysisSeconds; set => analysisSeconds = Mathf.Max(0, value); }

        public void SetConfiguredDays(IEnumerable<Data.DayDefinition> days)
        {
            if (activeRun != Guid.Empty) throw new InvalidOperationException("Campaign content cannot change during a run.");
            configuredDays = new List<Data.DayDefinition>(days ?? throw new ArgumentNullException(nameof(days)));
            indexed = false;
            EnsureCatalogIndexed();
        }

        public bool TryGetProductDefinition(string productId, out Data.ProductDefinition definition)
        {
            EnsureCatalogIndexed();
            return products.TryGetValue(productId, out definition);
        }

        public Data.ProductDefinition GetProductDefinition(string productId)
        {
            EnsureCatalogIndexed();
            if (!products.TryGetValue(productId, out var definition))
                throw new InvalidOperationException("Unconfigured product: " + productId);
            return definition;
        }

        public CampaignInfo CreateCampaign()
        {
            EnsureCatalogIndexed();
            var days = new List<DayInfo>(configuredDays.Count);
            foreach (var day in configuredDays)
            {
                var items = new List<ProductInfo>(day.Products.Count);
                foreach (var item in day.Products)
                    items.Add(new ProductInfo(item.DefinitionId, item.DisplayName, item.Category.ToTurkishText(),
                        item.GetFormattedFrontLabel() + "\n\n" + item.GetFormattedBackLabel()));
                var visibleRules = new List<RuleInfo>(day.InitialRules.Count);
                foreach (var rule in day.InitialRules) visibleRules.Add(new RuleInfo(rule.RuleId, rule.PlayerVisibleDescription));
                EmergencyInfo emergency = null;
                if (day.EmergencyEvent.HasEmergency)
                {
                    var spec = day.EmergencyEvent;
                    if (!spec.NewRule || !spec.EmergencyDocument) throw new InvalidOperationException("Emergency content is incomplete: " + day.DayId);
                    emergency = new EmergencyInfo(spec.TriggerAfterCompletedProducts, DocumentText(spec.EmergencyDocument),
                        new RuleInfo(spec.NewRule.RuleId, spec.NewRule.PlayerVisibleDescription));
                }
                var date = day.GameDate;
                days.Add(new DayInfo(LeadDayId(day.DayId), day.MorningNewspaper.Title,
                    new DateTime(date.Year, date.Month, date.Day), DocumentText(day.MorningNewspaper), items, visibleRules, emergency));
            }
            return new CampaignInfo(days);
        }

        public void BeginRun(Guid runId)
        {
            if (runId == Guid.Empty) throw new ArgumentException("A run ID is required.", nameof(runId));
            EnsureCatalogIndexed();
            StopAllCoroutines();
            activeRun = runId; reports.Clear(); outcomes.Clear(); dayResults.Clear(); runResult = EmptySummary();
            foreach (var day in configuredDays) dayResults.Add(LeadDayId(day.DayId), EmptySummary());
        }

        public void RequestAnalysis(ProductSession session, Action<AnalysisReport> completed)
        {
            RequireActiveRun(session);
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (!isActiveAndEnabled) throw new InvalidOperationException("Inspection backend must be active.");
            if (reports.TryGetValue(session.Key, out var cached)) { completed(cached); return; }
            if (analysisSeconds == 0) DeliverReport(session, completed);
            else StartCoroutine(Analyze(session, completed));
        }

        private IEnumerator Analyze(ProductSession session, Action<AnalysisReport> completed)
        {
            yield return new WaitForSeconds(analysisSeconds);
            if (session.Key.RunId == activeRun) DeliverReport(session, completed);
        }

        private void DeliverReport(ProductSession session, Action<AnalysisReport> completed)
        {
            if (!reports.TryGetValue(session.Key, out var report))
            {
                var item = GetProductDefinition(session.Product.Id);
                string packaging = item.IsPackagingContaminated ? "Ambalaj kontamine: ayrı imha hedefine gönder."
                    : "Ambalaj: temiz " + item.PackagingMaterial.ToTurkishText().ToLowerInvariant() + ".";
                report = new AnalysisReport(session.Key, "R-" + session.Key.ProductId, item.AnalysisFindingDetail + "\n\n" + packaging);
                reports.Add(session.Key, report);
            }
            completed(report);
        }

        public DecisionEvaluation Evaluate(ProductSession session, PlayerDecision decision)
        {
            RequireActiveRun(session);
            var snapshotEntries = new List<Data.RuleSnapshotEntry>(session.Rules.Rules.Count);
            foreach (var info in session.Rules.Rules)
            {
                if (!ruleEntries.TryGetValue(info.Id, out var entry)) throw new InvalidOperationException("Unconfigured active rule: " + info.Id);
                snapshotEntries.Add(entry);
            }
            var date = session.Rules.GameDate;
            var snapshot = new Data.RuleSnapshot(session.Rules.Version, new Data.GameDate(date.Day, date.Month, date.Year), snapshotEntries);
            var evaluation = ProductRuleEvaluator.EvaluateDecision(session.Key.RunId.ToString("N"), session.Key.ProductId,
                GetProductDefinition(session.Product.Id), snapshot,
                decision == PlayerDecision.Approve ? Data.ProductDecision.Approve : Data.ProductDecision.Reject);
            string reason = evaluation.FailureReasons.Count == 0 ? "Ürün yürürlükteki kurallara uygun." : string.Join(" ", evaluation.FailureReasons);
            return new DecisionEvaluation(session.Key, decision,
                evaluation.ExpectedDecision == Data.ProductDecision.Approve ? PlayerDecision.Approve : PlayerDecision.Reject, reason, session.Rules.Version);
        }

        public ProductOutcome CommitOutcome(ProductSession session, DecisionEvaluation decision, WasteCategory? waste)
        {
            RequireActiveRun(session);
            if (decision == null || !decision.Key.Equals(session.Key)) throw new InvalidOperationException("Mismatched outcome identity.");
            if (outcomes.TryGetValue(session.Key, out var existing)) return existing;
            if ((decision.Submitted == PlayerDecision.Reject) != waste.HasValue) throw new InvalidOperationException("Rejected products require a completed sorting selection.");
            bool? sorted = waste.HasValue
                ? ProductRuleEvaluator.GetExpectedWasteBin(GetProductDefinition(session.Product.Id)) == ToDataWaste(waste.Value)
                : (bool?)null;
            int score = decision.Correct ? ProductRuleEvaluator.ScoreCorrectDecision
                : decision.Submitted == PlayerDecision.Approve ? ProductRuleEvaluator.ScoreFalseAcceptance : ProductRuleEvaluator.ScoreFalseRejection;
            if (sorted.HasValue) score += sorted.Value ? ProductRuleEvaluator.ScoreCorrectSorting : ProductRuleEvaluator.ScoreWrongSorting;
            var outcome = new ProductOutcome(decision, sorted, score);
            outcomes.Add(session.Key, outcome);
            dayResults[session.DayId] = Add(dayResults[session.DayId], outcome);
            runResult = Add(runResult, outcome);
            return outcome;
        }

        public InspectionSummary GetDaySummary(string dayId) => dayResults[dayId];
        public InspectionSummary GetRunSummary() => runResult;

        private void EnsureCatalogIndexed()
        {
            if (indexed) return;
            if (configuredDays == null || configuredDays.Count == 0) throw new InvalidOperationException("Assign campaign DayDefinition assets before starting the game.");
            products.Clear(); rules.Clear(); ruleEntries.Clear();
            foreach (var day in configuredDays)
            {
                if (!day || string.IsNullOrWhiteSpace(day.DayId) || !day.MorningNewspaper || day.Products.Count == 0)
                    throw new InvalidOperationException("Campaign contains an incomplete day.");
                foreach (var rule in day.InitialRules) IndexRule(rule);
                if (day.EmergencyEvent.HasEmergency) IndexRule(day.EmergencyEvent.NewRule);
                foreach (var item in day.Products)
                {
                    if (!item || string.IsNullOrWhiteSpace(item.DefinitionId) || products.ContainsKey(item.DefinitionId))
                        throw new InvalidOperationException("Campaign products must have unique configured identities.");
                    products.Add(item.DefinitionId, item);
                }
            }
            indexed = true;
        }

        private void IndexRule(Data.RuleDefinition rule)
        {
            if (!rule || string.IsNullOrWhiteSpace(rule.RuleId)) throw new InvalidOperationException("A campaign rule is missing.");
            if (rules.TryGetValue(rule.RuleId, out var existing))
            { if (existing != rule) throw new InvalidOperationException("Conflicting rule identity: " + rule.RuleId); return; }
            rules.Add(rule.RuleId, rule); ruleEntries.Add(rule.RuleId, rule.ToSnapshotEntry());
        }
        private void RequireActiveRun(ProductSession session)
        { if (session == null || activeRun == Guid.Empty || session.Key.RunId != activeRun) throw new InvalidOperationException("Stale inspection session."); }
        private static string LeadDayId(string id) => id == "DAY_01" ? "day-1" : id == "DAY_02" ? "day-2" : id;
        private static string DocumentText(Data.DocumentDefinition document) => document.Body + "\n\n" + document.TaskInstructionBox;
        private static InspectionSummary EmptySummary() => new InspectionSummary(0, 0, 0, 0, 0, 0, 0);
        private static InspectionSummary Add(InspectionSummary s, ProductOutcome o) => new InspectionSummary(s.Total + 1,
            s.Correct + (o.Decision.Correct ? 1 : 0), s.FalseAccepts + (!o.Decision.Correct && o.Decision.Submitted == PlayerDecision.Approve ? 1 : 0),
            s.FalseRejects + (!o.Decision.Correct && o.Decision.Submitted == PlayerDecision.Reject ? 1 : 0),
            s.Sorted + (o.SortingCorrect.HasValue ? 1 : 0), s.CorrectSorts + (o.SortingCorrect == true ? 1 : 0), s.Score + o.Score);
        private static Data.WasteBinType ToDataWaste(WasteCategory value) => value switch
        { WasteCategory.Paper => Data.WasteBinType.Paper, WasteCategory.Glass => Data.WasteBinType.Glass,
          WasteCategory.Plastic => Data.WasteBinType.Plastic, WasteCategory.Contaminated => Data.WasteBinType.Contaminated,
          _ => throw new ArgumentOutOfRangeException(nameof(value)) };
    }
}

