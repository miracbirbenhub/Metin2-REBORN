#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Metin2Reborn.Editor
{
    public static class Metin2Blue1PrefabBuilder
    {
        private const string ModelRoot = "Assets/Metin2/Imported/Blue1Required";
        private const string OutputRoot = "Assets/Metin2/Generated/Prefabs/Blue1Required";

        [MenuItem("Metin2/Blue 1/Build Required Building Prefabs")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            EnsureFolder(OutputRoot);

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelRoot });
            int models = 0, built = 0, renderers = 0;

            foreach (string guid in guids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) continue;

                models++;
                string fileName = Path.GetFileNameWithoutExtension(modelPath);
                string safeName = Sanitize(fileName);
                string prefabPath = OutputRoot + "/" + safeName + ".prefab";

                GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null) continue;

                instance.name = safeName;

                Renderer[] rs = instance.GetComponentsInChildren<Renderer>(true);
                renderers += rs.Length;

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                UnityEngine.Object.DestroyImmediate(instance);
                built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Metin2 Blue 1 prefab build: {models} FBX bulundu, {built} prefab oluşturuldu, {renderers} renderer.");
        }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c.ToString(), "_");
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
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
