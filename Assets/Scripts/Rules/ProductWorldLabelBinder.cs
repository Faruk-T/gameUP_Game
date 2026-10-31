using System;
using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUP.Rules
{
    /// <summary>Fills authored label faces; never creates UI or substitutes product data.</summary>
    public sealed class ProductWorldLabelBinder : MonoBehaviour
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private Text frontLabelText;
        [SerializeField] private Text backLabelText;
        public ProductDefinition ProductDefinition => productDefinition;
        public void Configure(ProductDefinition definition, Text front, Text back)
        { productDefinition = definition; frontLabelText = front; backLabelText = back; Bind(definition); }
        public void Bind(ProductDefinition definition)
        {
            if (!definition || !frontLabelText || !backLabelText) throw new InvalidOperationException("Product labels require a definition and both authored text faces.");
            productDefinition = definition;
            frontLabelText.text = definition.GetFormattedFrontLabel();
            backLabelText.text = definition.GetFormattedBackLabel();
        }
    }
}

