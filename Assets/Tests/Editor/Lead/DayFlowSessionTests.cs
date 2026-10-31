using System;
using System.Collections.Generic;
using System.Linq;
using GameUp.Contracts;
using NUnit.Framework;

namespace GameUp.Lead.Tests
{
    public sealed class DayFlowSessionTests
    {
        private DayFlowSession flow;
        private Backend backend;
        private static readonly RuleInfo BaseRule = new RuleInfo("base", "Base rule");
        [SetUp]
        public void Setup()
        {
            backend = new Backend();
            var first = new DayInfo("one", "One", new DateTime(2026, 11, 1), "news", Products("A", 2), new[] { BaseRule });
            var second = new DayInfo("two", "Two", new DateTime(2026, 11, 2), "news", Products("B", 6), new[] { BaseRule },
                new EmergencyInfo(3, "fax", new RuleInfo("ban", "Ban")));
            flow = new DayFlowSession(new CampaignInfo(new[] { first, second }), backend);
            flow.StartNewRun();
        }
        private static ProductInfo[] Products(string prefix, int count) => Enumerable.Range(1, count).Select(i => new ProductInfo(prefix + i, "Item", "Food", "Label")).ToArray();
        private void Ready()
        {
            if (flow.State == FlowState.MorningBriefing) flow.AcknowledgeMorning();
            flow.NotifyArrival(flow.Current.Key);
        }
        private void Finish(PlayerDecision decision = PlayerDecision.Approve, WasteCategory waste = WasteCategory.Plastic)
        {
            Ready(); var key = flow.Current.Key;
            Assert.That(flow.SubmitDecision(key, decision), Is.True);
            flow.NotifyDispositionFinished(key);
            if (decision == PlayerDecision.Reject) { flow.BeginSorting(key, waste); flow.NotifySortingFinished(key, waste); }
        }
        private void SecondDay() { Finish(); Finish(); flow.ContinueAfterSummary(); Ready(); }

        [Test] public void NewRunWaitsForMorningAcknowledgement()
        { Assert.That(flow.State, Is.EqualTo(FlowState.MorningBriefing)); Assert.That(flow.Current, Is.Null); }
        [Test] public void ArrivalMustFinishBeforeDecision()
        { flow.AcknowledgeMorning(); Assert.That(flow.SubmitDecision(flow.Current.Key, PlayerDecision.Approve), Is.False); }
        [Test] public void DocumentBlocksDecisions()
        { Ready(); flow.SetDocumentOpen(true); Assert.That(flow.SubmitDecision(flow.Current.Key, PlayerDecision.Approve), Is.False); }
        [Test] public void ClosingDocumentDoesNotUnlockRouting()
        { Ready(); flow.SubmitDecision(flow.Current.Key, PlayerDecision.Approve); flow.SetDocumentOpen(true); flow.SetDocumentOpen(false); Assert.That(flow.CanDecide, Is.False); }
        [Test] public void DoubleDecisionIsIgnored()
        { Ready(); var key = flow.Current.Key; flow.SubmitDecision(key, PlayerDecision.Approve); Assert.That(flow.SubmitDecision(key, PlayerDecision.Reject), Is.False); Assert.That(backend.Evaluations, Is.EqualTo(1)); }
        [Test] public void RejectWaitsForSortingBeforeCommit()
        { Ready(); var key = flow.Current.Key; flow.SubmitDecision(key, PlayerDecision.Reject); flow.NotifyDispositionFinished(key); Assert.That(flow.Phase, Is.EqualTo(ProductPhase.AwaitingSort)); Assert.That(backend.Commits, Is.Zero); Assert.That(flow.ProductIndex, Is.Zero); }
        [Test] public void DuplicateCompletionDoesNotAdvanceOrScoreTwice()
        { Ready(); var key = flow.Current.Key; Finish(); Assert.That(flow.NotifyDispositionFinished(key), Is.False); Assert.That(flow.ProductIndex, Is.EqualTo(1)); Assert.That(backend.Commits, Is.EqualTo(1)); }
        [Test] public void SortCallbackMustMatchSelectedWaste()
        { Ready(); var key = flow.Current.Key; flow.SubmitDecision(key, PlayerDecision.Reject); flow.NotifyDispositionFinished(key); flow.BeginSorting(key, WasteCategory.Glass); Assert.That(flow.NotifySortingFinished(key, WasteCategory.Paper), Is.False); Assert.That(backend.Commits, Is.Zero); Assert.That(flow.NotifySortingFinished(key, WasteCategory.Glass), Is.True); }
        [Test] public void SortingCannotStartTwice()
        { Ready(); var key = flow.Current.Key; flow.SubmitDecision(key, PlayerDecision.Reject); flow.NotifyDispositionFinished(key); flow.BeginSorting(key, WasteCategory.Glass); Assert.That(flow.BeginSorting(key, WasteCategory.Paper), Is.False); }
        [Test] public void WrongProductCannotCompleteArrival()
        { flow.AcknowledgeMorning(); Assert.That(flow.NotifyArrival(new ProductKey(flow.RunId, "other")), Is.False); Assert.That(flow.Phase, Is.EqualTo(ProductPhase.Arriving)); }
        [Test] public void AnalysisBlocksDecisionAndDuplicateRequest()
        { Ready(); var key = flow.Current.Key; flow.RequestAnalysis(key); Assert.That(flow.CanDecide, Is.False); Assert.That(flow.RequestAnalysis(key), Is.False); }
        [Test] public void WrongAnalysisDoesNotUnlockProduct()
        { Ready(); flow.RequestAnalysis(flow.Current.Key); flow.ReceiveAnalysis(new AnalysisReport(new ProductKey(flow.RunId, "other"), "wrong", "bad")); Assert.That(flow.Phase, Is.EqualTo(ProductPhase.Analyzing)); }
        [Test] public void AnalysisIsCachedPerProduct()
        { Ready(); var key = flow.Current.Key; flow.RequestAnalysis(key); backend.Pending(new AnalysisReport(key, "report", "clean")); flow.RequestAnalysis(key); Assert.That(backend.Analyses, Is.EqualTo(1)); }
        [Test] public void RestartRejectsOldCallbackEvenWhenProductIdRepeats()
        { Ready(); var oldKey = flow.Current.Key; flow.RequestAnalysis(oldKey); var oldCallback = backend.Pending; flow.StartNewRun(); Ready(); flow.RequestAnalysis(flow.Current.Key); oldCallback(new AnalysisReport(oldKey, "stale", "bad")); Assert.That(flow.LastReport, Is.Null); Assert.That(flow.Phase, Is.EqualTo(ProductPhase.Analyzing)); }
        [Test] public void EmergencyWaitsForThirdProductSorting()
        { SecondDay(); Finish(); Finish(); Ready(); var key = flow.Current.Key; flow.SubmitDecision(key, PlayerDecision.Reject); flow.NotifyDispositionFinished(key); Assert.That(flow.State, Is.EqualTo(FlowState.Working)); flow.BeginSorting(key, WasteCategory.Plastic); flow.NotifySortingFinished(key, WasteCategory.Plastic); Assert.That(flow.State, Is.EqualTo(FlowState.EmergencyBriefing)); Assert.That(flow.ProductIndex, Is.EqualTo(3)); }
        [Test] public void EmergencyCreatesNewSnapshotWithoutChangingOldOne()
        { SecondDay(); var old = flow.Current.Rules; Finish(); Finish(); Finish(); flow.AcknowledgeEmergency(); Assert.That(old.Version, Is.EqualTo(1)); Assert.That(old.Rules.Count, Is.EqualTo(1)); Assert.That(flow.Current.Rules.Version, Is.EqualTo(2)); Assert.That(flow.Current.Rules.Rules.Count, Is.EqualTo(2)); }
        [Test] public void EmergencyCanOnlyBeAcknowledgedOnce()
        { SecondDay(); Finish(); Finish(); Finish(); Assert.That(flow.AcknowledgeEmergency(), Is.True); Assert.That(flow.AcknowledgeEmergency(), Is.False); Assert.That(flow.Current.Rules.Version, Is.EqualTo(2)); }
        [Test] public void SnapshotCopiesMutableRuleList()
        { var source = new[] { BaseRule }; var snapshot = new RuleSnapshot(1, DateTime.Today, source); source[0] = new RuleInfo("replacement", "replacement"); Assert.That(snapshot.Rules[0].Id, Is.EqualTo("base")); }
        [Test] public void DayDoesNotAdvanceWithoutSummaryContinue()
        { Finish(); Finish(); Assert.That(flow.State, Is.EqualTo(FlowState.DaySummary)); Assert.That(flow.DayIndex, Is.Zero); }
        [Test] public void LastDayEndsInFinalSummary()
        { SecondDay(); Finish(); Finish(); Finish(); flow.AcknowledgeEmergency(); Finish(); Finish(); Finish(); flow.ContinueAfterSummary(); Assert.That(flow.State, Is.EqualTo(FlowState.GameSummary)); Assert.That(flow.RunSummary.Total, Is.EqualTo(8)); }
        [Test] public void RestartClearsResultsRulesAndRunIdentity()
        { SecondDay(); Finish(); Finish(); Finish(); flow.AcknowledgeEmergency(); var previous = flow.RunId; flow.StartNewRun(); Assert.That(flow.RunId, Is.Not.EqualTo(previous)); Assert.That(flow.DayIndex, Is.Zero); Assert.That(flow.RunSummary.Total, Is.Zero); Assert.That(flow.CurrentRules.Count, Is.EqualTo(1)); Assert.That(flow.LastReport, Is.Null); }
        [Test] public void DuplicateCampaignProductIdsAreRejected()
        { var day = new DayInfo("x", "x", DateTime.Today, "", Products("A", 1), new[] { BaseRule }); var other = new DayInfo("y", "y", DateTime.Today, "", Products("A", 1), new[] { BaseRule }); Assert.Throws<ArgumentException>(() => new CampaignInfo(new[] { day, other })); }
        [Test] public void RestartDuringCompletionNotificationDoesNotSpawnBeforeNewspaper()
        { flow.ProductCompleted += _ => flow.StartNewRun(); Finish(); Assert.That(flow.State, Is.EqualTo(FlowState.MorningBriefing)); Assert.That(flow.Current, Is.Null); Assert.That(flow.RunSummary.Total, Is.Zero); }

        private sealed class Backend : IInspectionBackend
        {
            public int Evaluations, Commits, Analyses;
            public Action<AnalysisReport> Pending;
            private readonly List<(string day, ProductOutcome result)> results = new List<(string, ProductOutcome)>();
            public void BeginRun(Guid runId) { results.Clear(); Evaluations = Commits = Analyses = 0; }
            public void RequestAnalysis(ProductSession session, Action<AnalysisReport> completed) { Analyses++; Pending = completed; }
            public DecisionEvaluation Evaluate(ProductSession session, PlayerDecision decision)
            { Evaluations++; return new DecisionEvaluation(session.Key, decision, PlayerDecision.Approve, "fixture", session.Rules.Version); }
            public ProductOutcome CommitOutcome(ProductSession session, DecisionEvaluation decision, WasteCategory? waste)
            { Commits++; var result = new ProductOutcome(decision, waste.HasValue ? true : (bool?)null, 10); results.Add((session.DayId, result)); return result; }
            public InspectionSummary GetDaySummary(string dayId) => Summary(results.Where(r => r.day == dayId).Select(r => r.result));
            public InspectionSummary GetRunSummary() => Summary(results.Select(r => r.result));
            private static InspectionSummary Summary(IEnumerable<ProductOutcome> source)
            { var items = source.ToArray(); return new InspectionSummary(items.Length, items.Count(x => x.Decision.Correct), 0, 0, 0, 0, items.Sum(x => x.Score)); }
        }
    }
}
