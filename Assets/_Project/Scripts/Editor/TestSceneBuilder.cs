using QueteI1.StatusEffects.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace QueteI1.StatusEffects.EditorTools
{
    /// <summary>Construit la scène de test minimaliste : deux entités, la simulation et le panneau de debug.</summary>
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/TestScene.unity";
        const string MaterialsFolder = "Assets/_Project/Materials";

        static readonly Color PlayerColor = new Color(0.25f, 0.55f, 1f);
        static readonly Color EnemyColor = new Color(0.95f, 0.35f, 0.3f);

        [MenuItem("Quête I1/Construire la scène de test", priority = 3)]
        public static void Build()
        {
            var library = EffectAssetGenerator.EnsureAssets();
            EditorFolders.Ensure("Assets/_Project/Scenes");
            EditorFolders.Ensure(MaterialsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
            RenderSettings.skybox = null;

            var light = new GameObject("Lumière").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Caméra de fond : remplit tout l'écran derrière le panneau de debug.
            var background = new GameObject("Caméra fond").AddComponent<Camera>();
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = new Color(0.06f, 0.065f, 0.08f);
            background.cullingMask = 0;
            background.depth = -10f;

            // Caméra de la scène : le panneau limite son rendu au cadre « vue de la scène ».
            var sceneCamera = new GameObject("Caméra scène").AddComponent<Camera>();
            sceneCamera.tag = "MainCamera";
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 2.3f;
            sceneCamera.transform.SetPositionAndRotation(new Vector3(0f, 3.9f, -10f), Quaternion.Euler(12f, 0f, 0f));
            sceneCamera.gameObject.AddComponent<AudioListener>();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Sol";
            ground.transform.position = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(26f, 0.2f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = Material("Sol", new Color(0.32f, 0.34f, 0.38f));

            // Chacun sa moitié de terrain : les étiquettes ne se chevauchent pas.
            var player = CreateEntity("Joueur", PrimitiveType.Capsule, new Vector3(-4f, 0f, 0f), 1f, PlayerColor,
                EntityControl.Keyboard, new BaseStats(100f, 4f, 12f, 10f), -9f, -0.5f);
            var enemy = CreateEntity("Ennemi", PrimitiveType.Cube, new Vector3(3f, 0f, 0f), 0.6f, EnemyColor,
                EntityControl.Patrol, new BaseStats(150f, 2.5f, 8f, 25f), 1f, 9f);

            var systemObject = new GameObject("Système d'effets");
            var runner = systemObject.AddComponent<SimulationRunner>();
            runner.EditorSetup(new[] { player, enemy });

            var demo = systemObject.AddComponent<DemoScenario>();
            // Ordre choisi pour que les 7 effets soient actifs en même temps sur le joueur vers t = 7 s,
            // Hâte (additif) et Ralentissement (multiplicatif) compris.
            demo.EditorSetup(runner, new[]
            {
                new DemoScenario.Step(EffectAssetGenerator.Load("07_Rage"), 0.3f),
                new DemoScenario.Step(EffectAssetGenerator.Load("07_Rage"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("03_Poison"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("03_Poison"), 0.5f),
                new DemoScenario.Step(EffectAssetGenerator.Load("03_Poison"), 0.5f),
                new DemoScenario.Step(EffectAssetGenerator.Load("04_Regeneration"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("08_Armure"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("05_Ralentissement"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("05_Ralentissement"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("06_Hate"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("09_Etourdissement"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("09_Etourdissement"), 0.6f),
                new DemoScenario.Step(EffectAssetGenerator.Load("02_Frappe"), 0.6f, 1),
                new DemoScenario.Step(EffectAssetGenerator.Load("09_Etourdissement"), 0.6f, 1)
            });

            var panel = systemObject.AddComponent<StatusDebugPanel>();
            panel.EditorSetup(runner, library, demo, sceneCamera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Scène de test construite : " + ScenePath);
        }

        static EntityBehaviour CreateEntity(string name, PrimitiveType shape, Vector3 position, float bodyHeight,
            Color color, EntityControl control, BaseStats stats, float minX, float maxX)
        {
            // Racine au sol : la rotation « mort » fait basculer le corps depuis ses pieds.
            var root = new GameObject(name);
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(shape);
            body.name = "Corps";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, bodyHeight, 0f);
            if (shape == PrimitiveType.Cube)
                body.transform.localScale = Vector3.one * 1.2f;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = Material(name, color);

            var entity = root.AddComponent<EntityBehaviour>();
            entity.EditorSetup(name, stats, color, control, renderer, minX, maxX);
            return entity;
        }

        static Material Material(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
