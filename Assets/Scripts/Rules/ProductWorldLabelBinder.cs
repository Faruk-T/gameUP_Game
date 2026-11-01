using System;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>Original product surfaces use Editor-baked UV print atlases; no separate label geometry.</summary>
    public sealed class ProductWorldLabelBinder : MonoBehaviour
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private MeshRenderer[] printedSurfaces;
        [SerializeField] private Bounds localBounds;
        [SerializeField] private float inspectionMultiplier;
        [SerializeField] private string printedLabelSignature;
        public ProductDefinition ProductDefinition => productDefinition;
        public MeshRenderer[] PrintedSurfaces => printedSurfaces;
        public Bounds LocalBounds => localBounds;
        public float InspectionMultiplier => inspectionMultiplier;
        public string PrintedLabelSignature => printedLabelSignature;
        public static string LabelSignature(ProductDefinition product) => Hash128.Compute("uv-print-v4\n" +
            product.GetFormattedFrontLabel() + "\n" + product.GetFormattedBackLabel() + "\n" + product.Condition + "\n" + product.ModelSource.name).ToString();
        public Quaternion InspectionRotation => PrintsOnTop(localBounds) ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
        public static bool PrintsOnTop(Bounds bounds) => bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z) * .45f;
        public void Configure(ProductDefinition definition, MeshRenderer[] surfaces, Bounds bounds)
        {
            if (!definition || surfaces == null || surfaces.Length == 0) throw new InvalidOperationException("Product printing requires original model surfaces.");
            productDefinition = definition; printedSurfaces = surfaces; localBounds = bounds;
            inspectionMultiplier = .86f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            printedLabelSignature = LabelSignature(definition);
        }
    }
}

