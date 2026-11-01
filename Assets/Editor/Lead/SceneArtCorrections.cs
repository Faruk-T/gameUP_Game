using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameUp.Editor
{
    internal static class SceneArtCorrections
    {
        private const string Path="Assets/Editor/Lead/SceneArtCorrections.json";
        [Serializable] private sealed class Record { public string Hierarchy, Source; public Vector3 Position, Scale; public Quaternion Rotation; }
        [Serializable] private sealed class Records { public Record[] Items; }
        private static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
        internal static void Apply()
        {
            var records=JsonUtility.FromJson<Records>(File.ReadAllText(Path));
            var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach(var item in records.Items)
            {
                var matches=all.Where(t=>Hierarchy(t)==item.Hierarchy).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("Preserve edited artist placement: "+item.Hierarchy);
                var t=matches[0];
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)!=item.Source)
                    throw new InvalidOperationException("Artist placement source changed: "+item.Hierarchy);
                t.localPosition=item.Position;t.localRotation=item.Rotation;t.localScale=item.Scale;
            }
        }
    }
}
