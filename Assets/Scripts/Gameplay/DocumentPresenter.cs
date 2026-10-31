using System;
using GameUp.Lead;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    public sealed class DocumentPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panel, illustrationArea;
        [SerializeField] private Image illustration;
        [SerializeField] private Text kind, title, body, confirmationLabel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button confirmButton;
        private long token;
        public event Action<long> Confirmed;
        public bool IsVisible => panel.activeSelf;
        public void Show(DocumentRequest document, Sprite picture)
        {
            bool changed = token != document.Token;
            token = document.Token;
            panel.SetActive(true);
            kind.text = document.Kind switch
            {
                Contracts.DocumentKind.Newspaper => "ANTALYA / SABAH GAZETESİ",
                Contracts.DocumentKind.Handbook => "DENETİM EL KİTAPÇIĞI",
                Contracts.DocumentKind.Analysis => "LABORATUVAR / FAKS RAPORU",
                _ => "BAKANLIK / ACİL DUYURU"
            };
            title.text = document.Title; body.text = document.Body; confirmationLabel.text = document.ButtonText;
            illustrationArea.SetActive(picture != null);
            illustration.sprite = picture;
            if (changed)
            {
                Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1; confirmButton.Select();
            }
        }
        public void Hide() => panel.SetActive(false);
        public void Confirm() => Confirmed?.Invoke(token);
    }
}
