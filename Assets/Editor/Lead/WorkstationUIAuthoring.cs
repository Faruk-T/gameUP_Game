using System.Linq;
using GameUp.App;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class WorkstationUIAuthoring
    {
        internal static void Apply(GameObject canvas)
        {
            var card=canvas.transform.Find("ProductCard");if(card)UnityEngine.Object.DestroyImmediate(card.gameObject);
            foreach(var source in canvas.GetComponents<AudioSource>())UnityEngine.Object.DestroyImmediate(source);
            var paper=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Documents/NewspaperPaperV2.png");
            var document=canvas.transform.Find("DocumentOverlay/DocumentPage").GetComponent<Image>();document.sprite=paper;document.color=Color.white;
            var presenter=canvas.GetComponent<DocumentPresenter>();var scroll=document.GetComponentInChildren<ScrollRect>(true);
            if(!Enumerable.Range(0,scroll.onValueChanged.GetPersistentEventCount()).Any(i=>scroll.onValueChanged.GetPersistentTarget(i)==presenter&&scroll.onValueChanged.GetPersistentMethodName(i)==nameof(DocumentPresenter.ScrollChanged)))
                UnityEventTools.AddPersistentListener(scroll.onValueChanged,presenter.ScrollChanged);
            var overlay=canvas.transform.Find("ResultsOverlay");
            var page=overlay.Find("ShiftPaper");
            if(!page)
            {
                page=Image("ShiftPaper",overlay,0,0,1360,960,Color.white,paper).transform;
                foreach(var child in overlay.Cast<Transform>().Where(t=>t!=page).ToArray())child.SetParent(page,false);
                Text("Authority",page,"T.C.  /  BAKANLIK DENETİM BİRİMİ",0,416,1180,42,24,Ink,TextAnchor.MiddleCenter);
                Text("DocumentCaption",page,"ANTALYA GIDA GÜVENLİĞİ TESİSİ  •  VARDİYA KAYDI",0,260,1150,40,22,Ink,TextAnchor.MiddleCenter);
                Image("ReportRule",page,0,225,1080,2,Ink);
                Text("ReportFooter",page,"COP31  /  GÜVENLİ GIDA, SÜRDÜRÜLEBİLİR GELECEK",0,-435,1150,35,20,Ink,TextAnchor.MiddleCenter);
            }
            page.GetComponent<Image>().sprite=paper;page.GetComponent<Image>().color=Color.white;
            foreach(var text in page.GetComponentsInChildren<Text>(true).Where(t=>t.name=="ResultsTitle"||t.name=="ResultsBody"))text.color=Ink;
            var title=page.Find("ResultsTitle").GetComponent<Text>();title.fontSize=48;title.rectTransform.anchoredPosition=new Vector2(0,340);title.rectTransform.sizeDelta=new Vector2(1200,100);
            var body=page.Find("ResultsBody").GetComponent<Text>();body.fontSize=30;body.rectTransform.anchoredPosition=new Vector2(0,-10);body.rectTransform.sizeDelta=new Vector2(1020,420);
            AddButtonSounds(canvas);
        }
        internal static void AddButtonSounds(GameObject root)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true))if(!button.GetComponent<UIButtonSound>())button.gameObject.AddComponent<UIButtonSound>();
        }
    }
}
