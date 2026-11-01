using System;
using System.Collections;
using GameUp.App;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace GameUp.Gameplay
{
    /// <summary>Owns the opening only. Gameplay starts after the authored Timeline ends.</summary>
    public sealed class IntroCinematic : MonoBehaviour
    {
        [Serializable] private struct Caption
        {
            public float Start, End;
            [TextArea] public string Text;
        }
        [SerializeField] private PlayableDirector director;
        [SerializeField] private GameObject cinematicCamera, gameplayCamera, gameplayInterface;
        [SerializeField] private CanvasGroup overlay, quote, blackout;
        [SerializeField] private Text caption;
        [SerializeField] private CanvasGroup captionBackdrop;
        [SerializeField] private Caption[] captions;
        [SerializeField] private ShorelineTurtle[] turtles;
        [SerializeField] private float quoteEnd = 19, fadeDuration = 1.2f;
        private int captionIndex = -2;
        public bool IsPlaying { get; private set; } = true;
        public double Duration => director.duration;
        public double Elapsed => director.time;
        public void SampleAt(double time){director.time=time;EvaluatePresentation();}

        public IEnumerator Play()
        {
            if (!director || !cinematicCamera || !gameplayCamera || !gameplayInterface || !overlay || !quote || !blackout || !caption || !captionBackdrop || turtles==null || turtles.Length==0)
                throw new InvalidOperationException("Opening cinematic requires its saved Timeline, cameras and UI.");
            IsPlaying = true;
            foreach(var turtle in turtles)turtle.SetDirected(true);
            gameplayInterface.SetActive(false);
            gameplayCamera.SetActive(false);
            cinematicCamera.SetActive(true);
            overlay.gameObject.SetActive(true);
            ApplicationRoot.Require().SetWorldInput(false);
            Cursor.visible = false;
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.Play();
            director.time = 0;
            EvaluatePresentation();
            while (director.time < director.duration)
            {
                yield return null;
                if (!UnityEngine.Application.isBatchMode && !UnityEngine.Application.isFocused) continue;
                director.time = Math.Min(director.duration, director.time + Time.unscaledDeltaTime);
                EvaluatePresentation();
            }
            director.Stop();
            cinematicCamera.SetActive(false);
            overlay.gameObject.SetActive(false);
            gameplayCamera.SetActive(true);
            gameplayInterface.SetActive(true);
            foreach(var turtle in turtles)turtle.SetDirected(false);
            IsPlaying = false;
        }
        private void EvaluatePresentation()
        {
            director.Evaluate();
            float t = (float)director.time;
            foreach(var turtle in turtles)turtle.SamplePose(t);
            quote.alpha = Mathf.Min(Mathf.Clamp01(t / fadeDuration), Mathf.Clamp01((quoteEnd - t) / fadeDuration));
            blackout.alpha = t <= quoteEnd ? 1 : Mathf.Clamp01(1 - (t - quoteEnd) / fadeDuration);
            blackout.alpha = Mathf.Max(blackout.alpha, Mathf.Clamp01((t - (float)director.duration + fadeDuration) / fadeDuration));
            int next = -1;
            for (int i = 0; i < captions.Length; i++)
                if (t >= captions[i].Start && t < captions[i].End) { next = i; break; }
            if (next == captionIndex) return;
            captionIndex = next;
            caption.text = next < 0 ? "" : captions[next].Text;
            captionBackdrop.alpha=next<0?0:1;
        }
    }
}
