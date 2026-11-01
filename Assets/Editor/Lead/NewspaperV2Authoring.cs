using System.Linq;
using GameUp.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    public static class NewspaperV2Authoring
    {
        internal const string CarettaPath="Assets/Art/Documents/CarettaNewsPhoto.asset";
        [MenuItem("GameUP32/Authoring/Apply newspaper reference style")]
        public static void Apply()
        {
            var paperImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Documents/NewspaperPaperV2.png");
            paperImporter.textureType=TextureImporterType.Sprite;paperImporter.spriteImportMode=SpriteImportMode.Single;paperImporter.alphaIsTransparency=true;
            paperImporter.mipmapEnabled=false;paperImporter.maxTextureSize=2048;paperImporter.SaveAndReimport();
            var referenceImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Documents/NewspaperReference.jpg");
            referenceImporter.textureType=TextureImporterType.Default;referenceImporter.npotScale=TextureImporterNPOTScale.None;referenceImporter.maxTextureSize=4096;referenceImporter.mipmapEnabled=false;referenceImporter.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Documents/NewspaperReference.jpg");
            var sprite=Sprite.Create(texture,new Rect(160,612,1020,580),new Vector2(.5f,.5f),100);
            sprite.name="Caretta_Halftone_PressPhotograph";var saved=AssetDatabase.LoadAssetAtPath<Sprite>(CarettaPath);
            if(saved){EditorUtility.CopySerialized(sprite,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(sprite);}else AssetDatabase.CreateAsset(sprite,CarettaPath);
            var scene=EditorSceneManager.OpenScene(ProductionProjectSetup.GamePath);var roots=scene.GetRootGameObjects();
            var canvas=roots.Single(r=>r.name=="GameCanvas");var presenter=canvas.GetComponent<DocumentPresenter>();
            var overlay=canvas.transform.Find("DocumentOverlay");var old=overlay.Find("Newspaper");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var previous in canvas.GetComponents<NewspaperView>())UnityEngine.Object.DestroyImmediate(previous);
            Set(presenter,"newspaper",NewspaperAuthoring.Create(overlay,presenter));WorkstationUIAuthoring.Apply(canvas);
            ConveyorAuthoring.FitDesk(roots.Single(r=>r.name=="InspectionOffice").transform);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var newspaper=canvas.GetComponent<NewspaperView>();var days=ProductionProjectSetup.Days();
            newspaper.SendMessage("Awake",SendMessageOptions.RequireReceiver);
            overlay.gameObject.SetActive(true);overlay.Find("DocumentPage").gameObject.SetActive(false);
            for(int i=0;i<days.Length;i++)
            {
                newspaper.Show(days[i],days[i].InitialRules.Select(r=>new GameUp.Contracts.RuleInfo(r.RuleId,r.PlayerVisibleDescription)).ToArray());
                UnityEngine.Canvas.ForceUpdateCanvases();
                foreach(var text in overlay.Find("Newspaper").GetComponentsInChildren<Text>())
                {
                    while(text.preferredHeight>text.rectTransform.rect.height+1&&text.fontSize>18&&(text.name=="ArticleColumnLeft"||text.name=="ArticleColumnRight"||text.name=="DailyRules"))text.fontSize--;
                    if(text.preferredHeight>text.rectTransform.rect.height+1)throw new System.InvalidOperationException("Newspaper text overflow: "+text.name+" day="+(i+1)+" height="+text.preferredHeight+" available="+text.rectTransform.rect.height);
                }
                ProductionSnapshot.Capture(canvas.GetComponent<Canvas>(),null,"Docs/Previews/Workstation/NewspaperDay"+(i+1)+".png",1920,1080);
            }
            var dynamicNames=new[]{"Dateline","Section","Headline","Byline","PhotoCaption","ArticleColumnLeft","ArticleColumnRight","DailyRules"};
            foreach(var text in overlay.Find("Newspaper").GetComponentsInChildren<Text>(true))if(dynamicNames.Contains(text.name))text.text="";
            newspaper.Hide();overlay.gameObject.SetActive(false);overlay.Find("DocumentPage").gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ProductionProjectSetup.FirstPath);WorkstationReview.Capture();
            Debug.Log("NEWSPAPER_REFERENCE_STYLE_OK / three live editions / caretta halftone / blank aged paper / daily rule box");
        }
    }
}
