using Nymara.Player;
using UnityEngine;

namespace Nymara.Visual
{
    /// <summary>
    /// Animação provisória de Bren por troca de quadros (sem Animator).
    /// Apenas LÊ o estado do PlayerController2D / PlayerCombat / PlayerRespawn e escolhe o sprite;
    /// também aplica um leve "squash & stretch" no transform do corpo. A cor continua a cargo do
    /// PlayerVisualFeedback. Não escreve nada em física nem em gameplay.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class BrenSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerRespawn respawn;

        [Header("Locomoção")]
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] run;
        [SerializeField] private Sprite jump, apex, fall, land, dash, hurt;

        [Header("Combo (preparação / golpe / recuperação)")]
        [SerializeField] private Sprite atk1Wind, atk1Hit, atk1Rec;
        [SerializeField] private Sprite atk2Wind, atk2Hit, atk2Rec;
        [SerializeField] private Sprite atk3Wind, atk3Hit, atk3Rec;

        [Header("Carregado e aéreo")]
        [SerializeField] private Sprite charge, chargeFull, chargedHit;
        [SerializeField] private Sprite diveWind, dive;

        [Header("Ritmos")]
        [SerializeField, Min(0.05f)] private float idleFrameTime = 0.55f;
        [Tooltip("Distância (u) percorrida em um ciclo completo de corrida.")]
        [SerializeField, Min(0.1f)] private float runCycleDistance = 2.4f;
        [SerializeField, Min(0f)] private float landPoseTime = 0.09f;
        [SerializeField, Min(0f)] private float apexBand = 2.2f;

        [Header("Squash & stretch (só visual)")]
        [SerializeField] private Vector2 landSquash = new Vector2(1.12f, 0.88f);
        [SerializeField] private Vector2 jumpStretch = new Vector2(0.9f, 1.1f);
        [SerializeField] private Vector2 dashStretch = new Vector2(1.08f, 0.94f);
        [SerializeField, Min(1f)] private float squashRecover = 14f;

        private Transform bodyT;
        private Vector3 baseScale = Vector3.one;
        private Vector2 squash = Vector2.one;
        private float idleTimer, runPhase, landTimer;
        private float lastFallSpeed;

        private void Awake()
        {
            if (body != null)
            {
                bodyT = body.transform;
                baseScale = bodyT.localScale;
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Landed += OnLanded;
                controller.Jumped += OnJumped;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Landed -= OnLanded;
                controller.Jumped -= OnJumped;
            }
        }

        private void OnLanded()
        {
            landTimer = landPoseTime;
            float k = Mathf.Clamp01(lastFallSpeed / 14f);
            squash = Vector2.Lerp(Vector2.one, landSquash, 0.35f + 0.65f * k);
        }

        private void OnJumped() => squash = jumpStretch;

        private void LateUpdate()
        {
            if (body == null || controller == null) return;
            float dt = Time.deltaTime;
            Vector2 v = controller.Velocity;
            if (!controller.IsGrounded) lastFallSpeed = Mathf.Max(0f, -v.y);
            if (landTimer > 0f) landTimer -= dt;

            Sprite s = Choose(v, dt);
            if (s != null && body.sprite != s) body.sprite = s;

            // squash & stretch
            Vector2 target = controller.IsDashing ? dashStretch : Vector2.one;
            squash = Vector2.Lerp(squash, target, 1f - Mathf.Exp(-squashRecover * dt));
            if (bodyT != null)
                bodyT.localScale = new Vector3(baseScale.x * squash.x, baseScale.y * squash.y, baseScale.z);
        }

        private Sprite Choose(Vector2 v, float dt)
        {
            if (respawn != null && respawn.IsRespawning) return hurt;
            if (controller.IsStunned) return hurt;

            if (combat != null && combat.IsAttacking)
            {
                var st = combat.State;
                switch (combat.CurrentKind)
                {
                    case AttackKind.Aerial:
                        if (st == CombatState.Startup) return diveWind;
                        if (st == CombatState.Active || controller.IsDiving) return dive;
                        return controller.IsGrounded ? land : fall;
                    case AttackKind.Charged:
                        if (st == CombatState.Startup) return chargeFull;
                        return chargedHit;
                    default:
                        int step = Mathf.Clamp(combat.ComboStep, 1, 3);
                        if (step == 1) return st == CombatState.Startup ? atk1Wind : st == CombatState.Active ? atk1Hit : atk1Rec;
                        if (step == 2) return st == CombatState.Startup ? atk2Wind : st == CombatState.Active ? atk2Hit : atk2Rec;
                        return st == CombatState.Startup ? atk3Wind : st == CombatState.Active ? atk3Hit : atk3Rec;
                }
            }

            if (combat != null && combat.ChargeProgress > 0f)
                return combat.IsFullyCharged ? chargeFull : charge;

            if (controller.IsDashing) return dash;

            if (!controller.IsGrounded)
            {
                if (Mathf.Abs(v.y) < apexBand) return apex;
                return v.y > 0f ? jump : fall;
            }

            if (landTimer > 0f) return land;

            float speed = Mathf.Abs(v.x);
            if (speed > 0.4f && run != null && run.Length > 0)
            {
                runPhase = Mathf.Repeat(runPhase + speed * dt / runCycleDistance, 1f);
                idleTimer = 0f;
                return run[Mathf.Min(run.Length - 1, (int)(runPhase * run.Length))];
            }

            runPhase = 0f;
            if (idle == null || idle.Length == 0) return body.sprite;
            idleTimer += dt;
            return idle[(int)(idleTimer / idleFrameTime) % idle.Length];
        }
    }
}
