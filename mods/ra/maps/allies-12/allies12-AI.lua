--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--------------------------------------------------------------------
-----------------	    DATA BLOCK - START	------------------------
--------------------------------------------------------------------

-- CheckSecure
BadGuyBaseAreaTL = WPos.New( (CPos.New(91, 33)).X * 1024,  (CPos.New(91, 33)).Y * 1024, 0)
BadGuyBaseAreaBR = WPos.New( (CPos.New(116, 52)).X * 1024, (CPos.New(116, 52)).Y * 1024, 0)

USSRBaseAreaTL = WPos.New( (CPos.New(45, 18)).X * 1024,  (CPos.New(45, 18)).Y * 1024, 0)
USSRBaseAreaBR = WPos.New( (CPos.New(73, 37)).X * 1024, (CPos.New(73, 37)).Y * 1024, 0)

USSRAttackPaths =
{
    {USSRLeftAtkPath1.Location, USSRLeftAtkPath2.Location, USSRLeftAtkPath3.Location, USSRLeftAtkPath4.Location},
    {USSRLeftAtkPath1.Location, USSRLeftAtkPath2.Location, USSRLeftAtkPath3.Location, USSRMidAtkPath2.Location},
    {USSRMidAtkPath1.Location, USSRMidAtkPath2.Location, USSRMidAtkPath3.Location}
}

BadGuyAttackPaths =
{
    --Repeated attack path for equal distribution
    {BadGuyLeftAtkPath1.Location, BadGuyLeftAtkPath2.Location},
    {BadGuyLeftAtkPath1.Location, BadGuyLeftAtkPath2.Location},
    {BadGuyMidAtkPath1.Location, BadGuyMidAtkPath2.Location, BadGuyMidAtkPath3A.Location},
    {BadGuyMidAtkPath1.Location, BadGuyMidAtkPath2.Location, BadGuyMidAtkPath3B.Location}
}

--Attacks
InfantryTypes = {"e1", "e2", "e4"}
InfantryBadGuyAttackGroup = { }
InfantryUSSRAttackGroup = { }

VehicleTypes = { "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "4tnk" }
VehicleAttackGroup = { }
--VehicleAttackGroupSize is defined in difficulty setup

ProductionInterval = DateTime.Seconds(5)--{}

SovietAircraftType = { "yak", "mig" }
PlanesAttackGroup = { }

LstEnemyTypes = { "3tnk", "3tnk", "v2rl" }
LstEnemyAmount = 3

USSRBaseBlueprints = {
    { type = "powr", actor = USSRPower1, cost = 300, shape = { 2, 3 }, location = CPos.New(67, 21) },
    { type = "apwr", actor = USSRPower2, cost = 500, shape = { 3, 3 }, location = CPos.New(60, 24) },
    { type = "apwr", actor = USSRPower3, cost = 500, shape = { 3, 3 }, location = CPos.New(63, 24) },
    { type = "apwr", actor = USSRPower4, cost = 500, shape = { 3, 3 }, location = CPos.New(66, 24) },

    { type = "powr", actor = USSRPower5, cost = 300, shape = { 2, 3 }, location = CPos.New(61, 28) },
    { type = "apwr", actor = USSRPower6, cost = 500, shape = { 3, 3 }, location = CPos.New(63, 28) },
    { type = "apwr", actor = USSRPower7, cost = 500, shape = { 3, 3 }, location = CPos.New(66, 28) },

    { type = "apwr", actor = USSRPower8, cost = 500, shape = { 3, 3 }, location = CPos.New(60, 32) },
    { type = "apwr", actor = USSRPower9, cost = 500, shape = { 3, 3 }, location = CPos.New(63, 32) },
    { type = "apwr", actor = USSRPower10, cost = 500, shape = { 3, 3 }, location = CPos.New(66, 31) },

    { type = "silo", actor = USSRSilo1, cost = 150, shape = { 1, 1 }, location = CPos.New(55, 18) },
    { type = "silo", actor = USSRSilo2, cost = 150, shape = { 1, 1 }, location = CPos.New(54, 20) },
    { type = "silo", actor = USSRSilo3, cost = 150, shape = { 1, 1 }, location = CPos.New(52, 20) },

    { type = "barr", actor = USSRBarr, cost = 500, shape = { 2, 3 }, location = CPos.New(51, 33) --[[, onBuilt = ProduceInfantry(USSRBarr, USSR)]] },
    { type = "weap", actor = USSRWeap, cost = 2000, shape = { 3, 3 }, location = CPos.New(51, 28) --[[, onBuilt = ProduceArmor(USSRWeap, USSR)]] },
    { type = "kenn", actor = USSRKenn, cost = 200, shape = { 1, 1 }, location = CPos.New(51, 24) },

    { type = "afld", actor = USSRAfld1, cost = 500, shape = { 3, 2 }, location = CPos.New(61, 18) },
    { type = "afld", actor = USSRAfld2, cost = 500, shape = { 3, 2 }, location = CPos.New(65, 18) },
    { type = "hpad", actor = USSRHpad1, cost = 500, shape = { 2, 3 }, location = CPos.New(61, 20) },
    { type = "hpad", actor = USSRHpad2, cost = 500, shape = { 2, 3 }, location = CPos.New(64, 20) },

    { type = "dome", actor = USSRDome, cost = 1400, shape = { 2, 3 }, location = CPos.New(49, 21) },
    --[[{ type = "stek", actor = USSRStek, cost = 1500, shape = { 3, 3 }, location = CPos.New(53, 21) },]]

    { type = "ftur", actor = USSRFtur, cost = 600, shape = { 1, 1 }, location = CPos.New(45, 27) },
    { type = "tsla", actor = USSRTesla1, cost = 1200, shape = { 1, 1 }, location = CPos.New(46, 33 ) },
    { type = "tsla", actor = USSRTesla2, cost = 1200, shape = { 1, 1 }, location = CPos.New(56, 37) },
    { type = "tsla", actor = USSRTesla3, cost = 1200, shape = { 1, 1 }, location = CPos.New(55, 29) },
    { type = "tsla", actor = USSRTesla4, cost = 1200, shape = { 1, 1 }, location = CPos.New(69, 29) },
    { type = "sam", actor = USSRSam1, cost = 700, shape = { 2, 1 }, location = CPos.New(46, 23) },
    { type = "sam", actor = USSRSam2, cost = 700, shape = { 2, 1 }, location = CPos.New(58, 36) },
    { type = "sam", actor = USSRSam3, cost = 700, shape = { 2, 1 }, location = CPos.New(56, 23) },
    { type = "sam", actor = USSRSam4, cost = 700, shape = { 2, 1 }, location = CPos.New(69, 27) },
    { type = "sam", actor = USSRSam5, cost = 700, shape = { 2, 1 }, location = CPos.New(47, 36) }
}

USSRToBeBuilt = {
    { type = "proc", actor = USSRProc1, cost = 1400, shape = { 3, 4 }, location = CPos.New(55, 30) },
    { type = "proc", actor = USSRProc2, cost = 1400, shape = { 3, 4 }, location = CPos.New(48, 24) }
}

BadGuyBaseBlueprints = {
	{ type = "powr", actor = BadGuyPower1, cost = 300, shape = { 2, 3 }, location = CPos.New(113, 39) },
	{ type = "apwr", actor = BadGuyPower2, cost = 500, shape = { 3, 3 }, location = CPos.New(110, 39) },
	{ type = "apwr", actor = BadGuyPower3, cost = 500, shape = { 3, 3 }, location = CPos.New(94, 39) },
	{ type = "apwr", actor = BadGuyPower4, cost = 500, shape = { 3, 3 }, location = CPos.New(95, 42) },
	{ type = "apwr", actor = BadGuyPower5, cost = 500, shape = { 3, 3 }, location = CPos.New(97, 45) },
    { type = "apwr", actor = BadGuyPower6, cost = 500, shape = { 3, 3 }, location = CPos.New(112, 47) },

    { type = "proc", actor = BadGuyProc, cost = 1400, shape = { 3, 4 }, location = CPos.New(98, 37) },
    { type = "silo", actor = BadGuySilo1, cost = 1400, shape = { 1, 1 }, location = CPos.New(102, 39) },
    { type = "silo", actor = BadGuySilo2, cost = 1400, shape = { 1, 1 }, location = CPos.New(104, 39) },

    { type = "barr", actor = BadGuyBarr, cost = 500, shape = { 2, 3 }, location = CPos.New(107, 45), --[[onBuilt = ProduceInfantry(BadGuyBarr, BadGuy)]] },
    { type = "kenn", actor = BadGuyKenn, cost = 200, shape = { 1, 1 }, location = CPos.New(106, 46) },
    { type = "spen", actor = BadGuySpen, cost = 800, shape = { 3, 3 }, location = CPos.New(89, 43) },

    { type = "hpad", actor = BadGuyHpad1, cost = 500, shape = { 2, 3 }, location = CPos.New(103, 43) },
    { type = "hpad", actor = BadGuyHpad2, cost = 500, shape = { 2, 3 }, location = CPos.New(110, 43) },

    { type = "fix", actor = BadGuyFix, cost = 500, shape = { 3, 3 }, location = CPos.New(99, 42) },

    { type = "dome", actor = BadGuyDome, cost = 1500, shape = { 2, 3 }, location = CPos.New(113, 43) },
    --[[{ type = "stek", actor = BadGuyStek, cost = 1500, shape = { 3, 3 }, location = CPos.New(107, 41) },]]

    { type = "ftur", actor = BadGuyFtur, cost = 600, shape = { 1, 1 }, location = CPos.New(110, 51) },
    { type = "tsla", actor = BadGuyTesla1, cost = 1200, shape = { 1, 1 }, location = CPos.New(107, 41) },
    { type = "tsla", actor = BadGuyTesla2, cost = 1200, shape = { 1, 1 }, location = CPos.New(104, 50) },
    { type = "tsla", actor = BadGuyTesla3, cost = 1200, shape = { 1, 1 }, location = CPos.New(89, 51) },

    { type = "sam", actor = BadGuySam1, cost = 700, shape = { 2, 1 }, location = CPos.New(100, 41) },
    { type = "sam", actor = BadGuySam2, cost = 700, shape = { 2, 1 }, location = CPos.New(95, 46) },
    { type = "sam", actor = BadGuySam3, cost = 700, shape = { 2, 1 }, location = CPos.New(108, 41) }
}

-- USSR
USSRGuardDog1Data = { actor = USSRRebuildableDog1, exists = true, pos = CPos.New(47, 24) }
USSRGuardDog2Data = { actor = USSRRebuildableDog2, exists = true, pos = CPos.New(52, 25) }
USSRGuardDog3Data = { actor = USSRRebuildableDog3, exists = true, pos = CPos.New(48, 35) }
USSRGuardDog4Data = { actor = USSRRebuildableDog4, exists = true, pos = CPos.New(52, 39) }
-- BadGuy
BadGuyGuardDog1Data = { actor = BadGuyRebuildableDog1, exists = true, pos = CPos.New(96, 45) }
BadGuyGuardDog2Data = { actor = BadGuyRebuildableDog2, exists = true, pos = CPos.New(106, 47) }
BadGuyGuardDog3Data = { actor = BadGuyRebuildableDog3, exists = true, pos = CPos.New(112, 43) }

USSRRebuildableDogs = { USSRGuardDog1Data, USSRGuardDog2Data, USSRGuardDog3Data, USSRGuardDog4Data}
BadGuyRebuildableDogs = { BadGuyGuardDog1Data, BadGuyGuardDog2Data, BadGuyGuardDog3Data}

USSRGuardHelicopters =
{
    {actor = USSRGuardingHind1, guardPoint = USSRHpad1 },
    {actor = USSRGuardingHind2, guardPoint = USSRHpad2 }
}

BadGuyGuardHelicopters =
{
    {actor = BadGuyGuardingHind1, guardPoint = BadGuyHpad1 },
    {actor = BadGuyGuardingHind2, guardPoint = BadGuyHpad2 }
}

GuardRadius = WDist.FromCells(12)

IronTankPath = {USSRMidAtkPath1.Location, USSRMidAtkPath2.Location, USSRMidAtkPath3.Location, USSRMidAtkPath4.Location}
ironSwitch = 0

NukeConditionInt = -1

--------------------------------------------------------------------
-----------------	    DATA BLOCK - END	------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	UTILS BLOCK - START	----------------------------
--------------------------------------------------------------------

PlayerMoney = function(owner)
	return owner.Cash + owner.Resources
end

GrantCash = function(player, amount)
    player.Cash = player.Cash + amount
end

--------------------------------------------------------------------
-----------------	UTILS BLOCK - END	----------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	BASE MANAGEMENT BLOCK - START	----------------
--------------------------------------------------------------------

IsHarvesterMissing = function(owner)
	return #owner.GetActorsByType("harv") == 0
end

BuildBase = function(collection, cyard, owner)
    for _, blueprint in ipairs(collection) do
        if not blueprint.actor then
            BuildBlueprint(blueprint, cyard, owner, collection)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function() --CHANGE: Change to 1 second
        BuildBase(collection, cyard, owner)
    end)
end

BuildBlueprint = function(blueprint, cyard, owner, collection)
    Trigger.AfterDelay(Actor.BuildTime(blueprint.type), function()   --CHANGE: Change to actual actor buildTime
		if cyard.IsDead or cyard.Owner ~= owner then
			return
		elseif PlayerMoney(owner) <= 299 and IsHarvesterMissing(owner) then
            return
		end
		if IsBuildAreaBlocked(owner, blueprint) then
			Trigger.AfterDelay(DateTime.Seconds(5), function()

				BuildBlueprint(blueprint, cyard, owner, collection)
			end)
			return
		end
		local actor = Actor.Create(blueprint.type, true, { Owner = owner, Location = blueprint.location })
		OnBlueprintBuilt(actor, blueprint, owner)

		Trigger.AfterDelay(DateTime.Seconds(1), function()
            BuildBase(collection, cyard, owner)
        end)
	end)
end

OnBlueprintBuilt = function(actor, blueprint, owner)
    owner.Cash = owner.Cash - blueprint.cost
	blueprint.actor = actor
	MaintainBuilding(actor, blueprint, 0.75)
	if blueprint.onBuilt then
		-- Build() will not work properly on producers if immediately called.
		Trigger.AfterDelay(DateTime.Seconds(1), function()
            blueprint.onBuilt(actor)
		end)
	end
end

IsBuildAreaBlocked = function(player, blueprint)
    local nw, se = blueprint.northwestEdge, blueprint.southeastEdge
    local blockers = Map.ActorsInBox(nw, se, function(actor)
		-- Neutral check is for ignoring trees near the refinery.
		return actor.Owner ~= Neutral and actor.CenterPosition.Z == 0 and actor.HasProperty("Health") and actor.Type ~= "stek"
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

BeginBaseMaintenance = function(collection, owner)
	Utils.Do(collection, function(blueprint)
		MaintainBuilding(blueprint.actor, blueprint)
	end)

	Utils.Do(owner.GetActors(), function(actor)
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

InsertBlueprints = function()
    local t = USSRBaseBlueprints
    local build = USSRToBeBuilt
    local index = 11
    Utils.Do(USSRToBeBuilt, function(b)
        table.insert(t, index, b)
        PrepareBlueprintEdges(b)
    end)
end

ProduceHarvester = function(owner, factory, delay)
	if PlayerMoney(owner) < Actor.Cost("harv") then
		return
	end

	local toBuild = { "harv" }
	owner.Build(toBuild, function()
		Trigger.AfterDelay(delay, function()
			ProduceArmor(factory)
		end)
	end)
end

--------------------------------------------------------------------
-----------------	BASE MANAGMENT BLOCK - END	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------	AI ATTACKING BLOCK - START	--------------------
--------------------------------------------------------------------

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

ProduceInfantry = function(barrack, owner)
    local delay = Utils.RandomInteger(DateTime.Seconds(2), DateTime.Seconds(4))
    --local owner = factory.Owner CHANGE: Remove this. Is just a reminder

	if (barrack.IsDead or barrack.Owner ~= owner) then
		return
	elseif PlayerMoney(owner) <= 299 and IsHarvesterMissing(owner) then
		return
	end

    local toBuild = { Utils.Random(InfantryTypes) }
    local allPaths = {}
    if barrack.Owner == USSR then
        allPaths = USSRAttackPaths
    else
        allPaths = BadGuyAttackPaths
    end

    local path = Utils.Random(allPaths)
	owner.Build(toBuild, function(units)
        if barrack.Owner == USSR then
            table.insert(InfantryUSSRAttackGroup, units[1])
            if #InfantryUSSRAttackGroup >= InfantryAttackGroupSize then
                SendUnits(InfantryUSSRAttackGroup, path)
                InfantryUSSRAttackGroup = { }
                Trigger.AfterDelay(DateTime.Minutes(2), function()
                    ProduceInfantry(barrack, owner)
			    end)
            else
                Trigger.AfterDelay(delay, function()
				    ProduceInfantry(barrack, owner)
			    end)
            end
        else
            table.insert(InfantryBadGuyAttackGroup, units[1])
            if #InfantryBadGuyAttackGroup >= InfantryAttackGroupSize then
                SendUnits(InfantryBadGuyAttackGroup, path)
                InfantryBadGuyAttackGroup = { }
                Trigger.AfterDelay(DateTime.Minutes(1.5), function()
                    ProduceInfantry(barrack, owner)
			    end)
            else
                Trigger.AfterDelay(delay, function()
				    ProduceInfantry(barrack, owner)
			    end) 
            end
        end
	end)
end

-----------------------
--- Tank Attacks    ---
-----------------------

ProduceArmor = function(factory)
	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
    local owner = factory.Owner

	if factory.IsDead or factory.Owner ~= owner then
		return
	elseif IsHarvesterMissing(owner) then
		if owner == USSR then
            ProduceHarvester(owner, factory, delay)
        else
            return
        end
    end

	local toBuild = { Utils.Random(VehicleTypes) }
    local allPaths = {}
    if factory.Owner == USSR then
        allPaths = USSRAttackPaths
    else
        allPaths = BadGuyAttackPaths
    end

    local path = Utils.Random(allPaths)
	owner.Build(toBuild, function(units)
        table.insert(VehicleAttackGroup, units[1])
        if #VehicleAttackGroup >= VehicleAttackGroupSize then
            SendUnits(VehicleAttackGroup, path)
            VehicleAttackGroup = { }
            Trigger.AfterDelay(VehicleAttackInterval, function()
                ProduceArmor(factory)
            end)
        else
            Trigger.AfterDelay(delay, function()
                ProduceArmor(factory)
            end)
        end
    end)
end

-----------------------
--- Air Attacks    ----
-----------------------

-- Decide if this is the proper way tio set attack frequency
ProduceAircraft = function()
    if (USSRAfld1.IsDead or USSRAfld1.Owner ~= USSR) and (USSRAfld2.IsDead or USSRAfld2.Owner ~= USSR) then
        return
    end

    USSR.Build(SovietAircraftType, function(units)
        local plane = units[1]
        PlanesAttackGroup[#PlanesAttackGroup + 1] = plane

        Trigger.OnKilled(plane, ProduceAircraft)

        local alive = Utils.Where(PlanesAttackGroup, function(p) return not p.IsDead end)
        if #alive < 2 then
            Trigger.AfterDelay(ProductionIntervalAir, ProduceAircraft)
        end

        InitializeAttackAircraft(plane, Greece)
    end)
end

PlanesAttack = function()
    local entry = Utils.Random({ IronTankEntry.Location, SovWaterEntry.Location, BadgerEntry.Location })
    local planeType = Utils.Random({SovietAircraftType})
    Media.Debug("Check till here 1")
    for p = 1, #planeType do
        Trigger.AfterDelay(DateTime.Seconds(0.25*p), function()
            local a = Actor.Create(planeType[p], true, { Owner = USSR, Location = entry })
            InitializeAttackAircraft(a, Greece)
        end)
    end
end

CheckPlaneAmmo = function(plane, dir)
    if not plane.IsDead and plane.AmmoCount() >= 1 then
        Trigger.AfterDelay(DateTime.Seconds(3), function()
            CheckPlaneAmmo(plane, dir)
        end)
    elseif not plane.IsDead then
            Trigger.ClearAll(plane)
            plane.Move(dir)
            plane.Destroy()
            return
    end
end

SendParadrop = function(waypoint, angle)

	if (USSRAfld1.IsDead or USSRAfld1.Owner ~= USSR) or (USSRAfld2.IsDead or USSRAfld2.Owner ~= USSR) then
		return
	end

	local airfield = BaseAfld
	local badger = PowerProxy.TargetParatroopers(waypoint, (Angle.SouthEast + angle ) )

	Utils.Do(badger, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
end

-----------------------
--- Naval Attacks  ----
-----------------------

EnemyLstReinforcements = function()
    local cargo = {}
    local path = Utils.Random(BadGuyAttackPaths)

    for i = 1, LstEnemyAmount do
        local c = Utils.Random(LstEnemyTypes)
        cargo[#cargo + 1] = c
    end
    local units = Reinforcements.ReinforceWithTransport(USSR, "lst", cargo, {SovWaterEntry.Location, SovWaterWaypoint.Location, EnemyLstDst.Location}, { SovWaterEntry.Location })[2]

    Utils.Do(units, function(u)
        Trigger.OnAddedToWorld(u, function()
            SendUnits({u}, path)
        end)
    end)
    Trigger.AfterDelay(DateTime.Minutes(3), function()
        if not BadGuySpen.IsDead then 
            EnemyLstReinforcements()
        else
            return
        end
    end)
end

EnemySubsReinforcements = function()
    local northLeftEdge = WPos.New( (CPos.New(62,18)).X * 1024,  (CPos.New(70,18)).Y * 1024, 0)
    local southRightEdge = WPos.New( (CPos.New(94,54)).X * 1024, (CPos.New(94,54)).Y * 1024, 0)
 
    local actors = Map.ActorsInBox( northLeftEdge, southRightEdge, function(actor)
		return actor.Owner == Greece and ( actor.Type == "pt" or actor.Type == "dd" or actor.Type == "ca" or actor.Type == "ss" or actor.Type == "spen" or actor.Type == "syrd" or actor.Type == "lst" )
	end)

    if #actors > 0 then
        local subs = Reinforcements.Reinforce(USSR, {"ss", "ss", "ss"}, { SovWaterEntry.Location, SovWaterEntry.Location + CVec.New(0, 2) })
        Utils.Do(subs, function(u)
            if not u.IsDead then
                u.AttackMove(SovWaterWaypoint.Location)
                IdleHunt(u)
            end
        end)
    end

    Trigger.AfterDelay(DateTime.Minutes(2), function()
        EnemySubsReinforcements()
    end)
end

--------------------------------------------------------------------
----------------	AI ATTACKING BLOCK - END        ----------------
--------------------------------------------------------------------

--------------------------------------------------------------------
----------------    SPECIAL BEHAVIORS BLOCK - START	----------------
--------------------------------------------------------------------

CheckDogs = function(owner, kenn)
    local col = {}
    if owner == USSR then 
        col = USSRRebuildableDogs
    else 
        col = BadGuyRebuildableDogs
    end

    for _, d in ipairs(col) do
        if not d.exists then
            ProduceDogs(d, owner, kenn)
            return
        end
    end
    Trigger.AfterDelay(DateTime.Seconds(10), function()
        CheckDogs(owner, kenn)
    end)
end

ProduceDogs = function(d, owner, kenn)
    Trigger.AfterDelay(DateTime.Seconds(9), function()
        if not kenn.IsDead then
            local dog = Reinforcements.Reinforce(owner, {"dog"}, { kenn.Location, kenn.Location + CVec.New(0, 1) })[1]
            d.exists = true
            Trigger.AfterDelay(DateTime.Seconds(1), function()
                dog.AttackMove(d.pos)
            end)

            Trigger.OnKilled(dog, function()
                d.exists = false
            end)
            Trigger.AfterDelay(DateTime.Seconds(10), function()
                CheckDogs(owner, kenn)
            end)
        end
    end)
end

InitHeliGuard = function(collection)
    for _, guard in ipairs(collection) do
        SetupGuardBehavior(guard)
    end
end

SetupGuardBehavior = function(guard)
    local heli = guard.actor
    local hpad = guard.guardPoint
    local loc = hpad.CenterPosition

    local function GuardLoop()
        if not heli.IsDead then

            if not heli.AmmoCount() then
                if not hpad.IsDead then
                    heli.ReturnToBase(hpad)
                end

                Trigger.OnIdle(heli, function()
                    GuardLoop()
                end)

                return
            end

            local enemies = Map.ActorsInCircle(loc, GuardRadius, function(e)
                return e.Owner == Greece and not e.IsDead and e.Type ~= "spy" and e.Type ~= "heli" and e.Type ~= "mh60" and e.Type ~= "tran" and e.Type ~= "hind"  and e.Type ~= "mig" and e.Type ~= "yak"
            end)

            if #enemies > 0 then
                heli.Attack(enemies[1])
            else
                if not hpad.IsDead then
                    heli.ReturnToBase(hpad)
                end
            end
            Trigger.AfterDelay(DateTime.Seconds(2), (GuardLoop))
        end
    end
    Trigger.AfterDelay(DateTime.Seconds(2), (GuardLoop))
end

SendIronCurtainAtk = function(units)
    if not units then
        units = Reinforcements.Reinforce(USSR, IronTanks, { IronTankEntry.Location, IronTankDst.Location })
    end

    Trigger.AfterDelay(DateTime.Seconds(20), function()
        Utils.Do(units, function(t)
            ironSwitch = ironSwitch + 1
            if not t.IsDead then
                IdleHunt(t)
                Trigger.OnDamaged(t, function()
                    if t.Health <= t.MaxHealth/2  then
                        IronCurtaining(t)
                        Trigger.ClearAll(t)
                    end
                end)
            end
        end)
    end)

    Trigger.AfterDelay(DateTime.Minutes(5), function()
        if not USSRIron.IsDead then
            SendIronCurtainAtk()
        end
    end)
end

IronCurtaining = function(actor)
    if ironSwitch > 0 then
        if not actor.IsDead and not USSRIron.IsDead then
            actor.GrantCondition("invulnerability", DateTime.Seconds(25))
        end
        ironSwitch = ironSwitch - 1
    end
end

--[[
PrepareNuclearLaunch = function()
    if not USSRMslo.IsDead then
        NukeConditionInt = USSRMslo.GrantCondition("prepare", nuclearCountdown)
    end

    Trigger.AfterDelay(nuclearCountdown, function()
        FindNuclearTarget()
    end)
end

FindNuclearTarget = function()
    local targets = Greece.GetActorsByType("atek")

    if #targets > 0 then
        Utils.Random(targets, function(t)
            NuclearLaunch(t)
        end)
    else
        targets = Greece.GetActorsByType("fact")
        if #targets > 0 then
            Utils.Random(targets, function(t)
                NuclearLaunch(t)
            end)
        else
            targets = Utils.Where(Greece.GetActors, function(a) return a.Type == "weap" end)
            if #targets > 0 then
                Utils.Random(targets, function(t)
                    NuclearLaunch(t)
                end)
            else
                Trigger.AfterDelay(DateTime.Seconds(10), function()
                   FindNuclearTarget()
                end)
            end
        end
    end
end

NuclearLaunch = function(loc)
    if not USSRMslo.IsDead then
        Media.PlaySpeechNotification(Greece, "AbombLaunchDetected")
        USSRMslo.ActivateNukePower(loc.Location)
        USSRMslo.RevokeCondition(NukeConditionInt)
    end
end
]]
--------------------------------------------------------------------
----------------    SPECIAL BEHAVIORS BLOCK - END	----------------
--------------------------------------------------------------------

SetupAIActivities = function()
    Media.Debug("Start Basic AI")

    USSR.Cash = USSRStartingCash
    BadGuy.Cash = BadGuyStartingCash

    --[[ IA GENERAL SETUP]]
    CheckDogs(BadGuy, BadGuyKenn)
    CheckDogs(USSR, USSRKenn)

    InitHeliGuard(USSRGuardHelicopters)
    InitHeliGuard(BadGuyGuardHelicopters)

    BeginBaseMaintenance(USSRBaseBlueprints, USSR)
    BeginBaseMaintenance(BadGuyBaseBlueprints, BadGuy)

    BuildBase(USSRBaseBlueprints, USSRFact, USSR)
    BuildBase(BadGuyBaseBlueprints, BadGuyFact, BadGuy)

    -- Main AI activities
    Trigger.AfterDelay(BadGuyActivationDelay, function()
        Media.Debug("Start BadGuy Combat AI")
        RunBadGuyActivities()
    end)

    Trigger.AfterDelay(DateTime.Minutes(2),  SendIronCurtainAtk)
    Trigger.AfterDelay(USSRActivationDelay, function()
        Media.Debug("Start USSR Combat AI")
        RunUSSRActivities()
    end)

    PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
    --[[
    Trigger.AfterDelay(DateTime.Seconds(5), function()
        if badgerCounterAtk > 0 then
            for i = 1, badgerCounterAtk do
                local rngAngle = Angle.New(Utils.Random({-1, 0, 1}) * 40) --WAngle New
                local rngPos = WVec.New(1024 * ( (-10) + i * 6), 1024 * (7), 0)
                Trigger.AfterDelay(DateTime.Seconds(0.25*i), function()
                    SendParadrop( (BadgerEntry.CenterPosition + rngPos), rngAngle) 
                end)
            end
        end
    end)
    ]]
end

RunUSSRActivities = function()
    InsertBlueprints()
    EnemySubsReinforcements()

    Trigger.AfterDelay(DateTime.Minutes(1), function()
        ProduceArmor(USSRWeap, USSR)
        ProduceInfantry(USSRBarr, USSR)
        ProduceAircraft()
    end)
    Trigger.AfterDelay(DateTime.Minutes(1.5), function()
        PlanesAttack()
    end)

    --[[
    if nuclearAtk == true then
        Trigger.AfterDelay(nuclearWaitTime, PrepareNuclearLaunch)
    end

    Trigger.AfterDelay(DateTime.Minutes(7.5), function()
        VehicleAttackGroupSize = VehicleAttackGroupSize + 1
        Trigger.AfterDelay(DateTime.Minutes(7.5), function()
            VehicleAttackGroupSize = VehicleAttackGroupSize + 1
        end)
    end)
    ]]
end

RunBadGuyActivities = function()
    ProduceInfantry(BadGuyBarr, BadGuy)

    Trigger.AfterDelay(DateTime.Minutes(3), function()
        Media.Debug("Lst")
        EnemyLstReinforcements()
    end)
end