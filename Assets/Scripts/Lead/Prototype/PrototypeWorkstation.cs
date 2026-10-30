using System;
using System.Collections;
using GameUp.Contracts;
using GameUp.Lead.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameUp.Lead.Prototype
{
    /// <summary>Small replaceable presentation port; Ozgur's interaction prefab replaces this component.</summary>
    public sealed class PrototypeWorkstation : MonoBehaviour, IProductPresentation
    {
        [SerializeField] private Transform product, arrival, desk, exit, disposal;
        [SerializeField] private Transform[] wasteTargets;
        [SerializeField] private Camera workstationCamera;
        [SerializeField] private LeadRuntime runtime;
        [SerializeField, Min(0.01f)] private float travelSeconds = 0.45f;
        private Quaternion initialCameraRotation;
        private float yaw, pitch;
        public void Configure(Transform item, Transform entry, Transform table, Transform approved, Transform trash,
            Transform[] bins, Camera camera, LeadRuntime owner)
        { product = item; arrival = entry; desk = table; exit = approved; disposal = trash; wasteTargets = bins; workstationCamera = camera; runtime = owner; }
        private void Awake() { if (workstationCamera) initialCameraRotation = workstationCamera.transform.rotation; }
        public void Present(ProductSession session, Action<ProductKey> arrived)
        {
            StopAllCoroutines(); product.gameObject.SetActive(true); product.localScale = Vector3.one * 0.28f;
            product.rotation = Quaternion.identity; product.position = arrival.position;
            StartCoroutine(Move(desk.position, () => arrived(session.Key)));
        }
        public void Route(ProductSession session, PlayerDecision decision, Action<ProductKey> finished)
        {
            StartCoroutine(Move(decision == PlayerDecision.Approve ? exit.position : disposal.position, () =>
            {
                product.gameObject.SetActive(decision == PlayerDecision.Reject);
                if (decision == PlayerDecision.Reject) { product.position = desk.position; product.localScale = Vector3.one * 0.16f; }
                finished(session.Key);
            }));
        }
        public void Sort(ProductSession session, WasteCategory waste, Action<ProductKey, WasteCategory> finished) =>
            StartCoroutine(Move(wasteTargets[(int)waste].position, () => { product.gameObject.SetActive(false); finished(session.Key, waste); }));
        private IEnumerator Move(Vector3 destination, Action completed)
        {
            Vector3 start = product.position;
            for (float time = 0; time < travelSeconds; time += Time.unscaledDeltaTime)
            { product.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, time / travelSeconds)); yield return null; }
            product.position = destination;
            completed();
        }
        public void ResetPresentation()
        {
            StopAllCoroutines();
            if (product) product.gameObject.SetActive(false);
            yaw = pitch = 0;
            if (workstationCamera) workstationCamera.transform.rotation = initialCameraRotation;
        }
        private void Update()
        {
            if (!runtime || runtime.Flow == null || !runtime.WorldInputAllowed) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) runtime.Inspect();
                if (keyboard.wKey.wasPressedThisFrame) runtime.Approve();
                if (keyboard.rKey.wasPressedThisFrame) runtime.Reject();
                if (keyboard.fKey.wasPressedThisFrame) runtime.Analyze();
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue();
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse.rightButton.isPressed && !overUi && workstationCamera)
            {
                yaw = Mathf.Clamp(yaw + delta.x * 0.08f, -35, 35);
                pitch = Mathf.Clamp(pitch - delta.y * 0.08f, -25, 25);
                workstationCamera.transform.rotation = initialCameraRotation * Quaternion.Euler(pitch, yaw, 0);
            }
            if (mouse.leftButton.isPressed && !overUi && runtime.ProductInspected && runtime.Flow.CanDecide)
                product.Rotate(Vector3.up, -delta.x * 0.6f, Space.World);
        }
    }
}
