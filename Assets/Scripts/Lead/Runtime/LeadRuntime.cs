using System;
using System.Linq;
using GameUp.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameUp.Lead.Unity
{
    public sealed class LeadRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour campaignSource;
        [SerializeField] private MonoBehaviour inspectionBackend;
        [SerializeField] private MonoBehaviour productPresentation;
        [SerializeField] private LeadView view;
        [SerializeField] private FaxPresenter fax;
        [SerializeField] private bool lockCursorDuringGameplay;
        private IProductPresentation presentation;
        private FlowState presentedState = FlowState.Boot;
        private bool resetting;
        private bool inspected;
        private bool handbookRead;
        public DayFlowSession Flow { get; private set; }
        public DocumentQueue Documents { get; } = new DocumentQueue();
        public bool ProductInspected => inspected;
        public bool WorldInputAllowed => Flow != null && Flow.State == FlowState.Working && !Documents.IsOpen;
        public event Action Refreshed;

        public void Configure(MonoBehaviour source, MonoBehaviour backend, MonoBehaviour workstation, LeadView ui, FaxPresenter printer)
        { campaignSource = source; inspectionBackend = backend; productPresentation = workstation; view = ui; fax = printer; }

        private void Start()
        {
            if (!(campaignSource is ICampaignSource source) || !(inspectionBackend is IInspectionBackend backend) ||
                !(productPresentation is IProductPresentation port) || !view)
                throw new InvalidOperationException("LeadRuntime needs a campaign source, inspection backend, presentation adapter and view.");
            presentation = port;
            Flow = new DayFlowSession(source.CreateCampaign(), backend);
            Flow.Changed += OnFlowChanged;
            Flow.PresentationRequested += PresentProduct;
            Flow.RoutingRequested += RouteProduct;
            Flow.SortingRequested += SortProduct;
            Flow.AnalysisAvailable += PresentAnalysis;
            Flow.ProductCompleted += ShowOutcome;
            Documents.Changed += OnDocumentChanged;
            view.Bind(this);
            Restart();
        }

        public void Restart()
        {
            if (Flow == null) return;
            resetting = true;
            Documents.Clear();
            presentation.ResetPresentation();
            if (fax) fax.ResetPresentation();
            inspected = false; handbookRead = false; presentedState = FlowState.Boot;
            view.SetFeedback("");
            Flow.StartNewRun();
            resetting = false;
            OnFlowChanged();
        }

        private void OnFlowChanged()
        {
            if (resetting) return;
            if (presentedState != Flow.State)
            {
                presentedState = Flow.State;
                var runId = Flow.RunId;
                if (Flow.State == FlowState.MorningBriefing)
                    Documents.Enqueue(new DocumentRequest(DocumentKind.Newspaper, Flow.Day.Title,
                        Flow.Day.Newspaper + "\n\nBUGÜNÜN GÖREVLERİ\n" + RulesText(), true, "Göreve başla",
                        () => { if (Flow.RunId == runId) Flow.AcknowledgeMorning(); }));
                else if (Flow.State == FlowState.EmergencyBriefing)
                {
                    if (fax) fax.Print(true);
                    Documents.Enqueue(new DocumentRequest(DocumentKind.Emergency, "Bakanlık acil faksı — 12.00",
                        Flow.Day.Emergency.Text, true, "Okudum, yeni kuralla devam et",
                        () => { if (Flow.RunId == runId) Flow.AcknowledgeEmergency(); }));
                }
            }
            Refresh();
        }

        private void OnDocumentChanged()
        {
            if (resetting || Flow == null) return;
            Flow.SetDocumentOpen(Documents.IsOpen);
            Refresh();
        }

        private void Refresh()
        {
            if (Flow == null) return;
            bool useCursor = Documents.IsOpen || Flow.State != FlowState.Working || !lockCursorDuringGameplay;
            Cursor.lockState = useCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = useCursor;
            view.Render(this);
            Refreshed?.Invoke();
        }

        private void PresentProduct(ProductSession session)
        {
            inspected = false;
            Refresh();
            presentation.Present(session, key => Flow.NotifyArrival(key));
        }
        private void RouteProduct(ProductSession session, PlayerDecision decision) =>
            presentation.Route(session, decision, key => Flow.NotifyDispositionFinished(key));
        private void SortProduct(ProductSession session, WasteCategory waste) =>
            presentation.Sort(session, waste, (key, selected) => Flow.NotifySortingFinished(key, selected));

        private void PresentAnalysis(AnalysisReport report)
        {
            if (fax) fax.Print(false);
            Documents.Enqueue(new DocumentRequest(DocumentKind.Analysis, $"Analiz raporu • {report.Key.ProductId}",
                $"RAPOR NO: {report.ReportId}\nÜRÜN: {report.Key.ProductId}\n\n{report.Text}"));
        }

        private void ShowOutcome(ProductOutcome outcome)
        {
            var decision = outcome.Decision;
            string result = decision.Correct ? "Doğru karar" : "Hatalı karar";
            string sorting = outcome.SortingCorrect.HasValue ? (outcome.SortingCorrect.Value ? " • Ayrıştırma doğru" : " • Ayrıştırma hatalı") : "";
            view.SetFeedback($"{decision.Key.ProductId}: {result}{sorting}\n{decision.Reason}  ({outcome.Score:+0;-0;0} puan)");
        }

        private string RulesText() => string.Join("\n\n", Flow.CurrentRules.Select(r => "• " + r.Text));

        public void OpenHandbook()
        {
            if (Flow == null || !WorldInputAllowed) return;
            handbookRead = true;
            Documents.Enqueue(new DocumentRequest(DocumentKind.Handbook, "Denetim el kitapçığı",
                $"TARİH: {Flow.Day.Date:dd.MM.yyyy}\n\n{RulesText()}\n\nATIKLAR\nReddedilen gıda imha edilir. Boş ambalajı malzemesine göre ayrıştır. Kontamine ambalajı ayrı imha hedefine gönder."));
        }

        public void Inspect()
        {
            if (Flow?.Current == null || !Flow.CanDecide) return;
            NotifyProductInspected(Flow.Current.Key);
        }
        public void NotifyProductInspected(ProductKey key)
        { if (Flow?.Current != null && Flow.Current.Key.Equals(key) && Flow.CanDecide) { inspected = true; Refresh(); } }
        public void Analyze() { if (Flow?.Current != null) Flow.RequestAnalysis(Flow.Current.Key); }
        public void Approve() { if (Flow?.Current != null) Flow.SubmitDecision(Flow.Current.Key, PlayerDecision.Approve); }
        public void Reject() { if (Flow?.Current != null) Flow.SubmitDecision(Flow.Current.Key, PlayerDecision.Reject); }
        public void SelectWaste(WasteCategory waste) { if (Flow?.Current != null) Flow.BeginSorting(Flow.Current.Key, waste); }
        public void ReopenReport()
        {
            if (WorldInputAllowed && Flow.LastReport != null) PresentAnalysis(Flow.LastReport);
        }
        public void ContinueSummary() { if (!Documents.IsOpen) Flow?.ContinueAfterSummary(); }
        public void ConfirmDocument(long token) => Documents.Confirm(token);
        public void Quit() => Application.Quit();

        public string TutorialText
        {
            get
            {
                if (Flow == null || Flow.State != FlowState.Working) return "";
                if (Flow.Phase == ProductPhase.AwaitingSort) return "Gıda imha edildi. Ambalajı doğru atık hedefine gönder.";
                if (Flow.DayIndex == 0 && Flow.ProductIndex < 2)
                {
                    if (!inspected) return "1 / Ürünü incele. Ön ve arka etiketini oku.";
                    if (!handbookRead) return "2 / El kitapçığını aç ve SKT kuralını kontrol et.";
                    return "3 / SKT'yi oyun tarihiyle karşılaştır; onay veya red ver.";
                }
                return Flow.DayIndex == 1 ? "Kaynak ve habitat etiketlerini incele; analiz raporunu ve güncel kuralları kontrol et." : "Etiket, SKT ve içerik bilgilerini karşılaştır.";
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || Flow == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame && Documents.Current != null) Documents.Dismiss(Documents.Current.Token);
            else if (keyboard.bKey.wasPressedThisFrame) OpenHandbook();
        }

        private void OnApplicationFocus(bool focus) { if (focus && view && Flow != null) Refresh(); }
        private void OnDestroy()
        {
            if (Flow != null)
            {
                Flow.Changed -= OnFlowChanged;
                Flow.PresentationRequested -= PresentProduct;
                Flow.RoutingRequested -= RouteProduct;
                Flow.SortingRequested -= SortProduct;
                Flow.AnalysisAvailable -= PresentAnalysis;
                Flow.ProductCompleted -= ShowOutcome;
            }
            Documents.Changed -= OnDocumentChanged;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
