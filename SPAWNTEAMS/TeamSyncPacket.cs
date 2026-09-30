using Fika.Core.Networking.LiteNetLib.Utils;

namespace SpawnTeams.Networking
{
    /// <summary>
    /// Paquete que cada cliente manda al elegir o confirmar su equipo, para que el resto
    /// pueda mostrarlo en su lista de jugadores. Usa INetSerializable, la misma interfaz
    /// que los paquetes internos de Fika, confirmada por inspeccion de Fika_Core.dll.
    /// </summary>
    public class TeamSyncPacket : INetSerializable
    {
        public string ProfileId;
        public int TeamId;
        public int TeamCount;

        public void Serialize(NetDataWriter writer)
        {
            NetIO.PutString(writer, ProfileId);
            NetIO.PutInt(writer, TeamId);
            NetIO.PutInt(writer, TeamCount);
        }

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = NetIO.GetString(reader);
            TeamId = NetIO.GetInt(reader);
            TeamCount = NetIO.GetInt(reader);
        }
    }
}
