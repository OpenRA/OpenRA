--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedUnits =
{
	{ delay = 0, types = { "1tnk", "1tnk", "2tnk", "2tnk" } },
	{ delay = DateTime.Seconds(3), types = { "e1", "e1", "e1", "e3", "e3" } },
	{ delay = DateTime.Seconds(7), types = { "e6", "e6", "thf" } }
}
ReinforceBaseUnits = { "1tnk", "1tnk", "2tnk", "arty", "arty" }
CivilianEvacuees = { "c2", "c3", "c5", "c6", "c8" }
USSROutpostFlameTurrets = { FlameTurret1, FlameTurret2 }
ExplosiveBarrels = { ExplosiveBarrel1, ExplosiveBarrel2 }
SuperTanks = { stnk1, stnk2, stnk3 }
SuperTankMoveWaypoints = { HospitalSuperTankPoint, AlliedBaseBottomRight, DemitriTriggerAreaCenter, DemitriLZ }
SuperTankMove = 1
SuperTankHuntWaypoints = { SuperTankHuntWaypoint1, SuperTankHuntWaypoint2, SuperTankHuntWaypoint3, SuperTankHuntWaypoint4 }
SuperTankHunt = 1
SuperTankHuntCounter = 1
ExtractionHeli = "tran"
ExtractionWaypoint = CPos.New(DemitriLZ.Location.X, 19)
ExtractionLZ = DemitriLZ.Location
BeachTrigger = { CPos.New(19, 44), CPos.New(20, 44), CPos.New(21, 44), CPos.New(22, 44), CPos.New(22, 45), CPos.New(23, 45), CPos.New(22, 44), CPos.New(24, 45), CPos.New(24, 46), CPos.New(24, 47), CPos.New(25, 47), CPos.New(25, 48) }
DemitriAreaTrigger = { CPos.New(32, 98), CPos.New(32, 99), CPos.New(33, 99), CPos.New(33, 100), CPos.New(33, 101), CPos.New(33, 102), CPos.New(32, 102), CPos.New(32, 103) }
HospitalAreaTrigger = { CPos.New(43, 41), CPos.New(44, 41), CPos.New(45, 41), CPos.New(46, 41), CPos.New(46, 42), CPos.New(46, 43), CPos.New(46, 44), CPos.New(46, 45), CPos.New(46, 46), CPos.New(45, 46), CPos.New(44, 46), CPos.New(43, 46) }

EvacuateCivilians = function()
	local evacuees = Reinforcements.Reinforce(neutral, CivilianEvacuees, { HospitalCivilianSpawnPoint.Location }, 0)

	Trigger.OnAnyKilled(evacuees, function()
		player.MarkFailedObjective(RescueCivilians)
	end)
	Trigger.OnAllRemovedFromWorld(evacuees, function()
		player.MarkCompletedObjective(RescueCivilians)
	end)

	Utils.Do(evacuees, function(civ)
		Trigger.OnIdle(civ, function()
			if civ.Location == AlliedBaseEntryPoint.Location then
				civ.Destroy()
			else
				civ.Move(AlliedBaseEntryPoint.Location)
			end
		end)
	end)
end

SpawnAndMoveAlliedBaseUnits = function()
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	Reinforcements.Reinforce(player, ReinforceBaseUnits, { AlliedBaseEntryPoint.Location, AlliedBaseMovePoint.Location }, 18)
end

SetupAlliedBase = function()
	local alliedOutpost = Map.ActorsInBox(AlliedBaseTopLeft.CenterPosition, AlliedBaseBottomRight.CenterPosition,
		function(self) return self.Owner == outpost end)

	Media.PlaySoundNotification(player, "BaseSetup")
	Utils.Do(alliedOutpost, function(building)
		building.Owner = player
	end)

	AlliedBaseHarv.Owner = player
	AlliedBaseHarv.FindResources()

	FindDemitri = player.AddObjective("Find Dr. Demitri. He is likely hiding in the village\n to the far south.")
	InfiltrateRadarDome = player.AddObjective("Reprogram the super tanks by sending a spy into\n the Soviet radar dome.")
	DefendOutpost = player.AddObjective("Defend and repair our outpost.", "Secondary", false)
	player.MarkCompletedObjective(FindOutpost)

	-- Don't fail the Objective instantly
	Trigger.AfterDelay(DateTime.Seconds(1), function()

		-- The actor might have been destroyed/crushed in this one second delay
		local actors = Utils.Where(alliedOutpost, function(actor) return actor.IsInWorld end)
		Trigger.OnAllRemovedFromWorld(actors, function() player.MarkFailedObjective(DefendOutpost) end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), function()
		if not SuperTankDomeIsInfiltrated then
			SuperTankAttack = true
			Utils.Do(SuperTanks, function(tnk)
				if not tnk.IsDead then
					Trigger.ClearAll(tnk)
					Trigger.AfterDelay(0, function()
						Trigger.OnIdle(tnk, function()
							if SuperTankAttack then
								if tnk.Location == SuperTankMoveWaypoints[SuperTankMove].Location then
									SuperTankMove = SuperTankMove + 1
									if SuperTankMove == 5 then
										SuperTankAttack = false
									end
								else
									tnk.AttackMove(SuperTankMoveWaypoints[SuperTankMove].Location, 2)
								end
							end
						end)
					end)
				end
			end)
		end
	end)
end

SendAlliedUnits = function()
	AddObjectives()

	Camera.Position = StartEntryPoint.CenterPosition

	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	Utils.Do(AlliedUnits, function(table)
		Trigger.AfterDelay(table.delay, function()
			local units = Reinforcements.Reinforce(player, table.types, { StartEntryPoint.Location, StartMovePoint.Location }, 18)

			Utils.Do(units, function(unit)
				if unit.Type == "e6" then
					Engineer = unit
					Trigger.OnKilled(unit, LandingPossible)
				elseif unit.Type == "thf" then
					Trigger.OnKilled(unit, function()
						player.MarkFailedObjective(StealMoney)
					end)
				end
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function() InitialUnitsArrived = true end)
end

LandingPossible = function()
	if not beachReached and (USSRSpen.IsDead or Engineer.IsDead) and LstProduced < 1 then
		player.MarkFailedObjective(CrossRiver)
	end
end

SuperTankDomeInfiltrated = function()
	SuperTankAttack = true
	Utils.Do(SuperTanks, function(tnk)
		tnk.Owner = friendlyMadTanks
		if not tnk.IsDead then
			tnk.GrantCondition("friendly")
			Trigger.ClearAll(tnk)
			tnk.Stop()
			if tnk.Location.Y > 61 then
				SuperTankHunt = 4
				SuperTankHuntCounter = -1
			end
			Trigger.AfterDelay(0, function()
				Trigger.OnIdle(tnk, function()
					if SuperTankAttack then
						if tnk.Location == SuperTankHuntWaypoints[SuperTankHunt].Location then
							SuperTankHunt = SuperTankHunt + SuperTankHuntCounter
							if SuperTankHunt == 0 or SuperTankHunt == 5 then
								SuperTankAttack = false
							end
						else
							tnk.AttackMove(SuperTankHuntWaypoints[SuperTankHunt].Location, 2)
						end
					else
						tnk.Hunt()
					end
				end)
			end)
		end
	end)

	player.MarkCompletedObjective(InfiltrateRadarDome)
	Trigger.AfterDelay(DateTime.Minutes(3), SuperTanksDestruction)
	ticked = DateTime.Minutes(3)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")

		Trigger.AfterDelay(DateTime.Seconds(4), function()
			Media.DisplayMessage("In 3 minutes the super tanks will self-destruct.")
			Media.PlaySpeechNotification(player, "WarningThreeMinutesRemaining")
		end)
	end)
end

SuperTanksDestruction = function()
	local badGuys = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == badguy and self.HasProperty("Health") end)
	Utils.Do(badGuys, function(unit)
		unit.Kill()
	end)

	Utils.Do(SuperTanks, function(tnk)
		if not tnk.IsDead then
			local camera = Actor.Create("camera", true, { Owner = player, Location = tnk.Location })
			Trigger.AfterDelay(DateTime.Seconds(3), camera.Destroy)

			Trigger.ClearAll(tnk)
			tnk.Kill()
		end
	end)

	player.MarkCompletedObjective(DefendOutpost)
end

CreateDemitri = function()
	local demitri = Actor.Create("demitri", true, { Owner = player, Location = DemitriChurchSpawnPoint.Location })
	demitri.Move(DemitriTriggerAreaCenter.Location)

	Media.PlaySpeechNotification(player, "TargetFreed")
	EvacuateDemitri = player.AddObjective("Evacuate Dr. Demitri with the helicopter waiting\n at our outpost.")
	player.MarkCompletedObjective(FindDemitri)

	local flarepos = CPos.New(DemitriLZ.Location.X, DemitriLZ.Location.Y - 1)
	local demitriLZFlare = Actor.Create("flare", true, { Owner = player, Location = flarepos })
	Trigger.AfterDelay(DateTime.Seconds(3), function() Media.PlaySpeechNotification(player, "SignalFlareNorth") end)

	local demitriChinook = Reinforcements.ReinforceWithTransport(player, ExtractionHeli, nil, { ExtractionWaypoint, ExtractionLZ })[1]

	Trigger.OnAnyKilled({ demitri, demitriChinook }, function()
		player.MarkFailedObjective(EvacuateDemitri)
	end)

	Trigger.OnRemovedFromWorld(demitriChinook, function()
		if not demitriChinook.IsDead then
			Media.PlaySpeechNotification(player, "TargetRescued")
			Trigger.AfterDelay(DateTime.Seconds(1), function() player.MarkCompletedObjective(EvacuateDemitri) end)
			Trigger.AfterDelay(DateTime.Seconds(3), SpawnAndMoveAlliedBaseUnits)
		end
	end)
	Trigger.OnRemovedFromWorld(demitri, function()
		if not demitriChinook.IsDead and demitriChinook.HasPassengers then
			demitriChinook.Move(ExtractionWaypoint + CVec.New(0, -1))
			demitriChinook.Destroy()
			demitriLZFlare.Destroy()
		end
	end)
end

ticked = -1
Tick = function()
	ussr.Resources = ussr.Resources - (0.01 * ussr.ResourceCapacity / 25)

	if InitialUnitsArrived then -- don't fail the mission straight at the beginning
		if not DemitriFound or not SuperTankDomeIsInfiltrated then
			if player.HasNoRequiredUnits() then
				player.MarkFailedObjective(EliminateSuperTanks)
			end
		end
	end

	if ticked > 0 then
		UserInterface.SetMissionText("The super tanks self-destruct in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 then
		FinishTimer()
		ticked = ticked - 1
	end
end

FinishTimer = function()
	for i = 0, 9, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("The super tanks are destroyed!", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), function() UserInterface.SetMissionText("") end)
end

SetupMission = function()
	TestCamera = Actor.Create("camera" ,true , { Owner = player, Location = ProvingGroundsCameraPoint.Location })
	Camera.Position = ProvingGroundsCameraPoint.CenterPosition
	TimerColor = player.Color

	Trigger.AfterDelay(DateTime.Seconds(12), function()
		Media.PlaySpeechNotification(player, "StartGame")
		Trigger.AfterDelay(DateTime.Seconds(2), SendAlliedUnits)
	end)
end

InitPlayers = function()
	player = Player.GetPlayer("Greece")
	neutral = Player.GetPlayer("Neutral")
	outpost = Player.GetPlayer("Outpost")
	badguy = Player.GetPlayer("BadGuy")
	ussr = Player.GetPlayer("USSR")
	ukraine = Player.GetPlayer("Ukraine")
	turkey = Player.GetPlayer("Turkey")
	friendlyMadTanks = Player.GetPlayer("FriendlyMadTanks")

	ussr.Cash = 2000
	Trigger.AfterDelay(0, function() badguy.Resources = badguy.ResourceCapacity * 0.75 end)
end

AddObjectives = function()
	InitObjectives(player)

	EliminateSuperTanks = player.AddObjective("Eliminate these super tanks.")
	StealMoney = player.AddObjective("Steal money from the nearby outpost with the Thief.")
	CrossRiver = player.AddObjective("Secure transport to the mainland.")
	FindOutpost = player.AddObjective("Find our outpost and start repairs on it.")
	RescueCivilians = player.AddObjective("Evacuate all civilians from the hospital.", "Secondary", false)
	BadGuyObj = badguy.AddObjective("Deny the destruction of the super tanks.")
	USSRObj = ussr.AddObjective("Deny the destruction of the super tanks.")
	UkraineObj = ukraine.AddObjective("Survive.")
	TurkeyObj = turkey.AddObjective("Destroy.")

	Trigger.OnPlayerLost(player, function()
		ussr.MarkCompletedObjective(USSRObj)
		badguy.MarkCompletedObjective(BadGuyObj)
		ukraine.MarkCompletedObjective(UkraineObj)
		turkey.MarkCompletedObjective(TurkeyObj)
	end)

	Trigger.OnPlayerWon(player, function()
		Media.DisplayMessage("Dr. Demitri has been extracted and the super tanks have been dealt with.")
		ussr.MarkFailedObjective(USSRObj)
		badguy.MarkFailedObjective(BadGuyObj)
		ukraine.MarkFailedObjective(UkraineObj)
		turkey.MarkFailedObjective(TurkeyObj)
	end)
end

InitTriggers = function()
	Trigger.OnAllKilled(SuperTanks, function()
		Trigger.AfterDelay(DateTime.Seconds(3), function() player.MarkCompletedObjective(EliminateSuperTanks) end)
	end)

	Trigger.OnKilled(SuperTankDome, function()
		if not SuperTankDomeIsInfiltrated then
			player.MarkFailedObjective(InfiltrateRadarDome)
		end
	end)
	Trigger.OnInfiltrated(SuperTankDome, function()
		if not SuperTankDomeIsInfiltrated then
			SuperTankDomeIsInfiltrated = true
			SuperTankDomeInfiltrated()
		end
	end)
	Trigger.OnCapture(SuperTankDome, function()
		if not SuperTankDomeIsInfiltrated then
			SuperTankDomeIsInfiltrated = true
			SuperTankDomeInfiltrated()
		end
	end)

	Trigger.OnKilled(UkraineBarrel, function()
		if not UkraineBuilding.IsDead then UkraineBuilding.Kill() end
	end)

	Trigger.OnAnyKilled(USSROutpostFlameTurrets, function()
		Utils.Do(ExplosiveBarrels, function(barrel)
			if not barrel.IsDead then barrel.Kill() end
		end)
	end)

	Trigger.OnKilled(DemitriChurch, function()
		if not DemitriFound then
			player.MarkFailedObjective(FindDemitri)
		end
	end)

	Trigger.OnKilled(Hospital, function()
		if not HospitalEvacuated then
			HospitalEvacuated = true
			player.MarkFailedObjective(RescueCivilians)
		end
	end)

	Trigger.OnInfiltrated(USSROutpostSilo, function()
		MoneyStolen = true
		player.MarkCompletedObjective(StealMoney)
	end)

	Trigger.OnKilledOrCaptured(USSROutpostSilo, function()
		if not MoneyStolen then
			player.MarkFailedObjective(StealMoney)
		end
	end)

	beachReached = false
	Trigger.OnEnteredFootprint(BeachTrigger, function(a, id)
		if not beachReached and a.Owner == player then
			beachReached = true
			Trigger.RemoveFootprintTrigger(id)
			player.MarkCompletedObjective(CrossRiver)
		end
	end)

	Trigger.OnPlayerDiscovered(outpost, function(_, discoverer)
		if not outpostReached and discoverer == player then
			outpostReached = true
			SetupAlliedBase()
		end
	end)

	Trigger.OnEnteredFootprint(DemitriAreaTrigger, function(a, id)
		if not DemitriFound and a.Owner == player then
			DemitriFound = true
			Trigger.RemoveFootprintTrigger(id)
			CreateDemitri()
		end
	end)

	Trigger.OnEnteredFootprint(HospitalAreaTrigger, function(a, id)
		if not HospitalEvacuated and a.Owner == player then
			HospitalEvacuated = true
			Trigger.RemoveFootprintTrigger(id)
			EvacuateCivilians()
		end
	end)

	local tanksLeft = 0
	Trigger.OnExitedProximityTrigger(ProvingGroundsCameraPoint.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Type == "5tnk" then
			tanksLeft = tanksLeft + 1
			if tanksLeft == 3 then
				if TestCamera.IsInWorld then TestCamera.Destroy() end
				Trigger.RemoveProximityTrigger(id)
			end
		end
	end)

	LstProduced = 0
	Trigger.OnKilled(USSRSpen, LandingPossible)
	Trigger.OnSold(USSRSpen, LandingPossible)
	Trigger.OnProduction(USSRSpen, function(self, produced)
		if produced.Type == "lst" then
			LstProduced = LstProduced + 1
			Trigger.OnKilled(produced, function()
				LstProduced = LstProduced - 1
				LandingPossible()
			end)
		end
	end)
end

WorldLoaded = function()
	InitPlayers()
	InitTriggers()
	SetupMission()
end
