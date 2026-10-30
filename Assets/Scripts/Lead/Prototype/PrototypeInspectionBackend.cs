using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameUp.Contracts;
using UnityEngine;

namespace GameUp.Lead.Prototype
{
    /// <summary>Pre-authored answer key for the lead integration scene, NOT the production rule evaluator.</summary>
    public sealed class PrototypeInspectionBackend : MonoBehaviour, IInspectionBackend, ICampaignSource
    {
        [SerializeField, Min(0)] private float analysisSeconds = 1.5f;
        private Guid runId;
        private readonly Dictionary<ProductKey, (string day, ProductOutcome result)> outcomes = new Dictionary<ProductKey, (string, ProductOutcome)>();
        public CampaignInfo CreateCampaign() => PrototypeCampaign.Create();
        public void BeginRun(Guid id) { StopAllCoroutines(); outcomes.Clear(); runId = id; }
        public void RequestAnalysis(ProductSession session, Action<AnalysisReport> completed) => StartCoroutine(Analyze(session, completed));
        private IEnumerator Analyze(ProductSession session, Action<AnalysisReport> completed)
        {
            yield return new WaitForSecondsRealtime(analysisSeconds);
            bool polluted = session.Key.ProductId == "D2-02" || session.Key.ProductId == "D2-06";
            completed(new AnalysisReport(session.Key, "R-" + session.Key.ProductId,
                polluted ? "Kirletici tespit edildi.\nÜrün uygun değil.\nAmbalaj kontamine: ayrı imha hedefine gönder." : "İncelenen örnekte kirletici tespit edilmedi.\nAmbalaj: temiz plastik.\nKaynak yasaklarını ayrıca kontrol et."));
        }
        public DecisionEvaluation Evaluate(ProductSession session, PlayerDecision decision)
        {
            RequireRun(session);
            return new DecisionEvaluation(session.Key, decision, ExpectedDecision(session), Reason(session), session.Rules.Version);
        }
        public ProductOutcome CommitOutcome(ProductSession session, DecisionEvaluation decision, WasteCategory? waste)
        {
            RequireRun(session);
            if (outcomes.TryGetValue(session.Key, out var existing)) return existing.result;
            if (!decision.Key.Equals(session.Key)) throw new InvalidOperationException("Wrong outcome key.");
            bool? sorted = waste.HasValue ? waste.Value == ExpectedWaste(session.Key.ProductId) : (bool?)null;
            int score = decision.Correct ? 10 : decision.Submitted == PlayerDecision.Approve ? -15 : -10;
            if (sorted.HasValue) score += sorted.Value ? 2 : -2;
            var result = new ProductOutcome(decision, sorted, score);
            outcomes.Add(session.Key, (session.DayId, result));
            return result;
        }
        private void RequireRun(ProductSession session)
        { if (session.Key.RunId != runId) throw new InvalidOperationException("Stale prototype session."); }
        public InspectionSummary GetDaySummary(string dayId) => Summarize(outcomes.Values.Where(v => v.day == dayId).Select(v => v.result));
        public InspectionSummary GetRunSummary() => Summarize(outcomes.Values.Select(v => v.result));
        private static InspectionSummary Summarize(IEnumerable<ProductOutcome> source)
        {
            var items = source.ToArray();
            return new InspectionSummary(items.Length, items.Count(x => x.Decision.Correct),
                items.Count(x => !x.Decision.Correct && x.Decision.Submitted == PlayerDecision.Approve),
                items.Count(x => !x.Decision.Correct && x.Decision.Submitted == PlayerDecision.Reject),
                items.Count(x => x.SortingCorrect.HasValue), items.Count(x => x.SortingCorrect == true), items.Sum(x => x.Score));
        }
        public static PlayerDecision ExpectedDecision(ProductSession session)
        {
            string id = session.Key.ProductId;
            bool rejected = id == "D1-02" || id == "D1-05" || id == "D1-06" || id == "D2-02" || id == "D2-06" || (id == "D2-04" && session.Rules.Version > 1);
            return rejected ? PlayerDecision.Reject : PlayerDecision.Approve;
        }
        public static WasteCategory ExpectedWaste(string id)
        {
            if (id == "D2-02" || id == "D2-06") return WasteCategory.Contaminated;
            if (id == "D1-05") return WasteCategory.Glass;
            if (id == "D1-01" || id == "D1-02" || id == "D1-04") return WasteCategory.Paper;
            return WasteCategory.Plastic;
        }
        private static string Reason(ProductSession session)
        {
            switch (session.Key.ProductId)
            {
                case "D1-02": case "D1-05": return "SKT oyun tarihinden önce.";
                case "D1-06": return "Ön yüz iddiası ile arka etiketteki ürün türü uyumsuz.";
                case "D2-02": case "D2-06": return "Analizde kirletici tespit edildi; ambalaj kontamine.";
                case "D2-04": if (session.Rules.Version > 1) return "Acil faks: B bölgesi dip balığı yasağı."; break;
            }
            return "Ürün yürürlükteki kurallara uygun.";
        }
    }

    public static class PrototypeCampaign
    {
        public static CampaignInfo Create()
        {
            var expiry = new RuleInfo("R_EXPIRY", "SKT oyun tarihinden önceyse reddet. Tarih bugünse diğer kurallar uygunsa geçerli.");
            var identity = new RuleInfo("R_IDENTITY", "Ön yüz iddiası ile arka etiketindeki ürün türü/içerik uyumsuzsa reddet.");
            var pollution = new RuleInfo("R_POLLUTION", "Analizde kirletici tespit edilen balıkları reddet. Kontamine ambalajı ayrı imha et.");
            var ban = new RuleInfo("R_SOURCE_BAN", "12.00 sonrası B bölgesinden gelen DİP balıkları, analiz temiz olsa bile reddedilir. Yüzey balıkları bu yasağa dahil değil.");
            return new CampaignInfo(new[]
            {
                new DayInfo("day-1", "Sıcak hava ve yerel üretim", new DateTime(2026, 11, 1),
                    "Antalya'da sıcak hava, gıda taşımacılığını gündeme taşıdı. Süt ürünleri sevkiyatlarında denetimler artırıldı.\n\nYerel üretimin korunması için ürün bilgileri dikkatle kontrol ediliyor. Bugün süt ürünlerinin SKT ve içerik etiketlerini denetle.",
                    new[]
                    {
                        P("D1-01", "Korkuteli Yanıksı Dondurması", "03.11.2026", "Dondurma", "Kâğıt"),
                        P("D1-02", "Korkuteli Yanıksı Dondurması", "31.10.2026", "Dondurma", "Kâğıt"),
                        P("D1-03", "Antalya Yörük Peyniri", "04.11.2026", "Peynir", "Plastik"),
                        P("D1-04", "Akseki Tulum Peyniri", "01.11.2026", "Tulum peyniri", "Kâğıt"),
                        P("D1-05", "Keçi Sütü Kaymağı", "31.10.2026", "Kaymak", "Cam"),
                        new ProductInfo("D1-06", "Sütlü dondurma", "Süt ürünleri", "ÖN YÜZ: Sütlü dondurma\nARKA ETİKET / TÜR: Sütlü buz\nSKT: 03.11.2026\nAMBALAJ: Plastik")
                    }, new[] { expiry, identity }),
                new DayInfo("day-2", "Deniz kirliliği ve caretta carettalar", new DateTime(2026, 11, 2),
                    "Kıyıda bulunan bir caretta carettanın ardından plastik kirliliği yeniden gündemde. Görevliler deniz canlılarından alınan örnekleri inceliyor.\n\nBalıkların kaynak ve habitat etiketlerini oku. Kirlilik görünüşten anlaşılamaz; analiz raporunu kullan.",
                    new[] { F("D2-01", "B", "Dip"), F("D2-02", "A", "Yüzey"), F("D2-03", "A", "Yüzey"), F("D2-04", "B", "Dip"), F("D2-05", "B", "Yüzey"), F("D2-06", "A", "Yüzey") },
                    new[] { expiry, pollution }, new EmergencyInfo(3,
                        "B bölgesinde sızıntı bildirildi.\n\n12.00'den sonra gelen B bölgesi DİP balıkları, analiz sonucu temiz görünse bile tedbir kapsamında reddedilecektir.\n\nKapsam: kaynak B + dip balığı. Önceki sevkiyatların kararları değişmez.", ban))
            });
        }
        private static ProductInfo P(string id, string name, string expiry, string kind, string packaging) =>
            new ProductInfo(id, name, "Süt ürünleri", $"ÜRÜN: {name}\nARKA ETİKET / TÜR: {kind}\nSKT: {expiry}\nAMBALAJ: {packaging}");
        private static ProductInfo F(string id, string zone, string habitat) =>
            new ProductInfo(id, "Balık sevkiyatı " + id.Substring(3), "Balık", $"KAYNAK: {zone} bölgesi\nHABİTAT: {habitat} balığı\nSKT: 04.11.2026\nAMBALAJ: Plastik\nAnaliz sonucu için cihazı kullan.");
    }
}
