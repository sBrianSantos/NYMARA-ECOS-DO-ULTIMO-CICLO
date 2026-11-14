using System;
using Nymara.Core;
using UnityEngine;

namespace Nymara.Player
{
    public enum MovementState
    {
        Grounded,
        Rising,
        Falling,
        Dashing,
        Hurt,
        Disabled
    }

    /// <summary>
    /// Movimento horizontal, pulo, dash, orientação e grounded check.
    /// É a ÚNICA fonte que escreve a velocidade do Rigidbody2D do protagonista;
    /// outros sistemas (combate, dano) fazem pedidos através da API pública.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Collider2D bodyCollider;
        [Tooltip("Opcional: estabilidade do protagonista, para aplicar recuo ao ser atingido.")]
        [SerializeField] private StabilityComponent stability;

        [Header("Detecção de chão e paredes")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField, Min(0.01f)] private float groundCheckDepth = 0.08f;
        [SerializeField, Range(0.3f, 1f)] private float groundCheckWidthFactor = 0.85f;
        [Tooltip("Layers que interrompem o dash (paredes e corpos de inimigos). Vazio = usa groundMask.")]
        [SerializeField] private LayerMask dashBlockMask;

        [Header("Movimento horizontal")]
        [SerializeField, Min(0f)] private float maxRunSpeed = 6f;
        [SerializeField, Min(0f)] private float groundAcceleration = 70f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 45f;
        [SerializeField, Min(0f)] private float airDeceleration = 25f;

        [Header("Pulo e gravidade")]
        [SerializeField, Min(0f)] private float jumpVelocity = 12f;
        [SerializeField, Min(0f)] private float baseGravityScale = 3f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.6f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 18f;
        [Tooltip("Ao soltar o pulo durante a subida, a velocidade vertical é multiplicada por este valor.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 14f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.2f;
        [Tooltip("Cargas de dash; restauradas ao tocar o chão (hipótese inicial: 1).")]
        [SerializeField, Min(1)] private int maxDashCharges = 1;
        [Tooltip("Fração da velocidade do dash mantida ao terminar.")]
        [SerializeField, Range(0f, 1f)] private float postDashSpeedKeep = 0.45f;
        [Tooltip("Padrão do MVP: sem invulnerabilidade durante o dash.")]
        [SerializeField] private bool dashGrantsInvulnerability = false;

        [Header("Impulso de ataque")]
        [Tooltip("Fração da velocidade do impulso mantida quando ele termina ou é cancelado (0 = para na hora).")]
        [SerializeField, Range(0f, 1f)] private float attackImpulseEndSpeedKeep = 0f;
        [Tooltip("Interrompe o impulso ao encostar em parede ou inimigo (usa a mesma máscara do dash), para não empurrá-los.")]
        [SerializeField] private bool attackImpulseStopsAtObstacles = true;

        [Header("Recuo ao ser atingido")]
        [SerializeField, Min(0f)] private float defaultHurtKnockback = 6f;
        [SerializeField, Min(0f)] private float defaultHurtLift = 4f;
        [SerializeField, Min(0f)] private float defaultHurtStun = 0.25f;

        private Rigidbody2D rb;
        private bool grounded;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;
        private bool jumpCutPending;
        private bool isRisingFromJump;

        private float dashTimeLeft;
        private float dashReadyTime;
        private int dashCharges;
        private int dashDirection = 1;

        private float stunTimeLeft;
        private bool controlEnabled = true;

        // Pedidos do combate
        private float horizontalControlMultiplier = 1f;
        private bool jumpBlocked;
        private bool facingLocked;
        private float impulseVelocityX;
        private float impulseTimeLeft;
        private bool impulseEndPending;
        private const float ImpulseTimeEpsilon = 0.0005f;
        private float fallSpeedClamp = -1f;

        public int Facing { get; private set; } = 1;
        public bool IsGrounded => grounded;
        public bool IsDashing => dashTimeLeft > 0f;
        public bool IsStunned => stunTimeLeft > 0f;
        public bool ControlEnabled => controlEnabled;
        public int DashCharges => dashCharges;
        /// <summary>True enquanto um impulso de ataque controla a velocidade horizontal.</summary>
        public bool IsImpulseActive => impulseTimeLeft > 0f;
        /// <summary>Contador de pulos executados (diagnóstico no HUD).</summary>
        public int JumpCount { get; private set; }
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

        public MovementState State
        {
            get
            {
                if (!controlEnabled) return MovementState.Disabled;
                if (IsDashing) return MovementState.Dashing;
                if (IsStunned) return MovementState.Hurt;
                if (grounded) return MovementState.Grounded;
                return rb.linearVelocity.y > 0.01f ? MovementState.Rising : MovementState.Falling;
            }
        }

        public event Action Jumped;
        public event Action Landed;
        public event Action DashStarted;
        public event Action DashEnded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (stability == null) stability = GetComponent<StabilityComponent>();

            if (input == null) Debug.LogError($"[{nameof(PlayerController2D)}] PlayerInputReader ausente.", this);
            if (bodyCollider == null) Debug.LogError($"[{nameof(PlayerController2D)}] Collider2D ausente.", this);
            if (groundMask.value == 0) Debug.LogWarning($"[{nameof(PlayerController2D)}] groundMask vazio: o personagem nunca estará no chão.", this);

            rb.gravityScale = baseGravityScale;
            dashCharges = maxDashCharges;
            Facing = transform.localScale.x >= 0f ? 1 : -1;
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.JumpPressed += OnJumpPressed;
                input.JumpReleased += OnJumpReleased;
                input.DashPressed += OnDashPressed;
            }
            if (stability != null) stability.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.JumpPressed -= OnJumpPressed;
                input.JumpReleased -= OnJumpReleased;
                input.DashPressed -= OnDashPressed;
            }
            if (stability != null) stability.Damaged -= OnDamaged;
        }

        // ------------------------------------------------------------------ input

        private void OnJumpPressed()
        {
            if (controlEnabled) lastJumpPressedTime = Time.time;
        }

        private void OnJumpReleased()
        {
            if (isRisingFromJump) jumpCutPending = true;
        }

        private void OnDashPressed()
        {
            if (!controlEnabled || IsDashing || IsStunned) return;
            if (dashCharges <= 0 || Time.time < dashReadyTime) return;

            float x = input != null ? input.MoveX : 0f;
            dashDirection = Mathf.Abs(x) > 0.01f ? (x > 0f ? 1 : -1) : Facing;
            ApplyFacing(dashDirection);

            dashCharges--;
            dashTimeLeft = dashDuration;
            isRisingFromJump = false;
            jumpCutPending = false;
            DropHorizontalImpulse();
            rb.gravityScale = 0f;
            if (dashGrantsInvulnerability && stability != null) stability.SetForcedInvulnerable(true);
            DashStarted?.Invoke();
        }

        private void OnDamaged(HitInfo hit)
        {
            if (!controlEnabled) return;
            int dir = hit.Direction != 0 ? hit.Direction : -Facing;
            float speed = hit.KnockbackSpeed > 0f ? hit.KnockbackSpeed : defaultHurtKnockback;
            float lift = hit.KnockbackLift > 0f ? hit.KnockbackLift : defaultHurtLift;
            float stun = hit.StaggerTime > 0f ? hit.StaggerTime : defaultHurtStun;
            ApplyKnockback(new Vector2(dir * speed, lift), stun);
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (!controlEnabled || facingLocked || IsDashing || IsStunned || input == null) return;
            if (input.MoveX > 0.01f) ApplyFacing(1);
            else if (input.MoveX < -0.01f) ApplyFacing(-1);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateGrounded();

            Vector2 v = rb.linearVelocity;

            if (IsDashing)
            {
                dashTimeLeft -= dt;
                v = new Vector2(dashDirection * dashSpeed, 0f);
                if (dashTimeLeft <= 0f || IsBlockedAhead(dashDirection, dashSpeed * dt))
                    v = EndDash(v);
                rb.linearVelocity = v;
                return;
            }

            if (grounded && dashCharges < maxDashCharges)
                dashCharges = maxDashCharges;

            if (IsStunned)
            {
                stunTimeLeft -= dt;
                v.x = Mathf.MoveTowards(v.x, 0f, airDeceleration * dt);
                ApplyGravity(ref v);
                rb.linearVelocity = v;
                return;
            }

            // Horizontal
            if (impulseEndPending)
            {
                // O impulso acabou (ou foi cancelado): não deixa velocidade residual involuntária.
                v.x *= attackImpulseEndSpeedKeep;
                impulseEndPending = false;
            }

            if (impulseTimeLeft > 0f)
            {
                // Impulso de ataque: mantém a velocidade pedida pela duração configurada,
                // sem passar pela aceleração/desaceleração normal (que o anularia em 1-2 passos).
                int impulseDirection = impulseVelocityX >= 0f ? 1 : -1;
                if (attackImpulseStopsAtObstacles && IsBlockedAhead(impulseDirection, Mathf.Abs(impulseVelocityX) * dt))
                {
                    v.x = 0f;
                    impulseTimeLeft = 0f;
                }
                else
                {
                    v.x = impulseVelocityX;
                    impulseTimeLeft -= dt;
                    // Tolerância: evita um passo extra por erro de arredondamento (ex.: 0,08 s = 4 passos de 0,02 s).
                    if (impulseTimeLeft <= ImpulseTimeEpsilon)
                    {
                        impulseTimeLeft = 0f;
                        impulseEndPending = true;
                    }
                }
            }
            else
            {
                float moveInput = controlEnabled && input != null ? input.MoveX : 0f;
                float target = moveInput * maxRunSpeed * horizontalControlMultiplier;
                bool accelerating = Mathf.Abs(target) > 0.01f && (Mathf.Abs(v.x) < Mathf.Abs(target) || Mathf.Sign(v.x) != Mathf.Sign(target));
                float rate = grounded
                    ? (accelerating ? groundAcceleration : groundDeceleration)
                    : (accelerating ? airAcceleration : airDeceleration);
                v.x = Mathf.MoveTowards(v.x, target, rate * dt);
            }

            // Pulo (com buffer e coyote time)
            bool canUseGround = grounded || Time.time - lastGroundedTime <= coyoteTime;
            bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            if (controlEnabled && !jumpBlocked && buffered && canUseGround)
            {
                v.y = jumpVelocity;
                lastJumpPressedTime = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                grounded = false;
                isRisingFromJump = true;
                JumpCount++;
                jumpCutPending = input != null && !input.JumpHeld;
                Jumped?.Invoke();
            }

            if (isRisingFromJump)
            {
                if (v.y <= 0f) isRisingFromJump = false;
                else if (jumpCutPending)
                {
                    v.y *= jumpCutMultiplier;
                    isRisingFromJump = false;
                }
                jumpCutPending = false;
            }

            ApplyGravity(ref v);
            rb.linearVelocity = v;
        }

        private void ApplyGravity(ref Vector2 v)
        {
            rb.gravityScale = v.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale;
            float limit = fallSpeedClamp >= 0f ? Mathf.Min(fallSpeedClamp, maxFallSpeed) : maxFallSpeed;
            if (v.y < -limit) v.y = -limit;
        }

        private Vector2 EndDash(Vector2 v)
        {
            dashTimeLeft = 0f;
            dashReadyTime = Time.time + dashCooldown;
            rb.gravityScale = baseGravityScale;
            if (dashGrantsInvulnerability && stability != null) stability.SetForcedInvulnerable(false);
            v.x = dashDirection * dashSpeed * postDashSpeedKeep;
            if (IsBlockedAhead(dashDirection, 0.02f)) v.x = 0f;
            DashEnded?.Invoke();
            return v;
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = grounded;
            grounded = false;
            if (bodyCollider != null && rb.linearVelocity.y <= 0.05f)
            {
                Bounds b = bodyCollider.bounds;
                Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDepth * 0.5f);
                Vector2 size = new Vector2(b.size.x * groundCheckWidthFactor, groundCheckDepth);
                grounded = Physics2D.OverlapBox(center, size, 0f, groundMask) != null;
            }

            if (grounded)
            {
                lastGroundedTime = Time.time;
                isRisingFromJump = false;
                if (!wasGrounded) Landed?.Invoke();
            }
        }

        private bool IsBlockedAhead(int direction, float distance)
        {
            LayerMask mask = dashBlockMask.value != 0 ? dashBlockMask : groundMask;
            if (bodyCollider == null) return false;
            Bounds b = bodyCollider.bounds;
            // Caixa um pouco menor que o corpo, para não detectar o próprio chão.
            Vector2 size = new Vector2(b.size.x * 0.5f, b.size.y * 0.7f);
            Vector2 origin = new Vector2(b.center.x, b.center.y + b.size.y * 0.05f);
            float castDistance = b.extents.x * 0.5f + distance;
            // O próprio corpo está na layer Player, fora da máscara.
            return Physics2D.BoxCast(origin, size, 0f, new Vector2(direction, 0f), castDistance, mask).collider != null;
        }

        private void ApplyFacing(int direction)
        {
            if (direction == 0 || direction == Facing) return;
            Facing = direction;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * direction;
            transform.localScale = s;
        }

        // ------------------------------------------------------------------ API para combate/dano/retorno

        /// <summary>Multiplicador do controle horizontal (0 = parado; 1 = normal).</summary>
        public void SetHorizontalControl(float multiplier) => horizontalControlMultiplier = Mathf.Clamp01(multiplier);

        public void SetJumpBlocked(bool blocked) => jumpBlocked = blocked;

        public void SetFacingLocked(bool locked) => facingLocked = locked;

        /// <summary>Limita a velocidade de queda (valor negativo desliga).</summary>
        public void SetFallSpeedClamp(float maxFall) => fallSpeedClamp = maxFall;

        /// <summary>
        /// Pede um impulso horizontal (ex.: avanço de golpe): a velocidade é mantida por <paramref name="duration"/>
        /// segundos e depois o movimento normal volta. Ignorado durante dash, atordoamento ou sem controle.
        /// </summary>
        public void RequestHorizontalImpulse(float velocityX, float duration)
        {
            if (!controlEnabled || IsDashing || IsStunned) return;
            if (duration <= 0f || Mathf.Approximately(velocityX, 0f)) return;
            impulseVelocityX = velocityX;
            impulseTimeLeft = duration;
            impulseEndPending = false;
        }

        /// <summary>Cancela o impulso em andamento; a velocidade residual é cortada no próximo passo de física.</summary>
        public void ClearHorizontalImpulse()
        {
            if (impulseTimeLeft > 0f) impulseEndPending = true;
            impulseTimeLeft = 0f;
        }

        /// <summary>Descarta o impulso sem tocar na velocidade (quando dash, recuo ou teleporte assumem o movimento).</summary>
        private void DropHorizontalImpulse()
        {
            impulseTimeLeft = 0f;
            impulseEndPending = false;
        }

        public void FaceDirection(int direction)
        {
            if (!facingLocked) ApplyFacing(direction);
        }

        public void ApplyKnockback(Vector2 velocity, float stunDuration)
        {
            if (IsDashing) EndDash(rb.linearVelocity);
            isRisingFromJump = false;
            DropHorizontalImpulse();
            rb.linearVelocity = velocity;
            stunTimeLeft = Mathf.Max(stunTimeLeft, stunDuration);
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value)
            {
                if (IsDashing) EndDash(rb.linearVelocity);
                DropHorizontalImpulse();
                rb.linearVelocity = Vector2.zero;
                lastJumpPressedTime = float.NegativeInfinity;
            }
        }

        /// <summary>Reposiciona o protagonista e limpa estados (usado no retorno).</summary>
        public void TeleportTo(Vector2 position)
        {
            dashTimeLeft = 0f;
            stunTimeLeft = 0f;
            rb.gravityScale = baseGravityScale;
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
            transform.position = position;
            dashCharges = maxDashCharges;
            horizontalControlMultiplier = 1f;
            jumpBlocked = false;
            facingLocked = false;
            fallSpeedClamp = -1f;
            DropHorizontalImpulse();
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (col == null) return;
            Bounds b = col.bounds;
            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - groundCheckDepth * 0.5f),
                new Vector3(b.size.x * groundCheckWidthFactor, groundCheckDepth, 0f));
        }
    }
}
