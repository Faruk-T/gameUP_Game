using System;
using System.Collections.Generic;
using System.Linq;
using GameUp.Contracts;

namespace GameUp.Lead
{
    /// <summary>Owns sequence and boundaries; judgment and scoring belong to the injected backend.</summary>
    public sealed class DayFlowSession
    {
        private readonly CampaignInfo campaign;
        private readonly IInspectionBackend backend;
        private readonly HashSet<ProductKey> completed = new HashSet<ProductKey>();
        private DecisionEvaluation pendingDecision;
        private bool emergencyAcknowledged;
        private RuleSnapshot activeRules;
        private WasteCategory? pendingWaste;
        public const int MistakeLimit = 3;

        public Guid RunId { get; private set; }
        public FlowState State { get; private set; } = FlowState.Boot;
        public ProductPhase Phase { get; private set; }
        public int DayIndex { get; private set; }
        public int ProductIndex { get; private set; }
        public int DailyMistakes { get; private set; }
        public DayInfo Day => campaign.Days[DayIndex];
        public ProductSession Current { get; private set; }
        public AnalysisReport LastReport { get; private set; }
        public ProductOutcome LastOutcome { get; private set; }
        public bool DocumentOpen { get; private set; }
        public bool CanDecide => State == FlowState.Working && Phase == ProductPhase.Ready && !DocumentOpen;
        public bool CanSort => State == FlowState.Working && Phase == ProductPhase.AwaitingSort && !DocumentOpen;
        public string ClockText => emergencyAcknowledged || State == FlowState.EmergencyBriefing ? "12.00" : "09.00";
        public InspectionSummary DaySummary => backend.GetDaySummary(Day.Id);
        public InspectionSummary RunSummary => backend.GetRunSummary();

        public event Action Changed;
        public event Action<ProductSession> PresentationRequested;
        public event Action<ProductSession, PlayerDecision> RoutingRequested;
        public event Action<ProductSession, WasteCategory> SortingRequested;
        public event Action<AnalysisReport> AnalysisAvailable;
        public event Action<ProductOutcome> ProductCompleted;

        public DayFlowSession(CampaignInfo campaign, IInspectionBackend backend)
        { this.campaign = campaign ?? throw new ArgumentNullException(nameof(campaign)); this.backend = backend ?? throw new ArgumentNullException(nameof(backend)); }

        public void StartNewRun()
        {
            RunId = Guid.NewGuid();
            State = FlowState.Boot; Phase = ProductPhase.None; Current = null;
            LastReport = null; LastOutcome = null; pendingDecision = null; DocumentOpen = false;
            completed.Clear(); DayIndex = 0;
            backend.BeginRun(RunId);
            BeginDay();
        }

        private void BeginDay()
        {
            DailyMistakes = 0;
            ProductIndex = 0; Current = null; LastReport = null; LastOutcome = null;
            emergencyAcknowledged = false;
            activeRules = new RuleSnapshot(1, Day.Date, Day.Rules);
            Phase = ProductPhase.None;
            State = FlowState.MorningBriefing;
            Changed?.Invoke();
        }

        public bool AcknowledgeMorning()
        {
            if (State != FlowState.MorningBriefing) return false;
            State = FlowState.Working;
            SpawnNext();
            return true;
        }

        private void SpawnNext()
        {
            LastReport = null; pendingDecision = null; pendingWaste = null;
            Current = new ProductSession(new ProductKey(RunId, Day.Products[ProductIndex].Id), Day.Id,
                Day.Products[ProductIndex], activeRules);
            Phase = ProductPhase.Arriving;
            Changed?.Invoke();
            PresentationRequested?.Invoke(Current);
        }

        private bool Matches(ProductKey key) => Current != null && Current.Key.Equals(key) && key.RunId == RunId;

        public bool NotifyArrival(ProductKey key)
        {
            if (!Matches(key) || Phase != ProductPhase.Arriving || State != FlowState.Working) return false;
            Phase = ProductPhase.Ready; Changed?.Invoke(); return true;
        }

        public void SetDocumentOpen(bool open)
        {
            if (DocumentOpen == open) return;
            DocumentOpen = open; Changed?.Invoke();
        }

        public bool RequestAnalysis(ProductKey key)
        {
            if (!Matches(key) || !CanDecide) return false;
            if (LastReport != null) { AnalysisAvailable?.Invoke(LastReport); return true; }
            Phase = ProductPhase.Analyzing; Changed?.Invoke();
            backend.RequestAnalysis(Current, ReceiveAnalysis);
            return true;
        }

        public void ReceiveAnalysis(AnalysisReport report)
        {
            if (report == null || !Matches(report.Key) || State != FlowState.Working || Phase != ProductPhase.Analyzing) return;
            LastReport = report; Phase = ProductPhase.Ready;
            // Open the report before listeners can submit a decision on the ready state.
            AnalysisAvailable?.Invoke(report);
            Changed?.Invoke();
        }

        public bool SubmitDecision(ProductKey key, PlayerDecision decision)
        {
            if (!Matches(key) || !CanDecide) return false;
            var evaluation = backend.Evaluate(Current, decision);
            if (evaluation == null || !evaluation.Key.Equals(key) || evaluation.Submitted != decision || evaluation.RuleVersion != Current.Rules.Version)
                throw new InvalidOperationException("The backend returned a mismatched decision.");
            pendingDecision = evaluation;
            Phase = ProductPhase.Routing; Changed?.Invoke();
            RoutingRequested?.Invoke(Current, decision);
            return true;
        }

        public bool NotifyDispositionFinished(ProductKey key)
        {
            if (!Matches(key) || State != FlowState.Working || Phase != ProductPhase.Routing) return false;
            if (pendingDecision.Submitted == PlayerDecision.Approve) CompleteProduct(null);
            else { Phase = ProductPhase.AwaitingSort; Changed?.Invoke(); }
            return true;
        }

        public bool BeginSorting(ProductKey key, WasteCategory waste)
        {
            if (!Matches(key) || !CanSort) return false;
            pendingWaste = waste;
            Phase = ProductPhase.Sorting; Changed?.Invoke();
            SortingRequested?.Invoke(Current, waste);
            return true;
        }

        public bool NotifySortingFinished(ProductKey key, WasteCategory waste)
        {
            if (!Matches(key) || State != FlowState.Working || Phase != ProductPhase.Sorting || pendingWaste != waste) return false;
            CompleteProduct(waste); return true;
        }

        private void CompleteProduct(WasteCategory? waste)
        {
            if (completed.Contains(Current.Key)) return;
            var result = backend.CommitOutcome(Current, pendingDecision, waste);
            if (result == null || !result.Decision.Key.Equals(Current.Key))
                throw new InvalidOperationException("The backend returned a mismatched outcome.");
            completed.Add(Current.Key);
            LastOutcome = result; Phase = ProductPhase.Complete;
            ProductIndex++;
            var completedRun = RunId;
            ProductCompleted?.Invoke(result);
            if (RunId != completedRun) return;
            int errors = (result.Decision.Correct ? 0 : 1) + (result.SortingCorrect == false ? 1 : 0);
            if (errors > 0)
            {
                DailyMistakes = Math.Min(MistakeLimit, DailyMistakes + errors);
                State = FlowState.MistakeBriefing;
                Changed?.Invoke();
                return;
            }
            AdvanceAfterProduct();
        }

        private void AdvanceAfterProduct()
        {
            if (ProductIndex >= Day.Products.Count) State = FlowState.DaySummary;
            else if (!emergencyAcknowledged && Day.Emergency != null && ProductIndex == Day.Emergency.AfterCompletedProducts)
                State = FlowState.EmergencyBriefing;
            else { SpawnNext(); return; }
            Changed?.Invoke();
        }

        public bool AcknowledgeMistake()
        {
            if (State != FlowState.MistakeBriefing) return false;
            if (DailyMistakes >= MistakeLimit)
            {
                State = FlowState.GameOver;
                Phase = ProductPhase.Complete;
                Changed?.Invoke();
            }
            else { State = FlowState.Working; AdvanceAfterProduct(); }
            return true;
        }

        public bool AcknowledgeEmergency()
        {
            if (State != FlowState.EmergencyBriefing || emergencyAcknowledged) return false;
            emergencyAcknowledged = true;
            var rules = activeRules.Rules.Concat(new[] { Day.Emergency.AdditionalRule }).GroupBy(r => r.Id).Select(g => g.First());
            activeRules = new RuleSnapshot(activeRules.Version + 1, Day.Date, rules);
            State = FlowState.Working; SpawnNext(); return true;
        }

        public bool ContinueAfterSummary()
        {
            if (State != FlowState.DaySummary) return false;
            if (DayIndex + 1 < campaign.Days.Count) { DayIndex++; BeginDay(); }
            else { State = FlowState.GameSummary; Current = null; Phase = ProductPhase.None; Changed?.Invoke(); }
            return true;
        }

        public IReadOnlyList<RuleInfo> CurrentRules => activeRules?.Rules ?? Array.Empty<RuleInfo>();
    }
}
