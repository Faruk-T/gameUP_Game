using UnityEngine;

namespace GameUP.Interaction
{
    public enum WorkstationAction { Product, Analyzer, Approve, Reject, Paper, Glass, Plastic, Contaminated, Handbook, Report }

    public sealed class WorkstationTarget : MonoBehaviour
    {
        [SerializeField] private WorkstationAction action;
        public WorkstationAction Action => action;
        public string Hint => action switch
        {
            WorkstationAction.Product => "E / Ürünü incele",
            WorkstationAction.Analyzer => "Sol tık / Analiz cihazı",
            WorkstationAction.Approve => "Sol tık / Ürünü onayla",
            WorkstationAction.Reject => "Sol tık / Ürünü reddet",
            WorkstationAction.Paper => "Sol tık / Kâğıt ambalaj",
            WorkstationAction.Glass => "Sol tık / Cam ambalaj",
            WorkstationAction.Plastic => "Sol tık / Plastik ambalaj",
            WorkstationAction.Contaminated => "Sol tık / Kontamine atık",
            WorkstationAction.Handbook => "B / El kitapçığı",
            WorkstationAction.Report => "T / Son analiz raporu",
            _ => ""
        };
    }
}
