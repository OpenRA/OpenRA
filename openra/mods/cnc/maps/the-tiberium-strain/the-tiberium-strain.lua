--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--- This mission's human player.
local Nod = Player.GetPlayer("Nod")

--- Owner of the defenders.
local GDI = Player.GetPlayer("GDI")

--- Owner of the homes and villagers.
local Villagers = Player.GetPlayer("Villagers")

---@type integer Objective to destroy GDI's bio centers.
local BioObjective

---@type integer Objective to destroy all villagers.
local VillagerObjective

---@type integer Final objective to keep all non-BIO structures intact.
local IntactObjective

--- Tally of most GDI deaths. Extra units on Hard do not apply.
local GDIDeathCount = 0

--- Number of counted GDI deaths that will trigger a map-wide hunt.
local HuntThreshold = 65

--- Has Nod's starting force arrived?
local NodArrived = false

--- Are Nod's village sweepers about to reinforce?
local SweepersRequested = false

--- Have civilians spawned from the south village road?
local RunnersArrived = false

--- Return one of the entry waypoints by the east ore field.
---@return actor waypoint
local function RandomEastEntry()
	return Map.NamedActor("EastEntry" .. Utils.RandomInteger(1, 5))
end

--- Is this actor a Nod attacker?
---@param actor actor
---@return boolean
local function IsMobileNod(actor)
	return not actor.IsDead and actor.HasProperty("Move") and actor.Owner == Nod
end

--- Is this a resident of the main village?
local function IsWestVillager(actor)
	return actor.HasTag("West Villager")
end

--- Is this actor alive and tagged to guard a wide area?
---@param actor actor
---@return boolean
local function IsLiveAreaGuard(actor)
	return not actor.IsDead and actor.HasTag("Area Guard")
end

--- Is this actor either dead or idle?
---@param actor actor
---@return boolean
local function IsDeadOrIdle(actor)
	return actor.IsDead or actor.IsIdle
end

--- Is Nod wiped out with no incoming sweepers?
--- @return boolean
local function IsNodDead()
	return NodArrived and Nod.HasNoRequiredUnits() and not SweepersRequested
end

--- Is this actor free to come hunt base attackers?
---@param actor actor
---@return boolean
local function CanDefendBase(actor)
	return not actor.IsDead and actor.IsIdle and not actor.HasTag("Area Guard")
end

--- Is GDI weak enough that Nod sweepers can be reinforced?
---@return boolean
local function AreSweepersReady()
	local defenders = Utils.Where(GDI.GetGroundAttackers(), function(a)
		-- Disregard the slow Mammoth Tank, which can be avoided if necessary.
		return a.Type ~= "htnk"
	end)

	return #defenders == 0
end

--- Reveal the defenses by GDI's west gate.
local function RevealWestBaseEntrance()
	local camera = Actor.Create("camera.small", true, { Owner = Nod, Location = WestGateReveal.Location })
	Trigger.AfterDelay(DateTime.Seconds(6), camera.Destroy)
end

--- Mark defeat for Nod's dead task force.
local function MarkNodDead()
	local objs = { BioObjective, VillagerObjective }

	Utils.Do(objs, function(obj)
		if Nod.IsObjectiveCompleted(obj) then
			return
		end

		Nod.MarkFailedObjective(obj)
	end)
end

--- Add the villager slaying objective if it's not already present.
--- This is done more than once as a safety check, partly because the
--- visibility cheat will bypass the usual trigger for this.
---@param announced? boolean
local function AddVillagerObjective(announced)
	if not announced or IsNodDead() then
		VillagerObjective = VillagerObjective or AddPrimaryObjective(Nod, "kill-every-villager-in-area")
		return
	end

	local speaker = UserInterface.GetFluentMessage("nod-soldier")
	Media.DisplayMessage(UserInterface.GetFluentMessage("careless-gdi-experiments-killed"), speaker, Nod.Color)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		if IsNodDead() then
			return
		end

		Media.DisplayMessage(UserInterface.GetFluentMessage("villagers-must-not-live"), speaker, Nod.Color)
	end)

	Trigger.AfterDelay(DateTime.Seconds(8), AddVillagerObjective)
end

--- Check the villager objective after each has been slain.
local function CheckVillagerObjective()
	-- Delay so HasNoRequiredUnits accounts for deaths during THIS tick.
	Trigger.AfterDelay(1, function()
		if RunnersArrived and Villagers.HasNoRequiredUnits() then
			AddVillagerObjective()
			Nod.MarkCompletedObjective(VillagerObjective)
		end
	end)
end

--- Spawn some extra Nod units for Easy difficulty.
local function ReinforceEasyNod()
	if IsNodDead() then
		return
	end

	local rallies = { ChemRally1, ChemRally2, FlameRally2 }
	Media.PlaySpeechNotification(Nod, "Reinforce")

	Utils.Do(rallies, function(rally)
		local path = { NorthWestEntry2.Location, rally.Location }
		Reinforcements.Reinforce(Nod, { "e5" }, path)
	end)
end

--- Prepare to count an actor's death toward GDI's hunt threshold.
---@param actor actor
local function TallyDeath(actor)
	Trigger.OnKilled(actor, function()
		GDIDeathCount = GDIDeathCount + 1

		if GDIDeathCount == HuntThreshold then
			-- The expected start -> village -> bridges -> SE corner -> base
			-- path should yield 56-63 GDI deaths once the south gate guards
			-- are cleared. 20 are from the village (incl. helicopter cargo).
			Utils.Do(GDI.GetGroundAttackers(), function(attacker)
				-- Stop any patrolling or area guarding.
				attacker.Stop()
				attacker.RemoveTag("Area Guard")
				attacker.Stance = "AttackAnything"
				IdleHunt(attacker)
			end)
		end

		if Difficulty == "easy" and GDIDeathCount % 15 == 0 then
			ReinforceEasyNod()
		end
	end)
end

--- Spawn and assemble Nod's starting force.
local function ReinforceFirstNod()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	local chemTypes = { "e5", "e5", "e5", "e5" }
	local flameTypes = { "ftnk" }

	for i = 1, 3 do
		local path = { Map.NamedActor("NorthWestEntry" .. i).Location, Map.NamedActor("FlameRally" .. i).Location }
		Reinforcements.Reinforce(Nod, flameTypes, path)

		if i ~= 3 then
			Trigger.AfterDelay(20, function()
				local chemPath = { NorthWestEntry2.Location, Map.NamedActor("ChemRally" .. i).Location }
				Reinforcements.Reinforce(Nod, chemTypes, chemPath, 10)
			end)
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		NodArrived = true
	end)
end

--- Spawn another Flame Tank for Nod after the bridge tank's death.
local function ReinforceNodTank()
	if IsNodDead() then
		return
	end

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, { "ftnk" }, { NorthWestEntry2.Location, Waypoint1.Location })
end

--- Reinforce a Flame Tank as a reward for the intersection tank's death.
--- This originally spawns in the starting area, with no runners to chase.
local function ReinforceVillageFlameTank()
	if IsNodDead() then
		return
	end

	local runnerTypes = { "c3", "c9", "c7" }
	local runners = Reinforcements.Reinforce(Villagers, runnerTypes, { SouthVillageEntry.Location }, 5, function(runner)
		Trigger.OnIdle(runner, function()
			if runner.Location == Waypoint3.Location then
				Trigger.ClearAll(runner)
			end

			runner.Move(Waypoint3.Location)
		end)
	end)

	RunnersArrived = true
	Trigger.OnAllKilled(runners, CheckVillagerObjective)
	local camera = Actor.Create("camera.small", true, { Owner = Nod, Location = SouthVillageEntry.Location })

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local path = { SouthVillageEntry.Location, SouthVillageEntry.Location + CVec.New(0, -2) }
		Reinforcements.Reinforce(Nod, { "ftnk" }, path)
		Media.PlaySpeechNotification(Nod, "Reinforce")
		camera.Destroy()
	end)
end

--- Reinforce Nod to resolve loose ends in the main village. This will speed
--- things along if Nod is close to victory but on the opposite end of the map.
local function ScheduleVillageSweepers()
	if #Utils.Where(Villagers.GetActors(), IsWestVillager) == 0 then
		return
	end

	if not AreSweepersReady() then
		Trigger.AfterDelay(10, ScheduleVillageSweepers)
		return
	end

	SweepersRequested = true

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Media.PlaySpeechNotification(Nod, "Reinforce")
		local path = { WestVillageEntry.Location, Intersection.Location }
		local types = { "ftnk", "e5", "e5", "e5", "e5", "e5" }
		Reinforcements.Reinforce(Nod, types, path, 10)
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		SweepersRequested = false
	end)
end

--- Airlift an infantry team to avenge GDI units near the intersection.
local function ReinforceVillageHelicopter()
	local path = { RandomEastEntry().Location, Waypoint0.Location }
	local passengerTypes = { "e2", "e2", "e2", "e3", "e3" }
	local passengers = Reinforcements.ReinforceWithTransport(GDI, "tran", passengerTypes, path, { path[1] })[2]

	Utils.Do(passengers, function(passenger)
		IdleHunt(passenger)
		TallyDeath(passenger)
	end)
end

--- Airlift an infantry team to hunt near the south edge's river crossing.
local function ReinforceRiverHelicopter()
	local path = { SouthRiverEntry.Location, Waypoint2.Location }
	local exit = RandomEastEntry().Location
	local passengerTypes = { "e1", "e1", "e1", "e1", "e3" }
	local passengers = Reinforcements.ReinforceWithTransport(GDI, "tran", passengerTypes, path, { exit })[2]

	Utils.Do(passengers, function(passenger)
		TallyDeath(passenger)

		Trigger.OnAddedToWorld(passenger, function()
			-- IdleHunt on its own may cause some of these infantry to take a
			-- long route around the map because the narrow canyon is occupied.
			passenger.AttackMove(SouthRiverRally.Location)
			passenger.AttackMove(SouthBridgeRally.Location)
			IdleHunt(passenger)
		end)
	end)
end

--- Reinforce some scouts to counterattack at the south gate.
local function ReinforceEdgeHumvees()
	local entry = RandomEastEntry()
	local path = { entry.Location, entry.Location + CVec.New(-2, 0) }

	Reinforcements.Reinforce(GDI, { "jeep", "jeep" }, path, 25, function(humvee)
		humvee.AttackMove(EastHumveeRally.Location, 2)
		humvee.AttackMove(SouthGate.Location, 2)
		humvee.AttackMove(Waypoint3.Location, 2)
		IdleHunt(humvee)
		TallyDeath(humvee)
	end)
end

--- Order a group to intercept (or chase away) nearby intruders when idle.
--- This is intended to feel similar to GUARD_AREA orders in TD '95.
---@param guards actor[] List of guards.
---@param center wpos Center of the group's search area.
---@param range wdist Radius of the area to guard.
local function GuardArea(guards, center, range)
	local activated = false
	local refreshed = false
	guards = Utils.Where(guards, IsLiveAreaGuard)

	if #guards == 0 then
		return
	end

	Trigger.OnEnteredProximityTrigger(center, range, function(target, id)
		if activated or not IsMobileNod(target) then
			return
		end

		activated = true
		Trigger.RemoveProximityTrigger(id)

		Utils.Do(guards, function(guard)
			if not IsLiveAreaGuard(guard) or not guard.IsIdle then
				return
			end

			guard.AttackMove(target.Location, 2)
			guard.AttackMove(guard.Location)
		end)
	end)

	Utils.Do(guards, function(guard)
		guard.Stance = "Defend"

		Trigger.OnIdle(guard, function()
			-- Does at least one guard remain after an intruder search?
			-- If so, reset the guards and prepare another search area.
			if refreshed or not activated or not Utils.All(guards, IsDeadOrIdle) then
				return
			end

			refreshed = true
			local survivors = Utils.Where(guards, IsLiveAreaGuard)

			Utils.Do(survivors, function(survivor)
				Trigger.Clear(survivor, "OnIdle")
			end)

			Trigger.AfterDelay(10, function()
				GuardArea(survivors, center, range)
			end)
		end)
	end)
end

--- Order a random armed villager in the west to begin hunting.
local function SendVillagerHunter()
	local villagers = Utils.Where(Villagers.GetGroundAttackers(), IsWestVillager)
	if #villagers == 0 then
		return
	end

	local guard = Utils.Random(villagers)
	IdleHunt(guard)
end

--- Send an available unit to defend a damaged base structure.
---@param location cpos Location of the structure attacker.
local function SendBaseGuard(location)
	local guards = Utils.Where(GDI.GetGroundAttackers(), CanDefendBase)
	if #guards == 0 then
		return
	end

	local guard = Utils.Random(guards)
	guard.AttackMove(location, 2)
	IdleHunt(guard)
end

--- Prepare simple events for the pre-placed villagers.
local function PrepareVillagers()
	Trigger.OnPlayerDiscovered(Villagers, function(_, discoverer)
		if discoverer == Nod then
			AddVillagerObjective(true)
		end
	end)

	local mobiles = Utils.Where(Villagers.GetActors(), function(actor)
		return actor.HasProperty("Move")
	end)

	Trigger.OnAllKilled(mobiles, CheckVillagerObjective)
end

--- Place all the footprint triggers needed at startup.
local function PrepareFootprints()
	local footprints =
	{
		{
			action = ReinforceRiverHelicopter,
			cells =
			{
				CPos.New(28, 43),
				CPos.New(28, 44),
				CPos.New(28, 45),
				CPos.New(28, 46)
			}
		},
		{
			action = ReinforceEdgeHumvees,
			cells =
			{
				CPos.New(49, 20),
				CPos.New(50, 20),
				CPos.New(51, 20),
				CPos.New(52, 20),
				CPos.New(53, 20)
			}
		},
		{
			action = RevealWestBaseEntrance,
			cells =
			{
				CPos.New(22, 11),
				CPos.New(22, 12),
				CPos.New(22, 13),
				CPos.New(22, 14),
				CPos.New(22, 15),
				CPos.New(22, 16)
			}
		}
	}

	Utils.Do(footprints, function(footprint)
		local activated = false

		Trigger.OnEnteredFootprint(footprint.cells, function(actor, id)
			if activated or not IsMobileNod(actor) then
				return
			end

			activated = true
			Trigger.RemoveFootprintTrigger(id)
			footprint.action()
		end)
	end)
end

--- Set up the flares and objective for GDI's bio centers.
local function PrepareBioCenters()
	local centers = { Bio1, Bio2, Bio3 }
	local offset = CVec.New(1, 0)

	Utils.Do(centers, function(center)
		Trigger.OnKilled(center, function()
			Actor.Create("flare", true, { Owner = GDI, Location = center.Location + offset })
		end)
	end)

	Trigger.OnAllKilled(centers, function()
		AddVillagerObjective()
		ScheduleVillageSweepers()
		Nod.MarkCompletedObjective(BioObjective)
	end)
end

--- Prepare GDI structures' survival objective, repairs, and calls for help.
local function PrepareBase()
	local baseStructures = Utils.Where(GDI.GetActors(), function(actor)
		return actor.HasProperty("StartBuildingRepairs") and actor.Type ~= "bio"
	end)

	Utils.Do(baseStructures, function(structure)
		Trigger.OnKilled(structure, function()
			Nod.MarkFailedObjective(IntactObjective)
		end)

		RepairBuilding(GDI, structure, 0.75)

		-- Do not guard defenses or the isolated Communications Center.
		if structure.HasProperty("Attack") or structure.Type == "hq" then
			return
		end

		Trigger.OnDamaged(structure, function(_, attacker)
			if attacker.Owner == structure.Owner or attacker.IsDead then
				return
			end

			SendBaseGuard(attacker.Location)
		end)
	end)
end

--- Prepare certain groups to begin area guarding. Some infantry that did area
--- guard have been skipped because their ORA range makes this feel redundant.
local function PrepareAreaGuards()
	local teams =
	{
		{
			actors = Map.ActorsWithTag("West Gate Grenadier"),
			range = 5
		},
		{
			actors = Map.ActorsWithTag("Village Entrance Guard"),
			center = VillageEntranceGrenadier.CenterPosition,
			range = 5
		},
		{
			actors = Map.ActorsWithTag("Second House Guard"),
			center = VillageHouse2.CenterPosition,
		},
		{
			actors = { HilltopHumvee }
		},
		{
			actors = { IntersectionHumvee }
		},
		{
			actors = { CommunicationsHumvee },
		},
		{
			actors = Map.ActorsWithTag("Communications Guard"),
			center = CommunicationsGrenadier.CenterPosition,
			range = 6
		},
		{
			actors = { BioGrenadier },
			range = 5
		}
	}

	Utils.Do(teams, function(team)
		local radius = WDist.FromCells(team.range or 7)
		local center = team.center or team.actors[1].CenterPosition
		GuardArea(team.actors, center, radius)
	end)
end

--- Prepare houses' survival objective, targetable switch, and calls for help.
local function PrepareVillageHouses()
	if Difficulty == "easy" then
		-- Require force-fire to attack houses, as butterfingers insurance.
		Actor.Create("villagetargetableswitch", true, { Owner = Villagers, Location = CPos.Zero })
	end

	local village = Utils.Where(Villagers.GetActors(), function(actor)
		return actor.HasProperty("Health") and not actor.HasProperty("Move")
	end)

	Utils.Do(village, function(house)
		Trigger.OnKilled(house, function()
			Nod.MarkFailedObjective(IntactObjective)
		end)

		-- Only houses inside the main village should call for help.
		if house.Location.X >= WestGateReveal.Location.X then
			return
		end

		local hunterCalled = false

		Trigger.OnDamaged(house, function()
			if hunterCalled or house.IsDead then
				return
			end

			hunterCalled = true
			SendVillagerHunter()
			Trigger.Clear(house, "OnDamaged")
		end)
	end)
end

--- Prepare the reveals and patrol for Hard's Mammoth Tank.
---@param tank actor
local function PrepareMammoth(tank)
	local path = { MammothPatrolPoint1.Location, MammothPatrolPoint2.Location, MammothPatrolPoint3.Location, MammothPatrolPoint4.Location }
	local encountered = false

	Trigger.OnEnteredProximityTrigger(MammothEntry.CenterPosition, WDist.FromCells(9), function(actor, id)
		if encountered or not IsMobileNod(actor) then
			return
		end

		encountered = true
		Trigger.RemoveProximityTrigger(id)
		tank.Patrol(path, true, DateTime.Seconds(3))
		local camera = Actor.Create("camera.small", true, { Owner = Nod, Location = MammothPatrolPoint1.Location })
		Trigger.AfterDelay(DateTime.Seconds(6), camera.Destroy)
	end)

	Trigger.OnEnteredProximityTrigger(MammothGateReveal.CenterPosition, WDist.FromCells(4), function(actor, id)
		if actor.Type ~= "htnk" then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		local camera = Actor.Create("camera", true, { Owner = Nod, Location = MammothGateReveal.Location })
		Trigger.AfterDelay(DateTime.Seconds(6), camera.Destroy)
	end)
end

--- Spawn some extra units for the light "remix" of Hard difficulty.
local function SpawnHardUnits()
	if Difficulty ~= "hard" then
		return
	end

	local spawns =
	{
		{
			type = "apc",
			cell = ApcEntry.Location,
			facing = Angle.SouthWest
		},
		{
			type = "htnk",
			cell = MammothEntry.Location,
			facing = Angle.West,
			action = PrepareMammoth
		},
		{
			type = "e2",
			cell = SouthGateGrenadierEntry.Location,
			facing = Angle.SouthEast,
			stance = "Defend"
		},
		{
			type = "e3",
			cell = SouthGateRocketEntry.Location,
			facing = Angle.SouthWest,
			stance = "Defend"
		},
		{
			type = "mtnk",
			cell = Waypoint0.Location,
			facing = Angle.North,
			stance = "Defend"
		},
		{
			type = "e3",
			cell = CentralFordEntry1.Location
		},
		{
			type = "e3",
			cell = CentralFordEntry2.Location
		},
		{
			type = "e3",
			cell = CentralFordEntry3.Location
		},
		{
			type = "mtnk",
			cell = SouthFordEntry.Location,
			stance = "Defend"
		}
	}

	Utils.Do(spawns, function(spawn)
		local facing = spawn.facing or Angle.West
		local actor = Actor.Create(spawn.type, true, { Owner = GDI, Location = spawn.cell, Facing = facing })
		actor.Stance = spawn.stance or actor.Stance

		if spawn.action then
			spawn.action(actor)
		end
	end)
end

WorldLoaded = function()
	InitObjectives(Nod)
	BioObjective = AddPrimaryObjective(Nod, "destroy-gdi-bio-centers")
	IntactObjective = AddPrimaryObjective(Nod, "keep-all-other-structures")

	PrepareFootprints()
	PrepareBase()
	PrepareBioCenters()
	PrepareVillageHouses()

	Utils.Do(GDI.GetGroundAttackers(), TallyDeath)
	PrepareVillagers()
	PrepareAreaGuards()
	SpawnHardUnits()
	ReinforceFirstNod()

	GDI.Cash = 5000
	Camera.Position = FlameRally2.CenterPosition

	Trigger.OnKilled(BridgeTank, ReinforceNodTank)
	local intersectionTeam = { IntersectionHumvee, IntersectionRifle1, IntersectionRifle2, IntersectionRifle3 }
	Trigger.OnAllKilled(intersectionTeam, ReinforceVillageHelicopter)

	Trigger.OnKilled(IntersectionTank, function()
		Trigger.AfterDelay(DateTime.Seconds(1), ReinforceVillageFlameTank)
	end)
end

Tick = function()
	if IsNodDead() then
		MarkNodDead()
	end

	if Nod.IsObjectiveCompleted(BioObjective) and Nod.IsObjectiveCompleted(VillagerObjective) then
		Nod.MarkCompletedObjective(IntactObjective)
	end
end
