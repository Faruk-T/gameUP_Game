using System;
using System.Text;
using GameUP.Contracts;

namespace GameUP.Rules
{
    /// <summary>Report facts and final disposition use the same product and frozen rules as scoring.</summary>
    public sealed class ProductInspectionReport
    {
        public bool EnvironmentalPositive { get; }
        public bool PhysicalSuitable { get; }
        public bool IdentityVerified { get; }
        public bool Expired { get; }
        public DecisionEvaluation Evaluation { get; }
        public string Text { get; }

        public ProductInspectionReport(ProductDefinition product, RuleSnapshot snapshot)
        {
            if (!product) throw new ArgumentNullException(nameof(product));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (product.LaboratoryProfile == LaboratoryProfile.Unspecified || string.IsNullOrWhiteSpace(product.ProductionPlace))
                throw new InvalidOperationException("Missing laboratory configuration: " + product.DefinitionId);
            Evaluation = ProductRuleEvaluator.EvaluateDecision("report", product.DefinitionId, product, snapshot, ProductDecision.Approve);
            EnvironmentalPositive = product.IsPolluted;
            PhysicalSuitable = product.Condition == FoodCondition.Fresh;
            IdentityVerified = !ProductRuleEvaluator.HasIdentityMismatch(product);
            foreach (var rule in snapshot.ActiveRules)
                if (rule.AppliesToCategory(product.Category) && rule.RuleType == RuleType.IngredientPercentage &&
                    ProductRuleEvaluator.IsRuleViolated(product, snapshot.GameDate, rule, out _)) IdentityVerified = false;
            Expired = product.ExpiryDate < snapshot.GameDate;
            var protocol = Protocol(product.LaboratoryProfile);
            var text = new StringBuilder();
            text.AppendLine("ÜRÜN: " + product.DisplayName + " / " + product.DefinitionId);
            text.AppendLine("DENETİM TARİHİ: " + snapshot.GameDate + "  •  KURAL SÜRÜMÜ: " + snapshot.Version);
            text.AppendLine("NUMUNE PROTOKOLÜ: " + protocol.name);
            text.AppendLine();
            bool reject = Evaluation.ExpectedDecision == ProductDecision.Reject;
            text.AppendLine("DENETİM SONUCU: " + (reject ? "UYGUNSUZ / RED" : "UYGUN / ONAY"));
            if (reject) foreach (var reason in Evaluation.FailureReasons) text.AppendLine("• " + reason);
            else text.AppendLine("Numune, SKT, etiket ve bu ürün için geçerli tüm bakanlık kuralları uygundur.");
            text.AppendLine();
            text.AppendLine("ÇEVRESEL KALINTI TESTİ: " + (EnvironmentalPositive ? "POZİTİF" : "NEGATİF"));
            text.AppendLine(protocol.residue + (EnvironmentalPositive ? " kalıntısı saptandı." : " kalıntısı saptanmadı."));
            text.AppendLine();
            text.AppendLine("MİKROBİYOLOJİK / FİZİKSEL ANALİZ: " + (PhysicalSuitable ? "UYGUN" : "UYGUNSUZ"));
            text.AppendLine(PhysicalSuitable ? protocol.normal + " SKT uygunluğu aşağıda ayrıca değerlendirilmiştir." : protocol.spoiled);
            text.AppendLine();
            text.AppendLine("İÇERİK & TÜR DOĞRULAMASI: " + (IdentityVerified ? "DOĞRULANDI" : "DOĞRULANMADI"));
            text.AppendLine("Beyan: " + product.DeclaredProductType + "; saptanan tür: " + product.ListedProductType + ".");
            text.AppendLine("İçerik: " + product.IngredientsSummary);
            text.AppendLine();
            text.AppendLine("KAYNAK & SKT KAYDI");
            text.AppendLine("Üretim yeri: " + product.ProductionPlace + "  •  Sevkiyat kaynağı: " + product.SourceRegion.ToTurkishText());
            text.AppendLine("Etiket SKT: " + product.ExpiryDate + "  •  Vardiya: " + snapshot.GameDate);
            string dateStatus = Expired ? "GEÇMİŞ — piyasaya sürülemez."
                : product.ExpiryDate.Equals(snapshot.GameDate) ? "BUGÜN SON GÜN — günün sonuna kadar geçerli."
                : "GEÇERLİ — vardiya tarihinden sonra.";
            text.AppendLine("SKT DURUMU: " + dateStatus);
            text.AppendLine();
            text.AppendLine("RED SONRASI AMBALAJ: " + ProductRuleEvaluator.GetExpectedWasteBin(product).ToTurkishText());
            text.AppendLine(product.IsPackagingContaminated ? "Ambalaj kontamine; ayrı imha gerekir." : product.PackagingLabelText);
            Text = text.ToString().TrimEnd();
        }

        private static (string name, string residue, string normal, string spoiled) Protocol(LaboratoryProfile profile) => profile switch
        {
            LaboratoryProfile.Milk => ("Süt / soğuk zincir", "Süt numunesinde çevresel kimyasal / ağır metal", "Numunenin koku, homojenlik ve mikrobiyolojik kontrolü uygun.", "Numunede kesilme, anormal koku veya ambalaj şişmesi saptandı."),
            LaboratoryProfile.Cheese => ("Peynir / olgunlaşma ve muhafaza", "Peynir numunesinde çevresel kimyasal / ağır metal", "Peynirin doku, yüzey ve mikrobiyolojik kontrolü uygun.", "Peynirde istenmeyen küf, doku bozulması veya renk değişimi saptandı."),
            LaboratoryProfile.Yogurt => ("Yoğurt / fermentasyon ve muhafaza", "Yoğurt numunesinde çevresel kimyasal / ağır metal", "Fermente numunenin kıvam ve muhafaza kontrolü uygun.", "Yoğurtta istenmeyen küf veya anormal gaz oluşumu / şişme saptandı."),
            LaboratoryProfile.Meat => ("Et / muhafaza ve tür", "Et numunesinde çevresel toksin / kimyasal", "Etin renk, koku ve mikrobiyolojik kontrolü uygun.", "Ette renk kaybı, anormal koku veya yüzey bozulması saptandı."),
            LaboratoryProfile.Fruit => ("Meyve / yüzey ve kalıntı", "Meyve numunesinde pestisit / ağır metal", "Meyvenin doku ve yüzey kontrolü uygun.", "Meyvede çürüme, küf veya doku yıkımı saptandı."),
            LaboratoryProfile.Eggs => ("Yumurta / kabuk ve muhafaza", "Yumurta numunesinde çevresel kimyasal / ağır metal", "Kabuk bütünlüğü ve numune kontrolü uygun.", "Kabuk veya numune bütünlüğünde bozulma saptandı."),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
    }
}
