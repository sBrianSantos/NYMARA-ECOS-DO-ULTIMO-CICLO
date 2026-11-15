using Nymara.Core;
using Nymara.Player;
using UnityEngine;

namespace Nymara.Visual
{
    /// <summary>
    /// Efeitos visuais discretos de Bren: poeira (pulo/pouso/dash), rastro do dash e do mergulho,
    /// arcos do Fio do Limiar, faíscas de impacto, brilho da carga e impacto do mergulho.
    /// Apenas reage a eventos/estados existentes; não altera gameplay.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PlayerVfx : MonoBehaviour
    {
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Material fxMaterial;

        [Header("Sprites")]
        [SerializeField] private Sprite dust;
        [SerializeField] private Sprite slash;
        [SerializeField] private Sprite slashHeavy;
        [SerializeField] private Sprite spark;
        [SerializeField] private Sprite glow;
        [SerializeField] private Sprite ring;
        [SerializeField] private Sprite groundBurst;

        [Header("Cores")]
        [SerializeField] private Color fioColor = new Color(1f, 0.95f, 0.8f, 1f);
        [SerializeField] private Color chargedColor = new Color(1f, 0.88f, 0.6f, 1f);
        [SerializeField] private Color dustColor = new Color(0.86f, 0.82f, 0.72f, 0.75f);
        [SerializeField] private Color ghostColor = new Color(0.82f, 0.9f, 1f, 0.42f);
        [SerializeField] private Color sparkColor = new Color(1f, 0.96f, 0.82f, 1f);

        [Header("Geometria (local, olhando para a direita)")]
        [SerializeField] private float feetOffset = -0.8f;
        [SerializeField] private Vector2 chargeGlowLocal = new Vector2(-0.6f, 0.25f);
        [SerializeField, Min(0.01f)] private float ghostInterval = 0.035f;

        private CombatState lastState;
        private bool lastFullyCharged, wasDiving;
        private float ghostTimer, lastFallSpeed;
        private SpriteRenderer chargeGlow;

        private void Awake()
        {
            if (glow != null)
            {
                var go = new GameObject("ChargeGlow");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = chargeGlowLocal;
                chargeGlow = go.AddComponent<SpriteRenderer>();
                chargeGlow.sprite = glow;
                chargeGlow.sortingOrder = 13;
                if (fxMaterial != null) chargeGlow.sharedMaterial = fxMaterial;
                chargeGlow.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Landed += OnLanded;
                controller.Jumped += OnJumped;
                controller.DashStarted += OnDashStarted;
            }
            if (combat != null) combat.TargetHit += OnTargetHit;
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Landed -= OnLanded;
                controller.Jumped -= OnJumped;
                controller.DashStarted -= OnDashStarted;
            }
            if (combat != null) combat.TargetHit -= OnTargetHit;
        }

        private int Facing => controller != null ? controller.Facing : 1;
        private Vector3 Feet => transform.position + new Vector3(0f, feetOffset, 0f);

        private SimpleFx Fx(Sprite s, Vector3 pos, Color c, float life, Vector3 s0, Vector3 s1, int order,
                            Transform parent = null, float rot = 0f, Vector2 vel = default, float spin = 0f, bool fadeIn = false)
            => SimpleFx.Spawn(s, pos, c, life, s0, s1, order, fxMaterial, parent, rot, vel, 0f, spin, fadeIn);

        private void Puff(Vector3 at, float size, Vector2 vel, float life = 0.35f)
            => Fx(dust, at, dustColor, life, Vector3.one * size * 0.55f, Vector3.one * size, 11, null, 0f, vel);

        private void OnJumped()
        {
            if (controller.JumpCount <= 1) Puff(Feet, 0.55f, new Vector2(0f, 0.3f), 0.3f);
            else Fx(ring, Feet + Vector3.up * 0.1f, new Color(1f, 1f, 1f, 0.35f), 0.2f, new Vector3(0.3f, 0.12f, 1f), new Vector3(0.9f, 0.3f, 1f), 11);
        }

        private void OnLanded()
        {
            bool diveImpact = wasDiving || (combat != null && combat.IsAttacking && combat.CurrentKind == AttackKind.Aerial);
            if (diveImpact)
            {
                Fx(groundBurst, Feet + Vector3.up * 0.05f, chargedColor, 0.3f, new Vector3(1.2f, 0.8f, 1f), new Vector3(2.6f, 1.3f, 1f), 14);
                Fx(spark, Feet + Vector3.up * 0.15f, sparkColor, 0.14f, Vector3.one * 0.6f, Vector3.one * 1.1f, 15, null, Random.Range(0f, 45f));
                Puff(Feet + Vector3.left * 0.3f, 0.9f, new Vector2(-2.2f, 0.25f), 0.4f);
                Puff(Feet + Vector3.right * 0.3f, 0.9f, new Vector2(2.2f, 0.25f), 0.4f);
                return;
            }
            float k = Mathf.Clamp01(lastFallSpeed / 16f);
            if (k < 0.15f) return;
            float size = Mathf.Lerp(0.45f, 0.85f, k);
            Puff(Feet + Vector3.left * 0.2f, size, new Vector2(-1.2f * (0.5f + k), 0.15f));
            Puff(Feet + Vector3.right * 0.2f, size, new Vector2(1.2f * (0.5f + k), 0.15f));
        }

        private void OnDashStarted()
        {
            ghostTimer = 0f;
            if (controller.IsGrounded) Puff(Feet - Vector3.right * (0.35f * Facing), 0.6f, new Vector2(-1.6f * Facing, 0.2f), 0.3f);
        }

        private void OnTargetHit(StabilityComponent target, HitInfo hit)
        {
            if (target == null) return;
            bool heavy = hit.IsHeavy;
            int dir = hit.Direction != 0 ? hit.Direction : Facing;
            Vector3 p = target.transform.position + new Vector3(-0.25f * dir, 0.15f, 0f);
            float s = heavy ? 1.15f : 0.75f;
            Fx(spark, p, sparkColor, heavy ? 0.18f : 0.13f, Vector3.one * s * 0.6f, Vector3.one * s, 20, null, Random.Range(0f, 90f));
            Fx(glow, p, new Color(1f, 0.92f, 0.75f, 0.6f), 0.16f, Vector3.one * s * 1.2f, Vector3.one * s * 2f, 19);
        }

        private void SpawnSlash(AttackKind kind, int step)
        {
            Transform t = transform;
            switch (kind)
            {
                case AttackKind.Aerial:
                    Fx(slash, t.TransformPoint(new Vector3(0f, -0.25f, 0f)), fioColor, 0.16f, new Vector3(0.75f, 0.75f, 1f), new Vector3(0.85f, 0.8f, 1f), 12, t, -90f);
                    break;
                case AttackKind.Charged:
                    Fx(slashHeavy, t.TransformPoint(new Vector3(0.1f, 0.2f, 0f)), chargedColor, 0.2f, new Vector3(1.1f, -0.55f, 1f), new Vector3(1.35f, -0.62f, 1f), 12, t, 0f);
                    Fx(glow, t.TransformPoint(new Vector3(1.0f, 0.25f, 0f)), new Color(1f, 0.9f, 0.65f, 0.7f), 0.2f, new Vector3(1.5f, 0.5f, 1f), new Vector3(3.2f, 0.7f, 1f), 12);
                    break;
                default:
                    if (step <= 1)
                        Fx(slash, t.TransformPoint(new Vector3(0.05f, 0.25f, 0f)), fioColor, 0.14f, new Vector3(0.82f, -0.72f, 1f), new Vector3(0.92f, -0.8f, 1f), 12, t, 0f);
                    else if (step == 2)
                        Fx(slash, t.TransformPoint(new Vector3(0.05f, 0.35f, 0f)), fioColor, 0.14f, new Vector3(0.82f, 0.78f, 1f), new Vector3(0.92f, 0.86f, 1f), 12, t, 20f);
                    else
                        Fx(slashHeavy, t.TransformPoint(new Vector3(0f, 0.3f, 0f)), fioColor, 0.17f, new Vector3(0.85f, -0.85f, 1f), new Vector3(0.95f, -0.95f, 1f), 12, t, -10f);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            if (!controller.IsGrounded) lastFallSpeed = Mathf.Max(0f, -controller.Velocity.y);

            // arco do golpe na transição para a fase ativa
            if (combat != null)
            {
                var st = combat.State;
                if (st == CombatState.Active && lastState != CombatState.Active)
                    SpawnSlash(combat.CurrentKind, combat.ComboStep);
                lastState = st;
                UpdateChargeGlow();
            }

            // rastro (dash e mergulho)
            bool trail = controller.IsDashing || controller.IsDiving;
            if (trail && body != null && body.sprite != null)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f)
                {
                    ghostTimer = ghostInterval;
                    Color gc = ghostColor;
                    if (controller.IsDiving) gc = new Color(fioColor.r, fioColor.g, fioColor.b, 0.32f);
                    var g = SimpleFx.Spawn(body.sprite, body.transform.position, gc, 0.16f, body.transform.lossyScale, body.transform.lossyScale,
                                           9, fxMaterial);
                    if (g != null) g.Renderer.flipX = body.flipX;
                }
            }
            wasDiving = controller.IsDiving;
        }

        private void UpdateChargeGlow()
        {
            if (chargeGlow == null) return;
            float p = combat.ChargeProgress;
            bool full = combat.IsFullyCharged;
            chargeGlow.enabled = p > 0f;
            if (p > 0f)
            {
                float pulse = full ? 1f + 0.12f * Mathf.Sin(Time.time * 18f) : 1f;
                float s = (0.5f + 1.1f * p) * pulse;
                chargeGlow.transform.localScale = new Vector3(s, s, 1f);
                Color c = Color.Lerp(new Color(1f, 0.85f, 0.55f, 0.6f), new Color(1f, 0.95f, 0.78f, 1f), full ? 1f : p);
                chargeGlow.color = c;
            }
            if (full && !lastFullyCharged && ring != null)
                Fx(ring, chargeGlow.transform.position, new Color(1f, 0.92f, 0.7f, 0.8f), 0.25f, Vector3.one * 0.2f, Vector3.one * 0.9f, 13);
            lastFullyCharged = full;
        }
    }
}
