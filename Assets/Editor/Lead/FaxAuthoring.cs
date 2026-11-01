using GameUp.Gameplay;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class FaxAuthoring
    {
        internal static void Apply(Transform office,GameSessionController session)
        {
            var loosePaper=office.Find("Prop_paper");if(loosePaper)UnityEngine.Object.DestroyImmediate(loosePaper.gameObject);
            var old=office.Find("MinistryFax");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("MinistryFax").transform;root.SetParent(office,false);
            var machine=FacilityPropAuthoring.Esma("fax",root,new Vector3(-.25f,1.29f,.18f),new Vector3(.92f,.46f,.7f));
            var signal=FacilityPropAuthoring.Esma("fax signal",root,new Vector3(.23f,1.63f,.37f),new Vector3(.14f,.24f,.14f));
            var light=new GameObject("SirenLight").AddComponent<Light>();light.transform.SetParent(signal.transform,false);light.transform.localPosition=Vector3.up*.12f;
            light.type=LightType.Point;light.color=new Color(1,.03f,.015f);light.intensity=2.8f;light.range=1.7f;light.shadows=LightShadows.None;light.enabled=false;
            var idle=ProductAuthoring.Material("FaxBeaconIdle",new Color(.2f,.025f,.02f));
            var alert=ProductAuthoring.Material("FaxBeaconAlert",new Color(1,.1f,.035f));alert.EnableKeyword("_EMISSION");alert.SetColor("_EmissionColor",new Color(3,.08f,.01f));
            var beacon=signal.GetComponentInChildren<MeshRenderer>();beacon.sharedMaterial=idle;
            var paper=FacilityPropAuthoring.Esma("paper",root,new Vector3(-.25f,1.08f,-.25f),new Vector3(.55f,.008f,.42f));
            var start=new GameObject("PaperStart").transform;start.SetParent(root,false);start.position=new Vector3(-.25f,1.27f,.03f);
            var end=new GameObject("PaperEnd").transform;end.SetParent(root,false);end.position=new Vector3(-.25f,1.08f,-.48f);
            var fax=root.gameObject.AddComponent<FaxPresenter>();Set(fax,"indicator",light);Set(fax,"beacon",beacon);Set(fax,"idleBeacon",idle);Set(fax,"alertBeacon",alert);
            Set(fax,"paper",paper.transform);Set(fax,"paperStart",start);Set(fax,"paperEnd",end);Set(session,"fax",fax);paper.SetActive(false);
            FacilityPropAuthoring.Sign(root,"MinistryFaxLabel",new Vector3(-.25f,.89f,-1.29f),0,.9f,.15f,"BAKANLIK / FAKS");
            CoastalAssetSetup.Outline(machine);
        }
    }
}
