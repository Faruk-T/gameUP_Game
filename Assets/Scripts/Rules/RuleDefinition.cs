using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUP.Contracts
{
    /// <summary>
    /// Kalıcı kural tanımı (IMPLEMENTATION.md Bölüm 6.1 ve Bölüm 9).
    /// Kitapçıkta gösterilen eşik ve koşullar doğrudan bu veriden gelir (Bölüm 4, Kural 5).
    /// </summary>
    [CreateAssetMenu(menuName = "GameUP32/Data/Rule Definition", fileName = "Rule_")]
    public class RuleDefinition : ScriptableObject
    {
        [Header("Temel Kimlik")]
        [SerializeField] private string ruleId = RuleIds.Expiry;
        [SerializeField] private RuleType ruleType = RuleType.ExpiryDate;

        [Header("Kategori Kapsamı")]
        [SerializeField] private bool appliesToAllCategories = true;
        [SerializeField] private List<ProductCategory> applicableCategories = new List<ProductCategory>();

        [Header("Oyuncu Kitapçık ve Sonuç Metinleri")]
        [SerializeField] private string ruleTitle = "Son Kullanma Tarihi (SKT) Denetimi";
        [TextArea(2, 5)]
        [SerializeField] private string playerVisibleDescription =
            "Ürünün SKT bilgisi, o günün tarihinden eski olamaz (SKT < Oyun Tarihi ise RED). Günün tarihine eşit SKT geçerlidir.";
        [SerializeField] private string shortFailureReason = "SKT geçmiş ürün";

        [Header("Kural Parametreleri (R_SOURCE_BAN & P1)")]
        [Tooltip("R_SOURCE_BAN için yasaklı kaynak bölgesi (ör. RegionB)")]
        [SerializeField] private SourceRegion bannedSourceRegion = SourceRegion.RegionB;
        [Tooltip("R_SOURCE_BAN için yasaklı habitat (ör. Bottom / Dip balığı)")]
        [SerializeField] private FishHabitat bannedHabitat = FishHabitat.Bottom;

        [Header("P1 Parametreleri (Tartı, Cetvel, İçerik Yüzdesi)")]
        [Tooltip("R_WEIGHT: İzin verilen maksimum gram sapması")]
        [SerializeField] private float weightToleranceGrams = 5f;
        [Tooltip("R_LENGTH: Minimum yasal boy (cm)")]
        [SerializeField] private float minAllowedLengthCm = 15f;
        [Tooltip("R_LENGTH: Maksimum yasal boy (cm, 0 ise üst sınır yok)")]
        [SerializeField] private float maxAllowedLengthCm = 0f;
        [Tooltip("R_INGREDIENT_PCT: Minimum içerik yüzdesi (0..100 birimiyle, ör. %10 için 10f)")]
        [SerializeField] private float minRequiredIngredientPercent = 10f;

        public string RuleId => ruleId;
        public RuleType RuleType => ruleType;
        public bool AppliesToAllCategories => appliesToAllCategories;
        public IReadOnlyList<ProductCategory> ApplicableCategories => applicableCategories;
        public string RuleTitle => ruleTitle;
        public string PlayerVisibleDescription => playerVisibleDescription;
        public string ShortFailureReason => shortFailureReason;

        public SourceRegion BannedSourceRegion => bannedSourceRegion;
        public FishHabitat BannedHabitat => bannedHabitat;
        public float WeightToleranceGrams => weightToleranceGrams;
        public float MinAllowedLengthCm => minAllowedLengthCm;
        public float MaxAllowedLengthCm => maxAllowedLengthCm;
        public float MinRequiredIngredientPercent => minRequiredIngredientPercent;

        public bool AppliesToCategory(ProductCategory category)
        {
            if (appliesToAllCategories) return true;
            return applicableCategories != null && applicableCategories.Contains(category);
        }

        /// <summary>
        /// Ürün masaya geldiği anda kuralın değiştirilemez (immutable) bir kopyasını üretir (Bölüm 6.2).
        /// </summary>
        public RuleSnapshotEntry ToSnapshotEntry()
        {
            var categoriesCopy = applicableCategories != null
                ? new List<ProductCategory>(applicableCategories).ToArray()
                : Array.Empty<ProductCategory>();

            return new RuleSnapshotEntry(
                ruleId: ruleId,
                ruleType: ruleType,
                appliesToAllCategories: appliesToAllCategories,
                applicableCategories: categoriesCopy,
                ruleTitle: ruleTitle,
                playerVisibleDescription: playerVisibleDescription,
                shortFailureReason: shortFailureReason,
                bannedSourceRegion: bannedSourceRegion,
                bannedHabitat: bannedHabitat,
                weightToleranceGrams: weightToleranceGrams,
                minAllowedLengthCm: minAllowedLengthCm,
                maxAllowedLengthCm: maxAllowedLengthCm,
                minRequiredIngredientPercent: minRequiredIngredientPercent
            );
        }

        public void InitializeRule(
            string id,
            RuleType type,
            bool allCategories,
            ProductCategory[] categories,
            string title,
            string description,
            string failureReason,
            SourceRegion bannedRegion = SourceRegion.None,
            FishHabitat bannedFishHabitat = FishHabitat.None,
            float weightTolerance = 5f,
            float minLength = 15f,
            float maxLength = 0f,
            float minIngredientPct = 10f)
        {
            ruleId = id;
            ruleType = type;
            appliesToAllCategories = allCategories;
            applicableCategories = categories != null ? new List<ProductCategory>(categories) : new List<ProductCategory>();
            ruleTitle = title;
            playerVisibleDescription = description;
            shortFailureReason = failureReason;
            bannedSourceRegion = bannedRegion;
            bannedHabitat = bannedFishHabitat;
            weightToleranceGrams = weightTolerance;
            minAllowedLengthCm = minLength;
            maxAllowedLengthCm = maxLength;
            minRequiredIngredientPercent = minIngredientPct;
        }
    }

    /// <summary>
    /// RuleSnapshot içinde tutulan, tamamen bağımsız ve sonradan değiştirilemez kural kopyası.
    /// Acil faks sonradan etkinleşse bile önceki ürünün RuleSnapshotEntry listesi değişmez (Bölüm 6.2 &amp; 11).
    /// </summary>
    [Serializable]
    public sealed class RuleSnapshotEntry
    {
        public string RuleId { get; }
        public RuleType RuleType { get; }
        public bool AppliesToAllCategories { get; }
        public IReadOnlyList<ProductCategory> ApplicableCategories { get; }
        public string RuleTitle { get; }
        public string PlayerVisibleDescription { get; }
        public string ShortFailureReason { get; }
        public SourceRegion BannedSourceRegion { get; }
        public FishHabitat BannedHabitat { get; }
        public float WeightToleranceGrams { get; }
        public float MinAllowedLengthCm { get; }
        public float MaxAllowedLengthCm { get; }
        public float MinRequiredIngredientPercent { get; }

        public RuleSnapshotEntry(
            string ruleId,
            RuleType ruleType,
            bool appliesToAllCategories,
            ProductCategory[] applicableCategories,
            string ruleTitle,
            string playerVisibleDescription,
            string shortFailureReason,
            SourceRegion bannedSourceRegion,
            FishHabitat bannedHabitat,
            float weightToleranceGrams,
            float minAllowedLengthCm,
            float maxAllowedLengthCm,
            float minRequiredIngredientPercent)
        {
            RuleId = ruleId ?? string.Empty;
            RuleType = ruleType;
            AppliesToAllCategories = appliesToAllCategories;
            ApplicableCategories = applicableCategories ?? Array.Empty<ProductCategory>();
            RuleTitle = ruleTitle ?? string.Empty;
            PlayerVisibleDescription = playerVisibleDescription ?? string.Empty;
            ShortFailureReason = shortFailureReason ?? string.Empty;
            BannedSourceRegion = bannedSourceRegion;
            BannedHabitat = bannedHabitat;
            WeightToleranceGrams = weightToleranceGrams;
            MinAllowedLengthCm = minAllowedLengthCm;
            MaxAllowedLengthCm = maxAllowedLengthCm;
            MinRequiredIngredientPercent = minRequiredIngredientPercent;
        }

        public bool AppliesToCategory(ProductCategory category)
        {
            if (AppliesToAllCategories) return true;
            for (int i = 0; i < ApplicableCategories.Count; i++)
            {
                if (ApplicableCategories[i] == category)
                    return true;
            }
            return false;
        }
    }
}

