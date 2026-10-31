using UnityEditor;
using UnityEngine;

namespace GameUP.Rules.Editor
{
    public static class GameUpRulesEditorTools
    {
        [MenuItem("GameUP32/Validation/Rules acceptance suite")]
        public static void Verify()
        {
            RulesAcceptanceSuite.RunAllTests(out bool passed, out string report);
            if (!passed) throw new System.InvalidOperationException(report);
            Debug.Log(report);
        }
    }
}
