using System.Collections;
using UnityEngine;

namespace GameUp.Gameplay
{
    public sealed class FaxPresenter : MonoBehaviour
    {
        [SerializeField] private Light indicator;
        [SerializeField] private Transform paper;
        [SerializeField] private AudioSource speaker;
        [SerializeField] private AudioClip notification;
        private Vector3 paperRest;
        private void Awake() => paperRest = paper.localPosition;
        public void Print(bool emergency)
        {
            ResetPresentation();
            speaker.pitch = emergency ? 1.3f : 0.8f; speaker.PlayOneShot(notification);
            StartCoroutine(Animate(emergency));
        }
        private IEnumerator Animate(bool emergency)
        {
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                indicator.color = emergency ? Color.red : Color.cyan;
                indicator.intensity = Mathf.PingPong(t * 5, 1) * 3;
                paper.localPosition = paperRest + Vector3.back * Mathf.Min(t, 1) * 0.2f;
                yield return null;
            }
            indicator.intensity = 0;
        }
        public void ResetPresentation()
        {
            StopAllCoroutines(); indicator.intensity = 0; paper.localPosition = paperRest; speaker.Stop();
        }
    }
}
