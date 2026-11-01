using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameUp.Contracts;
using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    [Serializable]
    public sealed class NewspaperEdition
    {
        public DocumentDefinition Source;
        public string IssueNumber, Section, Byline, PhotoCaption;
    }

    /// <summary>Newspaper composition uses the existing document assets; editorial metadata is authored once.</summary>
    public sealed class NewspaperView : MonoBehaviour
    {
        [SerializeField] private GameObject page;
        [SerializeField] private NewspaperEdition[] editions;
        [SerializeField] private Text dateline, section, headline, byline, leftColumn, rightColumn, caption, rules;
        [SerializeField] private Button confirmButton;
        private readonly Dictionary<DocumentDefinition, NewspaperEdition> index = new Dictionary<DocumentDefinition, NewspaperEdition>();
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        private void Awake()
        {
            foreach (var edition in editions)
                if (!edition.Source || !index.TryAdd(edition.Source, edition))
                    throw new InvalidOperationException("Newspaper editions must reference unique authored documents.");
        }
        public void Show(DayDefinition day, IReadOnlyList<RuleInfo> activeRules)
        {
            if (!index.TryGetValue(day.MorningNewspaper, out var edition))
                throw new InvalidOperationException("Missing newspaper edition: " + day.DayId);
            page.SetActive(true);
            var source = edition.Source;
            var date = day.GameDate;
            dateline.text = new DateTime(date.Year, date.Month, date.Day).ToString("d MMMM yyyy dddd", Turkish).ToUpper(Turkish)
                + "     •     SAYI " + edition.IssueNumber + "     •     ANTALYA BASKISI";
            section.text = edition.Section;
            headline.text = source.Title;
            byline.text = edition.Byline;
            caption.text = edition.PhotoCaption;
            SplitColumns(source.Body, out string first, out string second);
            leftColumn.text = first; rightColumn.text = second;
            var briefing = new StringBuilder();
            foreach (var rule in activeRules) briefing.Append("• ").AppendLine(rule.Text);
            rules.text = briefing.ToString().TrimEnd();
        }
        public void SelectConfirmation() => confirmButton.Select();
        public void Hide() => page.SetActive(false);

        private static void SplitColumns(string body, out string first, out string second)
        {
            // Break only between authored paragraphs; never cut a word or duplicate story text.
            int split = -1, distance = int.MaxValue;
            for (int start = body.IndexOf("\n\n", StringComparison.Ordinal); start >= 0;
                start = body.IndexOf("\n\n", start + 2, StringComparison.Ordinal))
            {
                int candidate = Math.Abs(start - body.Length / 2);
                if (candidate < distance) { split = start; distance = candidate; }
            }
            if (split < 0) throw new InvalidOperationException("Newspaper articles require at least two authored paragraphs.");
            first = body.Substring(0, split).Trim(); second = body.Substring(split + 2).Trim();
        }
    }
}
