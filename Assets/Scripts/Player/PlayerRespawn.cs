using System.Collections;
using Nymara.Core;
using Nymara.Enemies;
using Nymara.UI;
using UnityEngine;

namespace Nymara.Player
{
    /// <summary>
    /// Versão mínima do retorno do Portador do Pacto: quando a estabilidade chega a zero
    /// (ou ele cai para fora da área de teste), sua forma é "absorvida" pela ruptura,
    /// a tela escurece e ele é reconstruído no ponto de teste com estabilidade cheia.
    /// Não há morte nem perda de progresso. Ecos ainda aprisionados voltam ao estado inicial;
    /// Ecos já libertados permanecem libertados.
    /// </summary>
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private StabilityComponent stability;
        [Tooltip("Ponto de teste / Marco de Passagem provisório.")]
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private ScreenFade screenFade;
        [Tooltip("Transform visual que encolhe durante a absorção.")]
        [SerializeField] private Transform visualRoot;

        [Header("Tempos")]
        [SerializeField, Min(0f)] private float absorbDuration = 0.6f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float holdDarkDuration = 0.2f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.4f;
        [SerializeField, Min(0f)] private float invulnerabilityAfterReturn = 1f;

        [Header("Segurança")]
        [Tooltip("Abaixo desta altura o protagonista retorna ao ponto de teste.")]
        [SerializeField] private float fallLimitY = -15f;
        [SerializeField] private bool resetCapturedEnemiesOnReturn = true;

        private Vector3 visualBaseScale = Vector3.one;

        public bool IsRespawning { get; private set; }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (stability == null) stability = GetComponent<StabilityComponent>();
            if (visualRoot != null) visualBaseScale = visualRoot.localScale;
            if (respawnPoint == null)
                Debug.LogWarning($"[{nameof(PlayerRespawn)}] Sem ponto de retorno: será usada a posição inicial.", this);
        }

        private Vector3 initialPosition;

        private void Start()
        {
            initialPosition = transform.position;
        }

        private void OnEnable()
        {
            if (stability != null) stability.Depleted += OnDepleted;
        }

        private void OnDisable()
        {
            if (stability != null) stability.Depleted -= OnDepleted;
        }

        private void Update()
        {
            if (!IsRespawning && transform.position.y < fallLimitY)
                BeginReturn(false);
        }

        private void OnDepleted() => BeginReturn(true);

        public void BeginReturn(bool absorbed)
        {
            if (IsRespawning) return;
            StartCoroutine(ReturnRoutine(absorbed));
        }

        private IEnumerator ReturnRoutine(bool absorbed)
        {
            IsRespawning = true;
            if (stability != null) stability.SetForcedInvulnerable(true);
            if (input != null) input.SetGameplayInputEnabled(false);
            if (combat != null) combat.ResetCombat();
            if (controller != null) controller.SetControlEnabled(false);

            // Absorção pela ruptura: a forma se contrai (sem linguagem de morte).
            if (absorbed && visualRoot != null)
            {
                float t = 0f;
                while (t < absorbDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / absorbDuration);
                    float squash = Mathf.SmoothStep(1f, 0.1f, k);
                    visualRoot.localScale = new Vector3(visualBaseScale.x * Mathf.Lerp(1f, 0.6f, k), visualBaseScale.y * squash, visualBaseScale.z);
                    yield return null;
                }
            }

            if (screenFade != null) yield return screenFade.FadeTo(1f, fadeOutDuration);
            if (holdDarkDuration > 0f) yield return new WaitForSeconds(holdDarkDuration);

            // Reconstrução no ponto de teste.
            Vector3 target = respawnPoint != null ? respawnPoint.position : initialPosition;
            if (controller != null) controller.TeleportTo(target);
            else transform.position = target;
            if (visualRoot != null) visualRoot.localScale = visualBaseScale;
            if (stability != null)
            {
                stability.RestoreFull();
                stability.SetForcedInvulnerable(false);
                stability.GrantInvulnerability(invulnerabilityAfterReturn);
            }

            if (resetCapturedEnemiesOnReturn)
            {
                foreach (var enemy in FindObjectsByType<TestEnemyController>(FindObjectsSortMode.None))
                    enemy.ResetIfStillCaptured();
            }

            if (controller != null) controller.SetControlEnabled(true);
            if (input != null) input.SetGameplayInputEnabled(true);
            IsRespawning = false;

            if (screenFade != null) yield return screenFade.FadeTo(0f, fadeInDuration);
        }
    }
}
