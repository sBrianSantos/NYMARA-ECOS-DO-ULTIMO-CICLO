using System;
using System.Collections;
using Nymara.Core;
using UnityEngine;

namespace Nymara.Player
{
    public enum CombatState
    {
        Idle,
        Charging,
        Startup,
        Active,
        Recovery
    }

    public enum AttackKind
    {
        None,
        Basic,
        Charged,
        Aerial
    }

    /// <summary>Configuração de um golpe do Fio do Limiar (valores de protótipo).</summary>
    [Serializable]
    public class AttackDefinition
    {
        public string name = "Golpe";
        [Min(0f)] public float damage = 1f;
        [Min(0f)] public float startup = 0.05f;
        [Min(0.01f)] public float active = 0.1f;
        [Min(0f)] public float recovery = 0.15f;
        [Tooltip("Centro da hitbox, em espaço local (x positivo = para frente).")]
        public Vector2 hitboxOffset = new Vector2(0.9f, 0.1f);
        public Vector2 hitboxSize = new Vector2(1.3f, 1f);
        [Tooltip("Velocidade de avanço aplicada no início do golpe (0 = nenhum).")]
        public float lungeSpeed = 2.5f;
        [Tooltip("Duração do avanço, a partir do início do golpe (s). 0 = sem avanço.")]
        [Min(0f)] public float lungeDuration = 0.08f;
        [Min(0f)] public float knockbackSpeed = 3f;
        [Min(0f)] public float knockbackLift = 1.5f;
        [Min(0f)] public float staggerTime = 0.25f;
        [Tooltip("Pausa curta ao acertar (hit stop), em segundos reais. 0 = desligado.")]
        [Min(0f)] public float hitStop = 0.04f;
        public Color debugColor = new Color(1f, 0.85f, 0.3f, 0.45f);
    }

    /// <summary>
    /// Combate do protagonista: combo básico, ataque carregado e ataque aéreo.
    ///
    /// Regras de input (documentadas para o teste):
    /// - No chão, o ataque é decidido ao SOLTAR o botão: segurou menos que chargeThreshold → próximo golpe do combo;
    ///   segurou o suficiente → ataque carregado. Assim um único pressionar nunca gera dois ataques.
    /// - No ar, o ataque aéreo sai ao PRESSIONAR (sem carga), limitado por maxAerialAttacksPerAirtime até tocar o chão.
    ///   É um mergulho vertical: o controlador desce em linha reta e a hitbox abaixo do corpo fica ativa durante o mergulho.
    /// - Dash cancela qualquer ataque/carga. Sofrer impacto também cancela.
    /// - Pular cancela a carga; durante startup/ativo de golpe no chão o pulo fica bloqueado, na recuperação é liberado.
    /// - Soltar o botão durante um golpe fica guardado (buffer) e executa o próximo golpe quando o atual termina.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private StabilityComponent stability;
        [Tooltip("Hitbox usada pelo combo e pelo ataque carregado.")]
        [SerializeField] private AttackHitbox groundHitbox;
        [Tooltip("Hitbox própria do ataque aéreo.")]
        [SerializeField] private AttackHitbox aerialHitbox;

        [Header("Combo básico (hipótese: 3 golpes)")]
        [SerializeField] private AttackDefinition[] comboAttacks =
        {
            new AttackDefinition { name = "Golpe 1", damage = 1f, startup = 0.05f, active = 0.10f, recovery = 0.15f, hitboxOffset = new Vector2(0.85f, 0.1f), hitboxSize = new Vector2(1.3f, 1.0f), lungeSpeed = 2.5f, lungeDuration = 0.08f, knockbackSpeed = 3f, staggerTime = 0.25f },
            new AttackDefinition { name = "Golpe 2", damage = 1f, startup = 0.05f, active = 0.10f, recovery = 0.17f, hitboxOffset = new Vector2(0.9f, 0.1f), hitboxSize = new Vector2(1.4f, 1.1f), lungeSpeed = 3f, lungeDuration = 0.08f, knockbackSpeed = 3.5f, staggerTime = 0.25f },
            new AttackDefinition { name = "Golpe 3", damage = 1.5f, startup = 0.08f, active = 0.12f, recovery = 0.30f, hitboxOffset = new Vector2(1.0f, 0.1f), hitboxSize = new Vector2(1.7f, 1.2f), lungeSpeed = 4.5f, lungeDuration = 0.1f, knockbackSpeed = 6f, knockbackLift = 2.5f, staggerTime = 0.4f, hitStop = 0.06f, debugColor = new Color(1f, 0.6f, 0.2f, 0.5f) },
        };
        [Tooltip("Tempo após o fim de um golpe em que o próximo toque continua o combo (s).")]
        [SerializeField, Min(0f)] private float comboWindow = 0.35f;
        [Tooltip("Antecedência com que um toque durante o golpe atual é guardado (s).")]
        [SerializeField, Min(0f)] private float inputBufferTime = 0.2f;

        [Header("Ataque carregado")]
        [Tooltip("Tempo mínimo segurando o botão para executar o ataque carregado (s).")]
        [SerializeField, Min(0.05f)] private float chargeThreshold = 0.5f;
        [Tooltip("Atraso antes de exibir feedback de carga, para toques rápidos não piscarem (s).")]
        [SerializeField, Min(0f)] private float chargeFeedbackDelay = 0.1f;
        [Tooltip("Controle horizontal enquanto carrega (0 = parado).")]
        [SerializeField, Range(0f, 1f)] private float chargeMoveMultiplier = 0.25f;
        [SerializeField] private AttackDefinition chargedAttack = new AttackDefinition
        {
            name = "Carregado", damage = 3f, startup = 0.1f, active = 0.14f, recovery = 0.35f,
            hitboxOffset = new Vector2(1.1f, 0.1f), hitboxSize = new Vector2(2.2f, 1.4f), lungeSpeed = 7f, lungeDuration = 0.12f,
            knockbackSpeed = 9f, knockbackLift = 3f, staggerTime = 0.6f, hitStop = 0.09f, debugColor = new Color(1f, 0.35f, 0.15f, 0.55f)
        };

        [Header("Ataque aéreo (mergulho vertical)")]
        [Tooltip("A hitbox fica ativa no mínimo por 'active' e, além disso, enquanto o mergulho durar.")]
        [SerializeField] private AttackDefinition aerialAttack = new AttackDefinition
        {
            name = "Aéreo", damage = 1.5f, startup = 0.04f, active = 0.14f, recovery = 0.14f,
            hitboxOffset = new Vector2(0f, -1.0f), hitboxSize = new Vector2(1.1f, 1.1f), lungeSpeed = 0f, lungeDuration = 0f,
            knockbackSpeed = 4f, knockbackLift = 0f, staggerTime = 0.3f, hitStop = 0.04f, debugColor = new Color(0.4f, 0.8f, 1f, 0.5f)
        };
        [SerializeField, Min(1)] private int maxAerialAttacksPerAirtime = 1;
        [Tooltip("Velocidade da descida do mergulho (u/s). Deve ficar abaixo de maxFallSpeed do controlador.")]
        [SerializeField, Min(0.1f)] private float aerialDiveSpeed = 16f;
        [Tooltip("Duração máxima do mergulho (s); termina antes ao tocar o chão.")]
        [SerializeField, Min(0.01f)] private float aerialDiveMaxDuration = 0.4f;
        [Tooltip("Controle horizontal na recuperação do ataque aéreo (durante o mergulho não há deslocamento horizontal).")]
        [SerializeField, Range(0f, 1f)] private float aerialMoveMultiplier = 0.7f;

        private CombatState state = CombatState.Idle;
        private AttackKind currentKind = AttackKind.None;
        private AttackDefinition currentAttack;
        private AttackHitbox currentHitbox;
        private float phaseTimeLeft;

        private int comboIndex;
        private float comboExpireTime = float.NegativeInfinity;
        private bool holding;
        private float holdStartTime;
        private float bufferedReleaseTime = float.NegativeInfinity;
        private int aerialAttacksUsed;
        private Coroutine hitStopRoutine;

        public CombatState State => state;
        public AttackKind CurrentKind => currentKind;
        public int ComboStep => state == CombatState.Idle || state == CombatState.Charging ? 0 : (currentKind == AttackKind.Basic ? comboIndex + 1 : 0);
        public bool IsAttacking => state == CombatState.Startup || state == CombatState.Active || state == CombatState.Recovery;
        public bool IsCharging => state == CombatState.Charging && holding;
        public float HeldTime => holding ? Time.time - holdStartTime : 0f;
        /// <summary>0..1 durante a carga (0 enquanto o atraso de feedback não passou).</summary>
        public float ChargeProgress => IsCharging && HeldTime >= chargeFeedbackDelay ? Mathf.Clamp01(HeldTime / chargeThreshold) : 0f;
        public bool IsFullyCharged => IsCharging && HeldTime >= chargeThreshold;

        public event Action<AttackKind, int> AttackStarted;
        public event Action<StabilityComponent, HitInfo> TargetHit;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (stability == null) stability = GetComponent<StabilityComponent>();
            if (input == null || controller == null)
            {
                Debug.LogError($"[{nameof(PlayerCombat)}] PlayerInputReader/PlayerController2D ausente.", this);
                enabled = false;
                return;
            }
            if (groundHitbox == null || aerialHitbox == null)
                Debug.LogError($"[{nameof(PlayerCombat)}] Hitboxes não atribuídas.", this);
            if (comboAttacks == null || comboAttacks.Length == 0)
                Debug.LogError($"[{nameof(PlayerCombat)}] Combo sem golpes configurados.", this);
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += OnAttackPressed;
                input.AttackReleased += OnAttackReleased;
            }
            if (controller != null)
            {
                controller.DashStarted += CancelAll;
                controller.Jumped += OnJumped;
                controller.Landed += OnLanded;
            }
            if (stability != null) stability.Damaged += OnDamaged;
            if (groundHitbox != null) groundHitbox.TargetHit += OnHitboxHit;
            if (aerialHitbox != null) aerialHitbox.TargetHit += OnHitboxHit;
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= OnAttackPressed;
                input.AttackReleased -= OnAttackReleased;
            }
            if (controller != null)
            {
                controller.DashStarted -= CancelAll;
                controller.Jumped -= OnJumped;
                controller.Landed -= OnLanded;
            }
            if (stability != null) stability.Damaged -= OnDamaged;
            if (groundHitbox != null) groundHitbox.TargetHit -= OnHitboxHit;
            if (aerialHitbox != null) aerialHitbox.TargetHit -= OnHitboxHit;

            CancelAll();
            StopHitStop();
        }

        // ------------------------------------------------------------------ input

        private bool CanAct => controller.ControlEnabled && !controller.IsDashing && !controller.IsStunned;

        private void OnAttackPressed()
        {
            if (!CanAct) return;

            if (!controller.IsGrounded)
            {
                if (!IsAttacking && aerialAttacksUsed < maxAerialAttacksPerAirtime)
                    StartAttack(aerialAttack, AttackKind.Aerial, aerialHitbox);
                return;
            }

            holding = true;
            holdStartTime = Time.time;
            if (state == CombatState.Idle)
                EnterCharging();
        }

        private void OnAttackReleased()
        {
            if (!holding) return;
            float held = Time.time - holdStartTime;
            holding = false;

            if (state == CombatState.Charging)
            {
                if (!CanAct || !controller.IsGrounded) { ReturnToIdle(); return; }
                if (held >= chargeThreshold) StartAttack(chargedAttack, AttackKind.Charged, groundHitbox);
                else StartNextBasic();
            }
            else if (IsAttacking)
            {
                bufferedReleaseTime = Time.time;
            }
        }

        private void OnJumped()
        {
            if (state == CombatState.Charging)
            {
                holding = false;
                ReturnToIdle();
            }
            else if (state == CombatState.Recovery && currentKind != AttackKind.Aerial)
            {
                // Pulo liberado na recuperação: cancela o resto do golpe.
                ReturnToIdle();
            }
        }

        private void OnLanded()
        {
            aerialAttacksUsed = 0;
        }

        private void OnDamaged(HitInfo hit)
        {
            holding = false;
            CancelAll();
        }

        private void OnHitboxHit(StabilityComponent target, HitInfo hit)
        {
            TargetHit?.Invoke(target, hit);
            if (currentAttack != null && currentAttack.hitStop > 0f)
            {
                StopHitStop();
                hitStopRoutine = StartCoroutine(HitStop(currentAttack.hitStop));
            }
        }

        // ------------------------------------------------------------------ máquina de estados

        private void Update()
        {
            if (state == CombatState.Charging)
            {
                // Carga só existe no chão e com controle.
                if (!CanAct || !controller.IsGrounded)
                {
                    holding = false;
                    ReturnToIdle();
                }
                return;
            }

            if (!IsAttacking) return;

            phaseTimeLeft -= Time.deltaTime;
            if (phaseTimeLeft > 0f) return;

            switch (state)
            {
                case CombatState.Startup:
                    state = CombatState.Active;
                    phaseTimeLeft = currentAttack.active;
                    ActivateHitbox();
                    break;

                case CombatState.Active:
                    // Mergulho: a hitbox continua ativa até o mergulho terminar (chão ou tempo máximo).
                    if (currentKind == AttackKind.Aerial && controller.IsDiving) return;
                    state = CombatState.Recovery;
                    phaseTimeLeft = currentAttack.recovery;
                    if (currentHitbox != null) currentHitbox.Deactivate();
                    controller.SetJumpBlocked(false);
                    controller.SetFallSpeedClamp(-1f);
                    break;

                case CombatState.Recovery:
                    FinishAttack();
                    break;
            }
        }

        private void EnterCharging()
        {
            state = CombatState.Charging;
            currentKind = AttackKind.None;
            controller.SetHorizontalControl(chargeMoveMultiplier);
        }

        private void StartNextBasic()
        {
            if (comboAttacks == null || comboAttacks.Length == 0) { ReturnToIdle(); return; }
            if (Time.time > comboExpireTime) comboIndex = 0;
            comboIndex = Mathf.Clamp(comboIndex, 0, comboAttacks.Length - 1);
            StartAttack(comboAttacks[comboIndex], AttackKind.Basic, groundHitbox);
        }

        private void StartAttack(AttackDefinition attack, AttackKind kind, AttackHitbox hitbox)
        {
            if (attack == null || hitbox == null) { ReturnToIdle(); return; }

            if (currentHitbox != null) currentHitbox.Deactivate();
            currentAttack = attack;
            currentKind = kind;
            currentHitbox = hitbox;
            state = CombatState.Startup;
            phaseTimeLeft = attack.startup;
            bufferedReleaseTime = float.NegativeInfinity;

            // Golpe para o lado que o jogador está apontando no momento do ataque.
            float x = input.MoveX;
            if (Mathf.Abs(x) > 0.01f) controller.FaceDirection(x > 0f ? 1 : -1);
            controller.SetFacingLocked(true);

            if (kind == AttackKind.Aerial)
            {
                aerialAttacksUsed++;
                controller.ClearHorizontalImpulse();
                controller.SetHorizontalControl(aerialMoveMultiplier);
                controller.RequestVerticalDive(aerialDiveSpeed, aerialDiveMaxDuration);
            }
            else
            {
                controller.SetHorizontalControl(0f);
                controller.SetJumpBlocked(true);
                // Avanço do golpe: pedido ao controlador (único dono da velocidade), na direção atual.
                controller.ClearHorizontalImpulse();
                if (Mathf.Abs(attack.lungeSpeed) > 0f && attack.lungeDuration > 0f)
                    controller.RequestHorizontalImpulse(controller.Facing * attack.lungeSpeed, attack.lungeDuration);
            }

            AttackStarted?.Invoke(kind, kind == AttackKind.Basic ? comboIndex + 1 : 0);

            if (phaseTimeLeft <= 0f)
            {
                state = CombatState.Active;
                phaseTimeLeft = attack.active;
                ActivateHitbox();
            }
        }

        private void ActivateHitbox()
        {
            var hit = new HitInfo
            {
                Damage = currentAttack.damage,
                KnockbackSpeed = currentAttack.knockbackSpeed,
                KnockbackLift = currentAttack.knockbackLift,
                StaggerTime = currentAttack.staggerTime,
                Direction = controller.Facing,
                Source = gameObject,
                IsHeavy = currentKind == AttackKind.Charged
            };
            currentHitbox.Activate(currentAttack.hitboxOffset, currentAttack.hitboxSize, hit, currentAttack.debugColor);
        }

        private void FinishAttack()
        {
            AttackKind finished = currentKind;
            if (finished == AttackKind.Basic)
            {
                comboIndex++;
                if (comboIndex >= comboAttacks.Length) comboIndex = 0; // fim da sequência: recomeça
                comboExpireTime = Time.time + comboWindow;
            }
            else if (finished == AttackKind.Charged)
            {
                comboIndex = 0;
            }

            ReturnToIdle();

            bool hasBuffer = Time.time - bufferedReleaseTime <= inputBufferTime;
            bufferedReleaseTime = float.NegativeInfinity;

            if (hasBuffer && CanAct && controller.IsGrounded)
            {
                StartNextBasic();
            }
            else if (holding && CanAct && controller.IsGrounded)
            {
                // Botão continua pressionado: a carga começa a contar a partir de agora.
                holdStartTime = Time.time;
                EnterCharging();
            }
        }

        private void ReturnToIdle()
        {
            if (currentHitbox != null) currentHitbox.Deactivate();
            state = CombatState.Idle;
            currentKind = AttackKind.None;
            currentAttack = null;
            currentHitbox = null;
            phaseTimeLeft = 0f;
            if (controller != null)
            {
                controller.ClearHorizontalImpulse();
                controller.ClearVerticalDive();
                controller.SetHorizontalControl(1f);
                controller.SetJumpBlocked(false);
                controller.SetFacingLocked(false);
                controller.SetFallSpeedClamp(-1f);
            }
        }

        /// <summary>Cancela golpe/carga em andamento (dash, dano, retorno).</summary>
        public void CancelAll()
        {
            if (state != CombatState.Idle && state != CombatState.Charging)
                comboIndex = 0;
            if (state == CombatState.Charging) holding = false;
            bufferedReleaseTime = float.NegativeInfinity;
            ReturnToIdle();
        }

        /// <summary>Reinicia completamente o estado do combate.</summary>
        public void ResetCombat()
        {
            holding = false;
            comboIndex = 0;
            comboExpireTime = float.NegativeInfinity;
            aerialAttacksUsed = 0;
            CancelAll();
            StopHitStop();
        }

        // ------------------------------------------------------------------ hit stop

        private IEnumerator HitStop(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        private void StopHitStop()
        {
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
            }
            Time.timeScale = 1f;
        }
    }
}
