using System;
using System.Collections.Generic;
using System.Text;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// Saf, deterministik kural ve puan değerlendirme motoru (IMPLEMENTATION.md Bölüm 9 &amp; 12).
    /// Ürünün geçerliliğini yalnızca Faruk'un değerlendirme sistemi hesaplar (Bölüm 4, Kural 2).
    /// </summary>
    public static class ProductRuleEvaluator
    {
        // Bölüm 9.1 Puan Sabitleri
        public const int ScoreCorrectDecision = 10;
        public const int ScoreFalseRejection = -10;
        public const int ScoreFalseAcceptance = -15;
        public const int ScoreCorrectSorting = 2;
        public const int ScoreWrongSorting = -2;

        /// <summary>
        /// Ürünü sabitlenmiş kural kopyasına (RuleSnapshot) göre değerlendirir.
        /// Bu metot puan kaydı (CommitOutcome) yapmaz; saf hesaplama döndürür (Bölüm 9).
        /// </summary>
        public static DecisionEvaluation EvaluateDecision(
            string runId,
            string productId,
            ProductDefinition product,
            RuleSnapshot snapshot,
            ProductDecision playerDecision)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var violatedRuleIds = new List<string>();
            var failureReasons = new List<string>();

            IReadOnlyList<RuleSnapshotEntry> rules = snapshot.ActiveRules;
            for (int i = 0; i < rules.Count; i++)
            {
                RuleSnapshotEntry rule = rules[i];
                if (rule == null || !rule.AppliesToCategory(product.Category))
                    continue;

                if (IsRuleViolated(product, snapshot.GameDate, rule, out string detailedReason))
                {
                    if (!violatedRuleIds.Contains(rule.RuleId))
                    {
                        violatedRuleIds.Add(rule.RuleId);
                    }
                    failureReasons.Add(detailedReason);
                }
            }

            // En az bir geçersizlik koşulu varsa beklenen karar Reject, yoksa Approve olur (Bölüm 9).
            ProductDecision expectedDecision = violatedRuleIds.Count > 0
                ? ProductDecision.Reject
                : ProductDecision.Approve;

            return new DecisionEvaluation(
                runId: runId,
                productId: productId,
                expectedDecision: expectedDecision,
                playerDecision: playerDecision,
                violatedRuleIds: violatedRuleIds,
                failureReasons: failureReasons,
                ruleSnapshotVersion: snapshot.Version
            );
        }

        /// <summary>
        /// Tek bir kuralın ürün tarafından ihlal edilip edilmediğini kontrol eder.
        /// </summary>
        public static bool IsRuleViolated(
            ProductDefinition product,
            GameDate currentGameDate,
            RuleSnapshotEntry rule,
            out string failureReason)
        {
            failureReason = string.Empty;
            if (product == null || rule == null)
                return false;

            switch (rule.RuleType)
            {
                case RuleType.VisibleSpoilage:
                    if (product.Condition != FoodCondition.Spoiled) return false;
                    failureReason = $"[{rule.RuleId}] Görünür bozulma: küf, renk değişimi veya şişme";
                    return true;
                case RuleType.SourceRegionBan:
                    if (product.SourceRegion != rule.BannedSourceRegion || rule.BannedSourceRegion == SourceRegion.None) return false;
                    failureReason = $"[{rule.RuleId}] Geri çağrılan kaynak partisi: {product.SourceRegion.ToTurkishText()}";
                    return true;
                case RuleType.ExpiryDate:
                {
                    // Bölüm 9: ExpiryDate < GameDate geçersizdir.
                    // SKT'nin oyun tarihine eşit olması geçerlidir (T01, D1-04).
                    if (product.ExpiryDate < currentGameDate)
                    {
                        failureReason = $"[{rule.RuleId}] SKT geçmiş ({product.ExpiryDate} < {currentGameDate})";
                        return true;
                    }
                    return false;
                }

                case RuleType.IdentityMismatch:
                {
                    // Bölüm 9: Beyan edilen ürün türü ile arka etiketteki tür/içerik uyumsuz (T02, D1-06).
                    string declared = NormalizeLabelText(product.DeclaredProductType);
                    string listed = NormalizeLabelText(product.ListedProductType);

                    if (!string.Equals(declared, listed, StringComparison.OrdinalIgnoreCase))
                    {
                        failureReason = $"[{rule.RuleId}] Beyan/tür uyumsuzluğu (Ön: '{product.DeclaredProductType}' ≠ Arka: '{product.ListedProductType}')";
                        return true;
                    }
                    return false;
                }

                case RuleType.PollutionAnalysis:
                {
                    // Bölüm 9: Ürünün kirlilik verisi pozitif (T07, D2-02, D2-06).
                    if (product.IsPolluted)
                    {
                        failureReason = $"[{rule.RuleId}] Analiz kirlilik bulgusu pozitif";
                        return true;
                    }
                    return false;
                }

                case RuleType.SourceHabitatBan:
                {
                    // Bölüm 9 & 10.2: Kaynak B VE habitat dip balığı (T04, T05, T06, D2-04).
                    bool regionMatches = rule.BannedSourceRegion != SourceRegion.None &&
                                         product.SourceRegion == rule.BannedSourceRegion;
                    bool habitatMatches = rule.BannedHabitat != FishHabitat.None &&
                                          product.Habitat == rule.BannedHabitat;

                    if (regionMatches && habitatMatches)
                    {
                        failureReason = $"[{rule.RuleId}] Acil kaynak yasağı ({product.SourceRegion.ToTurkishText()} + {product.Habitat.ToTurkishText()})";
                        return true;
                    }
                    return false;
                }

                // P1 Kuralları (Tartı, Cetvel, İçerik Yüzdesi)
                case RuleType.WeightMismatch:
                {
                    if (!product.HasWeightData) return false;
                    float diff = Mathf.Abs(product.ActualWeightGrams - product.DeclaredWeightGrams);
                    if (diff > rule.WeightToleranceGrams)
                    {
                        failureReason = $"[{rule.RuleId}] Ağırlık uyumsuzluğu (Beyan: {product.DeclaredWeightGrams:0.#}g, Gerçek: {product.ActualWeightGrams:0.#}g)";
                        return true;
                    }
                    return false;
                }

                case RuleType.LengthBounds:
                {
                    if (!product.HasLengthData) return false;
                    bool tooShort = rule.MinAllowedLengthCm > 0f && product.ActualLengthCm < rule.MinAllowedLengthCm;
                    bool tooLong = rule.MaxAllowedLengthCm > 0f && product.ActualLengthCm > rule.MaxAllowedLengthCm;
                    if (tooShort || tooLong)
                    {
                        failureReason = $"[{rule.RuleId}] Boy sınırı ihlali ({product.ActualLengthCm:0.#} cm)";
                        return true;
                    }
                    return false;
                }

                case RuleType.IngredientPercentage:
                {
                    // Bölüm 14.3: Etiket, kural ve rapor aynı birimi (0..100 yüzde) kullanır.
                    if (!product.HasIngredientPercentData) return false;
                    bool belowMinimum = product.ActualIngredientPercent < rule.MinRequiredIngredientPercent;
                    bool claimMismatch = Mathf.Abs(product.DeclaredIngredientPercent - product.ActualIngredientPercent) > 0.01f;
                    if (belowMinimum || claimMismatch)
                    {
                        failureReason = $"[{rule.RuleId}] İçerik yüzdesi ihlali (Beyan: %{product.DeclaredIngredientPercent:0.###}, Gerçek: %{product.ActualIngredientPercent:0.###}, Eşik: %{rule.MinRequiredIngredientPercent:0.###})";
                        return true;
                    }
                    return false;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Beklenen atık ayrıştırma hedefini ambalaj malzemesi ve açık kontaminasyon verisinden hesaplar (Bölüm 12).
        /// Kirli ambalaj sırf cam/plastik olduğu için temiz geri dönüşüm sayılmaz (T13).
        /// </summary>
        public static WasteBinType GetExpectedWasteBin(ProductDefinition product)
        {
            if (product == null)
                return WasteBinType.None;

            if (product.IsPackagingContaminated)
                return WasteBinType.Contaminated;

            return product.PackagingMaterial;
        }

        /// <summary>
        /// Reddedilen ürünün ambalaj ayrıştırma seçimini değerlendirir (Bölüm 12).
        /// </summary>
        public static SortingEvaluation EvaluateSorting(
            string runId,
            string productId,
            ProductDefinition product,
            WasteBinType selectedBin)
        {
            WasteBinType expectedBin = GetExpectedWasteBin(product);
            return new SortingEvaluation(
                runId: runId,
                productId: productId,
                expectedBin: expectedBin,
                selectedBin: selectedBin
            );
        }

        /// <summary>
        /// Karar doğruluğuna göre puan farkını hesaplar (Bölüm 9.1).
        /// Doğru onay/red: +10, Hatalı red (sağlam ürünü reddetme): -10, Hatalı kabul (geçersiz ürünü onaylama): -15.
        /// </summary>
        public static int CalculateDecisionScoreDelta(DecisionEvaluation evaluation)
        {
            if (evaluation == null) return 0;
            if (evaluation.IsCorrect) return ScoreCorrectDecision;
            if (evaluation.IsFalseRejection) return ScoreFalseRejection;
            return ScoreFalseAcceptance;
        }

        /// <summary>
        /// Ayrıştırma doğruluğuna göre puan farkını hesaplar (Bölüm 9.1).
        /// Doğru hedef: +2, Yanlış hedef: -2.
        /// </summary>
        public static int CalculateSortingScoreDelta(bool sortingPerformed, SortingEvaluation sortingEvaluation)
        {
            if (!sortingPerformed || sortingEvaluation == null)
                return 0;

            return sortingEvaluation.IsCorrect ? ScoreCorrectSorting : ScoreWrongSorting;
        }

        /// <summary>
        /// Gün sonu ekranında oyuncuya gösterilecek kısa özet açıklamasını oluşturur (L07).
        /// </summary>
        public static string BuildOutcomeSummaryExplanation(
            ProductDefinition product,
            DecisionEvaluation decisionEvaluation,
            bool sortingPerformed,
            SortingEvaluation sortingEvaluation)
        {
            var sb = new StringBuilder();
            string name = product != null ? product.DisplayName : decisionEvaluation?.ProductId ?? "Ürün";

            if (decisionEvaluation != null)
            {
                if (decisionEvaluation.IsCorrect)
                {
                    sb.Append($"{name}: Doğru Karar ({decisionEvaluation.PlayerDecision.ToTurkishText()})");
                    if (decisionEvaluation.ViolatedRuleIds.Count > 0)
                    {
                        sb.Append($" — Neden: {string.Join(", ", decisionEvaluation.FailureReasons)}");
                    }
                }
                else if (decisionEvaluation.IsFalseAcceptance)
                {
                    sb.Append($"{name}: HATALI KABUL — Reddedilmeliydi ({string.Join("; ", decisionEvaluation.FailureReasons)})");
                }
                else if (decisionEvaluation.IsFalseRejection)
                {
                    sb.Append($"{name}: HATALI RED — Ürün tüm kurallara uygundu");
                }
            }

            if (sortingPerformed && sortingEvaluation != null)
            {
                if (sortingEvaluation.IsCorrect)
                {
                    sb.Append($" | Atık: Doğru ({sortingEvaluation.SelectedBin.ToTurkishText()})");
                }
                else
                {
                    sb.Append($" | Atık: Hatalı (Seçilen: {sortingEvaluation.SelectedBin.ToTurkishText()}, Beklenen: {sortingEvaluation.ExpectedBin.ToTurkishText()})");
                }
            }

            return sb.ToString();
        }

        private static string NormalizeLabelText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim().ToLowerInvariant();
        }
    }
}

