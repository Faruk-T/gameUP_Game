using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    internal static class WallArtFrameAuthoring
    {
        internal const string Prefab="SM_Prop_Blackboard_01";
        internal static void Create(Transform parent,Vector2 opening)
        {
            // The source has a recessed panel and a real bevelled wooden rim. Turn its
            // landscape frame upright; the existing print covers only the inner panel.
            var frame=FacilityPropAuthoring.Fit(FacilityPropAuthoring.Package(Prefab),parent,Vector3.zero,
                new Vector3(opening.y+.16f,opening.x+.16f,.12f),Quaternion.Euler(0,180,90));
            frame.name="ArtworkFrame";CoastalAssetSetup.Outline(frame);
        }
        internal static void Refresh(Transform root,RectTransform print)
        {
            foreach(var child in root.Cast<Transform>().Where(t=>t.name=="ArtworkFrame"||t.name.StartsWith("Prop_SM_Prop_Poster_",StringComparison.Ordinal)).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            Create(root,Vector2.Scale(print.sizeDelta,new Vector2(print.localScale.x,print.localScale.y)));
        }
    }
}
