using System.Collections;
using UnityEngine;

namespace Nymara.UI
{
    /// <summary>
    /// Fade de tela provisório (IMGUI, sem assets). Usado no retorno do protagonista.
    /// Usa tempo real para não depender de Time.timeScale.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] private Color fadeColor = new Color(0.05f, 0.02f, 0.1f, 1f);
        [SerializeField] private int guiDepth = -1000;

        private float alpha;

        public float Alpha => alpha;

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            targetAlpha = Mathf.Clamp01(targetAlpha);
            if (duration <= 0f)
            {
                alpha = targetAlpha;
                yield break;
            }

            float start = alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
                yield return null;
            }
            alpha = targetAlpha;
        }

        private void OnGUI()
        {
            if (alpha <= 0.001f) return;
            GUI.depth = guiDepth;
            Color previous = GUI.color;
            Color c = fadeColor;
            c.a *= alpha;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
