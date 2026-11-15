using Nymara.Core;
using Nymara.Enemies;
using UnityEngine;

namespace Nymara.Visual
{
    /// <summary>
    /// Visual provisório do Eco Aprisionado: troca de quadros por estado, marcas do Intervalo
    /// (brilho) que se intensificam no aviso do golpe, fragmentos flutuantes, clarão de impacto,
    /// arco do golpe e explosão de luz ao ser libertado.
    /// Apenas LÊ TestEnemyController / StabilityComponent; IA, vida e dano não são tocados.
    /// A cor do corpo continua sendo definida pelo TestEnemyController/EnemyLiberation.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class EcoVisual : MonoBehaviour
    {
        [SerializeField] private TestEnemyController controller;
        [SerializeField] private StabilityComponent stability;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer glowLayer;
        [SerializeField] private SpriteRenderer flashLayer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform[] shards;
        [SerializeField] private Material fxMaterial;

        [Header("Quadros: corpo / brilho / clarão (mesma ordem)")]
        [SerializeField] private Sprite[] bodyFrames;
        [SerializeField] private Sprite[] glowFrames;
        [SerializeField] private Sprite[] flashFrames;
        [Tooltip("Índices em bodyFrames: 0-1 parado, 2-3 andando, 4 aviso, 5 golpe, 6 atordoado.")]
        [SerializeField] private float idleFrameTime = 0.5f;
        [SerializeField] private float moveFrameTime = 0.16f;

        [Header("Efeitos")]
        [SerializeField] private Sprite slash;
        [SerializeField] private Sprite glow;
        [SerializeField] private Sprite ring;
        [SerializeField] private Color intervaloColor = new Color(0.9f, 0.55f, 1f, 1f);
        [SerializeField] private Color windupGlowColor = new Color(1f, 0.6f, 0.42f, 1f);
        [SerializeField] private Color liberationColor = new Color(1f, 0.95f, 0.78f, 1f);
        [SerializeField, Min(0.01f)] private float flashTime = 0.1f;
        [SerializeField] private float windupTimeForVisual = 0.45f;

        private const int Idle0 = 0, Move0 = 2, Windup = 4, Attack = 5, Stagger = 6;
        private EnemyState last;
        private float stateTime, animTime, flashTimer;
        private Vector3[] shardBase;
        private bool liberatedFx;

        private void Awake()
        {
            if (shards != null)
            {
                shardBase = new Vector3[shards.Length];
                for (int i = 0; i < shards.Length; i++) if (shards[i] != null) shardBase[i] = shards[i].localPosition;
            }
            if (flashLayer != null) flashLayer.color = new Color(1f, 1f, 1f, 0f);
        }

        private void OnEnable() { if (stability != null) stability.Damaged += OnDamaged; }
        private void OnDisable() { if (stability != null) stability.Damaged -= OnDamaged; }

        private void OnDamaged(HitInfo hit) => flashTimer = flashTime;

        private void SetFrame(int i)
        {
            if (bodyFrames == null || i >= bodyFrames.Length) return;
            if (body != null && bodyFrames[i] != null && controller.State != EnemyState.Liberated) body.sprite = bodyFrames[i];
            if (glowLayer != null && glowFrames != null && i < glowFrames.Length) glowLayer.sprite = glowFrames[i];
            if (flashLayer != null && flashFrames != null && i < flashFrames.Length) flashLayer.sprite = flashFrames[i];
        }

        private void LateUpdate()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            var st = controller.State;
            if (st != last)
            {
                OnStateEnter(st);
                last = st;
                stateTime = 0f;
            }
            stateTime += dt;
            animTime += dt;

            if (st == EnemyState.Liberated) { if (visualRoot != null) visualRoot.localRotation = Quaternion.identity; return; }

            int frame;
            switch (st)
            {
                case EnemyState.Windup: frame = Windup; break;
                case EnemyState.Attack: frame = Attack; break;
                case EnemyState.Staggered: frame = Stagger; break;
                case EnemyState.Recover: frame = Idle0 + 1; break;
                default:
                    float vx = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
                    frame = vx > 0.3f ? Move0 + ((int)(animTime / moveFrameTime) % 2) : Idle0 + ((int)(animTime / idleFrameTime) % 2);
                    break;
            }
            SetFrame(frame);

            // marcas do Intervalo: pulsam; no aviso esquentam e ficam mais fortes
            if (glowLayer != null)
            {
                Color c = intervaloColor;
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
                if (st == EnemyState.Windup)
                {
                    float k = Mathf.Clamp01(stateTime / Mathf.Max(0.05f, windupTimeForVisual));
                    c = Color.Lerp(intervaloColor, windupGlowColor, k);
                    pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 30f) + 0.3f * k;
                }
                else if (st == EnemyState.Attack) { c = windupGlowColor; pulse = 1.2f; }
                else if (st == EnemyState.Staggered) pulse = 0.45f;
                c.a = Mathf.Clamp01(c.a * pulse);
                glowLayer.color = c;
            }

            // clarão branco de impacto
            if (flashLayer != null)
            {
                flashTimer = Mathf.Max(0f, flashTimer - dt);
                flashLayer.color = new Color(1f, 1f, 1f, 0.85f * (flashTimer / flashTime));
            }

            // fragmentos flutuantes
            if (shards != null && shardBase != null)
            {
                float speed = st == EnemyState.Windup ? 9f : 2.2f;
                float amp = st == EnemyState.Windup ? 0.05f : 0.035f;
                for (int i = 0; i < shards.Length; i++)
                {
                    if (shards[i] == null) continue;
                    float t = Time.time * speed + i * 2.1f;
                    shards[i].localPosition = shardBase[i] + new Vector3(Mathf.Sin(t * 0.7f) * amp, Mathf.Sin(t) * amp * 1.6f, 0f);
                    shards[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * 25f);
                }
            }

            // atordoado: leve balanço (apenas o visual)
            if (visualRoot != null)
            {
                float z = st == EnemyState.Staggered ? Mathf.Sin(stateTime * 40f) * 6f * Mathf.Exp(-stateTime * 6f) : 0f;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, z);
            }
        }

        private void OnStateEnter(EnemyState st)
        {
            Transform t = transform;
            if (st == EnemyState.Attack && slash != null)
            {
                SimpleFx.Spawn(slash, t.TransformPoint(new Vector3(0.15f, 0.0f, 0f)), windupGlowColor, 0.16f,
                               new Vector3(0.8f, -0.75f, 1f), new Vector3(0.9f, -0.85f, 1f), 12, fxMaterial, t, -10f);
            }
            else if (st == EnemyState.Windup && glow != null)
            {
                SimpleFx.Spawn(glow, t.TransformPoint(new Vector3(0.45f, 0.35f, 0f)), windupGlowColor, windupTimeForVisual,
                               Vector3.one * 0.3f, Vector3.one * 0.9f, 13, fxMaterial, null, 0f, default, 0f, 0f, true);
            }
            else if (st == EnemyState.Liberated && !liberatedFx)
            {
                liberatedFx = true;
                Vector3 c = t.position + Vector3.up * 0.1f;
                SimpleFx.Spawn(ring, c, liberationColor, 0.6f, Vector3.one * 0.3f, Vector3.one * 1.7f, 14, fxMaterial);
                SimpleFx.Spawn(glow, c, new Color(1f, 0.95f, 0.8f, 0.8f), 0.7f, Vector3.one * 1.6f, Vector3.one * 3f, 13, fxMaterial);
                if (shards != null)
                    foreach (var s in shards)
                        if (s != null)
                        {
                            var sr = s.GetComponent<SpriteRenderer>();
                            Vector2 vel = ((Vector2)(s.position - t.position)).normalized * 1.5f + Vector2.up * 1.2f;
                            SimpleFx.Spawn(sr != null ? sr.sprite : glow, s.position, liberationColor, 0.8f, s.lossyScale, s.lossyScale * 0.2f, 12, fxMaterial, null, 0f, vel, 0f, 180f);
                        }
            }
        }
    }
}
