using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class GuideBookAuthoring
    {
        internal static void Apply(GameObject canvas)
        {
            var document = canvas.GetComponent<DocumentPresenter>();
            var old = canvas.transform.Find("DocumentOverlay/Handbook"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var previous = canvas.GetComponent<GuideBookView>(); if (previous) UnityEngine.Object.DestroyImmediate(previous);
            var view = canvas.AddComponent<GuideBookView>();
            var panel = Rect("Handbook", canvas.transform.Find("DocumentOverlay"), 0, 0, 1920, 1080);
            var bookObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Book-Page Curl/Example/prefabs/Book.prefab"), panel);
            bookObject.name = "CurlBook";
            var rect = (RectTransform)bookObject.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = new Vector2(0, 20); rect.sizeDelta = new Vector2(1320, 800); rect.localScale = Vector3.one;
            var book = bookObject.GetComponent<Book>(); book.canvas = canvas.GetComponent<Canvas>();
            var blank = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Documents/NewspaperPaperV2.png");
            book.background = blank; book.bookPages = Enumerable.Repeat(blank, 16).ToArray(); book.SetPageCount(16);
            var cover = Image("Cover", panel, 0, 20, 1355, 835, new Color(.35f, .19f, .1f), blank);
            cover.transform.SetAsFirstSibling();
            var bookSettings = new SerializedObject(book); var days = bookSettings.FindProperty("dailyPages"); days.arraySize = 3;
            for (int day = 0; day < 3; day++)
            {
                var item = days.GetArrayElementAtIndex(day); item.FindPropertyRelative("day").intValue = day + 1;
                item.FindPropertyRelative("element1").objectReferenceValue = blank; item.FindPropertyRelative("element2").objectReferenceValue = blank;
            }
            bookSettings.ApplyModifiedPropertiesWithoutUndo();
            var so = new SerializedObject(view); var surfaces = so.FindProperty("surfaces"); surfaces.arraySize = 4;
            var images = new[] { book.Left, book.Right, book.LeftNext, book.RightNext };
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i]; image.sprite = blank; image.color = Color.white;
                var heading = Text("PageHeading", image.transform, "", 0, 323, 550, 65, 34, Ink, TextAnchor.MiddleCenter, true);
                var body = Text("PageBody", image.transform, "", 0, -5, 550, 565, 26, Ink);
                body.resizeTextForBestFit = true; body.resizeTextMinSize = 19; body.resizeTextMaxSize = 26;
                var footer = Text("PageFooter", image.transform, "", 0, -341, 550, 50, 22, Ink, TextAnchor.MiddleCenter);
                var entry = surfaces.GetArrayElementAtIndex(i); entry.FindPropertyRelative("Surface").objectReferenceValue = image;
                entry.FindPropertyRelative("Heading").objectReferenceValue = heading; entry.FindPropertyRelative("Body").objectReferenceValue = body;
                entry.FindPropertyRelative("Footer").objectReferenceValue = footer;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var close = DocumentMenuAuthoring.PaperButton("Close", panel, "Kapat  [B / Esc]", 0, -460, 410, 66, document.Confirm);
            Text("PageHint", panel, "Sayfayı alt köşesinden tutup çevir.", 0, 486, 1100, 60, 28, Paper, TextAnchor.MiddleCenter);
            Set(view, "book", book); Set(view, "panel", panel.gameObject); Set(view, "close", close); Set(document, "handbook", view);
            UnityEventTools.AddPersistentListener(book.OnFlip, view.PageTurned);
            panel.gameObject.SetActive(false); WorkstationUIAuthoring.AddButtonSounds(panel.gameObject);
        }
    }
}
