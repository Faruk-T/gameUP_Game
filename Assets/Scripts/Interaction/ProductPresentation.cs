using System;
using System.Collections;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEngine;

namespace GameUP.Interaction
{
    /// <summary>Spawns the active product and moves it between fixed workstation points.</summary>
    public sealed class ProductPresentation : MonoBehaviour
    {
        [SerializeField] private InspectionService inspectionService;
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] private Transform deskPoint;
        [SerializeField] private Transform approvedExitPoint;
        [SerializeField] private Transform foodDisposalPoint;
        [SerializeField, Min(0f)] private float moveDuration = 0.45f;

        private Coroutine movement;
        private ProductSession activeSession;
        private GameObject activeProduct;

        public ProductSession ActiveSession => activeSession;
        public GameObject ActiveProduct => activeProduct;
        public bool IsMoving { get; private set; }
        public event Action<string, string> ArrivalFinished;

        private void OnEnable()
        {
            if (inspectionService != null) inspectionService.OnProductBegun += HandleProductBegun;
        }

        private void OnDisable()
        {
            if (inspectionService != null) inspectionService.OnProductBegun -= HandleProductBegun;
            if (movement != null) StopCoroutine(movement);
            IsMoving = false;
        }

        private void HandleProductBegun(BeginProductEvent evt)
        {
            if (inspectionService == null || arrivalPoint == null || deskPoint == null || evt.Definition == null)
                return;

            ProductSession session = inspectionService.ActiveProductSession;
            if (session == null || session.ProductId != evt.ProductId || session.RunId != evt.RunId)
                return;

            if (activeProduct != null) Destroy(activeProduct);
            activeSession = session;
            GameObject prefab = session.Definition.Prefab;
            activeProduct = prefab != null
                ? Instantiate(prefab, arrivalPoint.position, arrivalPoint.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            activeProduct.name = session.Definition.DisplayName;
            if (prefab == null)
            {
                activeProduct.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
                activeProduct.transform.localScale = Vector3.one * 0.45f;
            }

            if (activeProduct.GetComponent<WorkstationTarget>() == null)
                activeProduct.AddComponent<WorkstationTarget>();

            ProductWorldLabelBinder binder = activeProduct.GetComponent<ProductWorldLabelBinder>();
            if (binder == null) binder = activeProduct.AddComponent<ProductWorldLabelBinder>();
            binder.Bind(session.Definition);
            string runId = session.RunId;
            string productId = session.ProductId;
            MoveTo(deskPoint, () =>
            {
                if (activeSession != null && activeSession.RunId == runId && activeSession.ProductId == productId)
                    ArrivalFinished?.Invoke(runId, productId);
            });
        }

        public void Route(ProductDecision decision, Action finished)
        {
            Transform destination = decision == ProductDecision.Approve ? approvedExitPoint : foodDisposalPoint;
            if (destination == null) { finished?.Invoke(); return; }
            MoveTo(destination, finished);
        }

        public void Sort(Transform target, Action finished)
        {
            if (target == null) { finished?.Invoke(); return; }
            MoveTo(target, finished);
        }

        public void MoveTo(Transform destination, Action finished)
        {
            if (activeProduct == null || destination == null) { finished?.Invoke(); return; }
            if (movement != null) StopCoroutine(movement);
            movement = StartCoroutine(MoveRoutine(destination, finished));
        }

        private IEnumerator MoveRoutine(Transform destination, Action finished)
        {
            IsMoving = true;
            Vector3 startPosition = activeProduct.transform.position;
            Quaternion startRotation = activeProduct.transform.rotation;
            float elapsed = 0f;
            while (elapsed < moveDuration && activeProduct != null)
            {
                elapsed += Time.deltaTime;
                float t = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
                activeProduct.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, destination.position, t),
                    Quaternion.Slerp(startRotation, destination.rotation, t));
                yield return null;
            }
            if (activeProduct != null) activeProduct.transform.SetPositionAndRotation(destination.position, destination.rotation);
            IsMoving = false;
            movement = null;
            finished?.Invoke();
        }
    }
}
