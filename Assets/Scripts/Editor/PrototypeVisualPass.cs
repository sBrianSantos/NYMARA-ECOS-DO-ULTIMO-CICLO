using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nymara.Core;
using Nymara.Enemies;
using Nymara.Player;
using Nymara.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nymara.EditorTools
{
    /// <summary>
    /// Passe visual do Prototype 01 (arte provisória ilustrada).
    /// Reaplicável: configura a importação dos sprites, ajusta os prefabs (apenas filhos/componentes
    /// visuais) e monta a raiz "ForestArt" na cena. NÃO altera colliders, posições, escalas ou
    /// parâmetros de gameplay: os blocos só têm o SpriteRenderer desligado e a arte é desenhada
    /// exatamente sobre os limites de cada collider.
    /// Se a cena for reconstruída pelo PrototypeSceneBuilder, rode este menu novamente.
    /// </summary>
    public static class PrototypeVisualPass
    {
        private const string ArtRoot = "Assets/Art/Prototype";
        private const string ScenePath = "Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity";
        private const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
        private const string EcoPrefab = "Assets/Prefabs/Prototype/EcoAprisionado.prefab";
        private const string UnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private static readonly Vector2 LevelRef = new Vector2(19f, 2f);

        [MenuItem("NYMARA/Prototype/Aplicar Passe Visual (Prototype 01)", priority = 20)]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PassVisual] Saia do Play Mode antes de aplicar o passe visual.");
                return;
            }
            ConfigureImporters();
            SetupPlayerPrefab();
            SetupEcoPrefab();
            SetupScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[PassVisual] Passe visual aplicado (sprites, prefabs e ForestArt).");
        }

        // ------------------------------------------------------------------ importação
        [MenuItem("NYMARA/Prototype/Passe Visual ▸ Só reconfigurar sprites", priority = 21)]
        public static void ConfigureImporters()
        {
            var files = Directory.GetFiles(ArtRoot, "*.png", SearchOption.AllDirectories)
                .Select(f => f.Replace('\\', '/')).Where(f => !f.EndsWith("PrototypeSquare.png")).ToList();
            foreach (var path in files)
            {
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                float ppu = 128f;
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Vector4 border = Vector4.zero;
                bool fullRect = false, compress = false;

                if (n.StartsWith("Bren_")) pivot = new Vector2(0.5f, 18f / 336f);
                else if (n.StartsWith("Eco_")) pivot = new Vector2(150f / 320f, 10f / 272f);
                else if (n.StartsWith("BG_")) { ppu = 64f; fullRect = true; compress = true; }
                else if (n.StartsWith("FG_")) { ppu = 64f; fullRect = true; compress = true; pivot = new Vector2(0.5f, 0f); }
                else if (n == "Env_Ground") { fullRect = true; border = new Vector4(0, 0, 0, 64); }
                else if (n == "Env_GrassFringe") { fullRect = true; pivot = new Vector2(0.5f, 12f / 56f); }
                else if (n == "Env_LedgeUnder") { fullRect = true; pivot = new Vector2(0.5f, 1f); }
                else if (n == "Env_RockBlock") { fullRect = true; border = new Vector4(32, 24, 32, 40); }
                else if (n == "Env_MarkerStone") pivot = new Vector2(0.5f, 0f);
                else if (n.StartsWith("Env_")) fullRect = true;
                else if (n == "FX_Slash" || n == "FX_SlashHeavy") pivot = new Vector2(0.32f, 0.5f);
                else if (n == "FX_Dust" || n == "FX_GroundBurst") pivot = new Vector2(0.5f, 0.25f);

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 2048;
                ti.textureCompression = compress ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = pivot;
                s.spriteBorder = border;
                s.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                s.spriteExtrude = 1;
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }
        }

        private static Sprite S(string rel)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{rel}.png");
            if (s == null) Debug.LogError($"[PassVisual] Sprite não encontrado: {rel}");
            return s;
        }

        private static Material Unlit() => AssetDatabase.LoadAssetAtPath<Material>(UnlitMatPath);

        // ------------------------------------------------------------------ helpers de SerializedObject
        private static void Ref(SerializedObject so, string prop, Object v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[PassVisual] Campo '{prop}' não existe em {so.targetObject.GetType().Name}"); return; }
            p.objectReferenceValue = v;
        }

        private static void Arr(SerializedObject so, string prop, IList<Object> v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[PassVisual] Campo '{prop}' não existe"); return; }
            p.arraySize = v.Count;
            for (int i = 0; i < v.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
        }

        private static void Col(SerializedObject so, string prop, Color c)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[PassVisual] Campo '{prop}' não existe"); return; }
            p.colorValue = c;
        }

        private static void Num(SerializedObject so, string prop, float v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[PassVisual] Campo '{prop}' não existe"); return; }
            p.floatValue = v;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static Transform FindOrCreate(Transform parent, string name, int layer)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        // ------------------------------------------------------------------ Bren
        private static void SetupPlayerPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var visualRoot = root.transform.Find("VisualRoot");
                var bodyT = visualRoot.Find("Body");
                var body = bodyT.GetComponent<SpriteRenderer>();
                body.sprite = S("Bren/Bren_idle_0");
                body.drawMode = SpriteDrawMode.Simple;
                body.color = Color.white;
                body.sortingOrder = 10;
                bodyT.localPosition = new Vector3(0f, -0.8f, 0f);   // pés na base do CapsuleCollider (0,8 x 1,6)
                bodyT.localScale = Vector3.one;
                var marker = visualRoot.Find("FacingMarker");
                if (marker != null) marker.gameObject.SetActive(false); // a silhueta já indica a direção

                var ctrl = root.GetComponent<PlayerController2D>();
                var combat = root.GetComponent<PlayerCombat>();
                var respawn = root.GetComponent<PlayerRespawn>();

                var anim = new SerializedObject(GetOrAdd<BrenSpriteAnimator>(root));
                Ref(anim, "body", body); Ref(anim, "controller", ctrl); Ref(anim, "combat", combat); Ref(anim, "respawn", respawn);
                Arr(anim, "idle", new Object[] { S("Bren/Bren_idle_0"), S("Bren/Bren_idle_1") });
                Arr(anim, "run", Enumerable.Range(0, 6).Select(i => (Object)S($"Bren/Bren_run_{i}")).ToList());
                foreach (var f in new[] { "jump", "apex", "fall", "land", "dash", "hurt" }) Ref(anim, f, S($"Bren/Bren_{f}"));
                for (int i = 1; i <= 3; i++)
                {
                    Ref(anim, $"atk{i}Wind", S($"Bren/Bren_atk{i}_wind"));
                    Ref(anim, $"atk{i}Hit", S($"Bren/Bren_atk{i}_hit"));
                    Ref(anim, $"atk{i}Rec", S($"Bren/Bren_atk{i}_rec"));
                }
                Ref(anim, "charge", S("Bren/Bren_charge")); Ref(anim, "chargeFull", S("Bren/Bren_charge_full"));
                Ref(anim, "chargedHit", S("Bren/Bren_charged_hit"));
                Ref(anim, "diveWind", S("Bren/Bren_dive_wind")); Ref(anim, "dive", S("Bren/Bren_dive"));
                anim.ApplyModifiedPropertiesWithoutUndo();

                var vfx = new SerializedObject(GetOrAdd<PlayerVfx>(root));
                Ref(vfx, "controller", ctrl); Ref(vfx, "combat", combat); Ref(vfx, "body", body); Ref(vfx, "fxMaterial", Unlit());
                Ref(vfx, "dust", S("FX/FX_Dust")); Ref(vfx, "slash", S("FX/FX_Slash")); Ref(vfx, "slashHeavy", S("FX/FX_SlashHeavy"));
                Ref(vfx, "spark", S("FX/FX_Spark")); Ref(vfx, "glow", S("FX/FX_Glow")); Ref(vfx, "ring", S("FX/FX_Ring"));
                Ref(vfx, "groundBurst", S("FX/FX_GroundBurst"));
                Col(vfx, "fioColor", new Color(1f, 0.95f, 0.8f, 1f));
                vfx.ApplyModifiedPropertiesWithoutUndo();

                // tintas sutis: a arte mantém suas cores; os estados continuam legíveis
                var fb = new SerializedObject(root.GetComponent<PlayerVisualFeedback>());
                Col(fb, "baseColor", Color.white);
                Col(fb, "dashColor", new Color(0.86f, 0.94f, 1f));
                Col(fb, "chargeColor", new Color(1f, 0.9f, 0.72f));
                Col(fb, "fullChargeColor", new Color(1f, 0.97f, 0.86f));
                Col(fb, "attackColor", Color.white);
                Col(fb, "hurtColor", new Color(1f, 0.62f, 0.58f));
                Col(fb, "absorbedColor", new Color(0.62f, 0.48f, 0.86f));
                fb.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------------ Eco
        private static void SetupEcoPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(EcoPrefab);
            try
            {
                int layer = root.layer;
                var mat = Unlit();
                var visualRoot = root.transform.Find("VisualRoot");
                var bodyT = visualRoot.Find("Body");
                var body = bodyT.GetComponent<SpriteRenderer>();
                body.sprite = S("Ecos/Eco_idle_0");
                body.drawMode = SpriteDrawMode.Simple;
                body.sortingOrder = 10;
                bodyT.localPosition = new Vector3(0f, -0.6f, 0f);   // pés na base do BoxCollider (0,9 x 1,2)
                bodyT.localScale = Vector3.one;

                var eye = visualRoot.Find("IntervaloEye");
                if (eye != null) { var esr = eye.GetComponent<SpriteRenderer>(); if (esr != null) esr.enabled = false; }

                var glowT = FindOrCreate(visualRoot, "IntervaloGlow", layer);
                glowT.localPosition = bodyT.localPosition; glowT.localScale = Vector3.one;
                var glow = GetOrAdd<SpriteRenderer>(glowT.gameObject);
                glow.sprite = S("Ecos/Eco_idle_0_glow"); glow.sortingOrder = 11; glow.sharedMaterial = mat;
                glow.color = new Color(0.86f, 0.52f, 1f, 0.95f);

                var flashT = FindOrCreate(visualRoot, "HitFlash", layer);
                flashT.localPosition = bodyT.localPosition; flashT.localScale = Vector3.one;
                var flash = GetOrAdd<SpriteRenderer>(flashT.gameObject);
                flash.sprite = S("Ecos/Eco_idle_0_flash"); flash.sortingOrder = 12; flash.sharedMaterial = mat;
                flash.color = new Color(1f, 1f, 1f, 0f);

                var shardsT = FindOrCreate(visualRoot, "Shards", layer);
                shardsT.localPosition = Vector3.zero;
                var shardPos = new[] { new Vector3(-0.58f, 0.78f), new Vector3(-0.8f, 0.5f), new Vector3(-0.32f, 0.95f) };
                var shardScale = new[] { 0.55f, 0.42f, 0.35f };
                var shardList = new List<Object>();
                for (int i = 0; i < shardPos.Length; i++)
                {
                    var st = FindOrCreate(shardsT, $"Shard_{i}", layer);
                    st.localPosition = shardPos[i]; st.localScale = Vector3.one * shardScale[i];
                    var sr = GetOrAdd<SpriteRenderer>(st.gameObject);
                    sr.sprite = S("FX/FX_Shard"); sr.sortingOrder = 9;
                    sr.color = new Color(0.78f, 0.7f, 0.88f);
                    shardList.Add(st);
                }

                var ctrl = root.GetComponent<TestEnemyController>();
                var ev = new SerializedObject(GetOrAdd<EcoVisual>(root));
                Ref(ev, "controller", ctrl); Ref(ev, "stability", root.GetComponent<StabilityComponent>());
                Ref(ev, "rb", root.GetComponent<Rigidbody2D>()); Ref(ev, "body", body); Ref(ev, "glowLayer", glow);
                Ref(ev, "flashLayer", flash); Ref(ev, "visualRoot", visualRoot); Ref(ev, "fxMaterial", mat);
                Arr(ev, "shards", shardList);
                var names = new[] { "idle_0", "idle_1", "move_0", "move_1", "windup", "attack", "stagger" };
                Arr(ev, "bodyFrames", names.Select(n => (Object)S($"Ecos/Eco_{n}")).ToList());
                Arr(ev, "glowFrames", names.Select(n => (Object)S($"Ecos/Eco_{n}_glow")).ToList());
                Arr(ev, "flashFrames", names.Select(n => (Object)S($"Ecos/Eco_{n}_flash")).ToList());
                Col(ev, "intervaloColor", new Color(0.9f, 0.55f, 1f, 1f));
                Ref(ev, "slash", S("FX/FX_Slash")); Ref(ev, "glow", S("FX/FX_Glow")); Ref(ev, "ring", S("FX/FX_Ring"));
                ev.ApplyModifiedPropertiesWithoutUndo();

                // cores do corpo (apenas visual): mais claras para a arte não sumir no fundo
                var tc = new SerializedObject(ctrl);
                Col(tc, "baseColor", new Color(0.68f, 0.56f, 0.8f));
                Col(tc, "windupColor", new Color(1f, 0.62f, 0.46f));
                Col(tc, "staggerColor", new Color(0.86f, 0.78f, 0.96f));
                tc.ApplyModifiedPropertiesWithoutUndo();

                var lib = new SerializedObject(root.GetComponent<EnemyLiberation>());
                Ref(lib, "moteSprite", S("FX/FX_Glow"));
                Num(lib, "moteSize", 0.6f);
                var hide = lib.FindProperty("hideOnLiberation");
                var current = new List<Object>();
                for (int i = 0; i < hide.arraySize; i++) current.Add(hide.GetArrayElementAtIndex(i).objectReferenceValue);
                foreach (var go in new Object[] { glowT.gameObject, flashT.gameObject, shardsT.gameObject })
                    if (!current.Contains(go)) current.Add(go);
                Arr(lib, "hideOnLiberation", current);
                lib.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, EcoPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------------ cena
        private static void SetupScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogError("[PassVisual] 'Environment' não encontrado na cena."); return; }

            var old = GameObject.Find("ForestArt");
            if (old != null) Object.DestroyImmediate(old);
            var art = new GameObject("ForestArt").transform;
            var bg = new GameObject("Background").transform; bg.SetParent(art, false);
            var lvl = new GameObject("LevelDressing").transform; lvl.SetParent(art, false);
            var fg = new GameObject("Foreground").transform; fg.SetParent(art, false);

            var unlit = Unlit();
            // fundos em camadas (parallax)
            Parallax(bg, "BG_Far", S("Forest/BG_Far"), new Vector2(LevelRef.x, 3.5f), new Vector2(48f, 16f), -100, new Vector2(0.9f, 0.85f), Color.white, null);
            Parallax(bg, "BG_Mid", S("Forest/BG_Mid"), new Vector2(LevelRef.x, 6f), new Vector2(60f, 16f), -80, new Vector2(0.7f, 0.3f), Color.white, null);
            Parallax(bg, "BG_LightShafts", S("Forest/BG_LightShafts"), new Vector2(LevelRef.x, 7f), new Vector2(64f, 16f), -70, new Vector2(0.6f, 0.25f), new Color(1f, 1f, 1f, 0.75f), unlit);
            Parallax(bg, "BG_Near", S("Forest/BG_Near"), new Vector2(LevelRef.x, 6f), new Vector2(76f, 16f), -60, new Vector2(0.45f, 0.15f), Color.white, null);
            // primeiro plano: só abaixo da superfície jogável, escuro e discreto
            Parallax(fg, "FG_Ferns", S("Forest/FG_Ferns"), new Vector2(LevelRef.x, -5.0f), new Vector2(112f, 4f), 25, new Vector2(-0.25f, 0f), new Color(1f, 1f, 1f, 0.7f), null);

            int count = 0;
            foreach (var col in env.GetComponentsInChildren<BoxCollider2D>(true))
            {
                var go = col.gameObject;
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;   // só o visual graybox; collider intacto
                Bounds b = col.bounds;
                if (b.size == Vector3.zero) { var t = go.transform; b = new Bounds(t.position, new Vector3(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), 0f)); }
                string n = go.name;
                var holder = new GameObject("Art_" + n).transform; holder.SetParent(lvl, false);
                if (n.StartsWith("Floor"))
                {
                    Tiled(holder, "Ground", S("Forest/Env_Ground"), b.center, b.size, 0);
                    Tiled(holder, "Grass", S("Forest/Env_GrassFringe"), new Vector2(b.center.x, b.max.y), new Vector2(b.size.x, 56f / 128f), 4);
                    Underground(holder, b);
                }
                else if (n.StartsWith("Gap"))
                {
                    // fundo do fosso: mesma superfície de musgo (mais escura) para ler como chão
                    Tiled(holder, "PitFloor", S("Forest/Env_Ground"), b.center, b.size, 0, new Color(0.72f, 0.74f, 0.7f));
                    Tiled(holder, "Grass", S("Forest/Env_GrassFringe"), new Vector2(b.center.x, b.max.y), new Vector2(b.size.x, 56f / 128f), 4, new Color(0.72f, 0.74f, 0.7f));
                    Underground(holder, b);
                }
                else if (n.StartsWith("Platform"))
                {
                    Tiled(holder, "Ledge", S("Forest/Env_Ledge"), b.center, b.size, 1);
                    Tiled(holder, "HangingRoots", S("Forest/Env_LedgeUnder"), new Vector2(b.center.x, b.min.y + 0.04f), new Vector2(b.size.x - 0.2f, 72f / 128f), -6, new Color(0.85f, 0.85f, 0.85f));
                }
                else if (n.StartsWith("Step") || n.StartsWith("Obstacle"))
                {
                    Tiled(holder, "Rock", S("Forest/Env_RockBlock"), b.center, b.size, 1);
                }
                else if (n.StartsWith("Wall"))
                {
                    Tiled(holder, "Cliff", S("Forest/Env_Cliff"), b.center, b.size, 1);
                    bool left = b.center.x < LevelRef.x;
                    float x0 = left ? b.min.x - 16f : b.max.x, x1 = left ? b.min.x : b.max.x + 16f;
                    Tiled(holder, "CliffMass", S("Forest/Env_Cliff"), new Vector2((x0 + x1) / 2f, (b.min.y - 10f + b.max.y + 4f) / 2f),
                          new Vector2(x1 - x0, b.max.y + 4f - (b.min.y - 10f)), -4, new Color(0.55f, 0.58f, 0.55f));
                }
                count++;
            }
            foreach (var t in env.GetComponentsInChildren<Transform>(true))
                if (t.name == "Backdrop") { var sr = t.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }

            // Marco provisório do ponto de retorno: pedra com runa (só visual)
            var spawn = GameObject.Find("PlayerSpawn_MarcoProvisorio");
            if (spawn != null)
            {
                var m = spawn.transform.Find("Marker");
                if (m != null)
                {
                    var sr = m.GetComponent<SpriteRenderer>();
                    sr.sprite = S("Forest/Env_MarkerStone"); sr.color = Color.white; sr.sortingOrder = 2;
                    m.localScale = Vector3.one * 0.75f;
                    m.position = new Vector3(spawn.transform.position.x - 1.3f, 0f, 0f);
                }
            }

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.23f, 0.34f, 0.31f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PassVisual] Cena: {count} blocos receberam arte; colliders não foram alterados.");
        }

        private static SpriteRenderer Tiled(Transform parent, string name, Sprite s, Vector2 pos, Vector2 size, int order, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.sortingOrder = order;
            sr.color = color ?? Color.white;
            go.isStatic = true;
            return sr;
        }

        private static void Underground(Transform holder, Bounds b)
        {
            float top = b.min.y, bottom = -12f;
            if (top <= bottom) return;
            Tiled(holder, "Underground", S("Forest/Env_DeepEarth"), new Vector2(b.center.x, (top + bottom) / 2f), new Vector2(b.size.x, top - bottom), -5);
        }

        private static void Parallax(Transform parent, string name, Sprite s, Vector2 pos, Vector2 size, int order, Vector2 factor, Color color, Material mat)
        {
            var sr = Tiled(parent, name, s, pos, size, order, color);
            sr.gameObject.isStatic = false;
            if (mat != null) sr.sharedMaterial = mat;
            var pl = new SerializedObject(sr.gameObject.AddComponent<ParallaxLayer>());
            pl.FindProperty("factor").vector2Value = factor;
            pl.FindProperty("reference").vector2Value = LevelRef;
            pl.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
