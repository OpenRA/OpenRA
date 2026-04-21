--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
FirstPatrolActors = { Cruiser1, Gunboat1, Gunboat2 }
PatrolA = { Rocket1, Rocket2 }
PatrolB = { Rocket3, Rocket4 }
PatrolC = { Rocket5, Rocket6 }
PatrolD = { Rocket7, Rocket8 }
PatrolPathA = { RocketPath1.Location, RocketPath2.Location }
PatrolPathB = { RocketPath3.Location, RocketPath4.Location }
PatrolPathC = { RocketPath5.Location, RocketPath6.Location }
PatrolPathD = { RocketPath7.Location, RocketPath8.Location }

Helipads = { Helipad1, Helipad2, Helipad3, Helipad4 }
HeliType = { "heli" }

DestroyerSquad = { "dd", "dd" }

DestroyerDelays =
{
	easy = 4,
	normal = 3,
	hard = 2
}

TransportDelays =
{
	easy = 6,
	normal = 5,
	hard = 4
}

AlliedShips =
{
	easy = { "pt", "pt", "dd" },
	normal = { "pt", "dd", "dd" },
	hard = { "dd", "dd" , "dd" }
}

Helis = { }

PatrolWay = { ShipWaypoint1.Location, ShipWaypoint2.Location, ShipWaypoint3.Location, ShipWaypoint4.Location, ShipWaypoint5.Location }

TransportWays =
{
	{ AlliedTransportEntry1.Location, AlliedTransportDrop1.Location },
	{ AlliedTransportEntry2.Location, AlliedTransportDrop2.Location },
	{ AlliedTransportEntry3.Location, AlliedTransportDrop3.Location }
}

TransportUnits =
{
	easy = { { "1tnk", "1tnk", "2tnk" }, { "arty", "jeep", "1tnk" } },
	normal = { { "1tnk", "2tnk", "2tnk" }, { "arty", "arty", "1tnk" } },
	hard = { { "2tnk", "2tnk", "2tnk" }, { "arty", "arty", "2tnk" } }
}

SurpriseTransportWay = { SurpriseTransportEntry.Location, SurpriseTransportDrop.Location }

SurpriseTransportUnits =
{
	easy = { "jeep", "jeep", "1tnk" },
	normal = { "jeep", "jeep", "1tnk" },
	hard = { "jeep", "1tnk", "2tnk" }
}

ProductionDelays =
{
	easy = 3,
	normal = 2,
	hard = 1
}

SendCruiser = function()
	if NavalYard2.IsDead or NavalYard2.Owner ~= Greece then
		return
	end

	local boat = Reinforcements.Reinforce(Greece, { "ca" }, { CruiserEntry.Location })
	Utils.Do(boat, function(ca)
		ca.Move(DefaultCameraPosition.Location)
		Trigger.OnKilled(ca, function()
			Trigger.AfterDelay(DateTime.Minutes(3), SendCruiser)
		end)
	end)
end

DestroyerAttacks = function()
	if NavalYard3.IsDead or NavalYard3.Owner ~= Greece then
		return
	end

	local boats = Reinforcements.Reinforce(Greece, DestroyerSquad, { ShipEntry.Location })
	SendPatrol(boats, PatrolWay, false)
	Trigger.OnAllKilled(boats, function()
		Trigger.AfterDelay(DateTime.Minutes(DestroyerDelays[Difficulty]), DestroyerAttacks)
	end)
end

AlliedTransportReinforcements = function()
	local way = Utils.Random(TransportWays)
	local group = Utils.Random(TransportUnits[Difficulty])
	local units = Reinforcements.ReinforceWithTransport(Greece, "lst", group,  way, { way[2], way[1] } )[2]
	Utils.Do(units, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(PlayerBase.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(TransportDelays[Difficulty]), AlliedTransportReinforcements)
end

BuildingsHealing = function()
	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == Greece and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

StartPatrols = function()
	SendPatrol(FirstPatrolActors, PatrolWay, false)
	SendPatrol(PatrolA, PatrolPathA, true, 20)
	SendPatrol(PatrolB, PatrolPathB, true, 20)
	SendPatrol(PatrolC, PatrolPathC, true, 20)
	SendPatrol(PatrolD, PatrolPathD, true, 20)
end

SendPatrol = function(actors, path, loop, wait)
	Utils.Do(actors, function(actor)
		if actor.IsDead then
			return
		end

		actor.Patrol(path, loop or false, wait or 0)

		if not loop then
			IdleHunt(actor)
		end
	end)
end

ProduceShips = function()
	if NavalYard1.IsDead or NavalYard1.Owner ~= Greece then
		return
	end

	Greece.Build(AlliedShips[Difficulty], function(ships)
		Utils.Do(ships, function(a)
			a.AttackMove(ShipWaypoint5.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(ProductionDelays[Difficulty]), ProduceShips)
end

ProduceHelicopters = function()
	if Utils.All(Helipads, function(a) return a.IsDead or a.Owner ~= Greece end) then
		return
	end

	Greece.Build(HeliType, function(helis)
		local heli = helis[1]
		Helis[#Helis+1] = heli

		Trigger.OnKilled(heli, ProduceHelicopters)

		local alive = Utils.Where(Helis, function(y) return not y.IsDead end)
		if #alive < 2 then
			Trigger.AfterDelay(DateTime.Minutes(ProductionDelays[Difficulty]), ProduceHelicopters)
		end

		InitializeAttackAircraft(heli, USSR)
	end)
end

AlliedTransportAmbush = function(cargo, path)
	Trigger.OnEnteredProximityTrigger(AmbushTrigger.CenterPosition, WDist.FromCells(7), function(a, id)
		if a.Owner ~= USSR or a.Type == "badr" or a.Type == "u2" or a.Type == "camera.spyplane" then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		local units = Reinforcements.ReinforceWithTransport(Greece, "lst", cargo,  path, { path[2], path[1] } )[2]
		Utils.Do(units, function(u)
			Trigger.OnAddedToWorld(u, function()
				u.AttackMove(PlayerBase.Location)
				IdleHunt(u)
			end)
		end)
	end)
end

BridgeTrigger = function()
	TheBridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)[1]
	Trigger.OnKilled(BridgeBarrel, function()
		if not TheBridge.IsDead then
			TheBridge.Kill()
		end
	end)

	Trigger.OnEnteredProximityTrigger(BaseBridge.CenterPosition, WDist.FromCells(3), function(actor, id)
		if actor.Owner == USSR and actor.Type ~= "badr" and actor.Type ~= "u2" and actor.Type ~= "camera.spyplane" then
			Trigger.RemoveProximityTrigger(id)

			if not BridgeTank.IsDead and not BridgeBarrel.IsDead and not TheBridge.IsDead then
				BridgeTank.Attack(BridgeBarrel, true, true)
			end
		end
	end)
end

ActivateAI = function()
	BuildingsHealing()
	AlliedTransportAmbush(SurpriseTransportUnits[Difficulty], SurpriseTransportWay)
	BridgeTrigger()
	NavalYard1.IsPrimaryBuilding = true

	Trigger.AfterDelay(DateTime.Minutes(1), StartPatrols)
	Trigger.AfterDelay(DateTime.Minutes(3), AlliedTransportReinforcements)
	Trigger.AfterDelay(DateTime.Minutes(4), DestroyerAttacks)
	Trigger.AfterDelay(DateTime.Minutes(5), ProduceShips)
	Trigger.AfterDelay(DateTime.Minutes(6), ProduceHelicopters)
	Trigger.AfterDelay(DateTime.Minutes(7), SendCruiser)
end
