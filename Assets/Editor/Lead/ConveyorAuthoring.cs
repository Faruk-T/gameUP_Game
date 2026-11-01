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
        internal const float X = 2.1f, StartX = 20, EntryZ = 3.55f, EndZ = -14, CheckpointZ = -3.2f;
        internal static void Apply(Transform office, ProductPresentation presentation)
        {
            var old = office.Find("AirportConveyor");
            var sign = old ? old.Find("ConveyorDirection") : null;
            if (sign) sign.SetParent(office, true); // Preserve the user's wall sign placement.
            foreach (var child in office.Cast<Transform>().Where(t => t.name.StartsWith("Prop_donen alet", StringComparison.Ordinal) || t.name == "BeltInstructions" || t.name == "AirportConveyor").ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            FitDesk(office);
            var root = new GameObject("AirportConveyor").transform; root.SetParent(office, false);
            var start = new Vector3(StartX, 1.14f, EntryZ); var turn = new Vector3(X, 1.14f, EntryZ);
            var end = new Vector3(X, 1.14f, EndZ);
            Segment(root, start, turn + Vector3.right * .55f); Segment(root, turn + Vector3.back * .55f, end);
            FacilityPropAuthoring.Esma("donen alet yanları", root, new Vector3(X, 1.06f, EntryZ), new Vector3(1.3f, .16f, 1.3f));
            FacilityPropAuthoring.Esma("donen alet", root, new Vector3(X, 1.075f, EntryZ), new Vector3(1.1f, .11f, 1.1f));
            ReceivingBay(root);
            if (sign) sign.SetParent(root, true);
            else FacilityPropAuthoring.Sign(root, "ConveyorDirection", new Vector3(8.056f, 3.051f, 6.747f), 90, 1.65f, .24f, "KABUL  →  DENETİM  →  SEVK");
            Set(presentation, "arrivalPoint", Point("arrivalPoint", start, presentation));
            var checkpoint = Point("deskPoint", new Vector3(X, 1.14f, CheckpointZ), presentation); Set(presentation, "deskPoint", checkpoint);
            Set(presentation, "approvedExitPoint", Point("approvedExitPoint", end, presentation));
            Set(presentation, "foodDisposalPoint", Point("foodDisposalPoint", new Vector3(X + .65f, .6f, CheckpointZ - .9f), presentation));
            var corner = new GameObject("IntakeCorner").transform; corner.SetParent(root, false); corner.position = turn;
            var so = new SerializedObject(presentation); var path = so.FindProperty("intakePath"); path.arraySize = 2;
            path.GetArrayElementAtIndex(0).objectReferenceValue = corner; path.GetArrayElementAtIndex(1).objectReferenceValue = checkpoint;
            so.ApplyModifiedPropertiesWithoutUndo(); Number(presentation, "secondsPerMetre", .22f);
            Portals(office.Find("Room"));
            foreach (var t in root.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
        private static void Segment(Transform parent, Vector3 start, Vector3 end)
        {
            var root = new GameObject("ConveyorRun").transform; root.SetParent(parent, false);
            root.SetPositionAndRotation((start + end) * .5f - Vector3.up * 1.14f, Quaternion.LookRotation(end - start));
            float length = Vector3.Distance(start, end); int count = Mathf.CeilToInt(length / 3.8f); float module = length / count;
            for (int i = 0; i < count; i++)
            {
                float z = -length * .5f + module * (i + .5f);
                FacilityPropAuthoring.Esma("donen alet yanları", root, new Vector3(0, 1.06f, z), new Vector3(1.3f, .16f, module));
                FacilityPropAuthoring.Esma("donen alet", root, new Vector3(0, 1.075f, z), new Vector3(1.1f, .11f, module - .02f));
                foreach (float x in new[] { -.48f, .48f }) FacilityPropAuthoring.Pack("SM_Prop_Pole_01", root, new Vector3(x, .52f, z), new Vector3(.07f, 1.04f, .07f));
            }
            var metal = ProductAuthoring.Material("ConveyorSafetyRail", new Color(.5f, .57f, .56f), .55f);
            foreach (float x in new[] { -.65f, .65f }) ProductAuthoring.Shape("SafetyRail", root, PrimitiveType.Cube,
                new Vector3(x, 1.15f, 0), new Vector3(.06f, .1f, length - 1.4f), metal);
        }
        private static void ReceivingBay(Transform parent)
        {
            var bay = new GameObject("ServiceReceivingBay").transform; bay.SetParent(parent, false);
            const string buildings = "Assets/PolygonOffice/Prefabs/Buildings/";
            foreach (float z in new[] { EntryZ - 1.65f, EntryZ + 1.65f })
                CoastalEnvironmentAuthoring.Fitted(buildings + "SM_Bld_Wall_Blank_01.prefab", bay, new Vector3(14.25f, 2.6f, z), new Vector3(12.5f, 5.2f, .18f));
            var end = CoastalEnvironmentAuthoring.Fitted(buildings + "SM_Bld_Wall_Blank_01.prefab", bay, new Vector3(20.5f, 2.6f, EntryZ), new Vector3(3.3f, 5.2f, .18f));
            end.transform.localRotation = Quaternion.Euler(0, 90, 0);
            CoastalEnvironmentAuthoring.Fitted(buildings + "SM_Bld_Floor_Concrete_01.prefab", bay, new Vector3(14.25f, -.06f, EntryZ), new Vector3(12.5f, .12f, 3.3f));
            CoastalEnvironmentAuthoring.Fitted(buildings + "SM_Bld_Ceiling_01.prefab", bay, new Vector3(14.25f, 5.2f, EntryZ), new Vector3(12.5f, .12f, 3.3f));
            foreach (float x in new[] { 11f, 17f })
            {
                FacilityPropAuthoring.Pack("SM_Prop_Light_Fluorescent_01", bay, new Vector3(x, 4.95f, EntryZ), new Vector3(1.1f, .08f, .18f));
                var light = new GameObject("ReceivingLight", typeof(Light)).GetComponent<Light>(); light.transform.SetParent(bay, false);
                light.transform.localPosition = new Vector3(x, 4, EntryZ); light.type = LightType.Point;
                light.color = new Color(.86f, .93f, 1); light.intensity = 1.4f; light.range = 9;
            }
        }
        internal static void FitDesk(Transform office)
        {
            var desk = office.Find("Prop_desk"); var renderers = desk.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            desk.localScale = Vector3.Scale(desk.localScale, new Vector3(4.5f / bounds.size.x, 1, 1));
            var position = desk.localPosition; position.x = -1.2f; desk.localPosition = position;
        }
        private static Transform Point(string field, Vector3 position, ProductPresentation presentation)
        {
            var so = new SerializedObject(presentation); var point = (Transform)so.FindProperty(field).objectReferenceValue;
            point.position = position; return point;
        }
        private static void Portals(Transform room)
        {
            var leftSill = room.Find("FrontSill_Left");
            if (leftSill)
            {
                var material = leftSill.GetComponent<Renderer>().sharedMaterial;
                UnityEngine.Object.DestroyImmediate(leftSill.gameObject); UnityEngine.Object.DestroyImmediate(room.Find("FrontSill_Right").gameObject);
                ProductAuthoring.Shape("FrontSill", room, PrimitiveType.Cube, new Vector3(0, .6f, 8.5f), new Vector3(16, 1.2f, .25f), material);
                foreach (string name in new[] { "WindowRail_Left", "WindowRail_Right" })
                { var old = room.Find(name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject); }
                ProductAuthoring.Shape("WindowRail", room, PrimitiveType.Cube, new Vector3(0, 1.2f, 8.35f), new Vector3(16, .12f, .25f), material);
            }
            var architecture = room.Find("PolygonOfficeArchitecture");
            foreach (var wall in architecture.Cast<Transform>().Where(t => t.name.StartsWith("ConveyorPortal", StringComparison.Ordinal) ||
                t.name == "SM_Bld_Wall_Blank_01" && Mathf.Abs(t.localPosition.z + 8) < .02f && (Mathf.Abs(t.localPosition.x) < .02f || Mathf.Abs(t.localPosition.x - 3.2f) < .02f)).ToArray())
                UnityEngine.Object.DestroyImmediate(wall.gameObject);
            const float left = X - .75f, right = X + .75f;
            const string path = "Assets/PolygonOffice/Prefabs/Buildings/SM_Bld_Wall_Blank_01.prefab";
            CoastalEnvironmentAuthoring.Fitted(path, architecture, new Vector3(1.6f, 3.65f, -8), new Vector3(6.4f, 3.1f, .18f)).name = "ConveyorPortalHeader";
            CoastalEnvironmentAuthoring.Fitted(path, architecture, new Vector3((left - 1.6f) * .5f, 1.05f, -8), new Vector3(left + 1.6f, 2.1f, .18f)).name = "ConveyorPortalLeft";
            CoastalEnvironmentAuthoring.Fitted(path, architecture, new Vector3((right + 4.8f) * .5f, 1.05f, -8), new Vector3(4.8f - right, 2.1f, .18f)).name = "ConveyorPortalRight";
        }
    }
}
