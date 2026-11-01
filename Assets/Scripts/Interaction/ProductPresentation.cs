using System;
using System.Collections;
using System.Collections.Generic;
using GameUp.Contracts;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEngine;
using ProductSession = GameUp.Contracts.ProductSession;

namespace GameUP.Interaction
{
    [Serializable]
    public sealed class ProductVisual
    {
        public ProductDefinition Definition;
        public GameObject Visual;
        public ProductWorldLabelBinder Labels;
    }

    /// <summary>Uses an authored pool. No spawning, component creation or substitute models in play.</summary>
    public sealed class ProductPresentation : MonoBehaviour, IProductPresentation
    {
        [SerializeField] private ProductVisual[] visuals;
        [SerializeField] private Transform arrivalPoint, deskPoint, approvedExitPoint, foodDisposalPoint;
        [SerializeField] private GameObject packagingVisual;
        [SerializeField] private Transform[] wastePoints;
        [SerializeField, Min(0.01f)] private float moveDuration = 0.45f;
        private readonly Dictionary<string, ProductVisual> pool = new Dictionary<string, ProductVisual>(StringComparer.Ordinal);
        private ProductVisual active;
        private Coroutine movement;
        public ProductSession CurrentSession { get; private set; }
        public GameObject ActiveProduct => active?.Visual;
        public bool IsMoving => movement != null;

        private void Awake()
        {
            if (visuals == null || visuals.Length == 0 || !arrivalPoint || !deskPoint || !approvedExitPoint ||
                !foodDisposalPoint || !packagingVisual || wastePoints == null || wastePoints.Length != 4)
                throw new InvalidOperationException("ProductPresentation requires its authored pool and workstation points.");
            foreach (var item in visuals)
            {
                if (!item.Definition || !item.Visual || !item.Labels || !pool.TryAdd(item.Definition.DefinitionId, item))
                    throw new InvalidOperationException("Product pool contains missing or duplicate entries.");
                item.Visual.SetActive(false);
            }
            foreach (var target in wastePoints) if (!target) throw new InvalidOperationException("Missing waste point.");
            packagingVisual.SetActive(false);
        }

        public void Present(ProductSession session, Action<ProductKey> arrived)
        {
            ResetPresentation();
            if (!pool.TryGetValue(session.Product.Id, out active)) throw new InvalidOperationException("Product has no authored visual: " + session.Product.Id);
            CurrentSession = session;
            active.Labels.Bind(active.Definition);
            active.Visual.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
            active.Visual.SetActive(true);
            movement = StartCoroutine(Move(active.Visual.transform, deskPoint, () => arrived(session.Key)));
        }

        public void Route(ProductSession session, PlayerDecision decision, Action<ProductKey> finished)
        {
            RequireSession(session);
            movement = StartCoroutine(Move(active.Visual.transform, decision == PlayerDecision.Approve ? approvedExitPoint : foodDisposalPoint, () =>
            {
                active.Visual.SetActive(false);
                if (decision == PlayerDecision.Reject)
                {
                    packagingVisual.transform.SetPositionAndRotation(deskPoint.position, deskPoint.rotation);
                    packagingVisual.SetActive(true);
                }
                finished(session.Key);
            }));
        }

        public void Sort(ProductSession session, WasteCategory waste, Action<ProductKey, WasteCategory> finished)
        {
            RequireSession(session);
            movement = StartCoroutine(Move(packagingVisual.transform, wastePoints[(int)waste], () =>
            { packagingVisual.SetActive(false); finished(session.Key, waste); }));
        }

        private IEnumerator Move(Transform item, Transform destination, Action finished)
        {
            Vector3 start = item.position; Quaternion rotation = item.rotation;
            for (float time = 0; time < moveDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / moveDuration);
                item.SetPositionAndRotation(Vector3.Lerp(start, destination.position, t), Quaternion.Slerp(rotation, destination.rotation, t));
                yield return null;
            }
            item.SetPositionAndRotation(destination.position, destination.rotation);
            movement = null;
            finished();
        }
        private void RequireSession(ProductSession session)
        {
            if (CurrentSession == null || !CurrentSession.Key.Equals(session.Key) || active == null || IsMoving)
                throw new InvalidOperationException("Product presentation session is not ready.");
        }
        public void ResetPresentation()
        {
            if (movement != null) StopCoroutine(movement);
            movement = null;
            if (active != null && active.Visual) active.Visual.SetActive(false);
            if (packagingVisual) packagingVisual.SetActive(false);
            active = null; CurrentSession = null;
        }
        private void OnDisable() => ResetPresentation();
    }
}
