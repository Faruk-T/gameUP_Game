using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameUP.Contracts;
using UnityEngine;
using LeadAnalysisReport = GameUp.Contracts.AnalysisReport;
using LeadCampaignInfo = GameUp.Contracts.CampaignInfo;
using LeadDayInfo = GameUp.Contracts.DayInfo;
using LeadDecisionEvaluation = GameUp.Contracts.DecisionEvaluation;
using LeadEmergencyInfo = GameUp.Contracts.EmergencyInfo;
using LeadICampaignSource = GameUp.Contracts.ICampaignSource;
using LeadIInspectionBackend = GameUp.Contracts.IInspectionBackend;
using LeadInspectionSummary = GameUp.Contracts.InspectionSummary;
using LeadPlayerDecision = GameUp.Contracts.PlayerDecision;
using LeadProductInfo = GameUp.Contracts.ProductInfo;
using LeadProductKey = GameUp.Contracts.ProductKey;
using LeadProductOutcome = GameUp.Contracts.ProductOutcome;
using LeadProductSession = GameUp.Contracts.ProductSession;
using LeadRuleInfo = GameUp.Contracts.RuleInfo;
using LeadRuleSnapshot = GameUp.Contracts.RuleSnapshot;
using LeadWasteCategory = GameUp.Contracts.WasteCategory;

namespace GameUP.Rules
{
    /// <summary>
    /// LEAD_INTEGRATION.md "Faruk'un bağlayacağı sistem" maddesinin gerçek üretim (production) karşılığı.
    /// Hakan'ın LeadRuntime bileşenindeki 'Campaign Source' ve 'Inspection Backend' alanlarına doğrudan atanır.
    /// PrototypeInspectionBackend gibi ürün ID'sine göre sabit cevap anahtarı KULLANMAZ;
    /// kararları, analizi ve atık ayrıştırmayı tamamen ProductDefinition ve RuleDefinition verilerinden
    /// ProductRuleEvaluator aracılığıyla gerçek zamanlı hesaplar.
    /// </summary>
    public sealed class ProductionInspectionBackend : MonoBehaviour, LeadIInspectionBackend, LeadICampaignSource
    {
        [Header("Tasarım Verisi (Boşsa Otomatik Katalog Kullanılır)")]
        [SerializeField] private List<DayDefinition> configuredDays = new List<DayDefinition>();

        [Header("Analiz Cihazı Ayarı")]
        [SerializeField, Min(0f)] private float analysisSeconds = 1.25f;

        private Guid activeRunId;
        private DefaultGameContentFactory.GeneratedCatalog fallbackCatalog;

        private readonly Dictionary<string, ProductDefinition> productsById =
            new Dictionary<string, ProductDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, RuleDefinition> rulesById =
            new Dictionary<string, RuleDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<LeadProductKey, LeadAnalysisReport> cachedReports =
            new Dictionary<LeadProductKey, LeadAnalysisReport>();

        private readonly Dictionary<LeadProductKey, (string dayId, LeadProductOutcome outcome)> committedOutcomes =
            new Dictionary<LeadProductKey, (string dayId, LeadProductOutcome outcome)>();

        public float AnalysisSeconds
        {
            get => analysisSeconds;
            set => analysisSeconds = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            EnsureCatalogIndexed();
        }

        public void SetConfiguredDays(IEnumerable<DayDefinition> days)
        {
            configuredDays = days != null ? new List<DayDefinition>(days) : new List<DayDefinition>();
            EnsureCatalogIndexed();
        }

        /// <summary>
        /// ICampaignSource.CreateCampaign(): ScriptableObject verilerinden (DayDefinition, ProductDefinition, RuleDefinition)
        /// Hakan'ın DayFlowSession akışının beklediği CampaignInfo nesnesini üretir.
        /// </summary>
        public LeadCampaignInfo CreateCampaign()
        {
            EnsureCatalogIndexed();

            var dayInfos = new List<LeadDayInfo>(configuredDays.Count);
            for (int i = 0; i < configuredDays.Count; i++)
            {
                DayDefinition dayDef = configuredDays[i];
                if (dayDef == null) continue;

                string normalizedDayId = NormalizeLeadDayId(dayDef.DayId, i);
                DateTime dayDate = new DateTime(dayDef.GameDate.Year, dayDef.GameDate.Month, dayDef.GameDate.Day);

                string newspaperText = BuildNewspaperText(dayDef.MorningNewspaper);

                var productInfos = new List<LeadProductInfo>(dayDef.Products.Count);
                for (int pIdx = 0; pIdx < dayDef.Products.Count; pIdx++)
                {
                    ProductDefinition prod = dayDef.Products[pIdx];
                    if (prod == null) continue;

                    productInfos.Add(new LeadProductInfo(
                        id: prod.DefinitionId,
                        name: prod.DisplayName,
                        category: prod.Category.ToTurkishText(),
                        label: BuildLeadLabelText(prod)
                    ));
                }

                var ruleInfos = new List<LeadRuleInfo>(dayDef.InitialRules.Count);
                for (int rIdx = 0; rIdx < dayDef.InitialRules.Count; rIdx++)
                {
                    RuleDefinition r = dayDef.InitialRules[rIdx];
                    if (r == null) continue;

                    ruleInfos.Add(new LeadRuleInfo(r.RuleId, r.PlayerVisibleDescription));
                }

                LeadEmergencyInfo emergencyInfo = null;
                if (dayDef.EmergencyEvent != null && dayDef.EmergencyEvent.HasEmergency)
                {
                    RuleDefinition emRule = dayDef.EmergencyEvent.NewRule;
                    DocumentDefinition emDoc = dayDef.EmergencyEvent.EmergencyDocument;
                    string emBody = emDoc != null
                        ? (!string.IsNullOrWhiteSpace(emDoc.TaskInstructionBox)
                            ? $"{emDoc.Body}\n\n{emDoc.TaskInstructionBox}"
                            : emDoc.Body)
                        : emRule.PlayerVisibleDescription;

                    emergencyInfo = new LeadEmergencyInfo(
                        afterCompletedProducts: dayDef.EmergencyEvent.TriggerAfterCompletedProducts,
                        text: emBody,
                        additionalRule: new LeadRuleInfo(emRule.RuleId, emRule.PlayerVisibleDescription)
                    );
                }

                dayInfos.Add(new LeadDayInfo(
                    id: normalizedDayId,
                    title: dayDef.MorningNewspaper != null ? dayDef.MorningNewspaper.Title : dayDef.DayTitle,
                    date: dayDate,
                    newspaper: newspaperText,
                    products: productInfos,
                    rules: ruleInfos,
                    emergency: emergencyInfo
                ));
            }

            return new LeadCampaignInfo(dayInfos);
        }

        /// <summary>
        /// IInspectionBackend.BeginRun(Guid runId): Yeni oturumu başlatır, eski coroutine ve kayıtları temizler.
        /// </summary>
        public void BeginRun(Guid runId)
        {
            if (runId == Guid.Empty)
                throw new ArgumentException("Geçerli bir RunId gereklidir.", nameof(runId));

            if (isActiveAndEnabled)
            {
                StopAllCoroutines();
            }

            EnsureCatalogIndexed();
            activeRunId = runId;
            cachedReports.Clear();
            committedOutcomes.Clear();
        }

        /// <summary>
        /// IInspectionBackend.RequestAnalysis: Ürünün gizli kirlilik verisinden kimlikli analiz raporu üretir.
        /// Aynı ürün için tekrar çağrılırsa aynı raporu verir (T03).
        /// </summary>
        public void RequestAnalysis(LeadProductSession session, Action<LeadAnalysisReport> completed)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (completed == null) throw new ArgumentNullException(nameof(completed));

            if (analysisSeconds <= 0f || !isActiveAndEnabled)
            {
                DeliverAnalysisReport(session, completed);
                return;
            }

            StartCoroutine(AnalyzeRoutine(session, completed));
        }

        private IEnumerator AnalyzeRoutine(LeadProductSession session, Action<LeadAnalysisReport> completed)
        {
            Guid requestedRunId = session.Key.RunId;
            yield return new WaitForSecondsRealtime(analysisSeconds);

            // L05 & T09: Bekleme sırasında oyun yeniden başlatıldıysa eski callback teslim edilmez.
            if (requestedRunId != activeRunId)
                yield break;

            DeliverAnalysisReport(session, completed);
        }

        private void DeliverAnalysisReport(LeadProductSession session, Action<LeadAnalysisReport> completed)
        {
            if (session.Key.RunId != activeRunId)
                return;

            // T03: Aynı ürün için daha önce üretilmiş rapor varsa birebir aynı raporu döndür.
            if (cachedReports.TryGetValue(session.Key, out LeadAnalysisReport cached))
            {
                completed(cached);
                return;
            }

            EnsureCatalogIndexed();
            ProductDefinition product = ResolveProductDefinition(session.Product.Id);
            string reportId = $"R-{session.Key.ProductId}";

            string reportBody;
            if (product != null)
            {
                string packagingNote = product.IsPackagingContaminated
                    ? "Ambalaj kontamine: ayrı imha hedefine (Kontamine Atık) gönder."
                    : $"Ambalaj: temiz {product.PackagingMaterial.ToTurkishText().ToLowerInvariant()}.";

                reportBody = $"{product.AnalysisFindingDetail}\n{packagingNote}";
            }
            else
            {
                reportBody = "İncelenen örnekte kirletici tespit edilmedi.";
            }

            var report = new LeadAnalysisReport(session.Key, reportId, reportBody);
            cachedReports[session.Key] = report;
            completed(report);
        }

        /// <summary>
        /// IInspectionBackend.Evaluate: Yalnızca değerlendirme döndürür; puan kaydı CommitOutcome içinde tutulur.
        /// </summary>
        public LeadDecisionEvaluation Evaluate(LeadProductSession session, LeadPlayerDecision decision)
        {
            RequireActiveRun(session);
            EnsureCatalogIndexed();

            ProductDefinition product = ResolveProductDefinition(session.Product.Id);
            if (product == null)
                throw new InvalidOperationException($"Ürün tanımı bulunamadı: '{session.Product.Id}'");

            GameUP.Contracts.RuleSnapshot internalSnapshot = ConvertRuleSnapshot(session.Rules);
            ProductDecision internalPlayerDecision = decision == LeadPlayerDecision.Approve
                ? ProductDecision.Approve
                : ProductDecision.Reject;

            GameUP.Contracts.DecisionEvaluation internalEval = ProductRuleEvaluator.EvaluateDecision(
                runId: session.Key.RunId.ToString("N"),
                productId: session.Key.ProductId,
                product: product,
                snapshot: internalSnapshot,
                playerDecision: internalPlayerDecision
            );

            LeadPlayerDecision expectedLeadDecision = internalEval.ExpectedDecision == ProductDecision.Approve
                ? LeadPlayerDecision.Approve
                : LeadPlayerDecision.Reject;

            string reasonText = internalEval.FailureReasons.Count > 0
                ? string.Join(" ", internalEval.FailureReasons)
                : "Ürün yürürlükteki kurallara uygun.";

            return new LeadDecisionEvaluation(
                key: session.Key,
                submitted: decision,
                expected: expectedLeadDecision,
                reason: reasonText,
                ruleVersion: session.Rules.Version
            );
        }

        /// <summary>
        /// IInspectionBackend.CommitOutcome: Ürün başına idempotent sonuç ve puan kaydı (Bölüm 7, 9.1, T08).
        /// </summary>
        public LeadProductOutcome CommitOutcome(
            LeadProductSession session,
            LeadDecisionEvaluation decision,
            LeadWasteCategory? waste)
        {
            RequireActiveRun(session);
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (!decision.Key.Equals(session.Key))
                throw new InvalidOperationException("Karar anahtarı (ProductKey) oturum anahtarıyla eşleşmiyor.");

            // T08: Aynı ürün daha önce kaydedildiyse mevcut sonucu döndür (idempotent).
            if (committedOutcomes.TryGetValue(session.Key, out var existing))
            {
                return existing.outcome;
            }

            EnsureCatalogIndexed();
            ProductDefinition product = ResolveProductDefinition(session.Key.ProductId);

            bool? sortingCorrect = null;
            if (waste.HasValue && product != null)
            {
                WasteBinType expectedBin = ProductRuleEvaluator.GetExpectedWasteBin(product);
                WasteBinType selectedBin = ConvertWasteCategory(waste.Value);
                sortingCorrect = selectedBin == expectedBin;
            }

            int score = decision.Correct
                ? ProductRuleEvaluator.ScoreCorrectDecision
                : (decision.Submitted == LeadPlayerDecision.Approve
                    ? ProductRuleEvaluator.ScoreFalseAcceptance
                    : ProductRuleEvaluator.ScoreFalseRejection);

            if (sortingCorrect.HasValue)
            {
                score += sortingCorrect.Value
                    ? ProductRuleEvaluator.ScoreCorrectSorting
                    : ProductRuleEvaluator.ScoreWrongSorting;
            }

            var outcome = new LeadProductOutcome(decision, sortingCorrect, score);
            committedOutcomes.Add(session.Key, (session.DayId, outcome));
            return outcome;
        }

        public LeadInspectionSummary GetDaySummary(string dayId)
        {
            return BuildSummary(committedOutcomes.Values
                .Where(entry => string.Equals(entry.dayId, dayId, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.outcome));
        }

        public LeadInspectionSummary GetRunSummary()
        {
            return BuildSummary(committedOutcomes.Values.Select(entry => entry.outcome));
        }

        private void RequireActiveRun(LeadProductSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (activeRunId == Guid.Empty || session.Key.RunId != activeRunId)
            {
                throw new InvalidOperationException("Geçersiz veya eski (stale) oturum anahtarı.");
            }
        }

        private void EnsureCatalogIndexed()
        {
            if (fallbackCatalog == null)
            {
                fallbackCatalog = DefaultGameContentFactory.CreateCatalogInMemory();
            }

            if (configuredDays == null || configuredDays.Count == 0 || configuredDays[0] == null)
            {
                configuredDays = new List<DayDefinition> { fallbackCatalog.Day1, fallbackCatalog.Day2 };
            }

            productsById.Clear();
            rulesById.Clear();

            // Önce varsayılan kuralları indeksle
            IndexRule(fallbackCatalog.RuleExpiry);
            IndexRule(fallbackCatalog.RuleIdentity);
            IndexRule(fallbackCatalog.RulePollution);
            IndexRule(fallbackCatalog.RuleSourceBan);
            IndexRule(fallbackCatalog.RuleWeight);
            IndexRule(fallbackCatalog.RuleLength);
            IndexRule(fallbackCatalog.RuleIngredientPct);

            // Günlerdeki ürün ve kuralları indeksle
            for (int i = 0; i < configuredDays.Count; i++)
            {
                DayDefinition day = configuredDays[i];
                if (day == null) continue;

                if (day.InitialRules != null)
                {
                    for (int r = 0; r < day.InitialRules.Count; r++)
                        IndexRule(day.InitialRules[r]);
                }

                if (day.EmergencyEvent != null && day.EmergencyEvent.NewRule != null)
                {
                    IndexRule(day.EmergencyEvent.NewRule);
                }

                if (day.Products != null)
                {
                    for (int p = 0; p < day.Products.Count; p++)
                    {
                        ProductDefinition prod = day.Products[p];
                        if (prod != null && !string.IsNullOrWhiteSpace(prod.DefinitionId))
                        {
                            productsById[prod.DefinitionId] = prod;
                        }
                    }
                }
            }

            // P1 örnek ürünlerini de indeksle
            for (int i = 0; i < fallbackCatalog.P1SampleProducts.Count; i++)
            {
                ProductDefinition p1 = fallbackCatalog.P1SampleProducts[i];
                if (p1 != null && !productsById.ContainsKey(p1.DefinitionId))
                {
                    productsById[p1.DefinitionId] = p1;
                }
            }
        }

        private void IndexRule(RuleDefinition rule)
        {
            if (rule != null && !string.IsNullOrWhiteSpace(rule.RuleId))
            {
                rulesById[rule.RuleId] = rule;
            }
        }

        private ProductDefinition ResolveProductDefinition(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return null;

            return productsById.TryGetValue(productId, out ProductDefinition def) ? def : null;
        }

        private GameUP.Contracts.RuleSnapshot ConvertRuleSnapshot(LeadRuleSnapshot leadSnapshot)
        {
            GameDate gameDate = new GameDate(
                leadSnapshot.GameDate.Day,
                leadSnapshot.GameDate.Month,
                leadSnapshot.GameDate.Year);

            var entries = new List<RuleSnapshotEntry>(leadSnapshot.Rules.Count);
            for (int i = 0; i < leadSnapshot.Rules.Count; i++)
            {
                LeadRuleInfo info = leadSnapshot.Rules[i];
                if (info == null) continue;

                if (rulesById.TryGetValue(info.Id, out RuleDefinition ruleDef))
                {
                    entries.Add(ruleDef.ToSnapshotEntry());
                }
            }

            return new GameUP.Contracts.RuleSnapshot(leadSnapshot.Version, gameDate, entries);
        }

        private static WasteBinType ConvertWasteCategory(LeadWasteCategory category)
        {
            return category switch
            {
                LeadWasteCategory.Paper => WasteBinType.Paper,
                LeadWasteCategory.Glass => WasteBinType.Glass,
                LeadWasteCategory.Plastic => WasteBinType.Plastic,
                LeadWasteCategory.Contaminated => WasteBinType.Contaminated,
                _ => WasteBinType.None
            };
        }

        private static LeadInspectionSummary BuildSummary(IEnumerable<LeadProductOutcome> outcomes)
        {
            LeadProductOutcome[] items = outcomes.ToArray();
            return new LeadInspectionSummary(
                total: items.Length,
                correct: items.Count(x => x.Decision.Correct),
                falseAccepts: items.Count(x => !x.Decision.Correct && x.Decision.Submitted == LeadPlayerDecision.Approve),
                falseRejects: items.Count(x => !x.Decision.Correct && x.Decision.Submitted == LeadPlayerDecision.Reject),
                sorted: items.Count(x => x.SortingCorrect.HasValue),
                correctSorts: items.Count(x => x.SortingCorrect == true),
                score: items.Sum(x => x.Score)
            );
        }

        private static string NormalizeLeadDayId(string rawDayId, int index)
        {
            if (string.IsNullOrWhiteSpace(rawDayId))
                return $"day-{index + 1}";

            if (string.Equals(rawDayId, "DAY_01", StringComparison.OrdinalIgnoreCase))
                return "day-1";
            if (string.Equals(rawDayId, "DAY_02", StringComparison.OrdinalIgnoreCase))
                return "day-2";

            return rawDayId;
        }

        private static string BuildNewspaperText(DocumentDefinition doc)
        {
            if (doc == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(doc.TaskInstructionBox))
                return doc.Body;
            return $"{doc.Body}\n\n{doc.TaskInstructionBox}";
        }

        private static string BuildLeadLabelText(ProductDefinition prod)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ÖN YÜZ BEYANI: {prod.DeclaredProductType}");
            sb.AppendLine($"ARKA ETİKET / TÜR: {prod.ListedProductType}");
            sb.AppendLine($"İÇİNDEKİLER: {prod.IngredientsSummary}");
            sb.AppendLine($"SKT: {prod.ExpiryDate}");

            if (prod.Category == ProductCategory.Fish)
            {
                sb.AppendLine($"KAYNAK: {prod.SourceRegion.ToTurkishText()}");
                sb.AppendLine($"HABİTAT: {prod.Habitat.ToTurkishText()}");
                sb.AppendLine("NOT: Kirlilik görünüşten anlaşılamaz; analiz cihazını kullan.");
            }

            sb.Append($"AMBALAJ: {prod.PackagingMaterial.ToTurkishText()}");
            return sb.ToString();
        }
    }
}

