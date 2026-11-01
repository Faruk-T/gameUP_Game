using System;
using System.Collections.Generic;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// Saf C# oturum sonuç deposu (IMPLEMENTATION.md Bölüm 6.2, 7, 9.1, R05, R08).
    /// Ürün başına yalnızca bir kez terminal sonuç kaydeder (T08).
    /// Yeniden başlatmada tüm kayıtları temizler (T17).
    /// </summary>
    public sealed class RunResultsStore
    {
        private string currentRunId = string.Empty;
        private readonly List<ProductOutcome> allOutcomes = new List<ProductOutcome>();
        private readonly Dictionary<string, List<ProductOutcome>> outcomesByDay = new Dictionary<string, List<ProductOutcome>>(StringComparer.Ordinal);
        private readonly HashSet<string> committedProductKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> dayOrder = new List<string>();

        public string CurrentRunId => currentRunId;
        public IReadOnlyList<ProductOutcome> AllOutcomes => allOutcomes;

        public void ResetForNewRun(string newRunId)
        {
            currentRunId = newRunId ?? Guid.NewGuid().ToString("N");
            allOutcomes.Clear();
            outcomesByDay.Clear();
            committedProductKeys.Clear();
            dayOrder.Clear();
        }

        public bool IsProductCommitted(string runId, string productId)
        {
            if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(productId))
                return false;

            if (!string.Equals(currentRunId, runId, StringComparison.Ordinal))
                return false;

            return committedProductKeys.Contains(BuildCommitKey(runId, productId));
        }

        /// <summary>
        /// Ürün sonucunu yalnızca BİR KEZ kaydeder. Aynı ürün için tekrar çağrılırsa false döner ve çift puan üretmez (T08).
        /// </summary>
        public bool TryRecordOutcome(ProductOutcome outcome)
        {
            if (outcome == null)
                return false;

            if (!string.Equals(currentRunId, outcome.RunId, StringComparison.Ordinal))
                return false;

            string key = BuildCommitKey(outcome.RunId, outcome.ProductId);
            if (!committedProductKeys.Add(key))
            {
                // T08: Daha önce kaydedilmiş aynı ürüne gelen tekrar olayı ikinci puan üretmez.
                return false;
            }

            allOutcomes.Add(outcome);

            string dayId = string.IsNullOrWhiteSpace(outcome.DayId) ? "DAY_01" : outcome.DayId;
            if (!outcomesByDay.TryGetValue(dayId, out List<ProductOutcome> dayList))
            {
                dayList = new List<ProductOutcome>();
                outcomesByDay[dayId] = dayList;
                dayOrder.Add(dayId);
            }
            dayList.Add(outcome);
            return true;
        }

        public int GetCompletedCountForDay(string dayId)
        {
            if (string.IsNullOrWhiteSpace(dayId))
                return 0;

            return outcomesByDay.TryGetValue(dayId, out List<ProductOutcome> list) ? list.Count : 0;
        }

        public DaySummaryReport BuildDaySummary(string dayId)
        {
            string resolvedDayId = string.IsNullOrWhiteSpace(dayId) ? "DAY_01" : dayId;
            if (outcomesByDay.TryGetValue(resolvedDayId, out List<ProductOutcome> list))
            {
                return new DaySummaryReport(currentRunId, resolvedDayId, new List<ProductOutcome>(list).AsReadOnly());
            }
            return new DaySummaryReport(currentRunId, resolvedDayId, Array.Empty<ProductOutcome>());
        }

        public GameSummaryReport BuildGameSummary()
        {
            var dayReports = new List<DaySummaryReport>(dayOrder.Count);
            for (int i = 0; i < dayOrder.Count; i++)
            {
                dayReports.Add(BuildDaySummary(dayOrder[i]));
            }

            return new GameSummaryReport(
                runId: currentRunId,
                dayReports: dayReports.AsReadOnly(),
                allOutcomes: new List<ProductOutcome>(allOutcomes).AsReadOnly()
            );
        }

        private static string BuildCommitKey(string runId, string productId)
        {
            return $"{runId}::{productId}";
        }
    }
}

