using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nymara.Player;
using Nymara.UI;
using Nymara.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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

        /// <summary>Velocidade do Idle inicial (quadros por segundo); antes 7; reduzido a pedido para um Idle mais calmo.</summary>
        public const float InitialIdleFps = 5f;

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

        // ------------------------------------------------------------------ 2. importação do Idle inicial (direcional)
        /// <summary>
        /// PPU do Idle direcional: o personagem ocupa ~663–668 px (alfa &gt; 20) dos 724 px de altura do quadro.
        /// 663 / 394 ≈ 1,68 u, a mesma altura visual do Idle anterior (~538 px / 320 PPU).
        /// </summary>
        public const float InitialIdlePpu = 394f;

        // Idle Right e Left definitivos: dois spritesheets 2172×724 com 8 quadros lado a lado, na mesma grade de 271,5 px
        // (célula i começa em x = round(i × 271,5)). Medido nos PNGs (alfa > 20):
        //   Right: altura 654–671 px (média ≈ 663); pés na linha 696; centro da silhueta a ≈ 156,6 px da borda da célula.
        //   Left:  altura 663–673 px (média ≈ 668); pés na linha 700; centro da silhueta a ≈ 147,1 px da borda da célula.
        // Mesma altura do Idle anterior → PPU 394 nos dois. Pivô x no centro médio da silhueta (o leve balanço da arte é mantido);
        // pivô y 3 px abaixo da base do pé: Right 724 − 697 − 3 = 24 px; Left 724 − 701 − 3 = 20 px.
        public const int SheetWidth = 2172, SheetHeight = 724;
        public const float SheetCellWidth = 271.5f;
        public const float RightPivotX = 156.6f, RightPivotY = 24f;
        public const float LeftPivotX = 147.1f, LeftPivotY = 20f;

        [MenuItem("NYMARA/Bren/2. Configurar importação do Idle inicial (Right e Left)", priority = 42)]
        public static void ConfigureInitialIdleImport()
        {
            int n = ConfigureSheet(BrenArtPaths.InitialIdleRightSheet, BrenArtPaths.InitialIdleRightFrameName, RightPivotX, RightPivotY)
                  + ConfigureSheet(BrenArtPaths.InitialIdleLeftSheet, BrenArtPaths.InitialIdleLeftFrameName, LeftPivotX, LeftPivotY);
            Debug.Log($"[Bren] Idle inicial: {n} quadros (Right e Left, spritesheets fatiados), PPU {InitialIdlePpu}, pivô nos pés, bilinear, sem compressão.");
        }

        private static int ConfigureSheet(string path, System.Func<int, string> frameName, float pivotX, float pivotY)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError($"[Bren] Spritesheet não encontrado: {path}"); return 0; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = InitialIdlePpu;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;                              // 2172 px de largura: 2048 reduziria a arte
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteExtrude = 1;
            ti.SetTextureSettings(st);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var existing = new Dictionary<string, GUID>();
            foreach (var r in dp.GetSpriteRects()) existing[r.name] = r.spriteID;    // IDs estáveis entre execuções
            var rects = new List<SpriteRect>();
            for (int i = 0; i < BrenArtPaths.InitialIdleFrames; i++)
            {
                int x0 = Mathf.FloorToInt(i * SheetCellWidth + 0.5f);
                int x1 = i == BrenArtPaths.InitialIdleFrames - 1 ? SheetWidth : Mathf.FloorToInt((i + 1) * SheetCellWidth + 0.5f);
                string name = frameName(i + 1);
                rects.Add(new SpriteRect
                {
                    name = name,
                    rect = new Rect(x0, 0, x1 - x0, SheetHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(pivotX / (x1 - x0), pivotY / SheetHeight),
                    border = Vector4.zero,
                    spriteID = existing.TryGetValue(name, out var id) ? id : GUID.Generate(),
                });
            }
            dp.SetSpriteRects(rects.ToArray());
            var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            dp.Apply();
            ti.SaveAndReimport();
            return rects.Count;
        }

        private static Sprite[] LoadIdle(bool left)
        {
            string path = left ? BrenArtPaths.InitialIdleLeftSheet : BrenArtPaths.InitialIdleRightSheet;
            System.Func<int, string> frameName = left ? BrenArtPaths.InitialIdleLeftFrameName : BrenArtPaths.InitialIdleRightFrameName;
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return Enumerable.Range(1, BrenArtPaths.InitialIdleFrames)
                .Select(i => all.FirstOrDefault(sp => sp.name == frameName(i))).ToArray();
        }

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
