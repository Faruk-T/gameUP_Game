using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUP.Rules
{
    /// <summary>
    /// IMPLEMENTATION.md Bölüm 3 &amp; 14.1 (A06) ve 14.2 (I02):
    /// "Küçük metinler sabit texture'a gömülmek yerine değiştirilebilir etiket alanına konur."
    /// "Resident Evil benzeri inceleme: Ön/arka etiket okunur."
    ///
    /// Bu bileşen herhangi bir 3D ürün prefabına veya geçici küp/silindir nesnesine eklendiğinde,
    /// ürünün ÖN YÜZÜNE (Front Label) ve ARKA YÜZÜNE (Back Label - SKT, Tür, İçindekiler, Kaynak, Ambalaj)
    /// otomatik olarak okunabilir World-Space etiket yüzeyleri yerleştirir ve ProductDefinition verisiyle doldurur.
    /// Özgür ürünü fareyle döndürdüğünde oyuncu ön yüz iddiası ile arka yüz künyesini doğrudan 3D nesne üzerinde okur!
    /// </summary>
    public class ProductWorldLabelBinder : MonoBehaviour
    {
        [Header("Veri Kaynağı")]
        [SerializeField] private ProductDefinition productDefinition;

        [Header("İsteğe Bağlı Mevcut Metin Referansları (Boşsa Otomatik Oluşturulur)")]
        [SerializeField] private Text frontLabelText;
        [SerializeField] private Text backLabelText;

        [Header("3D Etiket Yerleşim Ayarları")]
        [SerializeField] private Vector3 frontLocalPosition = new Vector3(0f, 0f, -0.52f);
        [SerializeField] private Vector3 frontLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 backLocalPosition = new Vector3(0f, 0f, 0.52f);
        [SerializeField] private Vector3 backLocalEulerAngles = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector2 labelSizeMeters = new Vector2(0.88f, 0.88f);

        private GameObject generatedLabelsRoot;

        public ProductDefinition ProductDefinition => productDefinition;

        private void Awake()
        {
            if (productDefinition != null)
            {
                Bind(productDefinition);
            }
        }

        /// <summary>
        /// ProductDefinition ScriptableObject verisini 3D nesnenin ön ve arka etiketlerine yazar.
        /// </summary>
        public void Bind(ProductDefinition definition)
        {
            productDefinition = definition;
            if (definition == null) return;

            EnsureWorldSpaceLabels();

            if (frontLabelText != null)
            {
                frontLabelText.text = definition.GetFormattedFrontLabel();
            }

            if (backLabelText != null)
            {
                backLabelText.text = definition.GetFormattedBackLabel();
            }
        }

        /// <summary>
        /// Hakan'ın LeadContracts.ProductInfo nesnesiyle doğrudan bağlama desteği.
        /// </summary>
        public void BindFromLeadProductInfo(GameUp.Contracts.ProductInfo info, ProductDefinition optionalDefinition = null)
        {
            if (optionalDefinition != null)
            {
                Bind(optionalDefinition);
                return;
            }

            if (info == null) return;
            EnsureWorldSpaceLabels();

            if (frontLabelText != null)
            {
                frontLabelText.text = $"<b>{info.Name}</b>\nKategori: {info.Category}\nKod: {info.Id}";
            }

            if (backLabelText != null)
            {
                backLabelText.text = info.Label;
            }
        }

        private void EnsureWorldSpaceLabels()
        {
            if (frontLabelText != null && backLabelText != null)
                return;

            if (generatedLabelsRoot == null)
            {
                Transform existing = transform.Find("__ProductWorldLabels");
                if (existing != null)
                {
                    generatedLabelsRoot = existing.gameObject;
                }
                else
                {
                    generatedLabelsRoot = new GameObject("__ProductWorldLabels");
                    generatedLabelsRoot.transform.SetParent(transform, false);
                    generatedLabelsRoot.transform.localPosition = Vector3.zero;
                    generatedLabelsRoot.transform.localRotation = Quaternion.identity;
                    generatedLabelsRoot.transform.localScale = Vector3.one;
                }
            }

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (frontLabelText == null)
            {
                frontLabelText = CreateFaceCanvas(
                    parent: generatedLabelsRoot.transform,
                    canvasName: "FrontLabelCanvas",
                    localPos: frontLocalPosition,
                    localEuler: frontLocalEulerAngles,
                    bgColor: new Color32(252, 246, 230, 255),
                    textColor: new Color32(22, 42, 46, 255),
                    font: builtinFont,
                    fontSize: 22,
                    alignment: TextAnchor.MiddleCenter
                );
            }

            if (backLabelText == null)
            {
                backLabelText = CreateFaceCanvas(
                    parent: generatedLabelsRoot.transform,
                    canvasName: "BackLabelCanvas",
                    localPos: backLocalPosition,
                    localEuler: backLocalEulerAngles,
                    bgColor: new Color32(240, 244, 245, 255),
                    textColor: new Color32(20, 30, 35, 255),
                    font: builtinFont,
                    fontSize: 17,
                    alignment: TextAnchor.UpperLeft
                );
            }
        }

        private Text CreateFaceCanvas(
            Transform parent,
            string canvasName,
            Vector3 localPos,
            Vector3 localEuler,
            Color32 bgColor,
            Color32 textColor,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            var canvasGo = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localRotation = Quaternion.Euler(localEuler);

            const float pixelsPerUnit = 320f;
            Vector2 pixelSize = labelSizeMeters * pixelsPerUnit;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = pixelSize;
            canvasGo.transform.localScale = Vector3.one * (1f / pixelsPerUnit);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = bgColor;
            bgImage.raycastTarget = false;

            var textGo = new GameObject("LabelText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 14f);
            textRect.offsetMax = new Vector2(-14f, -14f);

            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }
    }
}
