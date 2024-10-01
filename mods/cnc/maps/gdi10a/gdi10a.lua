--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Destroy all to unlock airstrike
SamSites = { sam1, sam2, sam3, sam4 }

-- Buildings to rebuild if destroyed
RebuildableStructs = { gun1, gun2, gun3, gun4, obelisk1, power1, power2, power3, power4,
	power5, power6, power7, power8, airfield, hand, refinery }

-- Attack waves
AttackPaths =
{
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location, waypoint6.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint2.Location, waypoint13.Location, waypoint14.Location, waypoint15.Location, waypoint17.Location },
	{ waypoint11.Location, waypoint18.Location, waypoint12.Location, waypoint0.Location, waypoint7.Location, waypoint8.Location, waypoint9.Location, waypoint10.Location, waypoint17.Location }
}
-- Time between attack waves
AttackCooldown =
{
	hard = DateTime.Seconds(10), normal = DateTime.Seconds(20), easy = DateTime.Seconds(30)
}
-- Armor/vehicle production
ArmorProducer = {}
ArmorProducer.Player = Player.GetPlayer("Nod")
ArmorProducer.Cooldown = AttackCooldown[Difficulty]
-- kind of mimics what the original mission sends at you
ArmorProducer.ModelGroups = {
	{ "arty", "arty" },
	{ "bggy", "ltnk" },
	{ "ltnk", "ltnk" },
}
-- Two ARTY no auto-create
local art1Waypoints = { -- Attack Base 30
	waypoint0, waypoint2, waypoint3, waypoint4, waypoint5, waypoint6
}
-- Two BGGY auto-create
local auto1Waypoints = { -- Attack Units 30
	waypoint11, waypoint12, waypoint0, waypoint7, waypoint8, waypoint9,
	waypoint10
}
-- Two E1, one LTNK
local auto2Waypoints = { -- Patrol?
	waypoint11, waypoint0, waypoint3, waypoint0, waypoint7
}
-- Four E3
local moveWaypoints = { -- Patrol?
	waypoint18, waypoint19
}
ArmorProducer.AttackPaths = AttackPaths
ArmorProducer.AttackGrp = {}
ArmorProducer.StructureTypes = {"afld", "weap"}

-- Infantry production
InfProducer = {}
InfProducer.Player = Player.GetPlayer("Nod")
InfProducer.Cooldown = AttackCooldown[Difficulty]
-- kind of mimics what the original mission sends at you
InfProducer.ModelGroups = {
	{ "e1", "e1", "e3", "e3", "e4", "e4" },
	{ "e3", "e3", "e4", "e4" },
}
InfProducer.AttackPaths = AttackPaths
InfProducer.AttackGrp = {}
InfProducer.StructureTypes = {"hand", "pyle"}

-- Build queue for structures
CyardBuildQueue = {}

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	-- Prepare Objectives
	KillNod = AddPrimaryObjective(GDI, "destroy-nod-units-buildings")
	KillGDI = AddPrimaryObjective(Nod, "")

	AirSupport = AddSecondaryObjective(GDI, "destroy-sams")
	-- Trigger air1
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(AirSupport)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	InitObjectives(GDI)

	-- Look at player's MCV
	Camera.Position = Actor201.CenterPosition

	-- Repair non-rebuilding structures e.g silos
	-- Set up a few seconds after the game starts to give it a moment
	Utils.Do(Nod.GetActors(), function(actor)
		if actor.HasProperty("StartBuildingRepairs") then
			RepairBuilding(Nod, actor, 0.75)
		end
	end)

	Base.Init(Nod, RebuildableStructs, waypoint11.Location)

	--[[
	Two light tanks attack as GDI approaches the Nod base from the valley's
	southern exit. Trigger atk4 with team nod10. Cell numbers from scg10ea.ini.
	]]
	local cellTriggers_atk4 = CellsToPositions({
		2659, 2658, 2657, 2656, 2655, 2654, 2653, 2652, 2651, 2650, 2649, 2648,
		2647, 2646, 2645, 2595, 2594, 2593, 2592, 2591, 2590, 2589, 2588, 2587,
		2586, 2585, 2584, 2583, 2582, 2581,
	})
	local atk4 = Trigger.OnEnteredFootprint(cellTriggers_atk4, function(actor, id)
		if actor.Owner == GDI then
			-- Create team nod10: two light tanks
			local candidates = Nod.GetActorsByType('ltnk')
			local team = {}
			local s = 'DBG Atk4 Team=('
			for i = 1, #candidates do
				if not candidates[i].IsDead then
					s = s .. ' ' .. ActorString(candidates[i])
					table.insert(team, candidates[i])
					if #team == 2 then
						break
					end
				end
			end
			s = s .. ') attacking'
			Media.Debug(s)
			-- Move:11, Move:12, Move:0, Move:7, Move:8, Move:19
			local moves = { waypoint11, waypoint12, waypoint0, waypoint7, waypoint8, waypoint19 }
			Utils.Do(team, function(unit)
				if not unit.IsDead then
					for i = 1, #moves do
						unit.AttackMove(moves[i].Location, 2)
					end
					-- Attack Base:30
					unit.Hunt()
				end
			end)
			Trigger.RemoveFootprintTrigger(id) -- One shot
		end
	end)
end

SendUnits = function(units, path)
	Utils.Do(units, function(unit)
		if unit.IsDead then
			return
		end

		unit.Patrol(path, false)
		IdleHunt(unit)
		-- IdleHunt is defined in utils.lua
	end)
end

ProduceUnit = function(prodParms)

	local structures = prodParms.Player.GetActorsByTypes(prodParms.StructureTypes)
	for structure in structures do
		if not structure.IsDead then
			-- Try to build something
			local timerInterval = ProduceUnitHelper(prodParms)
			if timerInterval then
				-- Rearm our timer
				Trigger.AfterDelay(timerInterval, function()
					ProduceUnit(prodParms)
				end)
			end
			return
		end
	end
	-- No structures to build infantry; try again later
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		ProduceInfantry(prodParms)
	end)
end

-- Build infantry or vehicles/armour
ProduceUnitHelper = function(factory, prodParms)

	local needHarvester = #Nod.GetActorsByType("harv") == 0 or BuildingHarvester
	if (factory.Type == 'hand' or factory.Type == 'pyle') and
		BankBalance(Nod) <= Actor.Cost('harv')-1 and needHarvester then
		-- Infantry on hold while we replace harvester
		-- Assumes we own an afld or weap
		return DateTime.Seconds(10)
	elseif (factory.Type == 'afld' or factory.Type == 'weap') and
		needHarvester and not BuildingHarvester then
		-- Build a harvester first
		-- Assumes we own at most one vehicle producing structure
		--if not BuildingHarvester then
		return ProduceHarvester(factory, prodParms)
	end

	-- Build a group to attack together
	if #prodParms.AttackGrp == 0 then
		-- Choose a group to build
		prodParms.ModelGroup = Utils.Random(prodParms.ModelGroups)
	else
		RemoveLostActors(prodParms.AttackGrp)
	end
	-- Which units do we not have yet?  Randomly build one of them.
	local haveTypes = {}
	local s = string.format('ProduceUnit(%s,_): Have', factory.Type)
	for i, actor in ipairs(prodParms.AttackGrp) do
		table.insert(haveTypes, actor.Type)
		s = s .. ' ' .. actor.Type
	end
	local needUnits = CompareTables(haveTypes, prodParms.ModelGroup)
	s = s .. ', Need'
	for i, actor in ipairs(needUnits) do
		s = s .. ' ' .. actor
	end

	local timerInterval
	if #needUnits > 0 then
		-- Continue building the group
		local toBuild = { Utils.Random(needUnits) }
		print(string.format('; Build %s', toBuild[1]))
		-- TODO What if Build returns false? Do we try later?
		factory.Build(toBuild, function(unit)
			-- Unit ready
			for _,v in ipairs(unit) do
				table.insert(prodParms.AttackGrp, v)
			end
			-- Continue building
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				ProduceUnit(prodParms)
			end)
		end)
		-- Stop our timer until Build returns
		timerInterval = nil
	else
		-- Group ready
		local path = Utils.Random(prodParms.AttackPaths)
		-- Tried to use campain.lua: MoveAndHunt but it wants path elements to have a Location property
		SendUnits(prodParms.AttackGrp, path)
		prodParms.AttackGrp = {}
		prodParms.ModelGroup = nil
		timerInterval = prodParms.Cooldown
		s = s .. '; Attack!'
	end

	Media.Debug(s)
	print(s)
	return timerInterval
end

ProduceHarvester = function(factory, prodParms)
	local harv_type = "harv"
	if BankBalance(Nod) < Actor.Cost(harv_type) then
		-- Try again later
		Media.Debug(string.format('ProduceHarvester: Have $%d, need $%d', BankBalance(Nod), Actor.Cost(harv_type)))
		return DateTime.Seconds(5)
	else
		BuildingHarvester = true
		factory.Build({ harv_type }, function(units)
			-- Unit ready
			for idx, unit in pairs(units) do
				if unit.Type == 'harv' then
					local s='Harvester built: ' .. ActorString(unit)
					Media.Debug(s)
					print(s)
					--Unnecessary:
					--unit.FindResources()
					BuildingHarvester = false
				end
			end
			-- Continue building armour/vehicles
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				ProduceUnit(prodParms)
			end)
		end)
		-- Stop our timer until Build returns
		return nil
	end
end

-- Remove captured and killed actors from a table
RemoveLostActors = function (tbl)
    for i = #tbl, 1, -1 do
        if tbl[i].IsDead or tbl[i].Owner ~= Nod then
            table.remove(tbl, i)
        end
    end
end


Tick = function()
	-- Check objectives
	if DateTime.GameTime > DateTime.Seconds(5) then
		if GDI.HasNoRequiredUnits()  then
			Nod.MarkCompletedObjective(KillGDI)
		end
		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(KillNod)
		end
	end
end

--[[
  We use this to build an attack force
  t1 is our current group.  t2 is our model Group.  Find the difference between
  the groups.  When the difference is empty, then we are ready to attack.
--]]
function CompareTables(t1, t2)
    local t3 = {}
    local t1_counts = {}

    -- Count occurrences of each value in t1
    for _, v in pairs(t1) do
        t1_counts[v] = (t1_counts[v] or 0) + 1
    end

    -- Check each value in t2
    for _, v in pairs(t2) do
        if t1_counts[v] and t1_counts[v] > 0 then
            t1_counts[v] = t1_counts[v] - 1
        else
            table.insert(t3, v)
        end
    end

    return t3
end
