using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nymara.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Nymara.Dev
{
    /// <summary>
    /// TEMPORÁRIO (revisão do Bren Initial). Só existe em Play Mode quando Temp/NYMARA_probe existe.
    /// Dirige o personagem com um teclado virtual (mesmas ações A/D/Espaço) e mede física e sprites.
    /// Não altera cena, prefab nem configuração salva. Resultado em Temp/NYMARA_review_{cena}.txt.
    /// </summary>
    public class BrenReviewProbe : MonoBehaviour
    {
        private string tag_;
        private PlayerController2D pc;
        private Collider2D col;
        private Rigidbody2D rb;
        private SpriteRenderer body;
        private Keyboard kb;
        private readonly StringBuilder log = new StringBuilder();
        private int landed;
        private InputSettings.BackgroundBehavior oldBg;
        private InputSettings.EditorInputBehaviorInPlayMode oldEd;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!File.Exists("Temp/NYMARA_probe")) return;
            var go = new GameObject("BrenReviewProbe");
            go.AddComponent<BrenReviewProbe>().tag_ = File.ReadAllText("Temp/NYMARA_probe").Trim();
        }

        private void Press(params Key[] keys) => InputSystem.QueueStateEvent(kb, new KeyboardState(keys));

        private IEnumerator Start()
        {
            oldBg = InputSystem.settings.backgroundBehavior;
            oldEd = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            kb = InputSystem.AddDevice<Keyboard>("NymaraReviewKeyboard");

            pc = FindFirstObjectByType<PlayerController2D>();
            col = pc.GetComponent<Collider2D>();
            rb = pc.GetComponent<Rigidbody2D>();
            body = pc.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
            pc.Landed += () => landed++;
            log.AppendLine($"CENA {tag_} player={pc.name} collider={col.GetType().Name} {((CapsuleCollider2D)col).size} offset {col.offset} body.localY={body.transform.localPosition.y}");

            yield return new WaitForSeconds(2f);
            Snap("spawn");
            Capture("idle_direita");
            yield return Sample("idle_direita", 3f);

            Press(Key.A); yield return new WaitForSeconds(0.2f); Press(); yield return new WaitForSeconds(1f);
            Snap("virou_esquerda");
            Capture("idle_esquerda");
            yield return Sample("idle_esquerda", 2f);

            Press(Key.D); yield return new WaitForSeconds(0.2f); Press(); yield return new WaitForSeconds(1f);
            Snap("virou_direita");

            // movimento horizontal
            float x0 = pc.transform.position.x, vmax = 0f;
            Press(Key.D);
            for (float t = 0; t < 1f; t += Time.deltaTime) { vmax = Mathf.Max(vmax, pc.Velocity.x); yield return null; }
            Press(); yield return new WaitForSeconds(0.6f);
            Line($"MOVIMENTO dx={pc.transform.position.x - x0:0.00} vmax={vmax:0.00}");
            Snap("parou");

            // parede esquerda (Wall_Left: face em x = -24)
            pc.TeleportTo(new Vector2(-21f, 0.85f)); yield return new WaitForSeconds(0.8f);
            Press(Key.A); yield return new WaitForSeconds(3f);
            Snap("encostado_parede");
            Line($"PAREDE colliderMinX={col.bounds.min.x:0.000} (face da parede -24,000) bodyMinX={body.bounds.min.x:0.000}");
            Capture("parede");
            Press(); yield return new WaitForSeconds(0.5f);

            // pulo livre
            pc.TeleportTo(new Vector2(-18f, 0.85f)); yield return new WaitForSeconds(0.8f);
            float baseY = col.bounds.min.y, maxY = baseY; int l0 = landed;
            Press(Key.Space);
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { maxY = Mathf.Max(maxY, col.bounds.min.y); yield return null; }
            Press();
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { maxY = Mathf.Max(maxY, col.bounds.min.y); yield return null; }
            Line($"PULO altura={maxY - baseY:0.00} pousos={landed - l0}");
            Snap("depois_pulo");

            // teto: Platform_High (-9,5; base em y = 2,8)
            pc.TeleportTo(new Vector2(-9.5f, 0.85f)); yield return new WaitForSeconds(0.8f);
            float maxTop = col.bounds.max.y; float hairTop = body.bounds.max.y;
            Press(Key.Space);
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { maxTop = Mathf.Max(maxTop, col.bounds.max.y); yield return null; }
            Capture("teto");
            Press();
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { maxTop = Mathf.Max(maxTop, col.bounds.max.y); yield return null; }
            Line($"TETO colliderTopMax={maxTop:0.000} (base da plataforma 2,800) final bottom={col.bounds.min.y:0.000} grounded={pc.IsGrounded} topoDaArteParado={hairTop:0.000}");

            // degrau baixo (Step_Low topo em 1,2) e descida
            pc.TeleportTo(new Vector2(-14f, 3f)); yield return new WaitForSeconds(1.5f);
            Snap("sobre_degrau");
            Capture("degrau");
            l0 = landed;
            Press(Key.A); yield return new WaitForSeconds(1f); Press(); yield return new WaitForSeconds(1.2f);
            Snap("desceu_degrau");
            Line($"DESCIDA pousos={landed - l0}");

            // plataforma alta (Platform_Top topo 4,8) e queda até o chão
            pc.TeleportTo(new Vector2(-5f, 6.5f)); yield return new WaitForSeconds(1.5f);
            Snap("plataforma_alta");
            l0 = landed; float vfall = 0f;
            Press(Key.D);
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { vfall = Mathf.Min(vfall, pc.Velocity.y); yield return null; }
            Press();
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { vfall = Mathf.Min(vfall, pc.Velocity.y); yield return null; }
            Snap("queda_alta");
            Line($"QUEDA vMin={vfall:0.00} pousos={landed - l0}");

            // de volta ao ponto inicial, virado para a direita
            pc.TeleportTo(new Vector2(-20f, 0.85f)); yield return new WaitForSeconds(1f);
            Snap("final");
            Finish();
        }

        private IEnumerator Sample(string label, float seconds)
        {
            var names = new List<string>(); int changes = 0; string last = null; bool scaleOk = true; float minSx = 99, maxSx = -99;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                string n = body.sprite != null ? body.sprite.name : "NULL";
                if (n != last) { changes++; last = n; if (!names.Contains(n)) names.Add(n); }
                float sx = body.transform.lossyScale.x; minSx = Mathf.Min(minSx, sx); maxSx = Mathf.Max(maxSx, sx);
                yield return null;
            }
            Line($"SPRITES {label} trocas={changes - 1} em {seconds}s (≈{(changes - 1) / seconds:0.0}/s) escalaX[{minSx:0.00},{maxSx:0.00}] facing={pc.Facing} quadros={string.Join(",", names)}");
        }

        private void Snap(string label)
        {
            var b = col.bounds;
            float gap = float.NaN; string ground = "-";
            var hits = Physics2D.RaycastAll(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 5f);
            foreach (var h in hits)
            {
                if (h.collider.isTrigger || h.collider.attachedRigidbody == rb) continue;
                gap = b.min.y - h.point.y; ground = h.collider.name; break;
            }
            float feet = body.bounds.min.y;   // borda inferior do quadro; os pés ficam 3 px (0,0076 u) acima do pivô
            Line($"SNAP {label} pos=({pc.transform.position.x:0.000},{pc.transform.position.y:0.000}) v=({pc.Velocity.x:0.00},{pc.Velocity.y:0.00}) " +
                 $"grounded={pc.IsGrounded} facing={pc.Facing} estado={pc.State} colliderMin=({b.min.x:0.000},{b.min.y:0.000}) colliderMax=({b.max.x:0.000},{b.max.y:0.000}) " +
                 $"chao={ground} folgaColliderChao={gap:0.0000} pivoBody.y={body.transform.position.y:0.000} sprite={body.sprite?.name} escalaX={body.transform.lossyScale.x:0.00} flipX={body.flipX}");
        }

        private void Capture(string label) => ScreenCapture.CaptureScreenshot($"Temp/review_{tag_}_{label}.png");
        private void Line(string s) { log.AppendLine(s); }

        private void Finish()
        {
            File.WriteAllText($"Temp/NYMARA_review_{tag_}.txt", log.ToString());
            Cleanup();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        private void Cleanup()
        {
            if (kb != null) { InputSystem.RemoveDevice(kb); kb = null; }
            InputSystem.settings.backgroundBehavior = oldBg;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEd;
        }

        private void OnDestroy() => Cleanup();
    }
}
