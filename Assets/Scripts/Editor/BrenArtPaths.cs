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
            "Initial", "Initial/Idle", "Initial/Idle/Right", "Initial/Idle/Left", "Initial/Walk", "Initial/Run", "Initial/Jump", "Initial/Fall", "Initial/Land", "Initial/Hurt",
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
        /// <summary>Idle Right definitivo: um spritesheet com os 8 quadros lado a lado (fatiado na importação).</summary>
        public const string InitialIdleRightSheet = InitialIdleDir + "/Right/Bren_Idle_Right_Spritesheet.png";
        public static string InitialIdleRightFrameName(int i) => $"Bren_Idle_Right_{i:00}";
        /// <summary>Idle Left definitivo: spritesheet com os 8 quadros lado a lado, mesma grade do Right.</summary>
        public const string InitialIdleLeftSheet = InitialIdleDir + "/Left/Spritesheet de Idle do Bren Initial - Left.png";
        public static string InitialIdleLeftFrameName(int i) => $"Bren_Idle_Left_{i:00}";
        public const int InitialIdleFrames = 8;

    }
}
