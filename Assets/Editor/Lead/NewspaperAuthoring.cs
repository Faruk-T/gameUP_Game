using GameUp.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class NewspaperAuthoring
    {
        private static readonly Color Print=new Color(.105f,.085f,.055f);
        internal static NewspaperView Create(Transform overlay,DocumentPresenter presenter)
        {
            var view=overlay.parent.gameObject.AddComponent<NewspaperView>();
            var root=Rect("Newspaper",overlay,0,0,1520,1020);
            root.localRotation=Quaternion.Euler(0,0,.2f);
            var paper=Image("Newsprint",root,0,0,1520,1020,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Documents/NewspaperPaperV2.png"));
            var masthead=Text("Masthead",paper.transform,"GÜNLÜK HABERLER",0,385,1360,120,80,Print,TextAnchor.MiddleCenter,true);masthead.fontStyle=FontStyle.Bold;
            Text("EditionRibbon",paper.transform,"AKDENİZ POSTASI  /  COP31 ANTALYA ÖZEL SAYISI",-345,466,680,26,15,Print,TextAnchor.MiddleLeft);
            Set(view,"dateline",Text("Dateline",paper.transform,"",425,466,500,26,15,Print,TextAnchor.MiddleRight));
            Rule(paper.transform,322,1380,3);Rule(paper.transform,315,1380,1);
            Set(view,"section",Text("Section",paper.transform,"",0,285,1360,30,20,Print));
            Set(view,"headline",Text("Headline",paper.transform,"",0,225,1360,94,40,Print,TextAnchor.UpperLeft,true));
            Set(view,"byline",Text("Byline",paper.transform,"",0,163,1360,30,17,Print));
            Image("PhotoFrame",paper.transform,-370,-40,566,326,Print);
            var photo=Image("PressPhotograph",paper.transform,-370,-40,560,320,Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(NewspaperV2Authoring.CarettaPath));
            photo.preserveAspect=true;
            var caption=Text("PhotoCaption",paper.transform,"",-370,-240,600,44,17,Print);caption.fontStyle=FontStyle.Italic;Set(view,"caption",caption);
            var left=Text("ArticleColumnLeft",paper.transform,"",-370,-370,600,210,22,Print);
            var right=Text("ArticleColumnRight",paper.transform,"",355,15,630,265,24,Print);
            left.font=right.font=Title;left.lineSpacing=right.lineSpacing=1.04f;
            Set(view,"leftColumn",left);Set(view,"rightColumn",right);
            // The rule box is authored geometry; every word inside remains live campaign data.
            Image("TaskBoxTop",paper.transform,355,-139,640,3,Print);Image("TaskBoxBottom",paper.transform,355,-402,640,3,Print);
            Image("TaskBoxLeft",paper.transform,35,-270.5f,3,263,Print);Image("TaskBoxRight",paper.transform,675,-270.5f,3,263,Print);
            Image("TaskHeadingBand",paper.transform,355,-168,620,46,Print);
            Text("BulletinHeading",paper.transform,"BUGÜNÜN GÖREVLERİ",355,-168,590,42,28,Paper,TextAnchor.MiddleCenter,true);
            Set(view,"rules",Text("DailyRules",paper.transform,"",355,-292,590,190,22,Print));
            Text("FictionNotice",paper.transform,"COP31 HAZIRLIK SAYISI  •  KURMACA OYUN GAZETESİ",-365,-478,610,32,15,Print,TextAnchor.MiddleLeft);
            var confirm=Button("FoldNewspaper",paper.transform,"Gazeteyi katla ve göreve başla",355,-456,630,presenter.Confirm);
            confirm.GetComponent<Image>().color=Print;var label=confirm.GetComponentInChildren<Text>();label.color=Paper;label.fontSize=24;
            Set(view,"confirmButton",confirm);Set(view,"page",root.gameObject);
            var days=ProductionProjectSetup.Days();var so=new SerializedObject(view);var editions=so.FindProperty("editions");editions.arraySize=days.Length;
            for(int i=0;i<days.Length;i++)
            {
                var entry=editions.GetArrayElementAtIndex(i);entry.FindPropertyRelative("Source").objectReferenceValue=days[i].MorningNewspaper;
                entry.FindPropertyRelative("IssueNumber").stringValue=(31+i).ToString("000");
                entry.FindPropertyRelative("Section").stringValue=CampaignAuthoring.Sections[i];
                entry.FindPropertyRelative("Byline").stringValue="ANTALYA HABER SERVİSİ  •  Gıda güvenliği ve ortak yaşam alanlarımız";
                entry.FindPropertyRelative("PhotoCaption").stringValue="CARETTALAR İÇİN TEMİZ KIYILAR\nAtığı kaynağında ayır; ortak yaşam alanını koru.";
            }
            so.ApplyModifiedPropertiesWithoutUndo();root.gameObject.SetActive(false);return view;
        }
        private static void Rule(Transform parent,float y,float width,float thickness)=>Image("PrintingRule",parent,0,y,width,thickness,Print);
    }
}
