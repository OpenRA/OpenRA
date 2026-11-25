--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--@class blueprint
--@field type string
--@field actor actor
--@field cost integer
--@field shape integer[]
--@field location cpos 
--@field owner? player
--@field produce? string
--@field northwestEdge? wpos
--@field southeastEdge? wpos
-- Could add a new field for when to SellBuilding

---@class AirWave
---@field types string[]
---@field interval number
---@field path cpos[]
---@field owner? player

--------------------------------------------------------------------
-----------------	DIFFICULTY BLOCK - START	--------------------
--------------------------------------------------------------------

local USSRCashReserves = { easy = 60000, normal = 75000, hard = 100000 }
local BadGuyCashReserves = { easy = 30000, normal = 40000, hard = 50000 }

local USSRActivationDelays = { easy = DateTime.Minutes(11), normal = DateTime.Minutes(9), hard = DateTime.Minutes(7) }
local BadGuyActivationDelays = { easy = DateTime.Minutes(4), normal = DateTime.Minutes(3), hard = DateTime.Minutes(3)  }

local BadgerCounterAtks = { easy = 1, normal = 2, hard = 3}

local Diff_InfantryGroupSize = { easy = 7, normal = 8, hard = 10}
local Diff_VehicleAttackGroupSize = { easy = 4, normal = 5, hard = 6} -- starts in n and it increases to n + 2
local Diff_VehicleAttackInterval = { easy = DateTime.Minutes(3), normal = DateTime.Seconds(135), hard = DateTime.Seconds(135) }

local Diff_ProductionIntervalAir = { easy = DateTime.Seconds(120), normal = DateTime.Seconds(90), hard = DateTime.Seconds(60) }

-- Not being used
local FComSabotage = { easy = true, normal = false, hard = false }

--------------------------------------------------------------------
-----------------	DIFFICULTY BLOCK - END  	--------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	    DATA BLOCK - START	------------------------
--------------------------------------------------------------------

-- CheckSecure
local BadGuyBaseAreaTL = WPos.New( (CPos.New(91, 33)).X * 1024,  (CPos.New(91, 33)).Y * 1024, 0)
local BadGuyBaseAreaBR = WPos.New( (CPos.New(116, 52)).X * 1024, (CPos.New(116, 52)).Y * 1024, 0)

local USSRBaseAreaTL = WPos.New( (CPos.New(45, 18)).X * 1024,  (CPos.New(45, 18)).Y * 1024, 0)
local USSRBaseAreaBR = WPos.New( (CPos.New(73, 37)).X * 1024, (CPos.New(73, 37)).Y * 1024, 0)

local USSRAttackPaths =
{
    {USSRLeftAtkPath1.Location, USSRLeftAtkPath2.Location, USSRLeftAtkPath3.Location},
    {USSRLeftAtkPath1.Location, USSRLeftAtkPath2.Location, USSRLeftAtkPath3.Location, USSRMidAtkPath2.Location},
    {USSRMidAtkPath1.Location, USSRMidAtkPath2.Location, USSRMidAtkPath3.Location}
}

local BadGuyAttackPaths =
{
    --Repeated attack path for equal distribution
    {BadGuyLeftAtkPath1.Location, BadGuyLeftAtkPath2.Location},
    {BadGuyLeftAtkPath1.Location, BadGuyLeftAtkPath2.Location},
    {BadGuyMidAtkPath1.Location, BadGuyMidAtkPath2.Location, BadGuyMidAtkPath3A.Location},
    {BadGuyMidAtkPath1.Location, BadGuyMidAtkPath2.Location, BadGuyMidAtkPath3B.Location}
}

local InfantryTypes = {"e1", "e2", "e4"}
-- InfantryAttackGroupSize is defined in difficulty setup
local InfantryBadGuyAttackGroup = { }
local InfantryBadguyAttackInterval = DateTime.Seconds(90)
local InfantryUSSRAttackGroup = { }
local InfantryUSSRAttackInterval = DateTime.Minutes(2)

local VehicleTypes = { "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "4tnk" }
--VehicleAttackGroupSize is defined in difficulty setup
local VehicleAttackGroup = { }
--VehicleAttackInterval is defined in difficulty setup

local PlanesAttackGroup = { }
local ParadropIntervals = DateTime.Minutes(5)
local AircraftTypes = { "yak", "mig" }

local BasePlanes = {}
local TotalAflds = 2
local CurrentAirWave = 1

---@type AirWave[]
local  SovietAirTeams =
{
	{ types = { "yak", "yak" }, interval = DateTime.Seconds(120), path = { USSRAircraftOrigin1.Location }},
	{ types = { "yak", "yak" }, interval = DateTime.Seconds(110), path = { USSRAircraftOrigin1.Location }},
	{ types = { "yak", "mig" }, interval = DateTime.Seconds(110), path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) }	},
	{ types = { "yak", "yak", "yak" }, interval = DateTime.Seconds(219),  path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) } },
	{ types = { "yak", "yak", "mig" }, interval = DateTime.Seconds(210), path = { USSRAircraftOrigin1.Location, USSRAircraftOrigin1.Location + CVec.New(-1, 0) } }
}

local LstEnemyTypes = { "3tnk", "3tnk", "v2rl" }
local LstEnemyAmount = 3

---@type blueprint[]
local USSRBaseBlueprints = {
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
    --[[
    { type = "hpad", actor = USSRHpad1, cost = 500, shape = { 2, 3 }, location = CPos.New(61, 20) },
    { type = "hpad", actor = USSRHpad2, cost = 500, shape = { 2, 3 }, location = CPos.New(64, 20) },
    ]]
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

---@type actor
local USSRProc1, USSRProc2 = nil, nil
---@type blueprint[]
local USSRRefineriesBlueprints = {
    { type = "proc", actor = USSRProc1, cost = 1400, shape = { 3, 4 }, location = CPos.New(55, 30) },
    { type = "proc", actor = USSRProc2, cost = 1400, shape = { 3, 4 }, location = CPos.New(48, 24) }
}

---@type blueprint[]
local BadGuyBaseBlueprints = {
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

    --[[
    { type = "hpad", actor = BadGuyHpad1, cost = 500, shape = { 2, 3 }, location = CPos.New(103, 43) },
    { type = "hpad", actor = BadGuyHpad2, cost = 500, shape = { 2, 3 }, location = CPos.New(110, 43) },
    ]]

    { type = "fix", actor = BadGuyFix, cost = 500, shape = { 3, 3 }, location = CPos.New(99, 42) },

    { type = "dome", actor = BadGuyDome, cost = 1500, shape = { 2, 3 }, location = CPos.New(113, 43) },
    --{ type = "stek", actor = BadGuyStek, cost = 1500, shape = { 3, 3 }, location = CPos.New(107, 41) },

    { type = "ftur", actor = BadGuyFtur, cost = 600, shape = { 1, 1 }, location = CPos.New(110, 51) },
    { type = "tsla", actor = BadGuyTesla1, cost = 1200, shape = { 1, 1 }, location = CPos.New(107, 41) },
    { type = "tsla", actor = BadGuyTesla2, cost = 1200, shape = { 1, 1 }, location = CPos.New(104, 50) },
    { type = "tsla", actor = BadGuyTesla3, cost = 1200, shape = { 1, 1 }, location = CPos.New(89, 51) },

    { type = "sam", actor = BadGuySam1, cost = 700, shape = { 2, 1 }, location = CPos.New(100, 41) },
    { type = "sam", actor = BadGuySam2, cost = 700, shape = { 2, 1 }, location = CPos.New(95, 46) },
    { type = "sam", actor = BadGuySam3, cost = 700, shape = { 2, 1 }, location = CPos.New(108, 41) }
}

local USSRRebuildableDog1, USSRRebuildableDog2, USSRRebuildableDog3, USSRRebuildableDog4 = nil, nil, nil, nil
local USSRGuardDog1Data = { actor = USSRRebuildableDog1, exists = true, pos = CPos.New(47, 24) }
local USSRGuardDog2Data = { actor = USSRRebuildableDog2, exists = true, pos = CPos.New(52, 25) }
local USSRGuardDog3Data = { actor = USSRRebuildableDog3, exists = true, pos = CPos.New(48, 35) }
local USSRGuardDog4Data = { actor = USSRRebuildableDog4, exists = true, pos = CPos.New(52, 39) }

local BadGuyRebuildableDog1, BadGuyRebuildableDog2, BadGuyRebuildableDog3 = nil, nil, nil
local BadGuyGuardDog1Data = { actor = BadGuyRebuildableDog1, exists = true, pos = CPos.New(96, 45) }
local BadGuyGuardDog2Data = { actor = BadGuyRebuildableDog2, exists = true, pos = CPos.New(106, 47) }
local BadGuyGuardDog3Data = { actor = BadGuyRebuildableDog3, exists = true, pos = CPos.New(112, 43) }

local USSRRebuildableDogs = { USSRGuardDog1Data, USSRGuardDog2Data, USSRGuardDog3Data, USSRGuardDog4Data}
local BadGuyRebuildableDogs = { BadGuyGuardDog1Data, BadGuyGuardDog2Data, BadGuyGuardDog3Data}

local IronTankPath = { USSRMidAtkPath1.Location, USSRMidAtkPath2.Location, USSRMidAtkPath3.Location, USSRMidAtkPath4.Location }
local IronSwitch = 0

--------------------------------------------------------------------
-----------------	    DATA BLOCK - END	------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	UTILS BLOCK - START	----------------------------
--------------------------------------------------------------------
local function ________________UTILS________________() end

local function PlayerMoney(owner)
	return owner.Cash + owner.Resources
end

local function GrantCash(player, amount)
    player.Cash = player.Cash + amount
end

--------------------------------------------------------------------
-----------------	UTILS BLOCK - END	----------------------------
--------------------------------------------------------------------

--------------------------------------------------------------------
-----------------	BASE MANAGEMENT BLOCK - START	----------------
--------------------------------------------------------------------
local function ________________BASE_MANAGEMENT________________() end

---@param owner player
local function IsHarvesterMissing(owner)
	return #owner.GetActorsByType("harv") == 0
end

---@param collection blueprint[]
---@param cyard actor
---@param owner player
local function BuildBase(collection, cyard, owner)
    for _, blueprint in ipairs(collection) do
        if not blueprint.actor then
            BuildBlueprint(blueprint, cyard, owner, collection)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
        BuildBase(collection, cyard, owner)
    end)
end

---@param blueprint blueprint
---@param cyard actor
---@param owner player
---@param collection blueprint[]
local function BuildBlueprint(blueprint, cyard, owner, collection)
    Trigger.AfterDelay(Actor.BuildTime(blueprint.type), function()
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

---@param actor actor
---@param blueprint blueprint
---@param owner player
local function OnBlueprintBuilt(actor, blueprint, owner)
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

---@param player player
---@param blueprint blueprint
local function IsBuildAreaBlocked(player, blueprint)
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

---@param player player
---@param actors actor[]
local function ScatterBlockers(player, actors)
	Utils.Do(actors, function(actor)
		if actor.IsIdle and actor.Owner == player and actor.HasProperty("Scatter") then
			actor.Scatter()
		end
	end)
end

---@param collection blueprint[]
---@param owner player
local function BeginBaseMaintenance(collection, owner)
	Utils.Do(collection, function(blueprint)
		MaintainBuilding(blueprint.actor, blueprint)
	end)

	Utils.Do(owner.GetActors(), function(actor)
		if actor.HasProperty("StartBuildingRepairs") then
			MaintainBuilding(actor, nil, 0.75)
		end
	end)
end

---@param actor actor
---@param blueprint blueprint
---@param repairThreshold number
local function MaintainBuilding(actor, blueprint, repairThreshold)
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

---@param blueprint blueprint
local function PrepareBlueprintEdges(blueprint)
	local shapeX, shapeY = blueprint.shape[1], blueprint.shape[2]
	local northwestEdge = Map.CenterOfCell(blueprint.location) + WVec.New(-512, -512, 0)
    local southeastEdge = northwestEdge + WVec.New(shapeX * 1024, shapeY * 1024, 0)
	blueprint.northwestEdge = northwestEdge
    blueprint.southeastEdge = southeastEdge
end

--Insert blueprints[] to player base building blueprints[]
---@param blueprints blueprint[]
---@param insert blueprint[]
local function InsertBlueprints(blueprints, insert)
    Utils.Do(insert, function(b)
        local index = #blueprints
		table.insert(blueprints, index, b)
        PrepareBlueprintEdges(b)
    end)
end

---@param owner player
---@param factory actor
local function ProduceHarvester(owner, factory, delay)
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

local function SendUnits(units, path)
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

local function ProduceInfantry(barrack, owner)
    local delay = Utils.RandomInteger(DateTime.Seconds(2), DateTime.Seconds(4))

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
                Trigger.AfterDelay(InfantryUSSRAttackInterval, function()
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
                Trigger.AfterDelay(InfantryBadguyAttackInterval, function()
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

local function ProduceArmor(factory)
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

local function ProduceAircraft()
    if (USSRAfld1.IsDead or USSRAfld1.Owner ~= USSR) and (USSRAfld2.IsDead or USSRAfld2.Owner ~= USSR) then
        return
    end

    USSR.Build(AircraftTypes, function(units)
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

---@param dir wangle
---@param angle wangle
local function SendParadrop(dst, dir, angle)
	if (USSRAfld1.IsDead or USSRAfld1.Owner ~= USSR) or (USSRAfld2.IsDead or USSRAfld2.Owner ~= USSR) then
		return
	end
    if angle == nil then angle = Angle.New(0 * 48) end

	local badger = PowerProxy.TargetParatroopers(dst, (dir + angle ) )

	Utils.Do(badger, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
end

local function HarassingParadrop()
    if BadgerCounterAtk > 0 then
        for i = 1, BadgerCounterAtk do
            local rngAngle = Angle.New(Utils.Random({-1, 0, 1}) * 40) --WAngle New
            local rngPos = WVec.New(1024 * ( (-10) + i * 6), 1024 * (7), 0)
            Trigger.AfterDelay(DateTime.Seconds(6 * i), function()
                SendParadrop( (BadgerEntry.CenterPosition + rngPos), Angle.SouthEast, rngAngle)
            end)
        end
        Trigger.AfterDelay(ParadropIntervals, HarassingParadrop)
    end
end

local function PrepareAircraftReinforcements()
	local delay = DateTime.Seconds(10)--FirstAirDelays[Difficulty] or FirstAirDelays["normal"]

	Trigger.AfterDelay(delay, function()
		ScheduleAirWave(1)
	end)
end

---@param player player
local function HasAirfield(player)
	return player.HasPrerequisites({ "afld" })
end

---@param wave integer
local function ScheduleAirWave(wave)
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

---@param aircraft actor
---@param exit cpos
local function OnAircraftStranded(aircraft, exit)
	local oldOwner = aircraft.Owner

	if oldOwner == aircraft.Owner then
		aircraft.Stop()
		aircraft.Move(exit)
		aircraft.Destroy()
	end
end

local function AreSovietPlanesActive()
	local planes = { "mig", "yak" }
	return #USSR.GetActorsByTypes(planes) > 0
end

-----------------------
--- Naval Attacks  ----
-----------------------

local function EnemyLstReinforcements()
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

local function EnemySubsReinforcements()
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

-----------------------
-----    Dogs     -----
-----------------------

Trigger.OnKilled(USSRGuardDog1, function() USSRGuardDog1Data.exists = false end)
Trigger.OnKilled(USSRGuardDog2, function() USSRGuardDog2Data.exists = false end)
Trigger.OnKilled(USSRGuardDog3, function() USSRGuardDog3Data.exists = false end)
Trigger.OnKilled(USSRGuardDog4, function() USSRGuardDog4Data.exists = false end)
Trigger.OnKilled(BadGuyGuardDog1, function() BadGuyGuardDog1Data.exists = false end)
Trigger.OnKilled(BadGuyGuardDog2, function() BadGuyGuardDog2Data.exists = false end)
Trigger.OnKilled(BadGuyGuardDog3, function() BadGuyGuardDog3Data.exists = false end)

---@param d actor
---@param owner player 
---@param kenn actor
local function ProduceDogs(d, owner, kenn)
    Trigger.AfterDelay(DateTime.Seconds(9), function()
        if not kenn.IsDead and kenn.Owner ~= owner then
            local dog = Reinforcements.Reinforce(owner, {"dog"}, { kenn.Location, kenn.Location + CVec.New(0, 1) })[1]
            d.exists = true
            Trigger.OnKilled(dog, function()
                d.exists = false
            end)

            Trigger.AfterDelay(DateTime.Seconds(1), function()
                if not dog.IsDead then
                    dog.AttackMove(d.pos)
                end
            end)

            Trigger.AfterDelay(DateTime.Seconds(10), function()
                CheckDogs(owner, kenn)
            end)
        end
    end)
end

---@param owner player
---@param kenn actor
local function CheckDogs(owner, kenn)
    local col = {}
    if owner == USSR then 
        col = USSRRebuildableDogs
    else
        col = BadGuyRebuildableDogs
    end

    for _, d in ipairs(col) do
        if not d.exists then
            Media.Debug("ProduceDog")
            ProduceDogs(d, owner, kenn)
            return
        end
    end
    Trigger.AfterDelay(DateTime.Seconds(10), function()
        CheckDogs(owner, kenn)
    end)
end

--------------------------------------------------------------------
----------------    SPECIAL BEHAVIORS BLOCK - END	----------------
--------------------------------------------------------------------

local function RunUSSRActivities()
    InsertBlueprints(USSRBaseBlueprints, USSRRefineriesBlueprints)
    EnemySubsReinforcements()

    ProduceArmor(USSRWeap, USSR)
    ProduceInfantry(USSRBarr, USSR)
    ProduceAircraft()

    if NuclearAtk == true then
        Trigger.AfterDelay(NuclearWaitTime, PrepareNuclearLaunch)
    end

    Trigger.AfterDelay(DateTime.Minutes(10), function()
        VehicleAttackGroupSize = VehicleAttackGroupSize + 1
        Trigger.AfterDelay(DateTime.Minutes(10), function()
            VehicleAttackGroupSize = VehicleAttackGroupSize + 1
        end)
    end)
end

local function RunBadGuyActivities()
    ProduceInfantry(BadGuyBarr, BadGuy)

    Trigger.AfterDelay(DateTime.Minutes(3), function()
        EnemyLstReinforcements()
    end)
end

SetupAIActivities = function()
    USSRCashReserve = USSRCashReserves[Difficulty]
    BadGuyCashReserve = BadGuyCashReserves[Difficulty]

    USSRActivationDelay = USSRActivationDelays[Difficulty]
    BadGuyActivationDelay = BadGuyActivationDelays[Difficulty]

    BadgerCounterAtk = BadgerCounterAtks[Difficulty]

    InfantryGroupSize = Diff_InfantryGroupSize[Difficulty]
    VehicleAttackGroupSize = Diff_VehicleAttackGroupSize[Difficulty]
    VehicleAttackInterval = Diff_VehicleAttackInterval[Difficulty]

    ProductionIntervalAir = Diff_ProductionIntervalAir[Difficulty]

    USSR.Cash = USSRCashReserve
    BadGuy.Cash = BadGuyCashReserve

    -- Basic IA activities
    CheckDogs(BadGuy, BadGuyKenn)
    CheckDogs(USSR, USSRKenn)

    BeginBaseMaintenance(USSRBaseBlueprints, USSR)
    BeginBaseMaintenance(BadGuyBaseBlueprints, BadGuy)

    BuildBase(USSRBaseBlueprints, USSRFact, USSR)
    BuildBase(BadGuyBaseBlueprints, BadGuyFact, BadGuy)

    Trigger.AfterDelay(TimeBeforeIronTanks,  SendIronCurtainAtk)

    -- Main AI activities
    Trigger.AfterDelay(BadGuyActivationDelay, RunBadGuyActivities)

    Trigger.AfterDelay(USSRActivationDelay, RunUSSRActivities)
end
