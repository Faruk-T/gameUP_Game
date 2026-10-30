using System;
using System.Collections;
using System.IO;
using System.Linq;
using GameUp.Contracts;
using GameUp.Lead.Unity;
using UnityEngine;

namespace GameUp.Lead.Prototype
{
    /// <summary>Opt-in development-player acceptance run. Never runs during ordinary play.</summary>
    public sealed class LeadSmokeRunner : MonoBehaviour
    {
        [SerializeField] private LeadRuntime runtime;
        [SerializeField] private LeadView view;
        private string output;
        private bool active, finished;
        public void Configure(LeadRuntime owner, LeadView ui) { runtime = owner; view = ui; }

        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("--lead-smoke-test") || (!Debug.isDebugBuild && !Application.isEditor)) yield break;
            active = true;
            output = args.FirstOrDefault(a => a.StartsWith("--lead-output=", StringComparison.Ordinal))?.Substring("--lead-output=".Length)
                ?? Path.Combine(Application.persistentDataPath, "LeadSmoke");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            StartCoroutine(Watchdog());
            while (runtime.Flow == null) yield return null;
            yield return RunCampaign(0);
            if (finished) yield break;
            Require(runtime.Flow.RunSummary.Correct == 12 && runtime.Flow.RunSummary.CorrectSorts == 6 && runtime.Flow.RunSummary.Score == 132, "Perfect run must produce 12/12, 6/6 and 132 points.");
            yield return Capture("03-final-1080p.png");
            runtime.Restart();
            yield return RunCampaign(1);
            Require(runtime.Flow.RunSummary.Correct == 6 && !runtime.Flow.RunSummary.Passed, "Reject-all must fail at 50%.");
            runtime.Restart();
            yield return RunCampaign(2);
            Require(runtime.Flow.RunSummary.Correct == 6 && !runtime.Flow.RunSummary.Passed, "Approve-all must fail at 50%.");
            var previousRun = runtime.Flow.RunId;
            runtime.Restart();
            Require(runtime.Flow.RunId != previousRun && runtime.Flow.RunSummary.Total == 0 && runtime.Flow.LastReport == null && runtime.Flow.State == FlowState.MorningBriefing,
                "Restart must reset identity, results, reports and flow.");
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("04-newspaper-720p.png");
            if (finished) yield break;
            finished = true;
            File.WriteAllText(Path.Combine(output, "result.json"), "{\"result\":\"PASS\",\"campaigns\":3,\"productsProcessed\":36,\"perfectCorrect\":12,\"perfectSorting\":6,\"perfectScore\":132,\"rejectAllCorrect\":6,\"approveAllCorrect\":6,\"restartClean\":true}");
            Debug.Log("LEAD_SMOKE_PASS: three campaigns, 36 products, queued reports, emergency boundary and restart.");
            Application.Quit(0);
        }

        private IEnumerator RunCampaign(int mode)
        {
            bool capturedMorning = false, capturedEmergency = false, testedQueue = false;
            while (!finished && runtime.Flow.State != FlowState.GameSummary)
            {
                var flow = runtime.Flow;
                if (runtime.Documents.Current != null)
                {
                    var document = runtime.Documents.Current;
                    if (document.RequiresAcknowledgement) Require(!runtime.Documents.Dismiss(document.Token), "Mandatory briefing was dismissible.");
                    if (mode == 0 && document.Kind == DocumentKind.Newspaper && !capturedMorning)
                    { yield return Capture("01-newspaper-1080p.png"); capturedMorning = true; }
                    if (document.Kind == DocumentKind.Emergency)
                    {
                        Require(flow.ProductIndex == 3 && flow.Current.Key.ProductId == "D2-03", "Emergency spawned before sorting ended or after the next product.");
                        if (mode == 0 && !capturedEmergency) { yield return Capture("02-emergency-1080p.png"); capturedEmergency = true; }
                    }
                    if (mode == 0 && document.Kind == DocumentKind.Analysis && flow.Current.Key.ProductId == "D2-02")
                        yield return Capture("analysis-1080p.png");
                    view.Widgets.DocumentConfirm.onClick.Invoke();
                }
                else if (flow.State == FlowState.DaySummary) view.Widgets.SummaryContinue.onClick.Invoke();
                else if (flow.CanDecide)
                {
                    var session = flow.Current;
                    view.Widgets.Inspect.onClick.Invoke();
                    Require(runtime.ProductInspected, "Inspection UI is not connected.");
                    if (mode == 0 && session.Key.ProductId == "D1-01") yield return Capture("workstation-1080p.png");
                    if (session.Product.Category == "Balık" && flow.LastReport == null)
                    {
                        view.Widgets.Analyze.onClick.Invoke();
                        if (!testedQueue && mode == 0)
                        {
                            view.Widgets.Handbook.onClick.Invoke();
                            while (flow.Phase == ProductPhase.Analyzing && !finished) yield return null;
                            Require(runtime.Documents.Current?.Kind == DocumentKind.Handbook, "Analysis replaced an already open handbook.");
                            view.Widgets.DocumentConfirm.onClick.Invoke();
                            Require(runtime.Documents.Current?.Kind == DocumentKind.Analysis, "Queued report was lost.");
                            testedQueue = true;
                        }
                    }
                    else
                    {
                        if (session.Key.ProductId == "D2-04") Require(session.Rules.Version == 2, "Emergency rule snapshot was not applied.");
                        var decision = mode == 1 ? PlayerDecision.Reject : mode == 2 ? PlayerDecision.Approve : PrototypeInspectionBackend.ExpectedDecision(session);
                        if (decision == PlayerDecision.Approve) view.Widgets.Approve.onClick.Invoke();
                        else view.Widgets.Reject.onClick.Invoke();
                    }
                }
                else if (flow.CanSort)
                    view.Widgets.WasteButtons[(int)PrototypeInspectionBackend.ExpectedWaste(flow.Current.Key.ProductId)].onClick.Invoke();
                yield return null;
            }
        }

        private IEnumerator Capture(string name)
        {
            yield return null;
            yield return null;
            string path = Path.Combine(output, name);
            bool small = name.Contains("720p");
            PrototypeSnapshot.Capture(view.GetComponent<Canvas>(), Camera.main, path, small ? 1280 : 1920, small ? 720 : 1080);
            Require(File.Exists(path), "Screenshot could not be captured: " + name);
        }
        private IEnumerator Watchdog()
        { yield return new WaitForSecondsRealtime(120); if (!finished) Fail("Smoke run timed out."); }
        private void Require(bool condition, string message)
        { if (!condition) { Fail(message); throw new InvalidOperationException(message); } }
        private void OnLog(string condition, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Fail(condition + "\n" + stack); }
        private void Fail(string message)
        {
            if (!active || finished) return;
            finished = true;
            File.WriteAllText(Path.Combine(output, "failure.txt"), message);
            Application.Quit(1);
        }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
