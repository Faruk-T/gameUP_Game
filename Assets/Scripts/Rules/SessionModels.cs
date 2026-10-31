using System;
using System.Collections.Generic;
using System.Text;

namespace GameUP.Contracts
{
    /// <summary>
    /// Ürün başladığı anda sabitlenen kural kopyası (IMPLEMENTATION.md Bölüm 6.2).
    /// Acil olay (Emergency) etkinleştiğinde yeni bir RuleSnapshot örneği üretilir;
    /// önceki ürünlerin elindeki RuleSnapshot kesinlikle değişmez.
    /// </summary>
    [Serializable]
    public sealed class RuleSnapshot
    {
        public int Version { get; }
        public GameDate GameDate { get; }
        public IReadOnlyList<RuleSnapshotEntry> ActiveRules { get; }

        public RuleSnapshot(int version, GameDate gameDate, IEnumerable<RuleSnapshotEntry> activeRules)
        {
            Version = version;
            GameDate = gameDate;

            var list = new List<RuleSnapshotEntry>();
            if (activeRules != null)
            {
                foreach (var rule in activeRules)
                {
                    if (rule != null)
                    {
                        list.Add(rule);
                    }
                }
            }
            ActiveRules = list.AsReadOnly();
        }

        public bool ContainsRule(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return false;
            for (int i = 0; i < ActiveRules.Count; i++)
            {
                if (string.Equals(ActiveRules[i].RuleId, ruleId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Kitapçık (Rulebook) arayüzünün doğrudan kural verisinden beslenmesi için metin üretir (Bölüm 4, Kural 5).
        /// </summary>
        public string BuildRulebookBodyText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>GÜNÜN DENETİM TARİHİ: {GameDate}</b>");
            sb.AppendLine($"<i>Kural Sürümü: v{Version}</i>");
            sb.AppendLine();

            for (int i = 0; i < ActiveRules.Count; i++)
            {
                var r = ActiveRules[i];
                sb.AppendLine($"<b>{i + 1}. [{r.RuleId}] {r.RuleTitle}</b>");
                sb.AppendLine(r.PlayerVisibleDescription);
                sb.AppendLine();
            }

            sb.AppendLine("<b>ATIK AYRIŞTIRMA KURALLARI:</b>");
            sb.AppendLine("• Reddedilen ürünün gıda kısmı imha bandına gönderilir.");
            sb.AppendLine("• Temiz ambalajlar malzemesine göre Kâğıt, Cam veya Plastik kutusuna atılır.");
            sb.AppendLine("• Kirletici/toksin barındıran (kontamine) ambalajlar kesinlikle <b>Kontamine Atık</b> kutusuna atılmalıdır!");
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// Analiz cihazından fakslanan kimlikli rapor (IMPLEMENTATION.md Bölüm 6.2 &amp; L05).
    /// </summary>
    [Serializable]
    public sealed class AnalysisReport
    {
        public string RunId { get; }
        public string ProductId { get; }
        public string DefinitionId { get; }
        public string ProductDisplayName { get; }
        public string ReportId { get; }
        public bool IsPolluted { get; }
        public string FindingText { get; }
        public string FormattedDocumentText { get; }

        public AnalysisReport(
            string runId,
            string productId,
            string definitionId,
            string productDisplayName,
            string reportId,
            bool isPolluted,
            string findingText)
        {
            RunId = runId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            ProductDisplayName = productDisplayName ?? string.Empty;
            ReportId = reportId ?? string.Empty;
            IsPolluted = isPolluted;
            FindingText = findingText ?? string.Empty;

            string statusHeader = isPolluted
                ? "POZİTİF (TEHLİKELİ KİRLİLİK / TOKSİN TESPİT EDİLDİ)"
                : "NEGATİF (TEMİZ — GÜVENLİ SINIRLARDA)";

            var sb = new StringBuilder();
            sb.AppendLine($"<b>COP31 ANTALYA LABORATUVAR ANALİZ RAPORU</b>");
            sb.AppendLine($"Rapor No: <b>{ReportId}</b>");
            sb.AppendLine($"Ürün Kimliği: <b>{ProductId}</b> ({ProductDisplayName})");
            sb.AppendLine($"Sonuç Durumu: <b>{statusHeader}</b>");
            sb.AppendLine($"Laboratuvar Bulgusu: {FindingText}");
            FormattedDocumentText = sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// Oyuncunun verdiği Onay/Red kararının değerlendirme sonucu (IMPLEMENTATION.md Bölüm 6.2 &amp; 9).
    /// Hesaplama, puan kaydından (CommitOutcome) ayrıdır.
    /// </summary>
    [Serializable]
    public sealed class DecisionEvaluation
    {
        public string RunId { get; }
        public string ProductId { get; }
        public ProductDecision ExpectedDecision { get; }
        public ProductDecision PlayerDecision { get; }
        public bool IsCorrect => ExpectedDecision == PlayerDecision;
        public IReadOnlyList<string> ViolatedRuleIds { get; }
        public IReadOnlyList<string> FailureReasons { get; }
        public int RuleSnapshotVersion { get; }

        /// <summary>
        /// Sağlam ürünü reddetme (Hatalı Red).
        /// </summary>
        public bool IsFalseRejection => ExpectedDecision == ProductDecision.Approve && PlayerDecision == ProductDecision.Reject;

        /// <summary>
        /// Geçersiz ürünü piyasaya gönderme (Hatalı Kabul).
        /// </summary>
        public bool IsFalseAcceptance => ExpectedDecision == ProductDecision.Reject && PlayerDecision == ProductDecision.Approve;

        public DecisionEvaluation(
            string runId,
            string productId,
            ProductDecision expectedDecision,
            ProductDecision playerDecision,
            IEnumerable<string> violatedRuleIds,
            IEnumerable<string> failureReasons,
            int ruleSnapshotVersion)
        {
            RunId = runId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            ExpectedDecision = expectedDecision;
            PlayerDecision = playerDecision;
            RuleSnapshotVersion = ruleSnapshotVersion;

            var ruleList = new List<string>();
            if (violatedRuleIds != null)
            {
                foreach (var id in violatedRuleIds)
                {
                    if (!string.IsNullOrEmpty(id)) ruleList.Add(id);
                }
            }
            ViolatedRuleIds = ruleList.AsReadOnly();

            var reasonList = new List<string>();
            if (failureReasons != null)
            {
                foreach (var reason in failureReasons)
                {
                    if (!string.IsNullOrEmpty(reason)) reasonList.Add(reason);
                }
            }
            FailureReasons = reasonList.AsReadOnly();
        }
    }

    /// <summary>
    /// Reddedilen ürünün ambalaj ayrıştırma değerlendirmesi (IMPLEMENTATION.md Bölüm 12).
    /// </summary>
    [Serializable]
    public sealed class SortingEvaluation
    {
        public string RunId { get; }
        public string ProductId { get; }
        public WasteBinType ExpectedBin { get; }
        public WasteBinType SelectedBin { get; }
        public bool IsCorrect => ExpectedBin == SelectedBin;

        public SortingEvaluation(
            string runId,
            string productId,
            WasteBinType expectedBin,
            WasteBinType selectedBin)
        {
            RunId = runId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            ExpectedBin = expectedBin;
            SelectedBin = selectedBin;
        }
    }

    /// <summary>
    /// Ürün başına yalnızca BİR KEZ kaydedilen nihai sonuç (IMPLEMENTATION.md Bölüm 6.2 &amp; 9.1).
    /// </summary>
    [Serializable]
    public sealed class ProductOutcome
    {
        public string RunId { get; }
        public string DayId { get; }
        public string ProductId { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public DecisionEvaluation DecisionEvaluation { get; }
        public bool SortingPerformed { get; }
        public SortingEvaluation SortingEvaluation { get; }
        public int DecisionScoreDelta { get; }
        public int SortingScoreDelta { get; }
        public int TotalScoreDelta => DecisionScoreDelta + SortingScoreDelta;
        public string SummaryExplanation { get; }

        public ProductOutcome(
            string runId,
            string dayId,
            string productId,
            string definitionId,
            string displayName,
            DecisionEvaluation decisionEvaluation,
            bool sortingPerformed,
            SortingEvaluation sortingEvaluation,
            int decisionScoreDelta,
            int sortingScoreDelta,
            string summaryExplanation)
        {
            RunId = runId ?? string.Empty;
            DayId = dayId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            DecisionEvaluation = decisionEvaluation;
            SortingPerformed = sortingPerformed;
            SortingEvaluation = sortingEvaluation;
            DecisionScoreDelta = decisionScoreDelta;
            SortingScoreDelta = sortingScoreDelta;
            SummaryExplanation = summaryExplanation ?? string.Empty;
        }
    }

    /// <summary>
    /// Gün sonu özeti (IMPLEMENTATION.md Bölüm 9.1 &amp; L07).
    /// Ayrıştırma hiç yapılmamışsa (0 red varsa) FormattedSortingRatio "0/0" yerine "—" döner (T16).
    /// </summary>
    [Serializable]
    public sealed class DaySummaryReport
    {
        public string RunId { get; }
        public string DayId { get; }
        public int TotalProducts { get; }
        public int CorrectDecisionCount { get; }
        public int FalseAcceptanceCount { get; }
        public int FalseRejectionCount { get; }
        public int CorrectSortingCount { get; }
        public int TotalSortingCount { get; }
        public int TotalDayScore { get; }
        public IReadOnlyList<ProductOutcome> Outcomes { get; }

        public float DecisionAccuracyRatio => TotalProducts > 0
            ? (float)CorrectDecisionCount / TotalProducts
            : 0f;

        public int DecisionAccuracyPercent => TotalProducts > 0
            ? (int)Math.Round(DecisionAccuracyRatio * 100.0)
            : 0;

        /// <summary>
        /// T16 kabul ölçütü: Hiç red yoksa "0/0" yerine "—" gösterilir.
        /// </summary>
        public string FormattedSortingRatio => TotalSortingCount > 0
            ? $"{CorrectSortingCount}/{TotalSortingCount}"
            : "—";

        public DaySummaryReport(
            string runId,
            string dayId,
            IReadOnlyList<ProductOutcome> outcomes)
        {
            RunId = runId ?? string.Empty;
            DayId = dayId ?? string.Empty;
            Outcomes = outcomes ?? Array.Empty<ProductOutcome>();
            TotalProducts = Outcomes.Count;

            int correctDecisions = 0;
            int falseAccepts = 0;
            int falseRejects = 0;
            int correctSorts = 0;
            int totalSorts = 0;
            int score = 0;

            for (int i = 0; i < Outcomes.Count; i++)
            {
                var o = Outcomes[i];
                score += o.TotalScoreDelta;

                if (o.DecisionEvaluation != null)
                {
                    if (o.DecisionEvaluation.IsCorrect)
                        correctDecisions++;
                    else if (o.DecisionEvaluation.IsFalseAcceptance)
                        falseAccepts++;
                    else if (o.DecisionEvaluation.IsFalseRejection)
                        falseRejects++;
                }

                if (o.SortingPerformed && o.SortingEvaluation != null)
                {
                    totalSorts++;
                    if (o.SortingEvaluation.IsCorrect)
                        correctSorts++;
                }
            }

            CorrectDecisionCount = correctDecisions;
            FalseAcceptanceCount = falseAccepts;
            FalseRejectionCount = falseRejects;
            CorrectSortingCount = correctSorts;
            TotalSortingCount = totalSorts;
            TotalDayScore = score;
        }
    }

    /// <summary>
    /// Oyun sonu genel sonuç raporu (IMPLEMENTATION.md Bölüm 9.1 &amp; L07).
    /// Başarı etiketi için karar doğruluğu en az %75 olmalıdır (12 üründe en az 9 doğru karar).
    /// Ayrıştırma puanı bu koşulun yerine geçmez.
    /// </summary>
    [Serializable]
    public sealed class GameSummaryReport
    {
        public const float RequiredAccuracyRatio = 0.75f;

        public string RunId { get; }
        public int TotalProducts { get; }
        public int CorrectDecisionCount { get; }
        public int FalseAcceptanceCount { get; }
        public int FalseRejectionCount { get; }
        public int CorrectSortingCount { get; }
        public int TotalSortingCount { get; }
        public int TotalScore { get; }
        public IReadOnlyList<DaySummaryReport> DayReports { get; }
        public IReadOnlyList<ProductOutcome> AllOutcomes { get; }

        public float DecisionAccuracyRatio => TotalProducts > 0
            ? (float)CorrectDecisionCount / TotalProducts
            : 0f;

        public int DecisionAccuracyPercent => TotalProducts > 0
            ? (int)Math.Round(DecisionAccuracyRatio * 100.0)
            : 0;

        /// <summary>
        /// Bölüm 9.1: Karar doğruluğu en az %75 olmalıdır; ayrıştırma puanı bunun yerine geçmez.
        /// </summary>
        public bool IsSuccess => TotalProducts > 0 && DecisionAccuracyRatio >= RequiredAccuracyRatio;

        public string FormattedSortingRatio => TotalSortingCount > 0
            ? $"{CorrectSortingCount}/{TotalSortingCount}"
            : "—";

        public string ResultBadgeTitle => IsSuccess
            ? "BAŞARILI — COP31 ANTALYA DENETİMİ TAMAMLANDI"
            : "BAŞARISIZ — DENETİM DOĞRULUĞU YETERSİZ (<%75)";

        public GameSummaryReport(
            string runId,
            IReadOnlyList<DaySummaryReport> dayReports,
            IReadOnlyList<ProductOutcome> allOutcomes)
        {
            RunId = runId ?? string.Empty;
            DayReports = dayReports ?? Array.Empty<DaySummaryReport>();
            AllOutcomes = allOutcomes ?? Array.Empty<ProductOutcome>();
            TotalProducts = AllOutcomes.Count;

            int correctDecisions = 0;
            int falseAccepts = 0;
            int falseRejects = 0;
            int correctSorts = 0;
            int totalSorts = 0;
            int score = 0;

            for (int i = 0; i < AllOutcomes.Count; i++)
            {
                var o = AllOutcomes[i];
                score += o.TotalScoreDelta;

                if (o.DecisionEvaluation != null)
                {
                    if (o.DecisionEvaluation.IsCorrect)
                        correctDecisions++;
                    else if (o.DecisionEvaluation.IsFalseAcceptance)
                        falseAccepts++;
                    else if (o.DecisionEvaluation.IsFalseRejection)
                        falseRejects++;
                }

                if (o.SortingPerformed && o.SortingEvaluation != null)
                {
                    totalSorts++;
                    if (o.SortingEvaluation.IsCorrect)
                        correctSorts++;
                }
            }

            CorrectDecisionCount = correctDecisions;
            FalseAcceptanceCount = falseAccepts;
            FalseRejectionCount = falseRejects;
            CorrectSortingCount = correctSorts;
            TotalSortingCount = totalSorts;
            TotalScore = score;
        }
    }

    /// <summary>
    /// Masaya gelen tek bir ürünün çalışma zamanı (runtime) oturumu (IMPLEMENTATION.md Bölüm 6.2).
    /// </summary>
    [Serializable]
    public sealed class ProductSession
    {
        public string RunId { get; }
        public string DayId { get; }
        public string ProductId { get; }
        public int SequenceIndex { get; }
        public ProductDefinition Definition { get; }
        public RuleSnapshot Snapshot { get; }

        public bool IsAnalyzed { get; private set; }
        public AnalysisReport CachedAnalysisReport { get; private set; }

        public bool HasDecisionEvaluation { get; private set; }
        public DecisionEvaluation DecisionEvaluation { get; private set; }

        public bool HasSortingEvaluation { get; private set; }
        public SortingEvaluation SortingEvaluation { get; private set; }

        public bool IsOutcomeCommitted { get; private set; }
        public ProductOutcome CommittedOutcome { get; private set; }

        public ProductSession(
            string runId,
            string dayId,
            string productId,
            int sequenceIndex,
            ProductDefinition definition,
            RuleSnapshot snapshot)
        {
            RunId = runId ?? throw new ArgumentNullException(nameof(runId));
            DayId = dayId ?? string.Empty;
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            SequenceIndex = sequenceIndex;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public void CacheAnalysis(AnalysisReport report)
        {
            if (report == null) return;
            IsAnalyzed = true;
            CachedAnalysisReport = report;
        }

        /// <summary>
        /// İlk geçerli karar değerlendirmesini kilitler; seçilmiş karar sonradan değiştirilemez (Bölüm 7).
        /// </summary>
        public bool TrySetDecisionEvaluation(DecisionEvaluation evaluation)
        {
            if (HasDecisionEvaluation || IsOutcomeCommitted || evaluation == null)
                return false;

            HasDecisionEvaluation = true;
            DecisionEvaluation = evaluation;
            return true;
        }

        /// <summary>
        /// İlk geçerli ayrıştırma değerlendirmesini kilitler.
        /// </summary>
        public bool TrySetSortingEvaluation(SortingEvaluation evaluation)
        {
            if (HasSortingEvaluation || IsOutcomeCommitted || evaluation == null)
                return false;

            HasSortingEvaluation = true;
            SortingEvaluation = evaluation;
            return true;
        }

        /// <summary>
        /// Ürün başına yalnızca bir kez terminal sonuç kaydedilir (Bölüm 6.2 &amp; 7).
        /// </summary>
        public bool TryCommitOutcome(ProductOutcome outcome)
        {
            if (IsOutcomeCommitted || outcome == null)
                return false;

            IsOutcomeCommitted = true;
            CommittedOutcome = outcome;
            return true;
        }
    }
}

