using System;
using UnityEngine;

namespace Nymara.Core
{
    /// <summary>
    /// Estabilidade: recurso de resistência de personagens e criaturas.
    /// Chegar a zero NÃO significa morte — cada dono decide a consequência
    /// (libertação do inimigo, absorção/retorno do protagonista).
    /// </summary>
    [DisallowMultipleComponent]
    public class StabilityComponent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxStability = 5f;

        [Tooltip("Tempo de invulnerabilidade após receber um impacto (s). 0 = nenhum.")]
        [SerializeField, Min(0f)] private float invulnerabilityAfterHit = 0f;

        [Tooltip("Rótulo exibido no HUD de debug.")]
        [SerializeField] private string displayName = "Estabilidade";

        private float current;
        private float invulnerableUntil;
        private bool forcedInvulnerable;

        public float Current => current;
        public float Max => maxStability;
        public float Normalized => maxStability > 0f ? current / maxStability : 0f;
        public bool IsDepleted { get; private set; }
        public bool IsInvulnerable => forcedInvulnerable || Time.time < invulnerableUntil;
        public string DisplayName => displayName;

        /// <summary>(atual, máximo)</summary>
        public event Action<float, float> Changed;
        public event Action<HitInfo> Damaged;
        public event Action Depleted;
        public event Action Restored;

        private void Awake()
        {
            current = maxStability;
        }

        /// <summary>Aplica um impacto. Retorna true se foi aceito.</summary>
        public bool ApplyHit(HitInfo hit)
        {
            if (IsDepleted || IsInvulnerable || !isActiveAndEnabled)
                return false;

            current = Mathf.Max(0f, current - Mathf.Max(0f, hit.Damage));
            if (invulnerabilityAfterHit > 0f)
                invulnerableUntil = Time.time + invulnerabilityAfterHit;

            Changed?.Invoke(current, maxStability);
            Damaged?.Invoke(hit);

            if (current <= 0f)
            {
                IsDepleted = true;
                Depleted?.Invoke();
            }
            return true;
        }

        /// <summary>Restaura a estabilidade ao máximo e limpa o estado esgotado.</summary>
        public void RestoreFull()
        {
            current = maxStability;
            IsDepleted = false;
            invulnerableUntil = 0f;
            Changed?.Invoke(current, maxStability);
            Restored?.Invoke();
        }

        /// <summary>Invulnerabilidade temporária (ex.: após retorno do protagonista).</summary>
        public void GrantInvulnerability(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        public void SetForcedInvulnerable(bool value)
        {
            forcedInvulnerable = value;
        }
    }
}
