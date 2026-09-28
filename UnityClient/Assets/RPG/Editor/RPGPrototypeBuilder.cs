using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Metin2Reborn.RPG;

public static class RPGPrototypeBuilder
{
    [MenuItem("RPG/Build First Playable Prototype")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var state = new GameObject("GameState");
        state.AddComponent<RPGGameState>();

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "CombatGround";
        ground.transform.localScale = Vector3.one * 2.5f;

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Warrior";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, 0);
        var pc = player.AddComponent<RPGCombatant>();
        player.AddComponent<RPGAutoBattle>();

        for (int i = 0; i < 3; i++)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy_" + (i + 1);
            enemy.transform.position = new Vector3((i - 1) * 2.5f, 1, 5);
            enemy.transform.localScale = Vector3.one * 0.8f;
            var ec = enemy.AddComponent<RPGCombatant>();
            ec.SetTarget(player.GetComponent<RPGCombatant>());
        }

        var light = new GameObject("Sun");
        var sun = light.AddComponent<Light>();
        sun.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        var camera = cam.AddComponent<Camera>();
        cam.transform.position = new Vector3(0, 8, -10);
        cam.transform.rotation = Quaternion.Euler(35, 0, 0);

        var hud = new GameObject("RPGPrototype");
        hud.AddComponent<RPGDemoBootstrap>();

        EditorSceneManager.SaveScene(scene, "Assets/RPG/Scenes/FirstPlayable.unity");
        Selection.activeGameObject = player;
        Debug.Log("RPG: First playable prototype created.");
    }
}