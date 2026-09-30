using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SpawnTeams.Patches;
using UnityEngine;

namespace SpawnTeams
{
    [BepInPlugin(Guid, "Fika Spawn Teams", "0.1.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.spawnteams.fika";

        public static ManualLogSource Log;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<int> TeamCount;
        public static ConfigEntry<int> MyTeam;
        public static ConfigEntry<bool> ForceSpawnTogether;
        public static ConfigEntry<KeyboardShortcut> ToggleUi;

        private bool _ready;
        private bool _uiOpen;
        private Rect _window = new Rect(40f, 120f, 280f, 0f);

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("1. General", "Activado", true,
                "Desactivalo para volver al comportamiento de spawn vanilla de Fika.");

            TeamCount = Config.Bind("1. General", "Numero de equipos", 3,
                new ConfigDescription("Cuantos equipos se usan en la partida: 2 o 3.",
                    new AcceptableValueList<int>(2, 3)));

            MyTeam = Config.Bind("2. Mi equipo", "Equipo", 1,
                new ConfigDescription("Tu equipo. 0 = sin equipo (spawn vanilla). Cambialo con la tecla de la UI.",
                    new AcceptableValueRange<int>(0, 3)));

            ForceSpawnTogether = Config.Bind("3. Avanzado", "Forzar spawn compartido (Obsoleto)", false,
                "Dejar en false. Usa el ajuste 'SamePlace' del host de Fika.");

            ToggleUi = Config.Bind("2. Mi equipo", "Tecla de la UI", new KeyboardShortcut(KeyCode.F7),
                "Abre y cierra el selector de equipo.");

            try
            {
                if (!FikaRefl.Resolve())
                {
                    Log.LogWarning("[SpawnTeams] Inicializacion abortada.");
                    return;
                }

                var harmony = new Harmony(Guid);

                // Aquí parcheamos SOLAMENTE el SpawnRequest para cambiar los equipos
                harmony.Patch(FikaRefl.UpdatePlayerSpawn,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(SpawnRequestPatch), nameof(SpawnRequestPatch.Prefix))));
                
                _ready = true;
                FikaSyncManager.Initialize();
                Log.LogInfo("[SpawnTeams] Listo. Mod cargado con exito sin forzar el spawn.");
            }
            catch (Exception e)
            {
                Log.LogError("[SpawnTeams] Fallo al inicializar: " + e);
            }
        }

        private void Update()
        {
            if (!_ready) return;
            if (ToggleUi.Value.IsDown()) 
            {
                _uiOpen = !_uiOpen;
                if (_uiOpen) FikaSyncManager.BroadcastLocalTeam();
            }
        }

        private void OnGUI()
        {
            if (!_ready || !_uiOpen) return;
            _window = GUILayout.Window(GetInstanceID(), _window, DrawWindow, "Spawn Teams");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Estado: " + TeamState.Describe());

            if (!ModEnabled.Value)
            {
                if (GUILayout.Button("Activar mod")) ModEnabled.Value = true;
                GUI.DragWindow();
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Equipos en juego:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(TeamCount.Value == 2, " 2 equipos")) TeamCount.Value = 2;
            if (GUILayout.Toggle(TeamCount.Value == 3, " 3 equipos")) TeamCount.Value = 3;
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Tu equipo:");
            for (int i = 1; i <= TeamState.TeamCount; i++)
            {
                bool sel = TeamState.MyTeam == i;
                if (GUILayout.Button((sel ? "> " : "   ") + "Team " + i))
                {
                    TeamState.MyTeam = i;
                    FikaSyncManager.BroadcastLocalTeam();
                }
            }
            if (GUILayout.Button((TeamState.MyTeam == 0 ? "> " : "   ") + "Sin equipo"))
            {
                TeamState.MyTeam = 0;
                FikaSyncManager.BroadcastLocalTeam();
            }

            GUILayout.Space(6f);
            
            GUILayout.Label("Jugadores listos: " + TeamRoster.Count);
            foreach (var kv in TeamRoster.AllSortedByTeam())
            {
                string keyStr = kv.Key ?? "Unknown";
                string shortId = keyStr.Length > 8 ? keyStr.Substring(0, 8) : keyStr;
                string suffix = (keyStr == TeamRoster.LocalProfileId) ? " (tú)" : "";
                GUILayout.Label("  Team " + kv.Value.Team + " - " + shortId + "..." + suffix);
            }
            
            if (string.IsNullOrEmpty(TeamRoster.LocalProfileId))
            {
                GUILayout.Label("\n(Los jugadores aparecerán aquí\nen cuanto empiece la pantalla de carga)", GUI.skin.label);
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("Cerrar")) _uiOpen = false;

            GUI.DragWindow();
        }
    }
}