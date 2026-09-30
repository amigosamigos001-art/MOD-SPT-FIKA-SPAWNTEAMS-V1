using System;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using SpawnTeams.Networking;

namespace SpawnTeams
{
    /// <summary>
    /// Anuncia tu equipo al resto de jugadores de la raid y escucha los suyos, para poder
    /// mostrar una lista de "quien esta jugando y en que equipo" en la ventana F7.
    ///
    /// Se apoya en la API de modding publica de Fika (confirmada por inspeccion de IL de
    /// Fika_Core.dll 2.4.3, no supuesta):
    ///   - FikaEventDispatcher.SubscribeEvent&lt;FikaNetworkManagerCreatedEvent&gt; avisa en
    ///     cuanto hay un IFikaNetworkManager utilizable.
    ///   - IFikaNetworkManager.RegisterPacket&lt;T&gt; / SendData&lt;T&gt; son los mismos metodos
    ///     que usa Fika para sus propios paquetes internos.
    ///
    /// Esto es completamente independiente del sistema de spawn (SpawnRequestPatch): si este
    /// sync fallara por completo, los equipos seguirian apareciendo en spawns distintos igual,
    /// solo que sin la lista de jugadores en pantalla.
    /// </summary>
    internal static class FikaSyncManager
    {
        private static IFikaNetworkManager _manager;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
                Plugin.Log.LogInfo("[SpawnTeams] Sync de lista de jugadores suscrito.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] No se pudo suscribir a eventos de Fika: " + e);
            }
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent evt)
        {
            try
            {
                _manager = evt.Manager;
                _manager.RegisterPacket<TeamSyncPacket>(OnTeamSyncPacketReceived);
                Plugin.Log.LogInfo("[SpawnTeams] NetworkManager disponible. TeamSyncPacket registrado.");

                if (!string.IsNullOrEmpty(TeamRoster.LocalProfileId))
                    BroadcastLocalTeam();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] Error al conectar con el NetworkManager de Fika: " + e);
            }
        }

        private static void OnTeamSyncPacketReceived(TeamSyncPacket packet)
        {
            try
            {
                if (packet == null || string.IsNullOrEmpty(packet.ProfileId)) return;
                TeamRoster.RegisterRemote(packet.ProfileId, packet.TeamId, packet.TeamCount);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] Error procesando TeamSyncPacket: " + e);
            }
        }

        /// <summary>Llamar al registrar el equipo local o al cambiarlo desde la UI.</summary>
        public static void BroadcastLocalTeam()
        {
            if (_manager == null) return; // aun no hay NetworkManager; se manda en cuanto exista
            if (string.IsNullOrEmpty(TeamRoster.LocalProfileId)) return;

            try
            {
                var packet = new TeamSyncPacket
                {
                    ProfileId = TeamRoster.LocalProfileId,
                    TeamId = TeamState.MyTeam,
                    TeamCount = TeamState.TeamCount,
                };
                _manager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                Plugin.Log.LogInfo("[SpawnTeams] TeamSyncPacket enviado: Team " + packet.TeamId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] Error enviando TeamSyncPacket: " + e);
            }
        }
    }
}
