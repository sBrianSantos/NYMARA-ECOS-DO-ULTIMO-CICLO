using UnityEngine;
using UnityEngine.Rendering;

namespace Nymara.Visual
{
    /// <summary>
    /// Camada de parallax puramente visual. A posição é ajustada logo antes da câmera renderizar
    /// (depois do Cinemachine), então não interfere na câmera nem gera tremor.
    /// factor = 0 acompanha o mundo; 1 acompanha a câmera; negativo = primeiro plano (move mais rápido).
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Vector2 factor = new Vector2(0.5f, 0.3f);
        [Tooltip("Posição da câmera em que a camada fica exatamente na posição de cena.")]
        [SerializeField] private Vector2 reference = new Vector2(19f, 2f);

        private Vector3 basePos;
        private bool hasBase;

        private void Awake()
        {
            basePos = transform.position;
            hasBase = true;
        }

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

        private void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!hasBase || cam == null || cam.cameraType != CameraType.Game) return;
            Vector3 c = cam.transform.position;
            transform.position = new Vector3(basePos.x + (c.x - reference.x) * factor.x,
                                             basePos.y + (c.y - reference.y) * factor.y,
                                             basePos.z);
        }
    }
}
