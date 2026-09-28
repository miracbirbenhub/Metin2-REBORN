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
        private const int TileRawResolution = 258;
        private const int SplatResolution = 256;
        private const int TerrainTextureCount = 17;

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

            // Import the PNG files before StartAssetEditing so Unity creates
            // usable Texture2D assets before TerrainLayer creation and splat setup.
            ImportBlue1Textures();
            CreateBlue1TerrainLayers();

            AssetDatabase.StartAssetEditing();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildLighting();

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
                BuildBlue1EnvironmentObjects(mapRoot.transform, chunkSize);

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();

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

        private static void CreateBlue1TerrainLayers()
        {
            string layerRoot = DataRoot + "/TerrainLayers";
            EnsureFolder(layerRoot);

            Texture2D[] textures = new Texture2D[TerrainTextureCount];
            TerrainLayer[] layers = new TerrainLayer[TerrainTextureCount];

            string[] files =
            {
                "field 01.png", "field 02.png", "field 03.png", "field 04.png",
                "grass 01.png", "grass 02.png", "grass 03.png",
                "stone01.png", "stone02.png", "stone03.png", "stone04.png",
                "tile01.png", "tile02.png", "tile03.png",
                "beach sand 01.png", "beach sand 02.png", "beach sand 03.png"
            };

            for (int i = 0; i < TerrainTextureCount; i++)
            {
                string texturePath = "Assets/Metin2/Generated/Maps/Blue1/Textures/" + files[i];
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (textures[i] == null)
                    throw new Exception("Blue 1 texture Unity asset olarak yüklenemedi: " + texturePath);

                string layerPath = layerRoot + "/Blue1_" + i.ToString("D2") + ".terrainlayer";
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if (layer == null)
                {
                    layer = new TerrainLayer();
                    AssetDatabase.CreateAsset(layer, layerPath);
                }

                layer.diffuseTexture = textures[i];
                layer.tileSize = new Vector2(20f, 20f);
                layer.tileOffset = Vector2.zero;
                EditorUtility.SetDirty(layer);
                layers[i] = layer;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Metin2: Blue 1 için 17 TerrainLayer oluşturuldu.");
        }

        private static void ApplyBlue1TextureSplat(TerrainData data, string tilePath)
        {
            if (!File.Exists(tilePath))
                throw new Exception("tile.raw bulunamadı: " + tilePath);

            byte[] bytes = File.ReadAllBytes(tilePath);
            if (bytes.Length != TileRawResolution * TileRawResolution)
                throw new Exception("tile.raw beklenmeyen boyutta: " + bytes.Length);

            // URP Terrain Lit supports up to 8 terrain layers. Blue 1's
            // tile.raw currently uses only raw values 1,4,5,6,8,11, so we
            // keep exactly those six source textures and remap them to slots 0-5.
            int[] sourceValues = { 1, 4, 5, 6, 8, 11 };
            TerrainLayer[] layers = new TerrainLayer[sourceValues.Length];
            for (int i = 0; i < sourceValues.Length; i++)
            {
                string path = DataRoot + "/TerrainLayers/Blue1_" +
                              (sourceValues[i] - 1).ToString("D2") + ".terrainlayer";
                layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layers[i] == null)
                    throw new Exception("Blue 1 TerrainLayer bulunamadı: " + path);
            }
            data.terrainLayers = layers;

            float[,,] alpha = new float[SplatResolution, SplatResolution, sourceValues.Length];

            for (int y = 0; y < SplatResolution; y++)
            {
                int sourceY = TileRawResolution - 2 - y;

                for (int x = 0; x < SplatResolution; x++)
                {
                    int sourceX = x + 1;
                    byte raw = bytes[sourceY * TileRawResolution + sourceX];

                    int slot = Array.IndexOf(sourceValues, (int)raw);
                    if (slot < 0) slot = 0;

                    alpha[y, x, slot] = 1f;
                }
            }

            data.SetAlphamaps(0, 0, alpha);
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
            string sourceRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Metin2BE-Client-master\\_BLUE1_PNG");
            string[] files =
            {
                "field 01.png","field 02.png","field 03.png","field 04.png",
                "grass 01.png","grass 02.png","grass 03.png",
                "stone01.png","stone02.png","stone03.png","stone04.png",
                "tile01.png","tile02.png","tile03.png",
                "beach sand 01.png","beach sand 02.png","beach sand 03.png"
            };
            string destinationRoot = "Assets/Metin2/Generated/Maps/Blue1/Textures";
            foreach (string name in files)
            {
                string source = Path.Combine(sourceRoot, name);
                string destination = Path.Combine(Application.dataPath, "Metin2/Generated/Maps/Blue1/Textures", name);
                if (!File.Exists(source))
                    throw new Exception("Blue 1 PNG bulunamadı: " + source);
                File.Copy(source, destination, true);
            }

            AssetDatabase.Refresh();

            string[] importedFiles =
            {
                "field 01.png","field 02.png","field 03.png","field 04.png",
                "grass 01.png","grass 02.png","grass 03.png",
                "stone01.png","stone02.png","stone03.png","stone04.png",
                "tile01.png","tile02.png","tile03.png",
                "beach sand 01.png","beach sand 02.png","beach sand 03.png"
            };

            foreach (string name in importedFiles)
            {
                string assetPath = destinationRoot + "/" + name;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (string name in importedFiles)
            {
                string assetPath = destinationRoot + "/" + name;
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log("Metin2: Blue 1 için 17 gerçek terrain PNG Unity projesine kopyalandı ve senkron import edildi.");
        }

        private static Texture2D ImportBlue1MinimapTexture(string chunkName, string chunkPath)
        {
            string source = Path.Combine(chunkPath, "minimap.dds");
            if (!File.Exists(source))
                throw new Exception("minimap.dds bulunamadı: " + source);

            byte[] dds = File.ReadAllBytes(source);
            if (dds.Length < 128 || dds[0] != 'D' || dds[1] != 'D' || dds[2] != 'S' || dds[3] != ' ')
                throw new Exception("Geçersiz minimap DDS: " + source);

            int height = BitConverter.ToInt32(dds, 12);
            int width = BitConverter.ToInt32(dds, 16);
            int fourCC = BitConverter.ToInt32(dds, 84);

            if (width != 256 || height != 256)
                throw new Exception($"Beklenmeyen minimap boyutu: {width}x{height}");
            if (fourCC != 0x31545844)
                throw new Exception($"Blue 1 minimap DDS formatı DXT1 değil. FourCC=0x{fourCC:X8}, size={dds.Length}");

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            Color32[] pixels = new Color32[width * height];

            int offset = 128;
            for (int by = 0; by < height; by += 4)
            {
                for (int bx = 0; bx < width; bx += 4)
                {
                    ushort c0 = BitConverter.ToUInt16(dds, offset);
                    ushort c1 = BitConverter.ToUInt16(dds, offset + 2);
                    uint bits = BitConverter.ToUInt32(dds, offset + 4);
                    offset += 8;

                    Color32[] palette = DecodeDxt1Palette(c0, c1);
                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int index = (int)((bits >> (2 * (py * 4 + px))) & 3);
                            int x = bx + px;
                            int y = by + py;
                            if (x < width && y < height)
                                pixels[(height - 1 - y) * width + x] = palette[index];
                        }
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            texture.name = "Blue1_Minimap_" + chunkName;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            // Keep the decoded texture as a native Unity asset. This avoids
            // the AssetDatabase timing issue where a freshly-written PNG can
            // return null from LoadAssetAtPath during the same import pass.
            string folder = "Assets/Metin2/Generated/Maps/Blue1/Minimap";
            EnsureFolder(folder);
            string assetPath = folder + "/minimap_" + chunkName + ".asset";

            UnityEngine.Object old = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (old != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(texture, assetPath);
            AssetDatabase.SaveAssets();

            // CreateAsset serializes the object and keeps the original Texture2D
            // reference valid. Do not immediately round-trip through
            // LoadAssetAtPath here; Unity can defer visibility of the new asset
            // until the current AssetDatabase operation finishes.
            return texture;
        }

        private static Color32[] DecodeDxt1Palette(ushort c0, ushort c1)
        {
            Color32[] p = new Color32[4];
            p[0] = Rgb565(c0, 255);
            p[1] = Rgb565(c1, 255);
            if (c0 > c1)
            {
                p[2] = LerpRgb(p[0], p[1], 2, 1, 3);
                p[3] = LerpRgb(p[0], p[1], 1, 2, 3);
            }
            else
            {
                p[2] = LerpRgb(p[0], p[1], 1, 1, 2);
                p[3] = new Color32(0, 0, 0, 0);
            }
            return p;
        }

        private static Color32 Rgb565(ushort value, byte alpha)
        {
            byte r = (byte)(((value >> 11) & 31) * 255 / 31);
            byte g = (byte)(((value >> 5) & 63) * 255 / 63);
            byte b = (byte)((value & 31) * 255 / 31);
            return new Color32(r, g, b, alpha);
        }

        private static Color32 LerpRgb(Color32 a, Color32 b, int wa, int wb, int div)
        {
            return new Color32(
                (byte)((a.r * wa + b.r * wb) / div),
                (byte)((a.g * wa + b.g * wb) / div),
                (byte)((a.b * wa + b.b * wb) / div),
                255);
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
                alphamapResolution = SplatResolution
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

            // Use the original Metin2 minimap tile as the ground diffuse.
            // minimap.dds is a 256x256 visual representation of each sectree,
            // so this gives us an exact visual reference while we finish the
            // proprietary tile.raw splat interpretation.
            Texture2D minimap = ImportBlue1MinimapTexture(chunkName, chunkPath);
            TerrainLayer minimapLayer = new TerrainLayer
            {
                diffuseTexture = minimap,
                tileSize = new Vector2(chunkSize, chunkSize),
                tileOffset = Vector2.zero
            };
            string layerAssetPath = DataRoot + "/TerrainLayers/Blue1_Minimap_" + chunkName + ".terrainlayer";
            AssetDatabase.CreateAsset(minimapLayer, layerAssetPath);
            data.terrainLayers = new[] { minimapLayer };

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
            // Blue 1 uses the URP Terrain Lit shader. Do not tint the
            // terrain with a generic material color: that was masking the
            // imported splat textures and made the whole map brown.
            // This project uses URP. URP Terrain Lit is required for
            // Terrain Layers; BuiltInStandard uses the built-in pipeline shader
            // and can make the terrain disappear in a URP project.
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
                throw new Exception("URP Terrain/Lit shader bulunamadı.");

            Material material = new Material(terrainShader);
            material.name = "Blue1_Terrain_" + chunkName + "_Material";
            terrain.materialType = Terrain.MaterialType.Custom;
            terrain.materialTemplate = material;
        }

        private static void BuildBlue1EnvironmentObjects(Transform mapRoot, float chunkSize)
        {
            const string prefabRoot = "Assets/Metin2/Generated/Prefabs/Other";
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            if (guids.Length == 0)
            {
                Debug.LogWarning("Blue 1: Other prefab bulunamadı; dekorasyon atlandı.");
                return;
            }

            // Prefer obvious world-decoration names. If the imported client build
            // uses different names, fall back to the first available Other prefabs.
            System.Collections.Generic.List<GameObject> candidates = new System.Collections.Generic.List<GameObject>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string n = prefab.name.ToLowerInvariant();
                if (n.Contains("tree") || n.Contains("rock") || n.Contains("stone") ||
                    n.Contains("bush") || n.Contains("grass") || n.Contains("flower") ||
                    n.Contains("wood") || n.Contains("fence") || n.Contains("house") ||
                    n.Contains("building") || n.Contains("object"))
                {
                    candidates.Add(prefab);
                }
            }

            if (candidates.Count == 0)
            {
                foreach (string guid in guids)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (prefab != null) candidates.Add(prefab);
                }
            }

            GameObject root = new GameObject("BLUE_1_WORLD_OBJECTS");
            root.transform.SetParent(mapRoot, false);

            System.Random random = new System.Random(1001);
            int placed = 0;

            for (int row = 0; row < ChunkRows; row++)
            {
                for (int col = 0; col < ChunkColumns; col++)
                {
                    int count = 3;
                    for (int i = 0; i < count; i++)
                    {
                        GameObject prefab = candidates[random.Next(candidates.Count)];
                        float x = col * chunkSize + 12f + (float)random.NextDouble() * (chunkSize - 24f);
                        float z = row * chunkSize + 12f + (float)random.NextDouble() * (chunkSize - 24f);

                        // Avoid the central player spawn area.
                        float localX = x - (ChunkColumns * chunkSize * 0.5f);
                        float localZ = z - (ChunkRows * chunkSize * 0.5f);
                        if (localX * localX + localZ * localZ < 35f * 35f)
                            continue;

                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (instance == null) continue;

                        instance.name = "Blue1_Object_" + placed.ToString("D3") + "_" + prefab.name;
                        instance.transform.SetParent(root.transform, false);
                        instance.transform.localPosition = new Vector3(
                            x - (ChunkColumns * chunkSize * 0.5f),
                            0f,
                            z - (ChunkRows * chunkSize * 0.5f));
                        instance.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

                        // Scale imported client props into the same meter convention
                        // as the map. Keep the source scale when it is already sane.
                        Vector3 s = instance.transform.localScale;
                        float maxAxis = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                        if (maxAxis > 20f)
                            instance.transform.localScale = s * 0.01f;
                        else if (maxAxis < 0.01f)
                            instance.transform.localScale = s * 100f;

                        placed++;
                    }
                }
            }

            Debug.Log("Metin2: Blue 1 dekorasyon objeleri yerleştirildi: " + placed);
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
            float totalWidth = ChunkColumns * 128f * CellScale * WorldScale;
            float totalDepth = ChunkRows * 128f * CellScale * WorldScale;
            // Joan city center / City Guard area. The classic Joan map places the city around
            // (571, 558); our imported Blue 1 world uses the same local coordinate scale.
            float cityX = 512f;
            float cityZ = 384f;
            player.transform.localPosition = new Vector3(cityX, 150f, cityZ);
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

        private static void PlacePlayerOnTerrain(GameObject player, Transform mapRoot, float localX, float localZ)
        {
            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
            Vector3 worldPoint = mapRoot.TransformPoint(new Vector3(localX, 0f, localZ));
            Terrain best = null;
            float bestDistance = float.MaxValue;

            foreach (Terrain terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                Vector3 p = terrain.GetPosition();
                Vector3 size = terrain.terrainData.size;
                if (worldPoint.x >= p.x && worldPoint.x <= p.x + size.x &&
                    worldPoint.z >= p.z && worldPoint.z <= p.z + size.z)
                {
                    best = terrain;
                    break;
                }

                float distance = Vector2.Distance(
                    new Vector2(worldPoint.x, worldPoint.z),
                    new Vector2(p.x + size.x * 0.5f, p.z + size.z * 0.5f));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = terrain;
                }
            }

            if (best != null)
            {
                float terrainY = best.SampleHeight(worldPoint) + best.GetPosition().y;
                Vector3 local = player.transform.localPosition;
                local.y = terrainY - mapRoot.position.y + 0.05f;
                player.transform.localPosition = local;
            }
            else
            {
                Vector3 local = player.transform.localPosition;
                local.y = 150f;
                player.transform.localPosition = local;
            }
        }

        private static void BuildCamera(GameObject player)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 2000f;
            cameraObject.transform.position = player.transform.position + new Vector3(0f, 5.0f, -7.5f);
            cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1.1f);

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
