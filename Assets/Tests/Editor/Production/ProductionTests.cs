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
                        Assert.That(backend.CreateCampaign().Days.Count,Is.EqualTo(3));
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
        [Test]
        public void RearWasteBinsAreReachableAndArtistPrefabsAreConnected()
        {
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath,OpenSceneMode.Additive);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                var camera=all.Select(t=>t.GetComponent<Camera>()).Single(c=>c&&c.CompareTag("MainCamera"));
                var player=camera.transform.parent;
                var bins=all.Select(t=>t.GetComponent<WorkstationTarget>()).Where(t=>t&&t.Action>=WorkstationAction.Paper&&t.Action<=WorkstationAction.Contaminated).ToArray();
                Assert.That(bins.Length,Is.EqualTo(4));Physics.SyncTransforms();
                foreach(var bin in bins)
                {
                    Assert.That(Vector3.Dot(player.forward,bin.transform.position-player.position),Is.LessThan(0),"Bin must be behind the player.");
                    var direction=bin.GetComponent<Collider>().bounds.center-camera.transform.position;
                    Assert.That(direction.magnitude,Is.LessThan(8));
                    Assert.That(Physics.Raycast(camera.transform.position,direction.normalized,out var hit,8,1<<8),Is.True);
                    Assert.That(hit.collider.GetComponent<WorkstationTarget>(),Is.SameAs(bin),"Bin must be targetable from the fixed seat.");
                }
                Assert.That(all.Any(t=>t.name=="CoastalWindow"||t.name=="CoastalWindowFrame"||t.name=="DairySamples"||t.name=="ChilledSamples"||t.name=="ProduceSamples"),Is.False);
                Assert.That(all.Any(t=>t.name=="Fax"||t.name=="FaxLabel"||t.name=="Model_fax"||t.name=="FaxIndicator"),Is.False,"The T report machine and its physical presentation are retired.");
                Assert.That(all.Select(t=>t.GetComponent<WorkstationTarget>()).Count(t=>t&&t.Action==WorkstationAction.Analyzer),Is.EqualTo(1),"The normal F analyzer stays available.");
                foreach(var target in all.Select(t=>t.GetComponent<WorkstationTarget>()).Where(t=>t))Assert.That(Enum.IsDefined(typeof(WorkstationAction),target.Action),Is.True,"No retired interaction IDs may remain in the scene.");
                var dependencies=new HashSet<string>(AssetDatabase.GetDependencies(ProductionProjectSetup.GamePath,true));
                foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler"}))
                    Assert.That(dependencies,Does.Contain(AssetDatabase.GUIDToAssetPath(guid)),"Artist model must have an authored use.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }

        [Test]
        public void CampaignNewsRulesAndUnlocksUseOnlyThe31ExistingModels()
        {
            var campaign=AssetDatabase.LoadAssetAtPath<GameUP.Contracts.CampaignDefinition>(CampaignAuthoring.CatalogPath);
            Assert.That(campaign.Days.Count,Is.EqualTo(3));
            Assert.That(campaign.Days.Select(d=>d.Products.Count),Is.EqualTo(new[]{8,10,13}));
            var unlocked=new HashSet<GameUP.Contracts.ProductCategory>();
            var sources=new HashSet<GameObject>();
            for(int i=0;i<campaign.Days.Count;i++)
            {
                var day=campaign.Days[i];
                foreach(var category in day.CategoryUnlocks)unlocked.Add(category);
                foreach(var product in day.Products)
                {
                    Assert.That(unlocked,Does.Contain(product.Category));
                    Assert.That(product.Category,Is.Not.EqualTo(GameUP.Contracts.ProductCategory.Fish));
                    Assert.That(product.ModelSource,Is.Not.Null);Assert.That(sources.Add(product.ModelSource),Is.True);
                    Assert.That(AssetDatabase.GetAssetPath(product.ModelSource),Does.StartWith("Assets/Art/Urunler/Mesh/"));
                }
                var news=day.MorningNewspaper;
                Assert.That((news.Title+" "+news.Body+" "+news.TaskInstructionBox).ToLowerInvariant(),Does.Not.Contain("balık"));
                Assert.That(news.RelatedRuleIds,Is.EquivalentTo(day.InitialRules.Select(r=>r.RuleId)));
                Assert.That(AssetDatabase.GetAssetPath(news.Illustration),Is.EqualTo("Assets/Art/Documents/Day"+(i+1)+"Products.png"));
            }
            Assert.That(sources.Count,Is.EqualTo(31));
            Assert.That(campaign.Days[0].Products.All(p=>p.Category==GameUP.Contracts.ProductCategory.Dairy||p.Category==GameUP.Contracts.ProductCategory.Meat),Is.True);
            Assert.That(campaign.Days[0].MorningNewspaper.Title.ToLowerInvariant(),Does.Contain("küresel"));
            Assert.That(campaign.Days[0].MorningNewspaper.Body,Does.Not.Contain("Antalya'da artan sıcaklık"));
            var recall=campaign.Days[2].EmergencyEvent;
            Assert.That(recall.TriggerAfterCompletedProducts,Is.EqualTo(3));
            Assert.That(recall.NewRule.RuleType,Is.EqualTo(GameUP.Contracts.RuleType.SourceRegionBan));
            Assert.That(recall.NewRule.ApplicableCategories,Is.EquivalentTo(new[]{GameUP.Contracts.ProductCategory.Dairy,GameUP.Contracts.ProductCategory.Meat,GameUP.Contracts.ProductCategory.Eggs}));
            Assert.That(AssetDatabase.GetDependencies(ProductionProjectSetup.GamePath,true).Any(p=>p.Contains("LegacyContent")||p.Contains("FishTray")),Is.False);
        }
        [Test]
        public void ConditionVariantsKeepOriginalMeshesAndCompileWithoutShaderErrors()
        {
            var shader=Shader.Find("GameUP/FoodCondition");
            Assert.That(shader,Is.Not.Null);
            Assert.That(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),Is.False);
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler"}))
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var original=source.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Distinct().ToArray();
                foreach(GameUP.Contracts.FoodCondition condition in Enum.GetValues(typeof(GameUP.Contracts.FoodCondition)))
                {
                    var variant=AssetDatabase.LoadAssetAtPath<GameObject>(ConditionVariantAuthoring.PathFor(source.name,condition));
                    Assert.That(variant,Is.Not.Null);
                    Assert.That(variant.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Distinct(),Is.EquivalentTo(original));
                    if(condition==GameUP.Contracts.FoodCondition.Spoiled)
                        foreach(var material in variant.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials))
                        {Assert.That(material.shader,Is.SameAs(shader));Assert.That(material.GetFloat("_Spoilage"),Is.EqualTo(1));}
                }
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
            Assert.That(session.IsIntroducing,Is.True);
            Assert.That(session.WorldInputAllowed,Is.False);
            Assert.That(session.Flow.State,Is.EqualTo(FlowState.Boot));
            session.HandleEscape();session.Restart();session.Approve();session.Reject();
            Assert.That(session.IsIntroducing,Is.True,"Escape, restart and decisions cannot skip the opening.");
            Assert.That(session.Documents.IsOpen,Is.False,"The newspaper must wait for the cinematic.");
            var openingFins=Object.FindObjectsByType<ShorelineTurtle>(FindObjectsSortMode.None).SelectMany(t=>t.GetComponentsInChildren<Transform>()).Where(t=>t.name.StartsWith("frontLeg",StringComparison.Ordinal)).ToArray();
            var finRotations=openingFins.Select(t=>t.localRotation).ToArray();
            var finPositions=openingFins.Select(t=>t.localPosition).ToArray();
            float introDeadline=Time.realtimeSinceStartup+OpeningTiming.End+15;
            var introduction=Object.FindFirstObjectByType<IntroCinematic>();
            var openingActors=Object.FindObjectsByType<ShorelineTurtle>(FindObjectsSortMode.None);
            var openingScene=session.gameObject.scene;
            var openingTransforms=openingScene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var openingLogo=openingTransforms.Single(t=>t.name=="COP31RisingLogo").GetComponent<CanvasGroup>();
            var openingFireworks=openingTransforms.Where(t=>t.name.StartsWith("COP31Firework_",StringComparison.Ordinal)).Select(t=>t.GetComponent<ParticleSystem>()).ToArray();
            var openingCamera=Object.FindFirstObjectByType<Camera>();
            var openingCanvas=GameObject.Find("OpeningCanvas").GetComponent<Canvas>();
            var openingNorth=openingTransforms.Single(t=>t.name=="NorthHinge");
            var openingSouth=openingTransforms.Single(t=>t.name=="SouthHinge");
            bool swimmingSeen=false,logoSeen=false,fireworksSeen=false,walkSeen=false;
            while(session.IsIntroducing)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(introDeadline),"Opening must finish naturally.");
                Assert.That(session.WorldInputAllowed,Is.False);
                Assert.That(session.Flow.State,Is.EqualTo(FlowState.Boot));
                if(!swimmingSeen&&introduction.Elapsed>=OpeningTiming.SwimReveal)
                {
                    Assert.That(openingActors.All(t=>t.IsSwimming),Is.True);Assert.That(openingLogo.alpha,Is.LessThan(.01));swimmingSeen=true;
                }
                if(!logoSeen&&introduction.Elapsed>=OpeningTiming.LogoRaised+.2f)
                {
                    Assert.That(swimmingSeen,Is.True);Assert.That(openingLogo.alpha,Is.GreaterThan(.99));Assert.That(openingActors.All(t=>t.IsCruising),Is.True);logoSeen=true;
                }
                if(introduction.Elapsed>=OpeningTiming.LogoStart&&introduction.Elapsed<OpeningTiming.WalkStart)
                    Assert.That(openingLogo.GetComponent<RectTransform>().anchoredPosition.x,Is.EqualTo(0).Within(.1));
                if(introduction.Elapsed<OpeningTiming.DoorsOpenStart)
                    Assert.That(Mathf.DeltaAngle(openingNorth.localEulerAngles.y,90),Is.EqualTo(0).Within(.1));
                if(introduction.Elapsed>=OpeningTiming.DoorsOpenEnd)
                    Assert.That(Mathf.DeltaAngle(openingNorth.localEulerAngles.y,0),Is.EqualTo(0).Within(.1));
                if(!fireworksSeen&&introduction.Elapsed>=46)
                {
                    Assert.That(logoSeen,Is.True);Assert.That(openingFireworks.Where(p=>p.gameObject.activeInHierarchy).Sum(p=>p.particleCount),Is.GreaterThan(0));fireworksSeen=true;
                    ProductionSnapshot.Capture(openingCanvas,openingCamera,"Logs/Visuals/cinematic-logo-fireworks.png",1920,1080);
                }
                if(!walkSeen&&introduction.Elapsed>=OpeningTiming.WalkStart+.3f)
                {
                    Assert.That(fireworksSeen,Is.True);Assert.That(openingLogo.alpha,Is.LessThan(.01));Assert.That(openingFireworks.All(p=>!p.gameObject.activeInHierarchy),Is.True);walkSeen=true;
                }
                yield return null;
            }
            Assert.That(swimmingSeen&&logoSeen&&fireworksSeen&&walkSeen,Is.True,"Opening stages must play in narrative order.");
            yield return null;
            Assert.That(Mathf.DeltaAngle(openingNorth.localEulerAngles.y,0),Is.EqualTo(0).Within(.1),"North door stays open after the cinematic.");
            Assert.That(Mathf.DeltaAngle(openingSouth.localEulerAngles.y,180),Is.EqualTo(0).Within(.1),"South door stays open after the cinematic.");
            foreach(var turtle in Object.FindObjectsByType<ShorelineTurtle>(FindObjectsSortMode.None))
            {
                var animator=turtle.GetComponentInChildren<Animator>();
                Assert.That(animator.GetCurrentAnimatorClipInfoCount(0),Is.EqualTo(1),"Artist animation must actually play.");
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).loop,Is.True);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.GreaterThan(1));
                Assert.That(turtle.IsSwimming,Is.True);
            }
            Assert.That(openingFins.Where((fin,i)=>Quaternion.Angle(fin.localRotation,finRotations[i])>.5f||Vector3.Distance(fin.localPosition,finPositions[i])>.002f).Any(),Is.True,"Imported animation must move the flipper bones.");
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(ApplicationRoot.Require().Settings.Sensitivity,Is.EqualTo(.17f).Within(.001f));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var input=Object.FindFirstObjectByType<WorkstationInteractionController>();
            var pause=Object.FindFirstObjectByType<PauseMenu>();
            var product=Object.FindFirstObjectByType<ProductPresentation>();
            var document=Object.FindFirstObjectByType<DocumentPresenter>();
            Assert.That(input,Is.Not.Null);Assert.That(pause,Is.Not.Null);Assert.That(product,Is.Not.Null);Assert.That(document,Is.Not.Null);
            Assert.That(session.Flow,Is.Not.Null);Assert.That(session.Documents.Current,Is.Not.Null);
            Capture("newspaper",session.gameObject.scene);
            AssertNewspaperReadable();
            Assert.That(session.Documents.Current.RequiresAcknowledgement,Is.True);session.HandleEscape();Assert.That(session.Documents.IsOpen,Is.True);
            ConfirmDocument(session);
            yield return Wait(()=>session.Flow.CanDecide);
            Capture("room-front",session.gameObject.scene);
            var camera=Object.FindFirstObjectByType<Camera>();
            var player=camera.transform.parent;var savedYaw=player.localRotation;var savedPitch=camera.transform.localRotation;
            player.localRotation=Quaternion.Euler(0,180,0);camera.transform.localRotation=Quaternion.Euler(8,0,0);
            Capture("room-rear",session.gameObject.scene);
            player.localRotation=Quaternion.Euler(0,-58,0);camera.transform.localRotation=Quaternion.Euler(-4,0,0);
            Capture("left-poster",session.gameObject.scene);
            var posterTitle=GameObject.Find("InspectionOffice/Room/FoodSecurityPoster/Print/Headline").GetComponent<Text>();
            Assert.That(posterTitle.preferredHeight,Is.LessThanOrEqualTo(posterTitle.rectTransform.rect.height+1));
            Assert.That(posterTitle.cachedTextGenerator.characterCountVisible,Is.GreaterThan(0));
            player.localRotation=savedYaw;camera.transform.localRotation=savedPitch;
            input.ToggleInspection();Assert.That(input.IsInspecting,Is.True);Assert.That(session.ProductInspected,Is.True);Capture("inspection",session.gameObject.scene);input.EndInspection();
            session.OpenHandbook();Assert.That(session.Flow.CanDecide,Is.False);
            Assert.That(GameObject.Find("InspectionOffice/Model_openBook"),Is.Not.Null);Assert.That(GameObject.Find("InspectionOffice/Model_closedBook"),Is.Null);
            Canvas.ForceUpdateCanvases();
            var handbookScroll=GameObject.Find("GameCanvas/DocumentOverlay/DocumentPage/DocumentScroll").GetComponent<ScrollRect>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(handbookScroll.content);
            var handbookBody=handbookScroll.content.GetComponentInChildren<Text>();
            Assert.That(handbookBody.supportRichText,Is.True);
            Assert.That(handbookScroll.content.rect.height,Is.GreaterThan(handbookScroll.viewport.rect.height),"The full manual must scroll.");
            Assert.That(handbookBody.rectTransform.rect.height,Is.GreaterThanOrEqualTo(handbookBody.preferredHeight-1),"Manual text must fit its content rect.");
            var handbookTitle=GameObject.Find("GameCanvas/DocumentOverlay/DocumentPage/DocumentTitle").GetComponent<Text>();
            Assert.That(handbookTitle.preferredHeight,Is.LessThanOrEqualTo(handbookTitle.rectTransform.rect.height+1));
            Capture("handbook",session.gameObject.scene);
            handbookScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
            Capture("handbook-bottom",session.gameObject.scene);document.Confirm();
            Assert.That(GameObject.Find("InspectionOffice/Model_closedBook"),Is.Not.Null);Assert.That(GameObject.Find("InspectionOffice/Model_openBook"),Is.Null);
            session.Analyze();Assert.That(session.Flow.Phase,Is.EqualTo(ProductPhase.Analyzing));
            pause.Open();var position=product.ActiveProduct.transform.position;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Time.timeScale,Is.Zero);Assert.That(session.Flow.Phase,Is.EqualTo(ProductPhase.Analyzing));Assert.That(product.ActiveProduct.transform.position,Is.EqualTo(position));
            Capture("pause",session.gameObject.scene);pause.Resume();
            yield return Wait(()=>session.Documents.IsOpen);Assert.That(session.Documents.Current.Kind,Is.EqualTo(DocumentKind.Analysis));Capture("analysis",session.gameObject.scene);document.Confirm();
            session.Backend.AnalysisSeconds=.04f;
            var speed=new SerializedObject(product);speed.FindProperty("moveDuration").floatValue=.055f;speed.ApplyModifiedPropertiesWithoutUndo();
            yield return Campaign(session,input,"correct");
            Assert.That(session.Flow.RunSummary.Total,Is.EqualTo(31));Assert.That(session.Flow.RunSummary.Correct,Is.EqualTo(31));
            Assert.That(session.Flow.RunSummary.CorrectSorts,Is.EqualTo(15));Assert.That(session.Flow.RunSummary.Score,Is.EqualTo(340));Assert.That(session.Flow.RunSummary.Passed,Is.True);
            Capture("final",session.gameObject.scene);
            var stale=session.Flow.LastOutcome.Decision.Key;
            for(int scenario=0;scenario<2;scenario++)
            {
                session.Restart();Assert.That(session.Flow.RunId,Is.Not.EqualTo(stale.RunId));Assert.That(session.Flow.RunSummary.Total,Is.Zero);
                Assert.That(session.Flow.NotifyArrival(stale),Is.False);Assert.That(input.IsInspecting,Is.False);
                yield return Campaign(session,input,scenario==0?"approve":"reject");
                Assert.That(session.Flow.RunSummary.Accuracy,Is.EqualTo(scenario==0?16f/31:15f/31).Within(.001));Assert.That(session.Flow.RunSummary.Passed,Is.False);
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
            var invalid=new HashSet<string>{"D1-02","D1-03","D1-06","D1-07","D2-02","D2-04","D2-06","D2-08","D2-10","D3-03","D3-04","D3-06","D3-08","D3-10","D3-12"};
            var targets=Object.FindObjectsByType<WorkstationTarget>(FindObjectsSortMode.None).Where(t=>t.Action!=WorkstationAction.Product).ToDictionary(t=>t.Action);
            float deadline=Time.realtimeSinceStartup+90;
            while(session.Flow.State!=FlowState.GameSummary)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Campaign stalled: "+session.Flow.State+"/"+session.Flow.Phase);
                if(session.Documents.IsOpen)
                {
                    if(session.Documents.Current.Kind==DocumentKind.Emergency)
                    {Assert.That(session.Flow.DayIndex,Is.EqualTo(2));Assert.That(session.Flow.ProductIndex,Is.EqualTo(3));if(scenario=="correct")Capture("emergency",session.gameObject.scene);}
                    if(session.Documents.Current.Kind==DocumentKind.Newspaper)
                    {if(scenario=="correct")Capture("newspaper-day"+(session.Flow.DayIndex+1),session.gameObject.scene);AssertNewspaperReadable();}
                    ConfirmDocument(session);
                }
                else if(session.Flow.State==FlowState.DaySummary)session.ContinueSummary();
                else if(session.Flow.CanDecide)
                {
                    string id=session.Flow.Current.Key.ProductId;
                    if(id=="D3-04")Assert.That(session.Flow.Current.Rules.Version,Is.EqualTo(2));
                    if(session.Flow.DayIndex>=1&&session.Flow.LastReport==null)
                    {input.Interact(targets[WorkstationAction.Analyzer]);yield return null;continue;}
                    if(id=="D3-01")Assert.That(session.Flow.Current.Rules.Version,Is.EqualTo(1));
                    if(scenario=="correct"&&(id=="D1-02"||id=="D2-02"||id=="D3-12"))
                    {input.ToggleInspection();Capture("spoiled-"+id,session.gameObject.scene);input.EndInspection();}
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
        private static void ConfirmDocument(GameSessionController session) => ButtonIn(session.Documents.Current.Kind==DocumentKind.Newspaper
            ?"GameCanvas/DocumentOverlay/Newspaper/Newsprint/FoldNewspaper":"GameCanvas/DocumentOverlay/DocumentPage/Confirm").onClick.Invoke();
        private static void AssertNewspaperReadable()
        {
            Canvas.ForceUpdateCanvases();
            var page=GameObject.Find("GameCanvas/DocumentOverlay/Newspaper/Newsprint");
            foreach(var text in page.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),"Clipped newspaper text: "+text.name);
                Assert.That(text.cachedTextGenerator.characterCountVisible,Is.GreaterThan(0),"Empty newspaper print: "+text.name);
            }
        }
        private static void Capture(string name,Scene scene)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Single(c=>c.gameObject.activeInHierarchy&&c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace);
            ProductionSnapshot.Capture(canvas,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).FirstOrDefault(),"Logs/Visuals/"+name+".png",1920,1080);
            if(name=="menu"||name=="newspaper")ProductionSnapshot.Capture(canvas,null,"Logs/Visuals/"+name+"-720.png",1280,720);
        }
    }
}
