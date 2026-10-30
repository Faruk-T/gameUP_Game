using System;
using GameUp.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Lead.Unity
{
    [Serializable]
    public sealed class LeadViewWidgets
    {
        public Text Header, Status, ProductTitle, ProductBody, Feedback, Tutorial;
        public Text DocumentKind, DocumentTitle, DocumentBody, DocumentButtonLabel;
        public Text SummaryTitle, SummaryBody, SummaryButtonLabel;
        public GameObject DocumentPanel, SummaryPanel;
        public Button Inspect, Approve, Reject, Analyze, Handbook, Report, DocumentConfirm, SummaryContinue, Restart, Quit;
        public Button[] WasteButtons;
        public ScrollRect DocumentScroll;
    }

    public sealed class LeadView : MonoBehaviour
    {
        [SerializeField] private LeadViewWidgets widgets;
        private LeadRuntime runtime;
        private long visibleDocumentToken;
        private FlowState? visibleSummaryState;
        public LeadViewWidgets Widgets => widgets;
        public void Configure(LeadViewWidgets value) => widgets = value;

        public void Bind(LeadRuntime owner)
        {
            if (widgets == null || !widgets.DocumentPanel || !widgets.SummaryPanel || widgets.WasteButtons?.Length != 4)
                throw new InvalidOperationException("LeadView must be authored with its widgets.");
            runtime = owner;
            widgets.Inspect.onClick.AddListener(owner.Inspect);
            widgets.Approve.onClick.AddListener(owner.Approve);
            widgets.Reject.onClick.AddListener(owner.Reject);
            widgets.Analyze.onClick.AddListener(owner.Analyze);
            widgets.Handbook.onClick.AddListener(owner.OpenHandbook);
            widgets.Report.onClick.AddListener(owner.ReopenReport);
            widgets.DocumentConfirm.onClick.AddListener(ConfirmVisibleDocument);
            widgets.SummaryContinue.onClick.AddListener(owner.ContinueSummary);
            widgets.Restart.onClick.AddListener(owner.Restart);
            widgets.Quit.onClick.AddListener(owner.Quit);
            for (int i = 0; i < widgets.WasteButtons.Length; i++)
            {
                var category = (WasteCategory)i;
                widgets.WasteButtons[i].onClick.AddListener(() => owner.SelectWaste(category));
            }
        }

        private void ConfirmVisibleDocument() => runtime.ConfirmDocument(visibleDocumentToken);
        public void SetFeedback(string message) { if (widgets?.Feedback) widgets.Feedback.text = message; }

        public void Render(LeadRuntime owner)
        {
            var flow = owner.Flow;
            widgets.Header.text = "ANTALYA / DENETİM NOKTASI";
            widgets.Status.text = $"GÜN {flow.DayIndex + 1}  •  {flow.Day.Date:dd.MM.yyyy}  •  {flow.ClockText}  •  {Math.Min(flow.ProductIndex + 1, flow.Day.Products.Count)}/{flow.Day.Products.Count} ÜRÜN";
            widgets.ProductTitle.text = flow.Current?.Product.Name ?? "Günün sevkiyatını bekliyorsun";
            widgets.ProductBody.text = owner.ProductInspected && flow.Current != null
                ? $"KİMLİK: {flow.Current.Key.ProductId}\n{flow.Current.Product.Label}" : "Etiketi görmek için ürünü incele.\n\nMasadaki ürünün ön ve arka yüzündeki bilgileri karşılaştır.";
            widgets.Tutorial.text = flow.Phase == ProductPhase.Analyzing ? "Analiz sürüyor… Rapor fakstan gelecek." : owner.TutorialText;
            widgets.Inspect.interactable = flow.CanDecide;
            widgets.Approve.interactable = flow.CanDecide;
            widgets.Reject.interactable = flow.CanDecide;
            widgets.Analyze.interactable = flow.CanDecide;
            widgets.Handbook.interactable = owner.WorldInputAllowed;
            widgets.Report.interactable = owner.WorldInputAllowed && flow.LastReport != null;
            foreach (var button in widgets.WasteButtons) button.interactable = flow.CanSort;

            var document = owner.Documents.Current;
            widgets.DocumentPanel.SetActive(document != null);
            if (document != null)
            {
                bool newDocument = visibleDocumentToken != document.Token;
                visibleDocumentToken = document.Token;
                widgets.DocumentKind.text = KindLabel(document.Kind);
                widgets.DocumentTitle.text = document.Title;
                widgets.DocumentBody.text = document.Body;
                widgets.DocumentButtonLabel.text = document.ButtonText;
                if (newDocument)
                {
                    Canvas.ForceUpdateCanvases();
                    if (widgets.DocumentScroll) widgets.DocumentScroll.verticalNormalizedPosition = 1;
                    widgets.DocumentConfirm.Select();
                }
            }

            bool summary = flow.State == FlowState.DaySummary || flow.State == FlowState.GameSummary;
            widgets.SummaryPanel.SetActive(summary && document == null);
            if (summary)
            {
                bool final = flow.State == FlowState.GameSummary;
                var result = final ? flow.RunSummary : flow.DaySummary;
                widgets.SummaryTitle.text = final ? (result.Passed ? "Görev tamamlandı" : "Denetim raporu: gelişim gerekli") : $"Gün {flow.DayIndex + 1} tamamlandı";
                string ratio = result.Sorted == 0 ? "—" : $"{result.CorrectSorts}/{result.Sorted}";
                widgets.SummaryBody.text = $"DOĞRU KARAR     {result.Correct}/{result.Total}  (%{result.Accuracy * 100:0})\n\nHATALI KABUL     {result.FalseAccepts}\nHATALI RED          {result.FalseRejects}\n\nAYRIŞTIRMA         {ratio}\nTOPLAM PUAN     {result.Score}";
                widgets.SummaryContinue.gameObject.SetActive(!final);
                widgets.SummaryButtonLabel.text = flow.DayIndex == 0 ? "Ertesi güne geç" : "Görev sonucunu gör";
                widgets.Restart.gameObject.SetActive(final);
                widgets.Quit.gameObject.SetActive(final);
                if (visibleSummaryState != flow.State)
                {
                    visibleSummaryState = flow.State;
                    if (final) widgets.Restart.Select(); else widgets.SummaryContinue.Select();
                }
            }
            else visibleSummaryState = null;
        }

        private static string KindLabel(DocumentKind kind)
        {
            switch (kind)
            {
                case DocumentKind.Newspaper: return "SABAH GAZETESİ";
                case DocumentKind.Handbook: return "EL KİTAPÇIĞI";
                case DocumentKind.Analysis: return "ANALİZ / FAKS";
                default: return "ACİL DUYURU";
            }
        }
    }
}
