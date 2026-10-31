using System;

namespace GameUP.Contracts
{
    /// <summary>
    /// Sabit kural kimlikleri (IMPLEMENTATION.md Bölüm 9).
    /// </summary>
    public static class RuleIds
    {
        public const string Expiry = "R_EXPIRY";
        public const string Identity = "R_IDENTITY";
        public const string Pollution = "R_POLLUTION";
        public const string SourceBan = "R_SOURCE_BAN";

        // P1 genişleme kuralları
        public const string Weight = "R_WEIGHT";
        public const string Length = "R_LENGTH";
        public const string IngredientPercent = "R_INGREDIENT_PCT";
    }

    /// <summary>
    /// Ürün kategorileri. P0: Dairy (Süt Ürünleri) ve Fish (Balık).
    /// Diğerleri P1 genişlemesi için hazırdır.
    /// </summary>
    public enum ProductCategory
    {
        Dairy = 0,
        Fish = 1,
        Meat = 2,
        Vegetable = 3,
        Fruit = 4,
        Snack = 5
    }

    /// <summary>
    /// Kural türleri (Bölüm 9 + P1).
    /// </summary>
    public enum RuleType
    {
        ExpiryDate = 0,          // R_EXPIRY: ExpiryDate < GameDate
        IdentityMismatch = 1,    // R_IDENTITY: Ön yüz beyanı != arka etiket tür/içerik
        PollutionAnalysis = 2,   // R_POLLUTION: Kirlilik verisi pozitif
        SourceHabitatBan = 3,    // R_SOURCE_BAN: Belirli kaynak bölgesi VE habitat yasağı
        WeightMismatch = 4,      // P1 - R_WEIGHT: Tartı ağırlığı ile etiket beyanı uyumsuz
        LengthBounds = 5,        // P1 - R_LENGTH: Cetvel boyu sınır dışı
        IngredientPercentage = 6 // P1 - R_INGREDIENT_PCT: Meyve/içerik yüzdesi eşik altında veya beyanla uyumsuz
    }

    /// <summary>
    /// Oyuncu veya sistemin ürün hakkındaki kararı.
    /// </summary>
    public enum ProductDecision
    {
        Approve = 0,
        Reject = 1
    }

    /// <summary>
    /// Reddedilen ürünlerin ambalaj ayrıştırma hedefleri (Bölüm 12).
    /// </summary>
    public enum WasteBinType
    {
        None = 0,
        Paper = 1,
        Glass = 2,
        Plastic = 3,
        Contaminated = 4
    }

    /// <summary>
    /// Balık veya yerel ürünlerin kaynak bölgesi (Bölüm 10.2).
    /// </summary>
    public enum SourceRegion
    {
        None = 0,
        RegionA = 1,
        RegionB = 2,
        AntalyaLocal = 3
    }

    /// <summary>
    /// Deniz ürünlerinin yaşam alanı / avlanma derinliği (Bölüm 10.2).
    /// </summary>
    public enum FishHabitat
    {
        None = 0,
        Surface = 1, // Yüzey balığı
        Bottom = 2   // Dip balığı
    }

    /// <summary>
    /// DocumentPresenter tarafından gösterilen belge türleri (Bölüm 13).
    /// </summary>
    public enum DocumentType
    {
        Newspaper = 0,
        Rulebook = 1,
        AnalysisReport = 2,
        EmergencyFax = 3
    }

    /// <summary>
    /// Küresel oyun akışı durumları (Bölüm 8.1 — Teknik Lider).
    /// </summary>
    public enum GlobalGameState
    {
        Boot = 0,
        MorningBriefing = 1,
        Working = 2,
        EmergencyBriefing = 3,
        DaySummary = 4,
        GameSummary = 5
    }

    /// <summary>
    /// Yerel ürün etkileşim durumu (Bölüm 8.2 — Özgür).
    /// </summary>
    public enum ProductInteractionState
    {
        Arriving = 0,
        OnDesk = 1,
        Inspecting = 2,
        Analyzing = 3,
        Routing = 4,
        AwaitingSort = 5,
        Finished = 6
    }

    /// <summary>
    /// Ekranda ve etiketlerde Türkçe gösterim yardımcıları.
    /// </summary>
    public static class GameUpTextFormatter
    {
        public static string ToTurkishText(this ProductCategory category)
        {
            return category switch
            {
                ProductCategory.Dairy => "Süt Ürünleri",
                ProductCategory.Fish => "Balık Ürünleri",
                ProductCategory.Meat => "Et Ürünleri",
                ProductCategory.Vegetable => "Sebze",
                ProductCategory.Fruit => "Meyve",
                ProductCategory.Snack => "Atıştırmalık",
                _ => category.ToString()
            };
        }

        public static string ToTurkishText(this WasteBinType bin)
        {
            return bin switch
            {
                WasteBinType.Paper => "Kâğıt Atık",
                WasteBinType.Glass => "Cam Atık",
                WasteBinType.Plastic => "Plastik Atık",
                WasteBinType.Contaminated => "Kontamine Atık",
                _ => "Belirsiz"
            };
        }

        public static string ToTurkishText(this SourceRegion region)
        {
            return region switch
            {
                SourceRegion.RegionA => "Bölge A",
                SourceRegion.RegionB => "Bölge B",
                SourceRegion.AntalyaLocal => "Antalya Yerel",
                _ => "—"
            };
        }

        public static string ToTurkishText(this FishHabitat habitat)
        {
            return habitat switch
            {
                FishHabitat.Surface => "Yüzey Balığı",
                FishHabitat.Bottom => "Dip Balığı",
                _ => "—"
            };
        }

        public static string ToTurkishText(this ProductDecision decision)
        {
            return decision == ProductDecision.Approve ? "Onay (Piyasaya Sür)" : "Red (İmha & Ayrıştır)";
        }
    }
}

