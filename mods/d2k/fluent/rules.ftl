## player.yaml
options-tech-level =
    .low = Low
    .medium = Medium
    .no-powers = No Superpowers
    .unrestricted = Unrestricted

checkbox-automatic-concrete =
    .label = Automatic Concrete
    .description = Concrete foundations are automatically laid under buildings

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
    .description = Worms roam the map, devouring unprepared forces

options-starting-units =
    .mcv-only = MCV Only
    .light-support = Light Support
    .heavy-support = Heavy Support

resource-spice = Spice

faction-random =
   .name = Any
   .description = Random House.
    A random house is chosen at the start of the game.

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
        - Combat Tanks are more durable but move at a slower speed.

    Special Units:
        - Sardaukar
        - Devastator

    Superweapon:
        - Death Hand Missile

faction-ordos =
   .name = Ordos
   .description = House Ordos
    From the icy world of Sigma Draconis IV, the insidious Ordos are known for
    their wealth, greed, and treachery. They often turn to mercenaries, sabotage,
    and forbidden Ixian technologies to gain the upper hand.

    Faction Variations:
        - Trikes are replaced by Raider Trikes.
        - Combat Tanks are faster but less durable.

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
notification-unit-lost = Unit lost.
notification-unit-promoted = Unit promoted.
notification-enemy-building-captured = Enemy building captured.
notification-primary-building-selected = Primary building selected.

## aircraft.yaml
actor-carryall-reinforce =
   .name = Carryall
   .description = Large winged, planet-bound ship
      Automatically lifts harvesters to and from Spice fields.
      Lifts vehicles to Repair Pads when ordered to.

actor-carryall-encyclopedia = Automatically transport Harvesters between the Spice Fields and Refineries. They can also pick up units and deliver them to the Repair Pad when ordered to.

    The Carryall is a lightly armored transport aircraft. It is vulnerable to missiles and can only be hit by anti-aircraft weapons.
actor-frigate-name = Frigate

actor-ornithopter =
   .name = Ornithopter
   .encyclopedia = The fastest aircraft on Dune, it is lightly armored and capable of dropping 500 lb bombs. Highly effective against infantry and lightly armored targets, with the ability to damage other armor types.

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
   .name = Light Infantry
   .description = General-purpose infantry.
      Strong vs. Infantry.
      Weak vs. Vehicles and Artillery.
   .encyclopedia = Lightly armored foot soldiers, equipped with 9mm RP assault rifles. They are effective against infantry and lightly armored vehicles.

    Light Infantry are resistant to missiles and large-caliber guns, but are very vulnerable to high-explosives, fire and bullet weapons.

actor-engineer =
   .name = Engineer
   .description = Infiltrates and captures enemy structures.
      Strong vs. Buildings.
      Weak vs. Everything.
      Repairs damaged cliffs.
   .encyclopedia = Can be used to capture enemy buildings.

    Engineers are resistant to anti-tank weaponry but are very vulnerable to high-explosives, fire and firearms.

actor-trooper =
   .name = Trooper
   .description = Anti-tank infantry.
      Strong vs. Tanks.
      Weak vs. Infantry and Artillery.
   .encyclopedia = Armed with wire-guided, armor-piercing missile warheads, Troopers are very effective against vehicles and buildings but struggle against infantry.

    Troopers are resistant to anti-tank weaponry but very vulnerable to high-explosives, fire and bullet weapons.

actor-thumper =
   .name = Thumper Infantry
   .description = Attracts nearby worms when deployed.
      Unarmed.
   .encyclopedia = Deploys a loud hammering device that draws Sandworms to the area.

actor-fremen =
   .name = Fremen
   .description = Elite infantry unit with assault rifles and rockets.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.
      Special Ability: Invisibility.
   .encyclopedia = The native desert warriors of Dune, armed with 10mm Assault Rifles and Rockets. Their firepower is equally effective against infantry and vehicles.

    Fremen units are very vulnerable to high-explosive and bullet weapons.

actor-grenadier =
   .name = Grenadier
   .description = Infantry with grenades.
      Strong vs. Buildings and Infantry.
      Weak vs. Vehicles.
   .encyclopedia = An infantry artillery unit strong against buildings. They have a chance of exploding when killed, so should not be grouped together.

actor-sardaukar =
   .name = Sardaukar
   .description = Elite Corrino assault infantry.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.
   .encyclopedia = Powerful heavy troopers equipped with a machine gun that is effective against infantry and a rocket launcher for targeting vehicles.

actor-mpsardaukar-description = Elite Harkonnen assault infantry.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.

actor-saboteur =
   .name = Saboteur
   .description = Sneaky infantry with explosives.
    Turns invisible for a limited time.
      Strong vs. Buildings.
      Weak vs. Everything.
      Special Ability: Destroys buildings.
   .encyclopedia = A specialized military unit of House Ordos, capable of demolishing enemy buildings upon entry, but dying in the resulting explosion. It can activate stealth mode to become invisible.

    The Saboteur is resistant to anti-tank weaponry, but very vulnerable to high-explosives, fire, and bullet weapons.

actor-nsfremen-description = Elite infantry unit with assault rifles and rockets.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.

## misc.yaml
actor-crate-name = Crate
actor-mpspawn-name = (multiplayer starting point)
actor-waypoint-name = (waypoint for scripted behavior)
actor-camera-name = (reveals area to owner)
actor-wormspawner-name = (worm spawning location)

actor-upgrade-conyard =
   .name = Construction Yard Upgrade
   .description = Unlocks additional construction options:
    (Large Concrete Slab and Rocket Turret)

actor-upgrade-barracks =
   .name = Barracks Upgrade
   .description = Unlocks additional infantry:
    (Trooper, Engineer and Thumper Infantry)

    Required to unlock faction-specific infantry:
    (Atreides: Grenadier, Harkonnen: Sardaukar)

actor-upgrade-light =
   .name = Light Factory Upgrade
   .description = Unlocks additional light units:
    (Missile Quad)

    Required to unlock a faction-specific light unit:
    (Ordos: Stealth Raider Trike)

actor-upgrade-heavy =
   .name = Heavy Factory Upgrade
   .description = Unlocks additional construction options:
    (Repair Pad and IX Research Center)

    Unlocks additional heavy units:
    (Siege Tank, Missile Tank and MCV)

actor-upgrade-hightech =
   .name = High Tech Factory Upgrade
   .description = Unlocks the Atreides Air Strike superweapon.

actor-deathhand =
   .name = Death Hand
   .encyclopedia = Armed with atomic cluster munitions, it detonates above its target, inflicting great damage over a wide area.

## structures.yaml
notification-construction-complete = Construction complete.
notification-unit-ready = Unit ready.
notification-repairing = Repairing.
notification-unit-repaired = Unit repaired.
notification-select-target = Select target.
notification-missile-launch-detected = Missile launch detected.
notification-airstrike-ready = Airstrike ready.
notification-building-lost = Building lost.
notification-reinforcements-have-arrived = Reinforcements have arrived.
notification-death-hand-missile-prepping = Death Hand missile prepping.
notification-death-hand-missile-ready = Death Hand missile ready.
notification-fremen-ready = Fremen ready.
notification-saboteur-ready = Saboteur ready.

meta-concrete =
   .generic-name = Structure
   .description = Provides a strong foundation that protects
    against terrain damage.

actor-concrete-a =
   .name = Concrete Slab
   .encyclopedia = Buildings not constructed on a Concrete Slab will sustain ongoing damage from the harsh desert environment of Dune. While repairs are possible, placing structures on concrete prevents continuous weathering.

    Concrete is vulnerable to most weapons and cannot be repaired once damaged.

actor-concrete-b-name = Large Concrete Slab

actor-construction-yard =
   .name = Construction Yard
   .description = Produces structures.
   .encyclopedia = Serving as the foundation of any base built on Arrakis, the Construction Yard produces a small amount of power and enables the construction of new structures. Protect this structure! It is critical to the success of your base.

    Construction yards are fairly strong, but vulnerable to all weapons to varying degrees.

actor-wind-trap =
   .name = Wind Trap
   .description = Supplies power to other structures.
   .encyclopedia = Produces power and water for your base. Large, above-ground ducts funnel wind currents underground into massive turbines, which drive power generators and humidity extractors.

    Wind Traps are vulnerable to most weapons.

actor-barracks =
   .name = Barracks
   .description = Trains infantry.
   .encyclopedia = Required for producing and training light infantry units, it can be upgraded to train advanced infantry in later missions.

    Barracks are vulnerable to most weapons.

actor-refinery =
   .name = Spice Refinery
   .description = Harvesters unload Spice here for processing.
   .encyclopedia = The basis of all Spice production on Dune. Harvesters transport mined Spice to the Refinery where it is converted into credits. Refined Spice is automatically distributed to Silos and Refineries for storage. Each refinery can store Spice. A Spice Harvester is delivered by Carryall once a Refinery is built.

    Refineries are vulnerable to most weapons.

actor-silo =
   .name = Silo
   .description = Stores excess harvested Spice.
   .encyclopedia = Store mined Spice. Any surplus from Refineries is evenly distributed among all available Silos. If storage capacity is exceeded, excess Spice is lost. Destroyed or captured Silos redistribute their contents, provided there is sufficient space.

    The Spice Silo is vulnerable to most weapons.

actor-light-factory =
   .name = Light Factory
   .description = Produces light vehicles.
   .encyclopedia = Required to produce small, lightly armored combat vehicles. It can be upgraded in later missions to manufacture more advanced light vehicles.

    A Light Factory is vulnerable to most weapons.

actor-heavy-factory =
   .name = Heavy Factory
   .description = Produces heavy vehicles.
   .encyclopedia = Enables the construction of heavy vehicles such as Harvesters and Combat Tanks. With upgrades, it unlocks advanced vehicles, though some may require additional buildings.

    The Heavy Factory is vulnerable to most weapons.

actor-outpost =
   .name = Outpost
   .description = Provides a radar map of the battlefield.
      Requires power to operate.
   .encyclopedia = Once enough power is available, the Radar Outpost activates, providing a radar map.

    The Radar Outpost is vulnerable to most weapons.

actor-starport =
   .name = Starport
   .description = Dropzone for quick reinforcements, at a price.
   .encyclopedia = Unlocks intergalactic trade with the CHOAM Merchants' Guild, where vehicles and airborne units can be purchased at varying rates. This facility is essential for acquiring units from the Guild.

    Even with heavy armor, the Starport is vulnerable to most weapons.

actor-wall =
   .name = Concrete Wall
   .generic-name = Structure
   .description = Stops units and blocks enemy fire.
   .encyclopedia = The most effective defensive barriers on Dune, blocking tank fire and impeding unit movement.

    Walls can only be damaged by explosive weapons, missiles and shells. Similar to concrete slabs, they cannot be repaired once damaged.

actor-medium-gun-turret =
   .name = Gun Turret
   .description = Defensive structure.
      Strong vs. Tanks.
      Weak vs. Infantry and Aircraft.
   .encyclopedia = A medium-range weapon that is effective against all types of vehicle, particularly heavily armored ones. It automatically fires upon any enemy unit within its range and requires power to operate.

    The Gun Turret is resistant to firearms and explosive weapons, but vulnerable to missiles and high-caliber guns.

actor-large-gun-turret =
   .name = Rocket Turret
   .description = Defensive structure.
      Strong vs. Infantry and Aircraft.
      Weak vs. Tanks.

      Requires power to operate.
   .encyclopedia = An enhanced defensive structure with a longer range and faster rate of fire than the Gun Turret. Its advanced targeting system requires power to operate.

    The Rocket Turret is resistant to firearms and explosive weapons, but vulnerable to missiles and high-caliber guns.

actor-repair-pad =
   .name = Repair Pad
   .description = Repairs vehicles.
     Allows MCVs to be built.
   .encyclopedia = Repairs units for a fraction of their production cost.

    The Repair Pad is vulnerable to most weapons.

actor-high-tech-factory =
   .name = High Tech Factory
   .description = Unlocks advanced technology.
   .airstrikepower-name = Air Strike
   .airstrikepower-description = Ornithopters bomb the target.
   .encyclopedia = Produces airborne units, and is required to build Carryalls. House Atreides can upgrade this facility to build Ornithopters for air strikes in later missions.

    The High Tech Factory is vulnerable to most weapons.

actor-research-centre =
   .name = IX Research Center
   .description = Unlocks advanced tanks.
   .encyclopedia = Provides technology upgrades for both structures and vehicles. This facility is required to develop advanced special weapons and prototypes.

    The IX Research Center is vulnerable to most weapons.

actor-palace =
   .name = Palace
   .description = Unlocks elite infantry and weapons.
   .encyclopedia = Serves as the command center once built, offering additional options and special weapons.

    Even with heavy armor, the Palace is vulnerable to most weapons.
   .nukepower-name = Death Hand
   .nukepower-description = Launches an atomic missile at a target location.
   .produceactorpower-fremen-name = Recruit Fremen
   .produceactorpower-fremen-description = Elite infantry unit with assault rifles and rockets.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.
      Special Ability: Invisibility.
   .produceactorpower-saboteur-name = Recruit Saboteur
   .produceactorpower-saboteur-description = Sneaky infantry with explosives.
    Can be deployed to become invisible for a limited time.
      Strong vs Buildings.
      Weak vs Everything.
      Special Ability: Destroys buildings.

## vehicles.yaml
actor-mcv =
   .name = Mobile Construction Vehicle
   .description = Deploys into a Construction Yard.
      Unarmed.
   .encyclopedia = Must be driven to an area where it can be deployed. After finding a suitable rock surface, the MCV can be transformed into a Construction Yard.

    MCVs are resistant to bullets and light-explosives. They are vulnerable to missiles and high-caliber guns.

actor-harvester =
   .name = Spice Harvester
   .description = Collects Spice for processing.
      Unarmed.
   .encyclopedia = Resistant to bullets, and to some degree, high explosives. They are vulnerable to missiles and high-caliber guns.

    A Harvester is included with a Refinery.

actor-trike =
   .name = Trike
   .description = Fast scout.
      Strong vs. Infantry.
      Weak vs. Tanks.
   .encyclopedia = Lightly armored, three-wheeled vehicles armed with heavy machine guns, effective against infantry and lightly armored vehicles.

    Trikes are vulnerable to most weapons, high-caliber guns are slightly less effective against them.

actor-quad =
   .name = Missile Quad
   .description = Missile Scout.
      Strong vs. Vehicles.
      Weak vs. Infantry.
   .encyclopedia = Superior to the Trike in both armor and firepower, the Quad is a four-wheeled vehicle firing armor-piercing rockets. It is effective against most vehicles.

    Quads are resistant to bullets and, to a lesser degree, explosives. They are vulnerable to missiles and high-caliber guns.

actor-siege-tank =
   .name = Siege Tank
   .description = Siege Artillery.
      Strong vs. Infantry and Buildings.
      Weak vs. Tanks.
   .encyclopedia = Incredibly effective against infantry and lightly armored vehicles, but struggles against heavily armored targets. It has a long firing range.

    Siege Tanks are resistant to bullets, and to some degree, explosives. They are vulnerable to missiles and high-caliber guns.

actor-missile-tank =
   .name = Missile Tank
   .description = Rocket Artillery.
      Strong vs. Vehicles, Buildings and Aircraft.
      Weak vs. Infantry.
   .encyclopedia = Shoots down aircraft and is effective against most targets, except infantry.

    Missile Tanks are vulnerable to most weapons, high-caliber guns are slightly less effective.

actor-sonic-tank =
   .name = Sonic Tank
   .description = Fires sonic shocks.
      Strong vs. Infantry and Vehicles.
      Weak vs. Artillery.
   .encyclopedia = Most effective against infantry and lightly armored vehicles, but weaker against armored targets.

    Its sonic waves damage all units in their path.

    Resistant to bullets and small-explosives, but vulnerable to missiles and high-caliber guns.

actor-devastator =
   .name = Devastator
   .description = Super Heavy Tank.
      Strong vs. Tanks.
      Weak vs. Artillery.
   .encyclopedia = As the most powerful tank on Dune, the Devastator is slow but highly effective against most units. It fires dual plasma charges and can self-destruct on command, damaging nearby units and structures.

    Resistant to bullets and high explosives, but vulnerable to missiles and high-caliber guns.

actor-raider =
   .name = Raider Trike
   .description = Improved Scout.
      Strong vs. Infantry and Light Vehicles.
      Weak vs. Tanks.
   .encyclopedia = Raider Trikes, upgraded by House Ordos, have enhanced firepower, speed, and armor. Equipped with dual 20mm cannons, they are strong against infantry and lightly armored vehicles.

    Raiders are vulnerable to most weapons, though high-caliber guns are slightly less effective against them.

actor-stealth-raider =
   .name = Stealth Raider Trike
   .description = Invisible Raider Trike.
      Strong vs. Infantry and Light Vehicles.
      Weak vs. Tanks.
   .encyclopedia = A cloaked version of the Raider, good for stealth attacks. It uncloaks when it fires its machine guns.

actor-deviator =
   .name = Deviator
   .description = Fires a warhead that changes
    the allegiance of enemy vehicles.
   .encyclopedia = Fires missiles that release a silicon cloud, temporarily altering the allegiance of targeted vehicles. Personnel are only slightly affected by the cloud.

    The Deviator is vulnerable to most weapons, high-caliber guns are slightly less effective.

meta-combat-tank-description = Main Battle Tank.
      Strong vs. Tanks.
      Weak vs. Infantry.

actor-combat-tank-a =
   .name = Atreides Combat Tank
   .encyclopedia = Effective against most vehicles but less suited against lightly armored targets.

    Resistant to bullets and heavy explosives, but vulnerable to missiles and high-caliber guns.

actor-combat-tank-h =
   .name = Harkonnen Combat Tank
   .encyclopedia = Effective against most vehicles but less suited against lightly armored targets.

    Stronger than its counterparts, but also slower.

actor-combat-tank-o =
   .name = Ordos Combat Tank
   .encyclopedia = Effective against most vehicles but less suited against lightly armored targets.

    The fastest variant of Combat Tank, but also the weakest.

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
