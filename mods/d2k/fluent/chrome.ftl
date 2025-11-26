## encyclopedia.yaml, mainmenu.yaml
label-mentat-title = Mentat

## ingame-menu.yaml
label-menu-buttons-title = Options

## ingame-observer.yaml
button-observer-widget-options = Options (Esc)
button-replay-player-pause-tooltip = Pause
button-replay-player-play-tooltip = Play

button-replay-player-slow =
    .tooltip = Slow speed
    .label = 50%

button-replay-player-regular =
    .tooltip = Regular speed
    .label = 100%

button-replay-player-fast =
    .tooltip = Fast speed
    .label = 200%

button-replay-player-maximum =
    .tooltip = Maximum speed
    .label = MAX

label-basic-stats-player-header = Player
label-basic-stats-cash-header = Cash
label-basic-stats-power-header = Power
label-basic-stats-kills-header = Kills
label-basic-stats-deaths-header = Deaths
label-basic-stats-assets-destroyed-header = Destroyed
label-basic-stats-assets-lost-header = Lost
label-basic-stats-experience-header = Score
label-basic-stats-actions-min-header = APM
label-economy-stats-player-header = Player
label-economy-stats-cash-header = Cash
label-economy-stats-income-header = Income
label-economy-stats-assets-header = Assets
label-economy-stats-earned-header = Earned
label-economy-stats-spent-header = Spent
label-economy-stats-harvesters-header = Harvesters
label-economy-stats-carryalls-header = Carryalls
label-production-stats-player-header = Player
label-production-stats-header = Production
label-support-powers-player-header = Player
label-support-powers-header = Support Powers
label-army-player-header = Player
label-army-header = Army
label-combat-stats-player-header = Player
label-combat-stats-assets-destroyed-header = Destroyed
label-combat-stats-assets-lost-header = Lost
label-combat-stats-units-killed-header = U. Killed
label-combat-stats-units-dead-header = U. Lost
label-combat-stats-buildings-killed-header = B. Killed
label-combat-stats-buildings-dead-header = B. Lost
label-combat-stats-army-value-header = Army Value
label-combat-stats-vision-header = Vision
label-deliver-in-timer = DELIVERY IN: { $time }

## ingame-observer.yaml, ingame-player.yaml
label-mute-indicator = Audio Muted

## ingame-player.yaml
supportpowers-support-powers-palette =
    .ready = READY
    .hold = ON HOLD

button-command-bar-attack-move =
    .tooltip = Attack Move
    .tooltipdesc =
    Selected units will move to the desired location
    and attack any enemies they encounter en route.

    Hold <(Ctrl)> while targeting to order an Assault Move
    that attacks any units or structures encountered en route.

    Left-click icon then right-click on target location.

button-command-bar-force-move =
    .tooltip = Force Move
    .tooltipdesc =
    Selected units will move to the desired location
     - Default activity for the target is suppressed
     - Vehicles will attempt to crush enemies at the target location
     - Deployed thumpers will undeploy and move to the target location

    Left-click icon then right-click on target.
    Hold <(Alt)> to activate temporarily while commanding units.

button-command-bar-force-attack =
    .tooltip = Force Attack
    .tooltipdesc =
    Selected units will attack the targeted unit or location
     - Default activity for the target is suppressed
     - Allows targeting of own or ally forces

    Left-click icon then right-click on target.
    Hold <(Ctrl)> to activate temporarily while commanding units.

button-command-bar-guard =
    .tooltip = Guard
    .tooltipdesc =
    Selected units will follow the targeted unit.

    Left-click icon then right-click on target unit.

button-command-bar-deploy =
    .tooltip = Deploy
    .tooltipdesc =
    Selected units will perform their default deploy activity
     - MCVs will unpack into a Construction Yard
     - Thumpers will start or stop attracting worms
     - Devastators will become immobilized and explode

    Acts immediately on selected units.

button-command-bar-scatter =
    .tooltip = Scatter
    .tooltipdesc =
    Selected units will stop their current activity
    and move to a nearby location.

    Acts immediately on selected units.

button-command-bar-stop =
    .tooltip = Stop
    .tooltipdesc =
    Selected units will stop their current activity.
    Selected buildings will reset their rally point.

    Acts immediately on selected targets.

button-command-bar-queue-orders =
    .tooltip = Waypoint Mode
    .tooltipdesc =
    Use Waypoint Mode to give multiple linking commands
    to the selected units. Units will execute the commands
    immediately upon receiving them.

    Left-click icon then give commands in the game world.
    Hold <(Shift)> to activate temporarily while commanding units.

button-stance-bar-attackanything =
    .tooltip = Attack Anything Stance
    .tooltipdesc =
    Set the selected units to Attack Anything stance:
     - Units will attack enemy units and structures on sight
     - Units will pursue attackers across the battlefield

button-stance-bar-defend =
    .tooltip = Defend Stance
    .tooltipdesc =
    Set the selected units to Defend stance:
     - Units will attack enemy units on sight
     - Units will not move or pursue enemies

button-stance-bar-returnfire =
    .tooltip = Return Fire Stance
    .tooltipdesc =
    Set the selected units to Return Fire stance:
     - Units will retaliate against enemies that attack them
     - Units will not move or pursue enemies

button-stance-bar-holdfire =
    .tooltip = Hold Fire Stance
    .tooltipdesc =
    Set the selected units to Hold Fire stance:
     - Units will not fire upon enemies
     - Units will not move or pursue enemies

button-top-buttons-repair-tooltip = Repair
button-top-buttons-sell-tooltip = Sell
button-top-buttons-beacon-tooltip = Place Beacon
button-top-buttons-power-tooltip = Power Down
button-top-buttons-options-tooltip = Options

productionpalette-sidebar-production-palette =
    .ready = READY
    .hold = ON HOLD

purchase-panel-button-tooltip = Order selected units
purchase-panel-label-delivery = DELIVERY IN:

button-production-types-building-tooltip = Buildings
button-production-types-infantry-tooltip = Infantry
button-production-types-vehicle-tooltip = Light Vehicles
button-production-types-tanks-tooltip = Heavy Vehicles
button-production-types-aircraft-tooltip = Aircraft
button-production-types-starport-tooltip = Starport
button-production-types-upgrade-tooltip = Upgrades
button-production-types-scroll-up-tooltip = Scroll up
button-production-types-scroll-down-tooltip = Scroll down

## aibattle.yaml
button-ai-battle = AI Battle
label-ai-battle-title = AI Battle Test
label-ai-battle-map = Map
label-ai-battle-players = AI Players
label-ai-battle-sim-speed = Simulation Speed
button-ai-battle-change-map = Change Map
button-ai-battle-start = Start Battle
label-ai-battle-header-slot = #
label-ai-battle-header-ai = AI
label-ai-battle-header-faction = Faction
label-ai-battle-header-team = Team
label-ai-battle-game-options = Game Options
label-ai-battle-explored-map = Explored Map
label-ai-battle-fog-of-war = Fog of War
label-ai-battle-starting-cash = Starting Cash:
label-ai-battle-game-speed = Game Speed:
tooltip-ai-battle-explored-map = Players will start with the map fully revealed
tooltip-ai-battle-fog-of-war = Enable fog of war (enemy units hidden when not in view)

## AI Battle speed controls (ingame-observer.yaml)
button-ai-battle-pause-tooltip = Pause simulation
button-ai-battle-play-tooltip = Resume simulation

button-ai-battle-1x-tooltip = Real-time playback (1x speed)
button-ai-battle-2x-tooltip = Fast playback (2x speed)
button-ai-battle-4x-tooltip = Faster playback (4x speed)
button-ai-battle-8x-tooltip = Very fast playback (8x speed)
button-ai-battle-16x-tooltip = Ultra fast playback (16x speed)
button-ai-battle-32x-tooltip = Extreme playback (32x speed)
button-ai-battle-64x-tooltip = Near maximum playback (64x speed)
button-ai-battle-128x-tooltip = Maximum playback speed (128x)

button-ai-battle-1x =
    .label = 1x

button-ai-battle-2x =
    .label = 2x

button-ai-battle-4x =
    .tooltip = Faster playback (4x speed)
    .label = 4x

button-ai-battle-8x =
    .label = 8x

button-ai-battle-16x =
    .label = 16x

button-ai-battle-32x =
    .label = 32x

button-ai-battle-64x =
    .label = 64x

button-ai-battle-128x =
    .label = 128x

## aibattle-results.yaml
label-ai-battle-results-title = Battle Results
label-ai-battle-winner = Winner
label-ai-battle-duration = Duration:
label-ai-battle-damage = Damage
label-stats-player = Player
button-ai-battle-watch-replay = Watch Replay

## ingame-aibattle-replay.yaml
label-ai-battle-replay-title = AI Battle Replay
label-ai-battle-rewinding = Rewinding to { $time }...
label-ai-battle-seeking = Seeking to { $time }...
label-ai-battle-statistics = Statistics
label-ai-battle-stats = Stats
label-no-fog = Disable Shroud
label-combined-vision = All AI Vision
