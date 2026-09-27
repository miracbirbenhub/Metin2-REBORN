#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Metin2Reborn.Editor
{
    public static class Metin2AssetPipeline
    {
        private const string Root = "Assets/Metin2";
        private const string Warrior = Root + "/Characters/Warrior";
        private const string Scenes = Root + "/Scenes";

        [MenuItem("Metin2/Rebuild Warrior Import")]
        public static void RebuildWarriorImport()
        {
            EnsureFolders();

            string fbx = FindAsset("EXPORT.fbx");
            if (string.IsNullOrEmpty(fbx))
                fbx = FindAsset("warrior_novice.fbx");

            if (string.IsNullOrEmpty(fbx))
            {
                Debug.LogError("Metin2: Warrior FBX bulunamadı. EXPORT.fbx veya warrior_novice.fbx ekleyin.");
                return;
            }

            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null)
            {
                Debug.LogError("Metin2: FBX yüklenemedi: " + fbx);
                return;
            }

            string prefabPath = Warrior + "/Warrior_Novice.prefab";
            PrefabUtility.SaveAsPrefabAsset(model, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Metin2: Warrior prefab hazır: " + prefabPath);
        }

        [MenuItem("Metin2/Create Vertical Slice Scene")]
        public static void CreateVerticalSliceScene()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Map1_TestGround";
            ground.transform.localScale = Vector3.one * 10f;

            string prefabPath = Warrior + "/Warrior_Novice.prefab";
            GameObject warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (warriorPrefab == null)
            {
                RebuildWarriorImport();
                AssetDatabase.Refresh();
                warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            if (warriorPrefab != null)
            {
                GameObject warrior = (GameObject)PrefabUtility.InstantiatePrefab(warriorPrefab);
                warrior.name = "Warrior";
                warrior.transform.position = Vector3.zero;

                if (warrior.GetComponent<CharacterController>() == null)
                {
                    var cc = warrior.AddComponent<CharacterController>();
                    cc.height = 1.8f;
                    cc.radius = 0.35f;
                    cc.center = Vector3.up * 0.9f;
                }
                if (warrior.GetComponent<Metin2PlayerController>() == null)
                    warrior.AddComponent<Metin2PlayerController>();
                if (warrior.GetComponent<Metin2AutoAttacker>() == null)
                    warrior.AddComponent<Metin2AutoAttacker>();

                ApplyWarriorTexture(warrior);
            }

            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = "MetinStone_Test";
            stone.transform.position = new Vector3(0f, 1f, 4f);
            stone.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            stone.AddComponent<Metin2Targetable>();

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            var follow = cameraObject.AddComponent<Metin2FollowCamera>();
            if (GameObject.Find("Warrior") != null) follow.SetTarget(GameObject.Find("Warrior").transform);
            cameraObject.transform.position = new Vector3(0f, 4f, -6f);

            GameObject lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, Scenes + "/WarriorVerticalSlice.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Metin2: Vertical Slice sahnesi oluşturuldu.");
        }

        private static void ApplyWarriorTexture(GameObject warrior)
        {
            string texturePath = FindAsset("warrior_novice_blue.png");
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogWarning("Metin2: warrior_novice_blue.png bulunamadı; mevcut FBX materyali korunuyor.");
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) return;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Warrior_Novice_Blue_Auto";
            material.mainTexture = texture;
            string materialPath = Warrior + "/Warrior_Novice_Blue_Auto.mat";
            AssetDatabase.CreateAsset(material, materialPath);

            Renderer[] renderers = warrior.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                    renderer.sharedMaterial = material;
                else
                {
                    for (int i = 0; i < slots.Length; i++) slots[i] = material;
                    renderer.sharedMaterials = slots;
                }
            }

            EditorUtility.SetDirty(warrior);
            AssetDatabase.SaveAssets();
        }

        private static string FindAsset(string fileName)
        {
            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileName(path), fileName, System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return string.Empty;
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Metin2");
            CreateFolderIfMissing(Root, "Characters");
            CreateFolderIfMissing(Root + "/Characters", "Warrior");
            CreateFolderIfMissing(Root, "Scenes");
        }

        private static void CreateFolderIfMissing(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
