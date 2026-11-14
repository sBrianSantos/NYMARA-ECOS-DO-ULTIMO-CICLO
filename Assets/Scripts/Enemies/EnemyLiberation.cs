using System.Collections;
using UnityEngine;

namespace Nymara.Enemies
{
    /// <summary>
    /// Estado final de LIBERTAÇÃO do Eco Aprisionado.
    /// Linguagem visual de limpeza, não de morte: clarão suave, a cor sombria do
    /// Intervalo dá lugar a uma luz clara, a forma se eleva serenamente,
    /// fragmentos de luz sobem e a presença se dissolve.
    /// Sem sangue, quedas, desmembramento ou texto de morte.
    /// </summary>
    public class EnemyLiberation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Sprite usado para os fragmentos de luz (pode ser o mesmo quadrado graybox).")]
        [SerializeField] private Sprite moteSprite;
        [Tooltip("Colliders desligados ao libertar (corpo físico).")]
        [SerializeField] private Collider2D[] collidersToDisable;
        [Tooltip("Elementos que representam a influência do Intervalo e somem ao libertar (ex.: o olho).")]
        [SerializeField] private GameObject[] hideOnLiberation;

        [Header("Aparência")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color liberatedColor = new Color(1f, 0.95f, 0.75f);
        [SerializeField, Min(0f)] private float flashDuration = 0.12f;
        [SerializeField, Min(0.1f)] private float riseDuration = 1.6f;
        [SerializeField] private float riseHeight = 1.2f;
        [SerializeField, Min(0)] private int moteCount = 8;
        [SerializeField, Min(0f)] private float moteSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float moteLifetime = 1.2f;
        [SerializeField, Min(0.01f)] private float moteSize = 0.15f;
        [Tooltip("Desativa o GameObject ao fim da sequência (false = permanece em pose pacífica).")]
        [SerializeField] private bool disableWhenFinished = true;

        public bool IsPlaying { get; private set; }
        public bool HasFinished { get; private set; }

        public void Play()
        {
            if (IsPlaying || HasFinished) return;
            StartCoroutine(LiberationRoutine());
        }

        private IEnumerator LiberationRoutine()
        {
            IsPlaying = true;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            if (collidersToDisable != null)
                foreach (var col in collidersToDisable)
                    if (col != null) col.enabled = false;

            if (hideOnLiberation != null)
                foreach (var go in hideOnLiberation)
                    if (go != null) go.SetActive(false);

            if (body != null) body.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            SpawnMotes();

            Vector3 start = transform.position;
            Vector3 baseScale = body != null ? body.transform.localScale : Vector3.one;
            float t = 0f;
            while (t < riseDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / riseDuration);
                float ease = 1f - (1f - k) * (1f - k);
                transform.position = start + Vector3.up * (riseHeight * ease);
                if (body != null)
                {
                    Color c = liberatedColor;
                    c.a = 1f - k;
                    body.color = c;
                    float breathe = 1f + 0.08f * Mathf.Sin(k * Mathf.PI * 3f);
                    body.transform.localScale = baseScale * breathe;
                }
                yield return null;
            }

            IsPlaying = false;
            HasFinished = true;
            if (disableWhenFinished) gameObject.SetActive(false);
        }

        private void SpawnMotes()
        {
            if (moteSprite == null || moteCount <= 0) return;
            for (int i = 0; i < moteCount; i++)
            {
                var go = new GameObject("LiberationMote");
                go.transform.position = transform.position + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.5f), 0f);
                go.transform.localScale = Vector3.one * moteSize;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = moteSprite;
                sr.color = liberatedColor;
                sr.sortingOrder = body != null ? body.sortingOrder + 1 : 20;
                var mote = go.AddComponent<LiberationMote>();
                Vector2 dir = new Vector2(Random.Range(-0.5f, 0.5f), 1f).normalized;
                mote.Init(dir * moteSpeed * Random.Range(0.6f, 1.2f), moteLifetime * Random.Range(0.7f, 1.1f));
            }
        }
    }
}
