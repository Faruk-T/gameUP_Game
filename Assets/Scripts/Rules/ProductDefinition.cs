using System;
using System.Text;
using UnityEngine;

namespace GameUP.Contracts
{
    /// <summary>
    /// Kalıcı ürün tasarım verisi (IMPLEMENTATION.md Bölüm 6.1).
    /// ÖNEMLİ: Analiz edildi, karar verildi, puanlandı gibi çalışma zamanı (runtime)
    /// durumları kesinlikle bu ScriptableObject üzerine yazılmaz (Bölüm 6.2).
    /// </summary>
    [CreateAssetMenu(menuName = "GameUP32/Data/Product Definition", fileName = "Product_")]
    public class ProductDefinition : ScriptableObject
    {
        [Header("Kimlik ve Görsel")]
        [Tooltip("Bölüm 10'daki senaryo kimliği (ör. D1-01, D2-04)")]
        [SerializeField] private string definitionId = "D1-01";
        [SerializeField] private ProductCategory category = ProductCategory.Dairy;
        [SerializeField] private string displayName = "Korkuteli Yanıksı Dondurması";
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject modelSource;
        [SerializeField] private FoodCondition condition;
        [SerializeField] private Sprite productIcon;

        [Header("Etiket & Tarih Bilgileri (Oyuncuya Görünür)")]
        [Tooltip("Son Kullanma Tarihi (SKT)")]
        [SerializeField] private GameDate expiryDate = new GameDate(3, 11, 2026);
        [Tooltip("Ön yüzdeki ürün türü iddiası (ör. 'Keçi Sütlü Dondurma')")]
        [SerializeField] private string declaredProductType = "Keçi Sütlü Dondurma";
        [Tooltip("Arka etiketteki gerçek ürün türü (ör. 'Keçi Sütlü Dondurma' veya 'Sütlü Buz')")]
        [SerializeField] private string listedProductType = "Keçi Sütlü Dondurma";
        [TextArea(2, 4)]
        [SerializeField] private string ingredientsSummary = "%100 Antalya keçi sütü, pancar şekeri, salep.";
        [TextArea(2, 4)]
        [SerializeField] private string customFrontLabelNote = "";
        [TextArea(2, 4)]
        [SerializeField] private string customBackLabelNote = "";

        [Header("Kaynak ve Habitat (Balık & Yerel Ürünler)")]
        [SerializeField] private SourceRegion sourceRegion = SourceRegion.AntalyaLocal;
        [SerializeField] private FishHabitat habitat = FishHabitat.None;

        [Header("Gizli Analiz Verisi (Rapor Çıkana Kadar Gizlidir)")]
        [Tooltip("Cihaz analizinde kirlilik/toksin pozitif çıkacak mı?")]
        [SerializeField] private bool isPolluted = false;
        [Tooltip("Analiz raporunda fakstan çıkacak bulgu metni")]
        [TextArea(2, 4)]
        [SerializeField] private string analysisFindingDetail = "TEMİZ — Ağır metal veya mikroplastik kalıntısına rastlanmadı.";

        [Header("Ambalaj ve Atık Ayrıştırma (Bölüm 12)")]
        [Tooltip("Ham ambalaj malzemesi (Kâğıt, Cam, Plastik)")]
        [SerializeField] private WasteBinType packagingMaterial = WasteBinType.Paper;
        [Tooltip("Ambalaj kimyasal/biyolojik olarak kontamine mi? Evet ise hedef Kontamine Atık olur.")]
        [SerializeField] private bool isPackagingContaminated = false;
        [SerializeField] private string packagingLabelText = "Geri Dönüştürülebilir Kâğıt Ambalaj";

        [Header("P1 Genişleme: Tartı, Cetvel ve İçerik Yüzdesi")]
        [SerializeField] private bool hasWeightData = false;
        [SerializeField] private float declaredWeightGrams = 250f;
        [SerializeField] private float actualWeightGrams = 250f;

        [SerializeField] private bool hasLengthData = false;
        [SerializeField] private float actualLengthCm = 20f;

        [Tooltip("Bölüm 14.3: Yüzde değerleri her yerde 0..100 birimiyle tutulur (ör. %10 -> 10f, %0.001 -> 0.001f).")]
        [SerializeField] private bool hasIngredientPercentData = false;
        [SerializeField] private string trackedIngredientName = "";
        [SerializeField] private float declaredIngredientPercent = 10f;
        [SerializeField] private float actualIngredientPercent = 10f;

        // Salt-okunur erişimciler (Runtime sızıntısını önler)
        public string DefinitionId => definitionId;
        public ProductCategory Category => category;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public GameObject ModelSource => modelSource;
        public FoodCondition Condition => condition;
        public Sprite ProductIcon => productIcon;
        public GameDate ExpiryDate => expiryDate;
        public string DeclaredProductType => declaredProductType;
        public string ListedProductType => listedProductType;
        public string IngredientsSummary => ingredientsSummary;
        public string CustomFrontLabelNote => customFrontLabelNote;
        public string CustomBackLabelNote => customBackLabelNote;
        public SourceRegion SourceRegion => sourceRegion;
        public FishHabitat Habitat => habitat;

        // Gizli analiz verisi (Sadece AnalysisService ve RuleEngine okur; UI doğrudan göstermez)
        public bool IsPolluted => isPolluted;
        public string AnalysisFindingDetail => analysisFindingDetail;

        // Ambalaj verisi
        public WasteBinType PackagingMaterial => packagingMaterial;
        public bool IsPackagingContaminated => isPackagingContaminated;
        public string PackagingLabelText => packagingLabelText;

        // P1 alanları
        public bool HasWeightData => hasWeightData;
        public float DeclaredWeightGrams => declaredWeightGrams;
        public float ActualWeightGrams => actualWeightGrams;
        public bool HasLengthData => hasLengthData;
        public float ActualLengthCm => actualLengthCm;
        public bool HasIngredientPercentData => hasIngredientPercentData;
        public string TrackedIngredientName => trackedIngredientName;
        public float DeclaredIngredientPercent => declaredIngredientPercent;
        public float ActualIngredientPercent => actualIngredientPercent;

        /// <summary>
        /// Ön yüz etiketi: İddia edilen ürün adı, tür beyanı ve özet not.
        /// </summary>
        public string GetFormattedFrontLabel()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{displayName}</b>");
            sb.AppendLine($"Tür Beyanı: {declaredProductType}");
            if (category == ProductCategory.Fish)
            {
                sb.AppendLine($"Av Bölgesi: {sourceRegion.ToTurkishText()} | Tür: {habitat.ToTurkishText()}");
            }
            if (!string.IsNullOrWhiteSpace(customFrontLabelNote))
            {
                sb.AppendLine(customFrontLabelNote);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Arka yüz etiketi: SKT, yasal ürün türü/içeriği, kaynak/habitat ve ambalaj bilgisi.
        /// Kirlilik verisi burada YER ALMAZ (analiz cihazından çıkar).
        /// </summary>
        public string GetFormattedBackLabel()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>ÜRÜN KÜNYESİ ({definitionId})</b>");
            sb.AppendLine($"SKT: {expiryDate}");
            sb.AppendLine($"Yasal Tür: {listedProductType}");
            sb.AppendLine($"İçindekiler: {ingredientsSummary}");

            if (sourceRegion != SourceRegion.None)
            {
                sb.AppendLine($"Kaynak: {sourceRegion.ToTurkishText()}");
            }
            if (habitat != FishHabitat.None)
            {
                sb.AppendLine($"Habitat: {habitat.ToTurkishText()}");
            }
            if (hasWeightData)
            {
                sb.AppendLine($"Net Ağırlık Beyanı: {declaredWeightGrams:0.#} g | Tartı Ölçümü: {actualWeightGrams:0.#} g");
            }
            if (hasLengthData)
            {
                sb.AppendLine($"Cetvel Boy Ölçümü: {actualLengthCm:0.#} cm");
            }
            if (hasIngredientPercentData && !string.IsNullOrWhiteSpace(trackedIngredientName))
            {
                sb.AppendLine($"Beyan Edilen {trackedIngredientName}: %{declaredIngredientPercent:0.###} | Gerçek Oran: %{actualIngredientPercent:0.###}");
            }

            sb.AppendLine($"Ambalaj: {packagingLabelText}");

            if (!string.IsNullOrWhiteSpace(customBackLabelNote))
            {
                sb.AppendLine(customBackLabelNote);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Editor veri üreteci ve birim testleri için güvenli başlatıcı.
        /// </summary>
        public void InitializeData(
            string id,
            ProductCategory productCategory,
            string name,
            GameDate skt,
            string declaredType,
            string listedType,
            string ingredients,
            SourceRegion region,
            FishHabitat fishHabitat,
            bool polluted,
            string findingDetail,
            WasteBinType rawPackagingMaterial,
            bool packagingContaminated,
            string packagingNote = "",
            string frontNote = "",
            string backNote = "",
            GameObject modelPrefab = null,
            Sprite iconSprite = null)
        {
            definitionId = id;
            category = productCategory;
            displayName = name;
            expiryDate = skt;
            declaredProductType = declaredType;
            listedProductType = listedType;
            ingredientsSummary = ingredients;
            sourceRegion = region;
            habitat = fishHabitat;
            isPolluted = polluted;
            analysisFindingDetail = findingDetail;
            packagingMaterial = rawPackagingMaterial;
            isPackagingContaminated = packagingContaminated;
            packagingLabelText = string.IsNullOrWhiteSpace(packagingNote)
                ? (packagingContaminated
                    ? $"{rawPackagingMaterial.ToTurkishText()} (KONTAMİNE / TEHLİKELİ ATIK UYARISI)"
                    : $"{rawPackagingMaterial.ToTurkishText()}")
                : packagingNote;
            customFrontLabelNote = frontNote;
            customBackLabelNote = backNote;
            if (modelPrefab != null)
            {
                prefab = modelPrefab;
            }
            if (iconSprite != null)
            {
                productIcon = iconSprite;
            }
        }

        /// <summary>
        /// P1 (Tartı, Cetvel, İçerik Yüzdesi) verilerini ayarlamak için yardımcı yöntem.
        /// </summary>
        public void SetP1Metrics(
            bool enableWeight = false, float declaredGrams = 0f, float actualGrams = 0f,
            bool enableLength = false, float lengthCm = 0f,
            bool enableIngredientPct = false, string ingredientName = "", float declaredPct = 0f, float actualPct = 0f)
        {
            hasWeightData = enableWeight;
            declaredWeightGrams = declaredGrams;
            actualWeightGrams = actualGrams;

            hasLengthData = enableLength;
            actualLengthCm = lengthCm;

            hasIngredientPercentData = enableIngredientPct;
            trackedIngredientName = ingredientName;
            declaredIngredientPercent = declaredPct;
            actualIngredientPercent = actualPct;
        }
    }
}
