using System;
using GameUP.Contracts;
using GameUP.Rules;

namespace GameUp.Editor
{
    internal static class ProductLaboratoryAuthoring
    {
        internal static void Configure(ProductDefinition product)
        {
            string model = product.ModelSource.name;
            var profile = model.StartsWith("milk", StringComparison.Ordinal) ? LaboratoryProfile.Milk
                : model.StartsWith("cheese", StringComparison.Ordinal) ? LaboratoryProfile.Cheese
                : model.StartsWith("Yoghurt", StringComparison.Ordinal) ? LaboratoryProfile.Yogurt
                : product.Category == ProductCategory.Meat ? LaboratoryProfile.Meat
                : product.Category == ProductCategory.Fruit ? LaboratoryProfile.Fruit
                : product.Category == ProductCategory.Eggs ? LaboratoryProfile.Eggs
                : throw new InvalidOperationException("Unsupported laboratory product: " + model);
            string place = model switch
            {
                "milk1" => "Antalya / Aksu", "milk2" => "Antalya / Korkuteli", "milk3" => "Antalya / Korkuteli",
                "milk4" => "Burdur / Bucak", "milk5" => "Antalya / Elmalı", "milk6" => "Antalya / Döşemealtı",
                "cheese1" => "Antalya / Elmalı", "cheese2" => "Antalya / Akseki", "cheese3" => "Antalya / Korkuteli",
                "cheese4" => "Isparta / Eğirdir", "cheese5" => "Antalya / Elmalı", "cheese6" => "Antalya / Manavgat",
                "Yoghurt_01" => "Antalya / Korkuteli", "Yoghurt_02" => "Antalya / Aksu", "Yoghurt_03" => "Antalya / Akseki",
                "meat1" => "Antalya / Kepez", "meat2" => "Antalya / Akseki", "meat3" => "Antalya / Korkuteli",
                "meat4" => "Antalya / Elmalı", "meat5" => "Antalya / Döşemealtı",
                "Cantaloupe" => "Antalya / Finike", "Cantaloupe_01" => "Antalya / Kumluca",
                "Papaya" or "Pineapple" or "Papaya_01" => "Antalya / Gazipaşa",
                "Pineapple_01" => "Antalya / Alanya", "Strawberry" or "Watermelon_01" => "Antalya / Serik",
                "Strawberry_01" => "Antalya / Aksu", "Watermelon" => "Antalya / Manavgat", "Eggs" => "Antalya / Döşemealtı",
                _ => throw new InvalidOperationException("Assign a production place: " + model)
            };
            string type = model switch
            {
                "milk1" or "milk4" => "Pastörize tam yağlı inek sütü", "milk2" => "%100 doğal çiftlik sütü",
                "milk3" => "Pastörize keçi sütü", "milk5" => "UHT yarım yağlı inek sütü", "milk6" => "Organik inek sütü",
                "cheese1" => "İnek sütünden beyaz peynir", "cheese2" => "Keçi sütünden tulum peyniri",
                "cheese3" => "İnek sütünden kaşar peyniri", "cheese4" => "Keçi sütünden salamura peynir",
                "cheese5" => "İnek sütünden lor peyniri", "cheese6" => "Keçi sütünden otlu peynir",
                "Yoghurt_01" => "Kaymaklı inek sütü yoğurdu", "Yoghurt_02" => "Süzme inek sütü yoğurdu", "Yoghurt_03" => "Sade keçi sütü yoğurdu",
                "meat1" or "meat4" => "Dana kıyma", "meat2" => "Kuzu kuşbaşı", "meat3" => "Dana antrikot", "meat5" => "Dana biftek",
                "Cantaloupe" or "Cantaloupe_01" => "Taze kavun", "Papaya" or "Papaya_01" => "Taze papaya",
                "Pineapple" or "Pineapple_01" => "Taze ananas", "Strawberry" or "Strawberry_01" => "Taze çilek",
                "Watermelon" or "Watermelon_01" => "Taze karpuz", "Eggs" => "A sınıfı tavuk yumurtası",
                _ => throw new InvalidOperationException("Assign a legal type: " + model)
            };
            bool mismatch = ProductRuleEvaluator.HasIdentityMismatch(product);
            string listed = mismatch ? product.ListedProductType : type;
            string ingredients = mismatch ? product.IngredientsSummary : profile switch
            {
                LaboratoryProfile.Milk => model == "milk3" ? "%100 keçi sütü." : "%100 inek sütü.",
                LaboratoryProfile.Cheese => (model == "cheese2" || model == "cheese4" || model == "cheese6" ? "Keçi sütü" : "İnek sütü") + ", peynir mayası, tuz." + (model == "cheese6" ? " Yenilebilir ot karışımı." : ""),
                LaboratoryProfile.Yogurt => (model == "Yoghurt_03" ? "Keçi sütü" : "İnek sütü") + ", yoğurt kültürü.",
                LaboratoryProfile.Meat => model == "meat2" ? "%100 kuzu eti." : "%100 dana eti.",
                LaboratoryProfile.Fruit => "%100 " + type.ToLowerInvariant().Replace("taze ", "") + ".",
                LaboratoryProfile.Eggs => "Kabuklu tavuk yumurtası.",
                _ => throw new ArgumentOutOfRangeException()
            };
            product.InitializeData(product.DefinitionId, product.Category, product.DisplayName, product.ExpiryDate, type, listed,
                ingredients, product.SourceRegion, product.Habitat, product.IsPolluted, string.Empty,
                product.PackagingMaterial, product.IsPackagingContaminated, product.PackagingLabelText,
                product.CustomFrontLabelNote, product.CustomBackLabelNote);
            product.InitializeLaboratory(profile, place);
        }
    }
}
