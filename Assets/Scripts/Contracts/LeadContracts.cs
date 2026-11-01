using System;
using System.Collections.Generic;
using System.Linq;

namespace GameUp.Contracts
{
    public enum FlowState { Boot, MorningBriefing, Working, EmergencyBriefing, DaySummary, GameSummary, MistakeBriefing, GameOver }
    public enum ProductPhase { None, Arriving, Ready, Analyzing, Routing, AwaitingSort, Sorting, Complete }
    public enum PlayerDecision { Approve, Reject }
    public enum WasteCategory { Paper, Glass, Plastic, Contaminated }
    public enum DocumentKind { Newspaper, Handbook, Analysis, Emergency, Infraction }

    public readonly struct ProductKey : IEquatable<ProductKey>
    {
        public Guid RunId { get; }
        public string ProductId { get; }
        public ProductKey(Guid runId, string productId)
        {
            if (runId == Guid.Empty) throw new ArgumentException("A run ID is required.", nameof(runId));
            if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("A product ID is required.", nameof(productId));
            RunId = runId;
            ProductId = productId;
        }
        public bool Equals(ProductKey other) => RunId == other.RunId && ProductId == other.ProductId;
        public override bool Equals(object obj) => obj is ProductKey other && Equals(other);
        public override int GetHashCode() => (RunId.GetHashCode() * 397) ^ (ProductId?.GetHashCode() ?? 0);
        public override string ToString() => $"{RunId:N}/{ProductId}";
    }

    public sealed class RuleInfo
    {
        public string Id { get; }
        public string Text { get; }
        public RuleInfo(string id, string text) { Id = id; Text = text; }
    }

    public sealed class RuleSnapshot
    {
        public int Version { get; }
        public DateTime GameDate { get; }
        public IReadOnlyList<RuleInfo> Rules { get; }
        public RuleSnapshot(int version, DateTime gameDate, IEnumerable<RuleInfo> rules)
        {
            Version = version;
            GameDate = gameDate.Date;
            Rules = Array.AsReadOnly((rules ?? throw new ArgumentNullException(nameof(rules))).ToArray());
        }
    }

    public sealed class ProductInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Label { get; }
        public ProductInfo(string id, string name, string category, string label)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Product ID is required.");
            Id = id; Name = name; Category = category; Label = label;
        }
    }

    public sealed class EmergencyInfo
    {
        public int AfterCompletedProducts { get; }
        public string Text { get; }
        public RuleInfo AdditionalRule { get; }
        public EmergencyInfo(int afterCompletedProducts, string text, RuleInfo additionalRule)
        {
            if (afterCompletedProducts < 1) throw new ArgumentOutOfRangeException(nameof(afterCompletedProducts));
            AfterCompletedProducts = afterCompletedProducts;
            Text = text;
            AdditionalRule = additionalRule ?? throw new ArgumentNullException(nameof(additionalRule));
        }
    }

    public sealed class DayInfo
    {
        public string Id { get; }
        public string Title { get; }
        public DateTime Date { get; }
        public string Newspaper { get; }
        public IReadOnlyList<ProductInfo> Products { get; }
        public IReadOnlyList<RuleInfo> Rules { get; }
        public EmergencyInfo Emergency { get; }
        public DayInfo(string id, string title, DateTime date, string newspaper,
            IEnumerable<ProductInfo> products, IEnumerable<RuleInfo> rules, EmergencyInfo emergency = null)
        {
            Id = id; Title = title; Date = date.Date; Newspaper = newspaper;
            var items = (products ?? throw new ArgumentNullException(nameof(products))).ToArray();
            if (items.Length == 0 || items.Any(p => p == null) || items.Select(p => p.Id).Distinct().Count() != items.Length)
                throw new ArgumentException("A day needs products with unique IDs.");
            if (emergency != null && emergency.AfterCompletedProducts >= items.Length)
                throw new ArgumentException("An emergency must leave at least one following product.");
            Products = Array.AsReadOnly(items);
            Rules = Array.AsReadOnly((rules ?? throw new ArgumentNullException(nameof(rules))).ToArray());
            Emergency = emergency;
        }
    }

    public sealed class CampaignInfo
    {
        public IReadOnlyList<DayInfo> Days { get; }
        public CampaignInfo(IEnumerable<DayInfo> days)
        {
            var items = (days ?? throw new ArgumentNullException(nameof(days))).ToArray();
            if (items.Length == 0 || items.Any(d => d == null)) throw new ArgumentException("A campaign needs days.");
            if (items.Select(d => d.Id).Distinct().Count() != items.Length) throw new ArgumentException("Day IDs must be unique.");
            var ids = items.SelectMany(d => d.Products).Select(p => p.Id).ToArray();
            if (ids.Distinct().Count() != ids.Length) throw new ArgumentException("Product IDs must be unique across the campaign.");
            Days = Array.AsReadOnly(items);
        }
    }

    public sealed class ProductSession
    {
        public ProductKey Key { get; }
        public string DayId { get; }
        public ProductInfo Product { get; }
        public RuleSnapshot Rules { get; }
        public ProductSession(ProductKey key, string dayId, ProductInfo product, RuleSnapshot rules)
        { Key = key; DayId = dayId; Product = product; Rules = rules; }
    }

    public sealed class AnalysisReport
    {
        public ProductKey Key { get; }
        public string ReportId { get; }
        public string Text { get; }
        public AnalysisReport(ProductKey key, string reportId, string text) { Key = key; ReportId = reportId; Text = text; }
    }

    public sealed class DecisionEvaluation
    {
        public ProductKey Key { get; }
        public PlayerDecision Submitted { get; }
        public PlayerDecision Expected { get; }
        public string Reason { get; }
        public int RuleVersion { get; }
        public bool Correct => Submitted == Expected;
        public DecisionEvaluation(ProductKey key, PlayerDecision submitted, PlayerDecision expected, string reason, int ruleVersion)
        { Key = key; Submitted = submitted; Expected = expected; Reason = reason; RuleVersion = ruleVersion; }
    }

    public sealed class ProductOutcome
    {
        public DecisionEvaluation Decision { get; }
        public bool? SortingCorrect { get; }
        public int Score { get; }
        public ProductOutcome(DecisionEvaluation decision, bool? sortingCorrect, int score)
        { Decision = decision; SortingCorrect = sortingCorrect; Score = score; }
    }

    public sealed class InspectionSummary
    {
        public int Total { get; }
        public int Correct { get; }
        public int FalseAccepts { get; }
        public int FalseRejects { get; }
        public int Sorted { get; }
        public int CorrectSorts { get; }
        public int Score { get; }
        public float Accuracy => Total == 0 ? 0 : (float)Correct / Total;
        public bool Passed => Total > 0 && Accuracy >= 0.75f;
        public InspectionSummary(int total, int correct, int falseAccepts, int falseRejects, int sorted, int correctSorts, int score)
        { Total = total; Correct = correct; FalseAccepts = falseAccepts; FalseRejects = falseRejects; Sorted = sorted; CorrectSorts = correctSorts; Score = score; }
    }

    // Callbacks must be delivered on Unity's main thread by the adapter.
    // Faruk supplies the production implementation; the prototype uses an isolated answer key.
    public interface IInspectionBackend
    {
        void BeginRun(Guid runId);
        void RequestAnalysis(ProductSession session, Action<AnalysisReport> completed);
        DecisionEvaluation Evaluate(ProductSession session, PlayerDecision decision);
        ProductOutcome CommitOutcome(ProductSession session, DecisionEvaluation decision, WasteCategory? waste);
        InspectionSummary GetDaySummary(string dayId);
        InspectionSummary GetRunSummary();
    }

    public interface ICampaignSource { CampaignInfo CreateCampaign(); }

    // Ozgur supplies the production implementation. Invoke each callback after the movement finishes.
    public interface IProductPresentation
    {
        void Present(ProductSession session, Action<ProductKey> arrived);
        void Route(ProductSession session, PlayerDecision decision, Action<ProductKey> finished);
        void Sort(ProductSession session, WasteCategory waste, Action<ProductKey, WasteCategory> finished);
        void ResetPresentation();
    }
}
