--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

PlayerInfo = { }
TankCamera =
{
	easy = "CAMERA.15_Cells",
	normal = "CAMERA.12_Cells",
	hard = "CAMERA.9_Cells"
}

AirBaseCamera =
{
	easy = "CAMERA.20_Cells",
	normal = "CAMERA.17_Cells",
	hard = "CAMERA.14_Cells"
}

PrisonBaseCamera =
{
	easy = "CAMERA.24_Cells",
	normal = "CAMERA.21_Cells",
	hard = "CAMERA.18_Cells"
}

CommunicationsCenterCamera =
{
	easy = "CAMERA.27_Cells",
	normal = "CAMERA.24_Cells",
	hard = "CAMERA.21_Cells"
}

MainBaseCamera =
{
	easy = "CAMERA.13_Cells",
	normal = "CAMERA.11_Cells",
	hard = "CAMERA.9_Cells"
}

MainBaseCameraMedium =
{
	easy = "CAMERA.17_Cells",
	normal = "CAMERA.15_Cells",
	hard = "CAMERA.13_Cells"
}

MainBaseCameraLarge =
{
	easy = "CAMERA.24_Cells",
	normal = "CAMERA.22_Cells",
	hard = "CAMERA.20_Cells"
}

MainBaseCameraEntry =
{
	easy = "CAMERA.19_Cells",
	normal = "CAMERA.17_Cells",
	hard = "CAMERA.15_Cells"
}

MainBaseCamera2 =
{
	easy = "CAMERA.18_Cells",
	normal = "CAMERA.16_Cells",
	hard = "CAMERA.14_Cells"
}

Difficulty = Map.LobbyOption("difficulty")

APCInsertionPath = { APCEntryPoint.Location, APCDropoffPoint.Location }

BackupHeliPath = { SpawnPoint1.Location, LZ2.Location }
BackupHeliPathStrike = { SpawnPoint1.Location, LZ.Location }
SovietReinforcements =
{
	easy = { "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" },
	normal = { "e1", "e1", "e1", "e1", "e3", "e3", "e3" },
	hard = { "e1", "e1", "e1", "e1", "e3", "e3" }
}

HindUnit1 = { Hind1 }
HindUnit2 = { Hind2 }
HindUnit3 = { Hind3 }

PatrolInfantry = { EnglandAirBasePatrolInfantry1, EnglandAirBasePatrolInfantry2, EnglandAirBasePatrolInfantry3 }
InfantryPath = { PatrolPoint12.Location, PatrolPoint13.Location, PatrolPoint14.Location }

PatrolDestroyers1 = { Destroyer2, Destroyer1 }
PatrolDestroyers2 = { Destroyer4, Destroyer5 }
DestroyerPath = { PatrolPoint1.Location, PatrolPoint7.Location, PatrolPoint8.Location }

NukeTimer =
{
	easy = DateTime.Minutes(30),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(20)
}

DemoTruckInsertionPath = { TransportInsertionPoint.Location, TransportDropoffPoint.Location }

APCDropoff = function()
	Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")

	local passengers = Reinforcements.ReinforceWithTransport(USSR, "apc.entryunit", { "thf" }, APCInsertionPath, { APCExitPoint.Location })[2]
	local thief = passengers[1]

	Trigger.OnKilled(thief, MissionFailed)
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		desertPatrolCam = Actor.Create(TankCamera[Difficulty], true, { Owner = USSR, Location = DesertCam.Location })
	end)

	if USSR.HasNoRequiredUnits() then
		MissionFailed()
	end
end

EnglandAirBaseReveal = function()
	if not EnglandAirBaseRevealed then
		Media.DisplayMessage("Hijack a UH60 Helicopter.")
		EnglandAirBaseCamera = Actor.Create(AirBaseCamera[Difficulty], true, { Owner = USSR, Location = AirBaseCam.Location })

		EnglandAirBaseRevealed = true
	end
end

BackupHeli = function()
	Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")

	local passengers = Reinforcements.ReinforceWithTransport(USSR, "tran", { "thf" }, BackupHeliPath, { APCEntryPoint.Location })[2]
	local thief = passengers[1]

	Trigger.OnKilled(thief, MissionFailed)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		local StrikeTeam = Reinforcements.ReinforceWithTransport(USSR, "tran", SovietReinforcements[Difficulty], BackupHeliPathStrike, { APCEntryPoint.Location })[2]
		local ThiefReinforcements = StrikeTeam[1]
	end)
end

Hind1PassengerCheck = function()
		Trigger.OnPassengerEntered(Hind1, function(transport, passenger)
			-- If someone enters a vehicle with no passengers, they're the owner.
			if transport.PassengerCount == 1 or transport.PassengerCount == 2 then
				transport.Owner = passenger.Owner
			end
			local pi = PlayerInfo[passenger.Owner.InternalName]
		
			-- Set passenger state
			pi.PassengerOfVehicle = transport
		
			-- Eject on death hack: Set the current health value when we need to eject anyone out.
			pi.EjectOnDeathHealth = passenger.Health
			-- actor.Owner = USSR
			-- Name tag hack: Setting the driver to display the proper pilot name.
			if transport.PassengerCount == 1 then
				pi.IsPilot = true
			end
		end)
		Trigger.OnPassengerExited(Hind1, function(transport, passenger)
			if not transport.IsDead and transport.PassengerCount == 0 then
				-- NOTE: With EjectOnDeath being busted, this might not be working as intended.
				transport.Owner = Neutral
			end
	
			local pi = PlayerInfo[passenger.Owner.InternalName]
	-- 	-- Set passenger state
			pi.PassengerOfVehicle = nil
		
		-- 	-- Name tag hack: Remove pilot info.
			pi.IsPilot = false
	end)
end

Hind2PassengerCheck = function()
	Trigger.OnPassengerEntered(Hind2, function(transport, passenger)
		if transport.PassengerCount == 1 then
			transport.Owner = passenger.Owner
		end
		local pi = PlayerInfo[passenger.Owner.InternalName]

		pi.PassengerOfVehicle = transport
		pi.EjectOnDeathHealth = passenger.Health
		if transport.PassengerCount == 1 then
			pi.IsPilot = true
		end
	end)
	Trigger.OnPassengerExited(Hind2, function(transport, passenger)
		if not transport.IsDead and transport.PassengerCount == 0 then
			transport.Owner = Neutral
		end

		local pi = PlayerInfo[passenger.Owner.InternalName]
		pi.PassengerOfVehicle = nil
		pi.IsPilot = false
	end)
end
			

Hind2PassengerCheck = function()
	Utils.Do(HindUnit2, function(actor2)
		if actor2.IsDead then
			return
		end
		if not actor2.HasPassengers then
			actor2.Owner = Neutral
		else
		-- if actor.HasPassengers then
			actor2.Owner = USSR
		end	
	end)

	Trigger.AfterDelay(0, Hind3PassengerCheck)
end

Hind3PassengerCheck = function()
	Utils.Do(HindUnit3, function(actor3)
		if actor3.IsDead then
			return
		end
		if not actor3.HasPassengers then
			actor3.Owner = Neutral
		end
		
		if actor3.HasPassengers then
			actor3.Owner = USSR
		end	
	end)

	Trigger.AfterDelay(0, Hind1PassengerCheck)
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

ActivatePatrolInfantry = function() 
	GroupPatrol(PatrolInfantry, InfantryPath, DateTime.Seconds(1))
end

ActivatePatrolDestroyers = function() 
	local units = PatrolDestroyers1
	Utils.Do(units, function(patrolunit)
		ActivatePatrolDestroyers1(patrolunit)
	end)
	local units = PatrolDestroyers2
	Utils.Do(units, function(patrolunit)
		ActivatePatrolDestroyers2(patrolunit)
	end)
end

ActivatePatrolDestroyers1 = function(a) 
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

ActivatePatrolDestroyers2 = function(a) 
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

NuclearTimerStarted = function()
	if HindCaptured then
		return
	end

	SendAlliedReinforcements()
	Media.PlaySoundNotification(USSR, "AlertBuzzer")

	HindCaptured = true

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.DisplayMessage("The Allies have initiated a launch sequence!", "Headquarters")
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			NukeCountdown()

			EnglandMainBaseCamera = Actor.Create(MainBaseCamera[Difficulty], true, { Owner = USSR, Location = EnglandMainBaseCamNukes.Location })

			PrisonCamera = Actor.Create(PrisonBaseCamera[Difficulty], true, { Owner = USSR, Location = PrisonCam.Location })

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				FreePrisoners = USSR.AddObjective("Destroy the Prison to free Volkov.", Primary, true)
				USSR.MarkCompletedObjective(SovietObjective)

				Trigger.AfterDelay(DateTime.Seconds(20), function()
					Media.DisplayMessage("Use Ammo Depots to repair and reload the Hind.")
				end)
			end)
		end)
	end)
end

NukeCountdown = function()
	Media.PlaySpeechNotification(USSR, "AtomBombPrepping")

	timerStarted = true
	remainingTime = NukeTimer[Difficulty]

	Tick = function()
		if remainingTime == DateTime.Minutes(20) then
			Media.PlaySpeechNotification(player, "TwentyMinutesRemaining")
		elseif remainingTime == DateTime.Minutes(10) then
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
			UserInterface.SetMissionText("Nuclear Launch In: " .. Utils.FormatTime(remainingTime), USSR.Color)
			remainingTime = remainingTime - 1
		elseif remainingTime == 0 then
--			NukeLaunch()
			England_Main.MarkCompletedObjective(AlliedObjective)
			UserInterface.SetMissionText("")
			Media.PlaySpeechNotification(USSR, "AtomBombLaunchDetected")
		end
	end
end

NukeLaunch = function()
	Media.PlaySpeechNotification(USSR, "AtomBombLaunchDetected")
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Lighting.Flash("LightningStrike", 20)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(USSR, "SovietEmpireFallen")
			Trigger.AfterDelay(DateTime.Seconds(1), function()
				England_Main.MarkCompletedObjective(AlliedObjective)
			end)
		end)
	end)
end

FreeVolkov = function()
	if USSR.IsObjectiveCompleted(FreePrisoners) then
		return
	end

	DestroyCommunications = USSR.AddObjective("Destroy the Communication Center.", Primary, true)

	local Volkov = Actor.Create("gnrl", true, { Owner = USSR, Location = PrisonersFreed.Location})

	Volkov.Scatter()

	if Difficulty == easy or Difficulty == hard then
		Volkov.GrantCondition(Difficulty)
	end

	Media.PlaySpeechNotification(USSR, "CommandoRescued")
	USSR.MarkCompletedObjective(FreePrisoners)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if CommunicationsCenter.IsDead then
			return
		end
		CommsCamera = Actor.Create(CommunicationsCenterCamera[Difficulty], true, { Owner = USSR, Location = NorthBaseCam.Location })
	end)
end

CommunicationsCenterDestroyed = function()
	if not USSR.IsObjectiveCompleted(FreePrisoners) then
		Media.DisplayMessage("Rescue Volkov.")
		return
	end

	Media.PlaySpeechNotification(USSR, "ObjectiveMet")

	Explosions = Actor.Create("bio", true, { Location = Explosion1.Location, Owner = USSR })
	Explosions.Kill()

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Explosions = Actor.Create("bio", true, { Location = Explosion3.Location, Owner = USSR })
		Explosions.Kill()

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Explosions = Actor.Create("bio", true, { Location = Explosion2.Location, Owner = USSR })
			Explosions.Kill()
		end)
	end)
end

DemoTruckOnApproach = function()
	USSR.MarkCompletedObjective(DestroyCommunications)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		EscortObjective = USSR.AddObjective("Escort the Demo Truck to Missile Silos.", Primary, true)

		DemoTruckCargo = Reinforcements.ReinforceWithTransport(USSR, "lst.in", { "dtrk" }, DemoTruckInsertionPath, { TransportInsertionPoint.Location })[2]
		DemoTruck = DemoTruckCargo[1]

		Trigger.OnKilled(DemoTruck, DemoDestroyed)

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			EnglandNukesCam = Actor.Create(MainBaseCameraMedium[Difficulty], true, { Owner = USSR, Location = EnglandMainBaseCamNukes.Location })

			Trigger.AfterDelay(DateTime.Seconds(12), function()
				DemolitionTruckPath()
				ActivateMainBase()
			end)
		end)
	end)
end

DemolitionTruckPath = function()
	DemoTruck.Move(PatrolPoint17.Location)
	if DemoTruck.IsIdle then
		DemoTruck.Move(PatrolPoint17.Location)
	end
end

MissionFailed = function()
	England_Main.MarkCompletedObjective(AlliedObjective)
end

DemoDestroyed = function()
	if DemoTruckParked then
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(USSR, "ConvoyUnitLost")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			USSR.MarkFailedObjective(EscortObjective)
		end)
	end)
end

MissionAccomplished = function()
	Explosions = Actor.Create("weap", true, { Location = DemoDropoffPoint.Location, Owner = USSR })
	Explosions.Kill()

	Lighting.Flash("LightningStrike", 20)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(USSR, "AlliedForcesFallen")
		USSR.MarkCompletedObjective(EscortObjective)
		USSR.MarkCompletedObjective(NukeObjective)
		England_Main.MarkFailedObjective(AlliedObjective)
	end)
end

RunInitialActivities = function()
	APCDropoff()

	Camera.Position = APCDropoffPoint.CenterPosition

	ActivatePatrolDestroyers()
	ActivatePatrolInfantry()

	Trigger.OnCapture(Tank1, EnglandAirBaseReveal)
	Trigger.OnCapture(Tank2, EnglandAirBaseReveal)

	-- Trigger.OnPassengerEntered(Hind1, Hind1PassengerCheck)
	Trigger.OnCapture(Hind1, Hind1PassengerCheck)
	Trigger.OnCapture(Hind2, Hind2PassengerCheck)
	Trigger.OnCapture(Hind3, Hind3PassengerCheck)

	Trigger.OnCapture(Hind1, NuclearTimerStarted)
	Trigger.OnCapture(Hind2, NuclearTimerStarted)
	Trigger.OnCapture(Hind3, NuclearTimerStarted)

	Trigger.OnKilled(Hind1, BackupHeli)
	Trigger.OnKilled(Hind2, BackupHeli)
	Trigger.OnKilled(Hind3, BackupHeli)

	if Difficulty == easy or Difficulty == hard then
		Hind1.GrantCondition(Difficulty)
		Hind2.GrantCondition(Difficulty)
		Hind3.GrantCondition(Difficulty)
	end

	MissionObjectivesMain = { Prison, CommunicationsCenter }
	Trigger.OnAllKilled(MissionObjectivesMain, DemoTruckOnApproach) 

	HindHelicoptersTeam = { Hind1, Hind2, Hind3 }
	Trigger.OnAllKilled(HindHelicoptersTeam, MissionFailed)

	Trigger.OnKilled(Prison, FreeVolkov)
	Trigger.OnKilled(CommunicationsCenter, CommunicationsCenterDestroyed)
end

PlayerIsTeamAi = function(player)
	return player.InternalName == AlphaTeamPlayerName or player.InternalName == BravoTeamPlayerName
end

PlayerIsHumanOrBot = function(player)
	return player.IsNonCombatant == false and PlayerIsTeamAi(player) == false
end

WorldLoaded = function()
	local teamPlayers = Player.GetPlayers(function(p)
		return PlayerIsHumanOrBot(p)
	end)

	USSR = Player.GetPlayer("USSR")
	Bad_Guy = Player.GetPlayer("BadGuy")
	ForEnglandJames = Player.GetPlayer("England Missile Silos")
	Neutral = Player.GetPlayer("Neutral")
	England_Air = Player.GetPlayer("England Air Base")
	England_North = Player.GetPlayer("England Northern Base")
	England_Main = Player.GetPlayer("England Main Base")
	England_Prison = Player.GetPlayer("England Prison Base")
	england_nuke = Player.GetPlayer("England Missile Silos")

	Utils.Do(teamPlayers, function(p)
		
		PlayerInfo[p.InternalName] =
			{
				IsPilot = false, 
			}

		if p.IsLocalPlayer then	LocalPlayerInfo = PlayerInfo[p.InternalName] end
	end)

	RunInitialActivities()

	SetupFactories()
	SetupMainFactories()

	Trigger.OnObjectiveAdded(USSR, function(p, id)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.DisplayMessage(p.GetObjectiveDescription(id), "Primary Objective")
		end)
	end)

	AlliedObjective = England_Main.AddObjective("Don't let soviets to destroy the Missile Silos.", Primary, true)
	SovietObjective = USSR.AddObjective("Hijack a Hind Helicopter.", Primary, true)
	NukeObjective = USSR.AddObjective("Destroy the Missile Silos.", Primary, true)

	Trigger.OnObjectiveCompleted(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective Completed")
	end)

	Trigger.OnObjectiveFailed(USSR, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective Failed")
	end)

	Trigger.OnPlayerLost(USSR, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(USSR, "MissionFailed")
		end)
	end)

	Trigger.OnPlayerWon(USSR, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(USSR, "MissionAccomplished")
		end)
	end)
end