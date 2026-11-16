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

        /// <summary>Velocidade do Idle inicial (quadros por segundo); pedido: testar entre 6 e 8.</summary>
        public const float InitialIdleFps = 7f;

        [MenuItem("NYMARA/Bren/Executar tudo (organizar, Idle, prefab, Prototype 02)", priority = 40)]
        public static void RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Bren] Saia do Play Mode primeiro."); return; }
            OrganizeFolders();
            ConfigureInitialIdleImport();
            CreateOrUpdateInitialPrefab();
            ArchiveLegacyIdle();
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

        // ------------------------------------------------------------------ 2. importação do Idle inicial (direcional)
        /// <summary>
        /// PPU do Idle direcional: o personagem ocupa ~663 px (alfa &gt; 0,5) dos 720 px de altura do quadro.
        /// 663 / 394 ≈ 1,68 u, a mesma altura visual do Idle anterior (~538 px / 320 PPU).
        /// </summary>
        public const float InitialIdlePpu = 394f;
        /// <summary>Pés: última linha com alfa em 703 de 720 → 16 px de margem inferior (mesmo critério do Idle anterior).</summary>
        public static readonly Vector2 InitialIdlePivot = new Vector2(0.5f, 16f / 720f);

        [MenuItem("NYMARA/Bren/2. Configurar importação do Idle inicial (Right e Left)", priority = 42)]
        public static void ConfigureInitialIdleImport()
        {
            int n = 0;
            for (int i = 1; i <= BrenArtPaths.InitialIdleFrames; i++)
                foreach (var path in new[] { BrenArtPaths.InitialIdleRight(i), BrenArtPaths.InitialIdleLeft(i) })
                {
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null) { Debug.LogError($"[Bren] Quadro não encontrado: {path}"); continue; }
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;      // quadro inteiro 256x720: sem tremulação por recorte
                    ti.spritePixelsPerUnit = InitialIdlePpu;
                    ti.mipmapEnabled = false;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.alphaIsTransparency = true;
                    ti.maxTextureSize = 2048;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    var s = new TextureImporterSettings();
                    ti.ReadTextureSettings(s);
                    s.spriteAlignment = (int)SpriteAlignment.Custom;
                    s.spritePivot = InitialIdlePivot;                   // mesmo pivô nas duas direções
                    s.spriteMeshType = SpriteMeshType.FullRect;
                    s.spriteExtrude = 1;
                    ti.SetTextureSettings(s);
                    ti.SaveAndReimport();
                    n++;
                }
            Debug.Log($"[Bren] Idle inicial direcional: {n} quadros como Sprite Single, PPU {InitialIdlePpu}, pivô nos pés, bilinear, sem compressão.");
        }

        private static Sprite[] LoadIdle(bool left) =>
            Enumerable.Range(1, BrenArtPaths.InitialIdleFrames)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(left ? BrenArtPaths.InitialIdleLeft(i) : BrenArtPaths.InitialIdleRight(i)))
                .ToArray();

        // ------------------------------------------------------------------ 3. prefab variante
        [MenuItem("NYMARA/Bren/3. Criar ou atualizar prefab Bren_Initial", priority = 43)]
        public static void CreateOrUpdateInitialPrefab()
        {
            EnsureFolder("Assets/Prefabs/Characters/Bren");
            var right = LoadIdle(false);
            var left = LoadIdle(true);
            if (right.Any(s => s == null) || left.Any(s => s == null)) { Debug.LogError("[Bren] Quadros de Idle Right/Left ausentes; rode o passo 2."); return; }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(InitialPrefabPath) == null)
            {
                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
                var preview = EditorSceneManager.NewPreviewScene();
                try
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
                    inst.name = "Bren_Initial";
                    ConfigureInitial(inst, right, left);
                    PrefabUtility.SaveAsPrefabAsset(inst, InitialPrefabPath);   // instância de prefab salva = Prefab Variant
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
            else
            {
                var root = PrefabUtility.LoadPrefabContents(InitialPrefabPath);
                try { ConfigureInitial(root, right, left); PrefabUtility.SaveAsPrefabAsset(root, InitialPrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log($"[Bren] {InitialPrefabPath} pronto (variante de {SwordPrefabPath}, Idle direcional).");
        }

        private static void SetArray(SerializedObject so, string prop, Sprite[] values)
        {
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>Overrides do Bren inicial sobre o prefab com espada. Só visuais/combate desligado; movimento e física herdados.</summary>
        private static void ConfigureInitial(GameObject root, Sprite[] idleRight, Sprite[] idleLeft)
        {
            var body = root.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
            body.sprite = idleRight[0];

            var anim = new SerializedObject(root.GetComponent<BrenSpriteAnimator>());
            anim.FindProperty("directionalSprites").boolValue = true;   // sem espelhamento: quadros próprios por direção
            SetArray(anim, "idle", idleRight);
            SetArray(anim, "idleLeft", idleLeft);
            // Sem arte própria ainda (Walk/Run/Jump/Fall/Land/Hurt): esses estados usam o Idle da direção atual.
            // Nunca os quadros da fase com espada.
            SetArray(anim, "run", new Sprite[0]);
            SetArray(anim, "runLeft", new Sprite[0]);
            foreach (var f in new[] { "jump", "apex", "fall", "land", "dash", "hurt",
                                      "jumpLeft", "apexLeft", "fallLeft", "landLeft", "dashLeft", "hurtLeft",
                                      "atk1Wind", "atk1Hit", "atk1Rec", "atk2Wind", "atk2Hit", "atk2Rec",
                                      "atk3Wind", "atk3Hit", "atk3Rec", "charge", "chargeFull", "chargedHit", "diveWind", "dive" })
                anim.FindProperty(f).objectReferenceValue = null;
            anim.FindProperty("idleFrameTime").floatValue = 1f / InitialIdleFps;
            anim.ApplyModifiedPropertiesWithoutUndo();

            // Fase sem arma: o combate fica desligado nesta variante (componente mantido por dependências de PlayerRespawn/HUD).
            var combat = root.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
        }

        // ------------------------------------------------------------------ 3b. arquivar o Idle anterior
        [MenuItem("NYMARA/Bren/3b. Arquivar Idle antigo do Bren inicial (Legacy)", priority = 45)]
        public static void ArchiveLegacyIdle()
        {
            EnsureFolder(BrenArtPaths.InitialIdleLegacyDir);
            // Segurança: só move se nenhum prefab/cena ainda depender dos arquivos antigos.
            var users = AssetDatabase.FindAssets("t:Prefab t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            int moved = 0;
            foreach (var file in BrenArtPaths.InitialIdleLegacyFiles)
            {
                string src = $"{BrenArtPaths.InitialIdleDir}/{file}";
                if (AssetDatabase.LoadMainAssetAtPath(src) == null) continue;   // já arquivado (idempotente)
                var dependents = users.Where(u => AssetDatabase.GetDependencies(u, true).Contains(src)).ToArray();
                if (dependents.Length > 0)
                {
                    Debug.LogWarning($"[Bren] {src} ainda é usado por {string.Join(", ", dependents)}; mantido no lugar.");
                    continue;
                }
                string err = AssetDatabase.MoveAsset(src, $"{BrenArtPaths.InitialIdleLegacyDir}/{file}");
                if (!string.IsNullOrEmpty(err)) Debug.LogError($"[Bren] Falha ao arquivar {src}: {err}");
                else moved++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bren] Idle antigo: {moved} arquivo(s) movidos para {BrenArtPaths.InitialIdleLegacyDir} (GUIDs preservados).");
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
