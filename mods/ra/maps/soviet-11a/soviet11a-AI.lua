--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
PatrolPathA = { RocketPath1.Location, RocketPath2.Location }
PatrolPathB = { RocketPath3.Location, RocketPath4.Location }
PatrolPathC = { RocketPath5.Location, RocketPath6.Location }

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

SurpriseTransportWay = { SurpriseTransportEntry.Location, SurpriseTransportDrop.Location }

TransportUnits =
{
	easy = { { "1tnk", "1tnk", "2tnk" }, { "arty", "jeep", "1tnk" } },
	normal = { { "1tnk", "2tnk", "2tnk" }, { "arty", "arty", "1tnk" } },
	hard = { { "2tnk", "2tnk", "2tnk" }, { "arty", "arty", "2tnk" } }
}

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

WesternBoatAttacks = function()
	if NavalYard3.IsDead or NavalYard3.Owner ~= Greece then
		return
	end

	local boats = Reinforcements.Reinforce(Greece, DestroyerSquad, { ShipEntry.Location })
	Utils.Do(boats, function(dd)
		dd.Patrol(PatrolWay, false, 1)
		IdleHunt(dd)
	end)
	Trigger.OnAllKilled(boats, function()
		Trigger.AfterDelay(DateTime.Minutes(DestroyerDelays), WesternBoatAttacks)
	end)
end

AlliedTransportReinforcements = function()
	local way = Utils.Random(TransportWays)
	local group = Utils.Random(TransportUnits)
	local units = Reinforcements.ReinforceWithTransport(Greece, "lst", group,  way, { way[2], way[1] } )[2]
	Utils.Do(units, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(PlayerBase.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(TransportDelays), AlliedTransportReinforcements)
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
	Utils.Do(FirstPatrolActors, function(a)
		if not a.IsDead then
			a.Patrol(PatrolWay, false, 1)
			IdleHunt(a)
		end
	end)

	Utils.Do(PatrolA, function(a)
		a.Patrol(PatrolPathA, true, 20)
	end)
	Utils.Do(PatrolB, function(b)
		b.Patrol(PatrolPathB, true, 20)
	end)
	Utils.Do(PatrolC, function(c)
		c.Patrol(PatrolPathC, true, 20)
	end)
end

ProduceShips = function()
	if NavalYard1.IsDead or NavalYard1.Owner ~= Greece then
		return
	end

	Greece.Build(AlliedShips, function(ships)
		Utils.Do(ships, function(a)
			a.AttackMove(ShipWaypoint5.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(ProductionDelays), ProduceShips)
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
			Trigger.AfterDelay(DateTime.Minutes(ProductionDelays), ProduceHelicopters)
		end

		InitializeAttackAircraft(heli, USSR)
	end)
end

AlliedTransportAmbush = function()
	Trigger.OnEnteredProximityTrigger(AmbushTrigger.CenterPosition, WDist.New(1024 * 7), function(a, id)
		if a.Owner == USSR then
			Trigger.RemoveProximityTrigger(id)
			local way = SurpriseTransportWay
			local group = SurpriseTransportUnits
			local units = Reinforcements.ReinforceWithTransport(Greece, "lst", group,  way, { way[2], way[1] } )[2]
			Utils.Do(units, function(a)
				Trigger.OnAddedToWorld(a, function()
					a.AttackMove(PlayerBase.Location)
					IdleHunt(a)
				end)
			end)
		end
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
	TransportDelays = TransportDelays[Difficulty]
	DestroyerDelays = DestroyerDelays[Difficulty]
	TransportUnits = TransportUnits[Difficulty]
	ProductionDelays = ProductionDelays[Difficulty]
	AlliedShips = AlliedShips[Difficulty]
	SurpriseTransportUnits = SurpriseTransportUnits[Difficulty]

	BuildingsHealing()
	AlliedTransportAmbush()
	BridgeTrigger()
	NavalYard1.IsPrimaryBuilding = true

	Trigger.AfterDelay(DateTime.Minutes(1), StartPatrols)
	Trigger.AfterDelay(DateTime.Minutes(3), AlliedTransportReinforcements)
	Trigger.AfterDelay(DateTime.Minutes(4), WesternBoatAttacks)
	Trigger.AfterDelay(DateTime.Minutes(5), ProduceShips)
	Trigger.AfterDelay(DateTime.Minutes(6), ProduceHelicopters)
	Trigger.AfterDelay(DateTime.Minutes(7), SendCruiser)
end
