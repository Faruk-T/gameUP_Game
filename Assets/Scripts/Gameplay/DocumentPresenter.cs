using System;
using System.Collections.Generic;
using GameUp.App;
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
        private long token;
        private DocumentKind visibleKind;
        private bool updating;
        private float lastScrollSound;
        public event Action<long> Confirmed;
        public bool IsVisible => panel.activeSelf;
        public void Show(DocumentRequest document, DayDefinition day, IReadOnlyList<RuleInfo> rules)
        {
            bool opened = !panel.activeSelf;
            bool changed = token != document.Token;
            panel.SetActive(true);
            if (changed)
            {
                updating=true;visibleKind=document.Kind;
                if(document.Kind==DocumentKind.Handbook)ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.BookOpen);
                if(document.Kind==DocumentKind.Newspaper)ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.Newspaper);
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
                        DocumentKind.Infraction => "BAKANLIK / İŞLEM DENETİM BİLDİRİMİ",
                        _ => "BAKANLIK / ACİL DUYURU"
                    };
                    title.text = document.Title; body.text = document.Body;
                    confirmationLabel.text = document.ButtonText;
                    Canvas.ForceUpdateCanvases();
                    scroll.verticalNormalizedPosition = 1;
                }
                updating=false;
            }
            if (changed || opened)
            {
                if (document.Kind == DocumentKind.Newspaper) newspaper.SelectConfirmation();
                else confirmButton.Select();
            }
        }
        public void Hide()
        {
            if(panel.activeSelf&&visibleKind==DocumentKind.Handbook)ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.BookClose);
            panel.SetActive(false);
        }
        public void ScrollChanged(Vector2 position)
        {
            if(updating||visibleKind!=DocumentKind.Handbook||!panel.activeSelf||Time.unscaledTime-lastScrollSound<.25f)return;
            lastScrollSound=Time.unscaledTime;ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.BookPage);
        }
        public void Confirm() => Confirmed?.Invoke(token);
    }
}
