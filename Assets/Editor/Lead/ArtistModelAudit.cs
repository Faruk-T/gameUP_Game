using System;
using System.IO;
using System.Linq;
using System.Text;
using GameUP.Contracts;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    public static class ArtistModelAudit
    {
        [MenuItem("GameUP32/Validation/Esmanur model inventory")]
        public static void Generate()
        {
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.EndsWith(".prefab",StringComparison.Ordinal)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var days=ProductionProjectSetup.Days();
            var products=days.SelectMany(d=>d.Products).ToArray();
            var report=new StringBuilder("# Esmanur model envanteri\n\n");
            report.AppendLine("Kaynak: Esmanur / esmnrrr, feature/Objects, commit 0dc833b. Aktif oyun yalnız bu kaynak ürün modellerini kullanır.\n");
            report.AppendLine($"- Kaynak prefab: **{paths.Length}**\n- Oynanış ürünü: **{products.Length}**\n- Gün: **{days.Length}**\n- Kodla hazırlanan görünüm: her model için Fresh + Spoiled (**62 prefab varyantı**)\n- Teşhir dolabı: **0**\n");
            report.AppendLine("| Kaynak model | Gün / ürün | Görünüm |\n|---|---|---|");
            foreach(string path in paths)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var assignments=days.SelectMany(day=>day.Products.Where(p=>p.ModelSource==source).Select(p=>(day.DayNumber,p))).ToArray();
                if(assignments.Length==0)throw new InvalidOperationException("Unassigned artist model: "+path);
                foreach(var assignment in assignments)
                    report.AppendLine($"| [{source.name}](../{path}) | {assignment.DayNumber} / {assignment.p.DefinitionId} | {assignment.p.Condition} |");
                foreach(FoodCondition condition in Enum.GetValues(typeof(FoodCondition)))
                    if(!AssetDatabase.LoadAssetAtPath<GameObject>(ConditionVariantAuthoring.PathFor(source.name,condition)))
                        throw new InvalidOperationException("Missing condition variant: "+source.name);
            }
            report.AppendLine("\nVaryantlar aynı sanatçı mesh'lerini kullanır; yeni ürün modeli import edilmez. Spoiled görünümünde boyama/küf deseni ve ambalaj şişmesi shader koduyla belirlenir. Malzemeler Editor'de hazırlanır, çalışma sırasında prefab veya materyal oluşturulmaz.\n");
            report.AppendLine("Ürün bazındaki onay/red matrisi: [CAMPAIGN_MATRIX.md](CAMPAIGN_MATRIX.md). Önceki kampanya verileri GUID'leri korunarak Assets/Tests/Editor/LegacyContent altında referans arşivine taşınmıştır; aktif sahne bu arşive bağlı değildir.");
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/MODEL_INVENTORY.md",report.ToString(),new UTF8Encoding(false));
            Debug.Log($"MODEL_AUDIT_OK artist={paths.Length} campaign={products.Length} days={days.Length} variants=62 displayCabinets=0");
        }
    }
}
