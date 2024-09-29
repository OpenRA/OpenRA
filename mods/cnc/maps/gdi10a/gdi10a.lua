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
ArmorProducer.Cooldown = AttackCooldown[Difficulty]
-- kind of mimics what the original mission sends at you
ArmorProducer.ModelGroups = {
	{ "arty", "arty" },
	{ "bggy", "ltnk" },
	{ "ltnk", "ltnk" },
}
ArmorProducer.AttackPaths = AttackPaths
ArmorProducer.AttackGrp = {}

-- Infantry production
InfProducer = {}
InfProducer.Cooldown = AttackCooldown[Difficulty]
-- kind of mimics what the original mission sends at you
InfProducer.ModelGroups = {
	{ "e1", "e1", "e3", "e3", "e4", "e4" },
	{ "e3", "e3", "e4", "e4" },
}
InfProducer.AttackPaths = AttackPaths
InfProducer.AttackGrp = {}

-- Build queue for structures
CyardBuildQueue = {}

WorldLoaded = function()
	print('====  WorldLoaded  ====')
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	-- Prepare Objectives
	KillNod = GDI.AddObjective("Destroy all Nod units and buildings.")
	KillGDI = Nod.AddPrimaryObjective("Kill GDI")

	AirSupport = GDI.AddObjective("Destroy the SAM sites to receive air support.", "Secondary", false)
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

	-- AI will try to replace certain buildings when lost
	BaseBlueprints = {}
	for i = 1, #RebuildableStructs do
		local structure = RebuildableStructs[i]
		local blueprint = {
			Type = structure.Type,
			Location = structure.Location,
		}
		table.insert(BaseBlueprints, blueprint)
		SetupNodBuilding(blueprint, structure, false)
	end

	--[[
	- Two Light Tanks are ordered to attack as GDI approaches the Nod base from the valley's southern exit. Trigger atk4 with team nod10.
    -- Trigger         atk4=Player Enters,Create Team,0,GoodGuy,nod10,0
	                     1        2            3      4    5      6   7
    -- TeamTypes       nod10=BadGuy,1,0,0,0,0,15,0,0,0,1,LTNK:2,7,Move:11,Move:12,Move:0,Move:7,Move:8,Move:19,Attack Base:30,0,0
	--                 ^^^^^ ^^^^^^ ^ ^ ^ ^ ^ ^^ ^ ^ ^ ^ ^^^^^^ ^ ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ ^ ^
	--                   1     2    a b c d e f  g h i 4   5    6                              7                              8 9
	-- 1: team name  2: owner
	-- Associate cells with atk4 trigger
	]]
	local cellTriggers_atk4 = CellsToPositions({
		2659, 2658, 2657, 2656, 2655, 2654, 2653, 2652, 2651, 2650, 2649, 2648,
		2647, 2646, 2645, 2595, 2594, 2593, 2592, 2591, 2590, 2589, 2588, 2587,
		2586, 2585, 2584, 2583, 2582, 2581,
	})
	Atk4 = Trigger.OnEnteredFootprint(cellTriggers_atk4, function(actor, id)
		if actor.Owner == GDI then
			-- Two light tanks:
			-- Actor200, Actor202, Actor203, Actor204
			-- Move:11, Move:12, Move:0, Move:7, Move:8, Move:19, Attack Base:30
			local moves = { waypoint11, waypoint12, waypoint0, waypoint7, waypoint8, waypoint19 }
			Utils.Do({Actor200, Actor202}, function(unit)
				if not unit.IsDead then
					for i = 1, #moves do
						unit.AttackMove(moves[i].Location, 2)
					end
					--?? what is 30?? unit.AttackMove(waypoint30.Location)
					unit.Hunt()
				end
			end)
			Trigger.RemoveFootprintTrigger(id) -- One shot
		end
	end)
end

SetupNodBuilding = function(blueprint, structure, autoRepair)
	Media.Debug("Setup " .. tostring(structure) .. " as " .. ActorString(blueprint) .. ' Bal$' .. BankBalance(Nod))
	Trigger.OnKilled(structure, function()
		-- Add to build queue
		Media.Debug(string.format('Bldg killed: Another in progress? %s  Queue_len=%d',
			tostring(RebuildingInProgress), #CyardBuildQueue))
		table.insert(CyardBuildQueue, blueprint)
		if not RebuildingInProgress then
			-- Build queue was empty; start building now
			-- This ensures we only build one structure at a time
			ProcessBuildQueue(CyardBuildQueue)
		end
	end)
	if autoRepair then
		RepairBuilding(Nod, structure, 0.75)
	end
	-- NOTE If newly built, it enters the world on the next tick
	if structure.Type == 'hand' or structure.Type == 'pyle' then
		Trigger.AfterDelay(AttackCooldown[Difficulty], function()
			print('Start infantry production+')
			ProduceUnit(structure, InfProducer)
			print('Start infantry production-')
		end)
	elseif structure.Type == 'afld' or structure.Type == 'weap' then
		Trigger.AfterDelay(AttackCooldown[Difficulty], function()
			print('Start tank production+')
			ProduceUnit(structure, ArmorProducer)
			print('Start tank production-')
		end)
	end
	-- New units' first move
	if structure.Type == 'hand' or structure.Type == 'afld' then
		structure.RallyPoint = waypoint11.Location
	end
end

-- Replace structures when destroyed
ProcessBuildQueue = function(queue)
	if RebuildingInProgress then
		local s='ProcessBuildQueue: should not happen while build in progress! Queue_len='..#queue
		Media.Debug(s)
		print(s)
	end
	while #queue > 0 do
		local rc = RebuildFromBlueprint(queue)
		Media.Debug('RebuildFromBlueprint: ' .. rc)
		if rc == 'cancelled' then
			table.remove(queue, 1)
		elseif rc == 'insufficient_funds' then
			-- Continue processing later
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				ProcessBuildQueue(queue)
			end)
			break
		elseif rc == 'in_progress' then
			-- Stop processing the queue while it builds
			table.remove(queue, 1)
			break
		else
			Media.Debug("Unknown return from RebuildFromBlueprint: " .. tostring(rc))
		end
	end
end

-- Helper for ProcessBuildQueue
-- Return true when the item is finished and we should move to the next item in queue
-- Return false when the item is in progress and we should not move to the next item the queue
RebuildFromBlueprint = function(queue)
	if #queue == 0 then
		local s = 'RebuildFromBlueprint: queue empty!'
		Media.Debug(s)
		print(s)
		return 'cancelled'
	end
	local blueprint = queue[1]
	local cyard = GetNodCyard()
	if not cyard then
		Media.Debug("Can't rebuild " .. ActorString(blueprint) .. ": No Cyard")
		return 'cancelled' -- Lost cyard; can never build again
	end

	local cost = Actor.Cost(blueprint.Type)
	if BankBalance(Nod) < cost then
		-- Can't build now
		Media.Debug("Can't rebuild " .. ActorString(blueprint) .. " yet. Cash$" .. BankBalance(Nod) .. ", Cost$" .. cost)
		return 'insufficient_funds'
	end

	-- Start building now
	RebuildingInProgress = true
	BankDeduct(Nod, cost)
	Trigger.AfterDelay(Actor.BuildTime(blueprint.Type), function()
		-- Construction complete
		RebuildingInProgress = false
		--[[TODO Check for obstacles
		if IsBuildAreaBlocked(Nod, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				BuildBlueprint(blueprint)  -- this function???
			end)
			return
		end
		]]--
		if cyard.IsDead or cyard.Owner ~= Nod then
			-- Lost our cyard; can't build anymore
			Media.Debug("Lost cyard; refunding $" .. cost .. " for " .. ActorString(blueprint))
			-- Credit
			Nod.Cash = Nod.Cash + cost
		else
			local structure = Actor.Create(blueprint.Type, true, { Owner = Nod, Location = blueprint.Location })
			SetupNodBuilding(blueprint, structure, true)
		end
		-- Continue processing the queue.
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			ProcessBuildQueue(queue)
		end)
	end)
	Media.Debug(string.format("Rebuilding %s: %d sec, $%d", ActorString(blueprint),
		Actor.BuildTime(blueprint.Type), cost))
	return 'in_progress'
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

-- Build infantry or vehicles/armour
ProduceUnit = function(factory, prodParms)

	local needHarvester = #Nod.GetActorsByType("harv") == 0 or BuildingHarvester
	if factory.IsDead or factory.Owner ~= Nod then
		-- Lost this structure, stop
		return
	elseif (factory.Type == 'hand' or factory.Type == 'pyle') and
		BankBalance(Nod) <= Actor.Cost('harv')-1 and needHarvester then
		-- Infantry on hold while we replace harvester
		-- Assumes we own an afld or weap
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			ProduceUnit(factory, prodParms)
		end)
		return
	elseif (factory.Type == 'afld' or factory.Type == 'weap') and
		needHarvester and not BuildingHarvester then
		-- Build a harvester first
		-- Assumes we own at most one vehicle producing structure
		--if not BuildingHarvester then
		ProduceHarvester(factory, prodParms)
		return
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
			local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
			Trigger.AfterDelay(delay, function()
				ProduceUnit(factory, prodParms)
			end)
		end)
	else
		-- Group ready
		local path = Utils.Random(prodParms.AttackPaths)
		-- Tried to use campain.lua: MoveAndHunt but it wants path elements to have a Location property
		SendUnits(prodParms.AttackGrp, path)
		prodParms.AttackGrp = {}
		prodParms.ModelGroup = nil
		Trigger.AfterDelay(prodParms.Cooldown, function()
			ProduceUnit(factory, prodParms)
		end)
		s = s .. '; Attack!'
	end

	Media.Debug(s)
	print(s)
end

ProduceHarvester = function(factory, prodParms)
	local harv_type = "harv"
	if BankBalance(Nod) < Actor.Cost(harv_type) then
		-- Try again later
		Media.Debug(string.format('ProduceHarvester: Have $%d, need $%d', BankBalance(Nod), Actor.Cost(harv_type)))
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			ProduceUnit(factory, prodParms)
		end)
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
				ProduceUnit(factory, prodParms)
			end)
		end)
	end
end

GetNodCyard = function()
	local cyards = Nod.GetActorsByType("fact")
	if (#cyards < 1) then
		return nil -- Lost cyard; can never build again
	else
		return cyards[1]
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

BankBalance = function(Player)
	return Player.Resources + Player.Cash
end

BankDeduct = function(Player, cost)
	if cost > BankBalance(Player) then
		Media.Debug(tostring(Player) .. ' cannot afford $' .. cost)
	end
	local spendRes = math.min(cost, Player.Resources)
	Player.Resources = Player.Resources - spendRes
	local spendCash = math.max(0, cost - spendRes)
	Player.Cash = Player.Cash - spendCash
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

ActorString = function(actor)
	return string.format('%s (%d,%d)', actor.Type, actor.Location.X, actor.Location.Y)
end
