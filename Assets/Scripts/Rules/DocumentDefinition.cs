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
}
