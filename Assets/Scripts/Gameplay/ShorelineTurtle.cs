using UnityEngine;

namespace GameUp.Gameplay
{
    /// <summary>Five saved actors crawl to the shoreline then keep swimming without spawning or searching.</summary>
    public sealed class ShorelineTurtle : MonoBehaviour
    {
        [SerializeField] private Vector3 beachStart, waterEntry, swimCenter;
        [SerializeField] private float crawlSeconds = 8.5f, phaseOffset, swimSpeed = .28f;
        [SerializeField] private float entrySeconds=5, swimRadiusX=4.5f, swimRadiusZ=3;
        [SerializeField] private float turnDirection=1;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem wake;
        private float elapsed;
        private bool directed, emitting;
        public bool IsSwimming => elapsed + phaseOffset >= crawlSeconds;
        public bool IsCruising => elapsed + phaseOffset >= crawlSeconds+entrySeconds;
        private void Update(){if(!directed)SamplePose(elapsed+Time.deltaTime);}
        public void SetDirected(bool value)=>directed=value;

        public void SamplePose(float seconds)
        {
            elapsed = seconds;
            float t = Mathf.Max(0, elapsed + phaseOffset);
            bool swimming=t>=crawlSeconds;
            if(emitting!=swimming)
            {
                emitting=swimming;var emission=wake.emission;emission.enabled=swimming;
                if(swimming)wake.Play();else wake.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            animator.speed=swimming?1.05f:.55f;
            if (t < crawlSeconds)
            {
                float u=t/crawlSeconds;
                float sway=Mathf.Sin(u*Mathf.PI)*Mathf.Sin(u*8+phaseOffset)*.19f;
                ApplyPose(Vector3.Lerp(beachStart,waterEntry,u)+Vector3.right*sway,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(waterEntry-beachStart,Vector3.up))*Quaternion.Euler(0,Mathf.Sin(u*8+phaseOffset)*3,Mathf.Sin(t*4)*1.3f));
                return;
            }
            var loopStart=swimCenter+Vector3.left*(swimRadiusX*turnDirection);
            float swimmingTime=t-crawlSeconds;
            if(swimmingTime<entrySeconds)
            {
                float u=swimmingTime/entrySeconds;
                var control=waterEntry+new Vector3(-1.3f*turnDirection,-.35f,3.2f);
                var tangent=2*(1-u)*(control-waterEntry)+2*u*(loopStart-control);
                ApplyPose((1-u)*(1-u)*waterEntry+2*(1-u)*u*control+u*u*loopStart,
                    Quaternion.LookRotation(tangent)*Quaternion.Euler(4*Mathf.Sin(u*Mathf.PI),0,-5*Mathf.Sin(u*Mathf.PI)));
                return;
            }
            float angle=(swimmingTime-entrySeconds)*swimSpeed;
            float stroke=(swimmingTime-entrySeconds)*2+phaseOffset;
            var offset=new Vector3(-Mathf.Cos(angle)*swimRadiusX*turnDirection,Mathf.Sin(stroke)*.075f-Mathf.Pow(Mathf.Max(0,Mathf.Sin(angle*.65f)),2)*.10f,Mathf.Sin(angle)*swimRadiusZ);
            var direction=new Vector3(Mathf.Sin(angle)*swimRadiusX*turnDirection,Mathf.Cos(stroke)*.15f/swimSpeed,Mathf.Cos(angle)*swimRadiusZ);
            ApplyPose(swimCenter+offset,Quaternion.LookRotation(direction)*Quaternion.Euler(0,0,-7*turnDirection*Mathf.Sin(angle)+2*Mathf.Sin(stroke)));
        }
        private void ApplyPose(Vector3 position,Quaternion rotation)
        {
            transform.localPosition=position;transform.localRotation=rotation;
            var foam=position-Vector3.ProjectOnPlane(rotation*Vector3.forward,Vector3.up)*.4f;foam.y=-.55f;
            wake.transform.position=transform.parent.TransformPoint(foam);wake.transform.rotation=transform.parent.rotation;
        }
    }
}
