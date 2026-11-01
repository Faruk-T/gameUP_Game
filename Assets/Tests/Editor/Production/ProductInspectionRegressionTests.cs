using System;
using System.Collections.Generic;
using System.Linq;
using GameUp.Editor;
using GameUP.Contracts;
using GameUP.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameUp.Production.Tests
{
    public sealed class ProductInspectionRegressionTests
    {
        private static CampaignDefinition Campaign => AssetDatabase.LoadAssetAtPath<CampaignDefinition>(CampaignAuthoring.CatalogPath);
        // Independent scenario contract: R=reject, S=visible spoilage, E=expired, I=identity, P=pollution, B=recall.
        // Never populated from the evaluator or authoring Issue flags.
        private static readonly string[] Cases = {
            "D1-01:", "D1-02:RS", "D1-03:RE", "D1-04:", "D1-05:", "D1-06:RI", "D1-07:RSE", "D1-08:",
            "D2-01:", "D2-02:RSE", "D2-03:", "D2-04:RP", "D2-05:", "D2-06:RS", "D2-07:", "D2-08:RE", "D2-09:", "D2-10:RP",
            "D3-01:", "D3-02:", "D3-03:RP", "D3-04:RB", "D3-05:", "D3-06:RS", "D3-07:", "D3-08:RE", "D3-09:", "D3-10:RP", "D3-11:", "D3-12:RSE", "D3-13:"
        };
        [TestCaseSource(nameof(Cases))]
        public void EveryProductReportAndBothDecisionsMatchItsAuthoredScenario(string scenario)
        {
            var parts = scenario.Split(':'); string id = parts[0], issues = parts[1];
            var day = Campaign.Days.Single(d => d.Products.Any(p => p.DefinitionId == id));
            var product = day.Products.Single(p => p.DefinitionId == id);
            int index = day.Products.ToList().IndexOf(product);
            var snapshot = CampaignInspectionAudit.Snapshot(day, index);
            var report = new ProductInspectionReport(product, snapshot);
            Assert.That(report.EnvironmentalPositive, Is.EqualTo(issues.Contains("P")), id);
            Assert.That(report.PhysicalSuitable, Is.EqualTo(!issues.Contains("S")), id);
            Assert.That(report.IdentityVerified, Is.EqualTo(!issues.Contains("I")), id);
            Assert.That(report.Expired, Is.EqualTo(issues.Contains("E")), id);
            var expected = issues.Contains("R") ? ProductDecision.Reject : ProductDecision.Approve;
            Assert.That(report.Evaluation.ExpectedDecision, Is.EqualTo(expected));
            foreach (ProductDecision choice in Enum.GetValues(typeof(ProductDecision)))
            {
                var verdict = ProductRuleEvaluator.EvaluateDecision("regression", id, product, snapshot, choice);
                Assert.That(verdict.IsCorrect, Is.EqualTo(choice == expected));
            }
            Assert.That(report.Text, Does.Contain(product.DefinitionId).And.Contain(product.DisplayName).And.Contain(product.ProductionPlace));
            Assert.That(report.Text, Does.Contain("ÇEVRESEL KALINTI TESTİ: " + (issues.Contains("P") ? "POZİTİF" : "NEGATİF")));
            Assert.That(report.Text, Does.Contain("DENETİM SONUCU: " + (issues.Contains("R") ? "UYGUNSUZ / RED" : "UYGUN / ONAY")));
            Assert.That(report.Text, Does.Contain("SKT DURUMU: " + (issues.Contains("E") ? "GEÇMİŞ" : product.ExpiryDate.Equals(day.GameDate) ? "BUGÜN SON GÜN" : "GEÇERLİ")));
            Assert.That(report.Text, Does.Not.Contain("şişme").Or.Contain("UYGUNSUZ"));
            Assert.That(product.Prefab.GetComponent<ProductWorldLabelBinder>().PrintedLabelSignature, Is.EqualTo(ProductWorldLabelBinder.LabelSignature(product)));
        }
        [Test]
        public void SameDayYogurtAndExpiredCheeseCannotBeMistakenForEachOther()
        {
            var day = Campaign.Days[0];
            var yogurt = new ProductInspectionReport(day.Products[3], CampaignInspectionAudit.Snapshot(day, 3));
            var cheese = new ProductInspectionReport(day.Products[2], CampaignInspectionAudit.Snapshot(day, 2));
            Assert.That(yogurt.Text, Does.Contain("01.11.2026").And.Contain("BUGÜN SON GÜN"));
            Assert.That(yogurt.Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Approve));
            Assert.That(cheese.EnvironmentalPositive, Is.False); Assert.That(cheese.PhysicalSuitable, Is.True);
            Assert.That(cheese.Expired, Is.True); Assert.That(cheese.Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Reject));
            Assert.That(cheese.Text, Does.Not.Contain("şişme").And.Not.Contain("küf"));
        }
        [Test]
        public void RecallDistinguishesHealthFromPermissionAndKeepsOldSnapshots()
        {
            var day = Campaign.Days[2]; var cheese = day.Products[3];
            var before = CampaignInspectionAudit.Snapshot(day, 3, false);
            var after = CampaignInspectionAudit.Snapshot(day, 3, true);
            Assert.That(new ProductInspectionReport(cheese, before).Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Approve));
            var report = new ProductInspectionReport(cheese, after);
            Assert.That(report.EnvironmentalPositive, Is.False); Assert.That(report.PhysicalSuitable, Is.True);
            Assert.That(report.Text, Does.Contain(RuleIds.BatchRecall));
            Assert.That(report.Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Reject));
            Assert.That(new ProductInspectionReport(day.Products[4], after).Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Approve), "B fruit is exempt.");
            Assert.That(new ProductInspectionReport(cheese, before).Evaluation.ExpectedDecision, Is.EqualTo(ProductDecision.Approve), "Old snapshot must not change.");
        }
        [Test]
        public void AllProductReportsAreDistinctAndSavedReportProseCannotOverrideFacts()
        {
            var reports = new HashSet<string>();
            foreach (var day in Campaign.Days)
            for (int i = 0; i < day.Products.Count; i++) Assert.That(reports.Add(new ProductInspectionReport(day.Products[i], CampaignInspectionAudit.Snapshot(day, i)).Text), Is.True);
            var original = Campaign.Days[0].Products[1]; var clone = UnityEngine.Object.Instantiate(original);
            try
            {
                var so = new SerializedObject(clone); so.FindProperty("analysisFindingDetail").stringValue = "HER ŞEY UYGUN — ONAY"; so.ApplyModifiedPropertiesWithoutUndo();
                var report = new ProductInspectionReport(clone, CampaignInspectionAudit.Snapshot(Campaign.Days[0], 1));
                Assert.That(report.PhysicalSuitable, Is.False); Assert.That(report.Text, Does.Not.Contain("HER ŞEY UYGUN"));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }
        [Test]
        public void EditorAuditChecksReportsAllSortingChoicesAndCorrectPlayWithOrWithoutAnalysis()
        {
            var result = CampaignInspectionAudit.Run();
            Assert.That(result.Rows.Count, Is.EqualTo(31)); Assert.That(result.Checks, Is.GreaterThan(1300));
            Assert.That(result.Errors, Is.Empty, string.Join("\n", result.Errors));
        }
        [Test]
        public void SuppliedModelsKeepTheirDecisionActionsAndRecyclingTargets()
        {
            var scene = EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath, OpenSceneMode.Additive);
            try
            {
                var office = scene.GetRootGameObjects().Single(r => r.name == "InspectionOffice").transform;
                var deskRenderers = office.Find("Prop_desk").GetComponentsInChildren<Renderer>(); var desk = deskRenderers[0].bounds;
                foreach (var renderer in deskRenderers.Skip(1)) desk.Encapsulate(renderer.bounds);
                foreach (var spec in new[] { ("Approve", GameUP.Interaction.WorkstationAction.Approve), ("Reject", GameUP.Interaction.WorkstationAction.Reject) })
                {
                    var target = office.Find(spec.Item1); Assert.That(target.GetComponent<GameUP.Interaction.WorkstationTarget>().Action, Is.EqualTo(spec.Item2));
                    Assert.That(target.GetComponent<Collider>(), Is.Not.Null);
                    var parts = target.Find("ArtistButton").GetComponentsInChildren<MeshFilter>(); Assert.That(parts.Length, Is.EqualTo(2));
                    foreach (var part in parts) Assert.That(AssetDatabase.GetAssetPath(part.sharedMesh), Is.EqualTo("Assets/Art/Environment/Mesh/buttons.fbx"));
                    foreach (var renderer in target.Find("ArtistButton").GetComponentsInChildren<Renderer>())
                    {
                        Assert.That(renderer.bounds.max.x, Is.LessThan(desk.max.x));
                        Assert.That(renderer.bounds.min.x, Is.GreaterThan(desk.min.x));
                        Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(desk.max.y - .03f));
                    }
                }
                foreach (var bin in office.Find("RecyclingStation").Cast<Transform>().Where(t => t.name.StartsWith("Waste_", StringComparison.Ordinal)))
                {
                    Assert.That(bin.GetComponent<Collider>(), Is.Not.Null); Assert.That(bin.GetComponent<GameUP.Interaction.WorkstationTarget>(), Is.Not.Null);
                    foreach (var filter in bin.GetComponentsInChildren<MeshFilter>()) Assert.That(AssetDatabase.GetAssetPath(filter.sharedMesh), Is.EqualTo("Assets/Art/Environment/Mesh/Trashbin.fbx"));
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        [TestCase(29, 2, 2024, true)]
        [TestCase(29, 2, 2026, false)]
        [TestCase(31, 11, 2026, false)]
        [TestCase(31, 12, 2026, true)]
        public void ExpiryDatesRespectRealCalendarBoundaries(int day, int month, int year, bool valid)
            => Assert.That(new GameDate(day, month, year).IsValid, Is.EqualTo(valid));
    }
}
