using System.Collections.Generic;
using System.Text;
using Nymara.Core;
using Nymara.Enemies;
using Nymara.Player;
using UnityEngine;

namespace Nymara.UI
{
    /// <summary>
    /// HUD de DEBUG do protótipo (IMGUI, sem assets nem UI final):
    /// estabilidade do protagonista e dos Ecos, estado atual e controles.
    /// Apenas lê dados; não altera gameplay.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private StabilityComponent playerStability;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerRespawn playerRespawn;
        [Tooltip("Opcional: mostra o input lido (útil para diagnosticar bindings).")]
        [SerializeField] private PlayerInputReader playerInput;
        [Tooltip("Se vazio, encontra todos os TestEnemyController da cena no Start.")]
        [SerializeField] private List<TestEnemyController> enemies = new List<TestEnemyController>();
        [SerializeField] private bool showControls = true;
        [SerializeField, Min(0.5f)] private float uiScale = 1f;

        private readonly StringBuilder sb = new StringBuilder(256);
        private GUIStyle label;
        private GUIStyle smallLabel;

        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PlayerBar = new Color(0.45f, 0.85f, 0.95f);
        private static readonly Color EnemyBar = new Color(0.65f, 0.4f, 0.85f);
        private static readonly Color LiberatedBar = new Color(1f, 0.93f, 0.7f);

        private void Start()
        {
            if (enemies.Count == 0)
                enemies.AddRange(FindObjectsByType<TestEnemyController>(FindObjectsSortMode.InstanceID));
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15 * uiScale), richText = true };
            label.normal.textColor = Color.white;
            smallLabel = new GUIStyle(label) { fontSize = Mathf.RoundToInt(13 * uiScale) };
        }

        private void OnGUI()
        {
            EnsureStyles();
            float x = 16f * uiScale;
            float y = 12f * uiScale;
            float w = 340f * uiScale;
            float line = 22f * uiScale;

            GUI.Box(new Rect(x - 8f, y - 6f, w + 16f, line * (6 + enemies.Count) + 10f), GUIContent.none);

            if (playerStability != null)
            {
                DrawBar(new Rect(x, y, w, line - 4f), playerStability.Normalized, PlayerBar,
                    $"Portador — estabilidade {playerStability.Current:0.#}/{playerStability.Max:0.#}");
                y += line;
            }

            sb.Clear();
            if (playerController != null)
            {
                sb.Append("Movimento: ").Append(playerController.State);
                sb.Append("   Dash: ").Append(playerController.DashCharges);
                sb.Append("   Vel: ").Append(playerController.Velocity.x.ToString("0.0")).Append(", ").Append(playerController.Velocity.y.ToString("0.0"));
            }
            GUI.Label(new Rect(x, y, w + 120f, line), sb.ToString(), smallLabel);
            y += line;

            sb.Clear();
            if (playerCombat != null)
            {
                sb.Append("Combate: ").Append(playerCombat.State);
                if (playerCombat.IsAttacking)
                {
                    sb.Append(" (").Append(playerCombat.CurrentKind);
                    if (playerCombat.ComboStep > 0) sb.Append(' ').Append(playerCombat.ComboStep);
                    sb.Append(')');
                }
                if (playerCombat.IsCharging)
                    sb.Append(playerCombat.IsFullyCharged ? "   CARGA COMPLETA" : $"   carga {playerCombat.ChargeProgress * 100f:0}%");
            }
            if (playerRespawn != null && playerRespawn.IsRespawning) sb.Append("   [retornando...]");
            GUI.Label(new Rect(x, y, w + 120f, line), sb.ToString(), smallLabel);
            y += line;

            sb.Clear();
            if (playerInput != null)
            {
                sb.Append("Input: X ").Append(playerInput.MoveX.ToString("0.0"));
                sb.Append("   Pulo ").Append(playerInput.JumpHeld ? "[X]" : "[ ]");
                sb.Append("   Ataque ").Append(playerInput.AttackHeld ? "[X]" : "[ ]");
                sb.Append("   Pulos lidos/executados: ").Append(playerInput.JumpPressCount);
                if (playerController != null) sb.Append('/').Append(playerController.JumpCount);
            }
            GUI.Label(new Rect(x, y, w + 120f, line), sb.ToString(), smallLabel);
            y += line + 4f;

            for (int i = 0; i < enemies.Count; i++)
            {
                TestEnemyController e = enemies[i];
                if (e == null) continue;
                StabilityComponent s = e.Stability;
                bool liberated = e.IsLiberated;
                string text = liberated
                    ? $"Eco Aprisionado {i + 1} — LIBERTADO"
                    : $"Eco Aprisionado {i + 1} — {(s != null ? $"{s.Current:0.#}/{s.Max:0.#}" : "?")}  [{e.State}]";
                DrawBar(new Rect(x, y, w, line - 4f), liberated ? 1f : (s != null ? s.Normalized : 0f), liberated ? LiberatedBar : EnemyBar, text);
                y += line;
            }

            if (showControls)
            {
                const string controls =
                    "A/D ou setas: mover   Espaço: pular (segurar = mais alto)   Shift: dash\n" +
                    "J / Botão esq.: tocar = combo · segurar e soltar = carregado · no ar = aéreo   R: reiniciar cena\n" +
                    "Gamepad: stick/D-pad · A/Cross pular · RB/R1 dash · X/Square atacar · Select reiniciar";
                float h = 62f * uiScale;
                GUI.Box(new Rect(8f, Screen.height - h - 8f, 760f * uiScale, h), GUIContent.none);
                GUI.Label(new Rect(16f, Screen.height - h - 4f, 760f * uiScale, h), controls, smallLabel);
            }
        }

        private void DrawBar(Rect r, float normalized, Color fill, string text)
        {
            Color prev = GUI.color;
            GUI.color = BarBack;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(normalized), r.height), Texture2D.whiteTexture);
            GUI.color = prev;
            var shadow = new Rect(r.x + 6f, r.y - 1f, r.width, r.height + 2f);
            GUI.Label(new Rect(shadow.x + 1f, shadow.y + 1f, shadow.width, shadow.height), $"<color=#000000>{text}</color>", smallLabel);
            GUI.Label(shadow, text, smallLabel);
        }
    }
}
