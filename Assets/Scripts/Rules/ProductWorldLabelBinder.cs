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
        public ProductDefinition ProductDefinition => productDefinition;
        public MeshRenderer[] PrintedSurfaces => printedSurfaces;
        public Bounds LocalBounds => localBounds;
        public float InspectionMultiplier => inspectionMultiplier;
        public Quaternion InspectionRotation => PrintsOnTop(localBounds) ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
        public static bool PrintsOnTop(Bounds bounds) => bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z) * .45f;
        public void Configure(ProductDefinition definition, MeshRenderer[] surfaces, Bounds bounds)
        {
            if (!definition || surfaces == null || surfaces.Length == 0) throw new InvalidOperationException("Product printing requires original model surfaces.");
            productDefinition = definition; printedSurfaces = surfaces; localBounds = bounds;
            inspectionMultiplier = .86f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }
    }
}

