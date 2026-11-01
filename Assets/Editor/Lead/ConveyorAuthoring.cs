using System;
using System.Linq;
using GameUP.Interaction;
using UnityEditor;
using UnityEngine;
using static GameUp.Editor.UIAuthoring;

namespace GameUp.Editor
{
    internal static class ConveyorAuthoring
    {
        internal const float X=3.7f,StartZ=18,EndZ=-14,CheckpointZ=.2f;
        internal static void Apply(Transform office,ProductPresentation presentation)
        {
            foreach(var child in office.Cast<Transform>().Where(t=>t.name.StartsWith("Prop_donen alet",StringComparison.Ordinal)||t.name=="BeltInstructions"||t.name=="AirportConveyor").ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            FitDesk(office);
            var root=new GameObject("AirportConveyor").transform;root.SetParent(office,false);
            for(int i=0;i<8;i++)
            {
                float z=EndZ+2+i*4;
                FacilityPropAuthoring.Esma("donen alet yanları",root,new Vector3(X,1.06f,z),new Vector3(1.3f,.16f,4));
                FacilityPropAuthoring.Esma("donen alet",root,new Vector3(X,1.075f,z),new Vector3(1.1f,.11f,3.98f));
                foreach(float x in new[]{X-.48f,X+.48f})
                    FacilityPropAuthoring.Pack("SM_Prop_Pole_01",root,new Vector3(x,.52f,z),new Vector3(.07f,1.04f,.07f));
            }
            var metal=ProductAuthoring.Material("ConveyorSafetyRail",new Color(.5f,.57f,.56f),.55f);
            foreach(float x in new[]{X-.65f,X+.65f})ProductAuthoring.Shape("SafetyRail",root,PrimitiveType.Cube,new Vector3(x,1.15f,2),new Vector3(.06f,.1f,32),metal);
            FacilityPropAuthoring.Sign(root,"ConveyorDirection",new Vector3(X,1.68f,3.4f),0,1.65f,.24f,"KABUL  →  DENETİM  →  SEVK");
            Set(presentation,"arrivalPoint",Point("Arrival",new Vector3(X,1.14f,StartZ),presentation));
            Set(presentation,"deskPoint",Point("InspectionDesk",new Vector3(X,1.14f,CheckpointZ),presentation));
            Set(presentation,"approvedExitPoint",Point("MarketExit",new Vector3(X,1.14f,EndZ),presentation));
            Set(presentation,"foodDisposalPoint",Point("FoodDisposal",new Vector3(X+.65f,.6f,CheckpointZ-.9f),presentation));
            Number(presentation,"secondsPerMetre",.22f);
            Portals(office.Find("Room"));
            foreach(var t in root.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
        }
        internal static void FitDesk(Transform office)
        {
            var desk=office.Find("Prop_desk");var renderers=desk.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            desk.localScale=Vector3.Scale(desk.localScale,new Vector3(7/bounds.size.x,1,1));
            var position=desk.localPosition;position.x=-.75f;desk.localPosition=position;
        }
        private static Transform Point(string name,Vector3 position,ProductPresentation presentation)
        {
            var p=new SerializedObject(presentation);string field=name=="Arrival"?"arrivalPoint":name=="InspectionDesk"?"deskPoint":name=="MarketExit"?"approvedExitPoint":"foodDisposalPoint";
            var point=(Transform)p.FindProperty(field).objectReferenceValue;
            point.position=position;return point;
        }
        private static void Portals(Transform room)
        {
            const float left=2.95f,right=4.45f;
            var sill=room.Find("FrontSill");
            if(sill)
            {
                var material=sill.GetComponent<Renderer>().sharedMaterial;UnityEngine.Object.DestroyImmediate(sill.gameObject);
                ProductAuthoring.Shape("FrontSill_Left",room,PrimitiveType.Cube,new Vector3((-8+left)*.5f,.6f,8.5f),new Vector3(left+8,1.2f,.25f),material);
                ProductAuthoring.Shape("FrontSill_Right",room,PrimitiveType.Cube,new Vector3((8+right)*.5f,.6f,8.5f),new Vector3(8-right,1.2f,.25f),material);
            }
            foreach(var rail in room.Cast<Transform>().Where(t=>t.name=="WindowRail"&&t.localPosition.y<2).ToArray())
            {
                var material=rail.GetComponent<Renderer>().sharedMaterial;UnityEngine.Object.DestroyImmediate(rail.gameObject);
                ProductAuthoring.Shape("WindowRail_Left",room,PrimitiveType.Cube,new Vector3((-8+left)*.5f,1.2f,8.35f),new Vector3(left+8,.12f,.25f),material);
                ProductAuthoring.Shape("WindowRail_Right",room,PrimitiveType.Cube,new Vector3((8+right)*.5f,1.2f,8.35f),new Vector3(8-right,.12f,.25f),material);
            }
            foreach(var mullion in room.Cast<Transform>().Where(t=>t.name=="WindowMullion"&&Mathf.Abs(t.localPosition.x-4)<.01f))mullion.localPosition=new Vector3(4.55f,2.8f,8.4f);
            var architecture=room.Find("PolygonOfficeArchitecture");
            foreach(var wall in architecture.Cast<Transform>().Where(t=>t.name=="SM_Bld_Wall_Blank_01"&&Mathf.Abs(t.localPosition.x-3.2f)<.02f&&Mathf.Abs(t.localPosition.z+8)<.02f).ToArray())
                UnityEngine.Object.DestroyImmediate(wall.gameObject);
            if(architecture.Find("ConveyorPortalHeader"))return;
            string path="Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Wall_Blank_01.prefab";
            var header=CoastalEnvironmentAuthoring.Fitted(path,architecture,new Vector3(3.2f,3.65f,-8),new Vector3(3.2f,3.1f,.18f));header.name="ConveyorPortalHeader";
            CoastalEnvironmentAuthoring.Fitted(path,architecture,new Vector3((1.6f+left)*.5f,1.05f,-8),new Vector3(left-1.6f,2.1f,.18f)).name="ConveyorPortalLeft";
            CoastalEnvironmentAuthoring.Fitted(path,architecture,new Vector3((4.8f+right)*.5f,1.05f,-8),new Vector3(4.8f-right,2.1f,.18f)).name="ConveyorPortalRight";
        }
    }
}
