using System;

namespace SpawnTeams
{
    internal static class TeamState
    {
        /// <summary>Equipo elegido por este jugador. 0 = sin equipo (comportamiento vanilla).</summary>
        public static int MyTeam
        {
            get { return Plugin.MyTeam.Value; }
            set { Plugin.MyTeam.Value = Clamp(value); }
        }

        public static int TeamCount => Plugin.TeamCount.Value == 3 ? 3 : 2;

        public static bool Active => Plugin.ModEnabled.Value && MyTeam >= 1 && MyTeam <= TeamCount;

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > TeamCount) return TeamCount;
            return v;
        }

        /// <summary>
        /// Clave de correlacion que se envia a Fika combinando el GroupId original con el equipo.
        /// Fika necesita el GroupId original intacto en alguna parte de la cadena para saber que
        /// todos estos jugadores pertenecen a la misma partida del Host. Al anadir el sufijo de
        /// team, creamos subgrupos validos dentro de la misma raid.
        /// </summary>
        public static string BuildKey(string originalGroupId)
        {
            // Si por algun motivo viene vacio, usamos el comportamiento anterior de emergencia
            if (string.IsNullOrEmpty(originalGroupId)) return "pvpve_team_" + MyTeam;
            
            return originalGroupId + "_team_" + MyTeam;
        }

        public static string Describe()
        {
            if (!Plugin.ModEnabled.Value) return "Mod desactivado";
            if (MyTeam == 0) return "Sin equipo (spawn vanilla)";
            return "Team " + MyTeam + " de " + TeamCount;
        }
    }
}