**Fika Spawn Teams**
A client-side BepInEx plugin for Fika (SPT Tarkov) that enables team-based PvP/Co-op spawning by dynamically splitting players into separate squads across the map.

**Overview**
By default, Fika forces all co-op players to spawn together in the same location (SamePlace). Fika Spawn Teams intercepts the raid initialization process in memory, forcing the game to use "Different Places" (AsOnline) and dynamically re-tagging player GroupId values (_team_1, _team_2, etc.).

This allows Escape from Tarkov's native spawn engine to treat teams as opposing squads and place them at separate spawn points around the map.

**Key Features**
Dynamic Squad Partitioning: Overwrites player GroupIds prior to spawn request dispatch, assigning distinct squad tags based on chosen teams.

Memory-Based Spawn Override: Automatically overrides Fika's hardcoded "Together" spawn restriction in memory when launching a raid.

In-Game GUI: Accessible via hotkey (F7 by default) to select your team, configure team count (2 or 3 teams), and view connected players in real time.

Network Synchronization: Broadcasts team selections across all connected clients to ensure match coherence.

Client-Side Only: Operates entirely within BepInEx/plugins/. No SPT server-side mod (user/mods/) is required.

**Requirements**
SPT (Server & Client)

Fika Core Plugin (com.fika.core)

BepInEx 5

**Installation**
Download the latest SpawnTeams.dll release.

Copy SpawnTeams.dll into <GameDirectory>/BepInEx/plugins/.

Note: All participating players (Host and Clients) must install the plugin to synchronize team tags properly.

**How to Use**
Host or join a Fika co-op lobby as usual.

Press F7 to open the Spawn Teams menu.

Configure the number of active teams (2 or 3) and select your team (Team 1, Team 2, etc.).

Verify that all players appear in the team roster in the menu.

Start the raid. The plugin automatically handles spawn separation without requiring manual adjustments in Fika's lobby settings.

**Configuration**
A configuration file is automatically generated at BepInEx/config/com.spawnteams.fika.cfg upon first run:

Activado -> Enables or disables the mod logic
Numero de equipos -> Total number of available teams (2 or 3).
Equipo -> Default assigned team (0 = Vanilla/No team).
Tecla de la UI -> Hotkey to toggle the team selector window.
