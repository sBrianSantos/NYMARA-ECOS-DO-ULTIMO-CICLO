using UnityEngine;

namespace Nymara.Enemies
{
    /// <summary>Fragmento de luz da libertação: sobe e se dissolve.</summary>
    public class LiberationMote : MonoBehaviour
    {
        private Vector2 velocity;
        private float lifetime;
        private float age;
        private SpriteRenderer sr;

        public void Init(Vector2 initialVelocity, float life)
        {
            velocity = initialVelocity;
            lifetime = Mathf.Max(0.05f, life);
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 1f - 1.5f * Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - age / lifetime;
                sr.color = c;
            }
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
