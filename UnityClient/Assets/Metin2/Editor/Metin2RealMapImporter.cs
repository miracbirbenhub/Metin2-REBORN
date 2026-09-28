#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
                BuildMobileHUD(player);
                BuildFallbackEnvironment();
                BuildBlue1SourceObjects(mapRoot.transform, chunkSize);

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

            // tile.raw stores the actual TextureSet index (1..17). Do NOT use
            // a hard-coded global list: different Blue 1 chunks can use
            // different subsets of the 17 textures. Unknown values were
            // previously forced to slot 0, which turned large parts of the
            // map into the brown field texture.
            List<int> usedValues = new List<int>();
            for (int i = 0; i < bytes.Length; i++)
            {
                int value = bytes[i];
                if (value < 1 || value > TerrainTextureCount) continue;
                if (!usedValues.Contains(value))
                    usedValues.Add(value);
            }
            usedValues.Sort();

            if (usedValues.Count == 0)
                throw new Exception("tile.raw içinde geçerli Blue 1 texture index bulunamadı: " + tilePath);

            // URP can render more than four Terrain Layers by using additional
            // passes. Eight is an HDRP single-pass limit, not a hard URP limit.
            // Blue 1 chunk 001002 legitimately uses 9 textures, so keep all
            // source textures instead of aborting the entire map import.
            TerrainLayer[] layers = new TerrainLayer[usedValues.Count];
            for (int i = 0; i < usedValues.Count; i++)
            {
                int sourceValue = usedValues[i];
                string path = DataRoot + "/TerrainLayers/Blue1_" +
                              (sourceValue - 1).ToString("D2") + ".terrainlayer";
                layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layers[i] == null)
                    throw new Exception("Blue 1 TerrainLayer bulunamadı: " + path);
            }
            data.terrainLayers = layers;

            float[,,] alpha = new float[SplatResolution, SplatResolution, layers.Length];

            for (int y = 0; y < SplatResolution; y++)
            {
                // tile.raw has a one-cell border around its 256x256 payload.
                // Keep the same orientation used by the source map.
                int sourceY = TileRawResolution - 2 - y;

                for (int x = 0; x < SplatResolution; x++)
                {
                    int sourceX = x + 1;
                    int raw = bytes[sourceY * TileRawResolution + sourceX];

                    int slot = usedValues.IndexOf(raw);

                    // Border/unused values can exist in tile.raw. Find the
                    // nearest valid texture index instead of blindly using
                    // grass/brown slot 0.
                    if (slot < 0)
                    {
                        int nearest = usedValues[0];
                        int distance = Mathf.Abs(raw - nearest);
                        for (int i = 1; i < usedValues.Count; i++)
                        {
                            int d = Mathf.Abs(raw - usedValues[i]);
                            if (d < distance)
                            {
                                distance = d;
                                nearest = usedValues[i];
                            }
                        }
                        slot = usedValues.IndexOf(nearest);
                    }

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

            for (int y = 0; y < SourceHeightResolution; y++)
            {
                for (int x = 0; x < SourceHeightResolution; x++)
                {
                    int index = (y * SourceHeightResolution + x) * 2;
                    source[y, x] = BitConverter.ToUInt16(bytes, index);
                }
            }

            // IMPORTANT: height.raw stores absolute terrain heights. The previous
            // importer normalized each 256m chunk independently between its own
            // min/max values, which destroyed the real mountain profile and made
            // neighbouring chunks disagree in elevation. Metin2 uses:
            // worldHeight = rawValue * HeightScale (0.5 cm units).
            // Convert that directly to Unity metres.
            const float terrainWorldHeight = 65535f * HeightScale * WorldScale;

            TerrainData data = new TerrainData
            {
                heightmapResolution = UnityHeightResolution,
                size = new Vector3(chunkSize, terrainWorldHeight, chunkSize),
                baseMapResolution = 128,
                alphamapResolution = SplatResolution
            };

            float[,] heights = new float[UnityHeightResolution, UnityHeightResolution];

            // height.raw is stored at 131x131 while Unity terrain uses the
            // corresponding 129x129 vertex grid. The inner 129 samples are the
            // actual terrain vertices; keep the same mapping for every chunk so
            // the 20 sectors line up consistently.
            for (int y = 0; y < UnityHeightResolution; y++)
            {
                for (int x = 0; x < UnityHeightResolution; x++)
                {
                    ushort value = source[y + 1, x + 1];
                    heights[y, x] = value / 65535f;
                }
            }

            data.SetHeights(0, 0, heights);

            // Use the real Metin2 tile.raw texture index map instead of the
            // minimap image. tile.raw is the terrain's 256x256 half-cell texture
            // index grid (stored as 258x258 with a one-cell border).
            ApplyBlue1TextureSplat(data, Path.Combine(chunkPath, "tile.raw"));

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
            terrain.heightmapPixelError = 3f;
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

        private sealed class SourceObjectProperty
        {
            public uint Id;
            public string ModelName;
        }

        private sealed class SourceMapObject
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public float HeightOffset;
            public uint PropertyId;
        }

        private static void BuildBlue1SourceObjects(Transform mapRoot, float chunkSize)
        {
            string clientRoot = Directory.GetParent(SourceMap).Parent.FullName;
            Dictionary<uint, SourceObjectProperty> properties = LoadSourceProperties(clientRoot);
            Dictionary<string, GameObject> prefabs = LoadGeneratedObjectPrefabs();
            GameObject root = new GameObject("BLUE_1_SOURCE_OBJECTS");
            root.transform.SetParent(mapRoot, false);

            int placed = 0;
            int unresolvedProperty = 0;
            int unresolvedPrefab = 0;

            for (int row = 0; row < ChunkRows; row++)
            {
                for (int col = 0; col < ChunkColumns; col++)
                {
                    string chunkName = row.ToString("D3") + col.ToString("D3");
                    string chunkPath = Path.Combine(SourceMap, chunkName);
                    string areaPath = Path.Combine(chunkPath, "areadata.txt");
                    if (!File.Exists(areaPath))
                    {
                        Debug.LogWarning("Blue 1: areadata.txt yok: " + chunkName);
                        continue;
                    }

                    List<SourceMapObject> objects = ReadAreaData(areaPath);
                    foreach (SourceMapObject data in objects)
                    {
                        if (!properties.TryGetValue(data.PropertyId, out SourceObjectProperty property))
                        {
                            unresolvedProperty++;
                            continue;
                        }

                        string key = NormalizeModelKey(property.ModelName);
                        GameObject prefab;
                        if (!prefabs.TryGetValue(key, out prefab))
                        {
                            prefab = FindPrefabByModelSuffix(prefabs, key);
                        }
                        if (prefab == null)
                        {
                            unresolvedPrefab++;
                            continue;
                        }

                        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (instance == null) continue;

                        instance.name = "Blue1_Source_" + placed.ToString("D5") + "_" + prefab.name;
                        instance.transform.SetParent(root.transform, false);

                        // Metin2 AreaData uses centimeters. X is east/west and
                        // Y is the north/south axis stored as negative values.
                        // Our Unity map is centered around (0,0), so convert the
                        // source coordinates into the same meter convention.
                        // Blue 1 source coordinates are absolute map coordinates in
                        // centimeters. The imported terrain is normalized so its lower-left
                        // corner is Unity (0,0,0). Blue 1 is 5 x 4 chunks = 1280 x 1024
                        // cells/meters after the 0.01 scale, so the map center is:
                        // X = 640m, Z = 512m. Source Y is the north/south axis and is negative.
                        float worldX = data.Position.x * WorldScale;
                        float worldZ = -data.Position.y * WorldScale;
                        float totalMapWidth = ChunkColumns * chunkSize;
                        float totalMapDepth = ChunkRows * chunkSize;
                        float localX = worldX;
                        float localZ = worldZ;

                        // mapRoot is currently kept at the terrain's lower-left origin.
                        // Do NOT subtract 640/512 here: those values would shift every
                        // source object away from the corresponding terrain chunk.
                        localX = Mathf.Clamp(localX, -chunkSize, totalMapWidth + chunkSize);
                        localZ = Mathf.Clamp(localZ, -chunkSize, totalMapDepth + chunkSize);

                        instance.transform.localPosition = new Vector3(localX, 0f, localZ);

                        // Noesis converts the GR2 geometry, but many original Metin2
                        // building models carry the same axis convention that initially
                        // made the Warrior appear on its head. Correct the model's local
                        // up-axis first, then apply the AreaData's original map rotation.
                        Quaternion modelCorrection = FindBestStandingRotation(instance);
                        Quaternion sourceRotation = Quaternion.Euler(
                            data.Rotation.x,
                            data.Rotation.y,
                            data.Rotation.z);
                        instance.transform.localRotation = sourceRotation * modelCorrection;

                        Vector3 scale = instance.transform.localScale;
                        float maxAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                        if (maxAxis > 20f) instance.transform.localScale = scale * 0.01f;
                        else if (maxAxis < 0.01f) instance.transform.localScale = scale * 100f;

                        PlaceObjectOnTerrain(instance, localX, localZ, data.HeightOffset * WorldScale);
                        ConfigureStaticBuilding(instance);
                        placed++;
                    }
                }
            }

            // Source AreaData buildings are the authoritative 3D world objects.
            // Keep the temporary chunk debug geometry out of the playable scene.
            Debug.Log("Metin2: Blue 1 gerçek AreaData objeleri: " + placed + " yerleştirildi. " +
                      "Property çözülemedi: " + unresolvedProperty + ", prefab bulunamadı: " + unresolvedPrefab +
                      ". DEBUG: Terrain_001002 merkezi (640,384).");
        }

        private static Dictionary<uint, SourceObjectProperty> LoadSourceProperties(string clientRoot)
        {
            Dictionary<uint, SourceObjectProperty> result = new Dictionary<uint, SourceObjectProperty>();
            string propertyRoot = Path.Combine(clientRoot, "property");
            if (!Directory.Exists(propertyRoot))
            {
                Debug.LogWarning("Metin2: property klasörü bulunamadı: " + propertyRoot);
                return result;
            }

            string[] files = Directory.GetFiles(propertyRoot, "*.prb", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                if (lines.Length < 3) continue;

                if (!uint.TryParse(lines[1].Trim(), out uint id)) continue;
                string model = ExtractQuotedValue(lines, "buildingfile");
                if (string.IsNullOrEmpty(model)) continue;

                result[id] = new SourceObjectProperty { Id = id, ModelName = model };
            }

            Debug.Log("Metin2: Blue 1 building property sayısı: " + result.Count);
            return result;
        }

        private static Dictionary<string, GameObject> LoadGeneratedObjectPrefabs()
        {
            Dictionary<string, GameObject> result = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            // First use generated prefabs.
            string prefabRoot = "Assets/Metin2/Generated/Prefabs";
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string key = NormalizeModelKey(prefab.name);
                if (!result.ContainsKey(key)) result.Add(key, prefab);
            }

            // Also index raw FBX/model assets imported from the Blue 1 required-model set.
            // These do not need to be converted into prefabs before AreaData placement.
            string modelRoot = "Assets/Metin2/Imported/Blue1Required";
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { modelRoot });
            foreach (string guid in modelGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                string key = NormalizeModelKey(model.name);
                if (!result.ContainsKey(key)) result.Add(key, model);
            }

            Debug.Log("Metin2: Source object model indexi: " + result.Count);
            return result;
        }

        private static GameObject FindPrefabByModelSuffix(Dictionary<string, GameObject> prefabs, string key)
        {
            foreach (KeyValuePair<string, GameObject> pair in prefabs)
            {
                if (pair.Key.EndsWith("_" + key, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return null;
        }

        private static List<SourceMapObject> ReadAreaData(string path)
        {
            List<SourceMapObject> result = new List<SourceMapObject>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].TrimStart().StartsWith("Start Object", StringComparison.OrdinalIgnoreCase)) continue;
                if (i + 3 >= lines.Length) continue;

                string[] p = lines[i + 1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 3) continue;
                if (!float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) continue;
                if (!uint.TryParse(lines[i + 2].Trim(), out uint propertyId)) continue;

                string[] r = lines[i + 3].Trim().Split('#');
                Vector3 rotation = Vector3.zero;
                if (r.Length >= 3)
                {
                    float.TryParse(r[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rotation.x);
                    float.TryParse(r[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rotation.y);
                    float.TryParse(r[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rotation.z);
                }

                float height = 0f;
                if (i + 4 < lines.Length)
                    float.TryParse(lines[i + 4].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out height);

                result.Add(new SourceMapObject
                {
                    Position = new Vector3(x, y, z),
                    Rotation = rotation,
                    HeightOffset = height,
                    PropertyId = propertyId
                });
            }
            return result;
        }

        private static string ExtractQuotedValue(string[] lines, string key)
        {
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
                int first = trimmed.IndexOf('"');
                int last = trimmed.LastIndexOf('"');
                if (first >= 0 && last > first) return trimmed.Substring(first + 1, last - first - 1);
            }
            return string.Empty;
        }

        private static string NormalizeModelKey(string value)
        {
            string name = value.Replace('\\', '/');
            name = Path.GetFileNameWithoutExtension(name);
            int lod = name.IndexOf("_lod_", StringComparison.OrdinalIgnoreCase);
            if (lod >= 0) name = name.Substring(0, lod);
            return name.Replace(" ", "_").Trim().ToLowerInvariant();
        }

        private static void ConfigureStaticBuilding(GameObject instance)
        {
            // Imported GR2/FBX buildings are real 3D scene objects. Keep them static
            // and give every mesh a conservative collider so the player can stand near
            // vendors/buildings instead of walking straight through them.
            instance.isStatic = true;
            MeshFilter[] meshes = instance.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mesh in meshes)
            {
                if (mesh == null || mesh.sharedMesh == null) continue;
                if (mesh.GetComponent<Collider>() != null) continue;
                MeshCollider collider = mesh.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = false;
            }
        }

        private static void PlaceObjectOnTerrain(GameObject instance, float localX, float localZ, float heightOffset)
        {
            Transform mapRoot = instance.transform.parent.parent;

            // Objects are now placed in the same lower-left-origin coordinate
            // system as the Terrain chunks.
            Vector3 world = mapRoot.TransformPoint(new Vector3(localX, 0f, localZ));
            Terrain best = null;
            foreach (Terrain terrain in UnityEngine.Object.FindObjectsOfType<Terrain>())
            {
                Vector3 p = terrain.GetPosition();
                Vector3 size = terrain.terrainData.size;
                if (world.x >= p.x && world.x <= p.x + size.x && world.z >= p.z && world.z <= p.z + size.z)
                {
                    best = terrain;
                    break;
                }
            }
            if (best == null) return;
            float y = best.SampleHeight(world) + best.GetPosition().y + heightOffset;
            Vector3 local = instance.transform.localPosition;
            local.y = y - instance.transform.parent.parent.position.y;
            instance.transform.localPosition = local;
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
            float cityX = 637f;
            float cityZ = 384f;
            player.transform.localPosition = new Vector3(cityX, 28f, cityZ);
            player.transform.rotation = Quaternion.identity;
            PlacePlayerOnTerrain(player, mapRoot, cityX, cityZ);

            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            AlignVisualFeetToPlayer(visual, player);

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

        private static void AlignVisualFeetToPlayer(GameObject visual, GameObject player)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float delta = player.transform.position.y - bounds.min.y + 0.03f;
            visual.transform.position += Vector3.up * delta;
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
            // Start close to the exact center of Terrain001002 so the source
            // object placement can be checked without navigating the whole map.
            cameraObject.transform.position = player.transform.position + new Vector3(0f, 8.0f, -12.0f);
            cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1.1f);

            Metin2FollowCamera follow = cameraObject.AddComponent<Metin2FollowCamera>();
            follow.SetTarget(player.transform);
        }

        private static void BuildMobileHUD(GameObject player)
        {
            GameObject eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasObject = new GameObject("Mobile HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject joystickObject = new GameObject("Movement Joystick");
            joystickObject.transform.SetParent(canvasObject.transform, false);
            RectTransform joystickRect = joystickObject.AddComponent<RectTransform>();
            joystickRect.anchorMin = new Vector2(0f, 0f);
            joystickRect.anchorMax = new Vector2(0f, 0f);
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.sizeDelta = new Vector2(190f, 190f);
            joystickRect.anchoredPosition = new Vector2(145f, 145f);

            Image joystickImage = joystickObject.AddComponent<Image>();
            joystickImage.sprite = GetOrCreateCircleSprite("Joystick_Background", 190, 0.38f);
            joystickImage.raycastTarget = true;

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(joystickObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(82f, 82f);
            handleRect.anchoredPosition = Vector2.zero;
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.sprite = GetOrCreateCircleSprite("Joystick_Handle", 82, 0.75f);
            handleImage.raycastTarget = false;

            Metin2VirtualJoystick joystick = joystickObject.AddComponent<Metin2VirtualJoystick>();
            joystick.SetPlayer(player.GetComponent<Metin2PlayerController>());

            Debug.Log("Metin2: Mobile joystick oluşturuldu ve Warrior'a bağlandı.");
        }

        private static Sprite GetOrCreateCircleSprite(string name, int size, float alpha)
        {
            string path = "Assets/Metin2/Generated/" + name + ".png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                Color32[] pixels = new Color32[size * size];
                float center = (size - 1) * 0.5f;
                float radius = center - 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        byte a = (byte)(Mathf.Clamp01((radius + 1f - distance)) * 255f * alpha);
                        pixels[y * size + x] = new Color32(255, 255, 255, a);
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllBytes(Path.Combine(Application.dataPath, "Metin2/Generated/" + name + ".png"), png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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