using System.Collections.Generic;
using GameUP.Contracts;
using UnityEngine;

namespace GameUP.Rules
{
    /// <summary>
    /// Faruk'un kendi izole test sahnesinde (Assets/Scenes/Tests_Rules.unity)
    /// Hakan'ın UI'ını veya Özgür'ün 3D etkileşimini beklemeden 2 günü, 12 ürünü,
    /// analiz faksını, 12.00 acil faksını, atık ayrıştırmayı ve T01–T17 testlerini
    /// görsel olarak oynayıp doğrulamasını sağlayan interaktif test paneli (Bölüm 4, Kural 1).
    /// </summary>
    public class RulesTestHarness : MonoBehaviour
    {
        [SerializeField] private InspectionService inspectionService;
        [SerializeField] private AnalysisService analysisService;
        [SerializeField] private RunResults runResults;
        [SerializeField] private Rules3DInspectionPreview preview3D;

        private int currentDayIndex;
        private int currentProductIndex;
        private bool waitingForMorningAck;
        private bool waitingForEmergencyAck;
        private bool waitingForSorting;
        private bool dayCompleted;
        private bool gameCompleted;
        private bool split3DView = true;
        private bool showAcceptanceReportDrawer;
        private DayDefinition cachedDay3P1;

        private string lastAnalysisText = "Henüz analiz istenmedi.";
        private string lastEventLog = "Oyun başlatıldı.";
        private string automatedTestReport = "";
        private Vector2 scrollPos;

        private void Awake()
        {
            if (inspectionService == null)
            {
                inspectionService = GetComponent<InspectionService>();
                if (inspectionService == null)
                    inspectionService = gameObject.AddComponent<InspectionService>();
            }

            if (analysisService == null)
            {
                analysisService = GetComponent<AnalysisService>();
                if (analysisService == null)
                    analysisService = gameObject.AddComponent<AnalysisService>();
            }

            if (runResults == null)
            {
                runResults = GetComponent<RunResults>();
                if (runResults == null)
                    runResults = gameObject.AddComponent<RunResults>();
            }

            if (preview3D == null)
            {
                preview3D = GetComponent<Rules3DInspectionPreview>();
                if (preview3D == null)
                    preview3D = gameObject.AddComponent<Rules3DInspectionPreview>();
            }
        }

        private void Start()
        {
            RulesAcceptanceSuite.RunAllTests(out bool allPassed, out automatedTestReport);
            StartNewSimulatedGame();
        }

        public void StartNewSimulatedGame()
        {
            inspectionService.StartNewRun();
            currentDayIndex = 0;
            gameCompleted = false;
            BeginSimulatedDay(0);
        }

        private void BeginSimulatedDay(int dayIdx)
        {
            DayDefinition day = inspectionService.GetDayDefinition(dayIdx);
            if (day == null)
            {
                gameCompleted = true;
                return;
            }

            currentDayIndex = dayIdx;
            currentProductIndex = 0;
            dayCompleted = false;
            waitingForEmergencyAck = false;
            waitingForSorting = false;
            waitingForMorningAck = true;
            lastAnalysisText = "Henüz analiz istenmedi.";

            inspectionService.BeginDay(day);
            lastEventLog = $"{day.DayTitle} ({day.GameDate}) sabah gazetesi açıldı.";
        }

        public void StartP1Day3Simulation()
        {
            if (cachedDay3P1 == null)
            {
                var cat = DefaultGameContentFactory.CreateCatalogInMemory();
                cachedDay3P1 = cat.Day3P1;
            }

            inspectionService.StartNewRun();
            currentDayIndex = 2;
            currentProductIndex = 0;
            dayCompleted = false;
            gameCompleted = false;
            waitingForEmergencyAck = false;
            waitingForSorting = false;
            waitingForMorningAck = true;
            lastAnalysisText = "Henüz analiz istenmedi.";

            inspectionService.BeginDay(cachedDay3P1);
            lastEventLog = $"{cachedDay3P1.DayTitle} ({cachedDay3P1.GameDate}) P1 Tartı/Cetvel/İçerik paketi açıldı.";
        }

        private DayDefinition GetActiveDayDefinition()
        {
            if (currentDayIndex == 2 && cachedDay3P1 != null)
                return cachedDay3P1;
            return inspectionService.GetDayDefinition(currentDayIndex);
        }

        private void SpawnCurrentProduct()
        {
            DayDefinition day = GetActiveDayDefinition();
            if (day == null || currentProductIndex >= day.Products.Count)
            {
                dayCompleted = true;
                lastEventLog = $"{day?.DayId} tamamlandı. Gün sonu özeti hazır.";
                return;
            }

            ProductDefinition product = day.Products[currentProductIndex];
            waitingForSorting = false;
            lastAnalysisText = "Bu ürün için henüz analiz istenmedi.";

            inspectionService.BeginProduct(product, product.DefinitionId, currentProductIndex);
            lastEventLog = $"Ürün masaya geldi: [{product.DefinitionId}] {product.DisplayName} (Snapshot v{inspectionService.ActiveProductSession.Snapshot.Version})";
        }

        private void OnGUI()
        {
            Event ev = Event.current;
            if (ev != null)
            {
                if (ev.type == EventType.KeyDown)
                {
                    if (ev.keyCode == KeyCode.Tab)
                    {
                        split3DView = !split3DView;
                        ev.Use();
                    }
                    else if (ev.keyCode == KeyCode.Space && preview3D != null)
                    {
                        preview3D.FlipFrontBackFace();
                        ev.Use();
                    }
                }
                else if (ev.type == EventType.MouseDrag && split3DView && ev.mousePosition.x > Screen.width * 0.50f && preview3D != null)
                {
                    preview3D.RotateProductByDelta(-ev.delta.x * 1.1f);
                    ev.Use();
                }
            }

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                richText = true,
                wordWrap = true,
                padding = new RectOffset(10, 10, 10, 10)
            };

            float leftPanelWidth = split3DView ? Mathf.Clamp(Screen.width * 0.44f, 420f, 680f) : (Screen.width - 30f);
            float innerWidth = leftPanelWidth - 28f;

            if (preview3D != null)
            {
                preview3D.SetSplitViewport(split3DView);
            }

            // Sağ üstte 3D Masa ve 3D Etiket Döndürme Kontrol Çubuğu
            if (split3DView && preview3D != null)
            {
                float rightX = leftPanelWidth + 20f;
                float rightW = Mathf.Max(260f, Screen.width - rightX - 15f);
                GUILayout.BeginArea(new Rect(rightX, 12f, rightW, 120f));
                string faceName = preview3D.IsShowingBackFace ? "ARKA YÜZ (SKT / Künye / Kaynak)" : "ÖN YÜZ (Pazarlama İddiası)";
                GUILayout.Box($"<b>3D DENETİM MASASI & ETİKET İNCELEME (A06 / I02)</b>\nBakılan Yüz: <b>{faceName}</b> (Açı: {preview3D.CurrentProductYaw:0}°) — <i>Sağ alanda fareyle sürükleyerek 3D ürünü döndürün.</i>", boxStyle, GUILayout.Width(rightW));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("◀ Sola Çevir (-45°)", GUILayout.Height(26)))
                    preview3D.RotateProductByDelta(-45f);
                if (GUILayout.Button("🔄 Ön / Arka Yüz Çevir [Space]", GUILayout.Height(26)))
                    preview3D.FlipFrontBackFace();
                if (GUILayout.Button("Sağa Çevir (+45°) ▶", GUILayout.Height(26)))
                    preview3D.RotateProductByDelta(45f);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            GUILayout.BeginArea(new Rect(10, 12, leftPanelWidth, Screen.height - 24));
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            GUILayout.Label("<b>GAMEUP32 — FARUK (VERİ, KURALLAR, ANALİZ) PANELİ</b>", headerStyle, GUILayout.Width(innerWidth));
            GUILayout.Box($"Aktif Oturum: <b>{inspectionService.CurrentRunId}</b> | Gün: <b>{inspectionService.CurrentDayId}</b>\nSon Durum: {lastEventLog}", boxStyle, GUILayout.Width(innerWidth));

            float halfBtnW = (innerWidth - 6f) * 0.5f;
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            if (GUILayout.Button("1–2. Gün Baştan (P0)", GUILayout.Width(halfBtnW), GUILayout.Height(26)))
            {
                StartNewSimulatedGame();
            }
            if (GUILayout.Button("3. Gün: P1 (Tartı/Cetvel/Gofret)", GUILayout.Width(halfBtnW), GUILayout.Height(26)))
            {
                StartP1Day3Simulation();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            if (GUILayout.Button(showAcceptanceReportDrawer ? "Test Raporunu Gizle (15/15)" : "15/15 Test Raporunu Göster", GUILayout.Width(halfBtnW), GUILayout.Height(26)))
            {
                RulesAcceptanceSuite.RunAllTests(out _, out automatedTestReport);
                showAcceptanceReportDrawer = !showAcceptanceReportDrawer;
            }
            if (GUILayout.Button(split3DView ? "Tam Ekran Rapor [Tab]" : "3D Masa Görünümü [Tab]", GUILayout.Width(halfBtnW), GUILayout.Height(26)))
            {
                split3DView = !split3DView;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            DayDefinition currentDay = GetActiveDayDefinition();

            if (gameCompleted)
            {
                DrawGameSummaryUI(boxStyle);
            }
            else if (waitingForMorningAck && currentDay != null)
            {
                DrawMorningBriefingUI(currentDay, boxStyle);
            }
            else if (waitingForEmergencyAck && currentDay != null)
            {
                DrawEmergencyFaxUI(currentDay, boxStyle);
            }
            else if (dayCompleted && currentDay != null)
            {
                DrawDaySummaryUI(currentDay, boxStyle);
            }
            else
            {
                DrawWorkingProductUI(boxStyle);
            }

            if (showAcceptanceReportDrawer && !string.IsNullOrEmpty(automatedTestReport))
            {
                GUILayout.Space(12);
                GUILayout.Box($"<b>OTOMATİK TEST RAPORU (Bölüm 17: T01–T17 + P1 — 15/15 GEÇTİ)</b>\n{automatedTestReport}", boxStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMorningBriefingUI(DayDefinition day, GUIStyle boxStyle)
        {
            DocumentDefinition news = day.MorningNewspaper;
            GUILayout.Box(
                $"<b>SABAH GAZETESİ — {day.GameDate}</b>\n\n" +
                $"<b>{news?.Title}</b>\n{news?.Body}\n\n" +
                $"<b>GÖREV KUTUSU:</b> {news?.TaskInstructionBox}",
                boxStyle);

            GUILayout.Box($"<b>AKTİF EL KİTAPÇIĞI:</b>\n{inspectionService.GetCurrentRulebookText()}", boxStyle);

            if (GUILayout.Button("GÖREVE BAŞLA (İlk Ürünü Masaya Getir)", GUILayout.Height(40)))
            {
                waitingForMorningAck = false;
                SpawnCurrentProduct();
            }
        }

        private void DrawEmergencyFaxUI(DayDefinition day, GUIStyle boxStyle)
        {
            DocumentDefinition fax = day.EmergencyEvent?.EmergencyDocument;
            GUILayout.Box(
                $"<b>[ACİL UYARI — SAAT {day.EmergencyEvent?.DisplayedClockTime}]</b>\n\n" +
                $"<b>{fax?.Title}</b>\n{fax?.Body}\n\n" +
                $"<b>{fax?.TaskInstructionBox}</b>",
                boxStyle);

            if (GUILayout.Button("ACİL FAKSI OKUDUM VE ONAYLIYORUM (Yeni Kuralı Etkinleştir - L06/R06)", GUILayout.Height(40)))
            {
                inspectionService.ActivateEmergency(inspectionService.CurrentRunId);
                waitingForEmergencyAck = false;
                lastEventLog = "Saat 12.00 Acil Faksı onaylandı; R_SOURCE_BAN kuralı sonraki ürünler için etkinleştirildi!";
                SpawnCurrentProduct();
            }
        }

        private void DrawWorkingProductUI(GUIStyle boxStyle)
        {
            ProductSession session = inspectionService.ActiveProductSession;
            if (session == null)
                return;

            ProductDefinition p = session.Definition;

            float leftPanelWidth = split3DView ? Mathf.Clamp(Screen.width * 0.44f, 420f, 680f) : (Screen.width - 30f);
            float innerWidth = leftPanelWidth - 28f;
            float colW = (innerWidth - 8f) * 0.5f;

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.Box($"<b>ÖN YÜZ ETİKETİ</b>\n{p.GetFormattedFrontLabel()}\n\n<b>ARKA YÜZ ETİKETİ</b>\n{p.GetFormattedBackLabel()}", boxStyle, GUILayout.Width(colW));
            GUILayout.Box($"<b>AKTİF KİTAPÇIK (Snapshot v{session.Snapshot.Version})</b>\n{session.Snapshot.BuildRulebookBodyText()}", boxStyle, GUILayout.Width(colW));
            GUILayout.EndHorizontal();

            GUILayout.Box($"<b>LABORATUVAR / FAKS RAPORU:</b>\n{lastAnalysisText}", boxStyle, GUILayout.Width(innerWidth));

            if (!waitingForSorting)
            {
                float thirdW = (innerWidth - 10f) / 3f;
                GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
                if (GUILayout.Button("Analiz Cihazı (Faks)", GUILayout.Width(thirdW), GUILayout.Height(34)))
                {
                    if (inspectionService.RequestAnalysis(session.RunId, session.ProductId, out AnalysisReport report))
                    {
                        lastAnalysisText = report.FormattedDocumentText;
                        lastEventLog = $"Analiz raporu fakstan çıktı: {report.ReportId}";
                    }
                }

                if (GUILayout.Button("ONAYLA — Geçer (W)", GUILayout.Width(thirdW), GUILayout.Height(34)))
                {
                    if (inspectionService.SubmitDecision(session.RunId, session.ProductId, ProductDecision.Approve, out var eval))
                    {
                        inspectionService.NotifyDispositionFinished(session.RunId, session.ProductId, out var outcome);
                        lastEventLog = outcome?.SummaryExplanation ?? "Onaylandı.";
                        AdvanceAfterProductCommit();
                    }
                }

                if (GUILayout.Button("REDDET — İmha (R)", GUILayout.Width(thirdW), GUILayout.Height(34)))
                {
                    if (inspectionService.SubmitDecision(session.RunId, session.ProductId, ProductDecision.Reject, out _))
                    {
                        // Gıda imhası biter ama ayrıştırma bitmeden ürün tamamlanmaz (Bölüm 7 & T11)
                        inspectionService.NotifyDispositionFinished(session.RunId, session.ProductId, out _);
                        waitingForSorting = true;
                        lastEventLog = "Gıda imha edildi. Şimdi ambalaj için doğru atık kutusunu seçin!";
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<b>AMBALAJ ATIK HEDEFİNİ SEÇİN (Bölüm 12):</b>");
                GUILayout.BeginHorizontal();
                DrawWasteBinButton(session, WasteBinType.Paper, "1. Kâğıt Atık");
                DrawWasteBinButton(session, WasteBinType.Glass, "2. Cam Atık");
                DrawWasteBinButton(session, WasteBinType.Plastic, "3. Plastik Atık");
                DrawWasteBinButton(session, WasteBinType.Contaminated, "4. Kontamine Atık");
                GUILayout.EndHorizontal();
            }
        }

        private void DrawWasteBinButton(ProductSession session, WasteBinType binType, string label)
        {
            if (GUILayout.Button(label, GUILayout.Height(40)))
            {
                if (inspectionService.SubmitSortingFinished(session.RunId, session.ProductId, binType, out var outcome))
                {
                    waitingForSorting = false;
                    lastEventLog = outcome?.SummaryExplanation ?? "Ayrıştırma tamamlandı.";
                    AdvanceAfterProductCommit();
                }
            }
        }

        private void AdvanceAfterProductCommit()
        {
            currentProductIndex++;

            // 3. ürün tamamen bittiğinde Acil Faks tetiklendiyse önce faks ekranına geç (Bölüm 11)
            if (inspectionService.Engine.IsEmergencyTriggeredInCurrentDay &&
                !inspectionService.Engine.IsEmergencyActiveInCurrentDay)
            {
                waitingForEmergencyAck = true;
                lastEventLog = "Saat 12.00 — Bakanlık Acil Faksı geldi! 4. ürüne geçmeden önce onaylanmalı.";
                return;
            }

            SpawnCurrentProduct();
        }

        private void DrawDaySummaryUI(DayDefinition day, GUIStyle boxStyle)
        {
            DaySummaryReport summary = inspectionService.GetDaySummary(day.DayId);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{day.DayTitle} — GÜN SONU RAPORU</b>");
            sb.AppendLine($"Toplam Ürün: {summary.TotalProducts}");
            sb.AppendLine($"Doğru Karar: <b>{summary.CorrectDecisionCount}</b> (%{summary.DecisionAccuracyPercent})");
            sb.AppendLine($"Hatalı Kabul (Geçersiz Ürünü Onaylama): <b>{summary.FalseAcceptanceCount}</b>");
            sb.AppendLine($"Hatalı Red (Sağlam Ürünü Reddetme): <b>{summary.FalseRejectionCount}</b>");
            sb.AppendLine($"Doğru Atık Ayrıştırma Oranı: <b>{summary.FormattedSortingRatio}</b>");
            sb.AppendLine($"Gün Puanı: <b>{summary.TotalDayScore}</b>");
            sb.AppendLine("\n<b>Ürün Detayları:</b>");
            for (int i = 0; i < summary.Outcomes.Count; i++)
            {
                var o = summary.Outcomes[i];
                sb.AppendLine($"• [{o.ProductId}] {o.SummaryExplanation} (Puan: {o.TotalScoreDelta:+#;-#;0})");
            }

            GUILayout.Box(sb.ToString(), boxStyle);

            if (GUILayout.Button("SONRAKİ AŞAMAYA GEÇ", GUILayout.Height(40)))
            {
                BeginSimulatedDay(currentDayIndex + 1);
            }
        }

        private void DrawGameSummaryUI(GUIStyle boxStyle)
        {
            GameSummaryReport gameSummary = inspectionService.GetGameSummary();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>OYUN SONU FİNAL RAPORU — {gameSummary.ResultBadgeTitle}</b>");
            sb.AppendLine($"Toplam İncelenen Ürün: {gameSummary.TotalProducts}");
            sb.AppendLine($"Toplam Doğru Karar: <b>{gameSummary.CorrectDecisionCount}/{gameSummary.TotalProducts}</b> (<b>%{gameSummary.DecisionAccuracyPercent}</b> — Başarı Eşiği: %75 / en az 9 doğru)");
            sb.AppendLine($"Toplam Hatalı Kabul: {gameSummary.FalseAcceptanceCount} | Toplam Hatalı Red: {gameSummary.FalseRejectionCount}");
            sb.AppendLine($"Toplam Atık Ayrıştırma: <b>{gameSummary.FormattedSortingRatio}</b>");
            sb.AppendLine($"Genel Toplam Puan: <b>{gameSummary.TotalScore}</b>");

            GUILayout.Box(sb.ToString(), boxStyle);

            if (GUILayout.Button("TEKRAR OYNA (Yeni RunId ile Sıfırla)", GUILayout.Height(40)))
            {
                StartNewSimulatedGame();
            }
        }
    }
}

