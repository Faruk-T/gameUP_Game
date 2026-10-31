using GameUp.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class NewspaperAuthoring
    {
        internal static NewspaperView Create(Transform overlay, DocumentPresenter presenter)
        {
            var view = overlay.parent.gameObject.AddComponent<NewspaperView>();
            var root = Rect("Newspaper", overlay, 0, 0, 1500, 940);
            root.localRotation = Quaternion.Euler(0, 0, .35f);
            Image("PaperShadow", root, 10, -10, 1514, 954, new Color(0, 0, 0, .4f));
            var paper = Image("Newsprint", root, 0, 0, 1500, 940, Color.white,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Documents/Newsprint.png"));
            Text("EditionRibbon", paper.transform, "YEREL YAŞAM  /  İKLİM  /  SOFRAMIZ", 0, 435, 1350, 30, 18, Ink, TextAnchor.MiddleCenter);
            Text("Masthead", paper.transform, "AKDENİZ POSTASI", 0, 365, 1380, 150, 72, Ink, TextAnchor.MiddleCenter, true);
            Rule(paper.transform, 320, 1380, 3);
            Set(view, "dateline", Text("Dateline", paper.transform, "", 0, 298, 1380, 30, 19, Ink, TextAnchor.MiddleCenter));
            Rule(paper.transform, 278, 1380, 1);
            Set(view, "section", Text("Section", paper.transform, "", 0, 253, 1380, 30, 19, new Color(.14f, .35f, .32f)));
            Set(view, "headline", Text("Headline", paper.transform, "", 0, 186, 1380, 112, 42, Ink, TextAnchor.UpperLeft, true));
            Set(view, "byline", Text("Byline", paper.transform, "", 0, 112, 1380, 28, 18, Ink));
            var photo = Image("PressPhotograph", paper.transform, -440, -63, 500, 281, Color.white);
            photo.preserveAspect = true; Set(view, "photograph", photo);
            var caption = Text("PhotoCaption", paper.transform, "", -440, -232, 500, 52, 19, Ink);
            caption.fontStyle = FontStyle.Italic; Set(view, "caption", caption);
            var left = Text("ArticleColumnLeft", paper.transform, "", 40, -83, 380, 330, 24, Ink);
            var right = Text("ArticleColumnRight", paper.transform, "", 455, -83, 380, 330, 24, Ink);
            left.font = right.font = Title; left.lineSpacing = right.lineSpacing = 1.06f;
            Set(view, "leftColumn", left); Set(view, "rightColumn", right);
            Image("ColumnRuleLeft", paper.transform, -170, -83, 1, 330, new Color(.15f, .2f, .18f, .3f));
            Image("ColumnRuleRight", paper.transform, 246, -83, 1, 330, new Color(.15f, .2f, .18f, .3f));
            Rule(paper.transform, -270, 1380, 2);
            Text("BulletinHeading", paper.transform, "DENETİM MASASINA NOT  /  BUGÜNÜN KURALLARI", 0, -294, 1380, 30, 19, Ink);
            Set(view, "rules", Text("DailyRules", paper.transform, "", 0, -353, 1380, 92, 20, Ink));
            Rule(paper.transform, -405, 1380, 1);
            Text("FictionNotice", paper.transform, "COP31 HAZIRLIK SAYISI\nKurmaca oyun gazetesi", -500, -439, 350, 42, 16, Ink, TextAnchor.MiddleLeft);
            Text("PageNumber", paper.transform, "01 / YEREL HABER", 540, -439, 300, 35, 17, Ink, TextAnchor.MiddleRight);
            var confirm = Button("FoldNewspaper", paper.transform, "Gazeteyi katla ve göreve başla", 0, -440, 630, presenter.Confirm);
            confirm.GetComponent<Image>().color = new Color(.16f, .31f, .28f);
            var label = confirm.GetComponentInChildren<Text>(); label.color = Paper; label.fontSize = 24;
            Set(view, "confirmButton", confirm); Set(view, "page", root.gameObject);
            var days = ProductionProjectSetup.Days();
            var so = new SerializedObject(view); var editions = so.FindProperty("editions"); editions.arraySize = days.Length;
            for (int i = 0; i < days.Length; i++)
            {
                var entry = editions.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Source").objectReferenceValue = days[i].MorningNewspaper;
                entry.FindPropertyRelative("IssueNumber").stringValue = (31 + i).ToString("000");
                entry.FindPropertyRelative("Section").stringValue = CampaignAuthoring.Sections[i];
                entry.FindPropertyRelative("Byline").stringValue = "ANTALYA HABER SERVİSİ   •   COP31'e hazırlanan kentten";
                entry.FindPropertyRelative("PhotoCaption").stringValue = CampaignAuthoring.Captions[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo(); root.gameObject.SetActive(false); return view;
        }
        private static void Rule(Transform parent, float y, float width, float thickness) =>
            Image("PrintingRule", parent, 0, y, width, thickness, Ink);
    }
}
