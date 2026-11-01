using System;
using System.Collections;
using GameUp.App;
using UnityEngine;

namespace GameUp.Gameplay
{
    public sealed class FaxPresenter : MonoBehaviour
    {
        [SerializeField] private Light indicator;
        [SerializeField] private MeshRenderer beacon;
        [SerializeField] private Material idleBeacon, alertBeacon;
        [SerializeField] private Transform paper, paperStart, paperEnd;
        private Coroutine printing;
        private bool alarming;
        private AudioManager audio;
        public bool IsAlarming => alarming;

        private void Awake()
        {
            if (!indicator || !beacon || !idleBeacon || !alertBeacon || !paper || !paperStart || !paperEnd)
                throw new InvalidOperationException("Fax requires its saved paper route and alarm references.");
            audio = ApplicationRoot.Require().Audio;
            ResetPresentation();
        }

        public void Print(bool alarm)
        {
            if (printing != null) StopCoroutine(printing);
            alarming = alarm;
            audio.PlayCue(GameAudioCue.Fax);
            if (alarm) audio.PlayCue(GameAudioCue.Alert);
            printing = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0;
            float duration = audio.FaxDuration;
            bool wasLit=false;
            paper.gameObject.SetActive(true);
            do
            {
                float previous=elapsed;
                elapsed += Time.unscaledDeltaTime;
                if(previous<duration)paper.SetPositionAndRotation(Vector3.Lerp(paperStart.position, paperEnd.position, Mathf.Clamp01(elapsed / duration)), paperEnd.rotation);
                bool lit = alarming && Mathf.Repeat(elapsed, .7f) < .38f;
                if(lit!=wasLit){indicator.enabled = lit; beacon.sharedMaterial = lit ? alertBeacon : idleBeacon;wasLit=lit;}
                yield return null;
            } while (elapsed < duration || alarming);
            audio.StopFax();
            indicator.enabled = false; beacon.sharedMaterial = idleBeacon; printing = null;
        }

        public void Acknowledge() => alarming = false;
        public void ResetPresentation()
        {
            if (printing != null) StopCoroutine(printing);
            if (audio) audio.StopFax();
            printing = null; alarming = false; indicator.enabled = false;
            beacon.sharedMaterial = idleBeacon; paper.gameObject.SetActive(false);
        }
        private void OnDisable() => ResetPresentation();
    }
}
