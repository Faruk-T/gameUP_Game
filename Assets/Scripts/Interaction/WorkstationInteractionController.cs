using System;
using GameUp.App;
using GameUp.Contracts;
using GameUp.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameUP.Interaction
{
    /// <summary>The sole gameplay input owner. Judgment and progression stay in GameSessionController.</summary>
    public sealed class WorkstationInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraYawRoot, inspectionAnchor;
        [SerializeField] private GameSessionController session;
        [SerializeField] private ProductPresentation presentation;
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private float rayDistance = 5, pitchLimit = 55, maxInspectionZoom = 0.25f;
        private WorkstationTarget hovered;
        private UserSettings settings;
        private bool inspecting, ignoreMouseDelta = true, dragging, pressedWhileInspecting;
        private float yaw, pitch, zoom;
        private Vector2 pressPosition;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        public bool IsInspecting => inspecting;

        private void Awake()
        {
            if (!playerCamera || !cameraYawRoot || !inspectionAnchor || !session || !presentation)
                throw new InvalidOperationException("Workstation input requires all scene references.");
            settings = ApplicationRoot.Require().Settings;
            yaw = cameraYawRoot.localEulerAngles.y;
            pitch = playerCamera.transform.localEulerAngles.x;
            if (pitch > 180) pitch -= 360;
        }
        private void OnEnable()
        {
            if (session) session.Refreshed += OnSessionChanged;
            if (settings != null) { settings.Changed += ApplySettings; ApplySettings(); }
        }
        private void OnDisable()
        {
            if (session) session.Refreshed -= OnSessionChanged;
            if (settings != null) settings.Changed -= ApplySettings;
        }
        private void ApplySettings() => playerCamera.fieldOfView = settings.FieldOfView;
        private void OnSessionChanged()
        {
            ignoreMouseDelta = true;
            if (inspecting && presentation.CurrentSession == null) inspecting = false;
            if (!session.WorldInputAllowed) { hovered = null; session.SetTargetHint(""); }
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { session.HandleEscape(); return; }
            if (!session.WorldInputAllowed || mouse == null) return;
            if (keyboard != null)
            {
                if (keyboard.bKey.wasPressedThisFrame) { EndInspection(); session.OpenHandbook(); return; }
                if (keyboard.tKey.wasPressedThisFrame) { EndInspection(); session.ReopenReport(); return; }
                if (keyboard.eKey.wasPressedThisFrame && (inspecting || hovered != null && hovered.Action == WorkstationAction.Product)) ToggleInspection();
                if (keyboard.wKey.wasPressedThisFrame) Decide(PlayerDecision.Approve);
                if (keyboard.rKey.wasPressedThisFrame) Decide(PlayerDecision.Reject);
                if (keyboard.fKey.wasPressedThisFrame) { EndInspection(); session.Analyze(); return; }
            }
            if (!session.WorldInputAllowed) return;
            if (inspecting && mouse.rightButton.wasPressedThisFrame) { EndInspection(); return; }

            Vector2 delta = mouse.delta.ReadValue();
            if (ignoreMouseDelta) { ignoreMouseDelta = false; delta = Vector2.zero; }
            delta = Vector2.ClampMagnitude(delta, 100);
            if (!(inspecting && mouse.leftButton.isPressed))
            {
                yaw += delta.x * settings.Sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * settings.Sensitivity, -pitchLimit, pitchLimit);
                cameraYawRoot.localRotation = Quaternion.Euler(0, yaw, 0);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            WorkstationTarget target = null;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, rayDistance, interactableLayers, QueryTriggerInteraction.Ignore))
                target = hit.collider.GetComponentInParent<WorkstationTarget>();
            if (hovered != target)
            {
                hovered = target;
                session.SetTargetHint(target ? target.Hint : (inspecting ? "Sol tık basılı tut + sürükle: Döndür  •  Tekerlek: Yakınlaş  •  E / Sağ tık: Bırak" : ""));
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                pressedWhileInspecting = inspecting;
                pressPosition = mouse.position.ReadValue();
                dragging = false;
                if (!inspecting && hovered)
                {
                    Interact(hovered);
                }
                else if (inspecting && hovered && hovered.Action != WorkstationAction.Product)
                {
                    EndInspection();
                    Interact(hovered);
                    return;
                }
            }
            if (inspecting && presentation.ActiveProduct)
            {
                if (mouse.leftButton.isPressed)
                {
                    if (Vector2.Distance(pressPosition, mouse.position.ReadValue()) > 2f || delta.sqrMagnitude > 0.05f)
                        dragging = true;
                    if (delta.sqrMagnitude > 0.0001f)
                    {
                        presentation.ActiveProduct.transform.Rotate(playerCamera.transform.up, -delta.x * 0.65f, Space.World);
                        presentation.ActiveProduct.transform.Rotate(playerCamera.transform.right, delta.y * 0.65f, Space.World);
                    }
                }
                if (keyboard != null)
                {
                    float keyYaw = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1f : 0f)
                                 - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1f : 0f);
                    float keyPitch = ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f)
                                   - ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1f : 0f);
                    if (Mathf.Abs(keyYaw) > 0.01f)
                        presentation.ActiveProduct.transform.Rotate(playerCamera.transform.up, -keyYaw * 120f * Time.deltaTime, Space.World);
                    if (Mathf.Abs(keyPitch) > 0.01f)
                        presentation.ActiveProduct.transform.Rotate(playerCamera.transform.right, keyPitch * 120f * Time.deltaTime, Space.World);
                }
                if (pressedWhileInspecting && mouse.leftButton.wasReleasedThisFrame && !dragging && hovered != null && hovered.Action == WorkstationAction.Product)
                {
                    EndInspection();
                    return;
                }
                zoom = Mathf.Clamp(zoom + mouse.scroll.ReadValue().y * 0.001f, -maxInspectionZoom, maxInspectionZoom);
                presentation.ActiveProduct.transform.position = inspectionAnchor.position - playerCamera.transform.forward * zoom;
            }
        }
        public void Interact(WorkstationTarget target)
        {
            if (!target || !session.WorldInputAllowed) return;
            switch (target.Action)
            {
                case WorkstationAction.Product: ToggleInspection(); break;
                case WorkstationAction.Analyzer: EndInspection(); session.Analyze(); break;
                case WorkstationAction.Approve: Decide(PlayerDecision.Approve); break;
                case WorkstationAction.Reject: Decide(PlayerDecision.Reject); break;
                case WorkstationAction.Handbook: EndInspection(); session.OpenHandbook(); break;
                case WorkstationAction.Report: EndInspection(); session.ReopenReport(); break;
                default:
                    if (target.Action >= WorkstationAction.Paper && target.Action <= WorkstationAction.Contaminated)
                        session.SelectWaste((WasteCategory)((int)target.Action - (int)WorkstationAction.Paper));
                    break;
            }
        }
        private void Decide(PlayerDecision decision)
        {
            if (!session.Flow.CanDecide) return;
            EndInspection();
            if (decision == PlayerDecision.Approve) session.Approve(); else session.Reject();
        }
        public void ToggleInspection()
        {
            if (!session.Flow.CanDecide || !presentation.ActiveProduct) return;
            if (inspecting) { EndInspection(); return; }
            inspecting = true; zoom = 0; dragging = false; pressedWhileInspecting = false;
            originalPosition = presentation.ActiveProduct.transform.position;
            originalRotation = presentation.ActiveProduct.transform.rotation;
            presentation.ActiveProduct.transform.position = inspectionAnchor.position;
            session.NotifyProductInspected(session.Flow.Current.Key);
            session.SetTargetHint("Sol tık basılı tut + sürükle: Döndür  •  Tekerlek: Yakınlaş  •  E / Sağ tık: Bırak");
        }
        public void EndInspection()
        {
            if (!inspecting) return;
            inspecting = false; pressedWhileInspecting = false; zoom = 0;
            if (presentation.ActiveProduct) presentation.ActiveProduct.transform.SetPositionAndRotation(originalPosition, originalRotation);
            session.SetTargetHint(hovered ? hovered.Hint : "");
        }
        private void OnApplicationFocus(bool focus) { if (focus) ignoreMouseDelta = true; }
    }
}
