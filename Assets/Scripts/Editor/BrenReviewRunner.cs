using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nymara.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nymara.EditorTools
{
    /// <summary>
    /// TEMPORÁRIO (revisão do Bren Initial). Só age se existir Temp/NYMARA_review_trigger.
    /// 1) Reimporta os spritesheets (passo 2) comparando os IDs dos sub-sprites antes/depois.
    /// 2) Testa a idempotência do ConfigureInitial sem salvar nada (prefab existente com Run simulado e variante nova).
    /// 3) Roda o passo 3 e compara o prefab. 4) Abre P02 e P01 em Play Mode com a sonda BrenReviewProbe.
    /// Resultado: Temp/NYMARA_review_editor.txt e Temp/NYMARA_review_{cena}.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class BrenReviewRunner
    {
        private const string Trigger = "Temp/NYMARA_review_trigger";
        private const string Stage = "NYMARA_review_stage";
        private const string P01 = "Assets/Scenes/Prototypes/Prototype_01_MovementCombat.unity";

        static BrenReviewRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (File.Exists(Trigger) && SessionState.GetString(Stage, "") == "") EditorApplication.delayCall += RunEditorChecks;
            // Reexecução só do Prototype 01 (o primeiro teste foi interrompido pelo Error Pause).
            if (File.Exists("Temp/NYMARA_review_p01only"))
                EditorApplication.delayCall += () =>
                {
                    if (!File.Exists("Temp/NYMARA_review_p01only") || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    File.Delete("Temp/NYMARA_review_p01only");
                    StartScene(P01, "P01");
                };
        }

        private static string Ids(string path) =>
            string.Join(" ", AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).Select(s =>
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string g, out long id);
                return $"{s.name}={id}";
            }));

        private static void RunEditorChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetString(Stage, "editor");
            var sb = new StringBuilder();
            try
            {
                string R = BrenArtPaths.InitialIdleRightSheet, L = BrenArtPaths.InitialIdleLeftSheet;
                string beforeR = Ids(R), beforeL = Ids(L);
                BrenPhaseSetup.ConfigureInitialIdleImport();
                string afterR = Ids(R), afterL = Ids(L);
                sb.AppendLine($"IDS_RIGHT_IGUAIS {beforeR == afterR} | {afterR}");
                sb.AppendLine($"IDS_LEFT_IGUAIS {beforeL == afterL} | {afterL}");
                foreach (var p in new[] { R, L })
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().OrderBy(s => s.name).ToArray();
                    sb.AppendLine($"SHEET {Path.GetFileName(p)} quadros {sprites.Length} " +
                                  string.Join(" ", sprites.Select(s => $"{s.name}:x{s.rect.x}w{s.rect.width}h{s.rect.height}p({s.pivot.x:0.0},{s.pivot.y:0.0})ppu{s.pixelsPerUnit}")));
                }

                var setup = typeof(BrenPhaseSetup);
                var configure = setup.GetMethod("ConfigureInitial", BindingFlags.NonPublic | BindingFlags.Static);
                var load = setup.GetMethod("LoadIdle", BindingFlags.NonPublic | BindingFlags.Static);
                var right = (Sprite[])load.Invoke(null, new object[] { false });
                var left = (Sprite[])load.Invoke(null, new object[] { true });

                // (a) prefab existente com "Run" já configurado (quadros do próprio Idle fazendo papel de Run; nada é salvo)
                var root = PrefabUtility.LoadPrefabContents(BrenPhaseSetup.InitialPrefabPath);
                try
                {
                    var so = new SerializedObject(root.GetComponent<BrenSpriteAnimator>());
                    Set(so, "run", right); Set(so, "runLeft", left);
                    so.FindProperty("jump").objectReferenceValue = right[0];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    configure.Invoke(null, new object[] { root, right, left });
                    so.Update();
                    sb.AppendLine($"IDEMPOTENTE_PRESERVA_RUN run={so.FindProperty("run").arraySize} runLeft={so.FindProperty("runLeft").arraySize} jump={(so.FindProperty("jump").objectReferenceValue != null)}");
                    // segunda execução: nada muda
                    configure.Invoke(null, new object[] { root, right, left });
                    so.Update();
                    sb.AppendLine($"IDEMPOTENTE_2X run={so.FindProperty("run").arraySize} runLeft={so.FindProperty("runLeft").arraySize} idle={so.FindProperty("idle").arraySize} idleLeft={so.FindProperty("idleLeft").arraySize} fps={1f / so.FindProperty("idleFrameTime").floatValue:0.##}");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }   // descartado, não salvo

                // (b) variante nova a partir do Bren com espada (só em memória)
                var preview = EditorSceneManager.NewPreviewScene();
                try
                {
                    var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BrenPhaseSetup.SwordPrefabPath);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
                    var so0 = new SerializedObject(inst.GetComponent<BrenSpriteAnimator>());
                    int swordRun = so0.FindProperty("run").arraySize; bool swordJump = so0.FindProperty("jump").objectReferenceValue != null;
                    configure.Invoke(null, new object[] { inst, right, left });
                    var so = new SerializedObject(inst.GetComponent<BrenSpriteAnimator>());
                    var singles = new[] { "jump", "apex", "fall", "land", "dash", "hurt", "atk1Wind", "atk1Hit", "atk1Rec", "atk2Wind", "atk2Hit", "atk2Rec", "atk3Wind", "atk3Hit", "atk3Rec", "charge", "chargeFull", "chargedHit", "diveWind", "dive" };
                    int left0 = singles.Count(f => so.FindProperty(f).objectReferenceValue != null);
                    sb.AppendLine($"VARIANTE_NOVA espada(run={swordRun}, jump={swordJump}) -> run={so.FindProperty("run").arraySize} runLeft={so.FindProperty("runLeft").arraySize} quadrosEspadaRestantes={left0} directional={so.FindProperty("directionalSprites").boolValue} idle={so.FindProperty("idle").arraySize}");
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }

                // (c) passo 3 de verdade (o prefab salvo deve ficar idêntico)
                BrenPhaseSetup.CreateOrUpdateInitialPrefab();
                AssetDatabase.SaveAssets();
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(BrenPhaseSetup.InitialPrefabPath);
                var sop = new SerializedObject(pf.GetComponent<BrenSpriteAnimator>());
                sb.AppendLine("PREFAB idle: " + Names(sop.FindProperty("idle")) + " | idleLeft: " + Names(sop.FindProperty("idleLeft")) +
                              $" | fps {1f / sop.FindProperty("idleFrameTime").floatValue:0.##} | directional {sop.FindProperty("directionalSprites").boolValue}" +
                              $" | body {pf.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>().sprite?.name}");
                var col = pf.GetComponent<CapsuleCollider2D>();
                sb.AppendLine($"COLLIDER {col.size.x}x{col.size.y} offset {col.offset} dir {col.direction}");
            }
            catch (System.Exception e) { sb.AppendLine("ERRO " + e); }
            File.WriteAllText("Temp/NYMARA_review_editor.txt", sb.ToString());
            StartScene(BrenPhaseSetup.Proto02Path, "P02");
        }

        private static string Names(SerializedProperty p) =>
            string.Join(",", Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).objectReferenceValue?.name ?? "NULL"));

        private static void Set(SerializedObject so, string prop, Sprite[] v)
        {
            var p = so.FindProperty(prop); p.arraySize = v.Length;
            for (int i = 0; i < v.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = v[i];
        }

        private static void StartScene(string path, string tag)
        {
            SessionState.SetString(Stage, tag);
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            File.WriteAllText("Temp/NYMARA_probe", tag);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredEditMode) return;
            string st = SessionState.GetString(Stage, "");
            if (st == "P02") EditorApplication.delayCall += () => StartScene(P01, "P01");
            else if (st == "P01")
            {
                SessionState.SetString(Stage, "done");
                if (File.Exists(Trigger)) File.Delete(Trigger);
                if (File.Exists("Temp/NYMARA_probe")) File.Delete("Temp/NYMARA_probe");
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.OpenScene(BrenPhaseSetup.Proto02Path, OpenSceneMode.Single);
                    File.WriteAllText("Temp/NYMARA_review_done", "ok");
                };
            }
        }
    }
}
