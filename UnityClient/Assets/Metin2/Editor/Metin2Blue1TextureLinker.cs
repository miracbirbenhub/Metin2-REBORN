#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metin2Reborn.Editor
{
    public static class Metin2Blue1TextureLinker
    {
        private const string TextureRoot = "Assets/Metin2/Imported/Blue1Required/Textures";
        private const string PrefabRoot = "Assets/Metin2/Generated/Prefabs";
        private const string MaterialRoot = "Assets/Metin2/Generated/Materials/Blue1";
        private const string SourceClientRoot = "C:/Users/roxy/OneDrive/Masaüstü/Metin2BE-Client-master";

        [MenuItem("Metin2/Blue 1/Link Building Textures")]
        public static void Link()
        {
            AssetDatabase.Refresh();
            EnsureFolder(MaterialRoot);

            var textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;
                textures[Normalize(Path.GetFileNameWithoutExtension(path))] = tex;
            }

            int prefabCount = 0, materialCount = 0, rendererCount = 0, linked = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                bool changed = false;
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    var sourceMaterials = renderer.sharedMaterials;
                    var materials = new Material[sourceMaterials.Length];

                    for (int i = 0; i < sourceMaterials.Length; i++)
                    {
                        Material source = sourceMaterials[i];
                        if (source == null) continue;

                        Material mat = new Material(source);
                        mat.name = prefab.name + "_" + renderer.name + "_" + i;
                        string matPath = MaterialRoot + "/" + Sanitize(mat.name) + ".mat";

                        Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (existing != null) UnityEngine.Object.DestroyImmediate(mat);
                        else
                        {
                            AssetDatabase.CreateAsset(mat, matPath);
                            existing = mat;
                            materialCount++;
                        }

                        Texture2D tex = FindTexture(textures, source.name, renderer.name, prefab.name, AssetDatabase.GetAssetPath(source));
                        if (tex == null)
                            tex = FindFamilyTexture(textures, prefab.name, source.name, renderer.name, i);
                        if (tex != null)
                        {
                            SetTexture(existing, tex);
                            linked++;
                            changed = true;
                        }

                        materials[i] = existing;
                    }

                    renderer.sharedMaterials = materials;
                    rendererCount++;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    prefabCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Metin2 Blue 1 texture link: {prefabCount} prefab, {rendererCount} renderer, {materialCount} material, {linked} texture baglantisi.");
        }

        [MenuItem("Metin2/Blue 1/Force Source Texture Link")]
        public static void ForceSourceLink()
        {
            AssetDatabase.Refresh();

            var textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                    textures[Normalize(Path.GetFileNameWithoutExtension(path))] = tex;
            }

            int prefabCount = 0, rendererCount = 0, linked = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Blue1Required/")) continue;

                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                bool changed = false;
                var family = FindSourceTextureFamily(textures, prefab.name);

                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == null || family.Count == 0) continue;

                        Texture2D tex = null;
                        string rendererKey = Normalize(renderer.name);
                        string materialKey = Normalize(materials[i].name);

                        foreach (var pair in family)
                        {
                            if ((!string.IsNullOrEmpty(rendererKey) && (pair.Key.Contains(rendererKey) || rendererKey.Contains(pair.Key))) ||
                                (!string.IsNullOrEmpty(materialKey) && (pair.Key.Contains(materialKey) || materialKey.Contains(pair.Key))))
                            {
                                tex = pair.Value;
                                break;
                            }
                        }

                        if (tex == null)
                            tex = family[Mathf.Clamp(i, 0, family.Count - 1)].Value;

                        if (tex != null)
                        {
                            Material mat = new Material(materials[i]);
                            mat.name = prefab.name + "_" + renderer.name + "_Source_" + i;
                            SetTexture(mat, tex);

                            string matPath = MaterialRoot + "/" + Sanitize(mat.name) + ".mat";
                            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (existing == null)
                            {
                                AssetDatabase.CreateAsset(mat, matPath);
                                existing = mat;
                            }
                            else
                            {
                                UnityEngine.Object.DestroyImmediate(mat);
                            }

                            materials[i] = existing;
                            linked++;
                            changed = true;
                        }
                    }

                    renderer.sharedMaterials = materials;
                    rendererCount++;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    prefabCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Metin2 Blue 1 FORCE texture link: {prefabCount} prefab, {rendererCount} renderer, {linked} texture baglantisi.");
        }

        private static List<KeyValuePair<string, Texture2D>> FindSourceTextureFamily(Dictionary<string, Texture2D> textures, string prefabName)
        {
            var result = new List<KeyValuePair<string, Texture2D>>();
            if (!Directory.Exists(SourceClientRoot)) return result;

            string prefabKey = Normalize(prefabName);
            foreach (string gr2 in Directory.GetFiles(SourceClientRoot, "*.gr2", SearchOption.AllDirectories))
            {
                if (!string.Equals(Normalize(Path.GetFileNameWithoutExtension(gr2)), prefabKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                string dir = Path.GetDirectoryName(gr2);
                if (string.IsNullOrEmpty(dir)) break;

                string baseKey = Normalize(Path.GetFileNameWithoutExtension(gr2));
                foreach (string dds in Directory.GetFiles(dir, "*.dds", SearchOption.TopDirectoryOnly))
                {
                    string key = Normalize(Path.GetFileNameWithoutExtension(dds));
                    if (textures.TryGetValue(key, out var tex))
                        result.Add(new KeyValuePair<string, Texture2D>(key, tex));
                }

                // Exact same-name texture gets priority and becomes the first slot.
                result.Sort((a, b) => {
                    bool ae = string.Equals(a.Key, baseKey, StringComparison.OrdinalIgnoreCase);
                    bool be = string.Equals(b.Key, baseKey, StringComparison.OrdinalIgnoreCase);
                    return be.CompareTo(ae);
                });

                break;
            }

            return result;
        }

        private static Texture2D FindFamilyTexture(Dictionary<string, Texture2D> textures, string prefabName, string materialName, string rendererName, int slot)
        {
            if (!Directory.Exists(SourceClientRoot)) return null;
            string prefabKey = Normalize(prefabName);
            if (string.IsNullOrEmpty(prefabKey)) return null;

            foreach (string gr2 in Directory.GetFiles(SourceClientRoot, "*.gr2", SearchOption.AllDirectories))
            {
                if (!string.Equals(Normalize(Path.GetFileNameWithoutExtension(gr2)), prefabKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                string dir = Path.GetDirectoryName(gr2);
                if (string.IsNullOrEmpty(dir)) return null;

                var candidates = new List<KeyValuePair<string, Texture2D>>();
                foreach (string dds in Directory.GetFiles(dir, "*.dds", SearchOption.TopDirectoryOnly))
                {
                    string key = Normalize(Path.GetFileNameWithoutExtension(dds));
                    if (textures.TryGetValue(key, out var tex))
                        candidates.Add(new KeyValuePair<string, Texture2D>(key, tex));
                }

                if (candidates.Count == 0) return null;

                string materialKey = Normalize(materialName);
                string rendererKey = Normalize(rendererName);

                foreach (var candidate in candidates)
                {
                    if ((!string.IsNullOrEmpty(materialKey) && (candidate.Key.Contains(materialKey) || materialKey.Contains(candidate.Key))) ||
                        (!string.IsNullOrEmpty(rendererKey) && (candidate.Key.Contains(rendererKey) || rendererKey.Contains(candidate.Key))))
                        return candidate.Value;
                }

                return candidates[Mathf.Clamp(slot, 0, candidates.Count - 1)].Value;
            }
            return null;
        }

        private static Texture2D FindTexture(Dictionary<string, Texture2D> textures, params string[] names)
        {
            foreach (string name in names)
            {
                string key = Normalize(name);
                if (string.IsNullOrEmpty(key)) continue;
                if (textures.TryGetValue(key, out var exact)) return exact;
            }

            foreach (string name in names)
            {
                string key = Normalize(name);
                if (key.Length < 4) continue;
                foreach (var pair in textures)
                {
                    if (pair.Key.Contains(key) || key.Contains(pair.Key))
                        return pair.Value;
                }
            }
            return null;
        }

        private static void SetTexture(Material mat, Texture2D tex)
        {
            if (mat == null || tex == null) return;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return Path.GetFileNameWithoutExtension(value)
                .ToLowerInvariant()
                .Replace("_", "").Replace("-", "").Replace(" ", "")
                .Replace(".", "").Replace("(", "").Replace(")", "");
        }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c.ToString(), "_");
            return value.Replace("/", "_").Replace("\\", "_");
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif