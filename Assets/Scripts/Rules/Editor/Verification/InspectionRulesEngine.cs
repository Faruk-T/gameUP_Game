using System;
using System.Collections.Generic;
using GameUP.Contracts;

namespace GameUP.Rules
{
    /// <summary>
    /// Kişi 3 (Faruk) — Oturum, Kural Snapshot, Analiz, Değerlendirme, Acil Kural ve Sonuç Kayıt Motoru
    /// (IMPLEMENTATION.md Bölüm 6, 7, 9, 11, 12 ve R01–R09).
    /// </summary>
    public sealed class InspectionRulesEngine : IRulesRuntimeService
    {
        private readonly RunResultsStore resultsStore = new RunResultsStore();
        private readonly List<RuleSnapshotEntry> currentDayActiveRules = new List<RuleSnapshotEntry>();

        private string currentRunId = string.Empty;
        private string currentDayId = string.Empty;
        private GameDate currentGameDate = new GameDate(1, 11, 2026);
        private int currentSnapshotVersion = 1;
        private RuleSnapshot currentDayRuleSnapshot;
        private EmergencyDefinition currentDayEmergency;

        private bool isEmergencyTriggeredInCurrentDay;
        private bool isEmergencyActiveInCurrentDay;

        private ProductSession activeProductSession;

        public RunResultsStore ResultsStore => resultsStore;
        public string CurrentRunId => currentRunId;
        public string CurrentDayId => currentDayId;
        public GameDate CurrentGameDate => currentGameDate;
        public RuleSnapshot CurrentDayRuleSnapshot => currentDayRuleSnapshot;
        public ProductSession ActiveProductSession => activeProductSession;
        public bool IsEmergencyTriggeredInCurrentDay => isEmergencyTriggeredInCurrentDay;
        public bool IsEmergencyActiveInCurrentDay => isEmergencyActiveInCurrentDay;

        // Bölüm 7 Olayları
        public event Action<BeginProductEvent> OnProductBegun;
        public event Action<AnalysisReadyEvent> OnAnalysisReady;
        public event Action<DecisionEvaluatedEvent> OnDecisionEvaluated;
        public event Action<ProductCompletedEvent> OnProductCompleted;
        public event Action<EmergencyActivatedEvent> OnEmergencyActivated;

        public InspectionRulesEngine()
        {
            StartNewRun();
        }

        /// <summary>
        /// Yeni oyun oturumu başlatır; tüm eski sonuçları, açık ürünleri ve acil kural eklerini temizler (L07, T17).
        /// </summary>
        public string StartNewRun(string customRunId = null)
        {
            currentRunId = string.IsNullOrWhiteSpace(customRunId)
                ? $"RUN-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}"
                : customRunId;

            resultsStore.ResetForNewRun(currentRunId);
            currentDayActiveRules.Clear();
            currentDayId = string.Empty;
            currentSnapshotVersion = 1;
            currentDayRuleSnapshot = new RuleSnapshot(currentSnapshotVersion, currentGameDate, Array.Empty<RuleSnapshotEntry>());
            currentDayEmergency = null;
            isEmergencyTriggeredInCurrentDay = false;
            isEmergencyActiveInCurrentDay = false;
            activeProductSession = null;

            return currentRunId;
        }

        /// <summary>
        /// Günü DayDefinition asset'i ile başlatır ve başlangıç RuleSnapshot'ını oluşturur.
        /// </summary>
        public RuleSnapshot BeginDay(DayDefinition dayDefinition)
        {
            if (dayDefinition == null)
                throw new ArgumentNullException(nameof(dayDefinition));

            var initialRulesArray = dayDefinition.InitialRules != null
                ? new List<RuleDefinition>(dayDefinition.InitialRules).ToArray()
                : Array.Empty<RuleDefinition>();

            return BeginDay(
                dayId: dayDefinition.DayId,
                gameDate: dayDefinition.GameDate,
                initialRules: initialRulesArray,
                emergencyDefinition: dayDefinition.EmergencyEvent
            );
        }

        /// <summary>
        /// Günü doğrudan parametrelerle başlatır.
        /// </summary>
        public RuleSnapshot BeginDay(
            string dayId,
            GameDate gameDate,
            RuleDefinition[] initialRules,
            EmergencyDefinition emergencyDefinition = null)
        {
            if (string.IsNullOrWhiteSpace(currentRunId))
            {
                StartNewRun();
            }

            currentDayId = string.IsNullOrWhiteSpace(dayId) ? "DAY_01" : dayId;
            currentGameDate = gameDate;
            currentSnapshotVersion = 1;
            currentDayEmergency = emergencyDefinition;
            isEmergencyTriggeredInCurrentDay = false;
            isEmergencyActiveInCurrentDay = false;
            activeProductSession = null;

            currentDayActiveRules.Clear();
            if (initialRules != null)
            {
                for (int i = 0; i < initialRules.Length; i++)
                {
                    RuleDefinition def = initialRules[i];
                    if (def != null && !ContainsRuleEntry(def.RuleId))
                    {
                        currentDayActiveRules.Add(def.ToSnapshotEntry());
                    }
                }
            }

            currentDayRuleSnapshot = new RuleSnapshot(currentSnapshotVersion, currentGameDate, currentDayActiveRules);
            return currentDayRuleSnapshot;
        }

        /// <summary>
        /// Bölüm 7 — BeginProduct: Yeni ürünü o anki sabitlenmiş RuleSnapshot ile başlatır.
        /// </summary>
        public ProductSession BeginProduct(
            ProductDefinition productDefinition,
            string customProductId = null,
            int sequenceIndex = 0)
        {
            if (productDefinition == null)
                throw new ArgumentNullException(nameof(productDefinition));

            if (string.IsNullOrWhiteSpace(currentRunId))
            {
                StartNewRun();
            }

            if (currentDayRuleSnapshot == null)
            {
                currentDayRuleSnapshot = new RuleSnapshot(currentSnapshotVersion, currentGameDate, currentDayActiveRules);
            }

            string resolvedProductId = string.IsNullOrWhiteSpace(customProductId)
                ? productDefinition.DefinitionId
                : customProductId;

            // Ürün başladığı anda mevcut RuleSnapshot referansı sabitlenir (immutable).
            activeProductSession = new ProductSession(
                runId: currentRunId,
                dayId: currentDayId,
                productId: resolvedProductId,
                sequenceIndex: sequenceIndex,
                definition: productDefinition,
                snapshot: currentDayRuleSnapshot
            );

            OnProductBegun?.Invoke(new BeginProductEvent(
                runId: activeProductSession.RunId,
                dayId: activeProductSession.DayId,
                productId: activeProductSession.ProductId,
                sequenceIndex: activeProductSession.SequenceIndex,
                definition: activeProductSession.Definition,
                ruleSnapshot: activeProductSession.Snapshot
            ));

            return activeProductSession;
        }

        /// <summary>
        /// Bölüm 7 — AnalysisRequested -> AnalysisReady (R03, T03, T09).
        /// </summary>
        public bool RequestAnalysis(string runId, string productId, out AnalysisReport report)
        {
            if (!ProductAnalysisProcessor.TryGenerateOrGetReport(activeProductSession, runId, productId, out report))
            {
                return false;
            }

            OnAnalysisReady?.Invoke(new AnalysisReadyEvent(runId, productId, report));
            return true;
        }

        /// <summary>
        /// Bölüm 7 — DecisionSubmitted -> DecisionEvaluated (R04, T08).
        /// İlk geçerli kararı kabul eder ve sabitlenmiş RuleSnapshot ile değerlendirir.
        /// </summary>
        public bool SubmitDecision(
            string runId,
            string productId,
            ProductDecision playerDecision,
            out DecisionEvaluation evaluation)
        {
            evaluation = null;
            if (!IsMatchingActiveSession(runId, productId))
                return false;

            // T08: Aynı ürün için zaten karar verilmişse ikinci kez kabul edilmez.
            if (activeProductSession.HasDecisionEvaluation || activeProductSession.IsOutcomeCommitted)
                return false;

            evaluation = ProductRuleEvaluator.EvaluateDecision(
                runId: activeProductSession.RunId,
                productId: activeProductSession.ProductId,
                product: activeProductSession.Definition,
                snapshot: activeProductSession.Snapshot,
                playerDecision: playerDecision
            );

            if (!activeProductSession.TrySetDecisionEvaluation(evaluation))
                return false;

            OnDecisionEvaluated?.Invoke(new DecisionEvaluatedEvent(runId, productId, evaluation));
            return true;
        }

        /// <summary>
        /// Bölüm 7 — DispositionFinished:
        /// Onaylanan üründe çıkış hareketi bittiğinde sonucu bir kez kaydeder ve ProductCompleted üretir.
        /// Reddedilen üründe ise gıda imha hareketi bitmiştir ama ayrıştırma (SortingFinished) beklenir;
        /// bu yüzden reddedilen üründe DispositionFinished tek başına ProductCompleted ÜRETMEZ (Bölüm 7 &amp; T11)!
        /// </summary>
        public bool NotifyDispositionFinished(
            string runId,
            string productId,
            out ProductOutcome committedApproveOutcome)
        {
            committedApproveOutcome = null;
            if (!IsMatchingActiveSession(runId, productId))
                return false;

            if (!activeProductSession.HasDecisionEvaluation || activeProductSession.IsOutcomeCommitted)
                return false;

            // Reddedilen üründe ayrıştırma aşaması beklenir (Bölüm 7).
            if (activeProductSession.DecisionEvaluation.PlayerDecision == ProductDecision.Reject)
            {
                return false;
            }

            return CommitOutcome(runId, productId, out committedApproveOutcome);
        }

        /// <summary>
        /// Bölüm 7 — SortingFinished:
        /// Reddedilen üründe oyuncu atık kutusunu seçip ambalaj hedefe ulaştığında çağrılır.
        /// Ayrıştırmayı değerlendirir ve birleşik sonucu bir kez kaydeder (Bölüm 7, 12, T11, T13).
        /// </summary>
        public bool SubmitSortingFinished(
            string runId,
            string productId,
            WasteBinType selectedBin,
            out ProductOutcome committedRejectOutcome)
        {
            committedRejectOutcome = null;
            if (!IsMatchingActiveSession(runId, productId))
                return false;

            if (!activeProductSession.HasDecisionEvaluation || activeProductSession.IsOutcomeCommitted)
                return false;

            // Yalnızca reddedilen ürünler ayrıştırmaya girer
            if (activeProductSession.DecisionEvaluation.PlayerDecision != ProductDecision.Reject)
                return false;

            if (selectedBin == WasteBinType.None)
                return false;

            SortingEvaluation sortingEval = ProductRuleEvaluator.EvaluateSorting(
                runId: activeProductSession.RunId,
                productId: activeProductSession.ProductId,
                product: activeProductSession.Definition,
                selectedBin: selectedBin
            );

            if (!activeProductSession.TrySetSortingEvaluation(sortingEval))
                return false;

            return CommitOutcome(runId, productId, out committedRejectOutcome);
        }

        /// <summary>
        /// Bölüm 7 — CommitOutcome:
        /// Nihai ürün sonucunu bir kez kaydeder ve ProductCompleted olayını tetikler (R05, T08, T11).
        /// </summary>
        public bool CommitOutcome(string runId, string productId, out ProductOutcome outcome)
        {
            outcome = null;
            if (!IsMatchingActiveSession(runId, productId))
                return false;

            if (!activeProductSession.HasDecisionEvaluation || activeProductSession.IsOutcomeCommitted)
                return false;

            DecisionEvaluation decisionEval = activeProductSession.DecisionEvaluation;

            // Reddedilen üründe ayrıştırma henüz tamamlanmadıysa sonuç kaydedilemez (Bölüm 7 & T11).
            bool requiresSorting = decisionEval.PlayerDecision == ProductDecision.Reject;
            if (requiresSorting && !activeProductSession.HasSortingEvaluation)
                return false;

            SortingEvaluation sortingEval = requiresSorting ? activeProductSession.SortingEvaluation : null;

            int decisionDelta = ProductRuleEvaluator.CalculateDecisionScoreDelta(decisionEval);
            int sortingDelta = ProductRuleEvaluator.CalculateSortingScoreDelta(requiresSorting, sortingEval);
            string summaryText = ProductRuleEvaluator.BuildOutcomeSummaryExplanation(
                product: activeProductSession.Definition,
                decisionEvaluation: decisionEval,
                sortingPerformed: requiresSorting,
                sortingEvaluation: sortingEval
            );

            var candidateOutcome = new ProductOutcome(
                runId: activeProductSession.RunId,
                dayId: activeProductSession.DayId,
                productId: activeProductSession.ProductId,
                definitionId: activeProductSession.Definition.DefinitionId,
                displayName: activeProductSession.Definition.DisplayName,
                decisionEvaluation: decisionEval,
                sortingPerformed: requiresSorting,
                sortingEvaluation: sortingEval,
                decisionScoreDelta: decisionDelta,
                sortingScoreDelta: sortingDelta,
                summaryExplanation: summaryText
            );

            if (!resultsStore.TryRecordOutcome(candidateOutcome))
                return false;

            if (!activeProductSession.TryCommitOutcome(candidateOutcome))
                return false;

            outcome = candidateOutcome;

            int completedInDay = resultsStore.GetCompletedCountForDay(currentDayId);
            bool shouldTriggerEmergency = CheckAndMarkEmergencyTrigger(completedInDay);

            OnProductCompleted?.Invoke(new ProductCompletedEvent(
                runId: outcome.RunId,
                dayId: outcome.DayId,
                productId: outcome.ProductId,
                outcome: outcome,
                completedProductsInDay: completedInDay,
                shouldTriggerEmergency: shouldTriggerEmergency
            ));

            return true;
        }

        /// <summary>
        /// Bölüm 7 &amp; 11 — ActivateEmergency:
        /// Oyuncu acil faksı onayladığında sonraki ürünler için yeni kuralı (R_SOURCE_BAN) etkinleştirir (R06, T12).
        /// Faksı yeniden açmak veya tekrar onay göndermek kuralı ikinci kez eklemez!
        /// </summary>
        public bool ActivateEmergency(string runId, RuleDefinition emergencyRule = null)
        {
            if (string.IsNullOrEmpty(runId) || !string.Equals(currentRunId, runId, StringComparison.Ordinal))
                return false;

            // T12: Zaten etkinleştirilmişse ikinci kez kural eklenmez veya snapshot sürümü artırılmaz.
            if (isEmergencyActiveInCurrentDay)
                return false;

            RuleDefinition ruleToActivate = emergencyRule != null
                ? emergencyRule
                : currentDayEmergency?.NewRule;

            if (ruleToActivate == null)
                return false;

            if (!ContainsRuleEntry(ruleToActivate.RuleId))
            {
                currentDayActiveRules.Add(ruleToActivate.ToSnapshotEntry());
            }

            isEmergencyTriggeredInCurrentDay = true;
            isEmergencyActiveInCurrentDay = true;
            currentSnapshotVersion++;

            // Yeni ürünler için yeni RuleSnapshot örneği oluşturulur; eski ürünlerin snapshot'ı değişmez (Bölüm 6.2 & 11).
            currentDayRuleSnapshot = new RuleSnapshot(currentSnapshotVersion, currentGameDate, currentDayActiveRules);

            string clockTime = currentDayEmergency != null && !string.IsNullOrWhiteSpace(currentDayEmergency.DisplayedClockTime)
                ? currentDayEmergency.DisplayedClockTime
                : "12.00";

            OnEmergencyActivated?.Invoke(new EmergencyActivatedEvent(
                runId: currentRunId,
                dayId: currentDayId,
                activatedRuleId: ruleToActivate.RuleId,
                displayedClockTime: clockTime,
                updatedRuleSnapshot: currentDayRuleSnapshot
            ));

            return true;
        }

        public DaySummaryReport GetDaySummary(string dayId = null)
        {
            string targetDay = string.IsNullOrWhiteSpace(dayId) ? currentDayId : dayId;
            return resultsStore.BuildDaySummary(targetDay);
        }

        public GameSummaryReport GetGameSummary()
        {
            return resultsStore.BuildGameSummary();
        }

        public string GetCurrentRulebookText()
        {
            return currentDayRuleSnapshot != null
                ? currentDayRuleSnapshot.BuildRulebookBodyText()
                : string.Empty;
        }

        private bool IsMatchingActiveSession(string runId, string productId)
        {
            if (activeProductSession == null)
                return false;

            if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(productId))
                return false;

            return string.Equals(activeProductSession.RunId, runId, StringComparison.Ordinal) &&
                   string.Equals(activeProductSession.ProductId, productId, StringComparison.Ordinal);
        }

        private bool ContainsRuleEntry(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
                return false;

            for (int i = 0; i < currentDayActiveRules.Count; i++)
            {
                if (string.Equals(currentDayActiveRules[i].RuleId, ruleId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool CheckAndMarkEmergencyTrigger(int completedProductsInDay)
        {
            if (isEmergencyTriggeredInCurrentDay || isEmergencyActiveInCurrentDay)
                return false;

            if (currentDayEmergency == null || !currentDayEmergency.HasEmergency)
                return false;

            if (completedProductsInDay >= currentDayEmergency.TriggerAfterCompletedProducts)
            {
                isEmergencyTriggeredInCurrentDay = true;
                return true;
            }

            return false;
        }
    }
}

