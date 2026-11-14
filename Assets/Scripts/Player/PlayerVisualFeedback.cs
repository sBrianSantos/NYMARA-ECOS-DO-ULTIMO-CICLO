using Nymara.Core;
using UnityEngine;

namespace Nymara.Player
{
    /// <summary>
    /// Feedback visual provisório do protagonista (graybox): cor por estado,
    /// carga do ataque, dash, impacto recebido e invulnerabilidade.
    /// Apenas lê o estado dos outros componentes; não altera gameplay.
    /// </summary>
    public class PlayerVisualFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private StabilityComponent stability;
        [SerializeField] private PlayerRespawn respawn;

        [Header("Cores")]
        [SerializeField] private Color baseColor = new Color(0.55f, 0.85f, 0.95f);
        [SerializeField] private Color dashColor = new Color(0.85f, 1f, 1f);
        [SerializeField] private Color chargeColor = new Color(1f, 0.75f, 0.25f);
        [SerializeField] private Color fullChargeColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color attackColor = new Color(0.75f, 0.95f, 1f);
        [SerializeField] private Color hurtColor = new Color(1f, 0.45f, 0.45f);
        [SerializeField] private Color absorbedColor = new Color(0.45f, 0.25f, 0.7f);

        [Header("Ritmos")]
        [SerializeField, Min(0.1f)] private float fullChargePulseSpeed = 18f;
        [SerializeField, Min(0.1f)] private float invulnerableBlinkSpeed = 20f;

        private void Awake()
        {
            if (body == null) Debug.LogError($"[{nameof(PlayerVisualFeedback)}] SpriteRenderer do corpo não atribuído.", this);
        }

        private void LateUpdate()
        {
            if (body == null) return;

            Color c = baseColor;
            if (respawn != null && respawn.IsRespawning)
            {
                c = absorbedColor;
            }
            else if (controller != null && controller.IsStunned)
            {
                c = hurtColor;
            }
            else if (controller != null && controller.IsDashing)
            {
                c = dashColor;
            }
            else if (combat != null && combat.IsFullyCharged)
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.time * fullChargePulseSpeed);
                c = Color.Lerp(chargeColor, fullChargeColor, t);
            }
            else if (combat != null && combat.ChargeProgress > 0f)
            {
                c = Color.Lerp(baseColor, chargeColor, combat.ChargeProgress);
            }
            else if (combat != null && combat.IsAttacking)
            {
                c = attackColor;
            }

            if (stability != null && stability.IsInvulnerable && (respawn == null || !respawn.IsRespawning))
            {
                bool visible = Mathf.Sin(Time.time * invulnerableBlinkSpeed) > 0f;
                c.a = visible ? 1f : 0.35f;
            }

            body.color = c;
        }
    }
}
