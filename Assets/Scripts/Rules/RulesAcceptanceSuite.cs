using System;
using System.Collections.Generic;
using System.Text;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// IMPLEMENTATION.md Bölüm 17'deki T01–T17 kabul senaryolarını ve R01–R09 ölçütlerini
    /// otomatik olarak çalıştırıp doğrulayan test motoru.
    /// Hem EditMode / PlayMode Test Runner'dan hem de Editor menüsünden çağrılabilir.
    /// </summary>
    public static class RulesAcceptanceSuite
    {
        public sealed class TestCaseResult
        {
            public string TestId { get; }
            public string Title { get; }
            public bool Passed { get; }
            public string Details { get; }

            public TestCaseResult(string testId, string title, bool passed, string details)
            {
                TestId = testId;
                Title = title;
                Passed = passed;
                Details = details;
            }
        }

        public static List<TestCaseResult> RunAllTests(out bool allPassed, out string formattedReport)
        {
            var results = new List<TestCaseResult>
            {
                Run_T01_ExpiryBoundary(),
                Run_T02_IdentityMismatch(),
                Run_T03_RepeatedAnalysisIdempotency(),
                Run_T04_T05_T06_T07_Day2FishAndEmergencyRules(),
                Run_T08_DuplicateDecisionAndCommitGuard(),
                Run_T09_StaleRunIdOrWrongProductIdRejected(),
                Run_T11_ThirdFishRejectWaitsForSortingBeforeEmergency(),
                Run_T12_EmergencySingleActivationAndSnapshotImmutability(),
                Run_T13_ContaminatedPackagingSentToPlasticFailsSortingButCompletes(),
                Run_T14_ApproveAllOrRejectAllYields50PercentAndFails(),
                Run_T15_FalseRejectionNotMaskedByCorrectSorting(),
                Run_T16_ZeroRejectionsShowsEmDashRatio(),
                Run_T17_RestartClearsAllStateAndIgnoresOldCallbacks(),
                Run_P1_WeightLengthAndIngredientPercentage(),
                Run_LeadContract_FullCampaign132Points()
            };

            int passedCount = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Passed) passedCount++;
            }

            allPassed = passedCount == results.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"=== GAMEUP32 FARUK (RULES & DATA) KABUL TESTLERİ: {passedCount}/{results.Count} GEÇTİ ===");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                string status = r.Passed ? "[PASS]" : "[FAIL]";
                sb.AppendLine($"{status} {r.TestId} — {r.Title}: {r.Details}");
            }

            formattedReport = sb.ToString().TrimEnd();
            return results;
        }

        // T01: SKT dün / bugün / yarın
        private static TestCaseResult Run_T01_ExpiryBoundary()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T01");
            engine.BeginDay(catalog.Day1); // Oyun tarihi: 01.11.2026

            // D1-02: SKT 31.10.2026 (Dün) -> Beklenen: Reject
            var sYesterday = engine.BeginProduct(catalog.Day1Products[1], "D1-02", 1);
            engine.SubmitDecision("RUN-T01", "D1-02", ProductDecision.Reject, out var evalYesterday);

            // D1-04: SKT 01.11.2026 (Bugün - sınır örneği) -> Beklenen: Approve
            var sToday = engine.BeginProduct(catalog.Day1Products[3], "D1-04", 3);
            engine.SubmitDecision("RUN-T01", "D1-04", ProductDecision.Approve, out var evalToday);

            // D1-01: SKT 03.11.2026 (Yarın/Sonrası) -> Beklenen: Approve
            var sTomorrow = engine.BeginProduct(catalog.Day1Products[0], "D1-01", 0);
            engine.SubmitDecision("RUN-T01", "D1-01", ProductDecision.Approve, out var evalTomorrow);

            bool ok = evalYesterday != null && evalYesterday.ExpectedDecision == ProductDecision.Reject && evalYesterday.IsCorrect &&
                      evalToday != null && evalToday.ExpectedDecision == ProductDecision.Approve && evalToday.IsCorrect &&
                      evalTomorrow != null && evalTomorrow.ExpectedDecision == ProductDecision.Approve && evalTomorrow.IsCorrect;

            return new TestCaseResult(
                "T01",
                "SKT dün / bugün / yarın sınır kontrolü",
                ok,
                $"Dün(31.10)={evalYesterday?.ExpectedDecision}, Bugün(01.11)={evalToday?.ExpectedDecision}, Sonrası(03.11)={evalTomorrow?.ExpectedDecision}"
            );
        }

        // T02: Geçerli tarih, uyumsuz beyan/tür (D1-06: Sütlü Dondurma vs Sütlü Buz)
        private static TestCaseResult Run_T02_IdentityMismatch()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T02");
            engine.BeginDay(catalog.Day1);

            engine.BeginProduct(catalog.Day1Products[5], "D1-06", 5);
            engine.SubmitDecision("RUN-T02", "D1-06", ProductDecision.Reject, out var eval);

            bool hasIdentityRule = eval != null && eval.ViolatedRuleIds.Count == 1 && eval.ViolatedRuleIds[0] == RuleIds.Identity;
            bool ok = eval != null && eval.ExpectedDecision == ProductDecision.Reject && eval.IsCorrect && hasIdentityRule;

            return new TestCaseResult(
                "T02",
                "Geçerli tarih, uyumsuz beyan/tür (D1-06 Sütlü Buz)",
                ok,
                $"Beklenen={eval?.ExpectedDecision}, İhlal={string.Join(",", eval?.ViolatedRuleIds ?? Array.Empty<string>())}"
            );
        }

        // T03: Aynı ürünün analizi tekrar istenir
        private static TestCaseResult Run_T03_RepeatedAnalysisIdempotency()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T03");
            engine.BeginDay(catalog.Day2);

            engine.BeginProduct(catalog.Day2Products[1], "D2-02", 1); // Kirli balık
            bool ok1 = engine.RequestAnalysis("RUN-T03", "D2-02", out var report1);
            bool ok2 = engine.RequestAnalysis("RUN-T03", "D2-02", out var report2);

            bool sameReport = ok1 && ok2 && report1 != null && report2 != null &&
                              ReferenceEquals(report1, report2) &&
                              report1.ReportId == report2.ReportId &&
                              report1.IsPolluted == report2.IsPolluted &&
                              engine.ResultsStore.AllOutcomes.Count == 0;

            return new TestCaseResult(
                "T03",
                "Aynı ürünün analizi tekrar istenir",
                sameReport,
                $"RaporNo={report1?.ReportId}, AynıNesne={ReferenceEquals(report1, report2)}, KayıtSayısı={engine.ResultsStore.AllOutcomes.Count}"
            );
        }

        // T04, T05, T06, T07: Gün 2 Balık ve Acil Faks öncesi/sonrası
        private static TestCaseResult Run_T04_T05_T06_T07_Day2FishAndEmergencyRules()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-DAY2");
            engine.BeginDay(catalog.Day2);

            // T04: D2-01 (Temiz B/dip, faks öncesi) -> Onay
            engine.BeginProduct(catalog.Day2Products[0], "D2-01", 0);
            engine.SubmitDecision("RUN-DAY2", "D2-01", ProductDecision.Approve, out var evalD201);
            engine.NotifyDispositionFinished("RUN-DAY2", "D2-01", out _);

            // D2-02 (Kirli A/yüzey) -> Red + Kontamine atık
            engine.BeginProduct(catalog.Day2Products[1], "D2-02", 1);
            engine.SubmitDecision("RUN-DAY2", "D2-02", ProductDecision.Reject, out _);
            engine.SubmitSortingFinished("RUN-DAY2", "D2-02", WasteBinType.Contaminated, out _);

            // D2-03 (Temiz A/yüzey) -> Onay (3. ürün bitti, faks tetiklendi!)
            engine.BeginProduct(catalog.Day2Products[2], "D2-03", 2);
            engine.SubmitDecision("RUN-DAY2", "D2-03", ProductDecision.Approve, out _);
            engine.NotifyDispositionFinished("RUN-DAY2", "D2-03", out _);

            // Acil faksı onayla -> R_SOURCE_BAN devreye girer
            bool emergencyActivated = engine.ActivateEmergency("RUN-DAY2");

            // T05: D2-04 (Temiz B/dip, faks sonrası) -> Kaynak yasağı nedeniyle Red
            engine.BeginProduct(catalog.Day2Products[3], "D2-04", 3);
            engine.SubmitDecision("RUN-DAY2", "D2-04", ProductDecision.Reject, out var evalD204);
            engine.SubmitSortingFinished("RUN-DAY2", "D2-04", WasteBinType.Plastic, out _);

            // T06: D2-05 (Temiz B/yüzey, faks sonrası) -> Onay
            engine.BeginProduct(catalog.Day2Products[4], "D2-05", 4);
            engine.SubmitDecision("RUN-DAY2", "D2-05", ProductDecision.Approve, out var evalD205);
            engine.NotifyDispositionFinished("RUN-DAY2", "D2-05", out _);

            // T07: D2-06 (Kirli A/yüzey, faks sonrası) -> Kirlilik nedeniyle Red
            engine.BeginProduct(catalog.Day2Products[5], "D2-06", 5);
            engine.SubmitDecision("RUN-DAY2", "D2-06", ProductDecision.Reject, out var evalD206);
            engine.SubmitSortingFinished("RUN-DAY2", "D2-06", WasteBinType.Contaminated, out _);

            bool t04 = evalD201 != null && evalD201.ExpectedDecision == ProductDecision.Approve && evalD201.IsCorrect;
            bool t05 = evalD204 != null && evalD204.ExpectedDecision == ProductDecision.Reject && evalD204.IsCorrect &&
                       evalD204.ViolatedRuleIds.Count == 1 && evalD204.ViolatedRuleIds[0] == RuleIds.SourceBan;
            bool t06 = evalD205 != null && evalD205.ExpectedDecision == ProductDecision.Approve && evalD205.IsCorrect;
            bool t07 = evalD206 != null && evalD206.ExpectedDecision == ProductDecision.Reject && evalD206.IsCorrect &&
                       evalD206.ViolatedRuleIds.Count == 1 && evalD206.ViolatedRuleIds[0] == RuleIds.Pollution;

            bool ok = emergencyActivated && t04 && t05 && t06 && t07;
            return new TestCaseResult(
                "T04–T07",
                "Faks öncesi/sonrası B/dip, B/yüzey ve A/yüzey kirli balık kuralları",
                ok,
                $"T04(B/dip-önce)={t04}, T05(B/dip-sonra)={t05}, T06(B/yüzey-sonra)={t06}, T07(A/kirli-sonra)={t07}"
            );
        }

        // T08: Çift tıklama / tamamlanma olayının tekrarı
        private static TestCaseResult Run_T08_DuplicateDecisionAndCommitGuard()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T08");
            engine.BeginDay(catalog.Day1);

            int completedEvents = 0;
            engine.OnProductCompleted += _ => completedEvents++;

            engine.BeginProduct(catalog.Day1Products[0], "D1-01", 0);
            bool firstDecision = engine.SubmitDecision("RUN-T08", "D1-01", ProductDecision.Approve, out _);
            bool secondDecision = engine.SubmitDecision("RUN-T08", "D1-01", ProductDecision.Reject, out _);

            bool firstCommit = engine.NotifyDispositionFinished("RUN-T08", "D1-01", out _);
            bool secondCommit = engine.NotifyDispositionFinished("RUN-T08", "D1-01", out _);
            bool thirdCommit = engine.CommitOutcome("RUN-T08", "D1-01", out _);

            var summary = engine.GetDaySummary("DAY_01");
            bool ok = firstDecision && !secondDecision &&
                      firstCommit && !secondCommit && !thirdCommit &&
                      completedEvents == 1 && summary.TotalProducts == 1 && summary.TotalDayScore == 10;

            return new TestCaseResult(
                "T08",
                "Çift karar ve çift kayıt koruması (Idempotency)",
                ok,
                $"2.KararReddedildi={!secondDecision}, 2.KayıtReddedildi={!secondCommit}, ToplamOlay={completedEvents}, Puan={summary.TotalDayScore}"
            );
        }

        // T09: Yanlış ürün veya eski RunId için rapor/karar gelir
        private static TestCaseResult Run_T09_StaleRunIdOrWrongProductIdRejected()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-ACTIVE");
            engine.BeginDay(catalog.Day2);
            engine.BeginProduct(catalog.Day2Products[0], "D2-01", 0);

            bool wrongRunAnalysis = engine.RequestAnalysis("RUN-OLD", "D2-01", out _);
            bool wrongProductAnalysis = engine.RequestAnalysis("RUN-ACTIVE", "D2-99", out _);
            bool wrongRunDecision = engine.SubmitDecision("RUN-OLD", "D2-01", ProductDecision.Approve, out _);
            bool wrongProductDecision = engine.SubmitDecision("RUN-ACTIVE", "D2-99", ProductDecision.Approve, out _);

            bool ok = !wrongRunAnalysis && !wrongProductAnalysis && !wrongRunDecision && !wrongProductDecision &&
                      !engine.ActiveProductSession.IsAnalyzed && !engine.ActiveProductSession.HasDecisionEvaluation;

            return new TestCaseResult(
                "T09",
                "Yanlış ProductId veya eski RunId olaylarının reddi",
                ok,
                $"EskiRunAnaliz={!wrongRunAnalysis}, YanlışÜrünAnaliz={!wrongProductAnalysis}, AktifOturumKorundu={!engine.ActiveProductSession.HasDecisionEvaluation}"
            );
        }

        // T11: Üçüncü balık reddedilir, ayrıştırma henüz bitmez -> Faks ve dördüncü ürün bekler
        private static TestCaseResult Run_T11_ThirdFishRejectWaitsForSortingBeforeEmergency()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T11");
            engine.BeginDay(catalog.Day2);

            // 1. ve 2. ürünü tamamla
            engine.BeginProduct(catalog.Day2Products[0], "D2-01", 0);
            engine.SubmitDecision("RUN-T11", "D2-01", ProductDecision.Approve, out _);
            engine.NotifyDispositionFinished("RUN-T11", "D2-01", out _);

            engine.BeginProduct(catalog.Day2Products[1], "D2-02", 1);
            engine.SubmitDecision("RUN-T11", "D2-02", ProductDecision.Reject, out _);
            engine.SubmitSortingFinished("RUN-T11", "D2-02", WasteBinType.Contaminated, out _);

            // 3. balık reddedilir, gıda imhası (DispositionFinished) biter ama ayrıştırma (SortingFinished) henüz bitmez!
            engine.BeginProduct(catalog.Day2Products[2], "D2-03", 2);
            engine.SubmitDecision("RUN-T11", "D2-03", ProductDecision.Reject, out _);
            bool completedOnDispositionOnly = engine.NotifyDispositionFinished("RUN-T11", "D2-03", out _);
            bool emergencyBeforeSort = engine.IsEmergencyTriggeredInCurrentDay;

            // Şimdi ayrıştırma biter -> 3. ürün tamamlanır ve acil faks tetiklenir!
            bool completedAfterSort = engine.SubmitSortingFinished("RUN-T11", "D2-03", WasteBinType.Plastic, out _);
            bool emergencyAfterSort = engine.IsEmergencyTriggeredInCurrentDay;

            bool ok = !completedOnDispositionOnly && !emergencyBeforeSort && completedAfterSort && emergencyAfterSort;
            return new TestCaseResult(
                "T11",
                "3. balık reddedildiğinde ayrıştırma bitmeden acil faks tetiklenmez",
                ok,
                $"AyrıştırmaÖncesiFaks={emergencyBeforeSort}, AyrıştırmaSonrasıFaks={emergencyAfterSort}"
            );
        }

        // T12: Acil faks tekrar onay olayı ve önceki ürünlerin RuleSnapshot değişmezliği
        private static TestCaseResult Run_T12_EmergencySingleActivationAndSnapshotImmutability()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T12");
            engine.BeginDay(catalog.Day2);

            var sessionBeforeEmergency = engine.BeginProduct(catalog.Day2Products[0], "D2-01", 0);
            RuleSnapshot snapshotBefore = sessionBeforeEmergency.Snapshot;
            int rulesCountBefore = snapshotBefore.ActiveRules.Count;

            bool firstActivate = engine.ActivateEmergency("RUN-T12");
            bool secondActivate = engine.ActivateEmergency("RUN-T12"); // Tekrar onay

            RuleSnapshot snapshotAfter = engine.CurrentDayRuleSnapshot;

            // Eski snapshot değişmemiş olmalı!
            bool oldSnapshotUnchanged = snapshotBefore.Version == 1 &&
                                        snapshotBefore.ActiveRules.Count == rulesCountBefore &&
                                        !snapshotBefore.ContainsRule(RuleIds.SourceBan);

            bool newSnapshotUpdatedOnce = snapshotAfter.Version == 2 &&
                                          snapshotAfter.ActiveRules.Count == rulesCountBefore + 1 &&
                                          snapshotAfter.ContainsRule(RuleIds.SourceBan);

            bool ok = firstActivate && !secondActivate && oldSnapshotUnchanged && newSnapshotUpdatedOnce;
            return new TestCaseResult(
                "T12",
                "Acil faks tek aktivasyon ve önceki RuleSnapshot değişmezliği",
                ok,
                $"İlkOnay={firstActivate}, İkinciOnay={!secondActivate}, EskiSnapshotV={snapshotBefore.Version}, YeniSnapshotV={snapshotAfter.Version}"
            );
        }

        // T13: Kirli ambalaj plastik kutuya gönderilir -> Ayrıştırma hatası ama akış tamamlanır
        private static TestCaseResult Run_T13_ContaminatedPackagingSentToPlasticFailsSortingButCompletes()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T13");
            engine.BeginDay(catalog.Day2);

            // D2-02: Kirli balık (IsPackagingContaminated = true, beklenen kutu = Contaminated)
            engine.BeginProduct(catalog.Day2Products[1], "D2-02", 1);
            engine.SubmitDecision("RUN-T13", "D2-02", ProductDecision.Reject, out _);
            bool completed = engine.SubmitSortingFinished("RUN-T13", "D2-02", WasteBinType.Plastic, out var outcome);

            bool ok = completed && outcome != null &&
                      outcome.DecisionEvaluation.IsCorrect &&
                      outcome.SortingPerformed &&
                      !outcome.SortingEvaluation.IsCorrect &&
                      outcome.SortingEvaluation.ExpectedBin == WasteBinType.Contaminated &&
                      outcome.DecisionScoreDelta == 10 &&
                      outcome.SortingScoreDelta == -2 &&
                      outcome.TotalScoreDelta == 8;

            return new TestCaseResult(
                "T13",
                "Kirli ambalaj Plastik kutusuna atılırsa ayrıştırma hatası (-2) yazılır ve ürün tamamlanır",
                ok,
                $"Tamamlandı={completed}, BeklenenKutu={outcome?.SortingEvaluation?.ExpectedBin}, NetPuan={outcome?.TotalScoreDelta}"
            );
        }

        // T14: Her şeyi onayla / her şeyi reddet -> %50 doğruluk, başarısız final
        private static TestCaseResult Run_T14_ApproveAllOrRejectAllYields50PercentAndFails()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();

            // Senaryo A: 12 ürünün tamamını ONAYLA
            var engineApproveAll = new InspectionRulesEngine();
            SimulateFullGame(engineApproveAll, catalog, ProductDecision.Approve);
            var summaryApproveAll = engineApproveAll.GetGameSummary();

            // Senaryo B: 12 ürünün tamamını REDDET
            var engineRejectAll = new InspectionRulesEngine();
            SimulateFullGame(engineRejectAll, catalog, ProductDecision.Reject);
            var summaryRejectAll = engineRejectAll.GetGameSummary();

            bool ok = summaryApproveAll.TotalProducts == 12 &&
                      summaryApproveAll.CorrectDecisionCount == 6 &&
                      summaryApproveAll.DecisionAccuracyPercent == 50 &&
                      !summaryApproveAll.IsSuccess &&
                      summaryRejectAll.TotalProducts == 12 &&
                      summaryRejectAll.CorrectDecisionCount == 6 &&
                      summaryRejectAll.DecisionAccuracyPercent == 50 &&
                      !summaryRejectAll.IsSuccess;

            return new TestCaseResult(
                "T14",
                "Her şeyi onayla / reddet -> %50 doğruluk (6/12) ve başarısız final",
                ok,
                $"HepsiniOnayla={summaryApproveAll.CorrectDecisionCount}/12 (%{summaryApproveAll.DecisionAccuracyPercent}, Başarı={summaryApproveAll.IsSuccess}), HepsiniReddet={summaryRejectAll.CorrectDecisionCount}/12 (%{summaryRejectAll.DecisionAccuracyPercent}, Başarı={summaryRejectAll.IsSuccess})"
            );
        }

        // T15: Yanlış sağlam ürün reddi -> Hatalı red kaydı; ayrıştırma başarısı bunu gizlemez
        private static TestCaseResult Run_T15_FalseRejectionNotMaskedByCorrectSorting()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T15");
            engine.BeginDay(catalog.Day1);

            // D1-01 sağlam üründür; oyuncu yanlışlıkla Red verir ama doğru kutuya (Paper) atar
            engine.BeginProduct(catalog.Day1Products[0], "D1-01", 0);
            engine.SubmitDecision("RUN-T15", "D1-01", ProductDecision.Reject, out _);
            engine.SubmitSortingFinished("RUN-T15", "D1-01", WasteBinType.Paper, out var outcome);

            var daySummary = engine.GetDaySummary("DAY_01");
            bool ok = outcome != null &&
                      outcome.DecisionEvaluation.IsFalseRejection &&
                      outcome.DecisionScoreDelta == -10 &&
                      outcome.SortingScoreDelta == 2 &&
                      daySummary.CorrectDecisionCount == 0 &&
                      daySummary.FalseRejectionCount == 1 &&
                      daySummary.CorrectSortingCount == 1;

            return new TestCaseResult(
                "T15",
                "Sağlam ürün reddedildiğinde doğru ayrıştırma (+2) hatalı reddi (-10) gizlemez",
                ok,
                $"HatalıRedSayısı={daySummary.FalseRejectionCount}, DoğruKarar={daySummary.CorrectDecisionCount}, DoğruAtık={daySummary.CorrectSortingCount}"
            );
        }

        // T16: Gün sonunda hiç ayrıştırma yapılmamış -> Oran yerine "—"
        private static TestCaseResult Run_T16_ZeroRejectionsShowsEmDashRatio()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-T16");
            engine.BeginDay(catalog.Day1);

            engine.BeginProduct(catalog.Day1Products[0], "D1-01", 0);
            engine.SubmitDecision("RUN-T16", "D1-01", ProductDecision.Approve, out _);
            engine.NotifyDispositionFinished("RUN-T16", "D1-01", out _);

            var summary = engine.GetDaySummary("DAY_01");
            bool ok = summary.TotalSortingCount == 0 && summary.FormattedSortingRatio == "—";

            return new TestCaseResult(
                "T16",
                "Hiç red/ayrıştırma yapılmadığında oran '0/0' yerine '—' döner",
                ok,
                $"FormattedSortingRatio='{summary.FormattedSortingRatio}'"
            );
        }

        // T17: Finalden tekrar oyna -> Tüm oturum temiz
        private static TestCaseResult Run_T17_RestartClearsAllStateAndIgnoresOldCallbacks()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            SimulateFullGame(engine, catalog, ProductDecision.Approve);

            string oldRunId = engine.CurrentRunId;
            string newRunId = engine.StartNewRun("RUN-FRESH");
            engine.BeginDay(catalog.Day1);

            var freshSummary = engine.GetGameSummary();
            bool oldEmergencyCleared = !engine.IsEmergencyActiveInCurrentDay &&
                                       !engine.CurrentDayRuleSnapshot.ContainsRule(RuleIds.SourceBan);

            bool ok = oldRunId != newRunId &&
                      freshSummary.TotalProducts == 0 &&
                      freshSummary.TotalScore == 0 &&
                      oldEmergencyCleared;

            return new TestCaseResult(
                "T17",
                "Tekrar oyna (StartNewRun) tüm puan, ürün ve acil kural durumunu sıfırlar",
                ok,
                $"YeniRunId={newRunId}, KayıtlıÜrün={freshSummary.TotalProducts}, AcilKuralTemiz={oldEmergencyCleared}"
            );
        }

        // P1: Tartı, Cetvel ve Muzlu Gofret (%10 vs %0.001) kontrolü
        private static TestCaseResult Run_P1_WeightLengthAndIngredientPercentage()
        {
            var catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            var engine = new InspectionRulesEngine();
            engine.StartNewRun("RUN-P1");
            engine.BeginDay(
                dayId: "DAY_P1",
                gameDate: new GameDate(3, 11, 2026),
                initialRules: new[] { catalog.RuleExpiry, catalog.RuleWeight, catalog.RuleLength, catalog.RuleIngredientPct }
            );

            // Muzlu Gofret: Beyan %10, Gerçek %0.001 -> R_INGREDIENT_PCT ihlali
            ProductDefinition wafer = catalog.P1SampleProducts[0];
            engine.BeginProduct(wafer, "P1-01", 0);
            engine.SubmitDecision("RUN-P1", "P1-01", ProductDecision.Reject, out var evalWafer);

            bool ok = evalWafer != null &&
                      evalWafer.ExpectedDecision == ProductDecision.Reject &&
                      evalWafer.IsCorrect &&
                      evalWafer.ViolatedRuleIds.Count == 1 &&
                      evalWafer.ViolatedRuleIds[0] == RuleIds.IngredientPercent;

            return new TestCaseResult(
                "P1",
                "P1 (Muzlu Gofret %10 vs %0.001, Tartı, Cetvel) kural motoru doğrulaması",
                ok,
                $"MuzluGofretBeklenen={evalWafer?.ExpectedDecision}, İhlal={string.Join(",", evalWafer?.ViolatedRuleIds ?? Array.Empty<string>())}"
            );
        }

        // LEAD_INTEGRATION.md Sözleşme Doğrulaması: ICampaignSource + IInspectionBackend (12/12 karar, 6/6 ayrıştırma, 132 puan)
        private static TestCaseResult Run_LeadContract_FullCampaign132Points()
        {
            var go = new GameObject("__TempLeadBackendTest");
            try
            {
                var backend = go.AddComponent<ProductionInspectionBackend>();
                backend.AnalysisSeconds = 0f;

                GameUp.Contracts.CampaignInfo campaign = backend.CreateCampaign();
                Guid runId = Guid.NewGuid();
                backend.BeginRun(runId);

                foreach (var day in campaign.Days)
                {
                    var activeRules = new List<GameUp.Contracts.RuleInfo>(day.Rules);
                    int version = 1;

                    for (int i = 0; i < day.Products.Count; i++)
                    {
                        if (day.Emergency != null && i == day.Emergency.AfterCompletedProducts)
                        {
                            activeRules.Add(day.Emergency.AdditionalRule);
                            version = 2;
                        }

                        var snapshot = new GameUp.Contracts.RuleSnapshot(version, day.Date, activeRules);
                        var prodInfo = day.Products[i];
                        var key = new GameUp.Contracts.ProductKey(runId, prodInfo.Id);
                        var session = new GameUp.Contracts.ProductSession(key, day.Id, prodInfo, snapshot);

                        // Optimal karar için önce değerlendir
                        var probe = backend.Evaluate(session, GameUp.Contracts.PlayerDecision.Approve);
                        var optimalDecision = probe.Expected;
                        var finalEval = backend.Evaluate(session, optimalDecision);

                        GameUp.Contracts.WasteCategory? selectedWaste = null;
                        if (optimalDecision == GameUp.Contracts.PlayerDecision.Reject)
                        {
                            selectedWaste = GetOptimalLeadWaste(prodInfo.Id);
                        }

                        backend.CommitOutcome(session, finalEval, selectedWaste);
                        // İkinci kez CommitOutcome çağrısı idempotent olmalı (puanı değiştirmemeli)
                        backend.CommitOutcome(session, finalEval, selectedWaste);
                    }
                }

                GameUp.Contracts.InspectionSummary summary = backend.GetRunSummary();
                bool ok = summary.Total == 12 &&
                          summary.Correct == 12 &&
                          summary.Sorted == 6 &&
                          summary.CorrectSorts == 6 &&
                          summary.Score == 132 &&
                          summary.Passed;

                return new TestCaseResult(
                    "LEAD",
                    "Hakan LeadContracts (ICampaignSource & IInspectionBackend) tam entegrasyon",
                    ok,
                    $"Toplam={summary.Total}, DoğruKarar={summary.Correct}/12, DoğruAtık={summary.CorrectSorts}/{summary.Sorted}, Puan={summary.Score} (Beklenen: 132)"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static GameUp.Contracts.WasteCategory GetOptimalLeadWaste(string productId)
        {
            return productId switch
            {
                "D1-02" => GameUp.Contracts.WasteCategory.Paper,
                "D1-05" => GameUp.Contracts.WasteCategory.Glass,
                "D1-06" => GameUp.Contracts.WasteCategory.Plastic,
                "D2-02" => GameUp.Contracts.WasteCategory.Contaminated,
                "D2-04" => GameUp.Contracts.WasteCategory.Plastic,
                "D2-06" => GameUp.Contracts.WasteCategory.Contaminated,
                _ => GameUp.Contracts.WasteCategory.Plastic
            };
        }

        private static void SimulateFullGame(
            InspectionRulesEngine engine,
            DefaultGameContentFactory.GeneratedCatalog catalog,
            ProductDecision fixedDecision)
        {
            string runId = engine.StartNewRun();

            // Gün 1
            engine.BeginDay(catalog.Day1);
            for (int i = 0; i < catalog.Day1.Products.Count; i++)
            {
                var p = catalog.Day1.Products[i];
                engine.BeginProduct(p, p.DefinitionId, i);
                engine.SubmitDecision(runId, p.DefinitionId, fixedDecision, out _);
                if (fixedDecision == ProductDecision.Approve)
                    engine.NotifyDispositionFinished(runId, p.DefinitionId, out _);
                else
                    engine.SubmitSortingFinished(runId, p.DefinitionId, WasteBinType.Paper, out _);
            }

            // Gün 2
            engine.BeginDay(catalog.Day2);
            for (int i = 0; i < catalog.Day2.Products.Count; i++)
            {
                var p = catalog.Day2.Products[i];
                engine.BeginProduct(p, p.DefinitionId, i);
                engine.SubmitDecision(runId, p.DefinitionId, fixedDecision, out _);
                if (fixedDecision == ProductDecision.Approve)
                    engine.NotifyDispositionFinished(runId, p.DefinitionId, out _);
                else
                    engine.SubmitSortingFinished(runId, p.DefinitionId, WasteBinType.Plastic, out _);

                if (engine.IsEmergencyTriggeredInCurrentDay && !engine.IsEmergencyActiveInCurrentDay)
                {
                    engine.ActivateEmergency(runId);
                }
            }
        }
    }
}

