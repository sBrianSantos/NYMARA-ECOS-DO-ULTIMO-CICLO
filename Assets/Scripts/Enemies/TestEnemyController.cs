using Nymara.Core;
using UnityEngine;

namespace Nymara.Enemies
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Windup,
        Attack,
        Recover,
        Staggered,
        Liberated
    }

    /// <summary>
    /// Eco Aprisionado (inimigo de TESTE, não canônico): patrulha, aproxima-se do
    /// protagonista e executa um golpe simples com aviso visual. Reage a impactos
    /// e, ao esgotar a estabilidade, entra no estado Liberated (ver EnemyLiberation).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TestEnemyController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private StabilityComponent stability;
        [SerializeField] private AttackHitbox attackHitbox;
        [SerializeField] private EnemyLiberation liberation;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Alvo a perseguir (o protagonista).")]
        [SerializeField] private Transform target;

        [Header("Patrulha")]
        [SerializeField, Min(0f)] private float patrolDistance = 2.5f;
        [SerializeField, Min(0f)] private float patrolSpeed = 1.5f;
        [SerializeField, Min(0f)] private float patrolPauseTime = 0.6f;

        [Header("Percepção e perseguição")]
        [SerializeField, Min(0f)] private float detectRange = 6f;
        [SerializeField, Min(0f)] private float maxVerticalDetect = 2.5f;
        [SerializeField, Min(0f)] private float chaseSpeed = 2.8f;
        [Tooltip("Distância máxima da posição inicial durante a perseguição.")]
        [SerializeField, Min(0f)] private float leashDistance = 7f;

        [Header("Ataque")]
        [SerializeField, Min(0f)] private float attackRange = 1.4f;
        [SerializeField, Min(0f)] private float windupTime = 0.45f;
        [SerializeField, Min(0.01f)] private float activeTime = 0.15f;
        [SerializeField, Min(0f)] private float recoverTime = 0.7f;
        [SerializeField, Min(0f)] private float attackDamage = 1f;
        [SerializeField, Min(0f)] private float attackKnockback = 6f;
        [SerializeField, Min(0f)] private float attackLift = 4f;
        [SerializeField, Min(0f)] private float attackStun = 0.25f;
        [SerializeField] private Vector2 attackHitboxOffset = new Vector2(0.8f, 0f);
        [SerializeField] private Vector2 attackHitboxSize = new Vector2(1.1f, 0.9f);

        [Header("Reação a impacto")]
        [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;
        [SerializeField, Min(0f)] private float hitFlashTime = 0.08f;
        [SerializeField, Min(0f)] private float minStaggerTime = 0.15f;

        [Header("Cores (graybox)")]
        [SerializeField] private Color baseColor = new Color(0.42f, 0.25f, 0.55f);
        [SerializeField] private Color windupColor = new Color(0.95f, 0.45f, 0.25f);
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color staggerColor = new Color(0.7f, 0.55f, 0.85f);

        private Rigidbody2D rb;
        private StabilityComponent targetStability;
        private Vector3 homePosition;
        private int facing = 1;
        private int patrolDirection = 1;
        private float stateTimeLeft;
        private float hitFlashUntil;

        public EnemyState State { get; private set; } = EnemyState.Patrol;
        public StabilityComponent Stability => stability;
        public bool IsLiberated => State == EnemyState.Liberated;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (stability == null) stability = GetComponent<StabilityComponent>();
            if (liberation == null) liberation = GetComponent<EnemyLiberation>();
            if (stability == null) Debug.LogError($"[{nameof(TestEnemyController)}] StabilityComponent ausente.", this);
            if (attackHitbox == null) Debug.LogError($"[{nameof(TestEnemyController)}] AttackHitbox não atribuída.", this);
            if (target != null) targetStability = target.GetComponent<StabilityComponent>();
            homePosition = transform.position;
            facing = transform.localScale.x >= 0f ? 1 : -1;
        }

        private void OnEnable()
        {
            if (stability != null)
            {
                stability.Damaged += OnDamaged;
                stability.Depleted += OnDepleted;
            }
        }

        private void OnDisable()
        {
            if (stability != null)
            {
                stability.Damaged -= OnDamaged;
                stability.Depleted -= OnDepleted;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetStability = target != null ? target.GetComponent<StabilityComponent>() : null;
        }

        // ------------------------------------------------------------------ eventos

        private void OnDamaged(HitInfo hit)
        {
            if (State == EnemyState.Liberated) return;

            hitFlashUntil = Time.time + hitFlashTime;
            if (attackHitbox != null) attackHitbox.Deactivate();

            float k = 1f - knockbackResistance;
            int dir = hit.Direction != 0 ? hit.Direction : -facing;
            rb.linearVelocity = new Vector2(dir * hit.KnockbackSpeed * k, hit.KnockbackLift * k);
            Face(-dir); // vira para quem o atingiu

            ChangeState(EnemyState.Staggered, Mathf.Max(minStaggerTime, hit.StaggerTime));
        }

        private void OnDepleted()
        {
            if (attackHitbox != null) attackHitbox.Deactivate();
            State = EnemyState.Liberated;
            if (liberation != null) liberation.Play();
            else gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ loop

        private void FixedUpdate()
        {
            if (State == EnemyState.Liberated) return;

            float dt = Time.fixedDeltaTime;
            stateTimeLeft -= dt;
            Vector2 v = rb.linearVelocity;

            switch (State)
            {
                case EnemyState.Patrol:
                    v.x = TickPatrol();
                    if (CanSeeTarget()) ChangeState(EnemyState.Chase, 0f);
                    break;

                case EnemyState.Chase:
                    v.x = TickChase();
                    break;

                case EnemyState.Windup:
                    v.x = 0f;
                    if (stateTimeLeft <= 0f)
                    {
                        ChangeState(EnemyState.Attack, activeTime);
                        ActivateAttack();
                    }
                    break;

                case EnemyState.Attack:
                    v.x = 0f;
                    if (stateTimeLeft <= 0f)
                    {
                        if (attackHitbox != null) attackHitbox.Deactivate();
                        ChangeState(EnemyState.Recover, recoverTime);
                    }
                    break;

                case EnemyState.Recover:
                    v.x = 0f;
                    if (stateTimeLeft <= 0f) ChangeState(CanSeeTarget() ? EnemyState.Chase : EnemyState.Patrol, patrolPauseTime);
                    break;

                case EnemyState.Staggered:
                    v.x = Mathf.MoveTowards(v.x, 0f, 20f * dt);
                    if (stateTimeLeft <= 0f) ChangeState(CanSeeTarget() ? EnemyState.Chase : EnemyState.Patrol, patrolPauseTime);
                    break;
            }

            rb.linearVelocity = v;
        }

        private void LateUpdate()
        {
            if (body == null || State == EnemyState.Liberated) return;
            if (Time.time < hitFlashUntil) body.color = hitFlashColor;
            else if (State == EnemyState.Windup) body.color = Color.Lerp(baseColor, windupColor, 1f - Mathf.Clamp01(stateTimeLeft / Mathf.Max(0.01f, windupTime)) * 0.5f);
            else if (State == EnemyState.Staggered) body.color = staggerColor;
            else body.color = baseColor;
        }

        private float TickPatrol()
        {
            if (stateTimeLeft > 0f) return 0f; // pausa nas extremidades

            float offset = transform.position.x - homePosition.x;
            if (offset > patrolDistance && patrolDirection > 0 || offset < -patrolDistance && patrolDirection < 0)
            {
                patrolDirection = -patrolDirection;
                stateTimeLeft = patrolPauseTime;
                return 0f;
            }
            Face(patrolDirection);
            return patrolDirection * patrolSpeed;
        }

        private float TickChase()
        {
            if (!CanSeeTarget())
            {
                ChangeState(EnemyState.Patrol, patrolPauseTime);
                return 0f;
            }

            float dx = target.position.x - transform.position.x;
            int dir = dx >= 0f ? 1 : -1;
            Face(dir);

            if (Mathf.Abs(dx) <= attackRange)
            {
                ChangeState(EnemyState.Windup, windupTime);
                return 0f;
            }

            float nextOffset = transform.position.x + dir * chaseSpeed * Time.fixedDeltaTime - homePosition.x;
            if (Mathf.Abs(nextOffset) > leashDistance) return 0f; // não sai da sua área
            return dir * chaseSpeed;
        }

        private bool CanSeeTarget()
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            if (targetStability != null && (targetStability.IsDepleted)) return false;
            Vector2 d = target.position - transform.position;
            return Mathf.Abs(d.x) <= detectRange && Mathf.Abs(d.y) <= maxVerticalDetect;
        }

        private void ActivateAttack()
        {
            if (attackHitbox == null) return;
            var hit = new HitInfo
            {
                Damage = attackDamage,
                KnockbackSpeed = attackKnockback,
                KnockbackLift = attackLift,
                StaggerTime = attackStun,
                Direction = facing,
                Source = gameObject
            };
            attackHitbox.Activate(attackHitboxOffset, attackHitboxSize, hit, new Color(1f, 0.3f, 0.3f, 0.45f));
        }

        private void ChangeState(EnemyState next, float duration)
        {
            State = next;
            stateTimeLeft = duration;
        }

        private void Face(int dir)
        {
            if (dir == 0 || dir == facing) return;
            facing = dir;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
        }

        /// <summary>
        /// Usado no retorno do protagonista: Ecos ainda aprisionados voltam ao início.
        /// Ecos libertados NÃO voltam a ser adversários.
        /// </summary>
        public void ResetIfStillCaptured()
        {
            if (State == EnemyState.Liberated) return;
            if (attackHitbox != null) attackHitbox.Deactivate();
            rb.linearVelocity = Vector2.zero;
            rb.position = homePosition;
            transform.position = homePosition;
            if (stability != null) stability.RestoreFull();
            hitFlashUntil = 0f;
            ChangeState(EnemyState.Patrol, patrolPauseTime);
        }
    }
}
