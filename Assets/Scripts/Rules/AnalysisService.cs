using System;
using System.Collections;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// Saf C# analiz rapor üreticisi (IMPLEMENTATION.md Bölüm 6.2, 7, 9, R03, T03, T09).
    /// Sonuç işlem sırasında rastgele üretilmez; ürünün gizli verisinden deterministik olarak döner.
    /// Aynı ürün için tekrar analiz istenirse birebir aynı rapor (aynı ReportId ve bulgu) döner (T03).
    /// </summary>
    public static class ProductAnalysisProcessor
    {
        public static bool TryGenerateOrGetReport(
            ProductSession activeSession,
            string requestedRunId,
            string requestedProductId,
            out AnalysisReport report)
        {
            report = null;

            // T09: Yanlış oturum (RunId) veya eski/farklı ürün (ProductId) isteği reddedilir.
            if (activeSession == null)
                return false;

            if (!string.Equals(activeSession.RunId, requestedRunId, StringComparison.Ordinal) ||
                !string.Equals(activeSession.ProductId, requestedProductId, StringComparison.Ordinal))
            {
                return false;
            }

            // T03: Tek üründe yeniden analiz isteği aynı bulguyu ve aynı raporu verir.
            if (activeSession.IsAnalyzed && activeSession.CachedAnalysisReport != null)
            {
                report = activeSession.CachedAnalysisReport;
                return true;
            }

            ProductDefinition def = activeSession.Definition;
            string safeDay = string.IsNullOrWhiteSpace(activeSession.DayId) ? "D0" : activeSession.DayId;
            string reportId = $"COP31-LAB-{safeDay}-{activeSession.SequenceIndex + 1:00}-{def.DefinitionId}";

            string findingDetail = !string.IsNullOrWhiteSpace(def.AnalysisFindingDetail)
                ? def.AnalysisFindingDetail
                : (def.IsPolluted
                    ? "POZİTİF — Deniz atığı / ağır metal ve mikroplastik toksisitesi saptandı. İmha edilmelidir."
                    : "NEGATİF — Toksik kalıntı veya mikroplastik saptanmadı. Kirlilik açısından temiz.");

            bool hasOtherViolation = false;
            string violationSummary = string.Empty;
            if (activeSession.Snapshot != null)
            {
                DecisionEvaluation checkEval = ProductRuleEvaluator.EvaluateDecision(
                    product: def,
                    snapshot: activeSession.Snapshot,
                    playerDecision: ProductDecision.Approve,
                    runId: activeSession.RunId,
                    productId: activeSession.ProductId);

                if (checkEval != null && checkEval.ExpectedDecision == ProductDecision.Reject)
                {
                    hasOtherViolation = true;
                    violationSummary = string.Join(" | ", checkEval.FailureReasons);
                }
            }

            report = new AnalysisReport(
                runId: activeSession.RunId,
                productId: activeSession.ProductId,
                definitionId: def.DefinitionId,
                productDisplayName: def.DisplayName,
                reportId: reportId,
                isPolluted: def.IsPolluted,
                findingText: findingDetail,
                hasAnyViolation: hasOtherViolation,
                violationSummary: violationSummary
            );

            activeSession.CacheAnalysis(report);
            return true;
        }
    }

    /// <summary>
    /// Sahnedeki Main/Systems/AnalysisService nesnesine takılabilen MonoBehaviour bileşeni (Bölüm 5).
    /// İster anında (senkron), ister kısa işlem süreli (Coroutine) analiz akışını yürütür;
    /// yeniden başlatmada (RunId değiştiğinde) gecikmiş eski callback'leri otomatik iptal eder (L05, T09, T17).
    /// </summary>
    public class AnalysisService : MonoBehaviour
    {
        [Tooltip("Analiz cihazı işlem süresi (saniye). 0 ise anında AnalysisReady üretir.")]
        [SerializeField] private float analysisDurationSeconds = 1.25f;

        [SerializeField] private InspectionService inspectionService;

        private Coroutine activeAnalysisCoroutine;

        public float AnalysisDurationSeconds
        {
            get => analysisDurationSeconds;
            set => analysisDurationSeconds = Mathf.Max(0f, value);
        }

        public bool IsAnalyzing { get; private set; }

        public event Action<AnalysisReadyEvent> OnAnalysisReady;

        private void Awake()
        {
            if (inspectionService == null)
            {
                inspectionService = FindFirstObjectByType<InspectionService>();
            }
        }

        /// <summary>
        /// Özgür veya Teknik Lider tarafından AnalysisRequested geldiğinde çağrılır (Bölüm 7).
        /// </summary>
        public void HandleAnalysisRequested(AnalysisRequestedEvent request)
        {
            RequestAnalysisForProduct(request.RunId, request.ProductId, useDelay: analysisDurationSeconds > 0f);
        }

        public bool RequestAnalysisForProduct(string runId, string productId, bool useDelay = false)
        {
            if (inspectionService == null)
            {
                inspectionService = FindFirstObjectByType<InspectionService>();
            }

            if (inspectionService == null || inspectionService.Engine == null)
                return false;

            // Önce oturum ve ürün kimliği doğrulaması
            ProductSession session = inspectionService.Engine.ActiveProductSession;
            if (session == null ||
                !string.Equals(session.RunId, runId, StringComparison.Ordinal) ||
                !string.Equals(session.ProductId, productId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!useDelay || analysisDurationSeconds <= 0f)
            {
                if (inspectionService.Engine.RequestAnalysis(runId, productId, out AnalysisReport immediateReport))
                {
                    OnAnalysisReady?.Invoke(new AnalysisReadyEvent(runId, productId, immediateReport));
                    return true;
                }
                return false;
            }

            if (activeAnalysisCoroutine != null)
            {
                StopCoroutine(activeAnalysisCoroutine);
            }

            activeAnalysisCoroutine = StartCoroutine(DelayedAnalysisRoutine(runId, productId, analysisDurationSeconds));
            return true;
        }

        public void CancelPendingAnalysis()
        {
            if (activeAnalysisCoroutine != null)
            {
                StopCoroutine(activeAnalysisCoroutine);
                activeAnalysisCoroutine = null;
            }
            IsAnalyzing = false;
        }

        private IEnumerator DelayedAnalysisRoutine(string expectedRunId, string expectedProductId, float delay)
        {
            IsAnalyzing = true;
            yield return new WaitForSeconds(delay);
            IsAnalyzing = false;
            activeAnalysisCoroutine = null;

            if (inspectionService == null || inspectionService.Engine == null)
                yield break;

            // L05 & T09: Bekleme sırasında oyun yeniden başlatıldıysa (RunId değiştiyse) veya ürün değiştiyse rapor üretilmez!
            if (inspectionService.Engine.RequestAnalysis(expectedRunId, expectedProductId, out AnalysisReport report))
            {
                OnAnalysisReady?.Invoke(new AnalysisReadyEvent(expectedRunId, expectedProductId, report));
            }
        }
    }
}

