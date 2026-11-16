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
            "Initial", "Initial/Idle", "Initial/Idle/Right", "Initial/Idle/Left", "Initial/Idle/Legacy", "Initial/Walk", "Initial/Run", "Initial/Jump", "Initial/Fall", "Initial/Land", "Initial/Hurt",
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

        /// <summary>Idle do Bren inicial, sprites próprios por direção (sem espelhamento). Caminhos completos .png.</summary>
        public const string InitialIdleDir = Root + "/Initial/Idle";
        public static string InitialIdleRight(int i) => $"{InitialIdleDir}/Right/Bren_Idle_Right_{i:00}.png";
        public static string InitialIdleLeft(int i) => $"{InitialIdleDir}/Left/Bren_Idle_Left_{i:00}.png";
        public const int InitialIdleFrames = 8;

        /// <summary>Idle anterior (não direcional) do Bren inicial, arquivado em Initial/Idle/Legacy.</summary>
        public const string InitialIdleLegacyDir = InitialIdleDir + "/Legacy";
        public static readonly string[] InitialIdleLegacyFiles =
        {
            "Bren_Idle_01.png", "Bren_Idle_02.png", "Bren_Idle_03.png", "Bren_Idle_04.png",
            "Bren_Idle_05.png", "Bren_Idle_06.png", "Bren_Idle_07.png", "Bren_Idle_08.png",
            "Bren_Idle_Spritesheet_Preview.png", "README.txt",
        };
    }
}
