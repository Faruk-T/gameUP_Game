using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUP.Contracts
{
    /// <summary>
    /// Gazete, Kitapçık ve Acil Faks belge tanımı (IMPLEMENTATION.md Bölüm 6.1 &amp; 13).
    /// </summary>
    [CreateAssetMenu(menuName = "GameUP32/Data/Document Definition", fileName = "Doc_")]
    public class DocumentDefinition : ScriptableObject
    {
        [SerializeField] private string documentId = "DOC_DAY1_NEWS";
        [SerializeField] private DocumentType documentType = DocumentType.Newspaper;
        [SerializeField] private string title = "Sıcak hava, gıda taşımacılığını gündeme taşıdı";
        [TextArea(3, 8)]
        [SerializeField] private string body = "";
        [TextArea(2, 4)]
        [SerializeField] private string taskInstructionBox = "";
        [SerializeField] private Sprite illustration;
        [SerializeField] private List<string> relatedRuleIds = new List<string>();
        [Tooltip("True ise Escape ile kapatılamaz, açık onay/devam düğmesi gerekir (Bölüm 8.3).")]
        [SerializeField] private bool requiresExplicitAcknowledge = true;

        public string DocumentId => documentId;
        public DocumentType DocumentType => documentType;
        public string Title => title;
        public string Body => body;
        public string TaskInstructionBox => taskInstructionBox;
        public Sprite Illustration => illustration;
        public IReadOnlyList<string> RelatedRuleIds => relatedRuleIds;
        public bool RequiresExplicitAcknowledge => requiresExplicitAcknowledge;

        public void InitializeDocument(
            string id,
            DocumentType type,
            string docTitle,
            string docBody,
            string taskBox,
            bool explicitAck,
            string[] ruleIds = null,
            Sprite sprite = null)
        {
            documentId = id;
            documentType = type;
            title = docTitle;
            body = docBody;
            taskInstructionBox = taskBox;
            requiresExplicitAcknowledge = explicitAck;
            relatedRuleIds = ruleIds != null ? new List<string>(ruleIds) : new List<string>();
            illustration = sprite;
        }
    }

    /// <summary>
    /// Gün ortası Bakanlık Acil Faksı tanımı (IMPLEMENTATION.md Bölüm 6.1, 10.2 ve 11).
    /// </summary>
    [Serializable]
    public class EmergencyDefinition
    {
        [Tooltip("Acil faks bu gün içinde etkin mi?")]
        [SerializeField] private bool hasEmergency = false;
        [Tooltip("Kaç ürün tamamen (ayrıştırma dahil) tamamlandıktan sonra tetiklenecek? (Gün 2 için: 3)")]
        [SerializeField] private int triggerAfterCompletedProducts = 3;
        [Tooltip("Ekranda gösterilecek kurgu saati (ör. 12.00)")]
        [SerializeField] private string displayedClockTime = "12.00";
        [Tooltip("Onaylandığında devreye girecek yeni kural (R_SOURCE_BAN)")]
        [SerializeField] private RuleDefinition newRule;
        [Tooltip("Sunulacak acil faks belgesi")]
        [SerializeField] private DocumentDefinition emergencyDocument;

        public bool HasEmergency => hasEmergency && newRule != null;
        public int TriggerAfterCompletedProducts => triggerAfterCompletedProducts;
        public string DisplayedClockTime => displayedClockTime;
        public RuleDefinition NewRule => newRule;
        public DocumentDefinition EmergencyDocument => emergencyDocument;

        public EmergencyDefinition() { }

        public EmergencyDefinition(
            bool enabled,
            int triggerCount,
            string clockTime,
            RuleDefinition rule,
            DocumentDefinition document)
        {
            hasEmergency = enabled;
            triggerAfterCompletedProducts = triggerCount;
            displayedClockTime = clockTime;
            newRule = rule;
            emergencyDocument = document;
        }
    }

    /// <summary>
    /// Bir oyun gününün tanımı (IMPLEMENTATION.md Bölüm 6.1 &amp; 10).
    /// </summary>
    [CreateAssetMenu(menuName = "GameUP32/Data/Day Definition", fileName = "Day_")]
    public class DayDefinition : ScriptableObject
    {
        [SerializeField] private string dayId = "DAY_01";
        [SerializeField] private int dayNumber = 1;
        [SerializeField] private string dayTitle = "1. Gün — Süt Ürünleri Denetimi";
        [SerializeField] private GameDate gameDate = new GameDate(1, 11, 2026);
        [SerializeField] private ProductCategory primaryCategory = ProductCategory.Dairy;
        [SerializeField] private DocumentDefinition morningNewspaper;
        [SerializeField] private List<RuleDefinition> initialRules = new List<RuleDefinition>();
        [SerializeField] private List<ProductDefinition> products = new List<ProductDefinition>();
        [SerializeField] private EmergencyDefinition emergencyEvent = new EmergencyDefinition();

        public string DayId => dayId;
        public int DayNumber => dayNumber;
        public string DayTitle => dayTitle;
        public GameDate GameDate => gameDate;
        public ProductCategory PrimaryCategory => primaryCategory;
        public DocumentDefinition MorningNewspaper => morningNewspaper;
        public IReadOnlyList<RuleDefinition> InitialRules => initialRules;
        public IReadOnlyList<ProductDefinition> Products => products;
        public EmergencyDefinition EmergencyEvent => emergencyEvent;

        public void InitializeDay(
            string id,
            int number,
            string title,
            GameDate date,
            ProductCategory category,
            DocumentDefinition newspaper,
            List<RuleDefinition> startRules,
            List<ProductDefinition> dayProducts,
            EmergencyDefinition emergency = null)
        {
            dayId = id;
            dayNumber = number;
            dayTitle = title;
            gameDate = date;
            primaryCategory = category;
            morningNewspaper = newspaper;
            initialRules = startRules ?? new List<RuleDefinition>();
            products = dayProducts ?? new List<ProductDefinition>();
            emergencyEvent = emergency ?? new EmergencyDefinition();
        }
    }
}

