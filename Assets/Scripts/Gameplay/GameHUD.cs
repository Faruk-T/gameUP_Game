using GameUp.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    public sealed class GameHUD : MonoBehaviour
    {
        [SerializeField] private Text status, productTitle, productLabel, tutorial, targetHint, feedback;
        [SerializeField] private GameObject productCard;
        public void Render(GameSessionController session)
        {
            var flow = session.Flow;
            string progress = flow.State == FlowState.Working
                ? $"ÜRÜN {Mathf.Min(flow.ProductIndex + 1, flow.Day.Products.Count)}/{flow.Day.Products.Count}"
                : $"{flow.ProductIndex}/{flow.Day.Products.Count} TAMAMLANDI";
            status.text = $"GÜN {flow.DayIndex + 1}  •  {flow.Day.Date:dd.MM.yyyy}  •  {flow.ClockText}  •  {progress}";
            productCard.SetActive(session.ProductInspected && flow.State == FlowState.Working);
            if (flow.Current != null)
            {
                productTitle.text = flow.Current.Product.Name;
                productLabel.text = "KİMLİK: " + flow.Current.Key.ProductId + "\n\n" + flow.Current.Product.Label;
            }
            tutorial.text = flow.Phase == ProductPhase.Analyzing ? "Analiz sürüyor… Rapor fakstan gelecek." : session.TutorialText;
        }
        public void SetTarget(string value) => targetHint.text = value;
        public void ClearFeedback() => feedback.text = "";
        public void PresentOutcome(ProductOutcome outcome)
        {
            var result = outcome.Decision;
            string sorting = outcome.SortingCorrect.HasValue ? (outcome.SortingCorrect.Value ? " • Ayrıştırma doğru" : " • Ayrıştırma hatalı") : "";
            feedback.text = $"{result.Key.ProductId}  {(result.Correct ? "Doğru karar" : "Hatalı karar")}{sorting}\n{result.Reason}  ({outcome.Score:+0;-0;0})";
        }
    }
}
