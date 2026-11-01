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
}

