using UnityEngine;

namespace Nymara.Visual
{
    /// <summary>
    /// Efeito visual simples baseado em sprite (sem sistema de partículas): aparece,
    /// muda de escala, desloca-se e some. Só visual; não interage com física nem gameplay.
    /// </summary>
    public class SimpleFx : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color color;
        private float life, age;
        private Vector3 scale0, scale1;
        private Vector2 velocity;
        private float gravity, spin;
        private bool fadeIn;

        public static SimpleFx Spawn(Sprite sprite, Vector3 position, Color color, float life,
                                     Vector3 scaleFrom, Vector3 scaleTo, int sortingOrder,
                                     Material material = null, Transform parent = null, float rotation = 0f,
                                     Vector2 velocity = default, float gravity = 0f, float spin = 0f, bool fadeIn = false)
        {
            if (sprite == null) return null;
            var go = new GameObject("FX_" + sprite.name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var fx = go.AddComponent<SimpleFx>();
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sprite = sprite;
            fx.sr.sortingOrder = sortingOrder;
            if (material != null) fx.sr.sharedMaterial = material;
            fx.color = color;
            fx.life = Mathf.Max(0.01f, life);
            fx.scale0 = scaleFrom;
            fx.scale1 = scaleTo;
            fx.velocity = velocity;
            fx.gravity = gravity;
            fx.spin = spin;
            fx.fadeIn = fadeIn;
            fx.Apply(0f);
            return fx;
        }

        public SpriteRenderer Renderer => sr;

        private void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / life);
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
            Apply(k);
            if (age >= life) Destroy(gameObject);
        }

        private void Apply(float k)
        {
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.LerpUnclamped(scale0, scale1, e);
            Color c = color;
            float a = fadeIn ? Mathf.Sin(k * Mathf.PI) : (1f - k * k);
            c.a *= a;
            if (sr != null) sr.color = c;
        }
    }
}
