using System;
using System.Collections.Generic;
using System.Text;
using GameUp.App;
using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    [Serializable]
    public sealed class GuidePageView
    {
        public Image Surface;
        public Text Heading, Body, Footer;
    }
    // The session supplies the real manual. The book owns only its page-curl presentation.
    public sealed class GuideBookView : MonoBehaviour
    {
        [SerializeField] private Book book;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button close;
        [SerializeField] private GuidePageView[] surfaces;
        private readonly List<string> pages = new List<string>();
        private string dateline;
        public int CurrentDay => book.CurrentDay;
        public Book Book => book;
        public IReadOnlyList<string> Pages => pages;
        private void Awake() => book.PageDisplayed += RenderPage;
        private void OnDestroy() { if (book) book.PageDisplayed -= RenderPage; }
        public void Show(string content, DayDefinition day)
        {
            pages.Clear(); var page = new StringBuilder();
            foreach (string line in content.Split('\n'))
            {
                if (page.Length > 0 && (page.Length + line.Length > 780 || line.StartsWith("<b><color=", StringComparison.Ordinal) && page.Length > 300))
                { pages.Add(page.ToString().Trim()); page.Clear(); }
                page.AppendLine(line);
            }
            if (page.Length > 0) pages.Add(page.ToString().Trim());
            dateline = "GÜN " + day.DayNumber + "  •  " + day.GameDate;
            book.SetPageCount(pages.Count);
            if (!book.TrySetDay(day.DayNumber)) throw new InvalidOperationException("Book has no pages for the actual campaign day.");
            panel.SetActive(true); book.ResetBook(); close.Select();
        }
        private void RenderPage(Image image, int index)
        {
            foreach (var surface in surfaces)
            {
                if (surface.Surface != image) continue;
                bool valid = index >= 0 && index < pages.Count;
                surface.Heading.text = valid ? "DENETÇİ EL KİTABI" : "";
                surface.Body.text = valid ? pages[index] : "";
                surface.Footer.text = valid ? dateline + "  /  " + (index + 1) + "–" + pages.Count : "";
                return;
            }
        }
        public void Hide() => panel.SetActive(false);
        public void PageTurned() => ApplicationRoot.Require().Audio.PlayCue(GameAudioCue.BookPage);
    }
}
