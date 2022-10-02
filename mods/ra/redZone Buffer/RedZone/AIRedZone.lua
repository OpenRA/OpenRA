AlliedReinforcements = function()
	SendWave()
	Utils.Do(ProducedUnitTypes, ProduceUnits)
end

ProducedUnitTypes =
{
	{ factory = Warfactory,       	types = { "jeep", "jeep" } },
	{ factory = EnglandPowerTent,	types = { "e1", "e3" } },
	{ factory = WarfactoryNorth,	types = { "2tnk", "jeep", "2tnk" } },
	{ factory = EnglandPrisonTent1, types = { "e1", "e3" } },
	{ factory = EnglandPrisonTent1, types = { "e1", "e3" } },
	{ factory = EnglandNorthTent,   types = { "e1", "e3" } }
	--PrisonTent
}

SetupFactories = function()
	Utils.Do(ProducedUnitTypes, function(production)
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
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

AlliedAttackWaypoints = { PatrolPoint17, PatrolPoint5 }
SpawnPoints = { SpawnPoint2 }
ZombieEntryPoints2 = { RoadSpawn3, RoadSpawn16, RoadSpawn13, RoadSpawn14 }
SpawnPoints2 = { Rally3, Rally4, Rally5 }
ZombieEntryPoints3 = { RoadSpawn17, RoadSpawn18, RoadSpawn2, RoadSpawn1 }
SpawnPoints3 = { Rally7, Rally8, Rally9 }


--√

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end
--√



--produce ignores tech tree and available money

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
	local lazyUnits = EnglandPower.GetGroundAttackers()
	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
	local lazynorthUnits = EnglandNorthBase.GetGroundAttackers()
	Utils.Do(lazynorthUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
	local lazyprisonUnits = EnglandPrison.GetGroundAttackers()
	Utils.Do(lazyprisonUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
	local lazymainUnits = EnglandMainBase.GetGroundAttackers()
	Utils.Do(lazymainUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

TaskForceTeam1 = { "2tnk", "e1", "e3", "e3" }

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
	local units = Reinforcements.Reinforce(EnglandPower, unitTypes, { entryCell }, 40, function(a)
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
	local lazyUnits = EnglandPower.GetGroundAttackers()

	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end
--England Main Base DemoTruckActivation
EnglandMainBaseActivites = function()
	Utils.Do(ProducedEnglandTypes, ProduceEnglandUnits)
	CheckPoint1()
end
UnitTypes = { "2tnk", "jeep", "1tnk" }
ProducedEnglandTypes =
{
	{ factory = WarfactoryMain,     types = { "2tnk", "arty", "1tnk" } },
	{ factory = EnglandMainTent,	types = { "e1", "e3" } },
	{ factory = WarfactoryMain1,	types = { "1tnk", "jeep" } },
	{ factory = EnglandMainTent1, 	types = { "e1", "e3" } },
	{ factory = MainBaseTent, 		types = { "e1", "e3" } },
	{ factory = MainBaseTent1, 		types = { "e1", "e3" } }
}

SetupEnglandFactories = function()
	Utils.Do(ProducedEnglandTypes, function(production)
		Trigger.OnProduction(production.factory, function(_, a) BindActorTriggers(a) end)
	end)
end

ProduceEnglandUnits = function(t)
	local factory = t.factory
	if not factory.IsDead then
		local unitType = t.types[Utils.RandomInteger(1, #t.types + 1)]
		factory.Wait(Actor.BuildTime(unitType))
		factory.Produce(unitType)
		factory.CallFunc(function() ProduceUnits(t) end)
	end
end

AlliedWaterTransReinforcements = { "2tnk", "2tnk", "e1", "e1", "e3" }
AlliedReinforcementsInsertionPath = { TransportInsertionPoint.Location, AlliedWaterDropoffPoint.Location }

MedTank = "2tnk"

CheckPoint1 = function()
	MediumTank1 = Actor.Create(MedTank, true, { Location = SpawnPoint1.Location, Owner = EnglandMainBase })
	MediumTank2 = Actor.Create(MedTank, true, { Location = SpawnPoint1.Location, Owner = EnglandMainBase })
	MediumTank1.AttackMove(PatrolPoint17.Location)
	MediumTank2.AttackMove(PatrolPoint17.Location)
	local nukedivisionReinforements1 = 	Reinforcements.Reinforce(EnglandMainBase, { "e3", "e1", "e1", "e1", "e3" }, { SpawnPoint1.Location, PatrolPoint17.Location })
	Trigger.OnAllKilled(nukedivisionReinforements1, CheckPoint2)
	local lazyUnits = EnglandMainBase.GetGroundAttackers()
	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

CheckPoint2 = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(PatrolPoint15.Location)
	end
	nukedivisionReinforements = Reinforcements.ReinforceWithTransport(EnglandMainBase, WaterTransport,
					AlliedWaterTransReinforcements, AlliedReinforcementsInsertionPath, { TransportInsertionPoint.Location })[2]	
	EnglandPowerEntry = Actor.Create("Camera.EnglandPowerBaseEntryCam", true, { Owner = Multi0, Location = EnglandMainBaseEnterCam.Location })
	EntryBaseBuildings = { MainBaseTent, MainBaseTent1, MainBasePower, MainBasePower1, MainBaseHpad1, MainBaseHpad2 }
	Trigger.OnAllKilled(EntryBaseBuildings, CheckPoint3)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.DisplayMessage("Clear a path for the demo truck")
	end)
end

HeliReinforcersPath = { SpawnPoint3.Location, PatrolPoint18.Location }
HeliReinforcersPath1 = { MainBaseWaypoint.Location, Waypoint1.Location }

Cruiser = "ca"
CheckPoint3 = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(PatrolPoint18.Location)
	end
	Reinforcements.ReinforceWithTransport(EnglandNorthBase, Startingheli,
		EnglandReinforcements, HeliReinforcersPath, { SpawnPoint3.Location })
	Reinforcements.ReinforceWithTransport(EnglandNorthBase, Startingheli,
		EnglandReinforcements, HeliReinforcersPath1, { MainBaseWaypoint.Location })
	England2ndBaseInvasion = Actor.Create("Camera.EnglandMain2ndBasecam", true, { Owner = Multi0, Location = EnglandMain2ndBasecamera.Location })
	Cruiser1 = Actor.Create(Cruiser, true, { Location = SpawnPoint4.Location, Owner = EnglandMainBase })
	Cruiser1.AttackMove(PatrolPoint11.Location)
	local MainBaseReinf = Reinforcements.Reinforce(EnglandMainBase, { "2tnk", "e3", "e3", "e1" }, { SpawnPoint4.Location, PatrolPoint18.Location })
	England2ndBaseBuildings = { WarfactoryMain, EnglandMainTent }
	Trigger.OnAllKilled(England2ndBaseBuildings, FinalCheckPoint)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Reinforcements.ReinforceWithTransport(EnglandNorthBase, Startingheli,
			EnglandReinforcements, InsertionPathEngland, { MainBaseWaypoint.Location })
	end)
end
InsertionPathEngland1 = { SpawnPoint3.Location, Waypoint1.Location }

InsertionPathEngland = { MainBaseWaypoint.Location, ChinookWaypoint.Location }
EnglandReinforcements = { "e1", "e3", "e1", "e3", "e1", "e3" }
Startingheli = "tran"
Artillery = "arty"
FinalCheckPoint = function()
	if DemoTruck.IsInWorld then
		DemoTruck.Stop()
		DemoTruck.Move(DemoDropoffPoint.Location)
		EnglandPowerMainBaseCamera = Actor.Create("Camera.EnglandMainLargeCam", true, { Owner = Multi0, Location = EnglandMainBaseCam.Location })
	end
	Artillery3 = Actor.Create(Artillery, true, { Location = MainBaseWaypoint1.Location, Owner = EnglandMainBase })
	Artillery3.AttackMove(DemoDropoffPoint.Location)
	Reinforcements.ReinforceWithTransport(EnglandNorthBase, Startingheli,
		EnglandReinforcements, InsertionPathEngland, { MainBaseWaypoint.Location })
end

DestroyNukesTrigger = { CPos.New(105,16) }

Trigger.OnEnteredFootprint(DestroyNukesTrigger, function(a, id)
	if a.Owner == Multi0 and a.Type == "dtrk" then
		Trigger.RemoveFootprintTrigger(id)
		DemoTruckParked = true 
		Media.PlaySpeechNotification(Multi0, "ExplosiveChargePlaced")
		Media.DisplayMessage("20 seconds until self destruct!")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			timerStarted = true
			remainingTime = DateTime.Seconds(20)
			Tick = function()
				if remainingTime > 0 and timerStarted then
					UserInterface.SetMissionText("Self Destruct in " .. Utils.FormatTime(remainingTime), Multi0.Color)
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
						MissileSilo1.Kill()
						MissileSilo2.Kill()
						MissileSilo3.Kill()
						MissileSilo4.Kill()
						MissileSilo5.Kill()
						Oil1.Kill()
						Oil2.Kill()
						Oil3.Kill()
						Oil4.Kill()
						Oil5.Kill()
						Oil6.Kill()
						Oil7.Kill()
						Oil8.Kill()
						Oil9.Kill()
						Oil10.Kill()
						Oil11.Kill()
						Oil12.Kill()
						Oil13.Kill()
					end)
				end
			end
		end)
	end
end)

--Keep healthy and Take care of your body its the only one you have that exists!