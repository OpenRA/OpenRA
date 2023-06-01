## Server Orders
notification-custom-rules = This map contains custom rules. Game experience may change.
notification-map-bots-disabled = Bots have been disabled on this map.
notification-two-humans-required = This server requires at least two human players to start a match.
notification-unknown-server-command = Unknown server command: { $command }
notification-admin-start-game = Only the host can start the game.
notification-no-start-until-required-slots-full = Unable to start the game until required slots are full.
notification-no-start-without-players = Unable to start the game with no players.
notification-insufficient-enabled-spawn-points = Unable to start the game until more spawn points are enabled.
notification-malformed-command = Malformed { $command } command
notification-state-unchanged-ready = Cannot change state when marked as ready.
notification-invalid-faction-selected = Invalid faction selected: { $faction }
notification-supported-factions = Supported values: { $factions }
notification-state-unchanged-game-started = Cannot change state when game started. ({ $command })
notification-requires-host = Only the host can do that.
notification-invalid-bot-slot = Can't add bots to a slot with another client.
notification-invalid-bot-type = Invalid bot type.
notification-admin-change-map = Only the host can change the map.
notification-lobby-disconnected = { $player } has left.
notification-player-disconnected = { $player } has disconnected.
notification-team-player-disconnected = { $player } (Team { $team }) has disconnected.
notification-observer-disconnected = { $player } (Spectator) has disconnected.
notification-unknown-map = Map was not found on server.
notification-searching-map = Searching for map on the Resource Center...
notification-admin-change-configuration = Only the host can change the configuration.
notification-changed-map = { $player } changed the map to { $map }
notification-option-changed = { $player } changed { $name } to { $value }.
notification-you-were-kicked = You have been kicked from the server.
notification-kicked = { $admin } kicked { $player } from the server.
notification-temp-ban = { $admin } temporarily banned { $player } from the server.
notification-admin-transfer-admin = Only admins can transfer admin to another player.
notification-admin-move-spectators = Only the host can move players to spectators.
notification-empty-slot = No-one in that slot.
notification-move-spectators = { $admin } moved { $player } to spectators.
notification-nick-changed = { $player } is now known as { $name }.
notification-player-dropped = A player has been dropped after timing out.
notification-connection-problems = { $player } is experiencing connection problems.
notification-timeout-dropped = { $player } has been dropped after timing out.
notification-timeout-dropped-in =
    { $timeout ->
        [one] { $player } will be dropped in { $timeout } second.
       *[other] { $player } will be dropped in { $timeout } seconds.
    }
notification-error-game-started = The game has already started.
notification-requires-password = Server requires a password.
notification-incorrect-password = Incorrect password.
notification-incompatible-mod = Server is running an incompatible mod.
notification-incompatible-version = Server is running an incompatible version.
notification-incompatible-protocol = Server is running an incompatible protocol.
notification-you-were-banned = You have been banned from the server.
notification-you-were-temp-banned = You have been temporarily banned from the server.
notification-game-full = The game is full.
notification-joined = { $player } has joined the game.
notification-new-admin = { $player } is now the admin.
notification-option-locked = { $option } cannot be changed.
notification-invalid-configuration-command = Invalid configuration command.
notification-admin-option = Only the host can set that option.
notification-error-number-teams = Number of teams could not be parsed: { $raw }
notification-admin-kick = Only the host can kick players.
notification-kick-self = The host is not allowed to kick themselves.
notification-kick-none = No-one in that slot.
notification-no-kick-game-started = Only spectators and defeated players can be kicked after the game has started.
notification-admin-clear-spawn = Only admins can clear spawn points.
notification-spawn-occupied = You cannot occupy the same spawn point as another player.
notification-spawn-locked = The spawn point is locked to another player slot.
notification-admin-lobby-info = Only the host can set lobby info.
notification-invalid-lobby-info = Invalid lobby info sent.
notification-player-color-terrain = Color was adjusted to be less similar to the terrain.
notification-player-color-player = Color was adjusted to be less similar to another player.
notification-invalid-player-color = Unable to determine a valid player color. A random color has been selected.
notification-invalid-error-code = Failed to parse error message.
notification-master-server-connected = Master server communication established.
notification-master-server-error = "Master server communication failed."
notification-game-offline = Game has not been advertised online.
notification-no-port-forward = Server port is not accessible from the internet.
notification-blacklisted-server-name = Server name contains a blacklisted word.
notification-requires-authentication = Server requires players to have an OpenRA forum account.
notification-no-permission-to-join = You do not have permission to join this server.
notification-slot-closed = Your slot was closed by the host.

## Server
notification-game-started = Game started

## PlayerMessageTracker
notification-chat-temp-disabled =
    { $remaining ->
        [one] Chat is disabled. Please try again in { $remaining } second.
       *[other] Chat is disabled. Please try again in { $remaining } seconds.
    }
