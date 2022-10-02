--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

ProducedUnitTypes =
{
	{ factory = ABarracks1,		types = { "e1", "e3" } },
	{ factory = ABarracks2,		types = { "e1", "e3" } },
	{ factory = ABarracks3,		types = { "e1", "e3" } },
	{ factory = AWarFactory1,	types = { "jeep", "jeep" } },
	{ factory = AWarFactory2,	types = { "2tnk", "jeep", "2tnk" } }
}

ProducedMainUnitTypes =
{
	{ factory = ABarracks4,		types = { "e1", "e3" } },
	{ factory = ABarracks5,		types = { "e1", "e3" } },
	{ factory = ABarracks6,		types = { "e1", "e3" } },
	{ factory = ABarracks7,		types = { "e1", "e3" } },
	{ factory = AWarFactory3,	types = { "2tnk", "arty", "1tnk" } },
	{ factory = AWarFactory4,	types = { "1tnk", "jeep" } }
}

AlliedAttackWaypoints = { PatrolPoint17, PatrolPoint5 }

SpawnPoints = { SpawnPoint2 }
SpawnPoints2 = { Rally3, Rally4, Rally5 }
SpawnPoints3 = { Rally7, Rally8, Rally9 }

ZombieEntryPoints2 = { RoadSpawn3, RoadSpawn16, RoadSpawn13, RoadSpawn14 }
ZombieEntryPoints3 = { RoadSpawn17, RoadSpawn18, RoadSpawn2, RoadSpawn1 }

MissileSilos = { MissileSilo1, MissileSilo2, MissileSilo3, MissileSilo4, MissileSilo5 }
Oils = { Oil1, Oil2, Oil3, Oil4, Oil5, Oil6, Oil7, Oil8, Oil9, Oil10, Oil11, Oil12, Oil13 }

TaskForceTeam1 = { "2tnk", "e1", "e3", "e3" }

UnitTypes = { "2tnk", "jeep", "1tnk" }

AlliedWaterTransReinforcements = { "2tnk", "2tnk", "e1", "e1", "e3" }
AlliedReinforcementsInsertionPath = { TransportInsertionPoint.Location, AlliedWaterDropoffPoint.Location }

HeliReinforcersPath = { SpawnPoint3.Location, PatrolPoint18.Location }
HeliReinforcersPath1 = { MainBaseWaypoint.Location, Waypoint1.Location }

InsertionPathEngland1 = { SpawnPoint3.Location, Waypoint1.Location }

InsertionPathEngland = { MainBaseWaypoint.Location, ChinookWaypoint.Location }
EnglandReinforcements = { "e1", "e3", "e1", "e3", "e1", "e3" }

DemoTruckTarget = { CPos.New(105,16) }

DemoCheckPoint2Base = { ABarracks6, ABarracks7, APower1, APower2, AHelipad1, AHelipad2 }
DemoCheckPoint3Base = { AWarFactory4, ABarracks5 }

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SendAlliedReinforcements = function()
	SendWave()
	Utils.Do(ProducedUnitTypes, ProduceUnits)
end

ActivateMainBase = function()
	Utils.Do(ProducedMainUnitTypes, ProduceMainUnits)
	DemoCheckPoint1()
end

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

SetupMainFactories = function()
	Utils.Do(ProducedMainUnitTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

ProduceUnits = function(t)		
	if CommunicationsCenter.IsDead then
		return
	end

	local factory = t.factory

	if not factory.IsDead then
		local unitType = t.types[Utils.RandomInteger(1, #t.types + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		if factory.Type == "tent" then
			factory.Produce(unitType, nil, "Infantry")
		else
			factory.Produce(unitType)
		end
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

ProduceMainUnits = function(t)
	local factory = t.factory
	if not factory.IsDead then
		local unitType = t.types[Utils.RandomInteger(1, #t.types + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

BindActorTriggers = function(a)
	if a.HasProperty("Hunt") then
		Trigger.OnIdle(a, function(a)
			if a.IsInWorld then
				a.Hunt()
			end
		end)
	else
		Trigger.OnIdle(a, function(a)
			if a.IsInWorld then
				a.AttackMove(PatrolPoint2.Location)
			end
		end)
	end

	local AirBaseAttackers = england_air.GetGroundAttackers()
	Utils.Do(AirBaseAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)

	local NorthAttackers = england_north.GetGroundAttackers()
	Utils.Do(NorthAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)

	local PrisonAttackers = england_prison.GetGroundAttackers()
	Utils.Do(PrisonAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)

	local MainAttackers = england_main.GetGroundAttackers()
	Utils.Do(MainAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

Wave = 0
Waves =
{
	{ delay = 500, units = { TaskForceTeam1 } },
	{ delay = 500, units = { TaskForceTeam1 } }
}

SendWave = function()
	if CommunicationsCenter.IsDead then
		return
	end
	Wave = Wave + 1
	local wave = Waves[Wave]
	Trigger.AfterDelay(wave.delay, function()
		Utils.Do(wave.units, function(units)
			local entry = Utils.Random(SpawnPoints).Location
			local target = Utils.Random(AlliedAttackWaypoints).Location
			SendUnits(entry, units, target)
		end)
		if (Wave < #Waves) then
			local delay = Utils.RandomInteger(DateTime.Seconds(10), DateTime.Seconds(20))
			Trigger.AfterDelay(delay, SendWave)
		end
	end)
end

SendUnits = function(entryCell, unitTypes, targetCell)
	local units = Reinforcements.Reinforce(england_air, unitTypes, { entryCell }, 40, function(a)
		if not a.HasProperty("AttackMove") then
			Trigger.OnIdle(a, function(a)
				a.Move(targetCell)
			end)
			return
		end

		Trigger.OnIdle(a, function(a)
			if a.Location ~= targetCell then
				a.AttackMove(targetCell)
			else
				a.Hunt()
			end
		end)
	end)
	local AirBaseAttackers = england_air.GetGroundAttackers()

	Utils.Do(AirBaseAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

DemoCheckPoint1 = function()
	MediumTank1 = Actor.Create("2tnk", true, { Location = SpawnPoint1.Location, Owner = england_main })
	MediumTank2 = Actor.Create("2tnk", true, { Location = SpawnPoint1.Location, Owner = england_main })

	MediumTank1.AttackMove(PatrolPoint17.Location)
	MediumTank2.AttackMove(PatrolPoint17.Location)

	local nukeDivisionReinforements1 = Reinforcements.Reinforce(england_main, { "e3", "e1", "e1", "e1", "e3" }, { SpawnPoint1.Location, PatrolPoint17.Location })

	Trigger.OnAllKilled(nukeDivisionReinforements1, DemoCheckPoint2)

	local MainAttackers = england_main.GetGroundAttackers()

	Utils.Do(MainAttackers, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

DemoCheckPoint2 = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(PatrolPoint15.Location)
	end

	nukeDivisionReinforements = Reinforcements.ReinforceWithTransport(england_main, "lst.in", AlliedWaterTransReinforcements, AlliedReinforcementsInsertionPath, { TransportInsertionPoint.Location })[2]

	EnglandMainBaseEntry = Actor.Create(MainBaseCameraEntry[Difficulty], true, { Owner = ussr, Location = EnglandMainBaseEnterCam.Location })

	Trigger.OnAllKilled(DemoCheckPoint2Base, DemoCheckPoint3)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.DisplayMessage("Clear a path for the demo truck.")
	end)
end

DemoCheckPoint3 = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(PatrolPoint18.Location)
	end

	Reinforcements.ReinforceWithTransport(england_north, "tran", EnglandReinforcements, HeliReinforcersPath, { SpawnPoint3.Location })
	Reinforcements.ReinforceWithTransport(england_north, "tran", EnglandReinforcements, HeliReinforcersPath1, { MainBaseWaypoint.Location })

	England2ndBaseInvasion = Actor.Create(MainBaseCamera2[Difficulty], true, { Owner = ussr, Location = EnglandMain2ndBasecamera.Location })

	Cruiser1 = Actor.Create("ca", true, { Location = SpawnPoint4.Location, Owner = england_main })
	Cruiser1.AttackMove(PatrolPoint11.Location)

	local MainBaseReinf = Reinforcements.Reinforce(england_main, { "2tnk", "e3", "e3", "e1" }, { SpawnPoint4.Location, PatrolPoint18.Location })

	Trigger.OnAllKilled(DemoCheckPoint3Base, FinalDemoCheckPoint)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Reinforcements.ReinforceWithTransport(england_north, "tran", EnglandReinforcements, InsertionPathEngland, { MainBaseWaypoint.Location })
	end)
end

FinalDemoCheckPoint = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(DemoDropoffPoint.Location)
		EnglandMainBaseCamera = Actor.Create(MainBaseCameraLarge[Difficulty], true, { Owner = ussr, Location = EnglandMainBaseCam.Location })
	end

	Artillery3 = Actor.Create("arty", true, { Location = MainBaseWaypoint1.Location, Owner = england_main })
	Artillery3.AttackMove(DemoDropoffPoint.Location)

	Reinforcements.ReinforceWithTransport(england_north, "tran", EnglandReinforcements, InsertionPathEngland, { MainBaseWaypoint.Location })
end

Trigger.OnEnteredFootprint(DemoTruckTarget, function(a, id)
	if a.Owner == ussr and a.Type == "dtrk" then
		Trigger.RemoveFootprintTrigger(id)

		DemoTruckParked = true 

		Media.PlaySpeechNotification(ussr, "ExplosiveChargePlaced")
		Media.DisplayMessage("20 seconds until self destruction!")

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			timerStarted = true

			remainingTime = DateTime.Seconds(20)

			if remainingTime > 0 and timerStarted then
				UserInterface.SetMissionText("Self Destruct In: " .. Utils.FormatTime(remainingTime), ussr.Color)
				remainingTime = remainingTime - 1
			elseif remainingTime == 0 then
				if DemoTruck.IsDead then
					return
				end
				MissionAccomplished()
				DemoTruck.Kill()
				UserInterface.SetMissionText("")
				Trigger.AfterDelay(DateTime.Seconds(0.5), function()
					local delay = Utils.RandomInteger(20, 20)
					Lighting.Flash("LightningStrike", delay)
					Utils.Do(MissileSilos, Kill)
					Utils.Do(Oils, Kill)
				end)
			end
		end)
	end
end)
