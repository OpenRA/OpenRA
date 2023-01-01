--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroup = { }
AttackGroupSize = 10
BGAttackGroup = { }
BGAttackGroupSize = 6
SovietInfantry = { "e1", "e2", "e4" }
SovietVehicles = { "4tnk", "3tnk", "3tnk", "3tnk" }

SovietAircraftType = { "mig" }
Migs = { }

ProductionInterval =
{
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(24),
	hard = DateTime.Seconds(15)
}

SendBGAttackGroup = function()
	if #BGAttackGroup < BGAttackGroupSize then
		return
	end

	Utils.Do(BGAttackGroup, function(unit)
		if not unit.IsDead then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	BGAttackGroup = { }
end

ProduceBadGuyInfantry = function()
	if BadGuyRax.IsDead or BadGuyRax.Owner ~= BadGuy then
		return
	end

	BadGuy.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(BGAttackGroup, units[1])
		SendBGAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceBadGuyInfantry)
	end)
end

AttackWaypoints = { AttackWaypoint1, AttackWaypoint2 }
SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	local way = Utils.Random(AttackWaypoints)
	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			unit.AttackMove(way.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceInfantry = function()
	if (USSRRax1.IsDead or USSRRax1.Owner ~= USSR) and (USSRRax2.IsDead or USSRRax2.Owner ~= USSR) then
		return
	end

	USSR.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if USSRWarFactory.IsDead or USSRWarFactory.Owner ~= USSR then
		return
	end

	USSR.Build({ Utils.Random(SovietVehicles) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

ProduceAircraft = function()
	if (Airfield1.IsDead or Airfield1.Owner ~= BadGuy) and (Airfield2.IsDead or Airfield2.Owner ~= BadGuy) and (Airfield3.IsDead or Airfield3.Owner ~= BadGuy) and (Airfield4.IsDead or Airfield4.Owner ~= BadGuy) then
		return
	end

	BadGuy.Build(SovietAircraftType, function(units)
		local mig = units[1]
		Migs[#Migs + 1] = mig

		Trigger.OnKilled(mig, ProduceAircraft)

		local alive = Utils.Where(Migs, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Seconds(ProductionInterval[Difficulty] / 2), ProduceAircraft)
		end

		InitializeAttackAircraft(mig, Greece)
	end)
end

ParadropDelay =
{
	easy = { DateTime.Minutes(1), DateTime.Minutes(2) },
	normal = { DateTime.Seconds(45), DateTime.Minutes(1) },
	hard = { DateTime.Seconds(30), DateTime.Seconds(45) }
}

ParadropLZs = { ParaLZ1.CenterPosition, ParaLZ2.CenterPosition, ParaLZ3.CenterPosition, ParaLZ4.CenterPosition, ParaLZ5.CenterPosition }

Paradrop = function()
	local aircraft = StandardDrop.TargetParatroopers(Utils.Random(ParadropLZs), Angle.NorthWest)
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelay[1], ParadropDelay[2]), Paradrop)
end

BombDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

SendParabombs = function()
	local targets = Utils.Where(Greece.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
			actor.Type ~= "brik" and
			actor.Type ~= "sbag"
	end)

	if #targets > 0 then
		local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
		proxy.TargetAirstrike(Utils.Random(targets).CenterPosition, Angle.NorthWest)
		proxy.Destroy()
	end

	Trigger.AfterDelay(DateTime.Minutes(BombDelays), SendParabombs)
end

ActivateAI = function()
	ParadropDelay = ParadropDelay[Difficulty]
	BombDelays = BombDelays[Difficulty]

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	ProduceBadGuyInfantry()
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(4), ProduceVehicles)
	Trigger.AfterDelay(DateTime.Minutes(6), ProduceAircraft)
end
