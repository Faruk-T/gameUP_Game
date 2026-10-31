using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Interaction
{
    public enum WorkstationAction { Product, Analyzer, Approve, Reject, Paper, Glass, Plastic, Contaminated }

    /// <summary>Add this to a collider to make it a raycast target for the workstation controls.</summary>
    public sealed class WorkstationTarget : MonoBehaviour
    {
        [SerializeField] private WorkstationAction action;
        public WorkstationAction Action => action;

        public WasteBinType WasteBin => action switch
        {
            WorkstationAction.Paper => WasteBinType.Paper,
            WorkstationAction.Glass => WasteBinType.Glass,
            WorkstationAction.Plastic => WasteBinType.Plastic,
            WorkstationAction.Contaminated => WasteBinType.Contaminated,
            _ => WasteBinType.None
        };
    }
}
