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
                            tex = FindNearbySourceTexture(textures, prefab.name, source.name, renderer.name);
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

        private static Texture2D FindNearbySourceTexture(Dictionary<string, Texture2D> textures, string prefabName, string materialName, string rendererName)
        {
            if (!Directory.Exists(SourceClientRoot)) return null;

            string targetKey = Normalize(prefabName);
            string materialKey = Normalize(materialName);
            string rendererKey = Normalize(rendererName);

            foreach (string gr2 in Directory.GetFiles(SourceClientRoot, "*.gr2", SearchOption.AllDirectories))
            {
                if (!string.Equals(Normalize(Path.GetFileNameWithoutExtension(gr2)), targetKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                string dir = Path.GetDirectoryName(gr2);
                if (string.IsNullOrEmpty(dir)) return null;

                string[] nearby = Directory.GetFiles(dir, "*.dds", SearchOption.AllDirectories);
                Texture2D fallback = null;

                foreach (string dds in nearby)
                {
                    string key = Normalize(Path.GetFileNameWithoutExtension(dds));
                    if (string.IsNullOrEmpty(key)) continue;

                    if (!string.IsNullOrEmpty(materialKey) &&
                        (key.Contains(materialKey) || materialKey.Contains(key)))
                    {
                        string unityPath = FindUnityTexturePath(textures, key);
                        if (unityPath != null) return textures[key];
                    }

                    if (!string.IsNullOrEmpty(rendererKey) &&
                        (key.Contains(rendererKey) || rendererKey.Contains(key)))
                    {
                        string unityPath = FindUnityTexturePath(textures, key);
                        if (unityPath != null) return textures[key];
                    }

                    string unityKey = key;
                    if (textures.TryGetValue(unityKey, out var candidate))
                        fallback = candidate;
                }

                return fallback;
            }

            return null;
        }

        private static string FindUnityTexturePath(Dictionary<string, Texture2D> textures, string key)
        {
            return textures.ContainsKey(key) ? key : null;
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