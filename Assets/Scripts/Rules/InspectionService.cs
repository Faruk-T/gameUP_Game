using System;
using System.Collections.Generic;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// Ana sahnede Main/Systems/InspectionService nesnesine eklenecek MonoBehaviour cephesi (IMPLEMENTATION.md Bölüm 5, 7, 9).
    /// Inspector'da DayDefinition asset'leri atanmamış olsa bile DefaultGameContentFactory ile
    /// otomatik olarak 2 gün ve 12 ürünü bellekte hazırlar (sıfır NullReferenceException garantisi).
    /// </summary>
    public class InspectionService : MonoBehaviour, IRulesRuntimeService
    {
        [Header("Tasarım Verileri (Boşsa Otomatik Doldurulur)")]
        [SerializeField] private List<DayDefinition> configuredDays = new List<DayDefinition>();

        private readonly InspectionRulesEngine engine = new InspectionRulesEngine();
        private DefaultGameContentFactory.GeneratedCatalog runtimeFallbackCatalog;

        public InspectionRulesEngine Engine => engine;

        public IReadOnlyList<DayDefinition> ConfiguredDays
        {
            get
            {
                EnsureCatalogReady();
                return configuredDays;
            }
        }

        public string CurrentRunId => engine.CurrentRunId;
        public string CurrentDayId => engine.CurrentDayId;
        public RuleSnapshot CurrentDayRuleSnapshot => engine.CurrentDayRuleSnapshot;
        public ProductSession ActiveProductSession => engine.ActiveProductSession;
        public bool IsEmergencyActiveInCurrentDay => engine.IsEmergencyActiveInCurrentDay;

        // Bölüm 7 Olayları
        public event Action<BeginProductEvent> OnProductBegun
        {
            add => engine.OnProductBegun += value;
            remove => engine.OnProductBegun -= value;
        }

        public event Action<AnalysisReadyEvent> OnAnalysisReady
        {
            add => engine.OnAnalysisReady += value;
            remove => engine.OnAnalysisReady -= value;
        }

        public event Action<DecisionEvaluatedEvent> OnDecisionEvaluated
        {
            add => engine.OnDecisionEvaluated += value;
            remove => engine.OnDecisionEvaluated -= value;
        }

        public event Action<ProductCompletedEvent> OnProductCompleted
        {
            add => engine.OnProductCompleted += value;
            remove => engine.OnProductCompleted -= value;
        }

        public event Action<EmergencyActivatedEvent> OnEmergencyActivated
        {
            add => engine.OnEmergencyActivated += value;
            remove => engine.OnEmergencyActivated -= value;
        }

        private void Awake()
        {
            EnsureCatalogReady();
        }

        public void EnsureCatalogReady()
        {
            if (configuredDays != null && configuredDays.Count > 0 && configuredDays[0] != null)
                return;

            if (runtimeFallbackCatalog == null)
            {
                runtimeFallbackCatalog = DefaultGameContentFactory.CreateCatalogInMemory();
            }

            if (configuredDays == null)
            {
                configuredDays = new List<DayDefinition>();
            }
            else
            {
                configuredDays.Clear();
            }

            configuredDays.Add(runtimeFallbackCatalog.Day1);
            configuredDays.Add(runtimeFallbackCatalog.Day2);
        }

        public DayDefinition GetDayDefinition(int dayIndex)
        {
            EnsureCatalogReady();
            if (dayIndex < 0 || dayIndex >= configuredDays.Count)
                return null;
            return configuredDays[dayIndex];
        }

        public string StartNewRun(string customRunId = null)
        {
            EnsureCatalogReady();
            return engine.StartNewRun(customRunId);
        }

        public RuleSnapshot BeginDay(int dayIndex)
        {
            DayDefinition day = GetDayDefinition(dayIndex);
            if (day == null)
                throw new ArgumentOutOfRangeException(nameof(dayIndex), $"Geçersiz gün indeksi: {dayIndex}");

            return engine.BeginDay(day);
        }

        public RuleSnapshot BeginDay(DayDefinition dayDefinition)
        {
            return engine.BeginDay(dayDefinition);
        }

        public RuleSnapshot BeginDay(
            string dayId,
            GameDate gameDate,
            RuleDefinition[] initialRules,
            EmergencyDefinition emergencyDefinition = null)
        {
            return engine.BeginDay(dayId, gameDate, initialRules, emergencyDefinition);
        }

        public ProductSession BeginProduct(
            ProductDefinition productDefinition,
            string customProductId = null,
            int sequenceIndex = 0)
        {
            return engine.BeginProduct(productDefinition, customProductId, sequenceIndex);
        }

        public bool RequestAnalysis(string runId, string productId, out AnalysisReport report)
        {
            return engine.RequestAnalysis(runId, productId, out report);
        }

        public bool SubmitDecision(
            string runId,
            string productId,
            ProductDecision playerDecision,
            out DecisionEvaluation evaluation)
        {
            return engine.SubmitDecision(runId, productId, playerDecision, out evaluation);
        }

        public bool NotifyDispositionFinished(
            string runId,
            string productId,
            out ProductOutcome committedApproveOutcome)
        {
            return engine.NotifyDispositionFinished(runId, productId, out committedApproveOutcome);
        }

        public bool SubmitSortingFinished(
            string runId,
            string productId,
            WasteBinType selectedBin,
            out ProductOutcome committedRejectOutcome)
        {
            return engine.SubmitSortingFinished(runId, productId, selectedBin, out committedRejectOutcome);
        }

        public bool CommitOutcome(string runId, string productId, out ProductOutcome outcome)
        {
            return engine.CommitOutcome(runId, productId, out outcome);
        }

        public bool ActivateEmergency(string runId, RuleDefinition emergencyRule = null)
        {
            return engine.ActivateEmergency(runId, emergencyRule);
        }

        public DaySummaryReport GetDaySummary(string dayId = null)
        {
            return engine.GetDaySummary(dayId);
        }

        public GameSummaryReport GetGameSummary()
        {
            return engine.GetGameSummary();
        }

        public string GetCurrentRulebookText()
        {
            return engine.GetCurrentRulebookText();
        }

        // =====================================================================
        // BÖLÜM 7 OLAY DİNLEYİCİ YARDIMCILARI (Event Bus / Callback uyumu için)
        // =====================================================================

        public void HandleAnalysisRequested(AnalysisRequestedEvent evt)
        {
            engine.RequestAnalysis(evt.RunId, evt.ProductId, out _);
        }

        public void HandleDecisionSubmitted(DecisionSubmittedEvent evt)
        {
            engine.SubmitDecision(evt.RunId, evt.ProductId, evt.Decision, out _);
        }

        public void HandleDispositionFinished(DispositionFinishedEvent evt)
        {
            engine.NotifyDispositionFinished(evt.RunId, evt.ProductId, out _);
        }

        public void HandleSortingFinished(SortingFinishedEvent evt)
        {
            engine.SubmitSortingFinished(evt.RunId, evt.ProductId, evt.SelectedBin, out _);
        }

        public void HandleEmergencyAcknowledged(EmergencyAcknowledgedEvent evt)
        {
            engine.ActivateEmergency(evt.RunId);
        }
    }
}

