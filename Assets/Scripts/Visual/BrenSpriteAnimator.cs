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

        [Header("Direcional (opcional; desligado = espelhamento pela escala, como no Bren com espada)")]
        [Tooltip("Ligado: o corpo nunca é espelhado. Os campos acima são os quadros virados para a DIREITA e os " +
                 "campos '...Left' abaixo os quadros próprios para a ESQUERDA. Estado sem quadro para a esquerda usa o Idle da esquerda.")]
        [SerializeField] private bool directionalSprites;
        [SerializeField] private Sprite[] idleLeft;
        [SerializeField] private Sprite[] runLeft;
        [SerializeField] private Sprite jumpLeft, apexLeft, fallLeft, landLeft, dashLeft, hurtLeft;

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
            if (s == null && directionalSprites) s = IdleFrame(dt);   // estado sem arte: Idle da direção atual, nunca espelhado
            if (s != null && body.sprite != s) body.sprite = s;

            // squash & stretch
            Vector2 target = controller.IsDashing ? dashStretch : Vector2.one;
            squash = Vector2.Lerp(squash, target, 1f - Mathf.Exp(-squashRecover * dt));
            if (bodyT != null)
            {
                // Sprites direcionais: compensa o flip de escala do personagem (feito pelo PlayerController2D) só no
                // transform visual do corpo. Física, colliders e hitboxes continuam espelhados como antes.
                float flip = directionalSprites && bodyT.parent != null && bodyT.parent.lossyScale.x < 0f ? -1f : 1f;
                bodyT.localScale = new Vector3(baseScale.x * squash.x * flip, baseScale.y * squash.y, baseScale.z);
            }
        }

        /// <summary>Direção vem do PlayerController2D.Facing (única fonte de verdade).</summary>
        private bool UseLeft => directionalSprites && controller != null && controller.Facing < 0;

        private Sprite Dir(Sprite right, Sprite left) => UseLeft ? left : right;
        private Sprite Side(Sprite right) => UseLeft ? null : right;   // sem versão para a esquerda (ex.: combate)

        private Sprite IdleFrame(float dt)
        {
            Sprite[] frames = UseLeft ? idleLeft : idle;
            if (frames == null || frames.Length == 0) return directionalSprites ? null : body.sprite;
            idleTimer += dt;   // mesmo relógio para as duas direções: a troca preserva a fase da animação
            return frames[(int)(idleTimer / idleFrameTime) % frames.Length];
        }

        private Sprite Choose(Vector2 v, float dt)
        {
            if (respawn != null && respawn.IsRespawning) return Dir(hurt, hurtLeft);
            if (controller.IsStunned) return Dir(hurt, hurtLeft);

            if (combat != null && combat.IsAttacking)
            {
                var st = combat.State;
                switch (combat.CurrentKind)
                {
                    case AttackKind.Aerial:
                        if (st == CombatState.Startup) return Side(diveWind);
                        if (st == CombatState.Active || controller.IsDiving) return Side(dive);
                        return controller.IsGrounded ? Dir(land, landLeft) : Dir(fall, fallLeft);
                    case AttackKind.Charged:
                        if (st == CombatState.Startup) return Side(chargeFull);
                        return Side(chargedHit);
                    default:
                        int step = Mathf.Clamp(combat.ComboStep, 1, 3);
                        if (step == 1) return Side(st == CombatState.Startup ? atk1Wind : st == CombatState.Active ? atk1Hit : atk1Rec);
                        if (step == 2) return Side(st == CombatState.Startup ? atk2Wind : st == CombatState.Active ? atk2Hit : atk2Rec);
                        return Side(st == CombatState.Startup ? atk3Wind : st == CombatState.Active ? atk3Hit : atk3Rec);
                }
            }

            if (combat != null && combat.ChargeProgress > 0f)
                return Side(combat.IsFullyCharged ? chargeFull : charge);

            if (controller.IsDashing) return Dir(dash, dashLeft);

            if (!controller.IsGrounded)
            {
                if (Mathf.Abs(v.y) < apexBand) return Dir(apex, apexLeft);
                return v.y > 0f ? Dir(jump, jumpLeft) : Dir(fall, fallLeft);
            }

            if (landTimer > 0f) return Dir(land, landLeft);

            float speed = Mathf.Abs(v.x);
            Sprite[] runFrames = UseLeft ? runLeft : run;
            if (speed > 0.4f && runFrames != null && runFrames.Length > 0)
            {
                runPhase = Mathf.Repeat(runPhase + speed * dt / runCycleDistance, 1f);
                idleTimer = 0f;
                return runFrames[Mathf.Min(runFrames.Length - 1, (int)(runPhase * runFrames.Length))];
            }

            runPhase = 0f;
            return IdleFrame(dt);
        }
    }
}
