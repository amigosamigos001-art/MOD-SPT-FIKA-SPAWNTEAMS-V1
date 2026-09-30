using System;
using System.Reflection;
using HarmonyLib;

namespace SpawnTeams.Patches
{
    internal static class SpawnRequestPatch
    {
        public static void Prefix(object[] __args, object __instance)
        {
            // 1. FORZAR DIFFERENT PLACES EN MEMORIA (Protegido para no congelar la carga)
            try
            {
                if (TeamState.Active && __instance != null)
                {
                    var raidSettingsProp = AccessTools.Property(__instance.GetType(), "RaidSettings") 
                                         ?? (object)AccessTools.Field(__instance.GetType(), "_raidSettings");
                    
                    object raidSettings = null;
                    if (raidSettingsProp is PropertyInfo pInfo) raidSettings = pInfo.GetValue(__instance);
                    else if (raidSettingsProp is FieldInfo fInfo) raidSettings = fInfo.GetValue(__instance);

                    if (raidSettings != null)
                    {
                        var timeWeatherProp = AccessTools.Property(raidSettings.GetType(), "TimeAndWeatherSettings") 
                                            ?? (object)AccessTools.Field(raidSettings.GetType(), "TimeAndWeatherSettings");
                        
                        object timeWeather = null;
                        if (timeWeatherProp is PropertyInfo tp) timeWeather = tp.GetValue(raidSettings);
                        else if (timeWeatherProp is FieldInfo tf) tf.GetValue(raidSettings);

                        if (timeWeather != null)
                        {
                            var spawnPlaceField = AccessTools.Field(timeWeather.GetType(), "PlayersSpawnPlace") 
                                                ?? (object)AccessTools.Property(timeWeather.GetType(), "PlayersSpawnPlace");
                            
                            // 1 = DifferentPlaces / AsOnline
                            if (spawnPlaceField is FieldInfo sf) sf.SetValue(timeWeather, 1);
                            else if (spawnPlaceField is PropertyInfo sp) sp.SetValue(timeWeather, 1, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("[SpawnTeams] No se pudo forzar DifferentPlaces en memoria, continuando normalmente: " + ex.Message);
            }

            // 2. REETIQUETADO DE GRUPOS (Lógica principal)
            try
            {
                if (!TeamState.Active) return;
                if (__args == null || __args.Length == 0 || __args[0] == null) return;
                if (!FikaRefl.SpawnRequestUsable) return;

                object request = __args[0];

                // Registrar perfil para la interfaz
                string profileId = FikaRefl.GetProfileId(request);
                if (!string.IsNullOrEmpty(profileId))
                {
                    TeamRoster.RegisterLocal(profileId);
                    FikaSyncManager.BroadcastLocalTeam();
                }

                string original = FikaRefl.GetGroupId(request);
                
                // Si ya fue modificado, no volvemos a tocarlo
                if (original != null && original.Contains("_team_")) return;

                // Generar nuevo GroupId por equipo
                string key = TeamState.BuildKey(original);

                if (FikaRefl.SetGroupId(request, key))
                {
                    __args[0] = request; 
                    Plugin.Log.LogInfo("[SpawnTeams] Request modificado: '" + original + "' -> '" + key + "' (Team " + TeamState.MyTeam + ")");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] Error reetiquetando grupo: " + e);
            }
        }
    }
}