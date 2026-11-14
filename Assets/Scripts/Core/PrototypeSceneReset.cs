using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Nymara.Core
{
    /// <summary>
    /// Debug do protótipo: recarrega a cena atual (tecla R / Select no gamepad)
    /// para repetir os testes do zero. Funciona mesmo se a cena não estiver no Build.
    /// </summary>
    public class PrototypeSceneReset : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionPath = "Prototype/ResetScene";

        private InputAction resetAction;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"[{nameof(PrototypeSceneReset)}] InputActionAsset não atribuído.", this);
                enabled = false;
                return;
            }
            resetAction = actions.FindAction(actionPath);
            if (resetAction == null)
            {
                Debug.LogError($"[{nameof(PrototypeSceneReset)}] Ação '{actionPath}' não encontrada.", this);
                enabled = false;
            }
        }

        private void OnEnable() => resetAction?.Enable();
        private void OnDisable() => resetAction?.Disable();

        private void Update()
        {
            if (resetAction != null && resetAction.WasPressedThisFrame())
                ReloadActiveScene();
        }

        public static void ReloadActiveScene()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogWarning("Cena fora da lista de Build: não é possível recarregar.");
#endif
        }
    }
}
