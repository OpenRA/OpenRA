## player.yaml
options-tech-level =
    .low = Low
    .medium = Medium
    .no-powers = No Powers
    .unrestricted = Unrestricted

checkbox-automatic-concrete =
    .label = Automatic Concrete
    .description = Concrete foundations are automatically created beneath buildings

notification-insufficient-funds = Insufficient funds.
notification-new-construction-options = New construction options.
notification-cannot-deploy-here = Cannot deploy here.
notification-low-power = Low power.
notification-base-under-attack = Base under attack.
notification-ally-under-attack = Our ally is under attack.
notification-harvester-under-attack = Harvester under attack.
notification-silos-needed = Silos needed.
notification-no-room-for-new-units = No room for new unit.
notification-cannot-build-here = Cannot build here.
notification-one-of-our-buildings-has-been-captured = One of our buildings has been captured.

## world.yaml
dropdown-map-worms =
    .label = Worms
    .description = Worms roam the map and devour unprepared forces

options-starting-units =
    .mcv-only = MCV Only
    .light-support = Light Support
    .heavy-support = Heavy Support

resource-spice = Spice

## defaults.yaml
notification-unit-lost = Unit lost.
notification-unit-promoted = Unit promoted.
notification-enemy-building-captured = Enemy Building captured.
notification-primary-building-selected = Primary building selected.

## aircraft.yaml
actor-carryall-reinforce =
   .name = Carryall
   .description = Large winged, planet-bound ship
      Automatically lifts harvesters from and to Spice.
      Lifts vehicles to Repair Pads when ordered.

actor-carryall-encyclopedia = Carryalls will automatically transport Harvesters back and forth from the Spice Fields to the Refineries. They will also pick up units and deliver them to the Repair Pad, when ordered to.

    The Carryall is a lightly armored transport aircraft. They are vulnerable to missiles and can only be hit by anti-aircraft weapons.
actor-frigate-name = Frigate

actor-ornithopter =
   .encyclopedia = The fastest aircraft on Dune, the Ornithopther is lightly armored and capable of dropping 500lb bombs. This unit is most effective against infantry and lightly armored targets, but also damages armored targets.
   .name = Ornithopter

actor-ornithopter-husk-name = Ornithopter
actor-carryall-husk-name = Carryall
actor-carryall-huskvtol-name = Carryall

## arrakis.yaml
notification-worm-attack = Worm attack.
notification-worm-sign = Worm sign.

actor-spicebloom-spawnpoint-name = Spice Bloom spawnpoint
actor-spicebloom-name = Spice Bloom
actor-sandworm-name = Sandworm
actor-sietch-name = Fremen Sietch

## defaults.yaml
meta-vehicle-generic-name = Unit
meta-husk-generic-name = Destroyed Unit
meta-aircrafthusk-generic-name = Unit
meta-infantry-generic-name = Unit
meta-plane-generic-name = Unit
meta-building-generic-name = Structure

## husks.yaml
actor-mcv-husk-name = Mobile Construction Vehicle (Destroyed)
actor-harvester-husk-name = Spice Harvester (Destroyed)
actor-siege-tank-husk-name = Siege Tank (Destroyed)
actor-missile-tank-husk-name = Missile Tank (Destroyed)
actor-sonic-tank-husk-name = Sonic Tank (Destroyed)
actor-devastator-husk-name = Devastator (Destroyed)
actor-deviator-husk-name = Deviator (Destroyed)
meta-combat-tank-husk-name = Combat Tank (Destroyed)

## infantry.yaml
actor-light-inf =
   .description = General-purpose infantry
      Strong vs Infantry
      Weak vs Vehicles, Artillery
   .name = Light Infantry
   .encyclopedia = Light Infantry are lightly armored foot soldiers, equipped with 9mm RP assault rifles. Light Infantry are effective against other infantry and lightly armored vehicles.

    Light Infantry are resistant to missiles and large-caliber guns, but very vulnerable to high-explosives, fire and bullet weapons.

actor-engineer =
   .description = Infiltrates and captures enemy structures
      Strong vs Buildings
      Weak vs Everything
      Can repair destroyed cliffs
   .name = Engineer
   .encyclopedia = Engineers can be used to capture enemy buildings.

    Engineers are resistant to anti-tank weaponry but very vulnerable to high-explosives, fire and bullet weapons.

actor-trooper =
   .description = Anti-tank infantry
      Strong vs Tanks
      Weak vs Infantry, Artillery
   .name = Trooper
   .encyclopedia = Armed with missile launchers, Troopers fire wire guided armor-piercing warheads. These units are particularly effective against vehicles (especially armored ones) and buildings. However, this unit is largely useless against infantry.

    Troopers are resistant to anti-tank weaponry but very vulnerable to high-explosives, fire and bullet weapons.

actor-thumper =
   .description = Attracts nearby worms when deployed
      Unarmed
   .name = Thumper Infantry
   .encyclopedia = Deploys a noisy hammering device which will attract sand worms to this area.

actor-fremen =
   .name = Fremen
   .description = Elite infantry unit armed with assault rifles and rockets
      Strong vs Infantry, Vehicles
      Weak vs Artillery
      Special Ability: Invisibility
   .encyclopedia = Fremen are the native desert warriors of Dune. Fremen ground units carry 10mm Assault Rifles and Rockets. Their firepower is equally effective against infantry and vehicles.

    Fremen units are very vulnerable to high-explosive and bullet weapons.

actor-grenadier =
   .description = Infantry armed with grenades.
      Strong vs Buildings, Infantry
      Weak vs Vehicles
   .name = Grenadier
   .encyclopedia = Grenadiers are an infantry artillery unit which are strong against buildings. They have a chance to explode on death, so don't group them together.

actor-sardaukar =
   .description = Elite assault infantry of Corrino
      Strong vs Infantry, Vehicles
      Weak vs Artillery
   .name = Sardaukar
   .encyclopedia = These powerful heavy troopers have a machine gun that's effective against infantry, and a rocket launcher for vehicles.

actor-mpsardaukar-description = Elite assault infantry of Harkonnen
      Strong vs Infantry, Vehicles
      Weak vs Artillery

actor-saboteur =
   .description = Sneaky infantry, armed with explosives.
    Can be deployed to become invisible for a limited time.
      Strong vs Buildings
      Weak vs Everything
      Special Ability: destroy buildings
   .name = Saboteur
   .encyclopedia = The Saboteur is a special military unit acquired by House Ordos. A single Saboteur can destroy any enemy building once he moves into it, though also destroys himself! A Saboteur can be stealthed by deploying itself.

    The Saboteur is resistant to anti-tank weaponry, but very vulnerable to high-explosives, fire, and bullet weapons.

actor-nsfremen-description = Elite infantry unit armed with assault rifles and rockets
      Strong vs Infantry, Vehicles
      Weak vs Artillery

## misc.yaml
actor-crate-name = Crate
actor-mpspawn-name = (multiplayer player starting point)
actor-waypoint-name = (waypoint for scripted behavior)
actor-camera-name = (reveals area to owner)
actor-wormspawner-name = (worm spawning location)

actor-upgrade-conyard =
   .name = Construction Yard Upgrade
   .description = Unlocks additional construction options
    (Large Concrete Slab, Rocket Turret)

actor-upgrade-barracks =
   .name = Barracks Upgrade
   .description = Unlocks additional infantry
    (Trooper, Engineer, Thumper Infantry)

    Required to unlock faction specific infantry
    (Atreides: Grenadier, Harkonnen: Sardaukar)

actor-upgrade-light =
   .name = Light Factory Upgrade
   .description = Unlocks additional light unit
    (Missile Quad)

    Required to unlock faction specific light unit
    (Ordos: Stealth Raider Trike)

actor-upgrade-heavy =
   .name = Heavy Factory Upgrade
   .description = Unlocks additional construction options
    (Repair Pad, IX Research Center)

    Unlocks additional heavy units
    (Siege Tank, Missile Tank, MCV)

actor-upgrade-hightech =
   .name = High Tech Factory Upgrade
   .description = Unlocks the Atreides Air Strike superweapon

actor-deathhand =
   .name = Death Hand
   .encyclopedia = The Death Hand warhead carries atomic cluster munitions. It detonates above the target, inflicting great damage over a wide area.

## structures.yaml
notification-construction-complete = Construction complete.
notification-unit-ready = Unit ready.
notification-repairing = Repairing.
notification-unit-repaired = Unit repaired.
notification-select-target = Select target.
notifciation-missile-launch-detected = Missile launch detected.
notification-airstrike-ready = Airstrike ready.
notification-building-lost = Building lost.
notification-reinforcements-have-arrived = Reinforcements have arrived.
notification-death-hand-missile-prepping = Death Hand missile prepping.
notification-death-hand-missile-ready = Death Hand missile ready.
notification-fremen-ready = Fremen ready.
notification-saboteur-ready = Saboteur ready.

meta-concrete =
   .generic-name = Structure
   .description = Provides a strong foundation that prevents
    damage from the terrain.

actor-concretea =
   .name = Concrete Slab
   .encyclopedia = If buildings are not placed on concrete, they will be damaged. Buildings can be repaired, but unless the building sits completely on concrete, the building will suffer continual weathering damage from the erosive desert environment.

    Concrete is vulnerable to most weapon types. Concrete cannot be repaired if damaged.

actor-concreteb-name = Large Concrete Slab

actor-construction-yard =
   .description = Produces structures.
   .encyclopedia = The Construction Yard is the foundation of any base built on Arrakis. Construction Yards produce a small amount of power and are required for the building of any new structures. Protect this structure! It is critical to the success of your base.

    Construction yards are fairly strong, but vulnerable in varying degrees to all types of weapons.
   .name = Construction Yard

actor-wind-trap =
   .description = Provides power for other structures.
   .name = Wind Trap
   .encyclopedia = Wind Traps provide power and water to an installation. Large, above-ground ducts funnel wind currents underground into massive turbines which power generators and humidity extractors.

    Wind Traps are vulnerable to most types of weapons.

actor-barracks =
   .description = Trains infantry.
   .name = Barracks
   .encyclopedia = Barracks are required to produce and train light infantry units. Barracks can be upgraded for the production of more advanced infantry in later missions.

    Barracks are vulnerable to most types of weapons.

actor-refinery =
   .description = Harvesters unload Spice here for processing.
   .name = Spice Refinery
   .encyclopedia = The Refinery is the basis of all Spice production on Dune. Harvesters transport mined Spice to the Refinery where it is converted into credits. Refined Spice is automatically distributed among the Silos and Refineries for storage. A refinery can store 2000 spice. A Spice Harvester is delivered via Carryall once a Refinery is built.

    Refineries are vulnerable to most types of weapons.

actor-silo =
   .description = Stores excess harvested Spice.
   .encyclopedia = The Spice Silo allows the player to store 1500 harvested Spice. When a Refinery completes processing, excess Spice is automatically distributed evenly among the Silos and Refineries. When harvested Spice exceeds Silo capacity, the excess will be lost. When Spice Silos are destroyed or captured, the amount stored will be dispersed among other Silos and Refineries unless there is insufficient storage capacity.

    The Spice Silo is vulnerable to most types of weapons.
   .name = Silo

actor-light-factory =
   .description = Produces light vehicles.
   .name = Light Factory
   .encyclopedia = The Light Factory is required for the production of small, lightly armored, combat vehicles. The Light Factory can be upgraded to produce more advanced light vehicles in later missions.

    A Light Factory is vulnerable to most types of weapons.

actor-heavy-factory =
   .description = Produces heavy vehicles.
   .name = Heavy Factory
   .encyclopedia = The Heavy Factory allows the player to build heavy vehicles such as Harvesters and Combat Tanks. When upgraded, this facility allows the construction of advanced vehicles, though some vehicles also require other buildings.

    The Heavy Factory is vulnerable to most types of weapons.

actor-outpost =
   .description = Provides a radar map of the battlefield.
      Requires power to operate.
   .name = Outpost
   .encyclopedia = If the player has sufficient power, the Radar Outpost will generate a radar map. Radar is automatically activated when construction of the Outpost is complete.

    The Radar Outpost is vulnerable to most types of weapons.

actor-starport =
   .name = Starport
   .description = Dropzone for quick reinforcements, at a price.
   .encyclopedia = The Starport allows you to engage in intergalactic trading with the C.H.O.A.M. Merchants' Guild. The Starport provides a trading market for vehicles and airborne units at varying rates. You cannot purchase units from the Guild without this facility.

    Armor is heavy, but the Starport is vulnerable to most types of weapons.

actor-wall =
   .description = Stop units and blocks enemy fire.
   .name = Concrete Wall
   .generic-name = Structure
   .encyclopedia = Base defense. Concrete walls are the most effective barriers on Dune. Concrete walls will block tank bullets and impede unit movement.

    Walls can only be damaged by explosive weapons, missiles and shells. Like concrete slabs, walls cannot be repaired if damaged.

actor-medium-gun-turret =
   .description = Defensive structure.
      Strong vs Tanks
      Weak vs Infantry, Aircraft
   .name = Gun Turret
   .encyclopedia = The Gun Turret has a medium range gun which is effective against vehicles, especially heavily armored vehicles. The Gun Turret will fire on any enemy unit within range.

    The Gun Turret is resistant to bullet and explosive weapons, but vulnerable to missiles and high-caliber guns.

actor-large-gun-turret =
   .description = Defensive structure.
      Strong vs Infantry, Aircraft
      Weak vs Tanks

      Requires power to operate.
   .name = Rocket Turret
   .encyclopedia = The substantially improved Rocket Turret has a longer range and a higher rate of fire than the Gun Turret. The Rocket Turret's advanced targeting equipment requires power to operate.

    The Rocket Turret is resistant to bullet and explosive weapons, but vulnerable to missiles and high-caliber guns.

actor-repair-pad =
   .description = Repairs vehicles.
     Allows construction of MCVs
   .name = Repair Pad
   .encyclopedia = With a Repair Pad, vehicles can be repaired for a varying price.

    The Repair Pad is vulnerable to most types of weapons.

actor-high-tech-factory =
   .description = Unlocks advanced technology.
   .name = High Tech Factory
   .encyclopedia = The High Tech Factory produces airborne units, and is required for the production of Carryalls. House Atreides can upgrade the High Tech Factory to build Ornithopters for an air strike in later missions.

    The High Tech Factory is vulnerable to most types of weapons.
   .airstrikepower-name = Air Strike
   .airstrikepower-description = Ornithopters hit the target with bombs

actor-research-centre =
   .description = Unlocks advanced tanks.
   .name = IX Research Center
   .encyclopedia = The IX Research Center provides technology upgrades for structures and vehicles. This facility is required for production of a number of advanced special weapons and prototypes.

    The IX Research Center is vulnerable to most types of weapons.

actor-palace =
   .description = Unlocks elite infantry and weapons.
   .name = Palace
   .encyclopedia = The Palace serves as the command center once it is constructed. Palaces feature unique additional options, making available advanced special weapons.

    Armor is heavy, but the Palace is vulnerable to most types of weapons.
   .nukepower-name = Death Hand
   .nukepower-description = Launches an atomic missile at a target location
   .produceactorpower-fremen-name = Recruit Fremen
   .produceactorpower-fremen-description = Elite infantry unit armed with assault rifles and rockets
      Strong vs Infantry, Vehicles
      Weak vs Artillery
      Special Ability: Invisibility
   .produceactorpower-saboteur-name = Recruit Saboteur
   .produceactorpower-saboteur-description = Sneaky infantry, armed with explosives.
    Can be deployed to become invisible for a limited time.
      Strong vs Buildings
      Weak vs Everything
      Special Ability: destroy buildings

## vehicles.yaml
actor-mcv =
   .description = Deploys into another Construction Yard
      Unarmed
   .name = Mobile Construction Vehicle
   .encyclopedia = The Mobile Construction Vehicle must be driven to a suitable deployment area. After locating an appropriate area of rock, the MCV can be transformed into a Construction Yard.

    MCVs are resistant to bullets and light-explosives. They are vulnerable to missiles and high-caliber guns.

actor-harvester =
   .description = Collects Spice for processing
      Unarmed
   .name = Spice Harvester
   .encyclopedia = Harvesters are resistant to bullets, and to some degree, high-explosives. These units are vulnerable to missiles and high-caliber guns.

    A Harvester is included with a Refinery.

actor-trike =
   .description = Fast scout
      Strong vs Infantry
      Weak vs Tanks
   .name = Trike
   .encyclopedia = Trikes are lightly armored, three-wheeled vehicles equipped with heavy machine guns, effective against infantry and lightly armored vehicles.

    Trikes are vulnerable to most weapons, though high-caliber guns are slightly less effective against them.

actor-quad =
   .description = Missile Scout
      Strong vs Vehicles
      Weak vs Infantry
   .name = Missile Quad
   .encyclopedia = Stronger than the Trike in both armor and firepower, the Quad is a four-wheeled vehicle firing armor-piercing rockets. The Quad is effective against most vehicles.

    Quads are resistant to bullets and explosives, to a lesser degree. However, Quads are vulnerable to missiles and high-caliber guns.

actor-siege-tank =
   .description = Siege Artillery
      Strong vs Infantry, Buildings
      Weak vs Tanks
   .name = Siege Tank
   .encyclopedia = Siege Tanks are very effective against infantry and lightly armored vehicles - but very weak against heavily armored targets. They fire over a long range.

    Siege Tanks are resistant to bullets, and to some degree, explosives. These units are vulnerable to missiles and high-caliber guns.

actor-missile-tank =
   .name = Missile Tank
   .description = Rocket Artillery
      Strong vs Vehicles, Buildings, Aircraft
      Weak vs Infantry
   .encyclopedia = The Missile Tank is anti-aircraft capable and effective against most targets, except infantry units.

    Missile Tanks are vulnerable to most weapons, though high-caliber guns are slightly less effective.

actor-sonic-tank =
   .description = Fires sonic shocks
      Strong vs Infantry, Vehicles
      Weak vs Artillery
   .name = Sonic Tank
   .encyclopedia = The Sonic Tank is most effective against infantry and lightly armored vehicles - but weaker against armored targets.

    The Sonic Tank will damage all units in its firing path.

    They are very resistant to bullets and small-explosives, but vulnerable to missiles and high-caliber guns.

actor-devastator =
   .description = Super Heavy Tank
      Strong vs Tanks
      Weak vs Artillery
   .name = Devastator
   .encyclopedia = The Devastator is the most powerful tank on Dune - powerfully effective against most units, but slow - and slow to fire. Nuclear powered, the Devastator fires dual plasma charges and may be ordered to self-destruct, damaging surrounding units and structures.

    The Devastator is very resistant to bullet and high-explosives, but vulnerable to missiles and high-caliber guns.

actor-raider =
   .description = Improved Scout
      Strong vs Infantry, Light Vehicles
      Weak vs Tanks
   .name = Raider Trike
   .encyclopedia = Raiders are similar to Trikes, but the Ordos have refined their fire power, speed and armor to create a powerful and maneuverable scout. With dual 20mm cannons, Raiders are most effective against infantry and lightly armored vehicles.

    Raiders are vulnerable to most types of weaponry, though high-caliber guns are slightly less effective.

actor-stealth-raider =
   .description = Invisible Raider Trike
      Strong vs Infantry, Light Vehicles
      Weak vs Tanks
   .name = Stealth Raider Trike
   .encyclopedia = A cloaked version of the raider, good for stealth attacks. Will uncloak when firing its machine guns.

actor-deviator =
   .name = Deviator
   .description = Fires a warhead which changes
    the allegiance of enemy vehicles
   .encyclopedia = The Deviator's missiles discharge a silicon cloud that interferes with vehicle controls - temporarily changing the allegiance of the targeted unit. Personnel are not seriously effected by the cloud.

    The Deviator is vulnerable to most types of weapon, though high-caliber guns are slightly less effective.

meta-combat-tank-description = Main Battle Tank
      Strong vs Tanks
      Weak vs Infantry

actor-combat-tank-a =
   .name = Atreides Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles.

    Atreides Combat Tanks are very resistant to bullet and heavy-explosives, but vulnerable to missiles and high-caliber guns.

actor-combat-tank-h =
   .name = Harkonnen Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles.

    The Harkonnen Combat Tank is stronger than its counterparts, but also slower.

actor-combat-tank-o =
   .name = Ordos Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles.

    The Ordos Combat Tank is the fastest variant of the Combat Tank, but it is also the weakest.

meta-DestroyableTile =
   .generic-name = Passage (destroyable)
   .name = Passage (destroyable)

meta-DestroyedTile =
   .generic-name = Passage (repairable)
   .name = Passage (repairable)
