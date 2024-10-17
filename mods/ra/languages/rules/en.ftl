## player.yaml
options-tech-level =
    .infantry-only = Infantry Only
    .low = Low
    .medium = Medium
    .no-superweapons = No Superweapons
    .unrestricted = Unrestricted

checkbox-kill-bounties =
    .label = Kill Bounties
    .description = Players receive cash bonuses when killing enemy units

checkbox-redeployable-mcvs =
    .label = Redeployable MCVs
    .description = Allow undeploying Construction Yard

checkbox-reusable-engineers =
    .label = Reusable Engineers
    .description = Engineers remain on the battlefield after capturing a structure

notification-insufficient-funds = Insufficient funds.
notification-new-construction-options = New construction options.
notification-cannot-deploy-here = Cannot deploy here.
notification-low-power = Low power.
notification-base-under-attack = Base under attack.
notification-ally-under-attack = Our ally is under attack.
notification-silos-needed = Silos needed.

## world.yaml
options-starting-units =
    .mcv-only = MCV Only
    .light-support = Light Support
    .heavy-support = Heavy Support

resource-minerals = Valuable Minerals

## Faction
faction-allies =
    .name = Allies

faction-england =
    .name = England
    .description = England: Counterintelligence
     Special Unit: British Spy
     Special Unit: Mobile Gap Generator

faction-france =
    .name = France
    .description = France: Deception
     Special Ability: Can build fake structures
     Special Unit: Phase Transport

faction-germany =
    .name = Germany
    .description = Germany: Technology
     Special Ability: Advanced Chronoshift
     Special Unit: Chrono Tank

faction-soviet =
    .name = Soviet

faction-russia =
    .name = Russia
    .description = Russia: Tesla Weapons
     Special Unit: Tesla Tank
     Special Unit: Shock Trooper

faction-ukraine =
    .name = Ukraine
    .description = Ukraine: Demolitions
     Special Ability: Parabombs
     Special Unit: Demolition Truck

faction-random =
    .name = Any
    .description = Random Country
     A random country will be chosen when the game starts.

faction-randomallies =
    .name = Allies
    .description = Random Allied Country
     A random Allied country will be chosen when the game starts.

faction-randomsoviet =
    .name = Soviet
    .description = Random Soviet Country
     A random Soviet country will be chosen when the game starts.

## aircraft.yaml
actor-badr-name = Badger

actor-mig =
   .description = Fast Ground-Attack Plane.
      Strong vs Buildings, Vehicles
      Weak vs Infantry, Aircraft
   .name = MiG Attack Plane
   .encyclopedia = A tricky aircraft to control, the MiG fires missiles from a distance while circling. With careful control, it is good at destroying enemy harvesters.

actor-yak =
   .description = Attack Plane armed with
    dual machine guns.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Yak Attack Plane
   .encyclopedia = Require forward momentum to fire, keeping them within the enemy’s weapon range while attacking.
   This makes them less durable and prone to being shot down mid-attack, reducing their effectiveness against organized forces unless they have overwhelming numbers or are used for a kamikaze strike on high-value targets.

actor-tran =
   .description = Fast Infantry Transport Helicopter.
      Unarmed
   .name = Chinook
   .encyclopedia = Functions like an airborne APC, transporting units across the battlefield.

actor-heli =
   .description = Helicopter gunship armed
    with multi-purpose missiles.
      Strong vs Buildings, Vehicles, Aircraft
      Weak vs Infantry
   .name = Longbow
   .encyclopedia = The only anti-aircraft airborne unit, ensuring air superiority for the Allies within its operational range.

actor-hind =
   .description = Helicopter gunship armed
    with dual chainguns.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Hind

actor-u2-name = Spy Plane
   .encyclopedia = Reveals a wide area of vision for a brief area of time.

actor-mh60 =
   .description = Helicopter gunship armed
    with dual chainguns.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Black Hawk
   .encyclopedia = The distinctive "rararararar" sound of a Black Hawk strikes fear into your enemies, making them cower in their bases.
   A well-microed Black Hawk is unparalleled, excelling against small groups or cleaning up infantry after a large engagement.

## civilian.yaml
actor-c10-name = Scientist
actor-tecn-name = Technician
actor-tecn2-name = Technician
actor-v01-name = Church
actor-v19-name = Oil Pump
actor-v19-husk-name = Husk (Oil Pump)
actor-barl-name = Explosive Barrel
actor-brl3-name = Explosive Barrel
actor-v25-name = Church
actor-lhus-name = Lighthouse
actor-windmill-name = Windmill

## decoration.yaml
actor-ice01-name = Ice Floe
actor-ice02-name = Ice Floe
actor-ice03-name = Ice Floe
actor-ice04-name = Ice Floe
actor-ice05-name = Ice Floe
actor-utilpol1-name = Utility Pole
actor-utilpol2-name = Utility Pole
actor-tanktrap1-name = Tank Trap
actor-tanktrap2-name = Tank Trap

## defaults.yaml
notification-unit-lost = Unit lost.
notification-airborne-unit-lost = Airborne Unit lost.
notification-naval-unit-lost = Naval Unit lost.
notification-unit-promoted = Unit promoted.
notification-primary-building-selected = Primary building selected.
notification-structure-captured = Structure captured.
notification-unit-stolen = Unit stolen.

meta-vehicle-generic-name = Vehicle
meta-infantry-generic-name = Soldier
meta-civinfantry-name = Civilian
meta-ship-generic-name = Ship
meta-neutralplane-generic-name = Plane
meta-helicopter-generic-name = Helicopter
meta-basicbuilding-generic-name = Structure
meta-techbuilding-name = Civilian Building
meta-ammobox-name = Ammo Box
meta-civfield-name = Field

meta-civhaystackorigloo =
   .winter-name = Igloo
   .summer-name = Haystacks

meta-tree-name = Tree
meta-treehusk-name = Tree (Burnt)
meta-box-name = Boxes
meta-husk-generic-name = Destroyed Vehicle
meta-planehusk-generic-name = Destroyed Plane
meta-helicopterhusk-generic-name = Destroyed Helicopter
meta-bridge-name = Bridge
meta-rock-name = Rock

meta-crate =
   .name = Crate
   .generic-name = Crate

meta-mine-name = Mine

## fakes.yaml
meta-fake-encyclopedia = Mimics a building with a fraction of its health, appearing fully intact until it is destroyed.

actor-fpwr =
   .description = Looks like a Power Plant.
   .name = Fake Power Plant
   .generic-name = Power Plant

actor-tenf =
   .description = Looks like an Allied Barracks.
   .name = Fake Allied Barracks
   .generic-name = Allied Barracks

actor-syrf =
   .description = Looks like a Naval Yard.
   .name = Fake Naval Yard
   .generic-name = Naval Yard

actor-spef =
   .description = Looks like a Sub Pen.
   .name = Fake Sub Pen
   .generic-name = Sub Pen

actor-weaf =
   .description = Looks like a War Factory.
   .name = Fake War Factory
   .generic-name = War Factory

actor-domf =
   .name = Fake Radar Dome
   .generic-name = Radar Dome
   .description = Looks like a Radar Dome.

actor-fixf =
   .description = Looks like a Service Depot.
   .name = Fake Service Depot
   .generic-name = Service Depot

actor-fapw =
   .description = Looks like an Advanced Power Plant.
   .name = Fake Advanced Power Plant
   .generic-name = Advanced Power Plant

actor-atef =
   .name = Fake Allied Tech Center
   .generic-name = Allied Tech Center
   .description = Looks like an Allied Tech Center.

actor-pdof =
   .name = Fake Chronosphere
   .generic-name = Chronosphere
   .description = Looks like a Chronosphere.
    Maximum 1 can be built.

actor-mslf =
   .name = Fake Missile Silo
   .generic-name = Missile Silo
   .description = Looks like a Missile Silo.
    Maximum 1 can be built.

actor-facf =
   .description = Looks like a Construction Yard.
   .name = Fake Construction Yard
   .generic-name = Construction Yard

## husks.yaml
actor-2tnk-husk-name = Husk (Medium Tank)
actor-3tnk-husk-name = Husk (Heavy Tank)
actor-4tnk-husk-name = Husk (Mammoth Tank)
actor-harv-fullhusk-name = Husk (Ore Truck)
actor-harv-emptyhusk-name = Husk (Ore Truck)
actor-mcv-husk-name = Husk (Mobile Construction Vehicle)
actor-mgg-husk-name = Husk (Mobile Gap Generator)
actor-tran-husk-name = Chinook
actor-tran-husk1-name = Husk (Chinook)
actor-tran-husk2-name = Husk (Chinook)
actor-badr-husk-name = Badger
actor-mig-husk-name = MiG Attack Plane
actor-yak-husk-name = Yak Attack Plane
actor-heli-husk-name = Longbow
actor-hind-husk-name = Hind
actor-u2-husk-name = Husk (Spy Plane)
actor-mh60-husk-name = Black Hawk

## infantry.yaml
notification-building-infiltrated = Building infiltrated.

actor-dog =
   .description = Anti-infantry unit.
    Can detect spies.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
   .name = Attack Dog
   .generic-name = Dog
   .encyclopedia = The only vision unit available before early vehicles arrive, it has low health. When encountering groups of infantry, it may enter a "killing spree," swiftly jumping from one enemy to the next. Even when camouflaged, it can easily detect and kill a Spy.

actor-e1 =
   .description = General-purpose infantry.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
   .name = Rifle Infantry
   .encyclopedia = A basic infantry unit, the Rifleman is inexpensive and quick to train, making it the backbone of your early-game army alongside Rocket Soldiers.

actor-e2 =
   .description = Infantry armed with grenades.
      Strong vs Buildings, Infantry
      Weak vs Vehicles, Aircraft
   .name = Grenadier
   .encyclopedia = Dealing less anti-structure damage than a flamethrower, it struggles to damage anything outside of buildings. Avoid grouping them together, as a single death can trigger a chain explosion.

actor-e3 =
   .description = Anti-tank/Anti-aircraft infantry.
      Strong vs Vehicles, Aircraft
      Weak vs Infantry
   .name = Rocket Soldier
   .encyclopedia = Equipped with powerful dragon rockets that easily destroy tanks and, when manually targeted, can take down air units from a distance. However, its rockets surprisingly struggle against infantry.

actor-e4 =
   .description = Advanced anti-structure unit.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
   .name = Flamethrower
   .encyclopedia = Incinerates infantry and demolishes structures in a blazing inferno. However, this fire will also harm your own troops, so keep them well away!

actor-e6 =
   .description = Infiltrates and captures
    enemy structures.
      Unarmed
   .name = Engineer
   .encyclopedia = Engineer: Captures neutral or enemy structures. Capturing an enemy faction's MCV grants access to both factions' technology. They can instantly repair any structure at the cost of their life.

actor-spy =
   .description = Infiltrates enemy structures for intel or
    sabotage. Exact effect depends on the
    building infiltrated.
    Loses disguise when attacking.
    Can detect spies.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
      Special Ability: Disguised
   .disguisetooltip-name = Spy
   .disguisetooltip-generic-name = Soldier
   .encyclopedia = Prepares an atomic bomb for launch. The explosion vaporizes everything within the immediate blast radius, with damage decreasing outward.
   Armored vehicles have a higher survival chance.

actor-spy-england-disguisetooltip =
   .name = British Spy
   .encyclopedia = Cheaper than a Spy, but just as effective.

actor-e7 =
   .description = Elite commando infantry. Armed with
    dual pistols and C4.
    Maximum 1 can be trained.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
      Special Ability: Destroy Building with C4
   .name = Tanya
   .encyclopedia = Tanya: Wipes out infantry infantry with her dual .45s and demolishes buildings with C4 explosives.

actor-medi =
   .description = Heals nearby infantry.
      Unarmed
   .name = Medic
   .encyclopedia = Medic: Heals infantry within its radius and works faster when in greater numbers, but cannot heal itself or while moving.

actor-mech =
   .description = Repairs nearby vehicles and restores
    husks to working condition by capturing them.
      Unarmed
   .name = Mechanic
   .encyclopedia = Repairs vehicles and salvages husks to restore them to working order at a fraction of their health.

actor-einstein-name = Prof. Einstein
actor-delphi-name = Agent Delphi
actor-chan-name = Scientist
actor-gnrl-name = General

actor-thf =
   .description = Steals enemy credits.
    Hijacks enemy vehicles.
      Unarmed
   .name = Thief
   .encyclopedia = Steals half of the enemy’s funds from refineries or silos—or your money back if there’s nothing to take. Also skilled at breaking into construction sites, stealing vehicles, and driving off with them.

actor-shok =
   .description = Elite infantry with portable Tesla coils.
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
   .name = Shock Trooper
   .encyclopedia = Only limited by build time and cost, Shock Troopers unleash devastating electric bursts on ground units before reloading when gathered in large numbers.

actor-zombie =
   .name = Zombie
   .description = Slow undead. Attacks in close combat.

actor-ant =
   .name = Giant Ant
   .generic-name = Ant
   .description = Irradiated insect that grew oversize.

actor-fireant-name = Fire Ant
actor-scoutant-name = Scout Ant
actor-warriorant-name = Warrior Ant

## misc.yaml
notification-sonar-pulse-ready = Sonar pulse ready.

actor-moneycrate-name = Money Crate
actor-healcrate-name = Heal Crate
actor-wcrate-name = Wooden Crate
actor-scrate-name = Steel Crate
actor-camera-name = (reveals area to owner)
actor-camera-paradrop-name = (support power proxy camera)
actor-camera-spyplane-name = (support power proxy camera)
actor-sonar-name = (support power proxy camera)
actor-flare-name = Flare
actor-mine-name = Ore Mine
actor-gmine-name = Gem Mine
actor-railmine-name = Abandoned Mine
actor-quee-name = Queen Ant
actor-lar1-name = Ant Larva
actor-lar2-name = Ant Larvae
actor-mpspawn-name = (multiplayer player starting point)
actor-waypoint-name = (waypoint for scripted behavior)
actor-ctflag-name = Flag

## ships.yaml
actor-ss =
   .description = Submerged anti-ship unit
    armed with torpedoes.
    Can detect other submarines.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
      Special Ability: Submerge
   .name = Submarine
   .encyclopedia = Cannot be targeted while underwater but must surface to fire, making it vulnerable during that time. It can be revealed by infantry, ground defenses, or a Spy Plane.

actor-msub =
   .description = Submerged anti-ground siege unit
    with anti-air capabilities.
    Can detect other submarines.
      Strong vs Buildings, Ground units, Aircraft
      Weak vs Naval units
      Special Ability: Submerge
   .name = Missile Submarine
   .encyclopedia = Possesses anti-air capabilities and can shell land units, though it has a shorter range and inflicts less damage than a Cruiser. It cannot engage other vessels in combat.

actor-dd =
   .description = Fast multi-role ship.
    Can detect submarines.
      Strong vs Naval units, Vehicles, Aircraft
      Weak vs Infantry
   .name = Destroyer
   .encyclopedia = More powerful than the Gunboat and equipped with anti-air capabilities.

actor-ca =
   .description = Very slow long-range ship.
      Strong vs Buildings, Ground units
      Weak vs Naval units, Aircraft
   .name = Cruiser
   .encyclopedia = Relies on other ships for protection, as it cannot engage marine vessels or aircraft. It excels at inflicting heavy damage on land units from a distance.

actor-lst =
   .description = General-purpose naval transport.
    Can carry infantry and tanks.
      Unarmed
   .name = Transport
   .encyclopedia = Carries a handful of ground units across water. These can only be loaded or unloaded on shore terrain and are most vulnerable while unloading.

actor-pt =
   .description = Light scout & support ship.
    Can detect submarines.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
   .name = Gunboat
   .encyclopedia =  A fast, lightly armored warship capable of attacking ships and submarines.

## structures.yaml
notification-construction-complete = Construction complete.
notification-unit-ready = Unit ready.
notification-unable-to-build-more = Unable to build more.
notification-unable-to-comply-building-in-progress = Unable to comply. Building in progress.
notification-repairing = Repairing.
notification-unit-repaired = Unit repaired.
notification-select-target = Select target.
notification-insufficient-power = Insufficient power.
notification-reinforcements-have-arrived = Reinforcements have arrived.
notification-abomb-prepping = A-bomb prepping.
notification-abomb-ready = A-bomb ready.
notification-abomb-launch-detected = A-bomb launch detected.
notification-iron-curtain-charging = Iron curtain charging.
notification-iron-curtain-ready = Iron curtain ready.
notification-chronosphere-charging = Chronosphere charging.
notification-chronosphere-ready = Chronosphere ready.
notification-satellite-launched = Satellite launched.
notification-credits-stolen = Credits stolen.
notification-spy-plane-ready = Spy plane ready.

actor-mslo =
   .name = Missile Silo
   .description = Provides an atomic bomb.
    Requires power to operate.
    Maximum 1 can be built.
      Special Ability: Atom Bomb
   .nukepower-name = Atom Bomb
   .nukepower-description = Launches a devastating atomic bomb
    at the target location
   .encyclopedia = Prepares an atomic bomb for launch on a timer. The explosion vaporizes everything within the immediate blast radius, with damage decreasing outward. Armored vehicles have a higher survival chance.

actor-gap =
   .name = Gap Generator
   .description = Obscures the enemy's view with a shroud
    Requires power to operate
   .encyclopedia = Generates an impenetrable black shroud that reduces the vision of most units.

actor-spen =
   .name = Sub Pen
   .description = Produces and repairs
    submarines and transports
   .encyclopedia = Constructs and repairs transports and submarines.

actor-syrd =
   .description = Produces and repairs
    ships and transports.
   .name = Naval Yard
   .encyclopedia = Constructs and repairs transports and surface warships. Build 7 for maximum production.

actor-iron =
   .description = Makes a group of units invulnerable
    for a short time.
    Requires power to operate.
    Maximum 1 can be built.
      Special Ability: Invulnerability
   .name = Iron Curtain
   .grantexternalconditionpower-ironcurtain-name = Invulnerability
   .grantexternalconditionpower-ironcurtain-description = Grants invulnerability to a group of units
    for 20 seconds
   .encyclopedia = Grants vehicles and buildings within its cross-shaped area temporary invulnerability for a while.

actor-pdox =
   .description = Teleports a group of units across the
    map for a short time.
    Requires power to operate.
    Maximum 1 can be built.
      Special Ability: Chronoshift
   .name = Chronosphere
   .chronoshiftpower-chronoshift-name = Chronoshift
   .chronoshiftpower-chronoshift-description = Teleports a group of units across
    the map for 20 seconds
   .encyclopedia = Teleports up to 5 units to a new location temporarily before returning them to their original position.
   .chronoshiftpower-advancedchronoshift-name = Advanced Chronoshift
   .chronoshiftpower-advancedchronoshift-description = Teleports a large group of units across
    the map for 20 seconds
   .encyclopedia = Teleports up to 13 units.

actor-tsla =
   .description = Advanced base defense.
    Requires power to operate.
    Can detect cloaked units.
      Strong vs Vehicles, Infantry
      Weak vs Aircraft
   .name = Tesla Coil
   .encyclopedia = Deals greater damage than a Turret with three shots per burst, though it has a longer reload time.

actor-agun =
   .description = Anti-Air base defense.
    Requires power to operate.
      Strong vs Aircraft
      Weak vs Ground units
   .name = AA Gun
   .encyclopedia = An AA gun fires much quicker than a SAM. It offers nearly instant hits and slightly greater range, but has reduced vision.

actor-dome =
   .description = Provides an overview
    of the battlefield.
    Requires power to operate.
   .name = Radar Dome
   .encyclopedia = A rapid-fire cannon encased in concrete, ideal for mowing down infantry.

actor-pbox =
   .name = Pillbox
   .description = Static defense with a fireport for
    a garrisoned soldier
    Capable of detecting cloaked units
      Strong vs. Infantry and Light armor
      Weak vs. Tanks and Aircraft
   .encyclopedia = A rapid-fire cannon encased in concrete, ideal for mowing down infantry.

actor-hbox =
   .name = Camo Pillbox
   .description = Camouflaged static defense with a fireport
    for a garrisoned soldier
    Can detect cloaked units
      Strong vs. Infantry and Light armor
      Weak vs. Tanks and Aircraft
   .encyclopedia = Camo Pillboxes remain hidden until they fire or are detected, making them useful for misleading opponents about your defenses.
   They are also effective against artillery and V2 units, as they must be detected first.

actor-gun =
   .description = Anti-Armor base defense.
    Can detect cloaked units.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
   .name = Turret
   .encyclopedia = A cannon mounted on a turret that delivers moderate damage to vehicles with single-shot bursts.

actor-ftur =
   .description = Anti-Infantry base defense.
    Can detect cloaked units.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Flame Tower
   .encyclopedia = Most effective when used in groups, maximizing the area-of-effect damage its fireballs inflict on clustered infantry.

actor-sam =
   .description = Anti-Air base defense.
    Requires power to operate.
      Strong vs Aircraft
      Weak vs Ground units
   .name = SAM Site
   .encyclopedia = SAM missiles have a shorter range but better tracking and greater vision.

actor-atek =
   .description = Provides Allied advanced technologies.
      Special Ability: GPS Satellite
   .name = Allied Tech Center
   .gpspower-name = GPS Satellite
   .gpspower-description = Reveals map terrain and provides tactical information
   information. = Requires power and active radar
   .description = Requires power and active radar
   .encyclopedia = Temporarily reveals the locations of structures and units on the map. Power and radar must be on at the same time.

actor-weap =
   .description = Produces vehicles.
   .name = War Factory
   .encyclopedia = Constructs vehicles. Build 4 for maximum production.

actor-fact =
   .description = Produces structures.
   .name = Construction Yard
   .encyclopedia = Enables construction of other structures within a designated area, with walls being an exception.
   New structures can only be placed within one cell of an existing structure.

actor-proc =
   .description = Refines Ore and Gems
    into credits.
   .name = Ore Refinery
   .descriptioin = Processes ore into cash and stores more credits than a Silo

actor-silo =
   .description = Stores excess refined
    Ore and Gems.
   .name = Silo
   .encyclopedia = Stores credits.

actor-hpad =
   .description = Produces and reloads
    helicopters.
   .name = Helipad
   .encyclopedia =  Constructs and reloads helicopters.

actor-afld =
   .description = Produces and reloads aircraft.
      Special Ability: Spy Plane
      Special Ability: Paratroopers
   .name = Airfield
   .airstrikepower-spyplane-name = Spy Plane
   .airstrikepower-spyplane-description = Reveals an area of the map
   .encyclopedia = Reveals an area of the map for a brief period of time.
   .paratrooperspower-paratroopers-name = Paratroopers
   .paratrooperspower-paratroopers-description = A Badger drops a squad of infantry
    at the selected location.
   .encyclopedia = Drops 3 Riflemen and 2 Rocket Soldiers, all at veterancy 1.
   .airstrikepower-parabombs-name = Parabombs
   .airstrikepower-parabombs-description = A Badger drops parachuted bombs
    at the selected location.
   .encyclopedia = Exclusive to Ukraine, this ability is unlocked with an airfield. A single Badger drops bombs in a straight line.

actor-afld-ukraine-description = Produces and reloads aircraft.
      Special Ability: Spy Plane
      Special Ability: Paratroopers
      Special Ability: Parabombs

actor-powr =
   .description = Provides power for other structures.
   .name = Power Plant
   .encyclopedia = Generates 100 power per plant, with output directly tied to its condition. Protect these structures to avoid low power mode.

actor-apwr =
   .description = Provides double the power of
    a standard Power Plant.
   .name = Advanced Power Plant
   .encyclopedia = Provides double the power of a standard Power Plant.

actor-stek =
   .description = Provides Soviet advanced technologies.
   .name = Soviet Tech Center
   .encyclopedia = Unlocks tier three Soviet units. Requires less power than its Allied version and has greater health.

actor-barr =
   .description = Trains infantry.
   .name = Soviet Barracks
   .encyclopedia = Trains Soviet infantry units. Build 7 for maximum production.

actor-kenn =
   .description = Trains attack dogs.
   .name = Kennel
   .encyclopedia = Trains attack dogs.

actor-tent =
   .description = Trains infantry.
   .name = Allied Barracks
   .encyclopedia = Trains Allied infantry units. Build 7 for maximum production.

actor-fix =
   .description = Repairs vehicles for credits.
   .name = Service Depot
   .encyclopedia = Repairs vehicles, aircraft, and deploys mines. Units can be set to move to a rally point after using the depot.

actor-sbag =
   .description = Stops infantry and light vehicles.
    Can be crushed by tanks.
   .name = Sandbag Wall
   .encyclopedia = Can be crushed by all types of vehicle, stops infantry. Stronger than Sandbags.

actor-fenc =
   .description = Stops infantry and light vehicles.
    Can be crushed by tanks.
   .name = Wire Fence
   .encyclopedia = Can be crushed by all types of vehicle, stops infantry. Stronger than Sandbags.

actor-brik =
   .description = Stop units and blocks enemy fire.
   .name = Concrete Wall
   .encyclopedia = Blocks nearly all ground units. Only a few units can fire over it. Can only be crushed by Mammoth Tanks.

actor-cycl-name = Chain-Link Barrier
actor-barb-name = Barbed-Wire Fence
actor-wood-name = Wooden Fence
actor-barracks-name = Infantry Production
actor-techcenter-name = Tech Center
actor-anypower-name = Any Power Generation

## vehicles.yaml
actor-v2rl =
   .description = Long-range rocket artillery.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
   .name = V2 Rocket Launcher
   .encyclopedia = Bigger explosions, greater durability, faster maneuverability, and more accurate firing than artillery.

actor-1tnk =
   .description = Fast tank, good for scouting.
      Strong vs Light armor
      Weak vs Infantry, Tanks, Aircraft
   .name = Light Tank
   .generic-name = Tank
   .encyclopedia = Excels in direct confrontations with light vehicles. Although lightly armored, it can crush units and inflict moderate damage on structures.

actor-2tnk =
   .description = Allied Main Battle Tank.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
   .name = Medium Tank
   .generic-name = Tank
   .encyclopedia = Medium Tanks are faster in both build time and speed. They offer better armor, making them effective for head-on engagements with Soviets.
   Their numbers enable them to crush more effectively, though husks can hinder this ability. Medium Tanks can also distribute their armor efficiently, giving them an advantage on larger maps.
   They are the first heavily armored tanks to enter the battlefield, and their speed allows them to quickly engage or disengage as needed, making them ideal for chasing down harvesters and MCVs.

actor-3tnk =
   .description = Soviet Main Battle Tank, with dual cannons
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
   .name = Heavy Tank
   .generic-name = Tank
   .encyclopedia = Heavy Tanks possess greater durability and higher burst damage than their Allied counterparts, allowing them to win 1:1 confrontations with Medium Tanks.
   Their ability to absorb more damage makes them highly effective at harassing harvesters, and their greater health allows them to either retreat or push through enemy lines.

actor-4tnk =
   .description = Big and slow tank, with anti-air capability.
    Can crush concrete walls.
      Strong vs Vehicles, Infantry, Aircraft
      Weak vs Nothing
   .name = Mammoth Tank
   .generic-name = Tank
   .encyclopedia = Often misunderstood as the strongest unit in Red Alert, the Mammoth Tank is actually a versatile all-rounder that doesn’t excel in any specific area.
   Its dual weapons allow it to continuously kite infantry, target aircraft, or engage tanks.

actor-arty =
   .description = Long-range artillery.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
   .name = Artillery
  .description = Similar to its counterpart, a single unit can halt infantry pushes when well protected. It has a higher fire rate and longer range.

actor-harv =
   .description = Collects Ore and Gems for processing.
      Unarmed
   .name = Ore Truck
   .generic-name = Harvester
   .encyclopedia =Harvests ore and transports it to a refinery. Though heavily armored, its slow speed makes it vulnerable to enemy attacks. Protecting Ore Trucks is essential; otherwise, your economy will quickly suffer.

actor-mcv =
   .description = Deploys into another Construction Yard.
      Unarmed
   .name = Mobile Construction Vehicle
   .encyclopedia = mobile Construction Yard, like other vehicles, gains speed on roads— a useful tip for quick escapes. However, it has significantly less health in this form.

actor-jeep =
   .description = Fast scout & anti-infantry vehicle.
    Can carry one infantry.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
   .name = Ranger
   .encyclopedia = The best scouting vehicle until air units are available. It swiftly maneuvers within enemy lines and can transport a single unit.

actor-apc =
   .description = Tough infantry transport.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Armored Personnel Carrier
   .encyclopedia = A lightly armored vehicle capable of crushing enemies and carrying up to 10 infantry units.

actor-mnly =
   .description = Lays mines to destroy
    unwary enemy units.
    Can detect mines.
      Unarmed
   .name = Minelayer
   .Minelayer = Destroys any unit that triggers up to two mines. Entire minefields can be deployed automatically. A single Minelayer is enough to reveal enemy mines, which can then be cleared by force firing them.

actor-truk =
   .description = Transports cash to other players.
      Unarmed
   .name = Supply Truck
   .encyclopedia = Single-use truck that transports a small amount of cash.

actor-mgg =
   .description = Regenerates the shroud nearby,
    obscuring the area.
      Unarmed
   .name = Mobile Gap Generator
   .encyclopedia = Aside from being mobile, it functions like its namesake. It's useful for creating decoy army movements or hiding units. Its shroud can be seen through the fog of war and toggled on or off with the deploy key.

actor-mrj =
   .name = Mobile Radar Jammer
   .description = Jams nearby enemy radar domes
    and deflects incoming missiles.
      Unarmed
   .encyclopedia = Ability to deflect any guided missile within its inner circle compensates for its fragility. The outer circle jams the enemy’s Radar Dome.

actor-ttnk =
   .description = Tank with mounted Tesla coil.
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
   .name = Tesla Tank
   .generic-name = Tank
   .encyclopedia = A mobile Tesla Coil capable of crushing units. When paired with Flak Trucks and/or an Iron Curtain, it forms one of the most powerful unit combinations in the game.

actor-ftrk =
   .description = Mobile unit with mounted Flak cannon.
      Strong vs Infantry, Light armor, Aircraft
      Weak vs Tanks
   .name = Mobile Flak
   .encyclopedia = The mobile air advantage the Allies lack, its superior range allows it to target unsupported infantry without taking return fire.

actor-dtrk =
   .description = Truck with actively armed nuclear
    explosives. Has very weak armor.
   .name = Demolition Truck
   .encyclopedia = A slow and fragile vehicle that can be detonated by a single infantry unit, yet it carries a tactical nuke capable of obliterating any army within its blast radius.

actor-ctnk =
   .description = Armed with anti-ground missiles.
    Teleports to areas within range.
      Strong vs Vehicles, Buildings
      Weak vs Infantry, Aircraft
      Special ability: Can teleport
   .name = Chrono Tank
   .generic-name = Tank
   .encyclopedia = A lightly armored tank that can teleport. When used in groups or with a Chronosphere, they can crush enemy forces and teleport away before taking significant damage.

actor-qtnk =
   .description = Deals seismic damage to nearby vehicles
    and structures.
      Strong vs Vehicles, Buildings
      Weak vs Infantry, Aircraft
   .name = MAD Tank
   .generic-name = Tank
   .encyclopedia = Deals seismic damage to structures upon deployment, destroying itself in the process. It is most effective when used in groups of three, as a single MAD Tank deals a fraction of damage to a structure's health.

actor-stnk =
   .description = Lightly armored infantry transport which
    can cloak. Armed with anti-ground missiles.
      Strong vs Light armor
      Weak vs Infantry, Tanks, Aircraft
   .name = Phase Transport
   .encyclopedia = An invisible APC that only becomes visible when critically damaged. It emits a global sound when built and is highly effective for surprise drops in the back of an opponent’s base.

## Civilian Tech
actor-hosp =
   .name = Hospital
   .captured-desc = Provides infantry with self-healing
   .capturable-desc = Capture to let infantry self-heal
   .encyclopedia = Restores small amounts of health to infantry at frequent intervals.

actor-fcom =
   .name = Forward Command
   .captured-desc = Provides buildable area
   .capturable-desc = Capture to give buildable area
   .encyclopedia = Esentially a Construction Yard with a smaller build radius and less health.

actor-miss =
   .name = Communications Center
   .captured-desc = Provides range of vision
   .capturable-desc = Capture to give visual range
   .encyclopedia = Provides an area of vision around it.

actor-bio =
   .name = Biological Lab
   .captured-desc = Provides prerequisite for Bio-Lab units
   .capturable-desc = Capture to produce Bio-Lab units
   .encyclopedia = Produces Zombies and Ants.

actor-oilb =
   .name = Oil Derrick
   .captured-desc = Provides additional funds
   .capturable-desc =  Capture to receive additional funds
   .encyclopedia = Becomes profitable under a minute after capture, depending on whether you receive the early income tick. Oil Derricks provide money frequents as well as a cash bonus upon capture. Its tick sounds are heard globally.

## misc.yaml
actor-powerproxy-parabombs =
   .name = Parabombs (Single Use)
   .description = A Badger drops parachuted bombs
    over a selected location
   .encyclopedia= Exclusive to Ukraine, this ability is unlocked with an airfield. A single Badger drops 10 bombs in a straight line.

actor-powerproxy-sonarpulse =
   .name = Sonar Pulse
   .description = Reveals all submarines in the vicinity for a
    short time
   .encyclopedia = Upon infiltrating a Naval Yard or Submarine Pen with a Spy, all submarines within a short radius are revealed for a brief period of time.

actor-powerproxy-paratroopers =
   .name = Paratroopers
   .description = A Badger drops a squad of infantry
    anywhere on the map
   .encyclopedia = A small group of units that can periodically be used to harass the enemy.

## ai.yaml
bot-rush-ai =
   .name = Rush AI

bot-normal-ai =
   .name = Normal AI

bot-turtle-ai =
   .name = Turtle AI

bot-naval-ai =
   .name = Naval AI
