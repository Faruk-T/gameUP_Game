using GameUP.Contracts;
using GameUP.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameUP.Interaction
{
    /// <summary>Play-scene helper: loads the first sample product and advances after each completed outcome.</summary>
    public sealed class InteractionTestBootstrap : MonoBehaviour
    {
        [SerializeField] private InspectionService inspectionService;
        [SerializeField] private WorkstationInteractionController interactionController;

        private DefaultGameContentFactory.GeneratedCatalog catalog;
        private int productIndex;
        private bool outcomeReady;
        private string message = "Test sahnesi hazırlanıyor…";

        private void Start()
        {
            if (inspectionService == null)
            {
                message = "HATA: InspectionService atanmamış.";
                return;
            }

            catalog = DefaultGameContentFactory.CreateCatalogInMemory();
            inspectionService.StartNewRun("INTERACTION-TEST");
            inspectionService.BeginDay(catalog.Day1);
            inspectionService.OnProductCompleted += HandleProductCompleted;
            BeginCurrentProduct();
        }

        private void OnDestroy()
        {
            if (inspectionService != null) inspectionService.OnProductCompleted -= HandleProductCompleted;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || catalog == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Analyzer);
            if (keyboard.f2Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Approve);
            if (keyboard.f3Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Reject);
            if (keyboard.digit1Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Paper);
            if (keyboard.digit2Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Glass);
            if (keyboard.digit3Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Plastic);
            if (keyboard.digit4Key.wasPressedThisFrame) ActivateTarget(WorkstationAction.Contaminated);

            if (!outcomeReady || !keyboard.nKey.wasPressedThisFrame) return;
            productIndex++;
            if (productIndex >= catalog.Day1Products.Count)
            {
                message = "Gün 1 örnek ürünleri tamamlandı. Play'i yeniden başlatarak sıfırlayın.";
                outcomeReady = false;
                return;
            }
            BeginCurrentProduct();
        }

        private void ActivateTarget(WorkstationAction action)
        {
            if (interactionController == null) return;
            WorkstationTarget[] targets = FindObjectsOfType<WorkstationTarget>();
            foreach (WorkstationTarget target in targets)
            {
                if (target.Action == action)
                {
                    interactionController.ActivateTestAction(target);
                    return;
                }
            }
        }

        private void BeginCurrentProduct()
        {
            outcomeReady = false;
            ProductDefinition definition = catalog.Day1Products[productIndex];
            inspectionService.BeginProduct(definition, definition.DefinitionId, productIndex);
            message = $"Test ürünü {productIndex + 1}/{catalog.Day1Products.Count}: {definition.DisplayName}";
        }

        private void HandleProductCompleted(ProductCompletedEvent evt)
        {
            outcomeReady = true;
            message = $"{evt.ProductId} sonucu kaydedildi. Sonraki örnek için N tuşuna basın.";
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 520, 118), "ETKİLEŞİM TEST SAHNESİ");
            GUI.Label(new Rect(30, 48, 490, 78),
                message + "\nFareyle bakış · E/ürün tıkla: incele · cihaz: analiz\nOnay/red düğmesine tıkla · F1 analiz, F2/F3 karar, 1–4 atık");
        }
    }
}
