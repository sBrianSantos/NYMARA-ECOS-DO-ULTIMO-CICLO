using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nymara.Player
{
    /// <summary>
    /// Lê as ações do Input System e as expõe como valores/eventos.
    /// Não contém regras de movimento nem de combate.
    /// Os bindings ficam no asset NymaraPrototype.inputactions (remapeáveis sem tocar no código).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string dashActionName = "Dash";
        [SerializeField] private string attackActionName = "Attack";

        [Tooltip("Valores de eixo abaixo disso são tratados como zero.")]
        [SerializeField, Range(0f, 0.9f)] private float moveDeadzone = 0.2f;

        private InputActionMap map;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private InputAction attackAction;
        private bool gameplayInputEnabled = true;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackHeld { get; private set; }
        /// <summary>Contador de pulos pressionados (diagnóstico no HUD).</summary>
        public int JumpPressCount { get; private set; }

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action DashPressed;
        public event Action AttackPressed;
        public event Action AttackReleased;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] InputActionAsset não atribuído.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(actionMapName);
            if (map == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] Action map '{actionMapName}' não encontrado.", this);
                enabled = false;
                return;
            }

            moveAction = FindRequired(moveActionName);
            jumpAction = FindRequired(jumpActionName);
            dashAction = FindRequired(dashActionName);
            attackAction = FindRequired(attackActionName);
            if (moveAction == null || jumpAction == null || dashAction == null || attackAction == null)
                enabled = false;
        }

        private InputAction FindRequired(string actionName)
        {
            InputAction action = map.FindAction(actionName);
            if (action == null)
                Debug.LogError($"[{nameof(PlayerInputReader)}] Ação '{actionMapName}/{actionName}' não encontrada.", this);
            return action;
        }

        private void OnEnable()
        {
            map?.Enable();
        }

        private void OnDisable()
        {
            map?.Disable();
            ClearState();
        }

        /// <summary>Liga/desliga a leitura de gameplay (ex.: durante o retorno do protagonista).</summary>
        public void SetGameplayInputEnabled(bool value)
        {
            gameplayInputEnabled = value;
            if (!value) ClearState();
        }

        private void ClearState()
        {
            MoveX = 0f;
            JumpHeld = false;
            AttackHeld = false;
        }

        private void Update()
        {
            if (!gameplayInputEnabled)
                return;

            float x = moveAction.ReadValue<float>();
            MoveX = Mathf.Abs(x) < moveDeadzone ? 0f : Mathf.Clamp(x, -1f, 1f);

            JumpHeld = jumpAction.IsPressed();
            AttackHeld = attackAction.IsPressed();

            if (jumpAction.WasPressedThisFrame())
            {
                JumpPressCount++;
                JumpPressed?.Invoke();
            }
            if (jumpAction.WasReleasedThisFrame()) JumpReleased?.Invoke();
            if (dashAction.WasPressedThisFrame()) DashPressed?.Invoke();
            if (attackAction.WasPressedThisFrame()) AttackPressed?.Invoke();
            if (attackAction.WasReleasedThisFrame()) AttackReleased?.Invoke();
        }
    }
}
