using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameUP.Contracts;
using UnityEditor;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    /// <summary>Authoring plan only. Shipping judgments use the rule evaluator and saved assets.</summary>
    public static class CampaignAuthoring
    {
        public const string Root="Assets/Data/Campaign";
        public const string CatalogPath=Root+"/Campaign.asset";
        [Flags] private enum Issue { None=0, Spoiled=1, Expired=2, Mislabel=4, Pollution=8, Recall=16 }
        private sealed class Sample
        {
            internal readonly string Model;
            internal readonly Issue Issues;
            internal readonly SourceRegion Region;
            internal readonly GameDate Skt;
            internal Sample(string model, GameDate skt, Issue issue=Issue.None, SourceRegion region=SourceRegion.AntalyaLocal)
            { Model=model; Skt=skt; Issues=issue; Region=region; }
        }
        private static readonly Sample[][] Plan={
            new[]{
                new Sample("milk1",      new GameDate(8,11,2026)),
                new Sample("meat1",      new GameDate(4,11,2026),  Issue.Spoiled),
                new Sample("cheese1",    new GameDate(25,10,2026), Issue.Expired),
                new Sample("Yoghurt_01", new GameDate(1,11,2026)),
                new Sample("meat2",      new GameDate(12,11,2026)),
                new Sample("milk2",      new GameDate(9,11,2026),  Issue.Mislabel),
                new Sample("cheese2",    new GameDate(29,10,2026), Issue.Spoiled | Issue.Expired),
                new Sample("meat3",      new GameDate(6,11,2026))
            },
            new[]{
                new Sample("Cantaloupe",    new GameDate(9,11,2026)),
                new Sample("Papaya",        new GameDate(30,10,2026), Issue.Spoiled | Issue.Expired),
                new Sample("Pineapple",     new GameDate(14,11,2026)),
                new Sample("Cantaloupe_01", new GameDate(11,11,2026), Issue.Pollution),
                new Sample("Papaya_01",     new GameDate(2,11,2026)),
                new Sample("Pineapple_01",  new GameDate(7,11,2026),  Issue.Spoiled),
                new Sample("milk3",         new GameDate(5,11,2026)),
                new Sample("cheese3",       new GameDate(1,11,2026),  Issue.Expired),
                new Sample("Yoghurt_02",    new GameDate(10,11,2026)),
                new Sample("meat4",         new GameDate(8,11,2026),  Issue.Pollution)
            },
            new[]{
                new Sample("milk4",         new GameDate(9,11,2026),  Issue.None,                 SourceRegion.RegionB),
                new Sample("Eggs",          new GameDate(3,11,2026)),
                new Sample("meat5",         new GameDate(12,11,2026), Issue.Pollution),
                new Sample("cheese4",       new GameDate(15,11,2026), Issue.Recall,               SourceRegion.RegionB),
                new Sample("Strawberry",    new GameDate(7,11,2026),  Issue.None,                 SourceRegion.RegionB),
                new Sample("Watermelon",    new GameDate(6,11,2026),  Issue.Spoiled),
                new Sample("Strawberry_01", new GameDate(11,11,2026)),
                new Sample("milk5",         new GameDate(2,11,2026),  Issue.Expired),
                new Sample("cheese5",       new GameDate(18,11,2026)),
                new Sample("Watermelon_01", new GameDate(10,11,2026), Issue.Pollution),
                new Sample("milk6",         new GameDate(8,11,2026)),
                new Sample("Yoghurt_03",    new GameDate(28,10,2026), Issue.Spoiled | Issue.Expired),
                new Sample("cheese6",       new GameDate(16,11,2026))
            }
        };
        internal static readonly string[] Sections={"AÇILAN GRUPLAR / ET VE SÜT ÜRÜNLERİ","YENİ GRUP / MEYVELER","YENİ GRUP / YUMURTA — KARMA DENETİM"};
        internal static readonly string[] Captions={
            "Bugün et ve süt ürünleri denetleniyor. SKT, etiket ve görünür bozulmayı birlikte kontrol edin.",
            "Meyveler bugün açılıyor. Temiz görünüm, çevresel kalıntı bulunmadığını tek başına kanıtlamaz.",
            "Yumurta bugün açılıyor. Kaynak etiketi ve Bakanlık duyurusu karar sürecine ekleniyor."
        };
        internal static readonly string[][] PhotoModels={new[]{"milk1","cheese1","Yoghurt_01","meat2"},
            new[]{"Cantaloupe","Papaya","Pineapple","Strawberry","Watermelon"},new[]{"Eggs","milk4","cheese4","meat5"}};

        [MenuItem("GameUP32/Authoring/Rebuild category campaign assets")]
        public static void Create()
        {
            foreach(var folder in new[]{Root,Root+"/Products",Root+"/Days",Root+"/Rules",Root+"/Documents"})Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var expiry=Rule("Expiry",RuleIds.Expiry,RuleType.ExpiryDate,"SKT","SKT bugünden eskiyse reddet. Bugünün SKT'si geçerlidir.");
            var identity=Rule("Identity",RuleIds.Identity,RuleType.IdentityMismatch,"Etiket","Ön yüzdeki tür beyanı ile arka etiket uyuşmuyorsa reddet.");
            var spoilage=Rule("Spoilage",RuleIds.Spoilage,RuleType.VisibleSpoilage,"Görünür bozulma","Küf, belirgin renk bozulması veya şişme varsa reddet.");
            var pollution=Rule("Analysis",RuleIds.Pollution,RuleType.PollutionAnalysis,"Analiz","Gizli kirlilik etiketle anlaşılmaz. Analiz POZİTİF ise reddet; ambalajı ayrı imhaya gönder.");
            var recall=Asset<RuleDefinition>(Root+"/Rules/Recall.asset");
            recall.InitializeRule(RuleIds.BatchRecall,RuleType.SourceRegionBan,false,new[]{ProductCategory.Dairy,ProductCategory.Meat,ProductCategory.Eggs},
                "Soğuk zincir geri çağırması","12.00 sonrası Bölge B kaynaklı et, süt ve yumurtayı reddet. Meyveler bu duyurunun dışında.","Geri çağrılan kaynak partisi",SourceRegion.RegionB);
            EditorUtility.SetDirty(recall);
            string[] titles={"Küresel ısınma, sofraların geleceğini tehdit ediyor","Atığı azalt; hasadı ve kıyıyı birlikte koru","Soğuk zincir ve kaynak takibi birlikte denetleniyor"};
            string[] bodies={
                "İklim krizi yalnızca yaz aylarının konusu değil. Değişen yağışlar, kuraklık ve aşırı hava olayları dünya genelinde gıda üretimini etkiliyor. Kasım ayında COP31'e hazırlanan Antalya'da sofranın yarını konuşuluyor.\n\nBugün masada et ve süt ürünleri var. SKT, etiket ve görünür bozulmayı inceleyin. Atığı kaynağında ayırmak, kıyıları ve caretta carettaların yaşam alanlarını korumaya da yardımcı olur.",
                "Meyveler bugün denetim kapsamına açılıyor. Küf ve renk kaybı görülebilir; çevresel kalıntılar laboratuvar analiziyle saptanır. Meyvelere, daha önce açılan et ve süt ürünleri de eşlik ediyor.\n\nReddedilen gıdanın ambalajını doğru hedefe gönderin. Atığın kıyıya ulaşmasını önlemek, caretta carettaların yaşam alanlarını korur. Temiz görünen bir ürünün analizini de kontrol edin.",
                "Yumurta grubu bugün açılıyor; et, süt ve meyvelerle karma vardiya başlıyor. İklim dayanıklılığı, enerjinin ve soğuk zincirin sürekliliğini de gündeme getiriyor. SKT ve bozulma kontrollerini sürdürün.\n\nKaynak etiketini ve analiz raporunu eşleştirin. Bakanlık yeni bir parti duyurusu gönderebilir. Acil faksı onayladığınızda yeni kural sonraki ürünlere uygulanır; önceki kararlar değişmez."
            };
            var emergency=Asset<DocumentDefinition>(Root+"/Documents/EmergencyRecall.asset");
            emergency.InitializeDocument("DOC_RECALL",DocumentType.EmergencyFax,"Bakanlık: Bölge B soğuk zincir duyurusu",
                "Bölge B sevkiyat deposunda soğuk zincir kesintisi bildirildi. Saat 12.00'den sonra gelen B kaynaklı ET, SÜT ve YUMURTA ürünleri, analiz temiz olsa bile tedbiren reddedilecektir.\n\nMEYVELER bu duyurunun kapsamında değildir. Önceki kararlar değiştirilmez.",
                "Kaynak B + et/süt/yumurta = RED. Kaynak B meyvesi için normal kontrolleri uygula.",true,new[]{RuleIds.BatchRecall});
            EditorUtility.SetDirty(emergency);
            var days=new List<DayDefinition>();var manifest=new StringBuilder("# Aktif kampanya / haber ve ürün matrisi\n\n3 gün, Esmanur'un 31 kaynak modeli. Bozulma görünümü Editor koduyla türetilir; kararlar ProductRuleEvaluator tarafından verilir.\n\n| Gün / ürün | Ürün adı | Kaynak model | SKT | Test beklentisi | Sebep |\n|---|---|---|---|---|---|\n");
            for(int d=0;d<Plan.Length;d++)
            {
                var photo=AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Documents/Day{d+1}Products.png");
                if(!photo)throw new InvalidOperationException("Generate campaign press photographs before authoring assets.");
                var news=Asset<DocumentDefinition>($"{Root}/Documents/News_{d+1}.asset");
                news.InitializeDocument("DOC_NEWS_"+(d+1),DocumentType.Newspaper,titles[d],bodies[d],Sections[d],true,
                    d==0?new[]{RuleIds.Expiry,RuleIds.Identity,RuleIds.Spoilage}:new[]{RuleIds.Expiry,RuleIds.Identity,RuleIds.Spoilage,RuleIds.Pollution},photo);
                EditorUtility.SetDirty(news);
                var products=new List<ProductDefinition>();
                for(int i=0;i<Plan[d].Length;i++)
                {
                    var spec=Plan[d][i];string id=$"D{d+1}-{i+1:00}";
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(RoomAuthoring.ArtistRoot+spec.Model+".prefab");
                    if(!source)throw new InvalidOperationException("Missing artist model: "+spec.Model);
                    var category=Category(spec.Model);
                    string name=ProductName(spec.Model);
                    bool spoiled=spec.Issues.HasFlag(Issue.Spoiled),polluted=spec.Issues.HasFlag(Issue.Pollution),mislabel=spec.Issues.HasFlag(Issue.Mislabel);
                    var packaging=PackagingFor(spec.Model, category);
                    string declaredType = DeclaredTypeFor(spec.Model, name);
                    string listedType = mislabel ? "Bitkisel Yağlı Sütlü İçecek" : declaredType;
                    string ingredients = mislabel
                        ? "Su, yağsız süt tozu, bitkisel palm yağı, emülgatör, aroma verici."
                        : Ingredients(spec.Model, category);
                    var product=Asset<ProductDefinition>($"{Root}/Products/{id}.asset");
                    string packagingLabel = PackagingDescriptionFor(spec.Model, packaging);
                    string finding = BuildAnalysisFinding(name, spec.Model, category, spec.Skt, spec.Region, declaredType, listedType, ingredients, spoiled, polluted, mislabel);
                    product.InitializeData(id,category,name,spec.Skt,declaredType,
                        listedType,ingredients,spec.Region,FishHabitat.None,polluted,
                        finding,
                        packaging,polluted,packagingLabel);
                    Set(product,"modelSource",source);var so=new SerializedObject(product);so.FindProperty("condition").enumValueIndex=spoiled?1:0;so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(product);
                    products.Add(product);manifest.AppendLine($"| {d+1} / {id} | {name} | {spec.Model} | {spec.Skt} | {(spec.Issues==Issue.None?"ONAY":"RED")} | {spec.Issues} |");
                }
                var rules=new List<RuleDefinition>{expiry,identity,spoilage};if(d>0)rules.Add(pollution);
                var day=Asset<DayDefinition>($"{Root}/Days/Day_{d+1:00}.asset");
                day.InitializeDay("DAY_"+(d+1).ToString("00"),d+1,Sections[d],new GameDate(d+1,11,2026),
                    d==0?ProductCategory.Dairy:d==1?ProductCategory.Fruit:ProductCategory.Eggs,news,rules,products,
                    d==2?new EmergencyDefinition(true,3,"12.00",recall,emergency):new EmergencyDefinition(),
                    d==0?new[]{ProductCategory.Dairy,ProductCategory.Meat}:d==1?new[]{ProductCategory.Fruit}:new[]{ProductCategory.Eggs});
                EditorUtility.SetDirty(day);days.Add(day);
            }
            var catalog=Asset<CampaignDefinition>(CatalogPath);var catalogSo=new SerializedObject(catalog);var list=catalogSo.FindProperty("days");list.arraySize=days.Count;
            for(int i=0;i<days.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=days[i];catalogSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs");File.WriteAllText("Docs/CAMPAIGN_MATRIX.md",manifest.ToString(),new UTF8Encoding(false));
            Debug.Log("CAMPAIGN_ASSETS_OK days=3 products=31 artistModels=31 expectedRejects=15 optimalScore=340");
        }
        private static RuleDefinition Rule(string filename,string id,RuleType type,string title,string text)
        {
            var rule=Asset<RuleDefinition>($"{Root}/Rules/{filename}.asset");rule.InitializeRule(id,type,true,Array.Empty<ProductCategory>(),title,text,title);EditorUtility.SetDirty(rule);return rule;
        }
        internal static T Asset<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        internal static ProductCategory Category(string model)=>model.StartsWith("meat",StringComparison.Ordinal)?ProductCategory.Meat:
            model=="Eggs"?ProductCategory.Eggs:model.StartsWith("milk",StringComparison.Ordinal)||model.StartsWith("cheese",StringComparison.Ordinal)||model.StartsWith("Yoghurt",StringComparison.Ordinal)?ProductCategory.Dairy:ProductCategory.Fruit;

        private static WasteBinType PackagingFor(string model, ProductCategory category) => model switch
        {
            "milk1" or "milk2" or "milk4" or "milk5" => WasteBinType.Paper,
            "milk3" or "milk6" => WasteBinType.Glass,
            "cheese1" or "cheese4" => WasteBinType.Paper,
            "cheese2" or "cheese3" => WasteBinType.Glass,
            "cheese5" or "cheese6" => WasteBinType.Plastic,
            "Yoghurt_01" or "Yoghurt_03" => WasteBinType.Glass,
            "Yoghurt_02" => WasteBinType.Plastic,
            "Eggs" => WasteBinType.Paper,
            "Cantaloupe" or "Cantaloupe_01" => WasteBinType.Plastic,
            "Papaya" => WasteBinType.Plastic,
            "Papaya_01" => WasteBinType.Paper,
            "Pineapple" => WasteBinType.Paper,
            "Pineapple_01" => WasteBinType.Plastic,
            "Strawberry" => WasteBinType.Plastic,
            "Strawberry_01" => WasteBinType.Glass,
            "Watermelon" => WasteBinType.Paper,
            "Watermelon_01" => WasteBinType.Plastic,
            _ => WasteBinType.Plastic
        };

        private static string PackagingDescriptionFor(string model, WasteBinType bin) => model switch
        {
            "milk1" or "milk2" or "milk4" or "milk5" => "Tetrapak Karton Süt Kutusu — Kâğıt Atık",
            "milk3" or "milk6" => "Cam Süt Şişesi — Cam Atık",
            "cheese1" or "cheese4" => "Mumlu Kâğıt / Karton Kutu — Kâğıt Atık",
            "cheese2" or "cheese3" => "Cam Salamura Kavanozu — Cam Atık",
            "cheese5" or "cheese6" => "Vakumlu Plastik Peynir Kabı — Plastik Atık",
            "Yoghurt_01" or "Yoghurt_03" => "Cam Yoğurt Kavanozu — Cam Atık",
            "Yoghurt_02" => "Plastik Yoğurt Kasesi — Plastik Atık",
            "Eggs" => "Karton Yumurta Viyolü — Kâğıt Atık",
            "Cantaloupe" or "Cantaloupe_01" => "Koruyucu Plastik File & Streç — Plastik Atık",
            "Papaya" => "Şeffaf Plastik Meyve Kabı — Plastik Atık",
            "Papaya_01" => "Karton Meyve Kutusu — Kâğıt Atık",
            "Pineapple" => "Karton Sevkiyat Viyolü — Kâğıt Atık",
            "Pineapple_01" => "Plastik Koruma Paketi — Plastik Atık",
            "Strawberry" => "Şeffaf Plastik Çilek Kabı — Plastik Atık",
            "Strawberry_01" => "Cam Muhafaza Kavanozu — Cam Atık",
            "Watermelon" => "Oluklu Karton Sevkiyat Kutusu — Kâğıt Atık",
            "Watermelon_01" => "Streçli Plastik Sevkiyat Paketi — Plastik Atık",
            _ => "Streçli Plastik Et Tabağı — " + bin.ToTurkishText()
        };

        private static string ProductName(string model) => model switch
        {
            "milk1"         => "Antalya Günlük Tam Yağlı Süt",
            "milk2"         => "Toros Doğal Çiftlik Sütü",
            "milk3"         => "Korkuteli Pastörize Keçi Sütü",
            "milk4"         => "Akseki Yayla Günlük Sütü",
            "milk5"         => "Elmalı UHT Yarım Yağlı Süt",
            "milk6"         => "Döşemealtı Organik Çiftlik Sütü",
            "cheese1"       => "Antalya Yörük Beyaz Peyniri",
            "cheese2"       => "Akseki Tam Yağlı Tulum Peyniri",
            "cheese3"       => "Korkuteli Köy Kaşar Peyniri",
            "cheese4"       => "Toros Keçi Ezine Peyniri",
            "cheese5"       => "Elmalı Geleneksel Lor Peyniri",
            "cheese6"       => "Manavgat Taze Otlu Peyniri",
            "Yoghurt_01"    => "Korkuteli Kaymaklı Ev Yoğurdu",
            "Yoghurt_02"    => "Antalya Süzme Çiftlik Yoğurdu",
            "Yoghurt_03"    => "Toros Doğal Kase Yoğurdu",
            "meat1"         => "Antalya Taze Dana Kıyma",
            "meat2"         => "Toros Kuzu Kuşbaşı Et",
            "meat3"         => "Korkuteli Dana Antrikot",
            "meat4"         => "Elmalı Kasap Köftelik Kıyma",
            "meat5"         => "Akdeniz Dinlendirilmiş Dana Biftek",
            "Cantaloupe"    => "Finike Kırkağaç Kavunu",
            "Cantaloupe_01" => "Kumluca Sera Kavunu",
            "Papaya"        => "Alanya Tropikal Papaya",
            "Papaya_01"     => "Gazipaşa Bahçe Papayası",
            "Pineapple"     => "Gazipaşa Sera Ananası",
            "Pineapple_01"  => "Alanya Tatlı Ananas",
            "Strawberry"    => "Serik Kokulu Tarla Çileği",
            "Strawberry_01" => "Aksu Sera Çileği",
            "Watermelon"    => "Manavgat Çekirdeksiz Karpuz",
            "Watermelon_01" => "Serik Ovaköy Karpuzu",
            "Eggs"          => "Döşemealtı Gezen Tavuk Yumurtası",
            _               => model
        };

        private static string DeclaredTypeFor(string model, string displayName)
        {
            if (model.StartsWith("milk", StringComparison.Ordinal)) return "%100 Pastörize Tam Yağlı Süt";
            if (model.StartsWith("cheese", StringComparison.Ordinal)) return "Geleneksel Tam Yağlı Peynir";
            if (model.StartsWith("Yoghurt", StringComparison.Ordinal)) return "Doğal Fermente Yoğurt";
            if (model.StartsWith("meat", StringComparison.Ordinal)) return "%100 Taze Karkas Et Ürünleri";
            if (model == "Eggs") return "A Sınıfı Doğal Köy Yumurtası";
            return "Taze Hasat Meyve (" + displayName + ")";
        }

        private static string Ingredients(string model, ProductCategory category)
        {
            if (model.StartsWith("milk", StringComparison.Ordinal)) return "%100 pastörize inek/keçi sütü (koruyucu içermez).";
            if (model.StartsWith("cheese", StringComparison.Ordinal)) return "Pastörize süt, peynir mayası, kaya tuzu.";
            if (model.StartsWith("Yoghurt", StringComparison.Ordinal)) return "Pastörize tam yağlı süt, canlı yoğurt kültürü.";
            if (category == ProductCategory.Meat) return "%100 soğuk zincir dana/kuzu eti (katkı maddesi içermez).";
            if (category == ProductCategory.Eggs) return "%100 doğal kabuklu gezen tavuk yumurtası.";
            return "%100 yerel hasat taze meyve.";
        }

        private static string BuildAnalysisFinding(
            string name, string model, ProductCategory category, GameDate skt, SourceRegion region,
            string declaredType, string listedType, string ingredients, bool spoiled, bool polluted, bool mislabel)
        {
            var sb = new StringBuilder();
            if (polluted)
            {
                string contaminant = category == ProductCategory.Fruit
                    ? "yasaklı organofosfat pestisit (tarım ilacı) ve ağır metal kalıntısı"
                    : "endüstriyel mikroplastik ve kimyasal dioksin bulaşması";
                sb.AppendLine("• ÇEVRESEL KALINTI TESTİ: POZİTİF (TEHLİKELİ) — " + name + " numunesinde " + contaminant + " saptandı! Ürün KESİN RED edilmeli ve ambalajı KIRMIZI (Kontamine Atık) kutusuna atılmalıdır.");
            }
            else
            {
                sb.AppendLine("• ÇEVRESEL KALINTI TESTİ: NEGATİF (TEMİZ) — " + name + " numunesinde pestisit, ağır metal veya mikroplastik kalıntısına rastlanmadı.");
            }

            if (spoiled)
            {
                sb.AppendLine("• MİKROBİYOLOJİK / FİZİKSEL ANALİZ: UYGUNSUZ — Numunede ileri derecede küflenme, renk bozulması ve doku yıkımı tespit edildi (RED).");
            }
            else
            {
                sb.AppendLine("• MİKROBİYOLOJİK / FİZİKSEL ANALİZ: UYGUN — Numunenin tazelik, renk ve doku değerleri standartlara uygun.");
            }

            if (mislabel)
            {
                sb.AppendLine("• İÇERİK & TÜR DOĞRULAMASI: UYUMSUZ (TAKLİT / TAĞŞİŞ) — Ön etiket beyanı ('" + declaredType + "') ile gerçek içerik ('" + listedType + "') uyuşmuyor (RED).");
            }
            else
            {
                sb.AppendLine("• İÇERİK & TÜR DOĞRULAMASI: DOĞRULANDI — İçerik (" + ingredients + ") ve yasal tür beyanı ('" + listedType + "') birbiriyle uyumlu.");
            }

            sb.Append("• KAYNAK & SKT KAYDI: Kaynak Bölgesi: ").Append(region.ToTurkishText())
              .Append("  |  Etiket SKT: ").Append(skt)
              .Append(" (Vardiya tarihi ve yürürlükteki faks duyurularıyla karşılaştırın).");
            return sb.ToString();
        }
    }
}
