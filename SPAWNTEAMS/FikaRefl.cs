using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpawnTeams
{
    /// <summary>
    /// Todo el acoplamiento con Fika/EFT vive aqui y se resuelve por nombre en runtime.
    /// Si Fika cambia una firma, el mod se desactiva con un log claro en vez de crashear.
    /// </summary>
    internal static class FikaRefl
    {
        public static Assembly FikaAssembly;

        public static MethodInfo UpdatePlayerSpawn;      // FikaRequestHandler.UpdatePlayerSpawn(PlayerSpawnRequest)
        public static MethodInfo CheckSpawnTogether;     // ClientGameController.CheckSpawnTogether()
        public static Type SpawnRequestType;             // PlayerSpawnRequest

        private static MemberInfo _groupIdMember;
        private static MemberInfo _serverIdMember;
        private static MemberInfo _profileIdMember;

        public static bool SpawnRequestUsable => _groupIdMember != null;

        public static bool Resolve()
        {
            FikaAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.IndexOf("Fika", StringComparison.OrdinalIgnoreCase) >= 0
                                     && a.GetName().Name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0);

            if (FikaAssembly == null)
            {
                FikaAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.StartsWith("Fika", StringComparison.OrdinalIgnoreCase));
            }

            if (FikaAssembly == null)
            {
                Plugin.Log.LogError("[SpawnTeams] No se encontro el ensamblado de Fika. El mod no hara nada.");
                return false;
            }

            Type[] types = SafeGetTypes(FikaAssembly);

            UpdatePlayerSpawn = FindMethod(types, "UpdatePlayerSpawn", minParams: 1);
            CheckSpawnTogether = FindMethod(types, "CheckSpawnTogether", minParams: 0);

            if (UpdatePlayerSpawn == null)
            {
                Plugin.Log.LogError("[SpawnTeams] No se encontro FikaRequestHandler.UpdatePlayerSpawn. " +
                                    "Fika ha cambiado su API; el mod no puede reasignar spawns.");
                return false;
            }

            ParameterInfo[] ps = UpdatePlayerSpawn.GetParameters();
            SpawnRequestType = ps.Length > 0 ? ps[0].ParameterType : null;
            if (SpawnRequestType != null)
            {
                _groupIdMember = FindMember(SpawnRequestType, "GroupId", "groupId", "Group");
                _serverIdMember = FindMember(SpawnRequestType, "ServerId", "serverId", "Server");
                _profileIdMember = FindMember(SpawnRequestType, "ProfileId", "profileId", "Profile");
            }

            if (_groupIdMember == null)
            {
                Plugin.Log.LogError("[SpawnTeams] El DTO " + (SpawnRequestType?.FullName ?? "?") +
                                    " no expone un campo GroupId escribible. El mod no puede continuar.");
                return false;
            }

            Plugin.Log.LogInfo("[SpawnTeams] Resuelto: " + UpdatePlayerSpawn.DeclaringType?.FullName +
                               "." + UpdatePlayerSpawn.Name + " / DTO " + SpawnRequestType?.Name);
            Plugin.Log.LogInfo("[SpawnTeams] CheckSpawnTogether: " +
                               (CheckSpawnTogether == null
                                   ? "NO encontrado (se seguira el ajuste vanilla PlayersSpawnPlace)"
                                   : CheckSpawnTogether.DeclaringType?.FullName + " -> " + CheckSpawnTogether.ReturnType.Name));
            return true;
        }

        public static string GetServerId(object request)
        {
            if (request == null || _serverIdMember == null) return string.Empty;
            try
            {
                object v = _serverIdMember is FieldInfo f
                    ? f.GetValue(request)
                    : ((PropertyInfo)_serverIdMember).GetValue(request, null);
                return v as string ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        public static string GetGroupId(object request)
        {
            if (request == null) return string.Empty;
            try
            {
                object v = _groupIdMember is FieldInfo f
                    ? f.GetValue(request)
                    : ((PropertyInfo)_groupIdMember).GetValue(request, null);
                return v as string ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        public static string GetProfileId(object request)
        {
            if (request == null || _profileIdMember == null) return string.Empty;
            try
            {
                object v = _profileIdMember is FieldInfo f
                    ? f.GetValue(request)
                    : ((PropertyInfo)_profileIdMember).GetValue(request, null);
                return v as string ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Escribe GroupId en el DTO. Si es struct, el llamante debe devolver el objeto
        /// (boxed) al array de argumentos de Harmony.
        /// </summary>
        public static bool SetGroupId(object request, string value)
        {
            if (request == null || _groupIdMember == null) return false;
            try
            {
                if (_groupIdMember is FieldInfo f)
                {
                    f.SetValue(request, value);
                    return true;
                }
                var p = (PropertyInfo)_groupIdMember;
                if (!p.CanWrite)
                {
                    // Propiedad autoimplementada de solo lectura -> escribimos el backing field.
                    FieldInfo backing = p.DeclaringType?.GetField("<" + p.Name + ">k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    if (backing == null) return false;
                    backing.SetValue(request, value);
                    return true;
                }
                p.SetValue(request, value, null);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SpawnTeams] Fallo escribiendo GroupId: " + e.Message);
                return false;
            }
        }

        private static MemberInfo FindMember(Type t, params string[] names)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (string n in names)
            {
                FieldInfo f = t.GetField(n, flags);
                if (f != null && f.FieldType == typeof(string)) return f;
                PropertyInfo p = t.GetProperty(n, flags);
                if (p != null && p.PropertyType == typeof(string)) return p;
            }
            // Ultimo recurso: cualquier miembro string cuyo nombre contenga "group".
            foreach (FieldInfo f in t.GetFields(flags))
                if (f.FieldType == typeof(string) && f.Name.IndexOf("group", StringComparison.OrdinalIgnoreCase) >= 0)
                    return f;
            return null;
        }

        private static MethodInfo FindMethod(IEnumerable<Type> types, string name, int minParams)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                                       BindingFlags.Public | BindingFlags.NonPublic;
            foreach (Type t in types)
            {
                MethodInfo[] ms;
                try { ms = t.GetMethods(flags); }
                catch { continue; }

                foreach (MethodInfo m in ms)
                {
                    if (!string.Equals(m.Name, name, StringComparison.Ordinal)) continue;
                    if (m.GetParameters().Length < minParams) continue;
                    if (m.IsAbstract) continue;
                    return m;
                }
            }
            return null;
        }

        private static Type[] SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }
    }
}
