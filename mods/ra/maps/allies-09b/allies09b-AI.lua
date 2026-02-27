--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SetDifficulty = function()
	if Difficulty == "easy" then

		DestroyHelperCameras = false

		WaterTransDelays = DateTime.Minutes(7)
		SubAttackGroupSize = 1

		ParadropDelay = DateTime.Minutes(6)
		ParabombDelay = DateTime.Minutes(7)

		FirstAirDelays = DateTime.Seconds(180)

	elseif Difficulty == "normal" then

		DestroyHelperCameras = true

		WaterTransDelays = DateTime.Minutes(6)
		SubAttackGroupSize = 2

		ParadropDelay = DateTime.Minutes(5)
		ParabombDelay = DateTime.Minutes(6)

		FirstAirDelays = DateTime.Seconds(120)

		--DateTime.TimeLimit = DateTime.Minutes(5) + DateTime.Seconds(3)
		--InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
		--InfantryDelay = DateTime.Seconds(18)
		--AttackGroupSize = 5
	elseif Difficulty == "hard" then

		DestroyHelperCameras = true

		WaterTransDelays = DateTime.Minutes(5)
		SubAttackGroupSize = 3

		ParadropDelay = DateTime.Minutes(4)
		ParabombDelay = DateTime.Minutes(5)

		FirstAirDelays = DateTime.Seconds(120)
		--DateTime.TimeLimit = DateTime.Minutes(3) + DateTime.Seconds(3)
		--InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
		--InfantryDelay = DateTime.Seconds(10)
		--VehicleTypes = { "ftrk" }
		--VehicleDelay = DateTime.Seconds(30)
		--AttackGroupSize = 7
	end
end

--------------------------------------------------------------------
-----------------	    DATA BLOCK - START	------------------------
--------------------------------------------------------------------

InfantryUnits =
{
	hard = { "e1", "e2", "e2", "e4", "e4" },
	normal = { "e1", "e1", "e2", "e2", "e4" },
	easy = { "e1", "e1", "e1", "e2", "e2" }
}
AttackProductionInterval =
{
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(40),
	hard = DateTime.Seconds(20)
}
WTransWays =
{
	{ USSRRFEntry1.Location, USSRUnload1.Location },
	{ USSRRFEntry1.Location, USSRUnload2.Location }
}
WTransUnits =
{
	hard = { { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl" }, { "v2rl", "v2rl", "e4", "e4", "3tnk" } },
	normal = { { "e1", "e1", "3tnk", "3tnk", "v2rl" }, { "e4", "e4", "e4", "e4", "v2rl" } },
	easy = { { "e1", "e1", "e1", "e2", "e2" }, { "e2", "3tnk", "3tnk" } }
}

InfantryAttackGroup = { }
InfantryAttackGroupSize = 5

PawPatrolPath = { BasePawPatrolWP1.Location, BasePawPatrolWP2.Location, BasePawPatrolWP3.Location, BasePawPatrolWP4.Location }

VehicleTypes = { "3tnk", "3tnk", "3tnk", "v2rl" }
VehicleAttackGroup = { }
VehicleAttackGroupSize = 3

LandAtkPaths = { LowerBaseProximityCam.Location }

NavalAtkProcedure = 0
NavalAtkType = "" -- To check whatto build on Spen

SubTypes = { "ss" }
SubAttackGroup = { }
NavalAtkPath = { NavalAtkWP1.Location, NavalAtkWP2.Location, NavalAtkWP3.Location }

LSTTypes = { "lst" }
LSTLimitAmount = 2

LSTLoadPoints = { LoadLst1.Location, LoadLst2.Location }
LSTUnloadPoints = {USSRUnload3.Location, USSRUnload4.Location}

AircraftTypes = { "yak" }
PlanesAttackGroup = { }

BaseBlueprints =
{
	{ type = "apwr", actor = BasePower1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "apwr", actor = BasePower2, cost = 500, shape = { 3, 3 }, location = CPos.New(83, 23) },
	{ type = "apwr", actor = BasePower3, cost = 500, shape = { 3, 3 }, location = CPos.New(86, 23) },
	{ type = "apwr", actor = BasePower4, cost = 500, shape = { 3, 3 }, location = CPos.New(71, 39) },
	{ type = "apwr", actor = BasePower5, cost = 500, shape = { 3, 3 }, location = CPos.New(92, 29) },
	{ type = "apwr", actor = BasePower6, cost = 500, shape = { 3, 3 }, location = CPos.New(93, 32) },
	{ type = "apwr", actor = BasePower7, cost = 500, shape = { 3, 3 }, location = CPos.New(93, 37) },

	{ type = "proc", actor = BaseRef, cost = 1400, shape = { 3, 4 }, location = CPos.New(72,28) },

	{ type = "barr", actor = BaseBarr1, cost = 400, shape = { 2, 3 }, location = CPos.New(77, 30)--[[, onBuilt = ProduceInfantry]] },
	{ type = "barr", actor = BaseBarr2, cost = 400, shape = { 2, 3 }, location = CPos.New(78, 34)--[[, onBuilt = ProduceInfantry]]  },
	{ type = "weap", actor = BaseWeap, cost = 2000, shape = { 3, 3 }, location = CPos.New(82, 31)--[[, onBuilt = ProduceArmor ]] },
	{ type = "spen", actor = BaseSpen, cost = 800, shape = { 3, 3 }, location = CPos.New(60, 36) },
	{ type = "kenn", actor = BaseKenn1, cost = 200, shape = { 1, 1 }, location = CPos.New(71, 32) },
	{ type = "kenn", actor = BaseKenn2, cost = 200, shape = { 1, 1 }, location = CPos.New(79,23) },

	{ type = "afld", actor = BaseAfld, cost = 500, shape = { 1, 1 }, location = CPos.New(89, 36)--[[, onBuilt = ProduceAircraft ]] },

	{ type = "dome", actor = BaseDome, cost = 1500, shape = { 2, 3 }, location = CPos.New(88, 32) },
	{ type = "stek", actor = BaseStek, cost = 1500, shape = { 3, 3 }, location = CPos.New(76, 23) },

-- Main gate ref pos CVec.New(-3,14)
	{ type = "ftur", actor = BaseFtur1, cost = 600, shape = { 1, 1 }, location = CPos.New(80, 39) },
	{ type = "ftur", actor = BaseFtur2, cost = 600, shape = { 1, 1 }, location = CPos.New(86, 39) },
	{ type = "tsla", actor = BaseTsla1, cost = 1200, shape = { 1, 1 }, location = CPos.New(79, 39) },
	{ type = "tsla", actor = BaseTsla2, cost = 1200, shape = { 1, 1 }, location = CPos.New(87, 39) },
-- Side gate ref pos CVec.New(-15,6)
	{ type = "ftur", actor = BaseFtur3, cost = 1200, shape = { 1, 1 }, location = CPos.New(70, 32) },
	{ type = "ftur", actor = BaseFtur4, cost = 1200, shape = { 1, 1 }, location = CPos.New(70, 35) },
	{ type = "tsla", actor = BaseTsla3, cost = 1200, shape = { 1, 1 }, location = CPos.New(71, 29) },
	{ type = "tsla", actor = BaseTsla4, cost = 1200, shape = { 1, 1 }, location = CPos.New(62, 34) }
}

--------------------------------------------------------------------
-----------------	    DATA BLOCK - END	------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	UTILS BLOCK - START	----------------------------
--------------------------------------------------------------------
local function __UTILS__() end

IsHarvesterMissing = function()
	return #USSR.GetActorsByType("harv") == 0
end

CheckPlayerMoney = function(owner)
	return owner.Cash + owner.Resources
end

GrantCash = function(player, amount)
    player.Cash = player.Cash + amount
end

--------------------------------------------------------------------
-----------------	UTILS BLOCK - END	----------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	BASE MANAGMENT BLOCK - START	----------------
--------------------------------------------------------------------
local function __BASE_MANAGEMENT__() end

---@param blueprints blueprint[]
---@param cyard any
---@param owner player
BuildBase = function(blueprints, cyard, owner)
	for _, blueprint in pairs(BaseBlueprints) do
		if not blueprint.actor then
			--Media.Debug("type: " .. tostring(blueprint.type))
			--Media.Debug("actor: " .. tostring(blueprint.actor))
			--Media.Debug("cost: " .. tostring(blueprint.cost))
			--Media.Debug("shape: " .. tostring(blueprint.shape))
			--Media.Debug("location: " .. tostring(blueprint.location))
			--Media.Debug("onBuilt: " .. tostring(blueprint.OnBuilt))

			--Media.Debug("A: " .. tostring(BaseBlueprints[13].OnBuilt))
			BuildBlueprint(blueprint, owner)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		BuildBase(blueprints, cyard, owner)
	end)
end

BuildBlueprint = function(blueprint, owner)
	Trigger.AfterDelay(Actor.BuildTime(blueprint.type), function()
		if BaseFact.IsDead or BaseFact.Owner ~= USSR then
			return
		elseif CheckPlayerMoney(owner) <= 299 and IsHarvesterMissing() then
			return
		end

		if IsBuildAreaBlocked(USSR, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(1--[[5]]), function()
				BuildBlueprint(blueprint, owner)
			end)
			return
		end

		local actor = Actor.Create(blueprint.type, true, { Owner = USSR, Location = blueprint.location })
		OnBlueprintBuilt(actor, blueprint)

		Trigger.AfterDelay(DateTime.Seconds(1--[[10]]), function()
			BuildBase(BaseBlueprints, BaseFact, USSR)
		end)
	end)
end

OnBlueprintBuilt = function(actor, blueprint)
	USSR.Cash = USSR.Cash - blueprint.cost
	blueprint.actor = actor

	MaintainBuilding(actor, blueprint, 0.75)

	if blueprint.onBuilt then
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			actor.Kill()
		end)

		-- Build() will not work properly on producers if immediately called.

		Trigger.AfterDelay(1, function()
			blueprint.onBuilt(actor)
		end)
	end

end

IsBuildAreaBlocked = function(player, blueprint)
	local nw, se = blueprint.northwestEdge, blueprint.southeastEdge
	local blockers = Map.ActorsInBox(nw, se, function(actor)
		-- Neutral check is for ignoring trees near the refinery.
		return actor.Owner ~= Neutral and actor.CenterPosition.Z == 0 and actor.HasProperty("Health")
	end)

	if #blockers == 0 then
		return false
	end

	ScatterBlockers(player, blockers)
	return true
end

ScatterBlockers = function(player, actors)
	Utils.Do(actors, function(actor)
		if actor.IsIdle and actor.Owner == player and actor.HasProperty("Scatter") then
			actor.Scatter()
		end
	end)
end

BeginBaseMaintenance = function()
	Utils.Do(BaseBlueprints, function(blueprint)
		MaintainBuilding(blueprint.actor, blueprint)
	end)
	Utils.Do(USSR.GetActors(), function(actor)
		if actor.HasProperty("StartBuildingRepairs") then
			MaintainBuilding(actor, nil, 0.75)
		end
	end)
end

MaintainBuilding = function(actor, blueprint, repairThreshold)
	if blueprint then
		Trigger.OnKilled(actor, function() blueprint.actor = nil end)
		Trigger.OnSold(actor, function() blueprint.actor = nil end)
		if not blueprint.northwestEdge then
			PrepareBlueprintEdges(blueprint)
		end
	end

	if repairThreshold then
		local original = actor.Owner

		Trigger.OnDamaged(actor, function()
			if actor.Owner ~= original or actor.Health > actor.MaxHealth * repairThreshold then
				return
			end

			actor.StartBuildingRepairs()
		end)
	end
end

PrepareBlueprintEdges = function(blueprint)
	local shapeX, shapeY = blueprint.shape[1], blueprint.shape[2]
	local northwestEdge = Map.CenterOfCell(blueprint.location) + WVec.New(-512, -512, 0)
	local southeastEdge = northwestEdge + WVec.New(shapeX * 1024, shapeY * 1024, 0)

	blueprint.northwestEdge = northwestEdge
	blueprint.southeastEdge = southeastEdge
end

ProduceHarvester = function(producer, owner, delay)
	if CheckPlayerMoney(owner) < Actor.Cost("harv") then
		return
	end

	local toBuild = { "harv" }
	Greece.Build(toBuild, function()
		Trigger.AfterDelay(delay, function()
			ProduceArmor(factory)
		end)
	end)
end

--------------------------------------------------------------------
-----------------	BASE MANAGMENT BLOCK - END	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------		ATTACKING BLOCK - START	--------------------
--------------------------------------------------------------------
local function ________AI_ATTACKS________() end

SendUnits = function(units, path)
	Utils.Do(units, function(unit)
		if unit.IsDead then
			return
		end

		unit.Patrol(path, false)
		IdleHunt(unit)
	end)
end

-----------------------
--- Inf Attacks     ---
-----------------------
local function __INF_ATTACKS__() end

BarrAvailableCheck = function(producer, owner)
	if not producer.IsDead or producer.Owner == owner then
		return true
	else
		return false
	end
end

KennAvailableCheck = function(producer, owner)
	if not producer.IsDead or producer.Owner == owner then
		return
	end
end

-- A check is needed for when there are more than one producer structure of the same type 
ProduceInfantry = function(producer, owner)
	if not BarrAvailableCheck(producer, owner) then
		--Media.Debug("Out of ProduceInfantry")
		return
	elseif CheckPlayerMoney(owner) <= 299 and IsHarvesterMissing() then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(InfantryUnits) }

	owner.Build(toBuild, function(units)
		table.insert(InfantryAttackGroup, units[1])

		if #InfantryAttackGroup >= InfantryAttackGroupSize then
			SendUnits(InfantryAttackGroup, LandAtkPaths)
			InfantryAttackGroup = { }
			Trigger.AfterDelay(AttackProductionInterval, function()
				ProduceInfantry(producer, owner)
			end)
		else
			Trigger.AfterDelay(delay, function()
				ProduceInfantry(producer, owner)
			end)
		end
	end)
end

DefendAgainstInfiltration = function(owner, dog)
	if not BaseKenn1.IsDead or BaseKenn1.Owner ~= owner then
		spawnPoint = BaseKenn1.Location
	elseif not BaseKenn2.IsDead or BaseKenn2.Owner ~= owner then
		spawnPoint = BaseKenn2.Location
	else
		return
	end

	local paw_patrol = nil

	if dog == nil then
		paw_patrol = Reinforcements.Reinforce(USSR, {"dog"}, {spawnPoint, spawnPoint + CVec.New(-1,1)})[1]
	else
		paw_patrol = dog
	end

	--Trigger.OnKilled(paw_patrol, function()
	--	Trigger.AfterDelay(DateTime.Seconds(30), function()
	--		DefendAgainstInfiltration(USSR)
	--	end)
	--end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		if not paw_patrol.IsDead then
			paw_patrol.Patrol(PawPatrolPath, true, DateTime.Seconds(6))
		end
	end)
end

-----------------------
--- Tank Attacks    ---
-----------------------
local function __TANK_ATTACKS__() end

GuardLeaders =
{
	{ exists = false, location },
	{ eixsts = false, location },
	{ exists = false, location }
}

GuardPoints = { GuardPoint1.Location, GuardPoint2.Location, GuardPoint3.Location }

GuardsOfPoints = { 
	{ leader = "", inf = { "e1", "e2", "e1", "e2" },  armor = { "3tnk", "3tnk", "v2rl" }, A },
	{ leader = "", inf = { "e1", "e2", "e1", "e2" },  armor = { "4tnk", "3tnk", "v2rl" }, B },
	{ leader = "", inf = { "e1", "e2", "e1", "e2" },  armor = { "3tnk", "v2rl", "v2rl" }, C }
}
-- GuardsLeaders
ProximityTriggers = { GuardPoint1.CenterPosition, Actor263.CenterPosition, Actor264.CenterPosition  }

--nw local northLeftEdge = WPos.New( (CPos.New(43, 22)).X * 1024,  (CPos.New(43, 22)).Y * 1024, 0)
--se local southRightEdge = WPos.New( (CPos.New(96, 67)).X * 1024, (CPos.New(96, 67)).Y * 1024, 0)

--Trigger.AfterDelay(DateTime.Minutes(5), function()

SetAIBehavior = function()


end

--CheckSecuredArea(northLeftEdge, southRightEdge)

DefensiveActivities = false
OffensiveActivities = false

CheckAIActivities = function()

end

PrepareGroupToBuild = function()
	if DefensiveActivities then
		local group = Utils.Random()
	end
end

CheckSecuredArea = function(nw, se)
    local actors = Map.ActorsInBox( nw, se, function(actor)
		return actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs")
    end)

	if #actors > 0 then
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			CheckSecuredArea(nw, se)
		end)
	end
end

WeapAvailableCheck = function(producer, owner)
	if not producer.IsDead or producer.Owner == owner then
		return true
	else
		return false
	end
end

ProduceArmor = function(producer, owner)
	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))

	if not WeapAvailableCheck(producer, owner) then
		--Media.Debug("Out of - ProduceArmor")
		return
	elseif IsHarvesterMissing() then
		--ProduceHarvester(factory, owner, delay)
		--return
	end

	local toBuild = { Utils.Random(VehicleTypes) }
	local target = {}

	USSR.Build(toBuild, function(units)
		table.insert(VehicleAttackGroup, units[1])

		if #VehicleAttackGroup >= VehicleAttackGroupSize then
			SendUnits(VehicleAttackGroup, LandAtkPaths)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(DateTime.Minutes(3), function()
				ProduceArmor(producer, owner)
			end)
		else
			Trigger.AfterDelay(delay, function()
				ProduceArmor(producer, owner)
			end)
		end
	end)
end

ProduceArmorGuards = function(producer, owner)
	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))

	if WeapAvailableCheck(producer, owner) then
		return
	elseif IsHarvesterMissing then
		ProduceHarvester(factory, owner, delay)
	end

	local toBuild = { Utils.Random(VehicleTypes) }
	local target = {  }
end

-----------------------
--- Air  Attacks   ----
-----------------------
local function __AIR_ATTACKS__() end

BasePlanes = {}
TotalAflds = 1

AfldAvailableCheck = function(producer, owner)
	if not producer.IsDead or producer.Owner == owner then
		return true
	else
		return false
	end
	TotalAflds = USSR.GetActorsByType("afld")
end

ProduceAircraft = function(producer, owner)
    if not AfldAvailableCheck(producer, owner) then
        return
    end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(AircraftTypes) }

    USSR.Build(toBuild, function(units)
        local plane = units[1]
		table.insert(BasePlanes, plane)
        PlanesAttackGroup[#PlanesAttackGroup + 1] = plane

        Trigger.OnKilled(plane, function()
			table.remove(BasePlanes)
			Trigger.AfterDelay(delay, function()
				ProduceAircraft(producer, owner)
			end)
		end)
        InitializeAttackAircraft(plane, Greece)
    end)
end

--Out of map attacks
SendParabombs = function()
	if BaseAfld.IsDead or BaseAfld.Owner ~= USSR then
		return
	end

	local airfield = BaseAfld
	local targets = Utils.Where(Greece.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
			actor.Type ~= "brik" and
			actor.Type ~= "sbag" or
			actor.Type == "pdox" or
			actor.Type == "atek"
	end)
	if #targets > 0 then
		airfield.TargetAirstrike(Utils.Random(targets).CenterPosition, Angle.NorthEast)
	end

	Trigger.AfterDelay(ParabombDelay, SendParabombs)
end

-- Maybe only leave this for hard difficulty
SendParadrop = function()
	if BaseAfld.IsDead or BaseAfld.Owner ~= USSR then
		return
	end

	local aircraft = ParadropProxy.TargetParatroopers(KosyginExtractPoint.CenterPosition)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
	Trigger.AfterDelay(ParadropDelay, SendParadrop)
end

CurrentAirWave = 1

PrepareAircraftReinforcements = function()
	local delay = DateTime.Seconds(10)--FirstAirDelays[Difficulty] or FirstAirDelays["normal"]

	Trigger.AfterDelay(delay, function()
		ScheduleAirWave(1)
	end)
	--Media.Debug("Prepare Air Attack")
end

--[[]]
HasAirfield = function(player)
	return player.HasPrerequisites({ "afld" })
end

---@param wave integer
ScheduleAirWave = function(wave)
	local team = SovietAirTeams[wave]
	if not team then
		team = SovietAirTeams[#SovietAirTeams]
		--return
	end
	Trigger.AfterDelay(team.interval, function()
		-- The last team was defeated before its scheduled repeat.
		if CurrentAirWave > wave then
			return
		end

		local units = Reinforcements.Reinforce(team.owner or USSR, team.types, team.path)
		ScheduleAirWave(wave)

		Utils.Do(units, function(unit)
			InitializeAttackAircraft(unit, Greece)

			Trigger.OnIdle(unit, function()
				if unit.AmmoCount() > 0 --[[or HasAirfield(unit.Owner)]] then -- #BasePlanes < TotalAflds
					--Media.Debug("On Idle - return")
					table.insert(BasePlanes, unit)
					return
				elseif HasAirfield(unit.Owner) and #BasePlanes < TotalAflds then
					return
				end
				OnAircraftStranded(unit, team.path[1])

			end)
		end)

		Trigger.OnAllRemovedFromWorld(units, function()
			if AreSovietPlanesActive() then
				--Media.Debug("Does it enters here?")
				return
			end

			--[[
			if team.onWaveDefeated then
				team.onWaveDefeated()
			end
			]]
			CurrentAirWave = CurrentAirWave + 1
			ScheduleAirWave(CurrentAirWave)
		end)
	end)
end

OnAircraftStranded = function(aircraft, exit)
	--Media.Debug("Stranded check")
	local oldOwner = aircraft.Owner
	--[[
	if oldOwner == USSR and HasAirfield(BadGuy) then
		aircraft.Owner = BadGuy
	elseif oldOwner == BadGuy and HasAirfield(USSR) then
		aircraft.Owner = USSR
	end
	]]
	if oldOwner == aircraft.Owner then
		--Media.Debug("Send aircraft to elimination")
		aircraft.Stop()
		aircraft.Move(exit)
		--aircraft.Move(exit)
		aircraft.Destroy()
	end
end

AreSovietPlanesActive = function()
	local planes = { "mig", "yak" }
	return #USSR.GetActorsByTypes(planes) > 0
end

---@type { interval: number, types: string[], path: cpos[], owner: player, onWaveDefeated: fun() }[]
SovietAirTeams =
{
	{ types = { "yak", "yak" }, interval = DateTime.Seconds(120), path = { USSRAircraftOrigin1.Location }},
	{ types = { "yak", "yak" }, interval = DateTime.Seconds(110), path = { USSRAircraftOrigin1.Location }},
	{ types = { "yak", "mig" }, interval = DateTime.Seconds(110), path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) }	},
	{ types = { "yak", "yak", "yak" }, interval = DateTime.Seconds(219),  path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) } },
	-- Original includes 2x Hind. Replaced with Yaks.
	{ types = { "yak", "yak", "mig" }, interval = DateTime.Seconds(210), path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) } }
}

-----------------------
--- Naval Attacks  ----
-----------------------
local function __NAVAL_ATTACKS__() end

SpenAvailableCheck = function(producer, owner)
	if not producer.IsDead and producer.Owner == owner then
		return true
	else
		return false
	end
end

PrepareAttackOptions = function()
	if NavalAtkProcedure >= 3 then
		NavalAtkType = "lst"
		NavalAtkProcedure = 0
	else
		NavalAtkType = "sub"
		NavalAtkProcedure = NavalAtkProcedure + 1
	end
end

PrepareNavalAtk = function(producer, owner)
	PrepareAttackOptions()

	if NavalAtkType == "lst" and #owner.GetActorsByType("lst") < 2 then
		ProduceLST(producer, owner)
	else
		ProduceSubs(producer, owner)
	end
end

ProduceSubs = function(producer, owner)
	if not SpenAvailableCheck(producer, owner) then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))

	owner.Build(SubTypes, function(units)
		table.insert(SubAttackGroup, units[1])

		if #SubAttackGroup >= SubAttackGroupSize then
			SendUnits(SubAttackGroup, NavalAtkPath)
			SubAttackGroup = { }
			Trigger.AfterDelay(AttackProductionInterval, function()
				PrepareNavalAtk(producer, owner)
			end)
		else
			Trigger.AfterDelay(delay, function()
				ProduceSubs(producer, owner)
			end)
		end
	end)
end

ProduceLST = function(producer, owner)
	if not SpenAvailableCheck(producer, owner) then
		return
	end

	owner.Build(LSTTypes, function(units)
		local OccupiedLST  = units[1]

		Trigger.AfterDelay(AttackProductionInterval, function()
			ProduceSubs(producer, owner)
		end)
	end)
end

--Out of map attacks
WaterLSTWaves = function()
	if BaseSpen.IsDead or BaseSpen.Owner ~= USSR then
		return
	end
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(USSR, "lst", units, way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(KosyginExtractPoint.Location)
			IdleHunt(a)
		end)
	end)
	Trigger.AfterDelay(WaterTransDelays, WaterLSTWaves)
end

--------------------------------------------------------------------
----------------		ATTACKING BLOCK - END	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------    SPECIAL BEHAVIORS BLOCK - START	----------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------    SPECIAL BEHAVIORS BLOCK - END	----------------
--------------------------------------------------------------------

ActivateAI = function()
	--Media.Debug("Activate AI")

	ParadropProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
	ParabombProxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })

	WTransUnits = WTransUnits[Difficulty]
	AttackProductionInterval = AttackProductionInterval[Difficulty]
	InfantryUnits = InfantryUnits[Difficulty]

	BeginBaseMaintenance()

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		BuildBase(BaseBlueprints, BaseFact, USSR)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function() --Reset to minutes
		ProduceArmor(BaseWeap, USSR)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function() --Reset to minutes
		ProduceInfantry(BaseBarr1, USSR)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function() --Reset to minutes
		PrepareAircraftReinforcements()
		--ProduceAircraft(BaseAfld, USSR)
	end)

	Trigger.AfterDelay(DateTime.Minutes(4), function() --Reset to minutes
		PrepareNavalAtk(BaseSpen, USSR)
	end)

	Trigger.AfterDelay(WaterTransDelays, WaterLSTWaves)
	Trigger.AfterDelay(ParadropDelay, SendParadrop)  --Reset to seconds
	Trigger.AfterDelay(ParabombDelay, SendParabombs)  --Reset to seconds
end
