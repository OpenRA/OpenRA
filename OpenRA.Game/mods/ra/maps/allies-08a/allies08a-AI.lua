--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
AttackGroupSize = 10
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles = { "3tnk", "3tnk", "v2rl" }
SovietAircraftType = { "mig" }
Migs = { }

GroundWavesUpgradeDelay = DateTime.Minutes(12)
GroundAttackUnitType = "Normal"
GroundAttackUnits =
{
	Normal = { {"4tnk", "3tnk", "e2", "e2", "e2" }, { "3tnk", "v2rl", "e4", "e4", "e4" } },
	Upgraded = { {"4tnk", "3tnk", "ftrk", "apc", "apc", "e1", "e1", "e1", "e1", "e1", "e2", "e2", "e2" }, { "3tnk", "v2rl", "ftrk", "apc", "apc", "e1", "e1", "e1", "e1", "e1", "e4", "e4", "e4" } }
}
GroundAttackPaths =
{
	{ SovEntry1.Location, ParaLZ3.Location, AttackChrono.Location },
	{ SovEntry2.Location, ParaLZ5.Location, AttackChrono.Location },
	{ SovEntry3.Location, ParaLZ5.Location, AttackChrono.Location }
}
GroundWavesDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

WTransUnits = { { "4tnk", "3tnk", "e2", "e2", "e2" }, { "3tnk", "v2rl", "e4", "e4", "e4" } }
WTransWays =
{
	{ DDEntry.Location, WaterLanding1.Location },
	{ WaterEntry2.Location, WaterLanding2.Location },
	{ WaterEntry2.Location, WaterLanding3.Location }
}
WTransDelays =
{
	easy = 5,
	normal = 4,
	hard = 3
}

ParadropLZs = { ParaLZ1.CenterPosition, ParaLZ2.CenterPosition, ParaLZ3.CenterPosition, ParaLZ4.CenterPosition, ParaLZ5.CenterPosition }
ParadropDelays =
{
	easy = 3,
	normal = 2,
	hard = 1
}

BombDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

ProductionInterval =
{
	easy = DateTime.Seconds(20),
	normal = DateTime.Seconds(10),
	hard = DateTime.Seconds(5)
}

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceInfantry = function()
	if USSRRax.IsDead or USSRRax.Owner ~= ussr then
		return
	end

	ussr.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= ussr then
		return
	end

	ussr.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

ProduceAircraft = function()
	if (Airfield1.IsDead or Airfield1.Owner ~= ussr) and (Airfield2.IsDead or Airfield2.Owner ~= ussr) then
		return
	end

	ussr.Build(SovietAircraftType, function(units)
		local mig = units[1]
		Migs[#Migs + 1] = mig

		Trigger.OnKilled(mig, ProduceAircraft)

		local alive = Utils.Where(Migs, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(mig, greece)
	end)
end

GroundWaves = function()
	local path = Utils.Random(GroundAttackPaths)
	local units = Reinforcements.Reinforce(ussr, Utils.Random(GroundAttackUnits[GroundAttackUnitType]), { path[1] })
	local lastWaypoint = path[#path]
	Utils.Do(units, function(unit)
		Trigger.OnAddedToWorld(unit, function()
			unit.Patrol(path)
			Trigger.OnIdle(unit, function()
				unit.AttackMove(lastWaypoint)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(GroundWavesDelays), GroundWaves)
end

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(ussr, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(AttackChrono.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end

Paradrop = function()
	local aircraft = PowerProxy.TargetParatroopers(Utils.Random(ParadropLZs))
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(ParadropDelays), Paradrop)
end

SendParabombs = function()
	local airfield = Airfield1
	if Airfield1.IsDead or Airfield1.Owner ~= ussr then
		if Airfield2.IsDead or Airfield2.Owner ~= ussr then
			return
		end

		airfield = Airfield2
	end

	local targets = Utils.Where(greece.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
			actor.Type ~= "brik" and
			actor.Type ~= "sbag" or
			actor.Type == "pdox" or
			actor.Type == "atek"
	end)

	if #targets > 0 then
		airfield.TargetAirstrike(Utils.Random(targets).CenterPosition)
	end

	Trigger.AfterDelay(DateTime.Minutes(BombDelays), SendParabombs)
end

ActivateAI = function()
	GroundWavesDelays = GroundWavesDelays[Difficulty]
	WTransDelays = WTransDelays[Difficulty]
	ParadropDelays = ParadropDelays[Difficulty]
	BombDelays = BombDelays[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == ussr and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == ussr and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	ProduceInfantry()
	ProduceVehicles()
	ProduceAircraft()
	Trigger.AfterDelay(DateTime.Minutes(GroundWavesDelays), GroundWaves)
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
	Trigger.AfterDelay(DateTime.Minutes(ParadropDelays), Paradrop)
	Trigger.AfterDelay(DateTime.Minutes(BombDelays), SendParabombs)
	Trigger.AfterDelay(GroundWavesUpgradeDelay, function() GroundAttackUnitType = "Upgraded" end)
end
