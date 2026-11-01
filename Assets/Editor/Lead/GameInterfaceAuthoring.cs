using System;
using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class GameInterfaceAuthoring
    {
        [MenuItem("GameUP32/Authoring/Refresh handbook layout")]
        public static void RefreshHandbookLayout()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var canvas=scene.GetRootGameObjects().Single(r=>r.name=="GameCanvas");
            var texts=canvas.GetComponentsInChildren<Text>(true);
            ConfigureBody(texts.Single(t=>t.name=="DocumentBody"));
            ConfigureFeedback(texts.Single(t=>t.name=="Feedback"));
            texts.Single(t=>t.name=="ScrollHint").text=ScrollHint;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("MAIN_HANDBOOK_LAYOUT_OK / saved scene preserved / F analysis retained / no T report station");
        }
        private const string ScrollHint="Fare tekerleği ile aşağı/yukarı kaydır";
        private static void ConfigureBody(Text body)
        {
            body.fontSize=26;body.verticalOverflow=VerticalWrapMode.Overflow;body.lineSpacing=1.15f;
        }
        private static void ConfigureFeedback(Text feedback)
        {
            feedback.rectTransform.anchoredPosition=new Vector2(-530,395);
            feedback.rectTransform.sizeDelta=new Vector2(820,88);
        }
        internal static Canvas Create(GameSessionController session)
        {
            var canvas=Canvas("GameCanvas");
            var hud=canvas.gameObject.AddComponent<GameHUD>();Set(session,"hud",hud);
            Image("StatusBar",canvas.transform,0,492,1920,96,new Color(.025f,.08f,.09f,.94f));
            Set(hud,"status",Text("Status",canvas.transform,"",-120,492,1600,70,28,Paper,TextAnchor.MiddleLeft));
            Text("PauseKey",canvas.transform,"ESC / Menü",810,492,250,70,24,Paper,TextAnchor.MiddleRight);
            Text("Reticle",canvas.transform,"·",0,0,30,50,38,Paper,TextAnchor.MiddleCenter);
            Set(hud,"targetHint",Text("Target",canvas.transform,"",0,-350,1600,60,24,Paper,TextAnchor.MiddleCenter));
            Image("InstructionBar",canvas.transform,0,-462,1920,156,new Color(.025f,.08f,.09f,.92f));
            Set(hud,"tutorial",Text("Tutorial",canvas.transform,"",0,-418,1760,60,27,Paper,TextAnchor.MiddleLeft));
            Text("Controls",canvas.transform,"E / İncele     B / Kitapçık     F / Analiz     W / Onay     R / Red     Sol tık / Etkileşim",0,-494,1760,55,24,new Color(.65f,.81f,.77f),TextAnchor.MiddleLeft);
            var feedback=Text("Feedback",canvas.transform,"",0,0,820,88,24,Paper);ConfigureFeedback(feedback);Set(hud,"feedback",feedback);
            var presenter=canvas.gameObject.AddComponent<DocumentPresenter>();Set(session,"documentsView",presenter);
            var overlay=Overlay("DocumentOverlay",canvas.transform,new Color(.015f,.055f,.065f,.86f));Set(presenter,"panel",overlay);
            var page=Image("DocumentPage",overlay.transform,0,0,1360,960,Paper);
            Set(presenter,"documentPage",page.gameObject);
            Set(presenter,"kind",Text("DocumentKind",page.transform,"",0,410,1200,50,24,new Color(.13f,.45f,.4f)));
            Set(presenter,"title",Text("DocumentTitle",page.transform,"",0,320,1200,120,48,Ink,TextAnchor.UpperLeft,true));
            var scroll=Scroll("DocumentScroll",page.transform,0,-15,1220,560,out var content);Set(presenter,"scroll",scroll);
            var body=Text("DocumentBody",content,"",0,0,1160,100,26,Ink);ConfigureBody(body);body.gameObject.AddComponent<LayoutElement>();Set(presenter,"body",body);
            var confirm=Button("Confirm",page.transform,"Devam",0,-390,760,presenter.Confirm);Set(presenter,"confirmButton",confirm);
            Text("ScrollHint",page.transform,ScrollHint,0,-315,1100,38,21,Ink,TextAnchor.MiddleRight);
            Set(presenter,"confirmationLabel",confirm.GetComponentInChildren<Text>());
            Set(presenter,"newspaper",NewspaperAuthoring.Create(overlay.transform,presenter));overlay.SetActive(false);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(scroll.onValueChanged,presenter.ScrollChanged);

            var results=canvas.gameObject.AddComponent<ResultsPresenter>();Set(session,"resultsView",results);
            var resultOverlay=Overlay("ResultsOverlay",canvas.transform,new Color(.02f,.07f,.08f,.96f));Set(results,"panel",resultOverlay);
            Set(results,"title",Text("ResultsTitle",resultOverlay.transform,"",0,360,1400,120,64,Paper,TextAnchor.MiddleCenter,true));
            Set(results,"body",Text("ResultsBody",resultOverlay.transform,"",0,30,750,510,34,Paper));
            var next=Button("Next",resultOverlay.transform,"Ertesi gün",0,-350,600,session.ContinueSummary);
            Set(results,"continueButton",next);Set(results,"continueLabel",next.GetComponentInChildren<Text>());
            Set(results,"restartButton",Button("Restart",resultOverlay.transform,"Yeniden başla",-270,-350,500,session.Restart));
            Set(results,"menuButton",Button("Menu",resultOverlay.transform,"Ana menü",270,-350,500,session.ReturnToMenu));resultOverlay.SetActive(false);

            var pause=canvas.gameObject.AddComponent<PauseMenu>();Set(session,"pause",pause);
            DocumentMenuAuthoring.CreatePause(canvas.gameObject,pause);
            GuideBookAuthoring.Apply(canvas.gameObject);
            WorkstationUIAuthoring.Apply(canvas.gameObject);
            return canvas;
        }
    }
}
