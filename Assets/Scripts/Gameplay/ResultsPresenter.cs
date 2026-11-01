using GameUp.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    public sealed class ResultsPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text title, body, continueLabel;
        [SerializeField] private Button continueButton, restartButton, menuButton;
        private bool showingFinal;
        public void Show(InspectionSummary result, int day, bool final, bool hasNextDay, bool failed, int mistakes)
        {
            bool select = !panel.activeSelf || showingFinal != final;
            panel.SetActive(true); showingFinal = final;
            title.text = failed?"Vardiya sonlandırıldı":final ? (result.Passed ? "Görev tamamlandı" : "Denetim raporu: gelişim gerekli") : $"Gün {day} / Vardiya raporu";
            string ratio = result.Sorted == 0 ? "—" : $"{result.CorrectSorts}/{result.Sorted}";
            body.text = (failed?"Üç yanlış işlem nedeniyle göreviniz durduruldu.\n\n":"")
                +$"DOĞRU KARAR    {result.Correct}/{result.Total}  (%{result.Accuracy * 100:0})\nHATALI KABUL    {result.FalseAccepts}\nHATALI RED         {result.FalseRejects}\n\nAYRIŞTIRMA        {ratio}\nGÜNLÜK HATA      {mistakes}/3\nTOPLAM PUAN    {result.Score}";
            continueButton.gameObject.SetActive(!final); restartButton.gameObject.SetActive(final); menuButton.gameObject.SetActive(final);
            continueLabel.text = hasNextDay ? "Ertesi güne geç" : "Görev sonucunu gör";
            if (select) { if (final) restartButton.Select(); else continueButton.Select(); }
        }
        public void Hide() => panel.SetActive(false);
    }
}
