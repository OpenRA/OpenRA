## player.yaml
options-tech-level =
    .low = Low
    .medium = Medium
    .no-powers = No Superpowers
    .unrestricted = Unrestricted

checkbox-automatic-concrete =
    .label = Automatic Concrete
    .description = Concrete foundations are automatically laid under buildings

notification-insufficient-funds = Insufficient funds
notification-new-construction-options = New construction options
notification-cannot-deploy-here = Cannot deploy here
notification-low-power = Low power
notification-base-under-attack = Base under attack
notification-ally-under-attack = Our ally is under attack
notification-harvester-under-attack = Harvester under attack
notification-silos-needed = Silos needed
notification-no-room-for-new-unit = No room for new unit
notification-cannot-build-here = Cannot build here
notification-one-of-our-buildings-has-been-captured = One of our buildings has been captured

## world.yaml
dropdown-map-worms =
    .label = Worms
    .description = Worms roam the map, devouring unprepared forces

options-starting-units =
    .mcv-only = MCV Only
    .light-support = Light Support
    .heavy-support = Heavy Support

resource-spice = Spice

faction-random =
   .name = Any
   .description = Random House
    A random house is chosen at the start of the game

faction-atreides =
   .name = Atreides
   .description = House Atreides
    The noble Atreides, from the water world of Caladan,
    rely on their Ornithopters to ensure air superiority.
    They have allied themselves with the Fremen, the fearsome
    native warriors of Dune who can move undetected in battle.

    Faction Variations:
        - Combat Tanks are balanced in terms of speed and durability

    Special Units:
        - Grenadier
        - Fremen
        - Sonic Tank

    Superweapon:
        - Airstrike

faction-harkonnen =
   .name = Harkonnen
   .description = House Harkonnen
    The evil Harkonnen will stop at nothing to gain control of the spice.
    They rely on brute force and atomic weapons to achieve their goals:
    wealth, and the destruction of House Atreides.

    Faction Variations:
        - Combat Tanks are more durable but move at a slower speed

    Special Units:
        - Sardaukar
        - Devastator

    Superweapon:
        - Death Hand Missile

faction-ordos =
   .name = Ordos
   .description = House Ordos
    The insidious Ordos of the icy planet Sigma Draconis IV
    are known for their wealth, greed and treachery.
    Relying heavily on mercenaries, they often resort
    to sabotage and the use of forbidden Ixian technologies.

    Faction Variations:
        - Trikes are replaced by Raider Trikes
        - Combat Tanks are faster but less durable

    Special Units:
        - Raider Trike
        - Stealth Raider Trike
        - Saboteur
        - Deviator

faction-corrino =
   .name = Corrino

faction-mercenaries =
   .name = Mercenaries

faction-smugglers =
   .name = Smugglers

faction-fremen =
   .name = Fremen

## defaults.yaml
notification-unit-lost = Unit lost
notification-unit-promoted = Unit promoted
notification-enemy-building-captured = Enemy building captured
notification-primary-building-selected = Primary building selected

## aircraft.yaml
actor-carryall-reinforce =
   .name = Carryall
   .description = A large, winged, planet-bound ship,
      that automatically lifts harvesters to and from spice fields
      and lifts vehicles to Repair Pads when ordered to

actor-carryall-encyclopedia = Carryalls automatically transport Harvesters between the Spice Fields and Refineries. They can also pick up units and deliver them to the Repair Pad.

    The Carryall is a lightly armored transport aircraft. It is vulnerable to missiles and can only be hit by anti-aircraft weapons.
actor-frigate-name = Frigate

actor-ornithopter =
   .encyclopedia = The Ornithopter is the fastest aircraft on Dune, it is lightly armored and capable of dropping 500lb bombs. It is highly effective against infantry and lightly armored targets, but can also damage other types of armor.
   .name = Ornithopter

actor-ornithopter-husk-name = Ornithopter
actor-carryall-husk-name = Carryall
actor-carryall-huskvtol-name = Carryall

## arrakis.yaml
notification-worm-attack = Worm attack
notification-worm-sign = Worm sign

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
      Strong vs. Infantry
      Weak vs. Vehicles and Artillery
   .name = Light Infantry
   .encyclopedia = Light Infantry are lightly armored foot soldiers, equipped with 9mm RP assault rifles. They are effective against infantry and lightly armored vehicles.

    Light Infantry are resistant to missiles and large-caliber guns, but are very vulnerable to high-explosives, fire and firearms

actor-engineer =
   .description = Infiltrates and captures enemy structures
      Strong vs. Buildings
      Weak vs. Everything
      Repairs damaged cliffs
   .name = Engineer
   .encyclopedia = Engineers can be used to capture enemy buildings

    Engineers are resistant to anti-tank weaponry but are very vulnerable to high-explosives, fire and firearms

actor-trooper =
   .description = Anti-tank infantry
      Strong vs. Tanks
      Weak vs. Infantry and Artillery
   .name = Trooper
   .encyclopedia = Armed with missile launchers, Troopers fire wire-guided, armor-piercing warheads. These units are particularly effective against vehicles, especially armored ones, and buildings. However, they are largely useless against infantry.

    Troopers are resistant to anti-tank weaponry but very vulnerable to high-explosives, fire and firearms

actor-thumper =
   .description = Attracts nearby worms when deployed
      Unarmed
   .name = Thumper Infantry
   .encyclopedia = Deploys a loud hammering device that draws sand worms to the area

actor-fremen =
   .name = Fremen
   .description = Elite infantry unit with assault rifles and rockets
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery
      Special Ability: Invisibility
   .encyclopedia = Fremen are the native desert warriors of Dune. Equipped with 10mm Assault Rifles and Rockets, their firepower is equally effective against infantry and vehicles.

    Fremen units are very vulnerable to high-explosive and ballistic weapons

actor-grenadier =
   .description = Infantry with grenades
      Strong vs. Buildings and Infantry
      Weak vs. Vehicles
   .name = Grenadier
   .encyclopedia = Grenadiers are infantry artillery units effective against buildings. They have a chance to explode on death, so do not group them together.

actor-sardaukar =
   .description = Elite Corrino assault infantry
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery
   .name = Sardaukar
   .encyclopedia = These powerful heavy troopers are equipped with a machine gun that is effective against infantry and a rocket launcher for targeting vehicles

actor-mpsardaukar-description = Elite Harkonnen assault infantry
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery

actor-saboteur =
   .description = Sneaky infantry with explosives
    Turns invisible for a limited time
      Strong vs. Buildings
      Weak vs. Everything
      Special Ability: Destroys buildings
   .name = Saboteur
   .encyclopedia = The Saboteur, a specialized military unit of House Ordos, can demolish any enemy building upon entry, though it will be destroyed in the process. It also has the ability to become invisible by activating its stealth mode.

    The Saboteur is resistant to anti-tank weaponry, but very vulnerable to high-explosives, fire, and firearms

actor-nsfremen-description = Elite infantry unit with assault rifles and rockets
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery

## misc.yaml
actor-crate-name = Crate
actor-mpspawn-name = Multiplayer starting point
actor-waypoint-name = Waypoint for scripted behavior
actor-camera-name = Reveals area to owner
actor-wormspawner-name = Worm spawning location

actor-upgrade-conyard =
   .name = Construction Yard Upgrade
   .description = Unlocks additional construction options:
    (Large Concrete Slab; Rocket Turret)

actor-upgrade-barracks =
   .name = Barracks Upgrade
   .description = Unlocks additional infantry
    (Trooper, Engineer and Thumper Infantry)

    Required to unlock faction specific infantry:
    (Atreides: Grenadier, Harkonnen: Sardaukar)

actor-upgrade-light =
   .name = Light Factory Upgrade
   .description = Unlocks additional light units:
    (Missile Quad)

    Required to unlock a faction specific light unit:
    (Ordos: Stealth Raider Trike)

actor-upgrade-heavy =
   .name = Heavy Factory Upgrade
   .description = Unlocks additional construction options:
    (Repair Pad and IX Research Center)

    Unlocks additional heavy units:
    (Siege Tank, Missile Tank and MCV)

actor-upgrade-hightech =
   .name = High Tech Factory Upgrade
   .description = Unlocks the Atreides Air Strike superweapon

actor-deathhand =
   .name = Death Hand
   .encyclopedia = The Death Hand warhead carries atomic cluster munitions. It detonates above the target, inflicting great damage over a wide area.

## structures.yaml
notification-construction-complete = Construction complete
notification-unit-ready = Unit ready
notification-repairing = Repairing
notification-unit-repaired = Unit repaired
notification-select-target = Select target
notification-missile-launch-detected = Missile launch detected
notification-airstrike-ready = Airstrike ready
notification-building-lost = Building lost
notification-reinforcements-have-arrived = Reinforcements have arrived
notification-death-hand-missile-prepping = Death Hand missile prepping
notification-death-hand-missile-ready = Death Hand missile ready
notification-fremen-ready = Fremen ready
notification-saboteur-ready = Saboteur ready

meta-concrete =
   .generic-name = Structure
   .description = Provides a strong foundation that protects
    against terrain damage

actor-concrete-a =
   .name = Concrete Slab
   .encyclopedia = Buildings not placed on concrete will incur damage. While repairs are possible, if a building is not entirely placed on concrete, it will experience ongoing weathering damage from the harsh desert environment.

    Concrete is vulnerable to most weapons and cannot be repaired once damaged

actor-concrete-b-name = Large Concrete Slab

actor-construction-yard =
   .description = Produces structures
   .encyclopedia = The Construction Yard is the foundation of any base built on Arrakis. It produces a small amount of power and is required for building new structures. Protect this structure! It is critical to the success of your base.

    Construction yards are fairly strong, but vulnerable to all weapons to varying degrees
   .name = Construction Yard

actor-wind-trap =
   .description = Supplies power to other structures
   .name = Wind Trap
   .encyclopedia = Wind Traps supply power and water to an installation. Large, above-ground ducts funnel wind currents underground into massive turbines, which drive power generators and humidity extractors.

    Wind Traps are vulnerable to most weapons

actor-barracks =
   .description = Trains infantry
   .name = Barracks
   .encyclopedia = Barracks are required to produce and train light infantry units. They can be upgraded to train more advanced infantry in later missions.

    Barracks are vulnerable to most weapons

actor-refinery =
   .description = Harvesters unload Spice here for processing
   .name = Spice Refinery
   .encyclopedia = The Refinery is the basis of all Spice production on Dune. Harvesters transport mined Spice to the Refinery where it is converted into credits. Refined Spice is automatically distributed to Silos and Refineries for storage. Each refinery can store up to 2,000 units of spice. A Spice Harvester is delivered by Carryall as soon as a Refinery is built.

    Refineries are vulnerable to most weapons

actor-silo =
   .description = Stores excess harvested Spice
   .encyclopedia = The Spice Silo can store up to 1,500 units of harvested Spice. Once a Refinery finishes processing, any excess Spice is automatically distributed evenly across all Silos and Refineries. If the harvested Spice exceeds the Silo's capacity, the surplus will be lost. When Spice Silos are destroyed or captured, the stored Spice is redistributed among other Silos and Refineries, provided there is sufficient storage capacity available.

    The Spice Silo is vulnerable to most weapons
   .name = Silo

actor-light-factory =
   .description = Produces light vehicles
   .name = Light Factory
   .encyclopedia = The Light Factory is required for the production of small, lightly armored, combat vehicles. In later missions, it can be upgraded to produce more advanced light vehicles.

    A Light Factory is vulnerable to most weapons

actor-heavy-factory =
   .description = Produces heavy vehicles
   .name = Heavy Factory
   .encyclopedia = The Heavy Factory enables the construction of heavy vehicles like Harvesters and Combat Tanks. With upgrades, it also supports the creation of advanced vehicles, though some may require additional buildings.

    The Heavy Factory is vulnerable to most weapons

actor-outpost =
   .description = Provides a radar map of the battlefield
      Requires power to operate
   .name = Outpost
   .encyclopedia = Once enough power is available, the Radar Outpost activates and generates a radar map as soon as it is built

    The Radar Outpost is vulnerable to most weapons

actor-starport =
   .name = Starport
   .description = Dropzone for quick reinforcements, at a price
   .encyclopedia = The Starport enables intergalactic trade with the CHOAM Merchants' Guild, offering a marketplace for vehicles and airborne units at varying rates. This facility is essential for purchasing units from the Guild.

    Even with heavy armor, the Starport is vulnerable to most weapons

actor-wall =
   .description = Stops units and blocks enemy fire
   .name = Concrete Wall
   .generic-name = Structure
   .encyclopedia = Base defense: Concrete walls are the most effective barriers on Dune, effectively blocking tank bullets and hindering unit movement

    Walls can only be damaged by explosive weapons, missiles and shells. Similar to concrete slabs, they cannot be repaired once damaged.

actor-medium-gun-turret =
   .description = Defensive structure
      Strong vs. Tanks
      Weak vs. Infantry and Aircraft
   .name = Gun Turret
   .encyclopedia = The Gun Turret has a medium-range weapon that is highly effective against vehicles, including heavily armored ones. It will automatically fire upon any enemy unit within its range.

    The Gun Turret is resistant to firearms and explosive weapons, but vulnerable to missiles and high-caliber guns

actor-large-gun-turret =
   .description = Defensive structure
      Strong vs. Infantry and Aircraft
      Weak vs. Tanks

      Requires power to operate
   .name = Rocket Turret
   .encyclopedia = The enhanced Rocket Turret boasts longer range and a higher rate of fire compared to the Gun Turret. Its advanced targeting system requires power to operate.

    The Rocket Turret is resistant to firearms and explosive weapons, but vulnerable to missiles and high-caliber guns

actor-repair-pad =
   .description = Repairs vehicles
     Allows MCVs to be built
   .name = Repair Pad
   .encyclopedia = Repair Pads repair units for up to 20% of the units cost

    The Repair Pad is vulnerable to most weapons

actor-high-tech-factory =
   .description = Unlocks advanced technology
   .name = High Tech Factory
   .encyclopedia = The High Tech Factory produces airborne units, and is required for producing Carryalls. House Atreides can upgrade this facility to build Ornithopters for air strikes in later missions.

    The High Tech Factory is vulnerable to most weapons
   .airstrikepower-name = Air Strike
   .airstrikepower-description = Ornithopters bomb the target

actor-research-centre =
   .description = Unlocks advanced tanks
   .name = IX Research Center
   .encyclopedia = The IX Research Center provides technology upgrades for structures and vehicles. This facility is required for producing a number of advanced special weapons and prototypes.

    The IX Research Center is vulnerable to most weapons

actor-palace =
   .description = Unlocks elite infantry and weapons
   .name = Palace
   .encyclopedia = The Palace serves as the command center once built, offering unique additional options and making advanced special weapons available

    Even with heavy armor, the Palace is vulnerable to most weapons
   .nukepower-name = Death Hand
   .nukepower-description = Launches an atomic missile at a target location
   .produceactorpower-fremen-name = Recruit Fremen
   .produceactorpower-fremen-description = Elite infantry unit with assault rifles and rockets
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery
      Special Ability: Invisibility
   .produceactorpower-saboteur-name = Recruit Saboteur
   .produceactorpower-saboteur-description = Sneaky infantry with explosives
    Can be deployed to become invisible for a limited time
      Strong vs Buildings
      Weak vs Everything
      Special Ability: Destroys buildings

## vehicles.yaml
actor-mcv =
   .description = Deploys into a Construction Yard
      Unarmed
   .name = Mobile Construction Vehicle
   .encyclopedia = The Mobile Construction Vehicle must be driven to a suitable deployment area. After finding a stable rock surface, the MCV can be transformed into a Construction Yard.

    MCVs are resistant to bullets and light-explosives. They are vulnerable to missiles and high-caliber guns.

actor-harvester =
   .description = Collects Spice for processing
      Unarmed
   .name = Spice Harvester
   .encyclopedia = Harvesters are resistant to bullets, and to some degree, high explosives. These units are vulnerable to missiles and high-caliber guns.

    A Harvester is included with a Refinery

actor-trike =
   .description = Fast scout
      Strong vs. Infantry
      Weak vs. Tanks
   .name = Trike
   .encyclopedia = Trikes are lightly armored, three-wheeled vehicles equipped with heavy machine guns, effective against infantry and lightly armored vehicles

    Trikes are vulnerable to most weapons, high-caliber guns are slightly less effective against them

actor-quad =
   .description = Missile Scout
      Strong vs. Vehicles
      Weak vs. Infantry
   .name = Missile Quad
   .encyclopedia = Stronger than the Trike in both armor and firepower, the Quad is a four-wheeled vehicle firing armor-piercing rockets. It is effective against most vehicles.

    Quads are resistant to bullets and, to a lesser degree, explosives. However, they are vulnerable to missiles and high-caliber guns.

actor-siege-tank =
   .description = Siege Artillery
      Strong vs. Infantry and Buildings
      Weak vs. Tanks
   .name = Siege Tank
   .encyclopedia = Siege Tanks are very effective against infantry and lightly armored vehicles but are very weak against heavily armored targets. They have a long firing range.

    Siege Tanks are resistant to bullets, and to some degree, explosives. These units are vulnerable to missiles and high-caliber guns.

actor-missile-tank =
   .name = Missile Tank
   .description = Rocket Artillery
      Strong vs. Vehicles, Buildings and Aircraft
      Weak vs. Infantry
   .encyclopedia = The Missile Tank can shoot down aircraft and is effective against most targets, except infantry

    Missile Tanks are vulnerable to most weapons, high-caliber guns are slightly less effective

actor-sonic-tank =
   .description = Fires sonic shocks
      Strong vs. Infantry and Vehicles
      Weak vs. Artillery
   .name = Sonic Tank
   .encyclopedia = The Sonic Tank is most effective against infantry and lightly armored vehicles - but weaker against armored targets.

    The Sonic Tank damages all units in its line of fire

    Very resistant to bullets and small-explosives, but vulnerable to missiles and high-caliber guns

actor-devastator =
   .description = Super Heavy Tank
      Strong vs. Tanks
      Weak vs. Artillery
   .name = Devastator
   .encyclopedia = The Devastator is the most powerful tank on Dune, highly effective against most units, thought it is slow moving and has a slow rate of fire. Powered by Nuclear energy, the Devastator fires dual plasma charges and can be ordered to self-destruct, causing damage to surrounding units and structures.

    The Devastator is very resistant to bullets and high explosives, but vulnerable to missiles and high-caliber guns

actor-raider =
   .description = Improved Scout
      Strong vs. Infantry and Light Vehicles
      Weak vs. Tanks
   .name = Raider Trike
   .encyclopedia = Raiders are similar to Trikes but have been upgraded by the Ordos with enhanced firepower, speed, and armor, making them powerful and highly maneuverable scouts. Equipped with dual 20mm cannons, Raiders excel against infantry and lightly armored vehicles

    Raiders are vulnerable to most weapons, high-caliber guns are slightly less effective

actor-stealth-raider =
   .description = Invisible Raider Trike
      Strong vs. Infantry and Light Vehicles
      Weak vs. Tanks
   .name = Stealth Raider Trike
   .encyclopedia = A cloaked version of the Raider, good for stealth attacks. It uncloaks when it fires its machine guns.

actor-deviator =
   .name = Deviator
   .description = Fires a warhead that changes
    the allegiance of enemy vehicles
   .encyclopedia = The Deviator's missiles discharge a silicon cloud that interferes with vehicle controls, temporarily changing the allegiance of the targeted unit. Personnel are only slightly affected by the cloud.

    The Deviator is vulnerable to most weapons, high-caliber guns are slightly less effective

meta-combat-tank-description = Main Battle Tank
      Strong vs. Tanks
      Weak vs. Infantry

actor-combat-tank-a =
   .name = Atreides Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles

    Atreides Combat Tanks are very resistant to bullets and heavy explosives, but vulnerable to missiles and high-caliber guns

actor-combat-tank-h =
   .name = Harkonnen Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles

    The Harkonnen Combat Tank is stronger than its counterparts, but also slower

actor-combat-tank-o =
   .name = Ordos Combat Tank
   .encyclopedia = The Combat Tank is effective against most vehicles, less so against lightly armored vehicles

    The Ordos Combat Tank is the fastest variant of the Combat Tank, but it is also the weakest

meta-destroyabletile =
   .generic-name = Passage (destroyable)
   .name = Passage (destroyable)

meta-destroyedtile =
   .generic-name = Passage (repairable)
   .name = Passage (repairable)

## ai.yaml
bot-omnius =
   .name = Omnius

bot-vidius =
   .name = Vidious

bot-gladius =
   .name = Gladius
