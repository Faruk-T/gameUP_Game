using System.Collections.Generic;
using UnityEngine;

namespace GameUP.Contracts
{
    [CreateAssetMenu(menuName = "GameUP32/Data/Campaign", fileName = "Campaign")]
    public sealed class CampaignDefinition : ScriptableObject
    {
        [SerializeField] private List<DayDefinition> days = new List<DayDefinition>();
        public IReadOnlyList<DayDefinition> Days => days;
    }
}
