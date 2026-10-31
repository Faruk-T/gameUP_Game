using System.Collections.Generic;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// IMPLEMENTATION.md Bölüm 9, 10, 11, 12 ve 13'teki tüm P0/P1 kurallarını,
    /// 12 ürünlük katalog verisini, gazeteleri, acil faksı ve 2 oyun gününü
    /// standart ve hatasız şekilde üreten fabrika sınıfı (R02, R07).
    /// </summary>
    public static class DefaultGameContentFactory
    {
        public sealed class GeneratedCatalog
        {
            public RuleDefinition RuleExpiry;
            public RuleDefinition RuleIdentity;
            public RuleDefinition RulePollution;
            public RuleDefinition RuleSourceBan;

            // P1 Kuralları
            public RuleDefinition RuleWeight;
            public RuleDefinition RuleLength;
            public RuleDefinition RuleIngredientPct;

            public DocumentDefinition Day1Newspaper;
            public DocumentDefinition Day2Newspaper;
            public DocumentDefinition Day2EmergencyFax;
            public DocumentDefinition Day3Newspaper;

            public List<ProductDefinition> Day1Products = new List<ProductDefinition>();
            public List<ProductDefinition> Day2Products = new List<ProductDefinition>();
            public List<ProductDefinition> Day3P1Products = new List<ProductDefinition>();
            public List<ProductDefinition> P1SampleProducts = new List<ProductDefinition>();

            public DayDefinition Day1;
            public DayDefinition Day2;
            public DayDefinition Day3P1;
        }

        /// <summary>
        /// Bellek içinde (runtime/test veya Editor asset kaydı için) tam veri setini oluşturur.
        /// </summary>
        public static GeneratedCatalog CreateCatalogInMemory()
        {
            var catalog = new GeneratedCatalog();

            // -----------------------------------------------------------------
            // 1. P0 KURALLARI (Bölüm 9)
            // -----------------------------------------------------------------
            catalog.RuleExpiry = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleExpiry.name = "Rule_R_EXPIRY";
            catalog.RuleExpiry.InitializeRule(
                id: RuleIds.Expiry,
                type: RuleType.ExpiryDate,
                allCategories: true,
                categories: null,
                title: "Son Kullanma Tarihi (SKT) Denetimi",
                description: "Ürünün arka etiketindeki SKT, günün tarihinden eski olamaz (SKT < Oyun Tarihi ise RED). SKT'nin günün tarihine eşit olması geçerlidir.",
                failureReason: "SKT geçmiş ürün"
            );

            catalog.RuleIdentity = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleIdentity.name = "Rule_R_IDENTITY";
            catalog.RuleIdentity.InitializeRule(
                id: RuleIds.Identity,
                type: RuleType.IdentityMismatch,
                allCategories: false,
                categories: new[] { ProductCategory.Dairy },
                title: "Beyan ve Tür/İçerik Uyumluluğu",
                description: "Süt ürünlerinde ön yüzdeki ürün türü iddiası ile arka etiketteki yasal ürün türü birebir uyumlu olmalıdır (ör. 'Dondurma' iddiasıyla 'Sütlü Buz' satılamaz).",
                failureReason: "Ön beyan ile arka etiket türü uyumsuz"
            );

            catalog.RulePollution = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RulePollution.name = "Rule_R_POLLUTION";
            catalog.RulePollution.InitializeRule(
                id: RuleIds.Pollution,
                type: RuleType.PollutionAnalysis,
                allCategories: false,
                categories: new[] { ProductCategory.Fish },
                title: "Deniz Kirliliği ve Toksin Analizi",
                description: "Balık ürünlerinde plastik/ağır metal kirliliği etiket üzerinden anlaşılamaz. Analiz cihazından çıkan faks raporunda kirlilik POZİTİF ise ürün reddedilmeli ve ambalajı Kontamine Atık kutusuna atılmalıdır.",
                failureReason: "Laboratuvar analizinde kirlilik pozitif"
            );

            catalog.RuleSourceBan = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleSourceBan.name = "Rule_R_SOURCE_BAN";
            catalog.RuleSourceBan.InitializeRule(
                id: RuleIds.SourceBan,
                type: RuleType.SourceHabitatBan,
                allCategories: false,
                categories: new[] { ProductCategory.Fish },
                title: "Bakanlık Acil Tedbir Yasağı (Bölge B Dip Balıkları)",
                description: "Saat 12.00 itibarıyla Bölge B'den avlanan DİP BALIKLARI, analiz raporu temiz çıksa bile tedbir kapsamında reddedilecektir. (Bölge B yüzey balıkları bu yasağa dahil değildir).",
                failureReason: "Bölge B dip balığı acil tedbir yasağı",
                bannedRegion: SourceRegion.RegionB,
                bannedFishHabitat: FishHabitat.Bottom
            );

            // -----------------------------------------------------------------
            // 2. P1 KURALLARI (Tartı, Cetvel, İçerik Yüzdesi - Bölüm 3 & 14.3)
            // -----------------------------------------------------------------
            catalog.RuleWeight = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleWeight.name = "Rule_R_WEIGHT";
            catalog.RuleWeight.InitializeRule(
                id: RuleIds.Weight,
                type: RuleType.WeightMismatch,
                allCategories: true,
                categories: null,
                title: "Tartı ve Net Ağırlık Kontrolü (P1)",
                description: "Tartıda görünen gerçek ağırlık ile etikette beyan edilen ağırlık arasındaki fark ±5 gramdan fazla olamaz.",
                failureReason: "Beyan edilen ağırlık ile gerçek ağırlık uyumsuz",
                weightTolerance: 5f
            );

            catalog.RuleLength = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleLength.name = "Rule_R_LENGTH";
            catalog.RuleLength.InitializeRule(
                id: RuleIds.Length,
                type: RuleType.LengthBounds,
                allCategories: false,
                categories: new[] { ProductCategory.Fish },
                title: "Asgari Avlanma Boyu Kontrolü (P1)",
                description: "Cetvelle ölçülen balık boyu en az 15 cm olmalıdır. 15 cm altındaki balıklar reddedilir.",
                failureReason: "Asgari boy sınırının altında",
                minLength: 15f,
                maxLength: 0f
            );

            catalog.RuleIngredientPct = ScriptableObject.CreateInstance<RuleDefinition>();
            catalog.RuleIngredientPct.name = "Rule_R_INGREDIENT_PCT";
            catalog.RuleIngredientPct.InitializeRule(
                id: RuleIds.IngredientPercent,
                type: RuleType.IngredientPercentage,
                allCategories: false,
                categories: new[] { ProductCategory.Snack, ProductCategory.Dairy },
                title: "Meyve / İçerik Yüzdesi Standardı (P1)",
                description: "Meyveli/aromalı ürünlerde gerçek meyve oranı en az %10 olmalı ve ön yüz beyanıyla eşleşmelidir (%0.001 gibi yanıltıcı oranlar reddedilir).",
                failureReason: "İçerik yüzdesi yasal eşiğin (%10) altında veya beyanla uyumsuz",
                minIngredientPct: 10f
            );

            // -----------------------------------------------------------------
            // 3. BELGELER: GAZETELER VE ACİL FAKS (Bölüm 10.1, 10.2 & 13)
            // -----------------------------------------------------------------
            catalog.Day1Newspaper = ScriptableObject.CreateInstance<DocumentDefinition>();
            catalog.Day1Newspaper.name = "Doc_Day1_Newspaper";
            catalog.Day1Newspaper.InitializeDocument(
                id: "DOC_DAY1_NEWS",
                type: DocumentType.Newspaper,
                docTitle: "Sıcak hava, gıda taşımacılığını gündeme taşıdı",
                docBody: "Antalya'daki sıcaklıkların ardından süt ürünleri sevkiyatlarında denetimler artırıldı. Yerel üretimin korunması için ürün bilgileri dikkatle kontrol ediliyor.",
                taskBox: "Bugün SKT'yi ve ürünün ön yüzündeki iddiayı arka etiketindeki tür/içerikle karşılaştır.",
                explicitAck: true,
                ruleIds: new[] { RuleIds.Expiry, RuleIds.Identity }
            );

            catalog.Day2Newspaper = ScriptableObject.CreateInstance<DocumentDefinition>();
            catalog.Day2Newspaper.name = "Doc_Day2_Newspaper";
            catalog.Day2Newspaper.InitializeDocument(
                id: "DOC_DAY2_NEWS",
                type: DocumentType.Newspaper,
                docTitle: "Deniz kirliliği ve caretta carettalar için uyarı",
                docBody: "Kıyıda bulunan bir caretta carettanın ardından plastik kirliliği yeniden gündeme geldi. Görevliler deniz canlılarından alınan örneklerin incelendiğini bildirdi.",
                taskBox: "Balıkların kaynak ve habitat etiketlerini incele. Kirlilik etiket üzerinden anlaşılamaz; analiz raporunu kontrol et.",
                explicitAck: true,
                ruleIds: new[] { RuleIds.Expiry, RuleIds.Pollution }
            );

            catalog.Day2EmergencyFax = ScriptableObject.CreateInstance<DocumentDefinition>();
            catalog.Day2EmergencyFax.name = "Doc_Day2_EmergencyFax";
            catalog.Day2EmergencyFax.InitializeDocument(
                id: "DOC_DAY2_EMERGENCY_FAX",
                type: DocumentType.EmergencyFax,
                docTitle: "BAKANLIK ACİL FAKSI — 12.00",
                docBody: "B bölgesinde sızıntı bildirildi. Bu saatten sonra gelen B bölgesi dip balıkları, analiz sonucu temiz görünse bile tedbir kapsamında reddedilecektir.\n\nKapsam: kaynak B + dip balığı. Önceki sevkiyatların kararları değiştirilmez.",
                taskBox: "YENİ KURAL ETKİN: Bölge B + Dip Balığı gelen tüm ürünleri analiz temiz olsa bile REDDET!",
                explicitAck: true,
                ruleIds: new[] { RuleIds.SourceBan }
            );

            // -----------------------------------------------------------------
            // 4. GÜN 1 ÜRÜNLERİ — SÜT ÜRÜNLERİ (Bölüm 10.1: D1-01 .. D1-06)
            // Oyun Tarihi: 01.11.2026
            // -----------------------------------------------------------------
            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_01_YaniksiDondurma_Gececek",
                id: "D1-01",
                category: ProductCategory.Dairy,
                displayName: "Korkuteli Yanıksı Dondurması",
                skt: new GameDate(3, 11, 2026),
                declaredType: "Keçi Sütlü Yanıksı Dondurma",
                listedType: "Keçi Sütlü Yanıksı Dondurma",
                ingredients: "%100 Korkuteli keçi sütü, pancar şekeri, doğal salep.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Süt ürünleri mikrobiyolojik açıdan uygun.",
                packaging: WasteBinType.Paper,
                contaminated: false
            ));

            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_02_YaniksiDondurma_SKT_Red",
                id: "D1-02",
                category: ProductCategory.Dairy,
                displayName: "Korkuteli Yanıksı Dondurması",
                skt: new GameDate(31, 10, 2026), // Red: SKT (31.10.2026 < 01.11.2026)
                declaredType: "Keçi Sütlü Yanıksı Dondurma",
                listedType: "Keçi Sütlü Yanıksı Dondurma",
                ingredients: "%100 Korkuteli keçi sütü, pancar şekeri, doğal salep.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Kimyasal kirlilik yok (Ancak SKT geçmiş).",
                packaging: WasteBinType.Paper,
                contaminated: false
            ));

            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_03_AntalyaYorukPeyniri_Gececek",
                id: "D1-03",
                category: ProductCategory.Dairy,
                displayName: "Antalya Yörük Peyniri",
                skt: new GameDate(4, 11, 2026),
                declaredType: "Tam Yağlı Yörük Peyniri",
                listedType: "Tam Yağlı Yörük Peyniri",
                ingredients: "Pastörize keçi ve koyun sütü, peynir mayası, deniz tuzu.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Uygun.",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_04_AksekiTulumPeyniri_SktSinirOnay",
                id: "D1-04",
                category: ProductCategory.Dairy,
                displayName: "Akseki Tulum Peyniri",
                skt: new GameDate(1, 11, 2026), // Onay: Tarih sınırı örneği (01.11.2026 == 01.11.2026)
                declaredType: "Geleneksel Tulum Peyniri",
                listedType: "Geleneksel Tulum Peyniri",
                ingredients: "Akseki yayla keçi sütü, doğal şirden mayası, kaya tuzu.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Uygun.",
                packaging: WasteBinType.Paper,
                contaminated: false
            ));

            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_05_KeciSutuKaymagi_SKT_Red",
                id: "D1-05",
                category: ProductCategory.Dairy,
                displayName: "Keçi Sütü Kaymağı",
                skt: new GameDate(31, 10, 2026), // Red: SKT
                declaredType: "Doğal Keçi Sütü Kaymağı",
                listedType: "Doğal Keçi Sütü Kaymağı",
                ingredients: "%100 Keçi sütü kreması.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Kimyasal kalıntı yok.",
                packaging: WasteBinType.Glass,
                contaminated: false
            ));

            catalog.Day1Products.Add(CreateProduct(
                assetName: "Product_D1_06_Dondurmam_SutluBuz_BeyanRed",
                id: "D1-06",
                category: ProductCategory.Dairy,
                displayName: "Dondurmam — Sütlü Dondurma",
                skt: new GameDate(5, 11, 2026),
                declaredType: "Hakiki Sütlü Dondurma",
                listedType: "Sütlü Buz", // Red: Beyan/tür uyumsuzluğu (R_IDENTITY)
                ingredients: "Su, şeker şurubu, bitkisel yağ, %2 süt tozu, aroma vericiler.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Toksik madde yok (Etiket beyan uyumsuzluğu mevcut).",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            // -----------------------------------------------------------------
            // 5. GÜN 2 ÜRÜNLERİ — BALIK VE ACİL FAKS (Bölüm 10.2: D2-01 .. D2-06)
            // Oyun Tarihi: 02.11.2026
            // -----------------------------------------------------------------
            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_01_Balik_B_Dip_FaksOncesiOnay",
                id: "D2-01",
                category: ProductCategory.Fish,
                displayName: "Antalya Mercan Balığı (B / Dip)",
                skt: new GameDate(4, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Mercan Balığı (Av Bölgesi: B, Habitat: Dip Balığı).",
                region: SourceRegion.RegionB,
                habitat: FishHabitat.Bottom,
                polluted: false, // Onay: Henüz yasak yok ve analiz temiz
                finding: "TEMİZ — Mikroplastik ve ağır metal değerleri normal sınırda.",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_02_Balik_A_Yuzey_KirliRed",
                id: "D2-02",
                category: ProductCategory.Fish,
                displayName: "Akdeniz Kefali (A / Yüzey)",
                skt: new GameDate(4, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Kefal Balığı (Av Bölgesi: A, Habitat: Yüzey Balığı).",
                region: SourceRegion.RegionA,
                habitat: FishHabitat.Surface,
                polluted: true, // Red: Kirlilik pozitif -> Kontamine atık
                finding: "POZİTİF — Yüksek oranda mikroplastik ve fabrika atığı toksini tespit edildi!",
                packaging: WasteBinType.Plastic,
                contaminated: true
            ));

            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_03_Balik_A_Yuzey_TemizOnay",
                id: "D2-03",
                category: ProductCategory.Fish,
                displayName: "Antalya Sardalyası (A / Yüzey)",
                skt: new GameDate(5, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Sardalya (Av Bölgesi: A, Habitat: Yüzey Balığı).",
                region: SourceRegion.RegionA,
                habitat: FishHabitat.Surface,
                polluted: false, // Onay
                finding: "TEMİZ — Herhangi bir plastik veya kimyasal kirlilik bulgusuna rastlanmadı.",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            // --- 3. ÜRÜN BİTTİKTEN SONRA SAAT 12.00 ACİL FAKSI DEVREYE GİRER ---

            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_04_Balik_B_Dip_FaksSonrasiYasakRed",
                id: "D2-04",
                category: ProductCategory.Fish,
                displayName: "Antalya Barbunu (B / Dip)",
                skt: new GameDate(5, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Barbun Balığı (Av Bölgesi: B, Habitat: Dip Balığı).",
                region: SourceRegion.RegionB,
                habitat: FishHabitat.Bottom,
                polluted: false, // Red: Analiz temiz olsa bile kaynak yasağı (R_SOURCE_BAN). Ambalaj temiz Plastik kalır!
                finding: "TEMİZ — Numunede anlık toksin görülmedi (Not: Bakanlık bölge tedbir kararlarını ayrıca kontrol ediniz).",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_05_Balik_B_Yuzey_FaksSonrasiOnay",
                id: "D2-05",
                category: ProductCategory.Fish,
                displayName: "Antalya Kupası (B / Yüzey)",
                skt: new GameDate(5, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Kupa Balığı (Av Bölgesi: B, Habitat: Yüzey Balığı).",
                region: SourceRegion.RegionB,
                habitat: FishHabitat.Surface,
                polluted: false, // Onay: Yasak yalnızca B + Dip balıklarını kapsar, B + Yüzey onaylanır!
                finding: "TEMİZ — Numune tamamen güvenli.",
                packaging: WasteBinType.Plastic,
                contaminated: false
            ));

            catalog.Day2Products.Add(CreateProduct(
                assetName: "Product_D2_06_Balik_A_Yuzey_KirliRed",
                id: "D2-06",
                category: ProductCategory.Fish,
                displayName: "Akdeniz İstavriti (A / Yüzey)",
                skt: new GameDate(4, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze İstavrit (Av Bölgesi: A, Habitat: Yüzey Balığı).",
                region: SourceRegion.RegionA,
                habitat: FishHabitat.Surface,
                polluted: true, // Red: Kirlilik devam eden bağımsız kural -> Kontamine atık
                finding: "POZİTİF — Ağır petrol türevi ve mikroplastik kontaminasyonu tespit edildi!",
                packaging: WasteBinType.Plastic,
                contaminated: true
            ));

            // -----------------------------------------------------------------
            // 6. P1 GENİŞLEME PAKETİ: 3. GÜN (ET, SEBZE, MEYVE, TARTI, CETVEL, GOFRET - Bölüm 3 & 14.3)
            // Oyun Tarihi: 03.11.2026 (3 Geçerli / 3 Geçersiz = %50 denge korunur)
            // -----------------------------------------------------------------
            catalog.Day3Newspaper = ScriptableObject.CreateInstance<DocumentDefinition>();
            catalog.Day3Newspaper.name = "Doc_Day3_P1_Newspaper";
            catalog.Day3Newspaper.InitializeDocument(
                id: "DOC_DAY3_P1_NEWS",
                type: DocumentType.Newspaper,
                docTitle: "Hassas tartı, boy ve içerik denetimleri başladı",
                docBody: "COP31 Antalya denetim noktasında kapsam genişletildi. Meyve, sebze, et ve paketli gıdalarda gramaj, asgari avlanma boyu ve gerçek meyve yüzdesi kontrolleri devreye alındı.",
                taskBox: "Tartı ağırlığını (±5g), balık boyunu (en az 15 cm) ve meyve yüzdesini (en az %10) kontrol et.",
                explicitAck: true,
                ruleIds: new[] { RuleIds.Expiry, RuleIds.Weight, RuleIds.Length, RuleIds.IngredientPercent }
            );

            // D3-01: Finike Portakalı (Meyve - Sağlam, Tartı 500g / 500g -> Onay)
            ProductDefinition d301 = CreateProduct(
                assetName: "Product_D3_01_FinikePortakali_Onay",
                id: "D3-01",
                category: ProductCategory.Fruit,
                displayName: "Finike Portakalı",
                skt: new GameDate(8, 11, 2026),
                declaredType: "Taze Finike Portakalı",
                listedType: "Taze Finike Portakalı",
                ingredients: "%100 Doğal Finike Portakalı (Coğrafi İşaretli).",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Pestisit kalıntısı saptanmadı.",
                packaging: WasteBinType.Paper,
                contaminated: false
            );
            d301.SetP1Metrics(enableWeight: true, declaredGrams: 500f, actualGrams: 500f);
            catalog.Day3P1Products.Add(d301);

            // D3-02: Kumluca Domatesi (Sebze - Tartı Hilesi: Beyan 1000g, Gerçek 820g -> Red: R_WEIGHT)
            ProductDefinition d302 = CreateProduct(
                assetName: "Product_D3_02_KumlucaDomatesi_TartiRed",
                id: "D3-02",
                category: ProductCategory.Vegetable,
                displayName: "Kumluca Salkım Domatesi",
                skt: new GameDate(6, 11, 2026),
                declaredType: "Sera Salkım Domates",
                listedType: "Sera Salkım Domates",
                ingredients: "Taze Kumluca Domatesi.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Kimyasal kalıntı yok (Tartı eksik gramaj uyarısı).",
                packaging: WasteBinType.Plastic,
                contaminated: false
            );
            d302.SetP1Metrics(enableWeight: true, declaredGrams: 1000f, actualGrams: 820f);
            catalog.Day3P1Products.Add(d302);

            // D3-03: Antalya Çipurası (Balık - Cetvel Boy İhlali: 11 cm < 15 cm -> Red: R_LENGTH)
            ProductDefinition d303 = CreateProduct(
                assetName: "Product_D3_03_AntalyaCipurasi_CetvelBoyRed",
                id: "D3-03",
                category: ProductCategory.Fish,
                displayName: "Antalya Çipurası (Yavru Boy)",
                skt: new GameDate(5, 11, 2026),
                declaredType: "Taze Deniz Balığı",
                listedType: "Taze Deniz Balığı",
                ingredients: "Taze Çipura (Av Bölgesi: A, Habitat: Yüzey Balığı).",
                region: SourceRegion.RegionA,
                habitat: FishHabitat.Surface,
                polluted: false,
                finding: "TEMİZ — Kirlilik yok (Ancak cetvel boyu 11 cm: yasal sınır 15 cm altındadır).",
                packaging: WasteBinType.Plastic,
                contaminated: false
            );
            d303.SetP1Metrics(enableLength: true, lengthCm: 11f);
            catalog.Day3P1Products.Add(d303);

            // D3-04 (ve P1-01): Muzlu Gofret (%10 Muz İddiası vs %0.001 -> Red: R_INGREDIENT_PCT)
            ProductDefinition bananaWafer = CreateProduct(
                assetName: "Product_P1_01_MuzluGofret_YuzdeRed",
                id: "P1-01",
                category: ProductCategory.Snack,
                displayName: "Tropik Muzlu Gofret (Kurmaca Parti)",
                skt: new GameDate(10, 11, 2026),
                declaredType: "Muzlu Gofret (%10 Muz İddialı)",
                listedType: "Muzlu Gofret (%10 Muz İddialı)",
                ingredients: "Buğday unu, bitkisel yağ, şeker, %0.001 muz tozu, yapay muz aroması.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Meyve oranı %0.001 olarak ölçüldü (Yasal eşik: %10).",
                packaging: WasteBinType.Plastic,
                contaminated: false
            );
            bananaWafer.SetP1Metrics(
                enableWeight: true, declaredGrams: 100f, actualGrams: 100f,
                enableLength: false, lengthCm: 0f,
                enableIngredientPct: true, ingredientName: "Muz Oranı", declaredPct: 10f, actualPct: 0.001f
            );
            catalog.P1SampleProducts.Add(bananaWafer);
            catalog.Day3P1Products.Add(bananaWafer);

            // D3-05: Toros Yayla Kuzu Eti (Et Ürünü - Sağlam, Tartı 750g/750g -> Onay)
            ProductDefinition d305 = CreateProduct(
                assetName: "Product_D3_05_TorosKuzuEti_Onay",
                id: "D3-05",
                category: ProductCategory.Meat,
                displayName: "Toros Yayla Kuzu Eti",
                skt: new GameDate(6, 11, 2026),
                declaredType: "Taze Yayla Kuzu Eti",
                listedType: "Taze Yayla Kuzu Eti",
                ingredients: "%100 Toros yaylası kuzu eti.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Çevresel atık veya ağır metal kalıntısına rastlanmadı.",
                packaging: WasteBinType.Plastic,
                contaminated: false
            );
            d305.SetP1Metrics(enableWeight: true, declaredGrams: 750f, actualGrams: 750f);
            catalog.Day3P1Products.Add(d305);

            // D3-06: Alanya Avokadosu (Meyve - Sağlam, Tartı 400g/400g -> Onay)
            ProductDefinition d306 = CreateProduct(
                assetName: "Product_D3_06_AlanyaAvokadosu_Onay",
                id: "D3-06",
                category: ProductCategory.Fruit,
                displayName: "Alanya Avokadosu",
                skt: new GameDate(9, 11, 2026),
                declaredType: "Doğal Alanya Avokadosu",
                listedType: "Doğal Alanya Avokadosu",
                ingredients: "%100 Alanya Avokadosu.",
                region: SourceRegion.AntalyaLocal,
                habitat: FishHabitat.None,
                polluted: false,
                finding: "TEMİZ — Uygun.",
                packaging: WasteBinType.Paper,
                contaminated: false
            );
            d306.SetP1Metrics(enableWeight: true, declaredGrams: 400f, actualGrams: 400f);
            catalog.Day3P1Products.Add(d306);

            // Ek P1 Senaryosu (Madde 13: Çevreye atılan zehirli atıklardan tüketmiş hayvansal et ürünü)
            ProductDefinition pollutedMeat = CreateProduct(
                assetName: "Product_P1_02_KuzuEti_ZehirliAtikRed",
                id: "P1-02",
                category: ProductCategory.Meat,
                displayName: "Ova Besi Eti (Şüpheli Parti)",
                skt: new GameDate(6, 11, 2026),
                declaredType: "Taze Dana/Kuzu Eti",
                listedType: "Taze Dana/Kuzu Eti",
                ingredients: "%100 Besi Eti (Sanayi havzası otlak bölgesi).",
                region: SourceRegion.RegionA,
                habitat: FishHabitat.None,
                polluted: true,
                finding: "POZİTİF — Hayvanın çevreye atılan zehirli sanayi/plastik atıklarından tükettiği saptandı! İmha edilmelidir.",
                packaging: WasteBinType.Plastic,
                contaminated: true
            );
            catalog.P1SampleProducts.Add(pollutedMeat);

            // -----------------------------------------------------------------
            // 7. GÜN TANIMLARI (DAY_01, DAY_02 ve P1 DAY_03)
            // -----------------------------------------------------------------
            catalog.Day1 = ScriptableObject.CreateInstance<DayDefinition>();
            catalog.Day1.name = "Day_01_Dairy";
            catalog.Day1.InitializeDay(
                id: "DAY_01",
                number: 1,
                title: "1. Gün — Süt Ürünleri Denetimi",
                date: new GameDate(1, 11, 2026),
                category: ProductCategory.Dairy,
                newspaper: catalog.Day1Newspaper,
                startRules: new List<RuleDefinition> { catalog.RuleExpiry, catalog.RuleIdentity },
                dayProducts: new List<ProductDefinition>(catalog.Day1Products),
                emergency: new EmergencyDefinition(false, 0, "", null, null)
            );

            catalog.Day2 = ScriptableObject.CreateInstance<DayDefinition>();
            catalog.Day2.name = "Day_02_Fish";
            catalog.Day2.InitializeDay(
                id: "DAY_02",
                number: 2,
                title: "2. Gün — Deniz Ürünleri ve Acil Kriz Denetimi",
                date: new GameDate(2, 11, 2026),
                category: ProductCategory.Fish,
                newspaper: catalog.Day2Newspaper,
                startRules: new List<RuleDefinition> { catalog.RuleExpiry, catalog.RulePollution },
                dayProducts: new List<ProductDefinition>(catalog.Day2Products),
                emergency: new EmergencyDefinition(
                    enabled: true,
                    triggerCount: 3,
                    clockTime: "12.00",
                    rule: catalog.RuleSourceBan,
                    document: catalog.Day2EmergencyFax
                )
            );

            catalog.Day3P1 = ScriptableObject.CreateInstance<DayDefinition>();
            catalog.Day3P1.name = "Day_03_P1_Extended";
            catalog.Day3P1.InitializeDay(
                id: "DAY_03",
                number: 3,
                title: "3. Gün (P1) — Tartı, Cetvel, Meyve/Sebze/Et ve Yüzde Denetimi",
                date: new GameDate(3, 11, 2026),
                category: ProductCategory.Snack,
                newspaper: catalog.Day3Newspaper,
                startRules: new List<RuleDefinition>
                {
                    catalog.RuleExpiry,
                    catalog.RuleWeight,
                    catalog.RuleLength,
                    catalog.RuleIngredientPct
                },
                dayProducts: new List<ProductDefinition>(catalog.Day3P1Products),
                emergency: new EmergencyDefinition(false, 0, "", null, null)
            );

            return catalog;
        }

        private static ProductDefinition CreateProduct(
            string assetName,
            string id,
            ProductCategory category,
            string displayName,
            GameDate skt,
            string declaredType,
            string listedType,
            string ingredients,
            SourceRegion region,
            FishHabitat habitat,
            bool polluted,
            string finding,
            WasteBinType packaging,
            bool contaminated)
        {
            var p = ScriptableObject.CreateInstance<ProductDefinition>();
            p.name = assetName;
            p.InitializeData(
                id: id,
                productCategory: category,
                name: displayName,
                skt: skt,
                declaredType: declaredType,
                listedType: listedType,
                ingredients: ingredients,
                region: region,
                fishHabitat: habitat,
                polluted: polluted,
                findingDetail: finding,
                rawPackagingMaterial: packaging,
                packagingContaminated: contaminated
            );
            return p;
        }
    }
}

