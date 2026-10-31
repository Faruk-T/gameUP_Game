using System;

namespace GameUP.Contracts
{
    // =========================================================================
    // BÖLÜM 7 — OLAY YÜKLERİ (EVENT PAYLOADS)
    // Her ürünle ilişkili yük RunId ve ProductId taşır.
    // Yanlış oturuma (RunId) veya eski ürüne (ProductId) gelen olay akışı ilerletmez.
    // =========================================================================

    [Serializable]
    public readonly struct BeginProductEvent
    {
        public readonly string RunId;
        public readonly string DayId;
        public readonly string ProductId;
        public readonly int SequenceIndex;
        public readonly ProductDefinition Definition;
        public readonly RuleSnapshot RuleSnapshot;

        public BeginProductEvent(
            string runId,
            string dayId,
            string productId,
            int sequenceIndex,
            ProductDefinition definition,
            RuleSnapshot ruleSnapshot)
        {
            RunId = runId;
            DayId = dayId;
            ProductId = productId;
            SequenceIndex = sequenceIndex;
            Definition = definition;
            RuleSnapshot = ruleSnapshot;
        }
    }

    [Serializable]
    public readonly struct ArrivalFinishedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;

        public ArrivalFinishedEvent(string runId, string productId)
        {
            RunId = runId;
            ProductId = productId;
        }
    }

    [Serializable]
    public readonly struct AnalysisRequestedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;

        public AnalysisRequestedEvent(string runId, string productId)
        {
            RunId = runId;
            ProductId = productId;
        }
    }

    [Serializable]
    public readonly struct AnalysisReadyEvent
    {
        public readonly string RunId;
        public readonly string ProductId;
        public readonly AnalysisReport Report;

        public AnalysisReadyEvent(string runId, string productId, AnalysisReport report)
        {
            RunId = runId;
            ProductId = productId;
            Report = report;
        }
    }

    [Serializable]
    public readonly struct DecisionSubmittedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;
        public readonly ProductDecision Decision;

        public DecisionSubmittedEvent(string runId, string productId, ProductDecision decision)
        {
            RunId = runId;
            ProductId = productId;
            Decision = decision;
        }
    }

    [Serializable]
    public readonly struct DecisionEvaluatedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;
        public readonly DecisionEvaluation Evaluation;

        public DecisionEvaluatedEvent(string runId, string productId, DecisionEvaluation evaluation)
        {
            RunId = runId;
            ProductId = productId;
            Evaluation = evaluation;
        }
    }

    [Serializable]
    public readonly struct DispositionFinishedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;
        public readonly ProductDecision ExecutedDecision;

        public DispositionFinishedEvent(string runId, string productId, ProductDecision executedDecision)
        {
            RunId = runId;
            ProductId = productId;
            ExecutedDecision = executedDecision;
        }
    }

    [Serializable]
    public readonly struct SortingFinishedEvent
    {
        public readonly string RunId;
        public readonly string ProductId;
        public readonly WasteBinType SelectedBin;

        public SortingFinishedEvent(string runId, string productId, WasteBinType selectedBin)
        {
            RunId = runId;
            ProductId = productId;
            SelectedBin = selectedBin;
        }
    }

    [Serializable]
    public readonly struct ProductCompletedEvent
    {
        public readonly string RunId;
        public readonly string DayId;
        public readonly string ProductId;
        public readonly ProductOutcome Outcome;
        public readonly int CompletedProductsInDay;
        public readonly bool ShouldTriggerEmergency;

        public ProductCompletedEvent(
            string runId,
            string dayId,
            string productId,
            ProductOutcome outcome,
            int completedProductsInDay,
            bool shouldTriggerEmergency)
        {
            RunId = runId;
            DayId = dayId;
            ProductId = productId;
            Outcome = outcome;
            CompletedProductsInDay = completedProductsInDay;
            ShouldTriggerEmergency = shouldTriggerEmergency;
        }
    }

    [Serializable]
    public readonly struct EmergencyAcknowledgedEvent
    {
        public readonly string RunId;
        public readonly string DayId;

        public EmergencyAcknowledgedEvent(string runId, string dayId)
        {
            RunId = runId;
            DayId = dayId;
        }
    }

    [Serializable]
    public readonly struct EmergencyActivatedEvent
    {
        public readonly string RunId;
        public readonly string DayId;
        public readonly string ActivatedRuleId;
        public readonly string DisplayedClockTime;
        public readonly RuleSnapshot UpdatedRuleSnapshot;

        public EmergencyActivatedEvent(
            string runId,
            string dayId,
            string activatedRuleId,
            string displayedClockTime,
            RuleSnapshot updatedRuleSnapshot)
        {
            RunId = runId;
            DayId = dayId;
            ActivatedRuleId = activatedRuleId;
            DisplayedClockTime = displayedClockTime;
            UpdatedRuleSnapshot = updatedRuleSnapshot;
        }
    }

    /// <summary>
    /// Kişi 3 (Faruk) tarafından Kişi 4 (Teknik Lider) ve Kişi 2 (Özgür) için sunulan
    /// değerlendirme, analiz, kural snapshot ve sonuç kayıt sözleşmesi (Bölüm 6, 7, 9, 11, 12).
    /// </summary>
    public interface IRulesRuntimeService
    {
        string CurrentRunId { get; }
        string CurrentDayId { get; }
        RuleSnapshot CurrentDayRuleSnapshot { get; }
        ProductSession ActiveProductSession { get; }
        bool IsEmergencyActiveInCurrentDay { get; }

        // Olaylar (Bölüm 7 tablosundaki yönlerle birebir uyumlu)
        event Action<BeginProductEvent> OnProductBegun;
        event Action<AnalysisReadyEvent> OnAnalysisReady;
        event Action<DecisionEvaluatedEvent> OnDecisionEvaluated;
        event Action<ProductCompletedEvent> OnProductCompleted;
        event Action<EmergencyActivatedEvent> OnEmergencyActivated;

        // Oturum ve Gün Başlatma
        string StartNewRun(string customRunId = null);
        RuleSnapshot BeginDay(DayDefinition dayDefinition);
        RuleSnapshot BeginDay(string dayId, GameDate gameDate, RuleDefinition[] initialRules, EmergencyDefinition emergencyDefinition = null);

        // Bölüm 7 Komutları
        ProductSession BeginProduct(ProductDefinition productDefinition, string customProductId = null, int sequenceIndex = 0);
        bool RequestAnalysis(string runId, string productId, out AnalysisReport report);
        bool SubmitDecision(string runId, string productId, ProductDecision playerDecision, out DecisionEvaluation evaluation);
        bool NotifyDispositionFinished(string runId, string productId, out ProductOutcome committedApproveOutcome);
        bool SubmitSortingFinished(string runId, string productId, WasteBinType selectedBin, out ProductOutcome committedRejectOutcome);
        bool CommitOutcome(string runId, string productId, out ProductOutcome outcome);
        bool ActivateEmergency(string runId, RuleDefinition emergencyRule = null);

        // Özet ve Sorgular
        DaySummaryReport GetDaySummary(string dayId = null);
        GameSummaryReport GetGameSummary();
        string GetCurrentRulebookText();
    }
}

