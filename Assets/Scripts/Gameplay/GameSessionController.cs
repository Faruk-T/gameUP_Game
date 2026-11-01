using System;
using System.Collections;
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
        [SerializeField] private IntroCinematic intro;
        [SerializeField] private FaxPresenter fax;
        private ApplicationRoot application;
        private IProductPresentation presentation;
        private FlowState presentedState = FlowState.Boot;
        private bool resetting, inspected, handbookRead;
        public DayFlowSession Flow { get; private set; }
        public DocumentQueue Documents { get; } = new DocumentQueue();
        public ProductionInspectionBackend Backend => backend;
        public bool ProductInspected => inspected;
        public bool IsIntroducing => intro.IsPlaying;
        public bool WorldInputAllowed => Flow != null && pause && application && !IsIntroducing && Flow.State == FlowState.Working && !Documents.IsOpen && !pause.IsOpen && !application.IsLoading;
        public event Action Refreshed;

        private void Awake()
        {
            application = ApplicationRoot.Require();
            if (!backend || !hud || !documentsView || !resultsView || !pause || !intro || !fax || !(presentationSource is IProductPresentation port))
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
        private IEnumerator Start()
        {
            application.Audio.BeginOpening();
            yield return intro.Play();
            application.Audio.EnterWorkstation();
            Restart();
        }
        public void Restart()
        {
            if (IsIntroducing) return;
            resetting = true;
            if (pause.IsOpen) pause.Resume();
            Documents.Clear(); presentation.ResetPresentation(); hud.ClearFeedback();fax.ResetPresentation();application.Audio.ResetWorkstation();
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
                        () => { if (Flow.RunId == run) { fax.Acknowledge(); Flow.AcknowledgeEmergency(); } }));
                }
                else if (Flow.State == FlowState.MistakeBriefing)
                {
                    var outcome=Flow.LastOutcome;
                    string errors=(!outcome.Decision.Correct?"HATALI KARAR: "+outcome.Decision.Reason+"\n":"")
                        +(outcome.SortingCorrect==false?"HATALI AYRIŞTIRMA: Ambalaj uygun atık kutusuna gönderilmedi.\n":"");
                    string conclusion=Flow.DailyMistakes>=DayFlowSession.MistakeLimit
                        ?"Üç yanlış işlem sınırına ulaşıldı. Vardiyanız sonlandırılmıştır."
                        :"El kitapçığını ve güncel kuralları yeniden kontrol edin. Aynı gün üçüncü yanlış işlemde vardiya sonlandırılır.";
                    fax.Print(true);
                    Documents.Enqueue(new DocumentRequest(DocumentKind.Infraction,"Bakanlık / Yanlış işlem bildirimi",
                        "TARİH: "+Flow.Day.Date.ToString("dd.MM.yyyy")+"\nÜRÜN: "+outcome.Decision.Key.ProductId
                        +"\nGÜNLÜK HATA: "+Flow.DailyMistakes+" / "+DayFlowSession.MistakeLimit+"\n\n"+errors+"\n"+conclusion,
                        true,Flow.DailyMistakes>=DayFlowSession.MistakeLimit?"Vardiya sonucunu gör":"Tebellüğ ettim, devam et",
                        () => { if (Flow.RunId == run) { fax.Acknowledge(); Flow.AcknowledgeMistake(); } }));
                }
                else if (Flow.State == FlowState.DaySummary) fax.Print(false);
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
            if (IsIntroducing || Flow.State == FlowState.Boot) return;
            application.SetWorldInput(WorldInputAllowed);
            bool blocked=Documents.IsOpen||pause.IsOpen;
            application.Audio.SetPaused(blocked);
            application.Audio.SetBeltRunning(Flow.State==FlowState.Working&&!blocked);
            application.Audio.SetAnalysis(Flow.Phase==ProductPhase.Analyzing,blocked);
            hud.Render(this);
            var document = Documents.Current;
            if (document != null && !pause.IsOpen)
            {
                documentsView.Show(document, backend.ConfiguredDays[Flow.DayIndex], Flow.CurrentRules);
            }
            else documentsView.Hide();
            if (!Documents.IsOpen && !pause.IsOpen && (Flow.State == FlowState.DaySummary || Flow.State == FlowState.GameSummary || Flow.State == FlowState.GameOver))
            {
                bool failed=Flow.State==FlowState.GameOver;
                bool final = Flow.State == FlowState.GameSummary||failed;
                resultsView.Show(final&&!failed ? Flow.RunSummary : Flow.DaySummary, Flow.DayIndex + 1, final,
                    Flow.DayIndex + 1 < backend.ConfiguredDays.Count,failed,Flow.DailyMistakes);
            }
            else resultsView.Hide();
            Refreshed?.Invoke();
        }
        private void PresentProduct(ProductSession session)
        { inspected = false; Render(); presentation.Present(session, key => Flow.NotifyArrival(key)); }
        private void RouteProduct(ProductSession session, PlayerDecision decision)
        {
            application.Audio.PlayCue(GameAudioCue.Button);
            presentation.Route(session, decision, key =>
            {
                if(decision==PlayerDecision.Reject)application.Audio.PlayCue(GameAudioCue.Disposal);
                Flow.NotifyDispositionFinished(key);
            });
        }
        private void SortProduct(ProductSession session, WasteCategory waste) =>
            presentation.Sort(session, waste, (key, bin) => Flow.NotifySortingFinished(key, bin));
        private void PresentAnalysis(AnalysisReport report)
        {
            fax.Print(false);
            var definition=backend.GetProductDefinition(report.Key.ProductId);
            Documents.Enqueue(new DocumentRequest(DocumentKind.Analysis, "Analiz raporu / " + definition.DisplayName,
                "RAPOR NO: " + report.ReportId + "\n\n" + report.Text));
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
            Documents.Enqueue(new DocumentRequest(DocumentKind.Handbook, "Denetim El Kitapçığı ve Saha Rehberi",
                HandbookContent.Build(Flow)));
        }
        public void HandleEscape()
        {
            if (IsIntroducing) return;
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
                    if (!inspected) return "1 / Sağdaki bantta gelen ürüne bakıp E ile incele. Döndürerek ön ve arka etiketi oku.";
                    if (!handbookRead) return "2 / B ile veya masadaki kitaba tıklayarak el kitapçığını ve günün özetini aç.";
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
