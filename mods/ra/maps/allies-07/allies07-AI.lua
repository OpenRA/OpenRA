--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--- This mission's human player.
local Greece = Player.GetPlayer("Greece")
--- Owner of the main Soviet base and most submarines.
local USSR = Player.GetPlayer("USSR")
--- Owner of the radar base, some scattered units, and the Hard battalion.
local BadGuy = Player.GetPlayer("BadGuy")

local AttackGroup = { }
local AttackGroupSize = 10
local BadGuyAttackGroup = { }
local BadGuyAttackSize = 8
local BadGuyAttackCount = 0

local SovietAircraftType = { "yak" }
local Yaks = { }
local SovietInfantry = { "e1", "e2", "e4" }
local SovietVehicles =
{
	hard = { "3tnk", "3tnk", "v2rl" },
	normal = { "3tnk" },
	easy = { "3tnk", "apc" }
}

local ProductionIntervals =
{
	easy = DateTime.Seconds(20),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(10)
}

local FirstAirDelays =
{
	easy = DateTime.Seconds(450),
	normal = DateTime.Seconds(360),
	hard = DateTime.Seconds(300)
}

local MainBaseActivationDelays =
{
	easy = DateTime.Seconds(720),
	normal = DateTime.Seconds(540),
	hard = DateTime.Seconds(420)
}

local RadarBaseActivationDelays =
{
	easy = DateTime.Seconds(75),
	normal = DateTime.Seconds(45),
	hard = DateTime.Seconds(30)
}

local MainBaseActivated = false
local RadarBaseActivated = false
local ParadropIntervals = { DateTime.Seconds(30), DateTime.Seconds(60) }
local ParadropCount = 0
local ParadropMax = 6
local ParadropLZs = { ParaLZ1.CenterPosition, ParaLZ2.CenterPosition, ParaLZ3.CenterPosition, ParaLZ4.CenterPosition }

--- Schedule paratroopers, loosely based on RA1 teams "para" and "tm05".
local function Paradrop()
	Trigger.AfterDelay(Utils.RandomInteger(ParadropIntervals[1], ParadropIntervals[2]), function()
		local aircraft = ParaProxy.TargetParatroopers(Utils.Random(ParadropLZs))
		Utils.Do(aircraft, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				IdleHunt(p)
			end)
		end)

		ParadropCount = ParadropCount + 1
		if ParadropCount <= ParadropMax then
			Paradrop()
		end
	end)
end

--- Form a small tank team from BadGuy's starting units for a hunt.
--- Based on the RA1 trigger "tm15".
local function SendBadGuyTank()
	local path = { RadarDefenseLanding.Location, ParaLZ4.Location, ParaLZ3.Location }
	local tank = BadGuy.GetActorsByType("3tnk")[1]
	local eastFlames = Utils.Where(BadGuy.GetActorsByType("e4"), function(flame)
		return flame.Location.X > ParaLZ2.Location.X
	end)

	Utils.Do(eastFlames, function(flame)
		flame.Patrol(path)
		IdleHunt(flame)

		if tank then
			tank.Guard(flame)
		end
	end)

	if tank then
		IdleHunt(tank)
	end
end

local function SendBadGuyAttackGroup()
	if #BadGuyAttackGroup < BadGuyAttackSize then
		return
	end

	Utils.Do(BadGuyAttackGroup, IdleHunt)
	BadGuyAttackGroup = { }
	BadGuyAttackCount = BadGuyAttackCount + 1

	if BadGuyAttackCount == 2 then
		SendBadGuyTank()
	end
end

local function ProduceBadGuyInfantry()
	if BadGuyRax.IsDead or BadGuyRax.Owner ~= BadGuy then
		return
	end

	BadGuy.Build({ Utils.Random(SovietInfantry) }, function(units)
		BadGuyAttackGroup[#BadGuyAttackGroup + 1] = units[1]
		SendBadGuyAttackGroup()
		Trigger.AfterDelay(ProductionIntervals[Difficulty], ProduceBadGuyInfantry)
	end)
end

local function SendAttackGroup()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, IdleHunt)
	AttackGroup = { }
end

local function ProduceUSSRInfantry()
	if USSRRax.IsDead or USSRRax.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietInfantry) }, function(units)
		AttackGroup[#AttackGroup + 1] = units[1]
		SendAttackGroup()
		Trigger.AfterDelay(ProductionIntervals[Difficulty], ProduceUSSRInfantry)
	end)
end

local function ProduceVehicles()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietVehicles[Difficulty]) }, function(units)
		AttackGroup[#AttackGroup + 1] = units[1]
		SendAttackGroup()
		Trigger.AfterDelay(ProductionIntervals[Difficulty], ProduceVehicles)
	end)
end

local function ProduceAircraft()
	if not USSR.HasPrerequisites({ "afld" }) then
		return
	end

	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)

		local alive = Utils.Where(Yaks, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionIntervals[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(yak, Greece)
	end)
end

local function ActivateRadarBase()
	if RadarBaseActivated then
		return
	end

	RadarBaseActivated = true
	ProduceBadGuyInfantry()
end

local function ActivateMainBase()
	if MainBaseActivated then
		return
	end

	MainBaseActivated = true
	ProduceUSSRInfantry()
	ProduceVehicles()
end

ActivateAI = function()
	local buildings = Utils.Where(USSR.GetActors(), function(self) return self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 0.75 then
				building.StartBuildingRepairs()
				ActivateMainBase()
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(60), Paradrop)
	Trigger.AfterDelay(MainBaseActivationDelays[Difficulty], ActivateMainBase)
	Trigger.AfterDelay(FirstAirDelays[Difficulty], ProduceAircraft)
	Trigger.AfterDelay(RadarBaseActivationDelays[Difficulty], ActivateRadarBase)

	-- Based on RA1 trigger "bact".
	Trigger.OnEnteredProximityTrigger(RadarDefenseProximity.CenterPosition, WDist.FromCells(10), function(actor, id)
		if actor.Owner ~= Greece then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		ActivateRadarBase()
	end)
end
