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
            internal readonly string Model;internal readonly Issue Issues;internal readonly SourceRegion Region;
            internal Sample(string model,Issue issue=Issue.None,SourceRegion region=SourceRegion.AntalyaLocal){Model=model;Issues=issue;Region=region;}
        }
        private static readonly Sample[][] Plan={
            new[]{new Sample("milk1"),new Sample("meat1",Issue.Spoiled),new Sample("cheese1",Issue.Expired),new Sample("Yoghurt_01"),
                new Sample("meat2"),new Sample("milk2",Issue.Mislabel),new Sample("cheese2",Issue.Spoiled),new Sample("meat3")},
            new[]{new Sample("Cantaloupe"),new Sample("Papaya",Issue.Spoiled),new Sample("Pineapple"),new Sample("Cantaloupe_01",Issue.Pollution),
                new Sample("Papaya_01"),new Sample("Pineapple_01",Issue.Spoiled),new Sample("milk3"),new Sample("cheese3",Issue.Expired),new Sample("Yoghurt_02"),new Sample("meat4",Issue.Pollution)},
            new[]{new Sample("milk4",region:SourceRegion.RegionB),new Sample("Eggs"),new Sample("meat5",Issue.Pollution),
                new Sample("cheese4",Issue.Recall,SourceRegion.RegionB),new Sample("Strawberry",region:SourceRegion.RegionB),new Sample("Watermelon",Issue.Spoiled),
                new Sample("Strawberry_01"),new Sample("milk5",Issue.Expired),new Sample("cheese5"),new Sample("Watermelon_01",Issue.Pollution),
                new Sample("milk6"),new Sample("Yoghurt_03",Issue.Spoiled),new Sample("cheese6")}
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
            var days=new List<DayDefinition>();var manifest=new StringBuilder("# Aktif kampanya / haber ve ürün matrisi\n\n3 gün, Esmanur'un 31 kaynak modeli. Bozulma görünümü Editor koduyla türetilir; kararlar ProductRuleEvaluator tarafından verilir.\n\n| Gün / ürün | Kaynak model | Test beklentisi | Sebep |\n|---|---|---|---|\n");
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
                    var category=Category(spec.Model);string name=ProductName(spec.Model);
                    bool spoiled=spec.Issues.HasFlag(Issue.Spoiled),polluted=spec.Issues.HasFlag(Issue.Pollution);
                    var packaging=category==ProductCategory.Dairy&&(spec.Model.StartsWith("milk",StringComparison.Ordinal)||spec.Model.StartsWith("cheese",StringComparison.Ordinal))
                        ||category==ProductCategory.Fruit&&!spec.Model.EndsWith("_01",StringComparison.Ordinal)||category==ProductCategory.Eggs?WasteBinType.Paper:WasteBinType.Plastic;
                    var product=Asset<ProductDefinition>($"{Root}/Products/{id}.asset");
                    var skt=spec.Issues.HasFlag(Issue.Expired)?(d==0?new GameDate(31,10,2026):new GameDate(d,11,2026)):new GameDate(d+5,11,2026);
                    product.InitializeData(id,category,name,skt,name,
                        spec.Issues.HasFlag(Issue.Mislabel)?"Bitkisel içecek":name,Ingredients(category),spec.Region,FishHabitat.None,polluted,
                        polluted?"POZİTİF — Çevresel kalıntı saptandı. Ambalaj kontamine; ayrı imha gerekir.":"NEGATİF — Numunede çevresel kalıntı saptanmadı. SKT, görünür bozulma ve yürürlükteki parti duyurularını ayrıca kontrol edin.",
                        packaging,spoiled||polluted,packaging.ToTurkishText());
                    Set(product,"modelSource",source);var so=new SerializedObject(product);so.FindProperty("condition").enumValueIndex=spoiled?1:0;so.ApplyModifiedPropertiesWithoutUndo();
                    products.Add(product);manifest.AppendLine($"| {d+1} / {id} | {spec.Model} | {(spec.Issues==Issue.None?"ONAY":"RED")} | {spec.Issues} |");
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
        private static string ProductName(string model)
        {
            if(model.StartsWith("milk",StringComparison.Ordinal))return "Süt / Parti "+model.Substring(4);
            if(model.StartsWith("cheese",StringComparison.Ordinal))return "Peynir / Parti "+model.Substring(6);
            if(model.StartsWith("meat",StringComparison.Ordinal))return "Paketli et / Parti "+model.Substring(4);
            if(model.StartsWith("Yoghurt",StringComparison.Ordinal))return "Yoğurt / Parti "+model.Substring(8);
            if(model=="Eggs")return "Yumurta kolisi";
            string name=model.StartsWith("Cantaloupe",StringComparison.Ordinal)?"Kavun":model.StartsWith("Papaya",StringComparison.Ordinal)?"Papaya":
                model.StartsWith("Pineapple",StringComparison.Ordinal)?"Ananas":model.StartsWith("Strawberry",StringComparison.Ordinal)?"Çilek":"Karpuz";
            return name+(model.EndsWith("_01",StringComparison.Ordinal)?" / B":" / A");
        }
        private static string Ingredients(ProductCategory category)=>category==ProductCategory.Dairy?"Süt, ürüne uygun kültür/maya.":category==ProductCategory.Meat?"Et numunesi.":category==ProductCategory.Eggs?"Kabuklu yumurta.":"Meyve numunesi.";
    }
}
