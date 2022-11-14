## MapOptions, MissionBrowserLogic
slowest = Slowest
slower = Slower
normal = Normal
fast = Fast
faster = Faster
fastest = Fastest

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
state-unchanged-ready = Cannot change state when marked as ready.
invalid-faction-selected = Invalid faction selected: { $faction }
supported-factions = Supported values: { $factions }
state-unchanged-game-started = Cannot change state when game started. ({ $command })
requires-host = Only the host can do that.
invalid-bot-slot = Can't add bots to a slot with another client.
invalid-bot-type = Invalid bot type.
only-host-change-map = Only the host can change the map.
lobby-disconnected = { $player } has left.
player-disconnected = { $player } has disconnected.
player-team-disconnected = { $player } (Team { $team }) has disconnected.
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

## PlayerMessageTracker
chat-temp-disabled =
    { $remaining ->
        [one] Chat is disabled. Please try again in { $remaining } second.
       *[other] Chat is disabled. Please try again in { $remaining } seconds.
    }

## ActorEditLogic
duplicate-actor-id = Duplicate Actor ID
enter-actor-id = Enter an Actor ID
owner = Owner

## ActorSelectorLogic
type = Type

## CommonSelectorLogic
search-results = Search Results
multiple = Multiple

## SaveMapLogic
unpacked = unpacked

save-map-failed-title = Failed to save map
save-map-failed-prompt = See debug.log for details.
save-map-failed-accept = OK

overwrite-map-failed-title = Warning
overwrite-map-failed-prompt = By saving you will overwrite
    an already existing map.
overwrite-map-failed-confirm = Save

overwrite-map-outside-edit-title = Warning
overwrite-map-outside-edit-prompt = "The map has been edited from outside the editor.
    By saving you may overwrite progress
overwrite-map-outside-edit-confirm = Save

## GameInfoLogic
objectives = Objectives
briefing = Briefing
options = Options
debug = Debug
chat = Chat

## GameInfoObjectivesLogic, GameInfoStatsLogic
in-progress = In progress
accomplished = Accomplished
failed = Failed

## GameInfoStatsLogic
mute = Mute this player
unmute = Unmute this player

## GameInfoStatsLogic
gone = Gone

kick-title = Kick { $player }?
kick-prompt = They will not be able to rejoin this game.
kick-accept = Kick

## GameTimerLogic
paused = Paused
max-speed = Max Speed
speed = { $percentage }% Speed
complete = { $percentage }% complete

## LobbyLogic, InGameChatLogic
chat-disabled = Chat Disabled
chat-availability =
    { $seconds ->
        [one] Chat available in { $seconds } second...
        *[other] Chat available in { $seconds } seconds...
    }

## IngameMenuLogic
leave = Leave
abort-mission = Abort Mission

leave-mission-title = Leave Mission
leave-mission-prompt = Leave this game and return to the menu?
leave-mission-accept = Leave
leave-mission-cancel = Stay

restart-button = Restart

restart-mission-title = Restart
restart-mission-prompt = Are you sure you want to restart?
restart-mission-accept = Restart
restart-mission-cancel = Stay

surrender-button = Surrender

surrender-title = Surrender
surrender-prompt = Are you sure you want to surrender?
surrender-accept = Surrender
surrender-cancel = Stay

load-game-button = Load Game
save-game-button = Save Game

music-button = Music

settings-button = Settings

return-to-map = Return to map
resume = Resume

save-map-button = Save Map

error-max-player-title = Error: Max player count exceeded
error-max-player-prompt = There are too many players defined ({ $players }/{ $max }).
error-max-player-accept = Back

exit-map-button = Exit Map Editor

exit-map-editor-title = Exit Map Editor
exit-map-editor-prompt-unsaved = Exit and lose all unsaved changes?
exit-map-editor-prompt-deleted = The map may have been deleted outside the editor.
exit-map-editor-confirm-anyway = Exit anyway
exit-map-editor-confirm = Exit

## IngamePowerBarLogic
## IngamePowerCounterLogic
power-usage = Power Usage: { $usage }/{ $capacity }
infinite-power = Infinite

## IngameSiloBarLogic
## IngameCashCounterLogic
silo-usage = Silo Usage: { $usage }/{ $capacity }

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

## DownloadPackageLogic
downloading = Downloading { $title }
fetching-mirror-list = Fetching list of mirrors...
downloading-from = Downloading from { $host } { $received } { $suffix }
downloading-from-progress = Downloading from { $host } { $received } / { $total } { $suffix } ({ $progress }%)
unknown-host = unknown host
verifying-archive = Verifying archive...
archive-validation-failed = Archive validation failed
extracting = Extracting...
extracting-entry = Extracting { $entry }
archive-extraction-failed = Archive extraction failed
mirror-selection-failed = Online mirror is not available. Please install from an original disc.

## InstallFromDiscLogic
detecting-drives = Detecting drives
checking-discs = Checking Discs
searching-disc-for = Searching for { $title }
content-package-installation = The following content packages will be installed:
game-discs = Game Discs
digital-installs = Digital Installs
game-content-not-found = Game Content Not Found
alternative-content-sources = Please insert or install one of the following content sources:
installing-content = Installing Content
copying-filename = Copying { $filename }
copying-filename-progress = Copying { $filename } ({ $progress }%)
installation-failed = Installation Failed
check-install-log = Refer to install.log in the logs directory for details.
extracting-filename = Extracting { $filename }
extracting-filename-progress = Extracting { $filename } ({ $progress }%)
cancel = Cancel
retry = Retry

## InstallFromDiscLogic, LobbyLogic
back = Back

# InstallFromDiscLogic, ModContentPromptLogic
continue = Continue

## ModContentLogic
manual-install = Manual Install

## ModContentPromptLogic
quit = Quit

## KickClientLogic
kick-client = Kick { $player }?

## KickSpectatorsLogic
kick-spectators =
    { $count ->
        [one] Are you sure you want to kick one spectator?
       *[other] Are you sure you want to kick { $count } spectators?
    }

## LobbyLogic
add = Add
remove = Remove
configure-bots = Configure Bots
n-teams = { $count } Teams
humans-vs-bots = Humans vs Bots
free-for-all = Free for all
configure-teams = Configure Teams

## LobbyLogic, CommonSelectorLogic, InGameChatLogic
all = All

## InputSettingsLogic, CommonSelectorLogic
none = None

## LobbyLogic, IngameChatLogic
team = Team

## LobbyOptionsLogic
not-available = Not Available

## LobbyUtils
slot = Slot
open = Open
closed = Closed
bots = Bots

# LobbyUtils, Server
bots-disabled = Bots Disabled

## MapPreviewLogic
connecting = Connecting...
downloading-map = Downloading { $size } kB
downloading-map-progress = Downloading { $size } kB ({ $progress }%)
retry-install = Retry Install
retry-search = Retry Search
## also MapChooserLogic
created-by = Created by { $author }

## SpawnSelectorTooltipLogic
disabled-spawn = Disabled spawn
available-spawn = Available spawn

## DisplaySettingsLogic
close = Close
medium = Medium
far = Far
furthest = Furthest

windowed = Windowed
legacy-fullscreen = Fullscreen (Legacy)
fullscreen = Fullscreen

display = Display { $number }

show-on-damage = Show On Damage
always-show = Always Show

automatic = Automatic
manual = Manual

## DisplaySettingsLogic, InputSettingsLogic
disabled = Disabled

## DisplaySettingsLogic, InputSettingsLogic, IntroductionPromptLogic
classic = Classic
modern = Modern
standard = Standard

## DisplaySettingsLogic, IntroductionPromptLogic
inverted = Inverted
joystick = Joystick

alt = Alt
ctrl = Ctrl
meta = Meta
shift = Shift

## SettingsLogic
settings-save-title = Restart Required
settings-save-prompt = Some changes will not be applied until
    the game is restarted.
settings-save-cancel = Continue

restart-title = Restart Now?
restart-prompt = Some changes will not be applied until
    the game is restarted. Restart now?
restart-accept = Restart Now
restart-cancel = Restart Later

reset-title = Reset { $panel }
reset-prompt = Are you sure you want to reset
    all settings in this panel?
reset-accept = Reset
reset-cancel = Cancel

## AssetBrowserLogic
all-packages = All Packages
length-in-seconds = { $length } sec

## ConnectionLogic
connecting-to-endpoint = Connecting to { $endpoint }...
could-not-connect-to-target = Could not connect to { $target }
unknown-error = Unknown error
password-required = Password Required
connection-failed = Connection Failed
mod-switch-failed = Failed to switch mod.

## GameSaveBrowserLogic
rename-save-title = Rename Save
rename-save-prompt = Enter a new file name:
rename-save-accept = Rename

delete-save-title = Delete selected game save?
delete-save-prompt = Delete '{ $save }'
delete-save-accept = Delete

delete-all-saves-title = Delete all game saves?
delete-all-saves-prompt =
    { $count ->
        [one] Delete { $count } save.
       *[other] Delete { $count } saves.
    }
delete-all-saves-accept = Delete All

save-deletion-failed = Failed to delete save file '{ $savePath }'. See the logs for details.

overwrite-save-title = Overwrite saved game?
overwrite-save-prompt = Overwrite { $file }?
overwrite-save-accept = Overwrite

## MainMenuLogic
loading-news = Loading news
news-retrival-failed = Failed to retrieve news: { $message }
news-parsing-failed = Failed to parse news: { $message }

## MapChooserLogic
all-maps = All Maps
no-matches = No matches
player-players =
    { $players ->
        [one] { $players } Player
       *[other] { $players } Players
    }
map-size-huge = (Huge)
map-size-large = (Large)
map-size-medium = (Medium)
map-size-small = (Small)

map-deletion-failed = Failed to delete map '{ $map }'. See the debug.log file for details.

delete-map-title = Delete map
delete-map-prompt = Delete the map '{ $title }'?
delete-map-accept = Delete

delete-all-maps-title = Delete maps
delete-all-maps-prompt = Delete all maps on this page?
delete-all-maps-accept = Delete

order-maps-players = Players
order-maps-date = Map Date

## MissionBrowserLogic
no-video-title = Video not installed
no-video-prompt = The game videos can be installed from the
    "Manage Content" menu in the mod chooser.
no-video-cancel = Back

cant-play-title = Unable to play video
cant-play-prompt = Something went wrong during video playback.
cant-play-cancel = Back

## MusicPlayerLogic
sound-muted = Audio has been muted in settings.
no-song-playing = No song playing

## MuteHotkeyLogic
audio-muted = Audio muted.
audio-unmuted = Audio unmuted.

## PlayerProfileLogic
loading-player-profile = Loading player profile...
loading-player-profile-failed = Failed to load player profile.

## ReplayBrowserLogic
duration = Duration: { $time }
singleplayer = Singleplayer
multiplayer = Multiplayer

victory = Victory
defeat = Defeat

today = Today
last-week = Last 7 days
last-fortnight = Last 14 days
last-month = Last 30 days

replay-duration-very-short = Under 5 min
replay-duration-short = Short (10 min)
replay-duration-medium = Medium (30 min)
replay-duration-long = Long (60+ min)

rename-replay-title = Rename Replay
rename-replay-prompt = Enter a new file name:
rename-replay-accept = Rename

delete-replay-title = Delete selected replay?
delete-replay-prompt = Delete replay { $replay }?
delete-replay-accept = Delete

delete-all-replays-title = Delete all selected replays?
delete-all-replays-prompt =
    { $count ->
        [one] Delete { $count } replay.
       *[other] Delete { $count } replays.
    }
delete-all-replays-accept = Delete All

replay-deletion-failed = Failed to delete replay file '{ $file }'. See the debug.log file for details.

## ReplayUtils
incompatible-replay-title = Incompatible Replay
incompatible-replay-prompt = Replay metadata could not be read.
incompatible-replay-accept = OK
-incompatible-replay-recorded = It was recorded with
incompatible-replay-unknown-version = { -incompatible-replay-recorded } an unknown version.
incompatible-replay-unknown-mod = { -incompatible-replay-recorded } an unknown mod.
incompatible-replay-unavailable-mod = { -incompatible-replay-recorded } an unavailable mod: { $mod }.
incompatible-replay-incompatible-version = { -incompatible-replay-recorded } an incompatible version:
    { $version }.
incompatible-replay-unavailable-map = { -incompatible-replay-recorded } an unavailable map:
    { $map }.

## ServerCreationLogic
internet-server-nat-A = Internet Server (UPnP/NAT-PMP
internet-server-nat-B-enabled = Enabled
internet-server-nat-B-not-supported = Not Supported
internet-server-nat-B-disabled = Disabled
internet-server-nat-C = ):

local-server = Local Server:

server-creation-failed-prompt = Could not listen on port { $port }
server-creation-failed-port-used = Check if the port is already being used.
server-creation-failed-error = Error is: "{ $message }" ({ $code })
server-creation-failed-title = Server Creation Failed
server-creation-failed-cancel = Back

## ServerListLogic
players-online =
    { $players ->
        [one] { $players } Player Online
       *[other] { $players } Players Online
    }

search-status-failed = Failed to query server list.
search-status-no-games = No games found. Try changing filters.
no-server-selected = No Server Selected

map-status-searching = Searching...
map-classification-unknown = Unknown Map

players-label =
    { $players ->
        [0] No Players
        [one] One Player
       *[other] { $players } Players
    }

bots-label =
    { $bots ->
        [0] No Bots
        [one] One Bot
       *[other] { $bots } Bots
    }

## ServerListLogic, ReplayBrowserLogic, ObserverShroudSelectorLogic
players = Players

## ServerListLogic, GameInfoStatsLogic
spectators = Spectators
spectators-label =
    { $spectators ->
        [0] No Spectators
        [one] One Spectator
       *[other] { $spectators } Spectators
    }

## ServerlistLogic, GameInfoStatsLogic, ObserverShroudSelectorLogic, SpawnSelectorTooltipLogic, ReplayBrowserLogic
team-number = Team { $team }
no-team = No Team

playing = Playing
waiting = Waiting

n-other-players =
    { $players ->
        [one] One other player
       *[other] { $players } other players
    }

in-progress-for =
    { $minutes ->
        [0] In progress
        [one] In progress for { $minutes } minute.
       *[other] In progress for { $minutes } minutes.
    }
password-protected = Password protected
waiting-for-players = Waiting for players
server-shutting-down = Server shutting down
unknown-server-state = Unknown server state

## Game
saved-screenshot = Saved screenshot { $filename }

## ChatCommands
invalid-command = { $name } is not a valid command.

## DebugVisualizationCommands
combat-geometry-description = toggles combat geometry overlay.
render-geometry-description = toggles render geometry overlay.
screen-map-overlay-description = toggles screen map overlay.
depth-buffer-description = toggles depth buffer overlay.
actor-tags-overlay-description = toggles actor tags overlay.

## DevCommands
cheats-disabled = Cheats are disabled.
invalid-cash-amount = Invalid amount of cash.
toggle-visibility = toggles visibility checks and minimap.
give-cash = gives the default or specified amount of money.
give-cash-all = gives the default or specified amount of money to all players and ai.
instant-building = toggles instant building.
build-anywhere = toggles the ability to build anywhere.
unlimited-power = toggles infinite power.
enable-tech = toggles the ability to build everything.
fast-charge = toggles almost instant support power charging.
dev-cheat-all = toggles all cheats and gives you some cash for your trouble.
dev-crash = crashes the game.
levelup-actor = adds a specified number of levels to the selected actors.
player-experience = adds a specified amount of player experience to the owner(s) of selected actors.
power-outage = causes owner(s) of selected actors to have a 5 second power outage.
kill-selected-actors = kills selected actors.
dispose-selected-actors = disposes selected actors.

## HelpCommands
available-commands = Here are the available commands:
no-description = no description available.
help-description = provides useful info about various commands

## PlayerCommands
pause-description = pause or unpause the game
surrender-description = self-destruct everything and lose the game

## DeveloperMode
cheat-used = Cheat used: { $cheat } by { $player }{ $suffix }

## CustomTerrainDebugOverlay
custom-terrain-debug-overlay-description = toggles the custom terrain debug overlay.

## CellTriggerOverlay
cell-trigger-overlay-description = toggles the script triggers overlay.

## ExitsDebugOverlay
exits-debug-overlay-description = Displays exits for factories.

## HierarchicalPathFinderOverlay
hpf-overlay-description = toggles the hierarchical pathfinder overlay.

## PathFinderOverlay
path-debug-description = toggles a visualization of path searching.

## TerrainGeometryOverlay
terrain-geometry-overlay = toggles the terrain geometry overlay.
