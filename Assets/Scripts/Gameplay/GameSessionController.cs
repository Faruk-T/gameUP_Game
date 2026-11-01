using System;
using System.Text;
using GameUp.App;
using GameUp.Contracts;
using GameUp.Lead;
using GameUP.Rules;
using UnityEngine;

namespace GameUp.Gameplay
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private ProductionInspectionBackend backend;
        [SerializeField] private MonoBehaviour presentationSource;
        [SerializeField] private GameHUD hud;
        [SerializeField] private DocumentPresenter documentsView;
        [SerializeField] private ResultsPresenter resultsView;
        [SerializeField] private PauseMenu pause;
        [SerializeField] private FaxPresenter fax;
        private ApplicationRoot application;
        private IProductPresentation presentation;
        private FlowState presentedState = FlowState.Boot;
        private bool resetting, inspected, handbookRead;
        public DayFlowSession Flow { get; private set; }
        public DocumentQueue Documents { get; } = new DocumentQueue();
        public ProductionInspectionBackend Backend => backend;
        public bool ProductInspected => inspected;
        public bool WorldInputAllowed => Flow != null && pause != null && application != null && Flow.State == FlowState.Working && !Documents.IsOpen && !pause.IsOpen && !application.IsLoading;
        public event Action Refreshed;

        private void Awake()
        {
            application = ApplicationRoot.Require();
            if (!backend || !hud || !documentsView || !resultsView || !pause || !fax || !(presentationSource is IProductPresentation port))
                throw new InvalidOperationException("Game scene composition is incomplete. Run the editor scene validator.");
            presentation = port;
            Flow = new DayFlowSession(backend.CreateCampaign(), backend);
            Flow.Changed += OnFlowChanged;
            Flow.PresentationRequested += PresentProduct;
            Flow.RoutingRequested += RouteProduct;
            Flow.SortingRequested += SortProduct;
            Flow.AnalysisAvailable += PresentAnalysis;
            Flow.ProductCompleted += hud.PresentOutcome;
            Documents.Changed += OnModalChanged;
            documentsView.Confirmed += ConfirmDocument;
            pause.Changed += OnModalChanged;
            application.SceneReady += OnSceneReady;
        }
        private void Start() => Restart();
        public void Restart()
        {
            resetting = true;
            if (pause.IsOpen) pause.Resume();
            Documents.Clear(); presentation.ResetPresentation(); fax.ResetPresentation(); hud.ClearFeedback();
            inspected = handbookRead = false; presentedState = FlowState.Boot;
            Flow.StartNewRun(); resetting = false; OnFlowChanged();
        }
        private void OnFlowChanged()
        {
            if (resetting) return;
            if (presentedState != Flow.State)
            {
                presentedState = Flow.State;
                Guid run = Flow.RunId;
                if (Flow.State == FlowState.MorningBriefing)
                    Documents.Enqueue(new DocumentRequest(DocumentKind.Newspaper, Flow.Day.Title,
                        Flow.Day.Newspaper + "\n\nBUGÜNÜN KURALLARI\n" + RulesText(), true, "Göreve başla",
                        () => { if (Flow.RunId == run) Flow.AcknowledgeMorning(); }));
                else if (Flow.State == FlowState.EmergencyBriefing)
                {
                    fax.Print(true);
                    Documents.Enqueue(new DocumentRequest(DocumentKind.Emergency, "Bakanlık acil faksı — 12.00",
                        Flow.Day.Emergency.Text, true, "Okudum, yeni kuralla devam et",
                        () => { if (Flow.RunId == run) Flow.AcknowledgeEmergency(); }));
                }
            }
            Render();
        }
        private void OnModalChanged()
        {
            if (resetting) return;
            Flow.SetDocumentOpen(Documents.IsOpen || pause.IsOpen);
            Render();
        }
        private void Render()
        {
            application.SetWorldInput(WorldInputAllowed);
            hud.Render(this);
            var document = Documents.Current;
            if (document != null && !pause.IsOpen)
            {
                documentsView.Show(document, backend.ConfiguredDays[Flow.DayIndex], Flow.CurrentRules);
            }
            else documentsView.Hide();
            if (!Documents.IsOpen && !pause.IsOpen && (Flow.State == FlowState.DaySummary || Flow.State == FlowState.GameSummary))
            {
                bool final = Flow.State == FlowState.GameSummary;
                resultsView.Show(final ? Flow.RunSummary : Flow.DaySummary, Flow.DayIndex + 1, final, Flow.DayIndex + 1 < backend.ConfiguredDays.Count);
            }
            else resultsView.Hide();
            Refreshed?.Invoke();
        }
        private void PresentProduct(ProductSession session)
        { inspected = false; Render(); presentation.Present(session, key => Flow.NotifyArrival(key)); }
        private void RouteProduct(ProductSession session, PlayerDecision decision) =>
            presentation.Route(session, decision, key => Flow.NotifyDispositionFinished(key));
        private void SortProduct(ProductSession session, WasteCategory waste) =>
            presentation.Sort(session, waste, (key, bin) => Flow.NotifySortingFinished(key, bin));
        private void PresentAnalysis(AnalysisReport report)
        {
            fax.Print(false);
            Documents.Enqueue(new DocumentRequest(DocumentKind.Analysis, "Analiz raporu • " + report.Key.ProductId,
                "RAPOR NO: " + report.ReportId + "\nÜRÜN: " + report.Key.ProductId + "\n\n" + report.Text));
        }
        public void NotifyProductInspected(ProductKey key)
        { if (Flow.Current != null && Flow.Current.Key.Equals(key) && Flow.CanDecide) { inspected = true; Render(); } }
        public void Approve() { if (Flow.Current != null) Flow.SubmitDecision(Flow.Current.Key, PlayerDecision.Approve); }
        public void Reject() { if (Flow.Current != null) Flow.SubmitDecision(Flow.Current.Key, PlayerDecision.Reject); }
        public void Analyze() { if (Flow.Current != null) Flow.RequestAnalysis(Flow.Current.Key); }
        public void SelectWaste(WasteCategory waste) { if (Flow.Current != null) Flow.BeginSorting(Flow.Current.Key, waste); }
        public void OpenHandbook()
        {
            if (!WorldInputAllowed) return;
            handbookRead = true;
            Documents.Enqueue(new DocumentRequest(
                DocumentKind.Handbook,
                "Denetim El Kitapçığı & Saha Rehberi",
                BuildHandbookText()));
        }
        private string BuildHandbookText()
        {
            string today = Flow.Day.Date.ToString("dd.MM.yyyy");
            int dayNum = Flow.DayIndex + 1;
            string unlockedGroups = Flow.DayIndex switch
            {
                0 => "Et ve Süt Ürünleri (Süt, Peynir, Yoğurt, Kıyma / Et)",
                1 => "Meyveler (Kavun, Papaya, Ananas) + Et ve Süt Ürünleri",
                _ => "Yumurta + Meyve, Et ve Süt Ürünleri (Karma Vardiya)"
            };
            string dailyFocus = Flow.DayIndex switch
            {
                0 => "• <b>SKT Kontrolü:</b> Bugünün tarihi (<b>" + today + "</b>) veya daha ileri bir tarih geçerlidir. Bugünden eski tarihli ürünleri reddedin.\n" +
                     "• <b>Ön ve Arka Etiket Uyumu:</b> Ürünü <b>E</b> ile elinize alıp fareyle çevirin. Ön yüzdeki ürün adı ile arka etiketteki yasal tür beyanı uyuşmuyorsa (ör. önde <i>Süt</i>, arkada <i>Sütlü İçecek</i> yazıyorsa) <b>RED</b> verin.\n" +
                     "• <b>Görünür Bozulma:</b> Ürünün üzerinde küf, yeşerme, renk kararması veya ambalaj şişmesi varsa <b>RED</b> verin.",
                1 => "• <b>Yeni Grup (Meyveler) & Gizli Kirlilik:</b> Dışarıdan taze ve temiz görünen ürünlerde çevresel kalıntı / toksin bulunabilir.\n" +
                     "• <b>Laboratuvar Analizi (F Tuşu):</b> Etiket ve görünümü sağlam olan ürünlerde mutlaka <b>F</b> tuşuna (veya soldaki <b>Analiz Cihazına</b>) basarak laboratuvar raporu alın.\n" +
                     "• <b>Kontamine Ambalaj Uyarısı:</b> Analiz raporu <b>POZİTİF</b> çıkarsa ürünü <b>R / RED</b> ile reddedin ve boş ambalajını arkadaki <b>KIRMIZI (Kontamine Atık)</b> kutusuna gönderin!",
                _ => "• <b>Karma Vardiya & Kaynak Takibi:</b> Yumurta, et, süt ve meyve gruplarının tamamı denetlenir. Arka etiketteki <b>Kaynak Bölgesi (A, B, C)</b> bilgisini kontrol edin.\n" +
                     "• <b>Bakanlık Acil Faksı Takibi:</b> Saat 12.00'de faks cihazından acil geri çağırma emri gelebilir. Faks onaylandıktan sonra gelen ürünlerde yasaklı bölge kuralını uygulayın."
            };

            var sb = new StringBuilder();
            sb.Append("<b><color=#1F6E64>VARDİYA DURUMU:</color></b> GÜN ").Append(dayNum)
              .Append("  •  <b>BUGÜNÜN TARİHİ: ").Append(today).AppendLine("</b>");
            sb.Append("<b><color=#1F6E64>DENETLENEN GRUPLAR:</color></b> ").AppendLine(unlockedGroups);
            sb.AppendLine();

            sb.AppendLine("<b><color=#1F6E64>1. SABAH GAZETESİ ÖZETİ (KAÇIRANLAR İÇİN GÜNLÜK İSTİHBARAT)</color></b>");
            sb.Append("<b>Manşet:</b> \"").Append(Flow.Day.Title).AppendLine("\"");
            sb.AppendLine(Flow.Day.Newspaper);
            sb.AppendLine();
            sb.AppendLine("<b>Bugün Özellikle Dikkat Edilecek Noktalar:</b>");
            sb.AppendLine(dailyFocus);

            bool emergencyActive = Flow.Day.Emergency != null && Flow.CurrentRules.Count > Flow.Day.Rules.Count;
            if (emergencyActive)
            {
                sb.AppendLine();
                sb.AppendLine("<b><color=#9E2A2B>[!] AKTİF BAKANLIK ACİL FAKSI (12.00 SONRASI ZORUNLU KURAL):</color></b>");
                sb.AppendLine(Flow.Day.Emergency.Text);
                sb.AppendLine("• <b>Özet Kural:</b> Kaynak <b>Bölge B</b> olan <b>ET, SÜT ve YUMURTA</b> ürünleri (analiz temiz olsa bile) <b>RED</b> edilir! <b>Meyveler</b> bu yasağın dışındadır.");
            }

            sb.AppendLine();
            sb.AppendLine("<b><color=#1F6E64>2. YÜRÜRLÜKTEKİ RESMÎ DENETİM KURALLARI</color></b>");
            foreach (var rule in Flow.CurrentRules)
                sb.Append("• <b>").Append(rule.Text).AppendLine("</b>");

            sb.AppendLine();
            sb.AppendLine("<b><color=#1F6E64>3. ATIK AYRIŞTIRMA REHBERİ (ARKADAKİ 4 GERİ DÖNÜŞÜM KUTUSU)</color></b>");
            sb.AppendLine("Ürüne <b>R / RED</b> verdiğinizde gıda kısmı otomatik imha edilir ve masada boş ambalaj kalır. Fareyle <b>arkanıza (180°)</b> dönüp ambalajı doğru kutuya tıklayarak gönderin:");
            sb.AppendLine("• <b><color=#295EB3>MAVİ KUTU (KÂĞIT):</color></b> Temiz kâğıt ve karton kutu ambalajlar (ör. süt kutusu, yumurta viyolü).");
            sb.AppendLine("• <b><color=#248059>YEŞİL KUTU (CAM):</color></b> Temiz cam şişe ve kavanoz ambalajlar.");
            sb.AppendLine("• <b><color=#B8860B>SARI KUTU (PLASTİK):</color></b> Temiz plastik kap, kase ve paketler (ör. yoğurt kabı, et/peynir tabağı).");
            sb.AppendLine("• <b><color=#A62E29>KIRMIZI KUTU (KONTAMİNE ATIK — AYRI İMHA):</color></b> Laboratuvar analizinde <b>POZİTİF (kirlilik / kontamine)</b> çıkan ürünlerin ambalajları! <i>(Malzemesi kâğıt, cam veya plastik olsa bile kontamine ambalajlar mutlaka KIRMIZI kutuya atılmalıdır!)</i>");

            sb.AppendLine();
            sb.AppendLine("<b><color=#1F6E64>4. NASIL OYNANIR? (KONTROLLER VE PUANLAMA)</color></b>");
            sb.AppendLine("• <b>E Tuşu veya Ürüne Sol Tık:</b> Ürünü yakından incelemeye al / masaya bırak.");
            sb.AppendLine("• <b>İncelemede Sol Tık + Sürükle:</b> Ürünü 360° çevirerek ön/arka etiketi ve küf izlerini incele.");
            sb.AppendLine("• <b>İncelemede Fare Tekerleği:</b> Ürüne yakınlaş / uzaklaş.");
            sb.AppendLine("• <b>F Tuşu veya Analiz Cihazı:</b> Ürünü laboratuvar analizine gönder (gizli kirliliği tespit eder).");
            sb.AppendLine("• <b>T Tuşu veya Faks Cihazı:</b> Son çıkan laboratuvar analiz raporunu tekrar aç.");
            sb.AppendLine("• <b>B Tuşu veya Masadaki Kitaplar:</b> Bu Denetim El Kitapçığını ve Gazete Özetini aç.");
            sb.AppendLine("• <b>W Tuşu veya Yeşil ONAY Düğmesi:</b> Sağlam ürünü onayla (piyasaya gönder).");
            sb.AppendLine("• <b>R Tuşu veya Kırmızı RED Düğmesi:</b> Kusurlu ürünü reddet ve ardından arkadaki kutularda ayrıştır.");
            sb.AppendLine("• <b>Puanlama:</b> Doğru Karar <b>+10</b>  |  Doğru Ayrıştırma <b>+5</b>  |  Hatalı Red <b>-5</b>  |  Hatalı Onay <b>-15</b>  |  Yanlış Kutu <b>-5</b>  (Gün sonu hedefi: en az <b>%75</b> doğruluk).");
            return sb.ToString();
        }
        public void ReopenReport() { if (WorldInputAllowed && Flow.LastReport != null) PresentAnalysis(Flow.LastReport); }
        public void HandleEscape()
        {
            if (pause.IsOpen) pause.Escape();
            else if (Documents.Current != null) Documents.Dismiss(Documents.Current.Token);
            else pause.Open();
        }
        public void ConfirmDocument(long token) => Documents.Confirm(token);
        public void ContinueSummary() { if (!Documents.IsOpen && !pause.IsOpen) Flow.ContinueAfterSummary(); }
        public void ReturnToMenu() => application.LoadMenu();
        public void SetTargetHint(string hint) => hud.SetTarget(hint);
        private string RulesText()
        {
            var text = new StringBuilder();
            foreach (var rule in Flow.CurrentRules) text.Append("• ").AppendLine(rule.Text).AppendLine();
            return text.ToString();
        }
        public string TutorialText
        {
            get
            {
                if (Flow.State != FlowState.Working) return "";
                if (Flow.Phase == ProductPhase.AwaitingSort) return "Gıda imha edildi. Arkana dön; ambalajı renk ve etiketine göre uygun kutuya gönder.";
                if (Flow.DayIndex == 0 && Flow.ProductIndex < 2)
                {
                    if (!inspected) return "1 / Ürüne bakıp E ile incele. Fareyle döndür; ön ve arka etiketi oku.";
                    if (!handbookRead) return "2 / B ile veya masadaki kitaplara tıklayarak el kitapçığını ve günün özetini aç.";
                    return "3 / SKT, etiket ve bozulma izlerini kontrol et; W ile onay veya R ile red ver.";
                }
                return "Küf, renk değişimi ve şişmeyi incele. F ile analiz al; kaynağı güncel faksla karşılaştır.";
            }
        }
        private void OnSceneReady(string scene) { if (scene == ApplicationRoot.GameScene) Render(); }
        private void OnApplicationFocus(bool focus) { if (Flow != null) Render(); }
        private void OnDestroy()
        {
            if (Flow != null)
            {
                Flow.Changed -= OnFlowChanged; Flow.PresentationRequested -= PresentProduct; Flow.RoutingRequested -= RouteProduct;
                Flow.SortingRequested -= SortProduct; Flow.AnalysisAvailable -= PresentAnalysis;
                if (hud) Flow.ProductCompleted -= hud.PresentOutcome;
            }
            Documents.Changed -= OnModalChanged;
            if (documentsView) documentsView.Confirmed -= ConfirmDocument;
            if (pause) pause.Changed -= OnModalChanged;
            if (application) application.SceneReady -= OnSceneReady;
        }
    }
}
