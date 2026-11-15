using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nymara.Player;
using Nymara.UI;
using Nymara.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nymara.EditorTools
{
    /// <summary>
    /// Separação das duas fases visuais de Bren e criação do Prototype 02 (Bren inicial, sem a Espada de Arua).
    /// - Organiza Assets/Art/Prototype/Bren em Shared / Initial / Arua_Sword (MoveAsset: GUIDs preservados).
    /// - Configura a importação dos 8 quadros de Idle do Bren inicial (sem alterar os PNGs).
    /// - Cria/atualiza o prefab variante Bren_Initial (base: Player.prefab do Prototype 01, que é o Bren com espada).
    /// - Cria a cena Prototype_02_Bren_Initial a partir de uma cópia do Prototype 01, sem inimigos e com o Bren inicial.
    /// O Prototype 01, o Player.prefab e os scripts de gameplay não são alterados.
    /// </summary>
    public static class BrenPhaseSetup
    {
        public const string SwordPrefabPath = "Assets/Prefabs/Prototype/Player.prefab";        // Bren com a Espada de Arua
        public const string InitialPrefabPath = "Assets/Prefabs/Characters/Bren/Bren_Initial.prefab";
        public const string Proto01Path = "Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity";
        public const string Proto02Path = "Assets/Scenes/Prototypes/Prototype_02_Bren_Initial.unity";
        private const string IdleDir = BrenArtPaths.Root + "/Initial/Idle";

        /// <summary>Velocidade do Idle inicial (quadros por segundo); pedido: testar entre 6 e 8.</summary>
        public const float InitialIdleFps = 7f;

        [MenuItem("NYMARA/Bren/Executar tudo (organizar, Idle, prefab, Prototype 02)", priority = 40)]
        public static void RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Bren] Saia do Play Mode primeiro."); return; }
            OrganizeFolders();
            ConfigureInitialIdleImport();
            CreateOrUpdateInitialPrefab();
            CreatePrototype02();
        }

        // ------------------------------------------------------------------ 1. pastas
        [MenuItem("NYMARA/Bren/1. Organizar pastas (Shared, Initial, Arua_Sword)", priority = 41)]
        public static void OrganizeFolders()
        {
            foreach (var rel in BrenArtPaths.Folders) EnsureFolder($"{BrenArtPaths.Root}/{rel}");
            EnsureFolder("Assets/Prefabs/Characters/Bren");

            int moved = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BrenArtPaths.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') != BrenArtPaths.Root) continue; // só os soltos na raiz
                string file = Path.GetFileNameWithoutExtension(path);
                if (!file.StartsWith("Bren_")) continue;
                string folder = BrenArtPaths.SwordFolderFor(file.Substring(5));
                if (folder == null) { Debug.LogWarning($"[Bren] Sem fase identificada, mantido: {path}"); continue; }
                string dest = $"{BrenArtPaths.Root}/Arua_Sword/{folder}/{Path.GetFileName(path)}";
                string err = AssetDatabase.MoveAsset(path, dest);
                if (!string.IsNullOrEmpty(err)) Debug.LogError($"[Bren] Falha ao mover {path}: {err}");
                else moved++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bren] Pastas organizadas; {moved} quadro(s) da fase com espada movidos para Arua_Sword (GUIDs preservados).");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ 2. importação do Idle inicial
        [MenuItem("NYMARA/Bren/2. Configurar importação do Idle inicial", priority = 42)]
        public static void ConfigureInitialIdleImport()
        {
            foreach (var name in BrenArtPaths.InitialIdle)
            {
                string path = $"{IdleDir}/{name}.png";
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { Debug.LogError($"[Bren] Quadro não encontrado: {path}"); continue; }
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;      // retângulo inteiro 224x600: sem tremulação entre quadros
                ti.spritePixelsPerUnit = 320f;                       // ~552 px de personagem ≈ 1,72 u (mesma altura do Bren atual)
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = new Vector2(0.5f, 8f / 600f);         // pés (linha 592 de 600) no pivô
                s.spriteMeshType = SpriteMeshType.FullRect;
                s.spriteExtrude = 1;
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }
            Debug.Log("[Bren] Idle inicial: 8 quadros como Sprite Single, PPU 320, pivô nos pés, bilinear, sem compressão.");
        }

        private static Sprite[] LoadInitialIdle() =>
            BrenArtPaths.InitialIdle.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>($"{IdleDir}/{n}.png")).ToArray();

        // ------------------------------------------------------------------ 3. prefab variante
        [MenuItem("NYMARA/Bren/3. Criar ou atualizar prefab Bren_Initial", priority = 43)]
        public static void CreateOrUpdateInitialPrefab()
        {
            EnsureFolder("Assets/Prefabs/Characters/Bren");
            var idle = LoadInitialIdle();
            if (idle.Any(s => s == null)) { Debug.LogError("[Bren] Quadros de Idle ausentes; rode o passo 2."); return; }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(InitialPrefabPath) == null)
            {
                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
                var preview = EditorSceneManager.NewPreviewScene();
                try
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
                    inst.name = "Bren_Initial";
                    ConfigureInitial(inst, idle);
                    PrefabUtility.SaveAsPrefabAsset(inst, InitialPrefabPath);   // instância de prefab salva = Prefab Variant
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
            else
            {
                var root = PrefabUtility.LoadPrefabContents(InitialPrefabPath);
                try { ConfigureInitial(root, idle); PrefabUtility.SaveAsPrefabAsset(root, InitialPrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log($"[Bren] {InitialPrefabPath} pronto (variante de {SwordPrefabPath}).");
        }

        /// <summary>Overrides do Bren inicial sobre o prefab com espada. Só visuais/combate desligado; movimento e física herdados.</summary>
        private static void ConfigureInitial(GameObject root, Sprite[] idle)
        {
            var body = root.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
            body.sprite = idle[0];

            var anim = new SerializedObject(root.GetComponent<BrenSpriteAnimator>());
            var idleProp = anim.FindProperty("idle");
            idleProp.arraySize = idle.Length;
            for (int i = 0; i < idle.Length; i++) idleProp.GetArrayElementAtIndex(i).objectReferenceValue = idle[i];
            anim.FindProperty("run").arraySize = 0;   // sem corrida própria ainda: o Idle continua aparecendo (sem misturar a arte da outra fase)
            foreach (var f in new[] { "jump", "apex", "fall", "land", "dash", "hurt",
                                      "atk1Wind", "atk1Hit", "atk1Rec", "atk2Wind", "atk2Hit", "atk2Rec",
                                      "atk3Wind", "atk3Hit", "atk3Rec", "charge", "chargeFull", "chargedHit", "diveWind", "dive" })
                anim.FindProperty(f).objectReferenceValue = null;
            anim.FindProperty("idleFrameTime").floatValue = 1f / InitialIdleFps;
            anim.ApplyModifiedPropertiesWithoutUndo();

            // Fase sem arma: o combate fica desligado nesta variante (componente mantido por dependências de PlayerRespawn/HUD).
            var combat = root.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
        }

        // ------------------------------------------------------------------ 4. cena
        [MenuItem("NYMARA/Bren/4. Criar cena Prototype_02_Bren_Initial", priority = 44)]
        public static void CreatePrototype02()
        {
            if (File.Exists(Proto02Path)) { Debug.LogWarning($"[Bren] {Proto02Path} já existe; nada foi recriado."); return; }
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(InitialPrefabPath);
            if (variant == null) { Debug.LogError("[Bren] Crie o prefab Bren_Initial primeiro (passo 3)."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Abre o Prototype 01 apenas como base e salva como NOVA cena; o arquivo do Prototype 01 não é gravado.
            Scene scene = EditorSceneManager.OpenScene(Proto01Path, OpenSceneMode.Single);
            var player = Object.FindFirstObjectByType<PlayerController2D>();
            var instanceRoot = player != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject) : null;
            if (instanceRoot == null) { Debug.LogError("[Bren] Instância do jogador não encontrada no Prototype 01."); return; }

            var settings = new PrefabReplacingSettings
            {
                changeRootNameToAssetName = true,
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
                logInfo = true,
            };
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instanceRoot, variant, settings, InteractionMode.AutomatedAction);

            var combatRoot = GameObject.Find("Combat");   // inimigos de teste: fora do escopo desta fase
            if (combatRoot != null) Object.DestroyImmediate(combatRoot);
            foreach (var hud in Object.FindObjectsByType<PrototypeHUD>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(hud);
                so.FindProperty("enemies").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, Proto02Path);
            Debug.Log($"[Bren] Cena criada: {Proto02Path} (ambiente do Prototype 01, Bren inicial, sem inimigos).");
        }
    }
}
