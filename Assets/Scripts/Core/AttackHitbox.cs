using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nymara.Core
{
    /// <summary>
    /// Hitbox de ataque separada dos colliders físicos.
    /// Fica inativa até Activate(); enquanto ativa, consulta uma caixa a cada passo
    /// de física e entrega no máximo UM impacto por alvo por janela de ataque.
    /// A caixa é posicionada em espaço local do dono, então espelha junto com a
    /// escala X do personagem (direção para a qual ele está virado).
    /// </summary>
    public class AttackHitbox : MonoBehaviour
    {
        [Tooltip("Layers que esta hitbox pode atingir.")]
        [SerializeField] private LayerMask targetMask;

        [Tooltip("Estabilidade do próprio dono (ignorada pela hitbox).")]
        [SerializeField] private StabilityComponent ownerStability;

        [Header("Debug")]
        [Tooltip("Sprite opcional exibido enquanto a hitbox está ativa.")]
        [SerializeField] private SpriteRenderer debugVisual;
        [SerializeField] private bool showDebugVisual = true;

        private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
        private readonly HashSet<StabilityComponent> hitThisWindow = new HashSet<StabilityComponent>();
        private Vector2 localOffset;
        private Vector2 size;
        private HitInfo template;
        private ContactFilter2D filter;

        public bool IsActive { get; private set; }

        /// <summary>Disparado para cada alvo efetivamente atingido nesta janela.</summary>
        public event Action<StabilityComponent, HitInfo> TargetHit;

        private void Awake()
        {
            SetDebugVisible(false);
        }

        private void OnDisable()
        {
            Deactivate();
        }

        /// <summary>Abre uma nova janela de ataque.</summary>
        public void Activate(Vector2 offset, Vector2 boxSize, HitInfo hit, Color debugColor)
        {
            localOffset = offset;
            size = new Vector2(Mathf.Abs(boxSize.x), Mathf.Abs(boxSize.y));
            template = hit;
            hitThisWindow.Clear();
            filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = true };
            IsActive = true;

            if (debugVisual != null)
            {
                debugVisual.transform.localPosition = offset;
                debugVisual.transform.localScale = new Vector3(size.x, size.y, 1f);
                debugVisual.color = debugColor;
            }
            SetDebugVisible(showDebugVisual);

            // Checagem imediata: golpes curtos não dependem do próximo FixedUpdate.
            CheckOverlap();
        }

        public void Deactivate()
        {
            IsActive = false;
            hitThisWindow.Clear();
            SetDebugVisible(false);
        }

        private void FixedUpdate()
        {
            if (IsActive)
                CheckOverlap();
        }

        private void CheckOverlap()
        {
            Vector2 center = transform.TransformPoint(localOffset);
            Vector3 lossy = transform.lossyScale;
            Vector2 worldSize = new Vector2(size.x * Mathf.Abs(lossy.x), size.y * Mathf.Abs(lossy.y));

            overlapResults.Clear();
            int count = Physics2D.OverlapBox(center, worldSize, 0f, filter, overlapResults);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapResults[i];
                if (col == null) continue;

                StabilityComponent target = col.GetComponentInParent<StabilityComponent>();
                if (target == null || target == ownerStability || hitThisWindow.Contains(target))
                    continue;

                hitThisWindow.Add(target);

                HitInfo hit = template;
                hit.Direction = transform.lossyScale.x >= 0f ? 1 : -1;
                if (hit.Source == null) hit.Source = ownerStability != null ? ownerStability.gameObject : gameObject;

                if (target.ApplyHit(hit))
                    TargetHit?.Invoke(target, hit);
            }
        }

        private void SetDebugVisible(bool visible)
        {
            if (debugVisual != null)
                debugVisual.enabled = visible;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsActive ? new Color(1f, 0.3f, 0.2f, 0.8f) : new Color(1f, 1f, 0f, 0.4f);
            Vector2 center = transform.TransformPoint(localOffset);
            Vector3 lossy = transform.lossyScale;
            Gizmos.DrawWireCube(center, new Vector3(size.x * Mathf.Abs(lossy.x), size.y * Mathf.Abs(lossy.y), 0f));
        }
    }
}
