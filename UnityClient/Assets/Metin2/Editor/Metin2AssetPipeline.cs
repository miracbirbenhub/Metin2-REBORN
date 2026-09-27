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
                NormalizeWarriorVisual(warrior);

                Renderer[] warriorRenderers = warrior.GetComponentsInChildren<Renderer>(true);
                int meshFilters = warrior.GetComponentsInChildren<MeshFilter>(true).Length;
                int skinnedMeshes = warrior.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                Debug.Log($"Metin2: Warrior diagnostic -> Renderers={warriorRenderers.Length}, MeshFilters={meshFilters}, SkinnedMeshes={skinnedMeshes}");

                if (warriorRenderers.Length == 0)
                    Debug.LogError("Metin2: Warrior FBX prefab içinde hiç Renderer yok. EXPORT.fbx modeli mesh/skeleton içermiyor veya converter çıktısı hatalı.");
                else
                {
                    Bounds diagnosticBounds = warriorRenderers[0].bounds;
                    for (int i = 1; i < warriorRenderers.Length; i++) diagnosticBounds.Encapsulate(warriorRenderers[i].bounds);
                    Debug.Log($"Metin2: Warrior bounds -> Center={diagnosticBounds.center}, Size={diagnosticBounds.size}");
                }
            }

            // Temporary placeholder removed: focus on making the imported Warrior visible first.

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";

            GameObject warriorObject = GameObject.Find("Warrior");
            if (warriorObject != null)
            {
                var renderers = warriorObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                    // Frame the imported model regardless of the FBX export scale.
                    float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (size < 0.01f) size = 2f;
                    float distance = Mathf.Clamp(size * 2.8f, 2.5f, 20f);
                    Vector3 focus = bounds.center + Vector3.up * (bounds.size.y * 0.05f);
                    cameraObject.transform.position = focus + new Vector3(0f, size * 0.35f, -distance);
                    cameraObject.transform.LookAt(focus);

                    camera.nearClipPlane = Mathf.Max(0.01f, size * 0.01f);
                    camera.farClipPlane = Mathf.Max(100f, size * 20f);
                }

                var follow = cameraObject.AddComponent<Metin2FollowCamera>();
                follow.SetTarget(warriorObject.transform);
            }
            else
            {
                cameraObject.transform.position = new Vector3(0f, 3f, -6f);
                cameraObject.transform.LookAt(Vector3.up);
            }

            GameObject lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, Scenes + "/WarriorVerticalSlice.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Metin2: Vertical Slice sahnesi oluşturuldu.");
        }

        public static void ConfigureForPlayableInstance(GameObject warrior)
        {
            ConfigureWarriorVisuals(warrior);
            NormalizeWarriorVisual(warrior);
        }

        private static void ConfigureWarriorVisuals(GameObject warrior)
        {
            // Keep the rotation selected by the playable scene builder.
            // The imported GR2/FBX uses a non-standard axis, so forcing identity here
            // would turn the Warrior back onto the ground.
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

        private static void NormalizeWarriorVisual(GameObject warrior)
        {
            Renderer[] renderers = warrior.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            if (height > 0.001f && (height < 0.5f || height > 4f))
            {
                float factor = 2f / height;
                warrior.transform.localScale *= factor;
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }

            float bottomOffset = bounds.min.y - warrior.transform.position.y;
            warrior.transform.position -= Vector3.up * bottomOffset;
        }

        private static void ApplyWarriorTextures(GameObject warrior)
        {
            string redPath = FindAsset("warrior_novice_red.png");
            string facePath = FindAsset("warrior_face.png");
            string hairPath = FindAsset("warrior_novice_hair.png");

            Material red = CreateOrReplaceMaterial("Warrior_Novice_Red_Auto", redPath);
            Material face = CreateOrReplaceMaterial("Warrior_Face_Auto", facePath);
            Material hair = CreateOrReplaceMaterial("Warrior_Hair_Auto", hairPath);

            Renderer[] renderers = warrior.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    if (red != null) renderer.sharedMaterial = red;
                    continue;
                }

                for (int i = 0; i < slots.Length; i++)
                {
                    string n = renderer.gameObject.name.ToLowerInvariant();
                    if (n.Contains("hair") && hair != null) slots[i] = hair;
                    else if ((n.Contains("face") || n.Contains("head")) && face != null) slots[i] = face;
                    else if (red != null) slots[i] = red;
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
