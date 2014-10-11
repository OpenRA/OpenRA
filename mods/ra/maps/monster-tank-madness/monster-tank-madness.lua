AlliedUnits =
{
	{ 0, { "1tnk", "1tnk", "2tnk", "2tnk" } },
	{ Utils.Seconds(3), { "e1", "e1", "e1", "e3", "e3" } },
	{ Utils.Seconds(7), { "e6" } }
}
ReinforceBaseUnits = { "1tnk", "1tnk", "2tnk", "arty", "arty" }
CivilianEvacuees = { "c1", "c2", "c5", "c7", "c8" }
USSROutpostFlameTurrets = { FlameTurret1, FlameTurret2 }
ExplosiveBarrels = { ExplosiveBarrel1, ExplosiveBarrel2 }
SuperTanks = { stnk1, stnk2, stnk3 }
SuperTankMoveWaypoints = { HospitalSuperTankPoint, AlliedBaseBottomRight, DemitriTriggerAreaCenter, DemitriLZ }
SuperTankMove = 1
SuperTankHuntWaypoints = { SuperTankHuntWaypoint1, SuperTankHuntWaypoint2, SuperTankHuntWaypoint3, SuperTankHuntWaypoint4 }
SuperTankHunt = 1
SuperTankHuntCounter = 1
ExtractionHeli = "tran"
ExtractionWaypoint = CPos.New(DemitriLZ.Location.X, 0)
ExtractionLZ = DemitriLZ.Location
BeachTrigger = { CPos.New(19, 44), CPos.New(20, 44), CPos.New(21, 44), CPos.New(22, 44), CPos.New(22, 45), CPos.New(23, 45), CPos.New(22, 44), CPos.New(24, 45), CPos.New(24, 46), CPos.New(24, 47), CPos.New(25, 47), CPos.New(25, 48) }
SetupAlliedBaseTrigger = { CPos.New(19, 33),  CPos.New(20, 33),  CPos.New(21, 33),  CPos.New(22, 33),  CPos.New(23, 33),  CPos.New(24, 33),  CPos.New(25, 33),  CPos.New(26, 33),  CPos.New(27, 33),  CPos.New(28, 33) }
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

	FindDemitri = player.AddPrimaryObjective("Find Dr. Demitri. He is likely hiding in the village\n to the far south.")
	InfiltrateRadarDome = player.AddPrimaryObjective("Reprogram the super tanks by sending a spy into\n the Soviet radar dome.")
	DefendOutpost = player.AddSecondaryObjective("Defend and repair our outpost.")
	player.MarkCompletedObjective(FindOutpost)

	Trigger.AfterDelay(Utils.Seconds(1), function() -- don't fail the Objective instantly
		Trigger.OnAllRemovedFromWorld(alliedOutpost, function() player.MarkFailedObjective(DefendOutpost) end)
	end)

	Trigger.AfterDelay(Utils.Minutes(1) + Utils.Seconds(40), function()
		if not SuperTankDomeIsInfiltrated then
			SuperTankAttack = true
			Utils.Do(SuperTanks, function(tnk)
				if not tnk.IsDead then
					Trigger.ClearAll(tnk)
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
				end
			end)
		end
	end)
end

SendAlliedUnits = function()
	Camera.Position = StartEntryPoint.CenterPosition
	Actor.Create("camera" ,true , { Owner = player, Location = ProvingGroundsCameraPoint.Location })
	Actor.Create("camera" ,true , { Owner = ussr, Location = USSRSpen.Location })

	Trigger.AfterDelay(Utils.Seconds(1), function() Media.PlaySpeechNotification(player, "ReinforcementsArrived") end)
		--To avoid overlapping "battlecontrol initialized" and "reinforcements have arrived"
	Utils.Do(AlliedUnits, function(table)
		Trigger.AfterDelay(table[1], function()
			Reinforcements.Reinforce(player, table[2], { StartEntryPoint.Location, StartMovePoint.Location }, 18)
		end)
	end)

	Trigger.AfterDelay(Utils.Seconds(1), function() InitialUnitsArrived = true end)
end

SuperTankDomeInfiltrated = function()
	turkey.SetStance(player, "Ally")
	turkey.SetStance(neutral, "Ally")

	SuperTankAttack = true
	Utils.Do(SuperTanks, function(tnk)
		if not tnk.IsDead then
			Trigger.ClearAll(tnk)
			tnk.Stop()
			if tnk.Location.Y > 61 then
				SuperTankHunt = 4
				SuperTankHuntCounter = -1
			end
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
		end
	end)

	player.MarkCompletedObjective(InfiltrateRadarDome)
	Trigger.AfterDelay(Utils.Minutes(3), SuperTanksDestruction)

	Trigger.AfterDelay(Utils.Seconds(2), function()
		Media.PlaySpeechNotification(player, "ControlCenterDeactivated")

		Trigger.AfterDelay(Utils.Seconds(3), function()
			Media.DisplayMessage("In 3 minutes the super tanks will self destruct.")
			Media.PlaySpeechNotification(player, "WarningThreeMinutesRemaining")
		end)
	end)
end

SuperTanksDestruction = function()
	local badGuys = Map.ActorsInBox(Map.TopLeft, Map.BottomRight,
		function(self) return self.Owner == badguy and self.HasProperty("Health") end)

	Utils.Do(badGuys, function(unit)
		unit.Kill()
	end)

	Utils.Do(SuperTanks, function(tnk)
		if not tnk.IsDead then
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
	EvacuateDemitri = player.AddPrimaryObjective("Evacuate Dr. Demitri with the helicopter waiting\n at our outpost.")
	player.MarkCompletedObjective(FindDemitri)

	local flarepos = CPos.New(DemitriLZ.Location.X, DemitriLZ.Location.Y - 1)
	local demitriLZFlare = Actor.Create("flare", true, { Owner = player, Location = flarepos })
	Trigger.AfterDelay(Utils.Seconds(3), function() Media.PlaySpeechNotification(player, "SignalFlareNorth") end)

	local demitriChinook = Reinforcements.ReinforceWithTransport(player, ExtractionHeli, nil, { ExtractionWaypoint, ExtractionLZ })[1]

	Trigger.OnAnyKilled({ demitri, demitriChinook }, function()
		player.MarkFailedObjective(EvacuateDemitri)
	end)

	Trigger.OnRemovedFromWorld(demitriChinook, function()
		if not demitriChinook.IsDead then
			Media.PlaySpeechNotification(player, "TargetRescued")
			Trigger.AfterDelay(Utils.Seconds(1), function() player.MarkCompletedObjective(EvacuateDemitri) end)
			Trigger.AfterDelay(Utils.Seconds(3), SpawnAndMoveAlliedBaseUnits)
		end
	end)
	Trigger.OnRemovedFromWorld(demitri, function()
		if not demitriChinook.IsDead and demitriChinook.HasPassengers then
			demitriChinook.Move(ExtractionWaypoint)
			Trigger.OnIdle(demitriChinook, demitriChinook.Destroy)
			demitriLZFlare.Destroy()
		end
	end)
end

Tick = function()
	ussr.Resources = ussr.Resources - (0.01 * ussr.ResourceCapacity / 25)

	if InitialUnitsArrived then -- don't fail the mission straight at the beginning
		if not DemitriFound or not SuperTankDomeIsInfiltrated then
			if player.HasNoRequiredUnits() then
				player.MarkFailedObjective(EliminateSuperTanks)
			end
		end
	end
end

InitPlayers = function()
	player = Player.GetPlayer("Greece")
	neutral = Player.GetPlayer("Neutral")
	outpost = Player.GetPlayer("Outpost")
	badguy = Player.GetPlayer("BadGuy")
	ussr = Player.GetPlayer("USSR")
	ukraine = Player.GetPlayer("Ukraine")
	turkey = Player.GetPlayer("Turkey")

	player.Cash = 0
	ussr.Cash = 2000
	--badguy.Resources = badguy.ResourceCapacity -- doesn't work, workaround below
	Trigger.OnCapture(Actor479, function()
		player.Cash = player.Cash + Utils.RandomInteger(1200, 1300)
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	EliminateSuperTanks = player.AddPrimaryObjective("Eliminate these super tanks.")
	CrossRiver = player.AddPrimaryObjective("Find a way to transport your forces to the mainland")
	FindOutpost = player.AddPrimaryObjective("Find our outpost and start repairs on it.")
	RescueCivilians = player.AddSecondaryObjective("Evacuate all civilians from the hospital.")
	BadGuyObj = badguy.AddPrimaryObjective("Deny the destruction of the super tanks.")
	USSRObj = ussr.AddPrimaryObjective("Deny the destruction of the super tanks.")
	UkraineObj = ukraine.AddPrimaryObjective("Survive.")
	TurkeyObj = turkey.AddPrimaryObjective("Destroy.")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")

		ussr.MarkCompletedObjective(USSRObj)
		badguy.MarkCompletedObjective(BadGuyObj)
		ukraine.MarkCompletedObjective(UkraineObj)
		turkey.MarkCompletedObjective(TurkeyObj)
	end)
	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Media.DisplayMessage("Dr. Demitri has been extracted and the super tanks have been dealt with.")

		ussr.MarkFailedObjective(USSRObj)
		badguy.MarkFailedObjective(BadGuyObj)
		ukraine.MarkFailedObjective(UkraineObj)
		turkey.MarkFailedObjective(TurkeyObj)
	end)
end

InitTriggers = function()
	Trigger.OnAllKilled(SuperTanks, function()
		Trigger.AfterDelay(Utils.Seconds(3), function() player.MarkCompletedObjective(EliminateSuperTanks) end)
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

	beachReached = false
	Trigger.OnEnteredFootprint(BeachTrigger, function(a, id)
		if not beachReached and a.Owner == player then
			beachReached = true
			Trigger.RemoveFootprintTrigger(id)
			player.MarkCompletedObjective(CrossRiver)
		end
	end)

	Trigger.OnEnteredFootprint(SetupAlliedBaseTrigger, function(a, id)
		if not outpostReached and a.Owner == player then
			outpostReached = true
			Trigger.RemoveFootprintTrigger(id)
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
end

WorldLoaded = function()

	InitPlayers()
	InitObjectives()
	InitTriggers()

	SendAlliedUnits()
end

