using Nymara.Player;
using UnityEditor;
using UnityEngine;

namespace Nymara.EditorTools
{
    /// <summary>
    /// Atalhos de teste do Prototype_01 (apenas Editor, apenas em Play Mode):
    /// teleporta o protagonista para o início de cada zona.
    /// </summary>
    public static class PrototypeDebugMenu
    {
        private const string Root = "NYMARA/Prototype/Teste (Play Mode)/";

        [MenuItem(Root + "Ir para Zona A - Movimento")]
        private static void GoZoneA() => Teleport(new Vector2(-20f, 0.85f));

        [MenuItem(Root + "Ir para Zona B - Combate")]
        private static void GoZoneB() => Teleport(new Vector2(15f, 0.85f));

        [MenuItem(Root + "Ir para Zona C - Integracao")]
        private static void GoZoneC() => Teleport(new Vector2(35f, 0.85f));

        [MenuItem(Root + "Ir para Zona A - Movimento", true)]
        [MenuItem(Root + "Ir para Zona B - Combate", true)]
        [MenuItem(Root + "Ir para Zona C - Integracao", true)]
        private static bool CanTeleport() => EditorApplication.isPlaying;

        private static void Teleport(Vector2 position)
        {
            var player = Object.FindFirstObjectByType<PlayerController2D>();
            if (player == null)
            {
                Debug.LogWarning("[PrototypeDebugMenu] PlayerController2D não encontrado na cena.");
                return;
            }
            player.TeleportTo(position);
        }
    }
}
