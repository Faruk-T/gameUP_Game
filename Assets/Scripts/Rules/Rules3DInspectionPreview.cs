using GameUP.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace GameUP.Rules
{
    /// <summary>
    /// Tests_Rules sahnesinde (veya bağımsız kural testlerinde) 3D COP31 Denetim Masasını,
    /// 3D Ürün + Ön/Arka Etiketini (ProductWorldLabelBinder), P1 Dijital Tartı/Cetvel ekranlarını,
    /// Analiz Cihazı LED durumunu ve 4 Atık Kutusunu görselleştiren bileşen.
    /// </summary>
    public sealed class Rules3DInspectionPreview : MonoBehaviour
    {
        [SerializeField] private InspectionService inspectionService;

        private Transform stationRoot;
        private Transform productHolder;
        private ProductWorldLabelBinder labelBinder;
        private Text scaleReadoutText;
        private Text rulerReadoutText;
        private Text analyzerReadoutText;
        private Text faxStatusText;
        private Renderer analyzerLedRenderer;
        private Renderer faxLedRenderer;

        private float targetProductYaw;
        private float currentProductYaw;
        private bool showingBackFace;

        public bool IsShowingBackFace => showingBackFace;
        public float CurrentProductYaw => currentProductYaw;

        private void Awake()
        {
            if (inspectionService == null) inspectionService = GetComponent<InspectionService>();

            Build3DWorkstationIfNeeded();
        }

        private void OnEnable()
        {
            if (inspectionService != null)
            {
                inspectionService.OnProductBegun += HandleProductBegun;
                inspectionService.OnAnalysisReady += HandleAnalysisReady;
                inspectionService.OnEmergencyActivated += HandleEmergencyActivated;
                inspectionService.OnProductCompleted += HandleProductCompleted;
            }
        }

        private void OnDisable()
        {
            if (inspectionService != null)
            {
                inspectionService.OnProductBegun -= HandleProductBegun;
                inspectionService.OnAnalysisReady -= HandleAnalysisReady;
                inspectionService.OnEmergencyActivated -= HandleEmergencyActivated;
                inspectionService.OnProductCompleted -= HandleProductCompleted;
            }
        }

        private void Update()
        {
            if (productHolder != null && productHolder.gameObject.activeSelf)
            {
                currentProductYaw = Mathf.LerpAngle(currentProductYaw, targetProductYaw, Time.unscaledDeltaTime * 10f);
                productHolder.localRotation = Quaternion.Euler(0f, currentProductYaw, 0f);
            }
        }

        public void RotateProductByDelta(float deltaYawDegrees)
        {
            targetProductYaw = Mathf.Repeat(targetProductYaw + deltaYawDegrees, 360f);
            showingBackFace = Mathf.Abs(Mathf.DeltaAngle(targetProductYaw, 180f)) < 90f;
        }

        public void FlipFrontBackFace()
        {
            showingBackFace = !showingBackFace;
            targetProductYaw = showingBackFace ? 180f : 0f;
        }

        public void ShowFrontFace()
        {
            showingBackFace = false;
            targetProductYaw = 0f;
        }

        public void ShowBackFace()
        {
            showingBackFace = true;
            targetProductYaw = 180f;
        }

        public void SetSplitViewport(bool splitView)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.rect = splitView ? new Rect(0.45f, 0f, 0.55f, 1f) : new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void HandleProductCompleted(ProductCompletedEvent evt)
        {
            if (productHolder != null)
            {
                productHolder.gameObject.SetActive(false);
            }
        }

        private void HandleEmergencyActivated(EmergencyActivatedEvent evt)
        {
            SetRendererColor(faxLedRenderer, new Color(0.95f, 0.18f, 0.18f));
            if (faxStatusText != null)
            {
                faxStatusText.text = "<color=#FF6666><b>ACİL FAKS (12.00)!</b></color>\nB Bölgesi + Dip Balığı İMHA";
            }
        }

        private void HandleProductBegun(BeginProductEvent evt)
        {
            Build3DWorkstationIfNeeded();
            if (evt.Definition == null)
                return;

            ProductDefinition def = evt.Definition;
            productHolder.gameObject.SetActive(true);
            ShowFrontFace();
            currentProductYaw = 0f;
            productHolder.localRotation = Quaternion.identity;

            if (labelBinder != null)
            {
                labelBinder.Bind(def);
            }

            if (faxStatusText != null && inspectionService != null)
            {
                bool emergencyActive = inspectionService.IsEmergencyActiveInCurrentDay;
                SetRendererColor(faxLedRenderer, emergencyActive ? new Color(0.95f, 0.18f, 0.18f) : new Color(0.18f, 0.55f, 0.25f));
                faxStatusText.text = emergencyActive
                    ? "<color=#FF6666><b>FAKS: ACİL YASAK AKTİF</b></color>\nB Bölgesi + Dip Balığı İMHA"
                    : $"<b>FAKS ÜNİTESİ</b>\nDurum: Normal\nGün: {evt.DayId}";
            }

            if (scaleReadoutText != null)
            {
                if (def.HasWeightData)
                {
                    bool weightOk = Mathf.Abs(def.ActualWeightGrams - def.DeclaredWeightGrams) <= 5f;
                    string colorHex = weightOk ? "#7CFC90" : "#FF6B6B";
                    scaleReadoutText.text =
                        $"<b>DİJİTAL TARTI (P1)</b>\n" +
                        $"Ölçülen: <color={colorHex}><b>{def.ActualWeightGrams:0} g</b></color>\n" +
                        $"Beyan: {def.DeclaredWeightGrams:0} g";
                }
                else
                {
                    scaleReadoutText.text =
                        $"<b>DİJİTAL TARTI (P1)</b>\n" +
                        $"Durum: Standart Paket\n" +
                        $"Kategori: {def.Category.ToTurkishText()}";
                }
            }

            if (rulerReadoutText != null)
            {
                if (def.HasLengthData)
                {
                    bool lenOk = def.ActualLengthCm >= 15f;
                    string colorHex = lenOk ? "#7CFC90" : "#FF6B6B";
                    rulerReadoutText.text =
                        $"<b>CETVEL ÖLÇÜMÜ (P1)</b>\n" +
                        $"Boy: <color={colorHex}><b>{def.ActualLengthCm:0.0} cm</b></color>\n" +
                        $"Min Yasal Boy: 15.0 cm";
                }
                else
                {
                    rulerReadoutText.text =
                        $"<b>CETVEL & AMBALAJ</b>\n" +
                        $"Ambalaj: {def.PackagingMaterial.ToTurkishText()}\n" +
                        $"SKT: {def.ExpiryDate}";
                }
            }

            SetRendererColor(analyzerLedRenderer, new Color(0.25f, 0.48f, 0.75f));
            if (analyzerReadoutText != null)
            {
                analyzerReadoutText.text =
                    $"<b>COP31 LAB ANALİZÖR</b>\n" +
                    $"Ürün: {def.DefinitionId}\n" +
                    $"Durum: Analize Hazır";
            }
        }

        private void HandleAnalysisReady(AnalysisReadyEvent evt)
        {
            if (evt.Report == null) return;
            bool clean = !evt.Report.IsPolluted;
            SetRendererColor(analyzerLedRenderer, clean ? new Color(0.18f, 0.85f, 0.35f) : new Color(0.95f, 0.22f, 0.22f));

            if (analyzerReadoutText != null)
            {
                string statusLine = clean
                    ? "<color=#7CFC90><b>SONUÇ: TEMİZ (CLEAN)</b></color>"
                    : "<color=#FF6B6B><b>SONUÇ: KİRLİ / ZEHİRLİ!</b></color>";
                analyzerReadoutText.text =
                    $"<b>COP31 LAB ANALİZÖR</b>\n" +
                    $"{statusLine}\n" +
                    $"Rapor: {evt.Report.ReportId}";
            }
        }

        private void Build3DWorkstationIfNeeded()
        {
            if (stationRoot != null)
                return;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0.45f, 1.52f, -1.98f);
                mainCam.transform.rotation = Quaternion.Euler(21f, 0f, 0f);
                mainCam.backgroundColor = new Color(0.08f, 0.12f, 0.16f, 1f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.rect = new Rect(0.45f, 0f, 0.55f, 1f);
            }

            stationRoot = new GameObject("COP31_3D_WorkstationPreview").transform;
            stationRoot.SetParent(transform, false);

            // 1. Denetim Masası
            CreateBox(stationRoot, "InspectionDesk", new Vector3(0.45f, 0.55f, 0f), new Vector3(2.4f, 0.12f, 1.25f), new Color(0.18f, 0.22f, 0.26f));
            // Konveyör Bandı Şeridi
            CreateBox(stationRoot, "ConveyorBelt", new Vector3(0.45f, 0.615f, 0.05f), new Vector3(2.2f, 0.02f, 0.40f), new Color(0.11f, 0.13f, 0.15f));

            // 2. 3D Ürün + ProductWorldLabelBinder (Sağ görünümün tam merkezinde)
            GameObject prodGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prodGo.name = "InspectedProduct_3D";
            prodGo.transform.SetParent(stationRoot, false);
            prodGo.transform.localPosition = new Vector3(0.45f, 0.92f, -0.05f);
            prodGo.transform.localScale = new Vector3(0.46f, 0.52f, 0.30f);
            SetRendererColor(prodGo.GetComponent<Renderer>(), new Color(0.88f, 0.91f, 0.96f));

            productHolder = prodGo.transform;
            labelBinder = prodGo.AddComponent<ProductWorldLabelBinder>();

            // 3. Dijital Tartı (Scale - P1) — Sol tarafta tam görünür
            Transform scaleBase = CreateBox(stationRoot, "DigitalScale_P1", new Vector3(-0.18f, 0.65f, -0.10f), new Vector3(0.46f, 0.06f, 0.38f), new Color(0.24f, 0.30f, 0.36f));
            scaleReadoutText = CreateWorldPanel(scaleBase, "ScaleScreen", new Vector3(0f, 2.3f, -0.45f), new Vector2(0.48f, 0.22f), new Color(0.05f, 0.14f, 0.18f, 0.95f));

            // 4. Cetvel (Ruler - P1) — Ürünün hemen önünde
            Transform rulerBase = CreateBox(stationRoot, "Ruler_P1", new Vector3(0.45f, 0.63f, -0.36f), new Vector3(0.60f, 0.02f, 0.10f), new Color(0.85f, 0.72f, 0.22f));
            rulerReadoutText = CreateWorldPanel(rulerBase, "RulerScreen", new Vector3(0f, 4.2f, -0.55f), new Vector2(0.50f, 0.18f), new Color(0.12f, 0.14f, 0.18f, 0.92f));

            // 5. Laboratuvar Analiz Cihazı + LED — Sağ tarafta
            Transform analyzerBox = CreateBox(stationRoot, "LabAnalyzer", new Vector3(1.12f, 0.86f, 0.10f), new Vector3(0.50f, 0.48f, 0.42f), new Color(0.22f, 0.27f, 0.33f));
            Transform analyzerLed = CreateBox(stationRoot, "AnalyzerLED", new Vector3(1.12f, 1.13f, -0.09f), new Vector3(0.16f, 0.05f, 0.05f), new Color(0.25f, 0.48f, 0.75f));
            analyzerLedRenderer = analyzerLed.GetComponent<Renderer>();
            analyzerReadoutText = CreateWorldPanel(analyzerBox, "AnalyzerScreen", new Vector3(0f, 0.05f, -0.52f), new Vector2(0.46f, 0.25f), new Color(0.06f, 0.12f, 0.16f, 0.96f));

            // 6. 12.00 Acil Faks Cihazı + Alarm Lambası
            Transform faxBox = CreateBox(stationRoot, "EmergencyFaxUnit", new Vector3(1.12f, 0.74f, -0.40f), new Vector3(0.42f, 0.22f, 0.30f), new Color(0.30f, 0.28f, 0.26f));
            Transform faxLed = CreateBox(stationRoot, "FaxAlarmLight", new Vector3(1.12f, 0.88f, -0.40f), new Vector3(0.10f, 0.06f, 0.10f), new Color(0.18f, 0.55f, 0.25f));
            faxLedRenderer = faxLed.GetComponent<Renderer>();
            faxStatusText = CreateWorldPanel(faxBox, "FaxScreen", new Vector3(0f, 0.12f, -0.53f), new Vector2(0.40f, 0.18f), new Color(0.14f, 0.10f, 0.12f, 0.95f));

            // 7. 4 Atık Kutusu (Kağıt, Cam, Plastik, Kontamine İmha) — Ön sırada tam görünür
            CreateWasteBin(stationRoot, "Bin_Paper", "KAĞIT ATIK", new Vector3(-0.15f, 0.35f, -0.78f), new Color(0.22f, 0.48f, 0.78f));
            CreateWasteBin(stationRoot, "Bin_Glass", "CAM ATIK", new Vector3(0.25f, 0.35f, -0.78f), new Color(0.18f, 0.65f, 0.45f));
            CreateWasteBin(stationRoot, "Bin_Plastic", "PLASTİK ATIK", new Vector3(0.65f, 0.35f, -0.78f), new Color(0.85f, 0.68f, 0.16f));
            CreateWasteBin(stationRoot, "Bin_Contaminated", "KONTAMİNE İMHA", new Vector3(1.05f, 0.35f, -0.78f), new Color(0.78f, 0.20f, 0.20f));
        }

        private static Transform CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            SetRendererColor(go.GetComponent<Renderer>(), color);
            return go.transform;
        }

        private static void CreateWasteBin(Transform parent, string name, string title, Vector3 localPos, Color color)
        {
            Transform bin = CreateBox(parent, name, localPos, new Vector3(0.30f, 0.38f, 0.28f), color);
            Text txt = CreateWorldPanel(bin, "BinLabel", new Vector3(0f, 0.15f, -0.52f), new Vector2(0.28f, 0.12f), new Color(0.08f, 0.08f, 0.10f, 0.90f));
            if (txt != null)
            {
                txt.text = $"<b>{title}</b>";
            }
        }

        private static Text CreateWorldPanel(Transform parent, string name, Vector3 localPos, Vector2 sizeMeters, Color bgColor)
        {
            GameObject canvasGo = new GameObject(name);
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localRotation = Quaternion.identity;

            Vector3 pScale = parent.lossyScale;
            float invX = Mathf.Abs(pScale.x) > 0.001f ? 1f / pScale.x : 1f;
            float invY = Mathf.Abs(pScale.y) > 0.001f ? 1f / pScale.y : 1f;
            float invZ = Mathf.Abs(pScale.z) > 0.001f ? 1f / pScale.z : 1f;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 180f);
            rt.localScale = new Vector3(
                (sizeMeters.x / 360f) * invX,
                (sizeMeters.y / 180f) * invY,
                0.001f * invZ);

            GameObject bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = bgColor;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(canvasGo.transform, false);
            RectTransform txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.05f, 0.05f);
            txtRt.anchorMax = new Vector2(0.95f, 0.95f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Text uiText = txtGo.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = 20;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.supportRichText = true;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        private static void SetRendererColor(Renderer r, Color color)
        {
            if (r == null) return;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
        }
    }
}
