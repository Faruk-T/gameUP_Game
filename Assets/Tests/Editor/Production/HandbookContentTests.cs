using GameUp.Contracts;
using GameUp.Editor;
using GameUp.Gameplay;
using GameUp.Lead;
using GameUP.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameUp.Production.Tests
{
    public sealed class HandbookContentTests
    {
        private GameObject host;
        private DayFlowSession flow;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("HandbookTestBackend");
            var backend = host.AddComponent<ProductionInspectionBackend>();
            backend.SetConfiguredDays(AssetDatabase.LoadAssetAtPath<GameUP.Contracts.CampaignDefinition>(CampaignAuthoring.CatalogPath).Days);
            flow = new DayFlowSession(backend.CreateCampaign(), backend);
            flow.StartNewRun();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ManualFollowsTheCurrentDayWithoutAnnouncingAnInactiveRecall(int day)
        {
            for (int i = 0; i < day; i++)
            {
                flow.AcknowledgeMorning();
                while (flow.State == FlowState.Working) CompleteNext();
                Assert.That(flow.ContinueAfterSummary(), Is.True);
            }
            string text = HandbookContent.Build(flow);
            Assert.That(text, Does.Contain(flow.Day.Date.ToString("dd.MM.yyyy")));
            Assert.That(text, Does.Contain(flow.Day.Title));
            Assert.That(text, Does.Contain(flow.Day.Newspaper));
            foreach (var product in flow.Day.Products) Assert.That(text, Does.Contain(product.Category));
            foreach (var rule in flow.CurrentRules) Assert.That(text, Does.Contain(rule.Text));
            Assert.That(text, Does.Not.Contain("AKTİF BAKANLIK ACİL FAKSI"));
            Assert.That(text, Does.Contain("Doğru ayrıştırma +2").And.Contain("Hatalı red -10").And.Contain("Yanlış kutu -2"));
            Assert.That(text, Does.Contain("Görünür bozulma bulunan veya analizde POZİTİF"));
            Assert.That(text, Does.Contain("F veya analiz cihazı").And.Not.Contain("T /"));
        }

        [Test]
        public void RecallAppearsOnlyAfterAcknowledgementAndClearsOnRestart()
        {
            for (int day = 0; day < 2; day++)
            {
                flow.AcknowledgeMorning();
                while (flow.State == FlowState.Working) CompleteNext();
                flow.ContinueAfterSummary();
            }
            flow.AcknowledgeMorning();
            while (flow.State == FlowState.Working) CompleteNext();
            Assert.That(flow.State, Is.EqualTo(FlowState.EmergencyBriefing));
            Assert.That(HandbookContent.Build(flow), Does.Not.Contain("AKTİF BAKANLIK ACİL FAKSI"));
            Assert.That(flow.AcknowledgeEmergency(), Is.True);
            string active = HandbookContent.Build(flow);
            Assert.That(active, Does.Contain("AKTİF BAKANLIK ACİL FAKSI").And.Contain(flow.Day.Emergency.Text));
            Assert.That(active, Does.Contain(flow.Day.Emergency.AdditionalRule.Text));
            flow.StartNewRun();
            Assert.That(HandbookContent.Build(flow), Does.Not.Contain("AKTİF BAKANLIK ACİL FAKSI"));
        }

        private void CompleteNext()
        {
            var key = flow.Current.Key;
            Assert.That(flow.NotifyArrival(key), Is.True);
            Assert.That(flow.SubmitDecision(key, PlayerDecision.Approve), Is.True);
            Assert.That(flow.NotifyDispositionFinished(key), Is.True);
        }
    }
}
