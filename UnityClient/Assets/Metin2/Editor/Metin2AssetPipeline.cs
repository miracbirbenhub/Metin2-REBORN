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
                warrior.transform.localScale = Vector3.one;
                // Keep the imported mesh visible even if its source materials are incomplete.
                foreach (Renderer renderer in warrior.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;

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

                ConfigureWarriorVisuals(warrior);
            }

            // Temporary placeholder removed: focus on making the imported Warrior visible first.

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

        private static void ConfigureWarriorVisuals(GameObject warrior)
        {
            // Keep the FBX hierarchy intact, but make the imported character behave
            // like a real third-person player instead of a raw model.
            warrior.transform.rotation = Quaternion.identity;

            Animator animator = warrior.GetComponent<Animator>();
            if (animator == null)
                animator = warrior.AddComponent<Animator>();
            animator.applyRootMotion = false;

            // CharacterController should wrap the visual model, not replace it.
            CharacterController cc = warrior.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.8f;
                cc.radius = 0.32f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.skinWidth = 0.04f;
                cc.stepOffset = 0.3f;
            }

            ApplyWarriorTextures(warrior);
        }

        private static void ApplyWarriorTextures(GameObject warrior)
        {
            string bluePath = FindAsset("warrior_novice_blue.png");
            string facePath = FindAsset("warrior_face.png");
            string hairPath = FindAsset("warrior_novice_hair.png");

            Material blue = CreateOrReplaceMaterial("Warrior_Novice_Blue_Auto", bluePath);
            Material face = CreateOrReplaceMaterial("Warrior_Face_Auto", facePath);
            Material hair = CreateOrReplaceMaterial("Warrior_Hair_Auto", hairPath);

            Renderer[] renderers = warrior.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    if (blue != null) renderer.sharedMaterial = blue;
                    continue;
                }

                for (int i = 0; i < slots.Length; i++)
                {
                    string n = renderer.gameObject.name.ToLowerInvariant();
                    if (n.Contains("hair") && hair != null) slots[i] = hair;
                    else if ((n.Contains("face") || n.Contains("head")) && face != null) slots[i] = face;
                    else if (blue != null) slots[i] = blue;
                }
                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
            }

            AssetDatabase.SaveAssets();
        }

        private static Material CreateOrReplaceMaterial(string materialName, string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath)) return null;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) return null;

            string materialPath = Warrior + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = materialName;
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.mainTexture = texture;
            material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
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
