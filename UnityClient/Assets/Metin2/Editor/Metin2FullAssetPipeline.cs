#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metin2Reborn.Editor
{
    public static class Metin2FullAssetPipeline
    {
        private const string Root = "Assets/Metin2";
        private const string Generated = Root + "/Generated";
        private const string GeneratedPrefabs = Generated + "/Prefabs";

        [MenuItem("Metin2/Full Asset Pipeline/Scan & Build All")]
        public static void ScanAndBuildAll()
        {
            EnsureFolderPath(GeneratedPrefabs);
            AssetDatabase.Refresh();

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Root });
            int built = 0;
            int skipped = 0;
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsSupportedModel(path)) continue;

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    skipped++;
                    continue;
                }

                string category = GetCategory(path);
                if (!counts.ContainsKey(category)) counts[category] = 0;
                counts[category]++;

                string folder = GeneratedPrefabs + "/" + category;
                EnsureFolderPath(folder);

                string safeName = Sanitize(Path.GetFileNameWithoutExtension(path));
                string prefabPath = folder + "/" + safeName + ".prefab";
                BuildPrefab(model, prefabPath, category);
                built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Metin2 Full Asset Pipeline: {built} prefab üretildi, {skipped} model atlandı.");
            foreach (var pair in counts)
                Debug.Log($"Metin2 Asset Category: {pair.Key} = {pair.Value}");
        }

        [MenuItem("Metin2/Full Asset Pipeline/Scan Only")]
        public static void ScanOnly()
        {
            AssetDatabase.Refresh();
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Root });
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsSupportedModel(path)) continue;

                string category = GetCategory(path);
                if (!counts.ContainsKey(category)) counts[category] = 0;
                counts[category]++;
            }

            Debug.Log("=== METIN2 ASSET SCAN ===");
            int total = 0;
            foreach (var pair in counts)
            {
                Debug.Log($"{pair.Key}: {pair.Value}");
                total += pair.Value;
            }
            Debug.Log($"TOPLAM MODEL: {total}");
        }

        private static void BuildPrefab(GameObject source, string prefabPath, string category)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(source);

            instance.name = source.name;

            if (category == "Characters" || category == "Monsters" ||
                category == "NPCs" || category == "Mounts")
            {
                Animator animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.applyRootMotion = false;
            }

            if (category == "Characters")
            {
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;
            }

            if (category == "Monsters" || category == "NPCs" || category == "Mounts")
                AddSafeCapsuleCollider(instance);

            if (category == "Items" || category == "Weapons" || category == "Armor")
                AddSafeBoxCollider(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void AddSafeCapsuleCollider(GameObject root)
        {
            if (root.GetComponent<Collider>() != null) return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = root.transform.InverseTransformPoint(b.center);
            collider.height = Mathf.Max(0.2f, b.size.y);
            collider.radius = Mathf.Max(0.05f, Mathf.Min(b.size.x, b.size.z) * 0.35f);
            collider.direction = 1;
        }

        private static void AddSafeBoxCollider(GameObject root)
        {
            if (root.GetComponent<Collider>() != null) return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(b.center);
            collider.size = b.size;
        }

        private static string GetCategory(string path)
        {
            string p = path.Replace("\\", "/").ToLowerInvariant();

            if (p.Contains("/characters/") || p.Contains("/warrior/") ||
                p.Contains("/ninja/") || p.Contains("/sura/") || p.Contains("/shaman/"))
                return "Characters";
            if (p.Contains("/monster") || p.Contains("/mob/") || p.Contains("/boss/"))
                return "Monsters";
            if (p.Contains("/npc/")) return "NPCs";
            if (p.Contains("/weapon") || p.Contains("/sword") || p.Contains("/dagger") ||
                p.Contains("/bow") || p.Contains("/bell") || p.Contains("/fan"))
                return "Weapons";
            if (p.Contains("/armor") || p.Contains("/armour")) return "Armor";
            if (p.Contains("/mount") || p.Contains("/horse")) return "Mounts";
            if (p.Contains("/item") || p.Contains("/icon")) return "Items";
            if (p.Contains("/map") || p.Contains("/maps") || p.Contains("/terrain") ||
                p.Contains("/environment"))
                return "Maps";
            if (p.Contains("/effect") || p.Contains("/skill") || p.Contains("/fx"))
                return "Effects";

            return "Other";
        }

        private static bool IsSupportedModel(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".fbx" || ext == ".obj" || ext == ".dae" ||
                   ext == ".gltf" || ext == ".glb";
        }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c.ToString(), "_");
            return value;
        }

        private static void EnsureFolderPath(string folder)
        {
            folder = folder.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
