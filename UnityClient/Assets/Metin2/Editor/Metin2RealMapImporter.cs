#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metin2Reborn.Editor
{
    public static class Metin2RealMapImporter
    {
        private const string SourceMap =
            @"C:\Users\roxy\OneDrive\Masaüstü\Metin2BE-Client-master\metin2_map_empire\metin2_empire_blue_1";

        private const string ScenePath = "Assets/Metin2/Scenes/Blue1RealMap.unity";
        private const string DataRoot = "Assets/Metin2/Generated/Maps/Blue1";

        private const int ChunkColumns = 5;
        private const int ChunkRows = 4;
        private const int SourceHeightResolution = 131;
        private const int UnityHeightResolution = 129;

        private const float WorldScale = 0.01f;
        private const float CellScale = 200f;
        private const float HeightScale = 0.5f;

        [MenuItem("Metin2/Import REAL Blue 1 Map")]
        public static void ImportBlue1()
        {
            if (!Directory.Exists(SourceMap))
                throw new Exception("Blue 1 source map bulunamadı: " + SourceMap);

            EnsureFolder("Assets/Metin2");
            EnsureFolder("Assets/Metin2/Scenes");
            EnsureFolder("Assets/Metin2/Generated");
            EnsureFolder("Assets/Metin2/Generated/Maps");
            EnsureFolder(DataRoot);
            EnsureFolder("Assets/Metin2/Generated/Maps/Blue1/Textures");

            AssetDatabase.StartAssetEditing();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildLighting();
                ImportBlue1Textures();

                float chunkSize = 128f * CellScale * WorldScale;
                float totalWidth = ChunkColumns * chunkSize;
                float totalDepth = ChunkRows * chunkSize;

                GameObject mapRoot = new GameObject("METIN2_BLUE_1_REAL_MAP");
                mapRoot.transform.position = new Vector3(-totalWidth * 0.5f, 0f, -totalDepth * 0.5f);

                for (int row = 0; row < ChunkRows; row++)
                {
                    for (int col = 0; col < ChunkColumns; col++)
                    {
                        string chunkName = row.ToString("D3") + col.ToString("D3");
                        string chunkPath = Path.Combine(SourceMap, chunkName);

                        if (!Directory.Exists(chunkPath))
                            throw new Exception("Eksik chunk: " + chunkName);

                        CreateTerrainChunk(mapRoot.transform, chunkName, chunkPath, col, row, chunkSize);
                    }
                }

                GameObject player = BuildPlayer(mapRoot.transform);
                BuildCamera(player);
                BuildFallbackEnvironment();

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Open the generated scene immediately so the imported map is visible.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Selection.activeGameObject = GameObject.Find("METIN2_BLUE_1_REAL_MAP");

                Debug.Log("Metin2: REAL BLUE 1 import tamamlandı. 20 terrain chunk oluşturuldu.");
                Debug.Log("Metin2: Terrain heightmap = 129x129, kaynak height.raw = 131x131.");
                Debug.Log("Metin2: WorldScale=" + WorldScale + ", ChunkSize=" + chunkSize);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void BuildLighting()
        {
            GameObject sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.ambientIntensity = 1f;
        }

        private static void ImportBlue1Textures()
        {
            string sourceRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Metin2BE-Client-master\\objects\\ymir work\\terrainmaps\\b");
            string[] files =
            {
                "field\\field 01.dds","field\\field 02.dds","field\\field 03.dds","field\\field 04.dds",
                "grass\\grass 01.dds","grass\\grass 02.dds","grass\\grass 03.dds",
                "stone\\stone01.dds","stone\\stone02.dds","stone\\stone03.dds","stone\\stone04.dds",
                "tile\\tile01.dds","tile\\tile02.dds","tile\\tile03.dds",
                "beach\\beach sand 01.dds","beach\\beach sand 02.dds","beach\\beach sand 03.dds"
            };
            string destinationRoot = "Assets/Metin2/Generated/Maps/Blue1/Textures";
            foreach (string relative in files)
            {
                string source = Path.Combine(sourceRoot, relative);
                string name = Path.GetFileName(relative);
                string destination = Path.Combine(Application.dataPath, "Metin2/Generated/Maps/Blue1/Textures", name);
                if (!File.Exists(source)) throw new Exception("Blue 1 texture bulunamadı: " + source);
                File.Copy(source, destination, true);
            }
            AssetDatabase.Refresh();
            Debug.Log("Metin2: Blue 1 için 17 gerçek terrain DDS texture Unity projesine kopyalandı.");
        }

        private static void CreateTerrainChunk(
            Transform parent,
            string chunkName,
            string chunkPath,
            int col,
            int row,
            float chunkSize)
        {
            string heightPath = Path.Combine(chunkPath, "height.raw");
            if (!File.Exists(heightPath))
                throw new Exception("height.raw bulunamadı: " + heightPath);

            byte[] bytes = File.ReadAllBytes(heightPath);
            if (bytes.Length != SourceHeightResolution * SourceHeightResolution * 2)
                throw new Exception(chunkName + " height.raw beklenmeyen boyutta: " + bytes.Length);

            ushort[,] source = new ushort[SourceHeightResolution, SourceHeightResolution];
            ushort min = ushort.MaxValue;
            ushort max = ushort.MinValue;

            for (int y = 0; y < SourceHeightResolution; y++)
            {
                for (int x = 0; x < SourceHeightResolution; x++)
                {
                    int index = (y * SourceHeightResolution + x) * 2;
                    ushort value = BitConverter.ToUInt16(bytes, index);
                    source[y, x] = value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            TerrainData data = new TerrainData
            {
                heightmapResolution = UnityHeightResolution,
                size = new Vector3(
                    chunkSize,
                    Mathf.Max(1f, (max - min) * HeightScale * WorldScale),
                    chunkSize),
                baseMapResolution = 128,
                alphamapResolution = 128
            };

            float[,] heights = new float[UnityHeightResolution, UnityHeightResolution];
            float range = Mathf.Max(1f, max - min);

            for (int y = 0; y < UnityHeightResolution; y++)
            {
                for (int x = 0; x < UnityHeightResolution; x++)
                {
                    ushort value = source[y + 1, x + 1];
                    heights[y, x] = Mathf.Clamp01((value - min) / range);
                }
            }

            data.SetHeights(0, 0, heights);

            string assetPath = DataRoot + "/Terrain_" + chunkName + ".asset";
            AssetDatabase.CreateAsset(data, assetPath);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terrain_" + chunkName;
            terrainObject.transform.SetParent(parent, false);

            // Keep the imported map at a playable local elevation. The source
            // absolute height values are not needed for the Unity world origin.
            terrainObject.transform.localPosition = new Vector3(
                col * chunkSize,
                0f,
                row * chunkSize);

            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 2000f;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            material.name = "Blue1_Terrain_" + chunkName + "_Material";
            material.color = new Color(0.24f, 0.38f, 0.20f);
            terrain.materialTemplate = material;
        }

        private static GameObject BuildPlayer(Transform mapRoot)
        {
            string path = "Assets/Metin2/Characters/Warrior/Warrior_Novice.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new Exception("Warrior_Novice.prefab bulunamadı: " + path);

            GameObject visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual == null)
                throw new Exception("Warrior prefab instance oluşturulamadı.");

            visual.name = "Warrior_Visual";
            visual.transform.rotation = FindBestStandingRotation(visual);
            Metin2AssetPipeline.ConfigureForPlayableInstance(visual);

            GameObject player = new GameObject("Warrior_Player");
            player.tag = "Player";
            player.transform.SetParent(mapRoot, false);

            // The terrain is normalized to start at local Y=0, so a small
            // positive spawn height places the player above the ground.
            player.transform.localPosition = new Vector3(0f, 8f, 0f);
            player.transform.rotation = Quaternion.identity;

            visual.transform.SetParent(player.transform, true);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = Vector3.up * 0.9f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;

            player.AddComponent<Metin2PlayerController>();
            player.AddComponent<Metin2AutoAttacker>();
            player.AddComponent<Metin2Inventory>();

            return player;
        }

        private static void BuildCamera(GameObject player)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 2000f;
            cameraObject.transform.position = player.transform.position + new Vector3(0f, 5.2f, -7.5f);
            cameraObject.transform.LookAt(player.transform.position + Vector3.up);

            Metin2FollowCamera follow = cameraObject.AddComponent<Metin2FollowCamera>();
            follow.SetTarget(player.transform);
        }

        private static void BuildFallbackEnvironment()
        {
            GameObject sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.ambientIntensity = 1f;

            GameObject info = new GameObject("BLUE_1_IMPORT_INFO");
            info.AddComponent<Blue1ImportInfo>();
        }

        private static Quaternion FindBestStandingRotation(GameObject player)
        {
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return Quaternion.identity;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 size = bounds.size;
            Quaternion[] candidates =
            {
                Quaternion.identity,
                Quaternion.Euler(90f, 0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f),
                Quaternion.Euler(0f, 0f, 90f),
                Quaternion.Euler(0f, 0f, -90f),
                Quaternion.Euler(180f, 0f, 0f),
                Quaternion.Euler(0f, 180f, 0f)
            };

            float bestScore = float.MinValue;
            Quaternion best = Quaternion.identity;

            foreach (Quaternion rotation in candidates)
            {
                Vector3 rotated = Abs(rotation * size);
                float horizontal = Mathf.Max(rotated.x, rotated.z);
                float score = rotated.y / Mathf.Max(horizontal, 0.001f);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = rotation;
                }
            }

            return best;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
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

        private sealed class Blue1ImportInfo : MonoBehaviour
        {
        }
    }
}
#endif
