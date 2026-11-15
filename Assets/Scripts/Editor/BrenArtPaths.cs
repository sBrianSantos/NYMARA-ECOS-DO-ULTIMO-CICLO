using System.Collections.Generic;

namespace Nymara.EditorTools
{
    /// <summary>
    /// Organização dos recursos visuais de Bren por fase.
    ///   Bren/Shared      : referências, conceitos e paletas comuns às duas fases.
    ///   Bren/Initial     : Bren antes de receber a Espada de Arua (sem arma).
    ///   Bren/Arua_Sword  : Bren com a Espada de Arua (usado no Prototype 01).
    /// Caminhos relativos a Assets/Art/Prototype, sem extensão.
    /// </summary>
    public static class BrenArtPaths
    {
        public const string Root = "Assets/Art/Prototype/Bren";

        public static readonly string[] Folders =
        {
            "Shared", "Shared/References", "Shared/Concepts", "Shared/Palettes",
            "Initial", "Initial/Idle", "Initial/Walk", "Initial/Run", "Initial/Jump", "Initial/Fall", "Initial/Land", "Initial/Hurt",
            "Arua_Sword", "Arua_Sword/Idle", "Arua_Sword/Walk", "Arua_Sword/Run", "Arua_Sword/Jump", "Arua_Sword/Fall",
            "Arua_Sword/Land", "Arua_Sword/Dash", "Arua_Sword/Hurt", "Arua_Sword/Combat",
            "Arua_Sword/Combat/Attack_01", "Arua_Sword/Combat/Attack_02", "Arua_Sword/Combat/Attack_03",
            "Arua_Sword/Combat/Charged_Attack", "Arua_Sword/Combat/Aerial_Attack",
        };

        /// <summary>Quadros provisórios da fase com espada (sufixo do arquivo Bren_&lt;sufixo&gt;.png) → subpasta.</summary>
        public static string SwordFolderFor(string frame)
        {
            if (frame.StartsWith("idle_")) return "Idle";
            if (frame.StartsWith("run_")) return "Run";
            if (frame == "jump" || frame == "apex") return "Jump";
            if (frame == "fall") return "Fall";
            if (frame == "land") return "Land";
            if (frame == "dash") return "Dash";
            if (frame == "hurt") return "Hurt";
            if (frame.StartsWith("atk1_")) return "Combat/Attack_01";
            if (frame.StartsWith("atk2_")) return "Combat/Attack_02";
            if (frame.StartsWith("atk3_")) return "Combat/Attack_03";
            if (frame.StartsWith("charge")) return "Combat/Charged_Attack";
            if (frame.StartsWith("dive")) return "Combat/Aerial_Attack";
            return null;
        }

        /// <summary>Caminho (relativo a Assets/Art/Prototype, sem .png) de um quadro da fase com espada.</summary>
        public static string Sword(string frame) => $"Bren/Arua_Sword/{SwordFolderFor(frame)}/Bren_{frame}";

        public static readonly string[] InitialIdle =
        {
            "Bren_Idle_01", "Bren_Idle_02", "Bren_Idle_03", "Bren_Idle_04",
            "Bren_Idle_05", "Bren_Idle_06", "Bren_Idle_07", "Bren_Idle_08",
        };
    }
}
