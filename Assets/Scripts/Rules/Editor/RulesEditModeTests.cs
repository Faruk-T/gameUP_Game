#if UNITY_EDITOR
using NUnit.Framework;

namespace GameUP.Rules.Editor
{
    /// <summary>
    /// Unity Test Runner (Window -> General -> Test Runner -> EditMode) üzerinden
    /// T01–T17 ve P1 kabul testlerinin tamamını doğrulayan NUnit test sınıfı (Bölüm 17).
    /// </summary>
    public class RulesEditModeTests
    {
        [Test]
        public void AllAcceptanceTests_T01_To_T17_And_P1_Pass()
        {
            var results = RulesAcceptanceSuite.RunAllTests(out bool allPassed, out string report);

            foreach (var r in results)
            {
                Assert.IsTrue(r.Passed, $"{r.TestId} ({r.Title}) başarısız oldu: {r.Details}");
            }

            Assert.IsTrue(allPassed, report);
        }
    }
}
#endif

