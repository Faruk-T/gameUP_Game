using System;
using System.Linq;
using GameUP.Interaction;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    internal static class DecisionButtonAuthoring
    {
        internal const string Source = "Assets/Art/Environment/Mesh/buttons.fbx";
        internal static void Apply(Transform office)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Source);
            ProductAuthoring.Material("DecisionButtonHousing", new Color(.12f, .14f, .15f), .5f);
            foreach (var entry in new[] { ("onay", "Accept"), ("red", "Reject"), ("button", "DecisionButtonHousing"), ("button.001", "DecisionButtonHousing") })
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Item1),
                    AssetDatabase.LoadAssetAtPath<Material>(ProductionProjectSetup.Art + "/" + entry.Item2 + ".mat"));
            AssetDatabase.SaveAssets(); importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            foreach (var old in office.Cast<Transform>().Where(t => t.name == "Prop_SM_Prop_Elevator_Button_01").ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var spec in new[] { ("Approve", "onay", "Cylinder", WorkstationAction.Approve), ("Reject", "red", "Cylinder.001", WorkstationAction.Reject) })
            {
                var target = office.Find(spec.Item1);
                if (!target || target.GetComponent<WorkstationTarget>().Action != spec.Item4)
                    throw new InvalidOperationException("Decision target is missing or incorrectly mapped: " + spec.Item1);
                target.localPosition = new Vector3(spec.Item4 == WorkstationAction.Approve ? .25f : .8f, 1.12f, -.8f);
                target.GetComponent<BoxCollider>().size = new Vector3(.44f, .2f, .44f);
                var label = office.Find(spec.Item1 + "Label");
                label.localPosition = new Vector3(target.localPosition.x, label.localPosition.y, label.localPosition.z);
                label.localScale = new Vector3(.7f, 1, 1);
                var renderer = target.GetComponent<MeshRenderer>(); if (renderer) UnityEngine.Object.DestroyImmediate(renderer);
                var filter = target.GetComponent<MeshFilter>(); if (filter) UnityEngine.Object.DestroyImmediate(filter);
                var old = target.Find("ArtistButton"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var parts = new GameObject("ArtistButton");
                foreach (string name in new[] { spec.Item2, spec.Item3 })
                {
                    var original = source.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == name);
                    var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(parts.transform, false);
                    part.transform.localRotation = original.transform.rotation;
                    part.GetComponent<MeshFilter>().sharedMesh = original.sharedMesh;
                    part.GetComponent<MeshRenderer>().sharedMaterials = original.GetComponent<Renderer>().sharedMaterials;
                }
                var visual = FacilityPropAuthoring.FitInstance(parts, target, Vector3.zero, new Vector3(.38f, .14f, .38f), Quaternion.identity, null);
                visual.name = "ArtistButton";
                CoastalAssetSetup.Outline(visual);
            }
        }
    }
}
