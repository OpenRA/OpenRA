--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
InfAttack = { }
ArmorAttack = { }
AttackPaths = { { AttackWaypoint1.Location }, { AttackWaypoint2.Location } }

AlliedInfantryTypes = { "e1", "e1", "e3" }
AlliedArmorTypes = { "jeep", "jeep", "1tnk", "1tnk", "2tnk", "2tnk", "arty" }

ProduceInfantry = function(barracks)
	if barracks.IsDead or barracks.Owner ~= Greece then
		return
	elseif GreeceMoney() <= 299 and IsHarvesterMissing() then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(AlliedInfantryTypes) }
	local path = Utils.Random(AttackPaths)
	Greece.Build(toBuild, function(units)
		InfAttack[#InfAttack + 1] = units[1]

		if #InfAttack >= 10 then
			SendUnits(InfAttack, path)
			InfAttack = { }
			Trigger.AfterDelay(DateTime.Minutes(2), function()
				ProduceInfantry(barracks)
			end)
		else
			Trigger.AfterDelay(delay, function()
				ProduceInfantry(barracks)
			end)
		end
	end)
end

ProduceArmor = function(factory)
	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))

	if factory.IsDead or factory.Owner ~= Greece then
		return
	elseif IsHarvesterMissing() then
		ProduceHarvester(factory, delay)
		return
	end

	local toBuild = { Utils.Random(AlliedArmorTypes) }
	local path = Utils.Random(AttackPaths)
	Greece.Build(toBuild, function(units)
		ArmorAttack[#ArmorAttack + 1] = units[1]

		if #ArmorAttack >= 6 then
			SendUnits(ArmorAttack, path)
			ArmorAttack = { }
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

ProduceHarvester = function(factory, delay)
	if GreeceMoney() < Actor.Cost("harv") then
		return
	end

	local toBuild = { "harv" }
	Greece.Build(toBuild, function()
		Trigger.AfterDelay(delay, function()
			ProduceArmor(factory)
		end)
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

IsHarvesterMissing = function()
	return #Greece.GetActorsByType("harv") == 0
end

GreeceMoney = function()
	return Greece.Cash + Greece.Resources
end

BaseBlueprints =
{
	{ type = "apwr", actor = Apwr, cost = 500, shape = { 3, 3 }, location = CPos.New(76, 41) },
	{ type = "tent", actor = Tent, cost = 400, shape = { 2, 3 }, location = CPos.New(67, 45), onBuilt = ProduceInfantry },
	{ type = "proc", actor = Proc, cost = 1400, shape = { 3, 4 }, location = CPos.New(71, 41) },
	{ type = "weap", actor = Weap, cost = 2000, shape = { 3, 3 }, location = CPos.New(67, 41), onBuilt = ProduceArmor },
	{ type = "apwr", actor = Apwr2, cost = 500, shape = { 3, 3 }, location = CPos.New(77, 45) }
}

--[[
	Similar to the original CnC/RA [BASE] and [STRUCTURES] .INI sections.
	Check a list every so often and (re)build structures missing from
	that list, in order, if circumstances allow for it.
]]
BuildBase = function()
	for _, blueprint in pairs(BaseBlueprints) do
		if not blueprint.actor then
			BuildBlueprint(blueprint)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBlueprint = function(blueprint)
	Trigger.AfterDelay(Actor.BuildTime(blueprint.type), function()
		if CYard.IsDead or CYard.Owner ~= Greece then
			return
		elseif GreeceMoney() <= 299 and IsHarvesterMissing() then
			return
		end

		if IsBuildAreaBlocked(Greece, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				BuildBlueprint(blueprint)
			end)
			return
		end

		local actor = Actor.Create(blueprint.type, true, { Owner = Greece, Location = blueprint.location })
		OnBlueprintBuilt(actor, blueprint)

		Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
	end)
end

OnBlueprintBuilt = function(actor, blueprint)
	Greece.Cash = Greece.Cash - blueprint.cost
	blueprint.actor = actor
	MaintainBuilding(actor, blueprint, 0.75)

	if blueprint.onBuilt then
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

	Utils.Do(Greece.GetActors(), function(actor)
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
