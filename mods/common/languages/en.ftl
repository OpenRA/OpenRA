## Server Orders
custom-rules = This map contains custom rules. Game experience may change.
map-bots-disabled = Bots have been disabled on this map.
two-humans-required = This server requires at least two human players to start a match.
unknown-server-command = Unknown server command: { $command }
only-only-host-start-game = Only the host can start the game.
no-start-until-required-slots-full = Unable to start the game until required slots are full.
no-start-without-players = Unable to start the game with no players.
insufficient-enabled-spawnPoints = Unable to start the game until more spawn points are enabled.
malformed-command = Malformed { $command } command
chat-disabled =
    { $remaining ->
        [one] Chat is disabled. Please try again in { $remaining } second.
       *[other] Chat is disabled. Please try again in { $remaining } seconds.
    }
state-unchanged-ready = Cannot change state when marked as ready.
invalid-faction-selected = Invalid faction selected: { $faction }
supported-factions = Supported values: { $factions }
state-unchanged-game-started = Cannot change state when game started. ({ $command })
requires-host = Only the host can do that.
invalid-bot-slot = Can't add bots to a slot with another client.
invalid-bot-type = Invalid bot type.
only-host-change-map = Only the host can change the map.
lobby-disconnected = { $player } has left.
player-disconnected =
    { $team ->
        [0] { $player } has disconnected.
       *[other] { $player } (Team { $team }) has disconnected.
    }
observer-disconnected = { $player } (Spectator) has disconnected.
unknown-map = Map was not found on server.
searching-map = Searching for map on the Resource Center...
only-host-change-configuration = Only the host can change the configuration.
changed-map = { $player } changed the map to { $map }
value-changed = { $player } changed { $name } to { $value }.
you-were-kicked = You have been kicked from the server.
kicked = { $admin } kicked { $player } from the server.
temp-ban = { $admin } temporarily banned { $player } from the server.
only-host-transfer-admin = Only admins can transfer admin to another player.
only-host-move-spectators = Only the host can move players to spectators.
empty-slot = No-one in that slot.
move-spectators = { $admin } moved { $player } to spectators.
nick = { $player } is now known as { $name }.
player-dropped = A player has been dropped after timing out.
connection-problems = { $player } is experiencing connection problems.
timeout = { $player } has been dropped after timing out.
timeout-in =
    { $timeout ->
        [one] { $player } will be dropped in { $timeout } second.
       *[other] { $player } will be dropped in { $timeout } seconds.
    }
error-game-started = The game has already started.
requires-password = Server requires a password.
incorrect-password = Incorrect password.
incompatible-mod = Server is running an incompatible mod.
incompatible-version = Server is running an incompatible version.
incompatible-protocol = Server is running an incompatible protocol.
banned = You have been banned from the server.
temp-banned = You have been temporarily banned from the server.
full = The game is full.
joined = { $player } has joined the game.
new-admin = { $player } is now the admin.
option-locked = { $option } cannot be changed.
invalid-configuration-command = Invalid configuration command.
admin-option = Only the host can set that option.
number-teams = Number of teams could not be parsed: { $raw }
admin-kick = Only the host can kick players.
kick-none = No-one in that slot.
no-kick-game-started = Only spectators can be kicked after the game has started.
admin-clear-spawn = Only admins can clear spawn points.
spawn-occupied = You cannot occupy the same spawn point as another player.
spawn-locked = The spawn point is locked to another player slot.
admin-lobby-info = Only the host can set lobby info.
invalid-lobby-info = Invalid lobby info sent.
player-color-terrain = Color was adjusted to be less similar to the terrain.
player-color-player = Color was adjusted to be less similar to another player.
invalid-player-color = Unable to determine a valid player color. A random color has been selected.
invalid-error-code = Failed to parse error message.
master-server-connected = Master server communication established.
master-server-error = "Master server communication failed."
game-offline = Game has not been advertised online.
no-port-forward = Server port is not accessible from the internet.
blacklisted-title = Server name contains a blacklisted word.
requires-forum-account = Server requires players to have an OpenRA forum account.
no-permission = You do not have permission to join this server.
slot-closed = Your slot was closed by the host.

## Server
game-started = Game started

## Server also LobbyUtils
bots-disabled = Bots Disabled

## ActorEditLogic
duplicate-actor-id = Duplicate Actor ID
enter-actor-id = Enter an Actor ID
owner = Owner

## ActorSelectorLogic
type = Type

## CommonSelectorLogic
search-results = Search Results
multiple = Multiple

## GameInfoLogic
objectives = Objectives
briefing = Briefing
options = Options
debug = Debug
chat = Chat

## GameInfoObjectivesLogic also GameInfoStatsLogic
in-progress = In progress
accomplished = Accomplished
failed = Failed

## GameTimerLogic
paused = Paused
max-speed = Max Speed
speed = { $percentage }% Speed
complete = { $percentage }% complete

## LobbyLogic, InGameChatLogic
chat-availability =
    { $seconds ->
        [zero] Chat Disabled
        [one] Chat available in { $seconds } second...
        *[other] Chat available in { $seconds } seconds...
    }

## IngamePowerBarLogic
## IngamePowerCounterLogic
power-usage = Power Usage

## IngameSiloBarLogic
## IngameCashCounterLogic
silo-usage = Silo Usage: { $resources }/{ $capacity }

## ObserverShroudSelectorLogic
camera-option-all-players = All Players
camera-option-disable-shroud = Disable Shroud
camera-option-other = Other

## ObserverStatsLogic
information-none = Information: None
basic = Basic
economy = Economy
production = Production
support-powers = Support Powers
combat = Combat
army = Army
earnings-graph = Earnings (graph)
army-graph = Army (graph)

## WorldTooltipLogic
unrevealed-terrain = Unrevealed Terrain

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, SpawnSelectorTooltipLogic
team-no-team =
    { $team ->
        [zero] No Team
       *[other] Team { $team }
    }

## LobbyLogic, CommonSelectorLogic, InGameChatLogic
all = All

## InputSettingsLogic, CommonSelectorLogic
none = None

## LobbyLogic, IngameChatLogic
team = Team

## ServerListLogic, ReplayBrowserLogic also ObserverShroudSelectorLogic
players = Players
