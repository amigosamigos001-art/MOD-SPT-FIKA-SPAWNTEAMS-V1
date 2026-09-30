using System.Collections.Generic;
using System.Linq;

namespace SpawnTeams
{
    /// <summary>
    /// Lista de jugadores conocidos en esta raid y su equipo, solo para mostrarla en la UI.
    /// No decide el spawn (eso ya lo hace SpawnRequestPatch por su cuenta, sin depender de
    /// esta lista) — esto es puramente informativo, para que veas quien mas ha entrado.
    /// </summary>
    internal static class TeamRoster
    {
        private static readonly Dictionary<string, TeamEntry> _players = new Dictionary<string, TeamEntry>();

        public static string LocalProfileId { get; private set; } = string.Empty;

        public struct TeamEntry
        {
            public int Team;
            public int TeamCount;
        }

        public static void RegisterLocal(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
            LocalProfileId = profileId;
            Set(profileId, TeamState.MyTeam, TeamState.TeamCount);
        }

        public static void RegisterRemote(string profileId, int team, int teamCount)
        {
            if (string.IsNullOrEmpty(profileId) || profileId == LocalProfileId) return;
            Set(profileId, team, teamCount);
        }

        private static void Set(string profileId, int team, int teamCount)
        {
            _players[profileId] = new TeamEntry { Team = team, TeamCount = teamCount };
        }

        public static IReadOnlyList<KeyValuePair<string, TeamEntry>> AllSortedByTeam()
        {
            return _players.OrderBy(kv => kv.Value.Team).ThenBy(kv => kv.Key).ToList();
        }

        public static int Count => _players.Count;
    }
}
