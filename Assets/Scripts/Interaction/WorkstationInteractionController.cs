using GameUP.Contracts;
using GameUP.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameUP.Interaction
{
    /// <summary>MVP fixed-desk camera, inspection and click-to-interact controller.</summary>
    public sealed class WorkstationInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraYawRoot;
        [SerializeField] private InspectionService inspectionService;
        [SerializeField] private ProductPresentation presentation;
        [SerializeField] private Transform inspectionAnchor;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField, Min(0.1f)] private float rayDistance = 4f;
        [SerializeField, Min(1f)] private float lookSensitivity = 0.12f;
        [SerializeField] private float pitchLimit = 65f;
        [SerializeField, Min(0f)] private float inspectionZoomStep = 0.002f;
        [SerializeField, Min(0f)] private float maxInspectionZoom = 1.2f;

        private WorkstationTarget hovered;
        private bool inspecting;
        private bool awaitingSort;
        private bool busy;
        private bool worldInputLocked;
        private float inspectionZoom;
        private Vector3 productOriginalPosition;
        private Quaternion productOriginalRotation;
        private float yaw;
        private float pitch;
        private bool ignoreMouseDeltaOnce = true;
        private string status = "Hedefe bakıp sol tıklayın. Ürünü incelemek için E.";

        public void SetWorldInputLocked(bool locked) => worldInputLocked = locked;

        public void ActivateTestAction(WorkstationTarget target)
        {
            if (target == null) return;
            hovered = target;
            ActivateHoveredTarget();
        }

        private void OnEnable()
        {
            if (cameraYawRoot != null) yaw = cameraYawRoot.localEulerAngles.y;
            if (playerCamera != null)
            {
                pitch = playerCamera.transform.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ignoreMouseDeltaOnce = true;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (playerCamera == null || inspectionService == null || presentation == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
                ignoreMouseDeltaOnce = true;
            }
            if (worldInputLocked || (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                return;

            UpdateLook();
            UpdateHover();
            UpdateInspectInput();

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !busy && !inspecting)
                ActivateHoveredTarget();
        }

        private void UpdateLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || (inspecting && mouse.leftButton.isPressed)) return;
            Vector2 rawDelta = mouse.delta.ReadValue();
            if (ignoreMouseDeltaOnce)
            {
                ignoreMouseDeltaOnce = false;
                return;
            }
            // Ignore absolute pointer warps (for example when the locked cursor first enters Game View).
            if (rawDelta.sqrMagnitude > 900f) return;
            Vector2 delta = Vector2.ClampMagnitude(rawDelta, 45f) * lookSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            if (cameraYawRoot != null) cameraYawRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateHover()
        {
            hovered = null;
            if (CameraRaycast(out RaycastHit hit)) hovered = hit.collider.GetComponentInParent<WorkstationTarget>();
        }

        private void UpdateInspectInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null || presentation.ActiveProduct == null || busy) return;

            if (inspecting && mouse.leftButton.wasPressedThisFrame && hovered != null && hovered.Action == WorkstationAction.Product)
            {
                ToggleInspection();
                return;
            }

            if (!inspecting && keyboard.eKey.wasPressedThisFrame && !awaitingSort)
            {
                inspecting = true;
                productOriginalPosition = presentation.ActiveProduct.transform.position;
                productOriginalRotation = presentation.ActiveProduct.transform.rotation;
                inspectionZoom = 0f;
                if (inspectionAnchor != null)
                    presentation.ActiveProduct.transform.position = inspectionAnchor.position;
                status = "İnceleme: fareyle döndürün, tekerlekle yaklaştırıp uzaklaştırın. Sol tık veya E: masaya bırak.";
            }
            else if (inspecting && keyboard.eKey.wasPressedThisFrame)
            {
                inspecting = false;
                presentation.ActiveProduct.transform.SetPositionAndRotation(productOriginalPosition, productOriginalRotation);
                inspectionZoom = 0f;
                status = "Ürün masaya bırakıldı.";
            }

            if (inspecting && mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                presentation.ActiveProduct.transform.Rotate(playerCamera.transform.up, -delta.x * 0.35f, Space.World);
                presentation.ActiveProduct.transform.Rotate(playerCamera.transform.right, delta.y * 0.35f, Space.World);
            }

            if (inspecting && inspectionAnchor != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    inspectionZoom = Mathf.Clamp(inspectionZoom + scroll * inspectionZoomStep, 0f, maxInspectionZoom);
                    presentation.ActiveProduct.transform.position = inspectionAnchor.position - playerCamera.transform.forward * inspectionZoom;
                }
            }
        }

        private bool CameraRaycast(out RaycastHit hit) =>
            Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, rayDistance, interactableLayers, QueryTriggerInteraction.Ignore);

        private void ActivateHoveredTarget()
        {
            if (hovered == null || presentation.IsMoving || presentation.ActiveSession == null) return;
            switch (hovered.Action)
            {
                case WorkstationAction.Product:
                    if (!awaitingSort) ToggleInspection();
                    break;
                case WorkstationAction.Analyzer:
                    if (!awaitingSort && inspectionService.RequestAnalysis(ActiveRunId, ActiveProductId, out AnalysisReport report))
                        status = $"ANALİZ RAPORU · {report.ReportId}\n{report.FormattedDocumentText}";
                    else status = "Bu ürün için analiz alınamadı.";
                    break;
                case WorkstationAction.Approve: SubmitDecision(ProductDecision.Approve); break;
                case WorkstationAction.Reject: SubmitDecision(ProductDecision.Reject); break;
                default:
                    if (awaitingSort && hovered.WasteBin != WasteBinType.None) CompleteSorting(hovered.WasteBin, hovered.transform);
                    break;
            }
        }

        private void ToggleInspection()
        {
            if (presentation.ActiveProduct == null || busy) return;
            inspecting = !inspecting;
            if (inspecting)
            {
                productOriginalPosition = presentation.ActiveProduct.transform.position;
                productOriginalRotation = presentation.ActiveProduct.transform.rotation;
                inspectionZoom = 0f;
                if (inspectionAnchor != null) presentation.ActiveProduct.transform.position = inspectionAnchor.position;
                status = "İnceleme açık. Fareyle döndürün, tekerlekle yaklaştırıp uzaklaştırın; ürüne sol tıklayarak masaya bırakın.";
            }
            else
            {
                presentation.ActiveProduct.transform.SetPositionAndRotation(productOriginalPosition, productOriginalRotation);
                inspectionZoom = 0f;
                status = "Ürün masaya bırakıldı.";
            }
        }

        private void SubmitDecision(ProductDecision decision)
        {
            if (inspecting || awaitingSort || !inspectionService.SubmitDecision(ActiveRunId, ActiveProductId, decision, out _))
            {
                status = "Karar şu anda verilemiyor.";
                return;
            }
            busy = true;
            presentation.Route(decision, () =>
            {
                busy = false;
                if (decision == ProductDecision.Approve)
                {
                    inspectionService.NotifyDispositionFinished(ActiveRunId, ActiveProductId, out _);
                    status = "Ürün onaylandı ve çıkışa gönderildi.";
                }
                else
                {
                    awaitingSort = true;
                    status = "Gıda imha edildi. Ambalaj hedefini seçin.";
                }
            });
        }

        private void CompleteSorting(WasteBinType bin, Transform target)
        {
            if (busy || !awaitingSort) return;
            busy = true;
            presentation.Sort(target, () =>
            {
                inspectionService.SubmitSortingFinished(ActiveRunId, ActiveProductId, bin, out _);
                awaitingSort = false;
                busy = false;
                status = $"Ambalaj {bin.ToTurkishText()} hedefine gönderildi.";
            });
        }

        private string ActiveRunId => presentation.ActiveSession.RunId;
        private string ActiveProductId => presentation.ActiveSession.ProductId;

        private void OnGUI()
        {
            if (playerCamera == null) return;
            GUI.color = Color.white;
            GUI.Box(new Rect(16, Screen.height - 100, 520, 84), "GAMEUP · MASA BAŞI DENETİM");
            GUI.Label(new Rect(30, Screen.height - 72, 490, 48),
                (hovered != null ? $"{hovered.name} · Sol tık" : "Fareyle bakış · Esc: fareyi serbest bırak") + "\n" + status);
            GUI.Label(new Rect(Screen.width * 0.5f - 5, Screen.height * 0.5f - 12, 20, 24), "+");
        }
    }
}
