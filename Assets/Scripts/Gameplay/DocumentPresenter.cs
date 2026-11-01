using System;
using System.Collections.Generic;
using GameUp.Contracts;
using GameUp.Lead;
using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    public sealed class DocumentPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panel, documentPage;
        [SerializeField] private NewspaperView newspaper;
        [SerializeField] private Text kind, title, body, confirmationLabel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button confirmButton;
        [SerializeField] private AudioSource notificationSpeaker;
        [SerializeField] private AudioClip emergencyNotification;
        private long token;
        public event Action<long> Confirmed;
        public bool IsVisible => panel.activeSelf;
        public void Show(DocumentRequest document, DayDefinition day, IReadOnlyList<RuleInfo> rules)
        {
            bool opened = !panel.activeSelf;
            bool changed = token != document.Token;
            panel.SetActive(true);
            if (changed)
            {
                notificationSpeaker.Stop();
                if(document.Kind==DocumentKind.Emergency)notificationSpeaker.PlayOneShot(emergencyNotification);
                token = document.Token;
                bool isNews = document.Kind == DocumentKind.Newspaper;
                documentPage.SetActive(!isNews);
                if (isNews) newspaper.Show(day, rules);
                else
                {
                    newspaper.Hide();
                    kind.text = document.Kind switch
                    {
                        DocumentKind.Handbook => "DENETİM EL KİTAPÇIĞI",
                        DocumentKind.Analysis => "LABORATUVAR / ANALİZ RAPORU",
                        _ => "BAKANLIK / ACİL DUYURU"
                    };
                    title.text = document.Title; body.text = document.Body;
                    confirmationLabel.text = document.ButtonText;
                    Canvas.ForceUpdateCanvases();
                    scroll.verticalNormalizedPosition = 1;
                }
            }
            if (changed || opened)
            {
                if (document.Kind == DocumentKind.Newspaper) newspaper.SelectConfirmation();
                else confirmButton.Select();
            }
        }
        public void Hide() => panel.SetActive(false);
        public void Confirm() => Confirmed?.Invoke(token);
    }
}
