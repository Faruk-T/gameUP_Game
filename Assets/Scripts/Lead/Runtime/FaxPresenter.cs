using System.Collections;
using UnityEngine;

namespace GameUp.Lead.Unity
{
    public sealed class FaxPresenter : MonoBehaviour
    {
        [SerializeField] private Light indicator;
        [SerializeField] private Transform paper;
        [SerializeField] private AudioSource speaker;
        private Vector3 paperRest;
        private AudioClip tone;
        public void Configure(Light light, Transform sheet, AudioSource audio) { indicator = light; paper = sheet; speaker = audio; }
        private void Awake()
        {
            if (paper) paperRest = paper.localPosition;
            var samples = new float[11025];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * 660 * i / 22050f) * 0.08f;
            tone = AudioClip.Create("FaxTone", samples.Length, 1, 22050, false);
            tone.SetData(samples, 0);
        }
        public void Print(bool emergency)
        {
            ResetPresentation();
            if (speaker && tone) { speaker.pitch = emergency ? 1.3f : 0.8f; speaker.PlayOneShot(tone); }
            StartCoroutine(Animate(emergency));
        }
        private IEnumerator Animate(bool emergency)
        {
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                if (indicator) { indicator.color = emergency ? Color.red : Color.cyan; indicator.intensity = Mathf.PingPong(t * 5, 1) * 3; }
                if (paper) paper.localPosition = paperRest + Vector3.back * Mathf.Min(t, 1) * 0.2f;
                yield return null;
            }
            if (indicator) indicator.intensity = 0;
        }
        public void ResetPresentation()
        {
            StopAllCoroutines();
            if (indicator) indicator.intensity = 0;
            if (paper) paper.localPosition = paperRest;
            if (speaker) speaker.Stop();
        }
        private void OnDestroy() { if (tone) Destroy(tone); }
    }
}
