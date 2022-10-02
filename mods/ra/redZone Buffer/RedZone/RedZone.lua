
APC = "apc.entryunit"
APCInsertionPath = { APCEntryPoint.Location, APCDropoffPoint.Location }
Theif = { "hijacker" }
BadgerType = { "bdgr" }
ParadropWaypoints = { PowerBaseCam }
ParadropSovietUnits = function()
	local lz = Utils.Random(ParadropWaypoints)
	local units = powerproxy.SendParatroopers(lz.CenterPosition)
	Utils.Do(units, function(a)
		local start = SpawnPoint1.CenterPosition + WVec.New(0, (i - 1) * 1536, Actor.CruiseAltitude(a))
		local dest = StartJeep.Location + CVec.New(0, 2 * i)
		local yak = Actor.Create(yakType, true, { CenterPosition = start, Owner = player, Facing = (Map.CenterOfCell(dest) - start).Facing })
		yak.Move(dest)
		yak.ReturnToBase(Airfields[i])
		i = i + 1
	end)
end
--Tick = function()
--	if Multi0.HasNoRequiredUnits() then
--		Multi0.MarkCompletedObjective(VillageRaidObjective)
--	end
--end
APCDropoff = function()
	Media.PlaySpeechNotification(Multi0, "ReinforcementsArrived")
	local passengers = Reinforcements.ReinforceWithTransport(Multi0, APC,
		Theif, APCInsertionPath, { APCExitPoint.Location })[2]
	local hijacker = passengers[1]
	Trigger.OnKilled(hijacker, PlayerKilledInAction)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		desertpatrolcam = Actor.Create("CAMERA.DesertPatrolCam", true, { Owner = Multi0, Location = DesertCam.Location })
	end)
	if Multi0.HasNoRequiredUnits() then
		MissionFailed()
	end
end
BackupHeliPath = { SpawnPoint1.Location, LZ2.Location }

BackupHeliPathStrike = { SpawnPoint1.Location, LZ.Location }
SovietReinforcements = { "e1", "e1", "e1", "e3", "e1", "e3" }
BackupHeli = function()
	Media.PlaySpeechNotification(Multi0, "ReinforcementsArrived")
		local passengers = Reinforcements.ReinforceWithTransport(Multi0, Startingheli,
			Theif, BackupHeliPath, { APCEntryPoint.Location })[2]
		local hijacker = passengers[1]
		Trigger.OnKilled(hijacker, PlayerKilledInAction)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
	local StrikeTeam = Reinforcements.ReinforceWithTransport(Multi0, Startingheli,
		SovietReinforcements, BackupHeliPathStrike, { APCEntryPoint.Location })[2]
	local HijackerReinforcements = StrikeTeam[1]	
	end)
end

--radar ping for prison
--ammo box explanation for reload
AirfieldTrigger = { CPos.New(39,99) }
TankPatrolUnits = { Tank1, Tank2, TankInfantry1, TankInfantry2 }
TankPatrolPath = { PatrolPoint3.Location, PatrolPoint2.Location, PatrolPoint5.Location }
EnglandPowerInfantryPatrolUnits = { EnglandPowerPatrolInfantry1, EnglandPowerPatrolInfantry2, EnglandPowerPatrolInfantry3 }
EnglandPowerInfantryPatrolPath = { PatrolPoint12.Location, PatrolPoint13.Location, PatrolPoint14.Location }
ShipPatrolUnits = { Destroyer2, Destroyer1 }
ShipPatrolUnits1 = { Destroyer, Destroyer5 }
ShipPath = { PatrolPoint1.Location, PatrolPoint7.Location, PatrolPoint8.Location }
HindUnit = { Hind1 }
HindUnit1 = { Hind2 }
HindUnit2 = { Hind3 }

PatrolActors = function() 
	local units = ShipPatrolUnits
	Utils.Do(units, function(patrolunit)
		ActivateShipPatrolUnit(patrolunit)
	end)
	local units = ShipPatrolUnits1
	Utils.Do(units, function(patrolunit)
		ActivateShipPatrolUnit1(patrolunit)
	end)
end
-- this fuction patrols group actors individually
ActivateShipPatrolUnit = function(a) 
	if a.IsInWorld then
		Trigger.OnIdle(a, function()
			a.AttackMove(PatrolPoint1.Location)
		end)
		if a.IsInWorld then
			Trigger.OnIdle(a, function()
			 	a.AttackMove(PatrolPoint7.Location)
			end)
			if a.IsInWorld then
				Trigger.OnIdle(a, function()
					a.AttackMove(PatrolPoint8.Location)
				end)
			end
		end
	end
end

ActivateShipPatrolUnit1 = function(a) 
	if a.IsInWorld then
		Trigger.OnIdle(a, function()
			a.AttackMove(PatrolPoint10.Location)
		end)
		if a.IsInWorld then
			Trigger.OnIdle(a, function()
			 	a.AttackMove(PatrolPoint11.Location)
			end)
		end
	end
end

RunInitialActivities = function()
	APCDropoff()
--	ParadropSovietUnits()
	Camera.Position = APCDropoffPoint.CenterPosition
	PatrolActors()
	ActivateInfantrylUnits()
--triggers
--	Trigger.OnDiscovered(Prison, RedAlert)
	Trigger.OnCapture(Tank1, EnglandPowerBaseReveal)
	Trigger.OnCapture(Tank2, EnglandPowerBaseReveal)
	Trigger.OnCapture(Hind1, Hind1Capture)
	Trigger.OnCapture(Hind2, Hind2Capture)
	Trigger.OnCapture(Hind3, Hind3Capture)
	Trigger.OnCapture(Hind1, NuclearTimerStarted)
	Trigger.OnCapture(Hind2, NuclearTimerStarted)
	Trigger.OnCapture(Hind3, NuclearTimerStarted)
	Trigger.OnKilled(Hind1, BackupHeli) 
	Trigger.OnKilled(Hind2, BackupHeli) 
	Trigger.OnKilled(Hind3, BackupHeli) 
	MissionObjectivesMain = { Prison, CommunicationsCenter }
	Trigger.OnAllKilled(MissionObjectivesMain, DemoTruckOnApproach) 
	HindHelicoptersTeam = { Hind1, Hind2, Hind3 }
	Trigger.OnAllKilled(HindHelicoptersTeam, MissionFailed) 
	Trigger.OnKilled(Prison, FreeVolkov) 
	Trigger.OnKilled(CommunicationsCenter, CommunicationsCenterDestroyed)
	Trigger.OnInfiltrated(Prison, function() 
		Trigger.AfterDelay(DateTime.Seconds(2), MissInfiltrated)
	end)
end

--RedAlert = function(discovered, discoverer)

ActivateInfantrylUnits = function() 
	GroupPatrol(EnglandPowerInfantryPatrolUnits, EnglandPowerInfantryPatrolPath, DateTime.Seconds(1))
end

--Patrols
MissionFailed = function()
	Multi0.MarkFailedObjective(SovietObjective1)
end

PlayTrack = function()
	Trigger.AfterDelay(DateTime.Seconds(0.000001), function()
		Media.PlaySound("Spinner.aud")
		Trigger.AfterDelay(DateTime.Seconds(312), function()
			PlayTrack()
		end)
	end)
end


--Mission
EnglandPowerBaseReveal = function()
	Media.DisplayMessage("Commandeer a Hind Helicopter")
	EnglandPowerBaseCamera = Actor.Create("Camera.EnglandPowerBase", true, { Owner = Multi0, Location = PowerBaseCam.Location })
end

Hind1Capture = function()
	Utils.Do(HindUnit, function(actor)
		if actor.IsDead then
			return
		end
		if not actor.HasPassengers then
			actor.Owner = Multi0Ally
		end
		if actor.HasPassengers then
			actor.Owner = Multi0
		end	
	end)
	Trigger.AfterDelay(DateTime.Seconds(0.000000000001), function()
		Hind1Capture()
	end) 
end
Hind2Capture = function()
	Utils.Do(HindUnit1, function(actor)
		if actor.IsDead then
			return
		end
		if not actor.HasPassengers then
			actor.Owner = Multi0Ally
		end
		if actor.HasPassengers then
			actor.Owner = Multi0
		end	
	end)
	Trigger.AfterDelay(DateTime.Seconds(0.000000000001), function()
		Hind2Capture()
	end)
end
Hind3Capture = function()
	Utils.Do(HindUnit2, function(actor)
		if actor.IsDead then
			return
		end
		if not actor.HasPassengers then
			actor.Owner = Multi0Ally
		end
		if actor.HasPassengers then
			actor.Owner = Multi0
		end	
	end)
	Trigger.AfterDelay(DateTime.Seconds(0.000000000001), function()
		Hind3Capture()
	end)
end

NuclearTimerStarted = function()
	if HindCaptured then
		return
	end
	AlliedReinforcements()
	Media.PlaySoundNotification(Multi0, "AlertBuzzer")
	HindCaptured = true
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.DisplayMessage("The Allies Have Initiated A Launch Sequence!", "Headquarters")
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			NukeCountdown()
			EnglandPowerMainBaseCamera = Actor.Create("Camera.EnglandMainCam", true, { Owner = Multi0, Location = EnglandMainBaseCamNukes.Location })
			PrisonCamera = Actor.Create("Camera.PrisonBaseCam", true, { Owner = Multi0, Location = PrisonCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				FreePrisoners = Multi0.AddPrimaryObjective("Destroy the Prison to Free Volkov", Multi0.Color)
				Multi0.MarkCompletedObjective(SovietObjective)
				Trigger.AfterDelay(DateTime.Seconds(20), function()
					Media.DisplayMessage("Use Ammo Depot to Repair and Reload Hind")
				end)
			end)
		end)
	end)
end

NukeCountdown = function()
	Media.PlaySpeechNotification(Multi0, "AtomBombPrepping")
	timerStarted = true
	remainingTime = DateTime.Minutes(20)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(player, "TwentyMinutesRemaining")
	end)
	Tick = function()
		if remainingTime == DateTime.Minutes(10) then
			Media.PlaySpeechNotification(player, "TenMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(5) then
			Media.PlaySpeechNotification(player, "WarningFiveMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(4) then
			Media.PlaySpeechNotification(player, "WarningFourMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(3) then
			Media.PlaySpeechNotification(player, "WarningThreeMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(2) then
			Media.PlaySpeechNotification(player, "WarningTwoMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(1) then
			Media.PlaySpeechNotification(player, "WarningOneMinuteRemaining")
		end
		if remainingTime > 0 and timerStarted then
			UserInterface.SetMissionText("Nuclear Launch In: " .. Utils.FormatTime(remainingTime), Multi0.Color)
			remainingTime = remainingTime - 1
		elseif remainingTime == 0 then
--			NukeLaunch()
			EnglandMainBase.MarkCompletedObjective(AlliedObjective)
--			UserInterface.SetMissionText("")
--			Media.PlaySpeechNotification(Multi0, "AtomBombLaunchDetected")
		end
	end
end

NukeLaunch = function()
	Media.PlaySpeechNotification(Multi0, "AtomBombLaunchDetected")
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		local delay = Utils.RandomInteger(20, 20)
		Lighting.Flash("LightningStrike", delay)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Multi0, "SovietEmpireFallen")
			Trigger.AfterDelay(DateTime.Seconds(1), function()
			EnglandMainBase.MarkCompletedObjective(AlliedObjective)
			end)
		end)
	end)
end

volkov = "gnrl"
Silo = "silo"
Hijacker = "hijacker"

MissInfiltrated = function()
	BioDestroy = Actor.Create(Silo, true, { Location = PrisonersFreed.Location, Owner = Multi0 })
	BioDestroy.Kill()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		BioDestroy = Actor.Create(Silo, true, { Location = PrisonersFreed.Location, Owner = Multi0 })
		BioDestroy.Kill()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			hijackerunit = Actor.Create(Hijacker, true, { Owner = Multi0, Location = PrisonersFreed.Location})
			VolkovUnit = Actor.Create(volkov, true, { Owner = Multi0, Location = PrisonersFreed.Location})
			hijackerunit.Scatter()
			VolkovUnit.Scatter()
			DestroyCommunications = Multi0.AddPrimaryObjective("Destroy Communication Center For Reinforcements")
			Multi0.MarkCompletedObjective(FreePrisoners)
			Trigger.AfterDelay(DateTime.Seconds(0.01), function()
				Prison.Kill()
				Media.PlaySpeechNotification(Multi0, "CommandoRescued")
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					if CommunicationsCenter.IsDead then
						return
					end
					CommsCamera = Actor.Create("Camera.CommsCenterCam", true, { Owner = Multi0, Location = NorthBaseCam.Location })
				end)
			end)
		end)
	end)
end

FreeVolkov = function()
	if Multi0.IsObjectiveCompleted(FreePrisoners) then
		return
	end
	DestroyCommunications = Multi0.AddPrimaryObjective("Destroy Communication Center For Reinforcements")
	VolkovUnit1 = Actor.Create(volkov, true, { Owner = Multi0, Location = PrisonersFreed.Location})
	VolkovUnit1.Scatter()
	Media.PlaySpeechNotification(Multi0, "CommandoRescued")
	Multi0.MarkCompletedObjective(FreePrisoners)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if CommunicationsCenter.IsDead then
			return
		end
		CommsCamera = Actor.Create("Camera.CommsCenterCam", true, { Owner = Multi0, Location = NorthBaseCam.Location })
		--if not Multi0.IsObjectiveCompleted(DestroyCommunications) then
			--Multi0.MarkFailedObjective(Destroy Allied Communications and cut off their reinforcements)
			--Media.DisplayMessage("Destroy Allied communications and cut off their reinforcements", "Mission")
		--end
	end)
end

BioFactory = "bio"
CommunicationsCenterDestroyed = function()
	if not Multi0.IsObjectiveCompleted(FreePrisoners) then
		Media.DisplayMessage("Rescue Volkov")
		return
	end
	Media.PlaySpeechNotification(Multi0, "ObjectiveMet")
	RadarExplosion = Actor.Create(BioFactory, true, { Location = Explosion1.Location, Owner = Multi0 })
	RadarExplosion.Kill()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		RadarExplosion1 = Actor.Create(BioFactory, true, { Location = Explosion3.Location, Owner = Multi0 })
		RadarExplosion1.Kill()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			RadarExplosion2 = Actor.Create(BioFactory, true, { Location = Explosion2.Location, Owner = Multi0 })
			RadarExplosion2.Kill()
	--if not Multi0.IsObjectiveCompleted(FreePrisoners) then
	--	Media.DisplayMessage("Free Volkov from prison", "Mission")
	--end
		end)
	end)
end

WaterTransport = "lst.in"
DemoTruckType = { "dtrk" }
--SovietReinforcements = { "3tnk", "3tnk", "v2rl", "e1", "e1", "e2" }
HeavyTank = "3tnk"
RifleInfantry = "e1"
DemoTruckInsertionPath = { TransportInsertionPoint.Location, TransportDropoffPoint.Location }

DemoTruckOnApproach = function()
	Multi0.MarkCompletedObjective(DestroyCommunications)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(Multi0, "ReinforcementsArrived")
		EscortObjective = Multi0.AddPrimaryObjective("Escort Demo Truck to Nuclear Silos")
		DemoTruckPassanger = Reinforcements.ReinforceWithTransport(Multi0, WaterTransport,
			DemoTruckType, DemoTruckInsertionPath, { TransportInsertionPoint.Location })[2]
		DemoTruck = DemoTruckPassanger[1]
		Trigger.OnKilled(DemoTruck, DemoDestroyed)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			EnglandNukesCam = Actor.Create("CAMERA.EnglandMainCam", true, { Owner = Multi0, Location = EnglandMainBaseCamNukes.Location })
			Trigger.AfterDelay(DateTime.Seconds(12), function()
				EnglandMainBaseActivites()
				DemolitionTruckPath()
			end)
		end)
	end)
end

--ProxyType = "powerproxy.paratroopers"
--ParadropWaypoints = { PatrolPoint16 }
--ParadropSovietUnits = function()
--	local lz = Utils.Random(ParadropWaypoints)
--	local DemoTruck = powerproxy.SendParatroopers(lz.CenterPosition)
--	Trigger.AfterDelay(DateTime.Seconds(30), function()
--		DemolitionTruckPath()
--	end)
--end

DemolitionTruckPath = function()
	DemoTruck.Move(PatrolPoint17.Location)
	if DemoTruck.IsIdle then
		DemoTruck.Move(PatrolPoint17.Location)
	end
end

NuclearSilosDestroyed = function()
	if not Multi0.IsObjectiveCompleted(DestroyCommunications) then
		Media.DisplayMessage("Rescue Volkov and Destroy Communications")
		return
	end
	Media.PlaySpeechNotification(Multi0, "ObjectiveMet")
end
WorldLoaded = function()
	Multi0 = Player.GetPlayer("Multi0")
	Neutral = Player.GetPlayer("Neutral")
	Multi0Ally = Player.GetPlayer("Multi0Ally")
	EnglandPower = Player.GetPlayer("EnglandPower")
	EnglandNorthBase = Player.GetPlayer("EnglandNorthBase")
	EnglandMainBase = Player.GetPlayer("EnglandMainBase")
	EnglandPrison = Player.GetPlayer("EnglandPrison")
	EnglandNukes = Player.GetPlayer("EnglandNukes")
--	powerproxy = Actor.Create(ProxyType, false, { Owner = Multi0Ally })
	SetupEnglandFactories()
	SetupFactories()
	RunInitialActivities()
--	Trigger.OnDiscovered(Hind1, function(a, EnglandMainBase)
--		Media.PlaySpeechNotification(Multi0, "AlliedForcesFallen")
--		Reinforcements.Reinforce(EnglandNorthBase, { "arty", "arty", "arty" }, { MainBaseWaypoint1.Location, DemoDropoffPoint.Location })
--	end)

	Trigger.OnObjectiveAdded(Multi0, function(p, id)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.DisplayMessage(p.GetObjectiveDescription(id), "Primary Objective")
		end)
	end)
	AlliedObjective = EnglandMainBase.AddPrimaryObjective("Stop Soviets", Multi0.Color)

	SovietObjective = Multi0.AddPrimaryObjective("Hijack a tank and commandeer a Hind", Multi0.Color)

	Trigger.OnObjectiveCompleted(Multi0, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective Completed")
	end)
		
	Trigger.OnObjectiveFailed(Multi0, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective Failed")
	end)
	Trigger.OnPlayerLost(Multi0, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Multi0, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(Multi0, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(Multi0, "MissionAccomplished")
		end)
	end)
end

MissionFailed = function()
	EnglandMainBase.MarkCompletedObjective(AlliedObjective)
end

WeaponsFactory = "weap"
MissionAccomplished = function()
	WeaponsFacDestroy = Actor.Create(WeaponsFactory, true, { Location = DemoDropoffPoint.Location, Owner = Multi0 })
	WeaponsFacDestroy.Kill()
	local delay = Utils.RandomInteger(20, 20)
	Lighting.Flash("LightningStrike", delay)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Multi0, "AlliedForcesFallen")
		Multi0.MarkCompletedObjective(EscortObjective)
		EnglandMainBase.MarkFailedObjective(AlliedObjective)
	end)
end

PlayerKilledInAction = function()
	Multi0.MarkFailedObjective(SovietObjective)
end

DemoDestroyed = function()
	if DemoTruckParked then
		return
	end
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Multi0, "ConvoyUnitLost")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Multi0.MarkFailedObjective(EscortObjective)
		end)
	end)
end

GroupPatrol = function(units, waypoints, delay) 
	local i = 1
	local stop = false
	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end
			if unit.Location == waypoints[i] then
				local bool = Utils.All(units, function(actor) return actor.IsIdle end)
				if bool then
					stop = true
					i = i + 1
					if i > #waypoints then
						i = 1
					end
					Trigger.AfterDelay(delay, function() stop = false end)
				end
			else
				unit.AttackMove(waypoints[i])
			end
		end)
	end)
end


