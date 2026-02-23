--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local Cabins = { NorthCabin, SouthCabin }
local NWFootprintTrigger = { CPos.New(41, 49), CPos.New(41, 48), CPos.New(41, 47), CPos.New(41, 46), CPos.New(41, 45), CPos.New(41, 44) }
local SWFootprintTrigger = { CPos.New(26, 77), CPos.New(27, 77), CPos.New(28, 77), CPos.New(29, 77), CPos.New(33, 77), CPos.New(34, 77), CPos.New(35, 77), CPos.New(36, 77), CPos.New(37, 77), CPos.New(38, 77), CPos.New(39, 77) }
local SEFootprintTrigger = { CPos.New(75, 83), CPos.New(76, 83), CPos.New(77, 83), CPos.New(78, 83), CPos.New(79, 83), CPos.New(80, 83), CPos.New(81, 83), CPos.New(82, 83), CPos.New(83, 83), CPos.New(84, 83), CPos.New(85, 83), CPos.New(86, 83), CPos.New(87, 83), CPos.New(88, 83), CPos.New(89, 83) }
local NWWaterPath = { WaterEntryNW.Location, WaterLandingNW.Location }
local NEWaterPath = { WaterEntryNE.Location, WaterLandingNE.Location }
local SEWaterPath = { WaterEntrySE.Location, WaterLandingSE.Location }
local NWWaterUnits = { "dtrk", "ttnk", "ttnk", "ttnk", "3tnk" }
local NEWaterUnits = { "dtrk", "v2rl", "e6", "e6", "e6" }
local SEWaterUnits = { "ttnk", "ttnk", "shok", "shok", "shok" }
local NorthPillboxes = { NorthPill1, NorthPill2 }
local SouthPillboxes = { SouthPill1, SouthPill2 }
local VehicleSquad1 = { Ltnk1, Jeep1 }
local VehicleSquad2 = { Ltnk2, Jeep2 }
local VehicleSquad3 = { Ltnk3, Jeep3 }
local CivSquads = { { "c1", "c3", "c7", "c10" }, { "c2", "c4", "c6", "c11" }, { "c11", "c10", "c9" }, { "c8", "c7", "c6" }, { "c5", "c4", "c3" }, { "c5", "c10" }, { "c4", "c2" }, { "c3", "c5" }, { "c9", "c11" } }
local AlliedInfantry = { "e1", "e1", "e1", "e3" }
local AttackGroupSize = 4
local InfantryDelay = DateTime.Seconds(10)
local IdlingUnits = { }

--- This mission's human player.
local USSR = Player.GetPlayer("USSR")
--- Owner of most Allied forces and the southern base.
local Greece = Player.GetPlayer("Greece")
--- Owner of the radar base and northern Pillboxes.
local GoodGuy = Player.GetPlayer("GoodGuy")
--- Owner of the civilians.
local Spain = Player.GetPlayer("Spain")

local KillAll = AddPrimaryObjective(USSR, "destroy-opposition")
local CaptureDome = AddSecondaryObjective(USSR, "capture-enemy-radar-dome")

local TroopsArrived = false
local ShockDrop = Actor.Create("shockdrop", false, { Owner = USSR })
local BombDrop = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })

---@param actor actor
---@return boolean
local function IsSovietUnit(actor)
	return actor.Owner == USSR and actor.Type ~= "badr"
end

---@param cells cpos[]
---@param filter fun(actor: actor)
---@param action fun()
local function BasicFootprint(cells, filter, action)
	local activated = false

	Trigger.OnEnteredFootprint(cells, function(actor, id)
		if activated or not filter(actor) then
			return
		end

		activated = true
		Trigger.RemoveFootprintTrigger(id)
		action()
	end)
end

local function SendGreekAttack()
	local attackers = Utils.Take(AttackGroupSize, Utils.Shuffle(IdlingUnits))
	Utils.Do(attackers, IdleHunt)
end

local function ProduceInfantry()
	if not Greece.HasPrerequisites({ "tent" }) then
		return
	end

	local toBuild = { Utils.Random(AlliedInfantry) }

	Greece.Build(toBuild, function(units)
		IdlingUnits = Utils.Where(IdlingUnits, function(iu)
			return not iu.IsDead
		end)

		local i = #IdlingUnits + 1
		IdlingUnits[i] = units[1]
		Trigger.AfterDelay(InfantryDelay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendGreekAttack()
		end
	end)
end

---@param actors actor[]
---@param goal cpos
local function SendHunters(actors, goal)
	Utils.Do(actors, function(actor)
		if actor.IsDead then
			return
		end

		actor.AttackMove(goal)
		IdleHunt(actor)
	end)
end

---@param house actor
---@param spawn cpos
---@param rally cpos
local function SendCivilians(house, spawn, rally)
	if house.IsDead then
		return
	end

	local civilians = Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { spawn, rally }, 0)

	Utils.Do(civilians, function(civilian)
		-- Ensure civilians remember to flee once done panicking.
		Trigger.OnIdle(civilian, function()
			if civilian.Location == rally then
				Trigger.ClearAll(civilian)
				return
			end

			civilian.Move(rally, 2)
		end)
	end)
end

local function PrepareCabins()
	local northDamaged = false
	local southDamaged = false

	Trigger.OnDamaged(NorthCabin, function()
		if northDamaged then
			return
		end

		northDamaged = true
		SendCivilians(NorthCabin, NEHermitSpawn.Location, CivRallyNW.Location)
	end)

	Trigger.OnDamaged(SouthCabin, function()
		if southDamaged then
			return
		end

		southDamaged = true
		SendCivilians(SouthCabin, SouthHermitSpawn.Location, CivRallySE.Location)
	end)

	Trigger.OnAllKilled(Cabins, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.SouthWest)
	end)
end

local function PrepareVillages()
	local neProxTriggered = false

	Trigger.OnEnteredProximityTrigger(NECivSpawn1.CenterPosition, WDist.FromCells(9), function(actor, id)
		if neProxTriggered or not IsSovietUnit(actor) then
			return
		end
		Trigger.RemoveFootprintTrigger(id)
		neProxTriggered = true

		local neVillageCam = Actor.Create("camera", true, { Owner = USSR, Location = NECivSpawn1.Location })
		BombDrop.TargetAirstrike(NECivSpawn6.CenterPosition, Angle.SouthEast)
		SendHunters(VehicleSquad1, NECivSpawn1.Location)

		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local neTroops = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", NEWaterUnits, NEWaterPath, { NEWaterPath[1] })[2]
		local engineers = Utils.Where(neTroops, function(a) return a.Type == "e6" end)

		Trigger.OnAllRemovedFromWorld(engineers, function()
			if not USSR.IsObjectiveCompleted(CaptureDome) then
				USSR.MarkFailedObjective(CaptureDome)
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			SendCivilians(NEVillage1, NECivSpawn1.Location, CivRallyNW.Location)
			SendCivilians(NEVillage2, NECivSpawn2.Location, CivRallyNW.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			SendCivilians(NEVillage3, NECivSpawn3.Location, CivRallyNW.Location)
			SendCivilians(NEVillage4, NECivSpawn4.Location, CivRallyNW.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			SendCivilians(NEVillage5, NECivSpawn5.Location, CivRallySE.Location)
			SendCivilians(NEVillage6, NECivSpawn6.Location, CivRallySE.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			SendCivilians(NEVillage7, NECivSpawn7.Location, CivRallySE.Location)
			SendCivilians(NEVillage8, NECivSpawn8.Location, CivRallySE.Location)
			SendCivilians(NEVillage9, NECivSpawn9.Location, CivRallySE.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(10), neVillageCam.Destroy)
	end)

	BasicFootprint(SWFootprintTrigger, IsSovietUnit, function()
		local swVillageCam = Actor.Create("camera", true, { Owner = USSR, Location = SWCivSpawn2.Location })
		BombDrop.TargetAirstrike(SWCivSpawn3.CenterPosition, Angle.South)
		Reinforcements.Reinforce(Greece, { 'e1', 'e1', 'e3', 'e3', 'e3' }, { SWCivSpawn1.Location, SWCivSpawn1.Location + CVec.New(0, 1) }, 0, IdleHunt)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			SendCivilians(SWVillage1, SWCivSpawn1.Location, CivRallySE.Location)
			SendCivilians(SWVillage2, SWCivSpawn2.Location, CivRallySE.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			SendCivilians(SWVillage3, SWCivSpawn3.Location, CivRallySE.Location)
			SendCivilians(SWVillage4, SWCivSpawn4.Location, CivRallyNW.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			SendCivilians(SWVillage5, SWCivSpawn5.Location, CivRallySE.Location)
			SendCivilians(SWVillage6, SWCivSpawn6.Location, CivRallyNW.Location)
			SendCivilians(SWVillage7, SWCivSpawn7.Location, CivRallySE.Location)
		end)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			ShockDrop.TargetParatroopers(SouthwestLZ.CenterPosition, Angle.SouthWest)
			SendHunters(VehicleSquad3, NECivSpawn1.Location)
			swVillageCam.Destroy()
		end)
	end)
end

local function PrepareBases()
	Trigger.OnAllKilled(NorthPillboxes, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.South)
	end)

	BasicFootprint(NWFootprintTrigger, IsSovietUnit, function()
		if not NWTent1.IsDead then
			Reinforcements.Reinforce(GoodGuy, AlliedInfantry, { NWSpawn1.Location, NWSpawn1.Location + CVec.New(0, 1) }, 0, IdleHunt)
		end
		if not NWTent2.IsDead then
			Reinforcements.Reinforce(GoodGuy, AlliedInfantry, { NWSpawn2.Location, NWSpawn2.Location + CVec.New(0, 1) }, 0, IdleHunt)
		end

		SendHunters(VehicleSquad2, SWCivSpawn1.Location)
	end)

	Trigger.OnKilled(RadarDome, function()
		if USSR.IsObjectiveCompleted(CaptureDome) then
			return
		end

		USSR.MarkFailedObjective(CaptureDome)
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDrop.TargetParatroopers(CivRallyNW.CenterPosition, Angle.West)
	end)

	Trigger.OnCapture(RadarDome, function()
		USSR.MarkCompletedObjective(CaptureDome)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", NWWaterUnits, NWWaterPath, { NWWaterPath[1] })
		end)
	end)

	Trigger.OnAllKilled(SouthPillboxes, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", SEWaterUnits, SEWaterPath, { SEWaterPath[1] })

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			Reinforcements.Reinforce(USSR, { "msub", "msub" }, { MSubEntry.Location, MSubStop.Location })
			ShockDrop.TargetParatroopers(SEShockDrop.CenterPosition, Angle.SouthWest)
		end)
	end)

	BasicFootprint(SEFootprintTrigger, IsSovietUnit, function()
		BombDrop.TargetAirstrike(SEBaseBombingRun.CenterPosition, Angle.East)
	end)
end

local function MissionStart()
	local LZCamera = Actor.Create("camera", true, { Owner = USSR, Location = LZ.Location })
	ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.New(508))

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.New(520))
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		LZCamera.Destroy()
		TroopsArrived = true
	end)

	ProduceInfantry()
end

Tick = function()
	if USSR.HasNoRequiredUnits() and TroopsArrived then
		USSR.MarkFailedObjective(KillAll)
	end

	if Greece.HasNoRequiredUnits() and GoodGuy.HasNoRequiredUnits() and Spain.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(KillAll)
	end
end

WorldLoaded = function()
	InitObjectives(USSR)
	Camera.Position = LZ.CenterPosition
	MissionStart()
	PrepareBases()
	PrepareCabins()
	PrepareVillages()
end
