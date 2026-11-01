using System;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>Authored product label surfaces; text is baked by the Editor from this definition.</summary>
    public sealed class ProductWorldLabelBinder : MonoBehaviour
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private MeshRenderer frontLabelSurface;
        [SerializeField] private MeshRenderer backLabelSurface;
        public ProductDefinition ProductDefinition => productDefinition;
        public MeshRenderer FrontLabelSurface => frontLabelSurface;
        public MeshRenderer BackLabelSurface => backLabelSurface;
        public void Configure(ProductDefinition definition, MeshRenderer front, MeshRenderer back)
        {
            if (!definition || !front || !back) throw new InvalidOperationException("Product labels require a definition and both baked surfaces.");
            productDefinition = definition;frontLabelSurface=front;backLabelSurface=back;
        }
    }
}

