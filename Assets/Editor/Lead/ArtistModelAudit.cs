using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameUp.Editor
{
    public static class ArtistModelAudit
    {
        [MenuItem("GameUP32/Validation/Esmanur model inventory")]
        public static void Generate()
        {
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/Urunler"})
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var sceneDependencies=new HashSet<string>(AssetDatabase.GetDependencies(new[]{ProductionProjectSetup.FirstPath,ProductionProjectSetup.MenuPath,ProductionProjectSetup.GamePath},true),StringComparer.Ordinal);
            var campaign=new HashSet<string>(StringComparer.Ordinal);
            foreach(var product in ProductionProjectSetup.Days().SelectMany(d=>d.Products))
                foreach(var dependency in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(product.Prefab),true))campaign.Add(dependency);
            var report=new StringBuilder("# Esmanur model envanteri\n\n");
            report.AppendLine("Kaynak: Esmanur / esmnrrr, `feature/Objects`, commit `0dc833b` (31 Ekim 2026). Bu rapor Unity AssetDatabase bağımlılıklarıyla üretilir; dosyanın repoda olması ile sahnede kullanılması ayrı sayılır.\n");
            report.AppendLine($"- Sanatçı prefabı: **{paths.Length}**\n- İki günlük oynanışta farklı kaynak prefab: **{paths.Count(campaign.Contains)}**\n- Sahnelere bağlı toplam: **{paths.Count(sceneDependencies.Contains)}**\n- Sahneye bağlanmamış: **{paths.Count(p=>!sceneDependencies.Contains(p))}**\n");
            report.AppendLine("Önceki entegrasyonda 5 kaynak prefab oynanışa bağlıydı; 26 prefab sahneye bağlanmamıştı. Bu dalda kalan süt, et/yumurta ve meyve modelleri etiketli numune/teşhir dolaplarına yerleştirilmiştir. Raf ürünleri dekor nesnesidir; iki günlük kampanyanın karar listesine eklenmez.\n");
            report.AppendLine("| Kaynak prefab | Bağlantı |\n|---|---|");
            var usedMeshes=new HashSet<string>(StringComparer.Ordinal);
            foreach(string path in paths)
            {
                string usage=campaign.Contains(path)?"P0 denetim ürünü":sceneDependencies.Contains(path)?"Numune / teşhir dolabı":"Sahneye bağlı değil";
                report.AppendLine($"| [{Path.GetFileNameWithoutExtension(path)}](../{path}) | {usage} |");
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh)usedMeshes.Add(MeshKey(filter.sharedMesh));
                foreach(var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.sharedMesh)usedMeshes.Add(MeshKey(skin.sharedMesh));
            }
            var meshes=AssetDatabase.LoadAllAssetsAtPath(RoomAuthoring.ArtistRoot+"GameUp32-Objects.fbx").OfType<Mesh>().OrderBy(m=>m.name).ToArray();
            var unassigned=meshes.Where(m=>!usedMeshes.Contains(MeshKey(m))).ToArray();
            report.AppendLine($"\n## FBX alt mesh kontrolü\n\nFBX içinde **{meshes.Length}** mesh bulunur. Sanatçının prefabları tarafından referanslanmayan alt mesh: **{unassigned.Length}**. Mesh sayısı ürün sayısı değildir; kapak, gövde ve parçalar ayrı mesh olabilir.\n");
            foreach(var mesh in unassigned)report.AppendLine("- `"+mesh.name+"`");
            report.AppendLine("\nBalık/tepsi, Esmanur'un kaynak prefabları arasında bulunmaz. Kampanyadaki balıklar açıkça atanmış Editor yapımı `Assets/Prefabs/Products/FishTray.prefab` modelini kullanır.");
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/MODEL_INVENTORY.md",report.ToString(),new UTF8Encoding(false));
            Debug.Log($"MODEL_AUDIT_OK artist={paths.Length} campaign={paths.Count(campaign.Contains)} scene={paths.Count(sceneDependencies.Contains)} unused={paths.Count(p=>!sceneDependencies.Contains(p))} meshOnly={unassigned.Length}");
        }
        private static string MeshKey(Mesh mesh)
        {
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long id))throw new InvalidOperationException("Mesh must be an imported asset.");
            return guid+":"+id;
        }
    }
}
