--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

WTransWays =
{
	{ USSRRFEntry.Location, USSRUnload1.Location },
	{ USSRRFEntry.Location, USSRUnload2.Location }
}
WTransUnits =
{
	hard = { { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl" }, { "v2rl", "v2rl", "e4", "e4", "3tnk" } },
	normal = { { "e1", "e1", "3tnk", "3tnk", "v2rl" }, { "e4", "e4", "e4", "e4", "v2rl" } },
	easy = { { "e1", "e1", "e1", "e2", "e2" }, { "e2", "3tnk", "3tnk" } }
}
WTransDelays =
{
	easy = 7,
	normal = 6,
	hard = 5
}
SubAttackGroupSize =
{
	easy = 1,
	normal = 2,
	hard = 3
}
InfantryUnits = 
{
	hard = { "e1", "e2", "e2", "e4", "e4" },
	normal = { "e1", "e1", "e2", "e2", "e4" },
	easy = { "e1", "e1", "e1", "e2", "e2" }
}
ProductionInterval =
{
	easy = DateTime.Seconds(60),
	normal = DateTime.Seconds(40),
	hard = DateTime.Seconds(20)
}
ParadropDelay =
{
	easy = 7,
	normal = 6,
	hard = 5
}

InfantryAttackGroup = { }
InfantryAttackGroupSize = 5
VehicleAttackGroup = { }
VehicleAttackGroupSize = 3
SubAttackGroup = { }
SovietAircraftType = { "yak" }
SovietSSType = { "ss" }
VehicleUnits = { "3tnk", "3tnk", "3tnk", "v2rl" }

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SendInfantryAttackGroup = function()
	if #InfantryAttackGroup < InfantryAttackGroupSize then
		return
	end
	Utils.Do(InfantryAttackGroup, IdleHunt)
	InfantryAttackGroup = { }
end

SendVehicleAttackGroup = function()
	if #VehicleAttackGroup < VehicleAttackGroupSize then
		return
	end
	Utils.Do(VehicleAttackGroup, IdleHunt)
	VehicleAttackGroup = { }
end

SendSubAttackGroup = function()
	if #SubAttackGroup < SubAttackGroupSize then
		return
	end
	Utils.Do(SubAttackGroup, IdleHunt)
	SubAttackGroup = { }
end

ProduceSovietInfantry = function()
	if (Barracks.IsDead or Barracks.Owner ~= USSR) and (BarracksA.IsDead or BarracksA.Owner ~= USSR) then
		return
	end
	USSR.Build({ Utils.Random(InfantryUnits) }, function(units)
		table.insert(InfantryAttackGroup, units[1])
		SendInfantryAttackGroup()
		Trigger.AfterDelay(ProductionInterval, ProduceSovietInfantry)
	end)
end

ProduceSovietVehicle = function()
	if WarFactory.IsDead or WarFactory.Owner ~= USSR then
		return
	end
	USSR.Build({ Utils.Random(VehicleUnits) }, function(units)
		table.insert(VehicleAttackGroup, units[1])
		SendVehicleAttackGroup()
		Trigger.AfterDelay(ProductionInterval, ProduceSovietVehicle)
	end)
end

ProduceSovietSub = function()
	if SubPen.IsDead or SubPen.Owner ~= USSR then
		return
	end
	USSR.Build(SovietSSType, function(units)
		table.insert(SubAttackGroup, units[1])
		SendSubAttackGroup()
		Trigger.AfterDelay(ProductionInterval, ProduceSovietSub)
	end)
end

ProduceAircraft = function()
	if Airfield.IsDead or Airfield.Owner ~= USSR then
		return
	end
	USSR.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Trigger.OnKilled(yak, ProduceAircraft)
		InitializeAttackAircraft(yak, Greece)
	end)
end

WTransWaves = function()
	if SubPen.IsDead or SubPen.Owner ~= USSR then
		return
	end
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(USSR, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(KosyginExtractPoint.Location)
			IdleHunt(a)
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end

MMGroupGuardGate = function()
	if not MM1.IsDead then
		MM1.AttackMove(WP78.Location)
	end
	if not MM2.IsDead then
		MM2.AttackMove(WP79.Location)
	end
	if not MM1.IsDead then
		MM3.AttackMove(WP80.Location)
	end
end

TankGroupWallGuard = function()
	if not WGTank01.IsDead then
		WGTank01.AttackMove(WP72.Location)
	end
	if not WGTank02.IsDead then
		WGTank02.AttackMove(WP72.Location)
	end
	if not WGV2.IsDead then
		WGV2.AttackMove(WP72.Location)
	end
end

Paradrop = function()
	if Airfield.IsDead or Airfield.Owner ~= USSR then
		return
	end
	local aircraft = PowerProxy.TargetParatroopers(KosyginExtractPoint.CenterPosition)
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(ParadropDelay), Paradrop)
end

ActivateAI = function()
	local difficulty = Map.LobbyOption("difficulty")
	WTransUnits = WTransUnits[difficulty]
	WTransDelays = WTransDelays[difficulty]
	SubAttackGroupSize = SubAttackGroupSize[difficulty]
	InfantryUnits = InfantryUnits[difficulty]
	ProductionInterval = ProductionInterval[difficulty]
	ParadropDelay = ParadropDelay[difficulty]
	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceAircraft)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceSovietInfantry)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceSovietVehicle)
	Trigger.AfterDelay(DateTime.Minutes(4), ProduceSovietSub)
	Trigger.AfterDelay(DateTime.Minutes(5), MMGroupGuardGate)
	Trigger.AfterDelay(DateTime.Minutes(5), TankGroupWallGuard)
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
	Trigger.AfterDelay(DateTime.Minutes(ParadropDelay), Paradrop)
end
