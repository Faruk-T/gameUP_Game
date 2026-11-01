using UnityEngine;

namespace GameUp.Gameplay
{
    /// <summary>Five saved actors crawl to the shoreline then keep swimming without spawning or searching.</summary>
    public sealed class ShorelineTurtle : MonoBehaviour
    {
        [SerializeField] private Vector3 beachStart, waterEntry, swimCenter;
        [SerializeField] private float crawlSeconds = 26, phaseOffset, swimSpeed = .12f;
        private float elapsed;
        public bool IsSwimming => elapsed + phaseOffset >= crawlSeconds;
        private void Update() => SamplePose(elapsed + Time.deltaTime);

        public void SamplePose(float seconds)
        {
            elapsed = seconds;
            float t = Mathf.Max(0, elapsed + phaseOffset);
            if (t < crawlSeconds)
            {
                transform.localPosition = Vector3.Lerp(beachStart, waterEntry, t / crawlSeconds);
                transform.localRotation = Quaternion.LookRotation(waterEntry - beachStart);
                return;
            }
            float angle = (t - crawlSeconds) * swimSpeed;
            // Circle starts at the entry point; its center is inland-independent and remains in deep water.
            var radius = waterEntry - swimCenter;
            var rotated = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0) * radius;
            transform.localPosition = swimCenter + rotated + Vector3.up * (Mathf.Sin(t * 1.6f) * .035f);
            var tangent = Vector3.Cross(Vector3.up, rotated);
            transform.localRotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(Mathf.Sin(t) * 2, 0, Mathf.Sin(t * .7f) * 3);
        }
    }
}
