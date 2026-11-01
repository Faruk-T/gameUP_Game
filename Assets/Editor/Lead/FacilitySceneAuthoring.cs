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
    /// <summary>Refreshes owned details in the saved scene without rebuilding the user's room.</summary>
    public static class FacilitySceneAuthoring
    {
        [MenuItem("GameUP32/Authoring/Refresh signs, portraits and artist props")]
        public static void RefreshDetails()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);
            var office=scene.GetRootGameObjects().Single(g=>g.name=="InspectionOffice").transform;
            foreach(var name in new[]{"AtaturkPortrait","PresidentPortrait"})
            {
                var print=(RectTransform)office.Find("Room/OfficeIdentity/"+name+"/PortraitPrint");
                var mat=print.Find("Mat");if(mat)UnityEngine.Object.DestroyImmediate(mat.gameObject);
                OfficeIdentityAuthoring.FillPortrait(print,print.Find("Photograph").GetComponent<Image>());
            }
            foreach(var name in new[]{"Fax","FaxLabel","Model_fax","Model_paper"})
            {
                var old=office.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            if(!office.Find("MinistryFax")&&!office.Find("Prop_paper"))FacilityPropAuthoring.Esma("paper",office,new Vector3(-2.85f,1.05f,-.8f),new Vector3(.6f,.006f,.42f));
            var signs=office.GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Lettering")&&t.Find("Lettering").GetComponent<Canvas>()).ToArray();
            foreach(var sign in signs)FacilityPropAuthoring.RefreshSign(sign);
            var presenter=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DocumentPresenter>(true)).Single();
            foreach(var text in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Text>(true)))
                if(text.name=="Controls")text.text=text.text.Replace("     T / Rapor","");
            FacilityPropAuthoring.Inventory();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var menu=EditorSceneManager.OpenScene(ProductionProjectSetup.MenuPath);
            var help=menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Text>(true)).Single(t=>t.name=="HelpBody");
            help.text=help.text.Replace("       T  —  Son rapor","");
            EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);
            Debug.Log("FACILITY_DETAILS_OK / signs="+signs.Length+" / F analysis retained / T device removed / portraits filled");
        }
        public static void RefreshAndCapture(){RefreshDetails();CoastalReview.Capture();}
    }
}
