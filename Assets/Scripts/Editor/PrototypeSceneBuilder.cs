using System.IO;
using Nymara.Core;
using Nymara.Enemies;
using Nymara.Player;
using Nymara.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Nymara.EditorTools
{
    /// <summary>
    /// Monta a cena graybox Prototype_01_MovementCombat, os prefabs de teste e os
    /// assets mínimos (sprite quadrado, material sem atrito, layers).
    /// Menu: NYMARA ▸ Prototype ▸ Build Prototype_01_MovementCombat.
    /// Não altera cenas existentes; só sobrescreve os próprios arquivos gerados após confirmação.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity";
        private const string ArtFolder = "Assets/Art/Prototype";
        private const string SpritePath = ArtFolder + "/PrototypeSquare.png";
        private const string MaterialFolder = "Assets/Materials/Prototype";
        private const string NoFrictionPath = MaterialFolder + "/NoFriction2D.physicsMaterial2D";
        private const string PrefabFolder = "Assets/Prefabs/Prototype";
        private const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        private const string EnemyPrefabPath = PrefabFolder + "/EcoAprisionado.prefab";
        private const string InputAssetPath = "Assets/Settings/Input/NymaraPrototype.inputactions";

        private const string GroundLayerName = "Ground";
        private const string PlayerLayerName = "Player";
        private const string EnemyLayerName = "Enemy";

        private static readonly Vector3 PlayerSpawn = new Vector3(-20f, 0.85f, 0f);

        private static readonly Color GroundColor = new Color(0.32f, 0.34f, 0.38f);
        private static readonly Color PlatformColor = new Color(0.42f, 0.45f, 0.5f);
        private static readonly Color ObstacleColor = new Color(0.5f, 0.42f, 0.36f);
        private static readonly Color WallColor = new Color(0.25f, 0.26f, 0.3f);

        [MenuItem("NYMARA/Prototype/Build Prototype_01_MovementCombat")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("NYMARA", "Saia do Play Mode antes de gerar a cena.", "OK");
                return;
            }

            bool generatedExists = File.Exists(ScenePath) || File.Exists(PlayerPrefabPath) || File.Exists(EnemyPrefabPath);
            if (generatedExists && !EditorUtility.DisplayDialog("NYMARA",
                    "A cena e/ou os prefabs do Prototype_01 já existem.\nRecriar e sobrescrever SOMENTE esses arquivos gerados?",
                    "Recriar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null)
            {
                Debug.LogError($"[PrototypeSceneBuilder] Input asset não encontrado em {InputAssetPath}.");
                return;
            }

            int groundLayer = EnsureLayer(GroundLayerName);
            int playerLayer = EnsureLayer(PlayerLayerName);
            int enemyLayer = EnsureLayer(EnemyLayerName);
            if (groundLayer < 0 || playerLayer < 0 || enemyLayer < 0) return;

            // Protagonista e Ecos são corpos sólidos entre si: avanços de golpe e dash NÃO atravessam inimigos
            // (atravessar é uma possível bênção futura, ex.: "Passo Cortante"). Ecos não colidem entre si.
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);

            EnsureFolder(ArtFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder("Assets/Scenes/Prototypes");

            Sprite square = EnsureSquareSprite();
            PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();

            GameObject playerPrefab = BuildPlayerPrefab(square, noFriction, inputAsset, groundLayer, playerLayer, enemyLayer);
            GameObject enemyPrefab = BuildEnemyPrefab(square, noFriction, enemyLayer, playerLayer);

            BuildScene(square, playerPrefab, enemyPrefab, inputAsset, groundLayer);

            AssetDatabase.SaveAssets();
            Debug.Log($"[PrototypeSceneBuilder] Cena gerada em {ScenePath}.");
        }

        // ================================================================== assets

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty p = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue))
                {
                    p.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }
            Debug.LogError($"[PrototypeSceneBuilder] Sem espaço livre para a layer '{layerName}'.");
            return -1;
        }

        private static Sprite EnsureSquareSprite()
        {
            if (!File.Exists(SpritePath))
            {
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static PhysicsMaterial2D EnsureNoFrictionMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (mat != null) return mat;
            mat = new PhysicsMaterial2D("NoFriction2D") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, NoFrictionPath);
            return mat;
        }

        // ================================================================== prefabs

        private static GameObject BuildPlayerPrefab(Sprite square, PhysicsMaterial2D noFriction, InputActionAsset inputAsset,
            int groundLayer, int playerLayer, int enemyLayer)
        {
            var root = new GameObject("Player") { layer = playerLayer, tag = "Player" };

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.sharedMaterial = noFriction;

            var body = root.AddComponent<CapsuleCollider2D>();
            body.direction = CapsuleDirection2D.Vertical;
            body.size = new Vector2(0.8f, 1.6f);
            body.sharedMaterial = noFriction;

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            var bodySprite = CreateSprite(visualRoot.transform, "Body", square, Vector2.zero, new Vector2(0.8f, 1.6f), new Color(0.55f, 0.85f, 0.95f), 10, playerLayer);
            CreateSprite(visualRoot.transform, "FacingMarker", square, new Vector2(0.24f, 0.42f), new Vector2(0.22f, 0.22f), new Color(0.1f, 0.15f, 0.25f), 11, playerLayer);

            var stability = root.AddComponent<StabilityComponent>();
            SetProps(stability, so =>
            {
                so.FindProperty("maxStability").floatValue = 5f;
                so.FindProperty("invulnerabilityAfterHit").floatValue = 0.8f;
                so.FindProperty("displayName").stringValue = "Portador do Pacto";
            });

            AttackHitbox groundHitbox = CreateHitbox(root.transform, "GroundHitbox", square, playerLayer, 1 << enemyLayer, stability);
            AttackHitbox aerialHitbox = CreateHitbox(root.transform, "AerialHitbox", square, playerLayer, 1 << enemyLayer, stability);

            var input = root.AddComponent<PlayerInputReader>();
            SetProps(input, so => so.FindProperty("actions").objectReferenceValue = inputAsset);

            var controller = root.AddComponent<PlayerController2D>();
            SetProps(controller, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("bodyCollider").objectReferenceValue = body;
                so.FindProperty("stability").objectReferenceValue = stability;
                so.FindProperty("groundMask").intValue = 1 << groundLayer;
                so.FindProperty("dashBlockMask").intValue = (1 << groundLayer) | (1 << enemyLayer);
            });

            var combat = root.AddComponent<PlayerCombat>();
            SetProps(combat, so =>
            {
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("controller").objectReferenceValue = controller;
                so.FindProperty("stability").objectReferenceValue = stability;
                so.FindProperty("groundHitbox").objectReferenceValue = groundHitbox;
                so.FindProperty("aerialHitbox").objectReferenceValue = aerialHitbox;
            });

            var respawn = root.AddComponent<PlayerRespawn>();
            SetProps(respawn, so =>
            {
                so.FindProperty("controller").objectReferenceValue = controller;
                so.FindProperty("combat").objectReferenceValue = combat;
                so.FindProperty("input").objectReferenceValue = input;
                so.FindProperty("stability").objectReferenceValue = stability;
                so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            });

            var feedback = root.AddComponent<PlayerVisualFeedback>();
            SetProps(feedback, so =>
            {
                so.FindProperty("body").objectReferenceValue = bodySprite;
                so.FindProperty("controller").objectReferenceValue = controller;
                so.FindProperty("combat").objectReferenceValue = combat;
                so.FindProperty("stability").objectReferenceValue = stability;
                so.FindProperty("respawn").objectReferenceValue = respawn;
            });

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildEnemyPrefab(Sprite square, PhysicsMaterial2D noFriction, int enemyLayer, int playerLayer)
        {
            var root = new GameObject("EcoAprisionado") { layer = enemyLayer };

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.mass = 3f; // resiste a ser empurrado pelo protagonista
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = noFriction;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.9f, 1.2f);
            box.sharedMaterial = noFriction;

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            var bodySprite = CreateSprite(visualRoot.transform, "Body", square, Vector2.zero, new Vector2(0.9f, 1.2f), new Color(0.42f, 0.25f, 0.55f), 10, enemyLayer);
            var eye = CreateSprite(visualRoot.transform, "IntervaloEye", square, new Vector2(0.24f, 0.25f), new Vector2(0.2f, 0.16f), new Color(0.95f, 0.3f, 0.6f), 11, enemyLayer);

            var stability = root.AddComponent<StabilityComponent>();
            SetProps(stability, so =>
            {
                so.FindProperty("maxStability").floatValue = 6f;
                so.FindProperty("invulnerabilityAfterHit").floatValue = 0f;
                so.FindProperty("displayName").stringValue = "Eco Aprisionado";
            });

            AttackHitbox hitbox = CreateHitbox(root.transform, "AttackHitbox", square, enemyLayer, 1 << playerLayer, stability);

            var liberation = root.AddComponent<EnemyLiberation>();
            SetProps(liberation, so =>
            {
                so.FindProperty("body").objectReferenceValue = bodySprite;
                so.FindProperty("moteSprite").objectReferenceValue = square;
                SerializedProperty cols = so.FindProperty("collidersToDisable");
                cols.arraySize = 1;
                cols.GetArrayElementAtIndex(0).objectReferenceValue = box;
                SerializedProperty hide = so.FindProperty("hideOnLiberation");
                hide.arraySize = 1;
                hide.GetArrayElementAtIndex(0).objectReferenceValue = eye.gameObject;
            });

            var controller = root.AddComponent<TestEnemyController>();
            SetProps(controller, so =>
            {
                so.FindProperty("stability").objectReferenceValue = stability;
                so.FindProperty("attackHitbox").objectReferenceValue = hitbox;
                so.FindProperty("liberation").objectReferenceValue = liberation;
                so.FindProperty("body").objectReferenceValue = bodySprite;
            });

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static AttackHitbox CreateHitbox(Transform parent, string name, Sprite square, int layer, int targetMask, StabilityComponent owner)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            var hitbox = go.AddComponent<AttackHitbox>();
            var debug = CreateSprite(go.transform, "DebugVisual", square, Vector2.zero, Vector2.one, new Color(1f, 0.85f, 0.3f, 0.45f), 30, layer);
            debug.enabled = false;
            SetProps(hitbox, so =>
            {
                so.FindProperty("targetMask").intValue = targetMask;
                so.FindProperty("ownerStability").objectReferenceValue = owner;
                so.FindProperty("debugVisual").objectReferenceValue = debug;
            });
            return hitbox;
        }

        // ================================================================== cena

        private static void BuildScene(Sprite square, GameObject playerPrefab, GameObject enemyPrefab, InputActionAsset inputAsset, int groundLayer)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Trocar de cena descarrega assets sem uso: recarrega as referências pelo caminho.
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

            // Câmera + luz global 2D (URP)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(PlayerSpawn.x, PlayerSpawn.y + 1.2f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<CinemachineBrain>();

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            // Grupos de organização
            Transform environment = new GameObject("Environment").transform;
            Transform characters = new GameObject("Characters").transform;
            Transform combat = new GameObject("Combat").transform;
            Transform checkpoints = new GameObject("Checkpoints").transform;
            Transform ui = new GameObject("UI").transform;

            BuildEnvironment(environment, square, groundLayer);

            // Ponto de teste / Marco de Passagem provisório
            var spawn = new GameObject("PlayerSpawn_MarcoProvisorio");
            spawn.transform.SetParent(checkpoints, false);
            spawn.transform.position = PlayerSpawn;
            CreateSprite(spawn.transform, "Marker", square, new Vector2(-1.2f, -0.25f), new Vector2(0.3f, 1.2f), new Color(0.95f, 0.9f, 0.7f, 0.6f), -5, 0);

            // Protagonista
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetParent(characters, false);
            player.transform.position = PlayerSpawn;

            // UI de debug
            var fadeGo = new GameObject("ScreenFade");
            fadeGo.transform.SetParent(ui, false);
            var fade = fadeGo.AddComponent<ScreenFade>();

            var resetGo = new GameObject("PrototypeSceneReset");
            resetGo.transform.SetParent(ui, false);
            var reset = resetGo.AddComponent<PrototypeSceneReset>();
            SetProps(reset, so => so.FindProperty("actions").objectReferenceValue = inputAsset);

            SetProps(player.GetComponent<PlayerRespawn>(), so =>
            {
                so.FindProperty("respawnPoint").objectReferenceValue = spawn.transform;
                so.FindProperty("screenFade").objectReferenceValue = fade;
            });

            // Encontros (Zona B e Zona C) com o mesmo prefab
            TestEnemyController enemyB = PlaceEnemy(enemyPrefab, scene, combat, "EcoAprisionado_ZonaB", new Vector3(22f, 0.65f, 0f), -1, player.transform);
            TestEnemyController enemyC = PlaceEnemy(enemyPrefab, scene, combat, "EcoAprisionado_ZonaC", new Vector3(44f, 0.65f, 0f), -1, player.transform);

            var hudGo = new GameObject("PrototypeHUD");
            hudGo.transform.SetParent(ui, false);
            var hud = hudGo.AddComponent<PrototypeHUD>();
            SetProps(hud, so =>
            {
                so.FindProperty("playerStability").objectReferenceValue = player.GetComponent<StabilityComponent>();
                so.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController2D>();
                so.FindProperty("playerCombat").objectReferenceValue = player.GetComponent<PlayerCombat>();
                so.FindProperty("playerRespawn").objectReferenceValue = player.GetComponent<PlayerRespawn>();
                so.FindProperty("playerInput").objectReferenceValue = player.GetComponent<PlayerInputReader>();
                SerializedProperty list = so.FindProperty("enemies");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(0).objectReferenceValue = enemyB;
                list.GetArrayElementAtIndex(1).objectReferenceValue = enemyC;
            });

            // Câmera de acompanhamento (Cinemachine 3)
            var cmGo = new GameObject("CM_PlayerFollow");
            cmGo.transform.position = camGo.transform.position;
            var vcam = cmGo.AddComponent<CinemachineCamera>();
            vcam.Follow = player.transform;
            vcam.Lens.OrthographicSize = 6f;
            var composer = cmGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.TargetOffset = new Vector3(0f, 1.2f, 0f);
            composer.Damping = new Vector3(0.35f, 0.5f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static TestEnemyController PlaceEnemy(GameObject prefab, Scene scene, Transform parent, string name, Vector3 position, int facing, Transform target)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Vector3 s = go.transform.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            go.transform.localScale = s;
            var controller = go.GetComponent<TestEnemyController>();
            // SetTarget grava o alvo e a estabilidade dele (ambos serializados na instância da cena).
            controller.SetTarget(target);
            EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            return controller;
        }

        private static void BuildEnvironment(Transform environment, Sprite square, int groundLayer)
        {
            // ---------------- Zona A — Movimento (x -25 .. 10)
            Transform zoneA = Group(environment, "Zone_A_Movement");
            Backdrop(zoneA, square, new Vector2(-9.5f, 4f), new Vector2(31f, 14f), new Color(0.12f, 0.14f, 0.2f));
            Block(zoneA, square, groundLayer, "Floor_A", new Vector2(-9f, -1f), new Vector2(30f, 2f), GroundColor);
            Block(zoneA, square, groundLayer, "Wall_Left", new Vector2(-24.5f, 4f), new Vector2(1f, 12f), WallColor);
            // Degrau baixo (sólido até o chão): o protagonista tem 1,6 de altura e não deve bater a cabeça em plataformas baixas.
            Block(zoneA, square, groundLayer, "Step_Low", new Vector2(-14f, 0.6f), new Vector2(3f, 1.2f), PlatformColor);
            Block(zoneA, square, groundLayer, "Platform_High", new Vector2(-9.5f, 3.0f), new Vector2(3f, 0.4f), PlatformColor);
            Block(zoneA, square, groundLayer, "Platform_Top", new Vector2(-5f, 4.6f), new Vector2(2.5f, 0.4f), PlatformColor);
            Block(zoneA, square, groundLayer, "Obstacle_Low", new Vector2(-0.5f, 0.75f), new Vector2(1f, 1.5f), ObstacleColor);
            Block(zoneA, square, groundLayer, "Obstacle_Tall", new Vector2(3f, 1.0f), new Vector2(1f, 2.0f), ObstacleColor);
            // Vão (x 6..10) com fundo raso: dá para sair pulando.
            Block(zoneA, square, groundLayer, "Gap_Bottom", new Vector2(8f, -3f), new Vector2(4f, 2f), WallColor);

            // ---------------- Zona B — Combate (x 10 .. 34)
            Transform zoneB = Group(environment, "Zone_B_Combat");
            Backdrop(zoneB, square, new Vector2(22f, 4f), new Vector2(24f, 14f), new Color(0.17f, 0.12f, 0.19f));
            Block(zoneB, square, groundLayer, "Floor_B_Arena", new Vector2(22f, -1f), new Vector2(24f, 2f), GroundColor);

            // ---------------- Zona C — Integração (x 34 .. 63)
            Transform zoneC = Group(environment, "Zone_C_Integration");
            Backdrop(zoneC, square, new Vector2(48.5f, 4f), new Vector2(29f, 14f), new Color(0.11f, 0.17f, 0.16f));
            Block(zoneC, square, groundLayer, "Floor_C", new Vector2(48f, -1f), new Vector2(28f, 2f), GroundColor);
            Block(zoneC, square, groundLayer, "Step_C1", new Vector2(39f, 0.6f), new Vector2(3f, 1.2f), PlatformColor);
            Block(zoneC, square, groundLayer, "Platform_C2", new Vector2(43.5f, 3.1f), new Vector2(3f, 0.4f), PlatformColor);
            Block(zoneC, square, groundLayer, "Platform_C3", new Vector2(48f, 2.2f), new Vector2(3f, 0.4f), PlatformColor);
            // Obstáculo alto: exige subir pela plataforma C3 (alterna movimento e combate).
            Block(zoneC, square, groundLayer, "Obstacle_C_Tall", new Vector2(52f, 1.3f), new Vector2(1f, 2.6f), ObstacleColor);
            Block(zoneC, square, groundLayer, "Wall_Right", new Vector2(62.5f, 4f), new Vector2(1f, 12f), WallColor);
        }

        // ================================================================== helpers

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Block(Transform parent, Sprite square, int layer, string name, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject(name) { layer = layer, isStatic = true };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = 0;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        private static void Backdrop(Transform parent, Sprite square, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = -10;
        }

        private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Vector2 localPos, Vector2 size, Color color, int order, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static void SetProps(Object target, System.Action<SerializedObject> edit)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
