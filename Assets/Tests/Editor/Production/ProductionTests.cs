using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameUp.App;
using GameUp.Contracts;
using GameUp.Editor;
using GameUp.Gameplay;
using GameUP.Interaction;
using GameUP.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace GameUp.Production.Tests
{
    public sealed class ProductionTests
    {
        [Test]
        public void BuildContainsExactlyTheThreeAuthoredScenes()
        {
            Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path),Is.EqualTo(new[]{ProductionProjectSetup.FirstPath,ProductionProjectSetup.MenuPath,ProductionProjectSetup.GamePath}));
            Assert.That(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),Is.EqualTo(ProductionProjectSetup.FirstPath));
        }
        [Test]
        public void EverySceneReferenceAndProductPrefabIsAuthored()
        {
            foreach(var path in new[]{ProductionProjectSetup.FirstPath,ProductionProjectSetup.MenuPath,ProductionProjectSetup.GamePath})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
                    {
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,t.name);
                        foreach(var component in t.GetComponents<MonoBehaviour>())
                        {
                            if(!component || !component.GetType().Assembly.GetName().Name.StartsWith("GameUp."))continue;
                            var so=new SerializedObject(component);var p=so.GetIterator();
                            while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference)
                                Assert.That(p.objectReferenceValue,Is.Not.Null,component.GetType().Name+"."+p.propertyPath);
                        }
                    }
                    if(path==ProductionProjectSetup.GamePath)
                    {
                        var backend=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ProductionInspectionBackend>(true)).Single();
                        Assert.That(backend.CreateCampaign().Days.Count,Is.EqualTo(2));
                        foreach(var day in backend.ConfiguredDays)
                        {
                            Assert.That(day.MorningNewspaper,Is.Not.Null);Assert.That(day.MorningNewspaper.Illustration,Is.Not.Null);
                            foreach(var item in day.Products)
                            {Assert.That(item.Prefab,Is.Not.Null);Assert.That(item.Prefab.GetComponent<ProductWorldLabelBinder>().ProductDefinition,Is.SameAs(item));}
                        }
                    }
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
        }
        [UnityTest]
        public IEnumerator FirstMenuGameCampaignRestartAndReturnUseRealProductionPorts()
        {
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            yield return new EnterPlayMode();
            // Construct captured locals after domain reload; EnterPlayMode cannot serialize closures.
            yield return ExerciseProduction();
            yield return new ExitPlayMode();
        }
        private static IEnumerator ExerciseProduction()
        {
            yield return Wait(()=>SceneManager.GetActiveScene().name==ApplicationRoot.MenuScene&&!ApplicationRoot.Require().IsLoading);
            Assert.That(Object.FindObjectsByType<ApplicationRoot>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Capture("menu",Object.FindFirstObjectByType<MenuController>().gameObject.scene);
            var menu=Object.FindFirstObjectByType<MenuController>();menu.ShowSettings();
            var settings=Object.FindFirstObjectByType<SettingsPanel>();
            Capture("settings",menu.gameObject.scene);
            float initialSensitivity=ApplicationRoot.Require().Settings.Sensitivity;
            settings.SetSensitivity(.17f);settings.Close();menu.ShowHelp();Capture("help",menu.gameObject.scene);menu.ShowHome();
            menu.ShowAbout();Capture("about",menu.gameObject.scene);menu.ShowHome();
            var start=ButtonIn("MenuCanvas/Home/Start");
            Assert.That(start,Is.Not.Null,"Authored start button must exist.");
            start.onClick.Invoke();
            yield return Wait(()=>SceneManager.GetActiveScene().name==ApplicationRoot.GameScene&&!ApplicationRoot.Require().IsLoading);
            yield return null;
            var session=Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(session,Is.Not.Null,"GameSystems must be active in Game.");
            Assert.That(ApplicationRoot.Require().Settings.Sensitivity,Is.EqualTo(.17f).Within(.001f));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var input=Object.FindFirstObjectByType<WorkstationInteractionController>();
            var pause=Object.FindFirstObjectByType<PauseMenu>();
            var product=Object.FindFirstObjectByType<ProductPresentation>();
            var document=Object.FindFirstObjectByType<DocumentPresenter>();
            Assert.That(input,Is.Not.Null);Assert.That(pause,Is.Not.Null);Assert.That(product,Is.Not.Null);Assert.That(document,Is.Not.Null);
            Assert.That(session.Flow,Is.Not.Null);Assert.That(session.Documents.Current,Is.Not.Null);
            Capture("newspaper",session.gameObject.scene);
            Assert.That(session.Documents.Current.RequiresAcknowledgement,Is.True);session.HandleEscape();Assert.That(session.Documents.IsOpen,Is.True);
            document.Confirm();
            yield return Wait(()=>session.Flow.CanDecide);
            input.ToggleInspection();Assert.That(input.IsInspecting,Is.True);Assert.That(session.ProductInspected,Is.True);Capture("inspection",session.gameObject.scene);input.EndInspection();
            session.OpenHandbook();Assert.That(session.Flow.CanDecide,Is.False);Capture("handbook",session.gameObject.scene);document.Confirm();
            session.Analyze();Assert.That(session.Flow.Phase,Is.EqualTo(ProductPhase.Analyzing));
            pause.Open();var position=product.ActiveProduct.transform.position;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Time.timeScale,Is.Zero);Assert.That(session.Flow.Phase,Is.EqualTo(ProductPhase.Analyzing));Assert.That(product.ActiveProduct.transform.position,Is.EqualTo(position));
            Capture("pause",session.gameObject.scene);pause.Resume();
            yield return Wait(()=>session.Documents.IsOpen);Assert.That(session.Documents.Current.Kind,Is.EqualTo(DocumentKind.Analysis));Capture("analysis",session.gameObject.scene);document.Confirm();
            session.Backend.AnalysisSeconds=.04f;
            yield return Campaign(session,input,"correct");
            Assert.That(session.Flow.RunSummary.Total,Is.EqualTo(12));Assert.That(session.Flow.RunSummary.Correct,Is.EqualTo(12));
            Assert.That(session.Flow.RunSummary.CorrectSorts,Is.EqualTo(6));Assert.That(session.Flow.RunSummary.Score,Is.EqualTo(132));Assert.That(session.Flow.RunSummary.Passed,Is.True);
            Capture("final",session.gameObject.scene);
            var stale=session.Flow.LastOutcome.Decision.Key;
            for(int scenario=0;scenario<2;scenario++)
            {
                session.Restart();Assert.That(session.Flow.RunId,Is.Not.EqualTo(stale.RunId));Assert.That(session.Flow.RunSummary.Total,Is.Zero);
                Assert.That(session.Flow.NotifyArrival(stale),Is.False);Assert.That(input.IsInspecting,Is.False);
                yield return Campaign(session,input,scenario==0?"approve":"reject");
                Assert.That(session.Flow.RunSummary.Accuracy,Is.EqualTo(.5f).Within(.001));Assert.That(session.Flow.RunSummary.Passed,Is.False);
            }
            session.Restart();document.Confirm();yield return Wait(()=>session.Flow.CanDecide);
            session.Analyze();session.Restart();yield return new WaitForSecondsRealtime(.15f);
            Assert.That(session.Documents.Current.Kind,Is.EqualTo(DocumentKind.Newspaper));Assert.That(session.Flow.LastReport,Is.Null);
            ApplicationRoot.Require().Settings.SetSensitivity(initialSensitivity);ApplicationRoot.Require().Settings.Save();
            session.ReturnToMenu();yield return Wait(()=>SceneManager.GetActiveScene().name==ApplicationRoot.MenuScene&&!ApplicationRoot.Require().IsLoading);
            Assert.That(Object.FindObjectsByType<ApplicationRoot>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        private static IEnumerator Campaign(GameSessionController session,WorkstationInteractionController input,string scenario)
        {
            var invalid=new HashSet<string>{"D1-02","D1-05","D1-06","D2-02","D2-04","D2-06"};
            var targets=Object.FindObjectsByType<WorkstationTarget>(FindObjectsSortMode.None).Where(t=>t.Action!=WorkstationAction.Product).ToDictionary(t=>t.Action);
            float deadline=Time.realtimeSinceStartup+90;
            while(session.Flow.State!=FlowState.GameSummary)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Campaign stalled: "+session.Flow.State+"/"+session.Flow.Phase);
                if(session.Documents.IsOpen)
                {
                    if(session.Documents.Current.Kind==DocumentKind.Emergency)
                    {Assert.That(session.Flow.ProductIndex,Is.EqualTo(3));Capture("emergency",session.gameObject.scene);}
                    Object.FindFirstObjectByType<DocumentPresenter>().Confirm();
                }
                else if(session.Flow.State==FlowState.DaySummary)session.ContinueSummary();
                else if(session.Flow.CanDecide)
                {
                    string id=session.Flow.Current.Key.ProductId;
                    if(id=="D2-04")Assert.That(session.Flow.Current.Rules.Version,Is.EqualTo(2));
                    if(session.Flow.DayIndex==1&&session.Flow.LastReport==null)
                    {input.Interact(targets[WorkstationAction.Analyzer]);yield return null;continue;}
                    bool reject=scenario=="reject"||scenario=="correct"&&invalid.Contains(id);
                    input.Interact(targets[reject?WorkstationAction.Reject:WorkstationAction.Approve]);
                }
                else if(session.Flow.CanSort)
                {
                    var definition=session.Backend.GetProductDefinition(session.Flow.Current.Product.Id);
                    var bin=ProductRuleEvaluator.GetExpectedWasteBin(definition);
                    input.Interact(targets[(WorkstationAction)((int)WorkstationAction.Paper+(int)bin-1)]);
                }
                yield return null;
            }
        }
        private static IEnumerator Wait(Func<bool> condition)
        {float deadline=Time.realtimeSinceStartup+25;while(!condition()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Timed out waiting for production flow.");yield return null;}}
        private static Button ButtonIn(string path)
        {
            var go=GameObject.Find(path);
            if(!go)throw new InvalidOperationException("Missing button " + path + " in " + SceneManager.GetActiveScene().name);
            return go.GetComponent<Button>();
        }
        private static void Capture(string name,Scene scene)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Single(c=>c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace);
            ProductionSnapshot.Capture(canvas,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).FirstOrDefault(),"Logs/Visuals/"+name+".png",1920,1080);
            if(name=="menu"||name=="newspaper")ProductionSnapshot.Capture(canvas,null,"Logs/Visuals/"+name+"-720.png",1280,720);
        }
    }
}
