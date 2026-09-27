#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Metin2Reborn.Editor
{
    public static class Metin2PlayableSliceBuilder
    {
        private const string Root = "Assets/Metin2";
        private const string Scenes = Root + "/Scenes";
        private const string Generated = Root + "/Generated/Prefabs/Monsters";
        private const string Runtime = Root + "/Scripts/Runtime";

        [MenuItem("Metin2/Build PLAYABLE Vertical Slice")]
        public static void Build()
        {
            EnsureFolder(Scenes);
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildGround();
            GameObject player = BuildPlayer();
            GameObject stone = BuildMetinStone();
            GameObject drop = BuildDropPrefab();
            ConfigureDrop(stone, drop);
            BuildMonster();
            BuildCamera(player);
            BuildMobileUI(player);
            GameObject bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<Metin2SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, Scenes + "/PlayableVerticalSlice.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Metin2: PLAYABLE Vertical Slice hazır.");
            Debug.Log("Metin2: Warrior -> hareket -> Metin Stone -> otomatik saldırı -> drop -> pickup.");
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

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Map1_Baslangic_Alani";
            ground.transform.localScale = Vector3.one * 12f;
            ground.transform.position = Vector3.zero;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "Map1_Ground_Auto";
            mat.color = new Color(0.25f, 0.38f, 0.22f);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject BuildPlayer()
        {
            string path = Root + "/Characters/Warrior/Warrior_Novice.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Metin2AssetPipeline.RebuildWarriorImport();
                AssetDatabase.Refresh();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            if (prefab == null)
                throw new System.Exception("Warrior_Novice.prefab bulunamadı.");

            // IMPORTANT:
            // The imported Warrior needs an X/Z visual tilt to stand upright.
            // Never put that tilt on the CharacterController root, otherwise the
            // capsule itself becomes horizontal and gravity makes the player fall
            // through the ground.
            GameObject visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual == null)
                throw new System.Exception("Warrior prefab instance oluşturulamadı.");

            visual.name = "Warrior_Visual";
            visual.transform.rotation = FindBestStandingRotation(visual);

            Metin2AssetPipeline.ConfigureForPlayableInstance(visual);

            GameObject player = new GameObject("Warrior_Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.02f, 0f);
            player.transform.rotation = Quaternion.identity;

            visual.transform.SetParent(player.transform, true);

            if (player.GetComponent<CharacterController>() == null)
            {
                CharacterController cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.32f;
                cc.center = Vector3.up * 0.9f;
                cc.stepOffset = 0.3f;
                cc.skinWidth = 0.04f;
            }

            Metin2PlayerController movement = player.AddComponent<Metin2PlayerController>();
            player.AddComponent<Metin2AutoAttacker>();
            player.AddComponent<Metin2Inventory>();

            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;

            return player;
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

            Debug.Log($"Metin2: Warrior auto-orientation -> {best.eulerAngles}, score={bestScore:F2}, sourceBounds={size}");
            return best;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static GameObject BuildMetinStone()
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = "Metin_Stone_Test";
            stone.transform.position = new Vector3(2.8f, 1.15f, 1.2f);
            stone.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "Metin_Stone_Auto";
            mat.color = new Color(0.12f, 0.12f, 0.15f);
            stone.GetComponent<Renderer>().sharedMaterial = mat;

            Metin2Targetable target = stone.AddComponent<Metin2Targetable>();
            target.Configure(180, null, 1);
            AddWorldHealthBar(stone, target);
            return stone;
        }

        private static GameObject BuildDropPrefab()
        {
            string folder = Root + "/Generated/Prefabs/Items";
            EnsureFolder(folder);

            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = "Yang_Drop";
            drop.transform.localScale = Vector3.one * 0.28f;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "Yang_Drop_Auto";
            mat.color = new Color(1f, 0.72f, 0.08f);
            drop.GetComponent<Renderer>().sharedMaterial = mat;

            Metin2Pickup pickup = drop.AddComponent<Metin2Pickup>();

            string path = folder + "/Yang_Drop.prefab";
            PrefabUtility.SaveAsPrefabAsset(drop, path);
            Object.DestroyImmediate(drop);

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void ConfigureDrop(GameObject stone, GameObject drop)
        {
            Metin2Targetable target = stone.GetComponent<Metin2Targetable>();
            target.Configure(180, drop, 2);
        }

        private static void BuildMonster()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { Generated });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                GameObject mob = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (mob == null) continue;

                mob.name = "Wild_Monster_Test";
                mob.transform.position = new Vector3(-4f, 0f, 3.5f);

                Renderer[] renderers = mob.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                    float h = Mathf.Max(0.1f, b.size.y);
                    if (h < 0.5f || h > 2.5f)
                        mob.transform.localScale *= 1.4f / h;
                }

                if (mob.GetComponent<Metin2Targetable>() == null)
                    mob.AddComponent<Metin2Targetable>();

                Metin2Targetable target = mob.GetComponent<Metin2Targetable>();
                target.Configure(120, null, 0);
                AddWorldHealthBar(mob, target);

                if (mob.GetComponent<Collider>() == null)
                {
                    CapsuleCollider cc = mob.AddComponent<CapsuleCollider>();
                    cc.height = 1.4f;
                    cc.radius = 0.4f;
                    cc.center = Vector3.up * 0.7f;
                }

                break;
            }
        }

        private static void AddWorldHealthBar(GameObject owner, Metin2Targetable target)
        {
            GameObject canvasObject = new GameObject("HealthBar");
            canvasObject.transform.SetParent(owner.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1.6f, 0.22f);
            canvasRect.localScale = Vector3.one * 0.01f;
            canvasRect.localPosition = new Vector3(0f, 1.65f, 0f);

            GameObject bgObject = new GameObject("Background", typeof(RectTransform));
            bgObject.transform.SetParent(canvasObject.transform, false);
            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bg = bgObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(canvasObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(0.02f, 0.02f);
            fillRect.offsetMax = new Vector2(-0.02f, -0.02f);
            Image fill = fillObject.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            Metin2HealthBar healthBar = canvasObject.AddComponent<Metin2HealthBar>();
            healthBar.Initialize(target);
        }

        private static void BuildCamera(GameObject player)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 500f;

            cameraObject.transform.position = player.transform.position + new Vector3(0f, 5.2f, -7.5f);
            cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1f);

            Metin2FollowCamera follow = cameraObject.AddComponent<Metin2FollowCamera>();
            follow.SetTarget(player.transform);
        }

        private static void BuildMobileUI(GameObject player)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasObject = new GameObject("MobileHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject baseObject = CreateUIObject("Joystick_Base", canvasObject.transform);
            RectTransform baseRect = baseObject.GetComponent<RectTransform>();
            baseRect.anchorMin = new Vector2(0f, 0f);
            baseRect.anchorMax = new Vector2(0f, 0f);
            baseRect.pivot = new Vector2(0f, 0f);
            baseRect.anchoredPosition = new Vector2(80f, 80f);
            baseRect.sizeDelta = new Vector2(180f, 180f);

            Image baseImage = baseObject.AddComponent<Image>();
            baseImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            baseImage.type = Image.Type.Sliced;
            baseImage.color = new Color(1f, 1f, 1f, 0.28f);

            GameObject handleObject = CreateUIObject("Joystick_Handle", baseObject.transform);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(82f, 82f);
            handleRect.anchoredPosition = Vector2.zero;

            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handleImage.color = new Color(1f, 1f, 1f, 0.55f);

            Metin2VirtualJoystick joystick = baseObject.AddComponent<Metin2VirtualJoystick>();
            joystick.SetPlayer(player.GetComponent<Metin2PlayerController>());

            CreateLabel(canvasObject.transform, "METIN2 REBORN", new Vector2(0f, -55f), new Vector2(420f, 70f), 30, TextAnchor.UpperCenter);
            CreateLabel(canvasObject.transform, "Warrior  •  Auto Combat  •  Map 1", new Vector2(0f, -105f), new Vector2(600f, 55f), 20, TextAnchor.UpperCenter);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void CreateLabel(Transform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = CreateUIObject(text, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.raycastTarget = false;
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
