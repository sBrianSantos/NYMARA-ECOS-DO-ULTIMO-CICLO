using UnityEngine;

namespace Nymara.Core
{
    /// <summary>
    /// Dados de um impacto entregue por um AttackHitbox a um StabilityComponent.
    /// Em NYMARA o dano reduz "estabilidade" (vínculo com o Intervalo), não vida.
    /// </summary>
    public struct HitInfo
    {
        /// <summary>Quanto de estabilidade o impacto remove.</summary>
        public float Damage;

        /// <summary>Velocidade de recuo aplicada ao alvo (unidades/s).</summary>
        public float KnockbackSpeed;

        /// <summary>Componente vertical do recuo (unidades/s).</summary>
        public float KnockbackLift;

        /// <summary>Tempo em que o alvo fica atordoado/interrompido (s).</summary>
        public float StaggerTime;

        /// <summary>Direção horizontal do golpe: +1 direita, -1 esquerda.</summary>
        public int Direction;

        /// <summary>Quem causou o impacto (pode ser nulo).</summary>
        public GameObject Source;

        /// <summary>Golpe pesado (ex.: ataque carregado) — usado para feedback.</summary>
        public bool IsHeavy;
    }
}
