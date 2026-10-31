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
        public bool WorldInputAllowed => Flow.State == FlowState.Working && !Documents.IsOpen && !pause.IsOpen && !application.IsLoading;
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
            Documents.Enqueue(new DocumentRequest(DocumentKind.Handbook, "Denetim el kitapçığı",
                "TARİH: " + Flow.Day.Date.ToString("dd.MM.yyyy") + "\n\n" + RulesText() +
                "\n\nATIKLAR\nReddedilen gıda imha edilir. Boş ambalajı malzemesine göre ayrıştır. Kontamine ambalajı ayrı imha hedefine gönder."));
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
                    if (!handbookRead) return "2 / B ile el kitapçığını aç ve SKT kuralını kontrol et.";
                    return "3 / SKT, etiket ve bozulma izlerini kontrol et; W ile onay veya R ile red ver.";
                }
                return "Küf, renk değişimi ve şişmeyi incele. F ile analiz al; kaynağı güncel faksla karşılaştır.";
            }
        }
        private void OnSceneReady(string scene) { if (scene == ApplicationRoot.GameScene) Render(); }
        private void OnApplicationFocus(bool focus) { if (Flow != null) Render(); }
        private void OnDestroy()
        {
            Flow.Changed -= OnFlowChanged; Flow.PresentationRequested -= PresentProduct; Flow.RoutingRequested -= RouteProduct;
            Flow.SortingRequested -= SortProduct; Flow.AnalysisAvailable -= PresentAnalysis; Flow.ProductCompleted -= hud.PresentOutcome;
            Documents.Changed -= OnModalChanged; documentsView.Confirmed -= ConfirmDocument; pause.Changed -= OnModalChanged;
            if (application) application.SceneReady -= OnSceneReady;
        }
    }
}
