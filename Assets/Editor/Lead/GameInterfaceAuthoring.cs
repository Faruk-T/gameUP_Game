using GameUp.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class GameInterfaceAuthoring
    {
        internal static void Create(GameSessionController session)
        {
            var canvas=Canvas("GameCanvas");
            var hud=canvas.gameObject.AddComponent<GameHUD>();Set(session,"hud",hud);
            Image("StatusBar",canvas.transform,0,492,1920,96,new Color(.025f,.08f,.09f,.94f));
            Set(hud,"status",Text("Status",canvas.transform,"",-120,492,1600,70,28,Paper,TextAnchor.MiddleLeft));
            Text("PauseKey",canvas.transform,"ESC / Menü",810,492,250,70,24,Paper,TextAnchor.MiddleRight);
            Text("Reticle",canvas.transform,"·",0,0,30,50,38,Paper,TextAnchor.MiddleCenter);
            Set(hud,"targetHint",Text("Target",canvas.transform,"",0,-70,1000,70,28,Paper,TextAnchor.MiddleCenter));
            Image("InstructionBar",canvas.transform,0,-462,1920,156,new Color(.025f,.08f,.09f,.92f));
            Set(hud,"tutorial",Text("Tutorial",canvas.transform,"",0,-418,1760,60,27,Paper,TextAnchor.MiddleLeft));
            Text("Controls",canvas.transform,"E / İncele     B / Kitapçık     F / Analiz     T / Rapor     W / Onay     R / Red     Sol tık / Etkileşim",0,-494,1760,55,24,new Color(.65f,.81f,.77f),TextAnchor.MiddleLeft);
            Set(hud,"feedback",Text("Feedback",canvas.transform,"",-530,395,820,88,24,Paper));
            var card=Image("ProductCard",canvas.transform,670,60,500,730,new Color(.97f,.95f,.88f,.98f));
            Set(hud,"productCard",card.gameObject);
            Set(hud,"productTitle",Text("ProductTitle",card.transform,"",0,285,430,120,32,Ink,TextAnchor.UpperLeft,true));
            var productScroll=Scroll("ProductLabelScroll",card.transform,0,-50,450,530,out var productContent);
            var label=Text("ProductLabel",productContent,"",0,0,430,530,25,Ink);
            label.gameObject.AddComponent<LayoutElement>();Set(hud,"productLabel",label);card.gameObject.SetActive(false);

            var presenter=canvas.gameObject.AddComponent<DocumentPresenter>();Set(session,"documentsView",presenter);
            var overlay=Overlay("DocumentOverlay",canvas.transform,new Color(.015f,.055f,.065f,.86f));Set(presenter,"panel",overlay);
            var page=Image("DocumentPage",overlay.transform,0,0,1360,960,Paper);
            Set(presenter,"documentPage",page.gameObject);
            Set(presenter,"kind",Text("DocumentKind",page.transform,"",0,410,1200,50,24,new Color(.13f,.45f,.4f)));
            Set(presenter,"title",Text("DocumentTitle",page.transform,"",0,320,1200,120,48,Ink,TextAnchor.UpperLeft,true));
            var scroll=Scroll("DocumentScroll",page.transform,0,-15,1220,560,out var content);Set(presenter,"scroll",scroll);
            var body=Text("DocumentBody",content,"",0,0,1160,100,26,Ink);body.verticalOverflow=VerticalWrapMode.Overflow;body.lineSpacing=1.15f;body.gameObject.AddComponent<LayoutElement>();Set(presenter,"body",body);
            var confirm=Button("Confirm",page.transform,"Devam",0,-390,760,presenter.Confirm);Set(presenter,"confirmButton",confirm);
            Text("ScrollHint",page.transform,"Fare tekerleği ile aşağı/yukarı kaydır",0,-315,1100,38,21,Ink,TextAnchor.MiddleRight);
            Set(presenter,"confirmationLabel",confirm.GetComponentInChildren<Text>());
            Set(presenter,"newspaper",NewspaperAuthoring.Create(overlay.transform,presenter));overlay.SetActive(false);

            var results=canvas.gameObject.AddComponent<ResultsPresenter>();Set(session,"resultsView",results);
            var resultOverlay=Overlay("ResultsOverlay",canvas.transform,new Color(.02f,.07f,.08f,.96f));Set(results,"panel",resultOverlay);
            Set(results,"title",Text("ResultsTitle",resultOverlay.transform,"",0,360,1400,120,64,Paper,TextAnchor.MiddleCenter,true));
            Set(results,"body",Text("ResultsBody",resultOverlay.transform,"",0,30,750,510,34,Paper));
            var next=Button("Next",resultOverlay.transform,"Ertesi gün",0,-350,600,session.ContinueSummary);
            Set(results,"continueButton",next);Set(results,"continueLabel",next.GetComponentInChildren<Text>());
            Set(results,"restartButton",Button("Restart",resultOverlay.transform,"Yeniden başla",-270,-350,500,session.Restart));
            Set(results,"menuButton",Button("Menu",resultOverlay.transform,"Ana menü",270,-350,500,session.ReturnToMenu));resultOverlay.SetActive(false);

            var pause=canvas.gameObject.AddComponent<PauseMenu>();Set(session,"pause",pause);
            var pauseOverlay=Overlay("PauseOverlay",canvas.transform,new Color(.025f,.08f,.09f,.96f));Set(pause,"overlay",pauseOverlay);
            var home=Rect("PauseHome",pauseOverlay.transform,0,0,1920,1080);Stretch(home);Set(pause,"home",home.gameObject);
            Text("Title",home,"Görev duraklatıldı",0,280,1200,140,70,Paper,TextAnchor.MiddleCenter,true);
            Set(pause,"resume",Button("Resume",home,"Devam et",0,70,550,pause.Resume));
            Button("Settings",home,"Ayarlar",0,-30,550,pause.ShowSettings);
            Button("Menu",home,"Ana menü",0,-130,550,pause.ReturnToMenu);
            Text("PauseHint",home,"Analiz ve ürün hareketi, devam ettiğinde sürer.",0,-290,1000,80,28,Paper,TextAnchor.MiddleCenter);
            Set(pause,"settings",Settings(pauseOverlay.transform));pauseOverlay.SetActive(false);
        }
    }
}
