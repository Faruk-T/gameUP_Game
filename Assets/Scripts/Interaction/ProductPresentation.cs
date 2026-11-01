using System;
using System.Collections;
using System.Collections.Generic;
using GameUp.Contracts;
using GameUP.Contracts;
using GameUP.Rules;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private Transform[] intakePath;
        [SerializeField] private GameObject packagingVisual;
        [SerializeField] private Transform[] wastePoints;
        [FormerlySerializedAs("moveDuration"), SerializeField, Min(0.001f)] private float secondsPerMetre = 0.22f;
        private readonly Dictionary<string, ProductVisual> pool = new Dictionary<string, ProductVisual>(StringComparer.Ordinal);
        private ProductVisual active;
        private Coroutine movement;
        public ProductSession CurrentSession { get; private set; }
        public GameObject ActiveProduct => active?.Visual;
        public bool IsMoving => movement != null;

        private void Awake()
        {
            if (visuals == null || visuals.Length == 0 || !arrivalPoint || !deskPoint || !approvedExitPoint ||
                !foodDisposalPoint || !packagingVisual || intakePath == null || intakePath.Length < 2 || wastePoints == null || wastePoints.Length != 4)
                throw new InvalidOperationException("ProductPresentation requires its authored pool and workstation points.");
            foreach (var item in visuals)
            {
                if (!item.Definition || !item.Visual || !item.Labels || !pool.TryAdd(item.Definition.DefinitionId, item))
                    throw new InvalidOperationException("Product pool contains missing or duplicate entries.");
                item.Visual.SetActive(false);
            }
            foreach (var target in wastePoints) if (!target) throw new InvalidOperationException("Missing waste point.");
            foreach (var point in intakePath) if (!point) throw new InvalidOperationException("Missing authored conveyor waypoint.");
            packagingVisual.SetActive(false);
        }

        public void Present(ProductSession session, Action<ProductKey> arrived)
        {
            ResetPresentation();
            if (!pool.TryGetValue(session.Product.Id, out active)) throw new InvalidOperationException("Product has no authored visual: " + session.Product.Id);
            CurrentSession = session;
            active.Visual.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
            active.Visual.SetActive(true);
            movement = StartCoroutine(Arrive(session, arrived));
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

        private IEnumerator Arrive(ProductSession session, Action<ProductKey> arrived)
        {
            foreach (var point in intakePath) yield return Travel(active.Visual.transform, point);
            movement = null; arrived(session.Key);
        }
        private IEnumerator Move(Transform item, Transform destination, Action finished)
        {
            yield return Travel(item, destination);
            movement = null; finished();
        }
        private IEnumerator Travel(Transform item, Transform destination)
        {
            Vector3 start = item.position; Quaternion rotation = item.rotation;
            float duration=Mathf.Max(.05f,Vector3.Distance(start,destination.position)*secondsPerMetre);
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                item.SetPositionAndRotation(Vector3.Lerp(start, destination.position, t), Quaternion.Slerp(rotation, destination.rotation, t));
                yield return null;
            }
            item.SetPositionAndRotation(destination.position, destination.rotation);
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
