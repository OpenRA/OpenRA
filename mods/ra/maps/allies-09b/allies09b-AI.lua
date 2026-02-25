--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "easy" then

	DestroyHelperCameras = false

	WTransDelays = 7
	SubAttackGroupSize = 1

	ParadropDelay = 7
	ParabombDelay = 7

elseif Difficulty == "normal" then

	DestroyHelperCameras = true

	WTransDelays = 6
	SubAttackGroupSize = 2

	ParadropDelay = 6
	ParabombDelay = 6

	--DateTime.TimeLimit = DateTime.Minutes(5) + DateTime.Seconds(3)
	--InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	--InfantryDelay = DateTime.Seconds(18)
	--AttackGroupSize = 5
elseif Difficulty == "hard" then

	DestroyHelperCameras = true

	WTransDelays = 5
	SubAttackGroupSize = 3

	ParadropDelay = 5
	ParabombDelay = 5
	--DateTime.TimeLimit = DateTime.Minutes(3) + DateTime.Seconds(3)
	--InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	--InfantryDelay = DateTime.Seconds(10)
	--VehicleTypes = { "ftrk" }
	--VehicleDelay = DateTime.Seconds(30)
	--AttackGroupSize = 7
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
ProductionInterval =
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
VehicleAttackGroup = { }
VehicleAttackGroupSize = 3
SubAttackGroup = { }
AircraftTypes = { "yak" }
SubTypes = { "ss" }
VehicleTypes = { "3tnk", "3tnk", "3tnk", "v2rl" }

--------------------------------------------------------------------
-----------------	    DATA BLOCK - END	------------------------
--------------------------------------------------------------------

--[[
BaseGuardians =
{
	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "v2rl", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	
	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "v2rl", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },

	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "3tnk", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "v2rl", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },

	{ type = "e1", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "e2", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "e4", actor = Apwr1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
}
]]

--------------------------------------------------------------------
-----------------	    DATA BLOCK - START	------------------------
--------------------------------------------------------------------

BaseBlueprints =
{
	{ type = "apwr", actor = BasePower1, cost = 500, shape = { 3, 3 }, location = CPos.New(73, 23) },
	{ type = "apwr", actor = BasePower2, cost = 500, shape = { 3, 3 }, location = CPos.New(83, 23) },
	{ type = "apwr", actor = BasePower3, cost = 500, shape = { 3, 3 }, location = CPos.New(86, 23) },
	{ type = "apwr", actor = BasePower4, cost = 500, shape = { 3, 3 }, location = CPos.New(71, 39) },
	{ type = "apwr", actor = BasePower5, cost = 500, shape = { 3, 3 }, location = CPos.New(92, 29) },
	{ type = "apwr", actor = BasePower6, cost = 500, shape = { 3, 3 }, location = CPos.New(93, 32) },
	{ type = "apwr", actor = BasePower7, cost = 500, shape = { 3, 3 }, location = CPos.New(93, 37) },
	{ type = "dome", actor = BaseDome, cost = 1500, shape = { 2, 3 }, location = CPos.New(88, 32) },
	{ type = "stek", actor = BaseStek, cost = 1500, shape = { 3, 3 }, location = CPos.New(76, 23) },
	{ type = "proc", actor = BaseRef, cost = 1400, shape = { 3, 4 }, location = CPos.New(72,28) },
	{ type = "barr", actor = BaseBarr1, cost = 400, shape = { 2, 3 }, location = CPos.New(77, 30)--[[, onBuilt = ProduceInfantry]] },
	{ type = "barr", actor = BaseBarr2, cost = 400, shape = { 2, 3 }, location = CPos.New(78, 34)--[[, onBuilt = ProduceInfantry]]  },
	{ type = "weap", actor = BaseWeap, cost = 2000, shape = { 3, 3 }, location = CPos.New(82, 31)--[[, onBuilt = ProduceArmor ]] },
	{ type = "spen", actor = BaseSpen, cost = 800, shape = { 3, 3 }, location = CPos.New(60, 36) },
	{ type = "afld", actor = BaseAfld, cost = 500, shape = { 1, 1 }, location = CPos.New(89, 36)--[[, onBuilt = ProduceAircraft ]] },
	{ type = "kenn", actor = BaseKenn1, cost = 200, shape = { 1, 1 }, location = CPos.New(71, 32) },
	{ type = "kenn", actor = BaseKenn2, cost = 200, shape = { 1, 1 }, location = CPos.New(79,23) },
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
-----------------	UTILS BLOCK - START	----------------------------
--------------------------------------------------------------------

IsHarvesterMissing = function()
	return #USSR.GetActorsByType("harv") == 0
end

USSRMoney = function()
	return USSR.Cash + USSR.Resources
end

--------------------------------------------------------------------
-----------------	UTILS BLOCK - END	----------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	BASE MANAGEMENT BLOCK - START	----------------
--------------------------------------------------------------------

BuildBase = function()
	for _, blueprint in pairs(BaseBlueprints) do
		if not blueprint.actor then
			Media.Debug("type: " .. tostring(blueprint.type))
			Media.Debug("actor: " .. tostring(blueprint.actor))
			Media.Debug("cost: " .. tostring(blueprint.cost))
			Media.Debug("shape: " .. tostring(blueprint.shape))
			Media.Debug("location: " .. tostring(blueprint.location))
			Media.Debug("onBuilt: " .. tostring(blueprint.OnBuilt))

			--Media.Debug("A: " .. tostring(BaseBlueprints[13].OnBuilt))
			BuildBlueprint(blueprint)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBlueprint = function(blueprint)
	Trigger.AfterDelay(DateTime.Seconds(1)--[[Actor.BuildTime(blueprint.type)]], function()
		--Media.Debug(tostring(blueprint.type))
		--Media.Debug(tostring(blueprint.type))
		--Media.Debug(tostring(blueprint.actor))
		--Media.Debug(tostring(blueprint.cost))
		--Media.Debug(tostring(blueprint.shape))
		--Media.Debug(tostring(blueprint.location))
		--Media.Debug(tostring(blueprint.onBuilt))

		if BaseFact.IsDead or BaseFact.Owner ~= USSR then
			return
		elseif USSRMoney() <= 299 and IsHarvesterMissing() then
			return
		end

		if IsBuildAreaBlocked(USSR, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(1--[[5]]), function()
				BuildBlueprint(blueprint)
			end)
			return
		end

		local actor = Actor.Create(blueprint.type, true, { Owner = USSR, Location = blueprint.location })
		OnBlueprintBuilt(actor, blueprint)

		Trigger.AfterDelay(DateTime.Seconds(1--[[10]]), BuildBase)
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

--------------------------------------------------------------------
-----------------	BASE BUILDING BLOCK - END	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------		ATTACKING BLOCK - START	--------------------
--------------------------------------------------------------------

ProduceHarvester = function(factory, delay)
	if USSRMoney() < Actor.Cost("harv") then
		return
	end

	local toBuild = { "harv" }
	Greece.Build(toBuild, function()
		Trigger.AfterDelay(delay, function()
			ProduceArmor(factory)
		end)
	end)
end

SendInfantryAttackGroup = function()
	if #InfantryAttackGroup < InfantryAttackGroupSize then
		return
	end
	Utils.Do(InfantryAttackGroup, IdleHunt)
	InfantryAttackGroup = { }
end

SendVehicleAttackGroup = function()
	if #VehicleAttackGroup < VehicleAttackGroupSize then
		return
	end
	Utils.Do(VehicleAttackGroup, IdleHunt)
	VehicleAttackGroup = { }
end

SendSubAttackGroup = function()
	if #SubAttackGroup < SubAttackGroupSize then
		return
	end
	Utils.Do(SubAttackGroup, IdleHunt)
	SubAttackGroup = { }
end

ProduceInfantry = function()
	if (BaseBarr1.IsDead or BaseBarr1.Owner ~= USSR) and (BaseBarr2.IsDead or BaseBarr2.Owner ~= USSR) then
		return
	elseif USSRMoney() <= 299 and IsHarvesterMissing() then
		return
	end

	USSR.Build({ Utils.Random(InfantryUnits) }, function(units)
		table.insert(InfantryAttackGroup, units[1])
		SendInfantryAttackGroup()
		Trigger.AfterDelay(ProductionInterval, ProduceInfantry)
	end)
end

SendUnits = function(units, path)
	Utils.Do(units, function(unit)
		if unit.IsDead then
			return
		end

		unit.Patrol(path, false)
		IdleHunt(unit)
	end)
end

ProduceArmor = function(factory)
	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))

	if factory.IsDead or factory.Owner ~= USSR then
		--Media.Debug("return")
		return
	elseif IsHarvesterMissing() then
		--Media.Debug("return - harv")
		ProduceHarvester(factory, delay)
		return
	end

	local toBuild = { Utils.Random(VehicleTypes) }
	local path = {}
	--[[local path = Utils.Random(AttackPaths)]]
	USSR.Build(toBuild, function(units)
		VehicleAttackGroup[#VehicleAttackGroup + 1] = units[1]

		if #VehicleAttackGroup >= VehicleAttackGroupSize then
			SendUnits(VehicleAttackGroup, path)
			VehicleAttackGroup = { }
			Trigger.AfterDelay(DateTime.Minutes(3), function()
				ProduceArmor(factory)
			end)
		else
			Trigger.AfterDelay(delay, function()
				ProduceArmor(factory)
			end)
		end
	end)

end

ProduceNavy = function()
	if BaseSpen.IsDead or BaseSpen.Owner ~= USSR then
		return
	end
	USSR.Build(SubTypes, function(units)
		table.insert(SubAttackGroup, units[1])
		SendSubAttackGroup()
		Trigger.AfterDelay(ProductionInterval, ProduceNavy)
	end)

end

ProduceAircraft = function()
	if BaseAfld.IsDead or BaseAfld.Owner ~= USSR then
		return
	end
	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Trigger.OnKilled(yak, ProduceAircraft)
		InitializeAttackAircraft(yak, Greece)
	end)
end


--------------------------------------------------------------------
----------------		OUT OF MAP BLOCK - START	----------------
--------------------------------------------------------------------

WTransWaves = function()
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
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end
--[[
TankGroupWallGuard = function()
	if not WGTank01.IsDead then
		WGTank01.AttackMove(WP72.Location)
	end
	if not WGTank02.IsDead then
		WGTank02.AttackMove(WP72.Location)
	end
	if not WGV2.IsDead then
		WGV2.AttackMove(WP72.Location)
	end
end
]]

-- Allies09a has paradrop so I left it there.
--[[]]
Paradrop = function()
	if BaseAfld.IsDead or BaseAfld.Owner ~= USSR then
		return
	end

	local airfield = BaseAfld
	local aircraft = PowerProxy.TargetParatroopers(KosyginExtractPoint.CenterPosition)

	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(1), Paradrop)
end

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

	Trigger.AfterDelay(DateTime.Minutes(ParabombDelay), SendParabombs)
end

--------------------------------------------------------------------
----------------		OUT OF MAP BLOCK - END	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------		ATTACKING BLOCK - END	--------------------
--------------------------------------------------------------------

ActivateAI = function()
	WTransUnits = WTransUnits[Difficulty]
	ProductionInterval = ProductionInterval[Difficulty]
	InfantryUnits = InfantryUnits[Difficulty]


	--PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })


	BeginBaseMaintenance()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		--BuildBase()
	end)

	--local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })	
	--Paradrop()

	--local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	--Utils.Do(buildings, function(actor)
	--	Trigger.OnDamaged(actor, function(building)
	--		if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
	--			building.StartBuildingRepairs()
	--		end
	--	end)
	--end)
	--Trigger.AfterDelay(DateTime.Minutes(2), ProduceAircraft)
	Trigger.AfterDelay(DateTime.Seconds(2), ProduceInfantry) --Reset to minutes

	--[[
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		ProduceArmor(BaseWeap) --Reset to minutes
	end)
	]]

	Trigger.AfterDelay(DateTime.Minutes(4), ProduceNavy)
	--Trigger.AfterDelay(DateTime.Minutes(5), MMGroupGuardGate)
	--Trigger.AfterDelay(DateTime.Minutes(5), TankGroupWallGuard)
	--Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
	--Trigger.AfterDelay(DateTime.Minutes(ParabombDelay), SendParabombs)
end