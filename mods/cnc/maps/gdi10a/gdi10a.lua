--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = hard
SamSites = { sam1, sam2, sam3, sam4 }

-- this is a list of all the buildings that will get rebuilt when they get destroyed
NodBase = { gun1, gun2, gun3, gun4, obelisk1, power1, power2, power3, power4, power5, power6, power7, power8, airfield, hand, refinery }

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	DestroyAll = GDI.AddObjective("Destroy all Nod units and buildings.")

	Trigger.AfterDelay(DateTime.Seconds(5), BuildAttackGroup)

	AirSupport = GDI.AddObjective("Destroy the SAM sites to receive air support.", "Secondary", false)
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(AirSupport)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	airfield.RallyPoint = waypoint11.Location
	hand.RallyPoint = waypoint11.Location

	-- set repair triggers a few seconds after the game starts to give it a moment
	Trigger.AfterDelay(DateTime.Seconds(5), SetRepairRebuildTriggers)

	Trigger.OnAnyKilled(NodBase, BuildBuilding)
end

SetRepairRebuildTriggers = function() 
	-- repair buildings as they are damaged
	for _, actor in ipairs(Map.ActorsInWorld) do
		-- nod buildings get auto-repair turned on
		if (actor.Owner == Nod and actor.HasProperty("StartBuildingRepairs")) then
			SetBuildingRepairTrigger(actor)
		end

		-- nod harvesters get auto-rebuild turned on
		if (actor.HasProperty("FindResources")) then
			SetHarvesterRebuildTrigger(actor)
		end
	end
end

--TODO, maybe: Nod "in-base" infantry patrol between waypoints 18 and 19; rebuild if any(some?)(all?) are killed; join patrol
--215-18, 223-26, 219-22 are patrol units
--TODO, maybe: Nod "in-base" tank(s?) respond to attacks on harvester; rebuild if killed; move to 19

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(DestroyAll)
	end

	if DateTime.GameTime > DateTime.Seconds(5) then
		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(DestroyAll)
		end
	end
end

-- base reconstruction code, some from gdi09-AI.lua
BuildBuilding = function(building)
	local cyards = Nod.GetActorsByType("fact")
	if (#cyards < 1) then
		return
	end

	local cyard = cyards[1]
	local buildingCost = Actor.Cost(building.Type)
	if CyardIsBuilding or Nod.Cash < buildingCost then
		Trigger.AfterDelay(DateTime.Seconds(10), function() BuildBuilding(building) end)
		return
	end

	CyardIsBuilding = true
	-- grab a pointer to the location here, it won't be valid when the delayed function fires later
	BLoc = building.Location

	Nod.Cash = Nod.Cash - buildingCost
	Trigger.AfterDelay(Actor.BuildTime(building.Type), function() 
		CyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= Nod then
			Nod.Cash = Nod.Cash + buildingCost
			return
		end

		local actor = Actor.Create(building.Type, true, { Owner = Nod, Location = BLoc })

		Trigger.OnKilled(actor, function()
			BuildBuilding(actor)
		end)

		SetBuildingRepairTrigger(actor)
	end)
end

--repair building trigger setup
function SetBuildingRepairTrigger(actor)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == Nod and building.Health < building.MaxHealth * 3/4 then
			building.StartBuildingRepairs()
		end
	end)
end

function SetHarvesterRebuildTrigger(actor)
	Trigger.OnKilled(actor, BuildHarvester)
end

-- attack wave code
AttackPaths =
{
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location, waypoint6.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint2.Location, waypoint13.Location, waypoint14.Location, waypoint15.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint7.Location, waypoint8.Location, waypoint9.Location, waypoint10.Location, waypoint17.Location }
}

-- kind of mimics what the original mission sends at you
AttackUnitTypes = 
{
	{ factory = "hand", types = { "e1", "e1", "e3", "e3", "e4", "e4" } },
	{ factory = "hand", types = { "e3", "e3", "e4", "e4" } },
	{ factory = "afld", types = { "arty", "arty" } },
	{ factory = "afld", types = { "bggy", "ltnk" } },
	{ factory = "afld", types = { "ltnk", "ltnk" } }
}

BuildHarvester = function() 
	local factoryArray = Nod.GetActorsByType("afld")
	if (#factoryArray > 0) then
		factoryArray[1].Build({ "harv" }, HarvesterHarvest)
	end
end

HarvesterHarvest = function(units) 
	for _, unit in ipairs(units) do
		unit.FindResources(path, false)
		SetHarvesterRebuildTrigger(unit)
	end
end

BuildAttackGroup = function()
	-- for LUA rookies, arrays are 1-indexed
	-- .Build takes an array of strings indicating unit types

	local unitSet = Utils.Random(AttackUnitTypes)

	local factoryArray = Nod.GetActorsByType(unitSet.factory)
	if (#factoryArray > 0) then
		factoryArray[1].Build(unitSet.types, SendAttackWave)
	end
end

-- callback function for when a unit group finishes building: takes an array of units and 
-- sends them on their merry way to the GDI base
SendAttackWave = function(units) 
	local path = Utils.Random(AttackPaths)

	for _, unit in ipairs(units) do
		unit.Patrol(path, false)
	end

	Trigger.AfterDelay(DateTime.Seconds(5), BuildAttackGroup)
end