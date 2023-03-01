--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

TanyaFreed = false
TruckStolen = false
SovietImportantGuys = { Officer3, Officer2, Officer1, Scientist1, Scientist2 }
Camera1Towers = { FlameTower2, FlameTower3 }
TruckExit = { TruckDrive1, TruckDrive2 }
TruckEntry = { TruckDrive3.Location, TruckDrive4.Location, TruckDrive5.Location }
TruckType = { "truk" }
DogCrew = { DogCrew1, DogCrew2, DogCrew3 }
SarinVictims = { SarinVictim1, SarinVictim2, SarinVictim3, SarinVictim4, SarinVictim5, SarinVictim6, SarinVictim7, SarinVictim8, SarinVictim9, SarinVictim10, SarinVictim11, SarinVictim12, SarinVictim13 }
Camera2Team = { Camera2Rifle1, Camera2Rifle2, Camera2Rifle3, Camera2Gren1, Camera2Gren2 }
PrisonAlarm = { CPos.New(55,75), CPos.New(55,76), CPos.New(55,77), CPos.New(55,81), CPos.New(55,82), CPos.New(55,83), CPos.New(60,77), CPos.New(60,81), CPos.New(60,82), CPos.New(60,83) }
GuardDogs = { PrisonDog1, PrisonDog2, PrisonDog3, PrisonDog4 }
TanyaTowers = { FlameTowerTanya1, FlameTowerTanya2 }
ExecutionArea = { CPos.New(91, 70), CPos.New(92, 70), CPos.New(93, 70), CPos.New(94, 70) }
FiringSquad = { FiringSquad1, FiringSquad2, FiringSquad3, FiringSquad4, FiringSquad5, Officer2 }
DemoTeam = { DemoDog, DemoRifle1, DemoRifle2, DemoRifle3, DemoRifle4, DemoFlame }
DemoTruckPath = { DemoDrive1, DemoDrive2, DemoDrive3, DemoDrive4 }
WinTriggerArea = { CPos.New(111, 59), CPos.New(111, 60), CPos.New(111, 61), CPos.New(111, 62), CPos.New(111, 63), CPos.New(111, 64), CPos.New(111, 65) }

ObjectiveTriggers = function()
	Trigger.OnEnteredFootprint(WinTriggerArea, function(a, id)
		if not EscapeGoalTrigger and a.Owner == Greece then
			EscapeGoalTrigger = true

			Greece.MarkCompletedObjective(ExitBase)
			if Difficulty == "hard" then
				Greece.MarkCompletedObjective(NoCasualties)
			end

			if not TanyaFreed then
				Greece.MarkFailedObjective(FreeTanyaObjective)
			else
				Greece.MarkCompletedObjective(FreeTanyaObjective)
			end
		end
	end)

	Trigger.OnKilled(Tanya, function()
		Greece.MarkFailedObjective(FreeTanyaObjective)
	end)

	Trigger.OnAllKilled(TanyaTowers, function()
		TanyaFreed = true
		if not Tanya.IsDead then
			Media.PlaySpeechNotification(Greece, "TanyaRescued")
			Tanya.Owner = Greece
		end
	end)

	Trigger.OnAllKilled(SovietImportantGuys, function()
		Greece.MarkCompletedObjective(KillVIPs)
	end)

	Trigger.OnInfiltrated(WarFactory2, function()
		if not StealMammoth.IsDead or StealMammoth.Owner == USSR then
			Greece.MarkCompletedObjective(StealTank)
			StealMammoth.Owner = Greece
		end
	end)
end

ConsoleTriggers = function()
	Trigger.OnEnteredProximityTrigger(Terminal1.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower1.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTower1.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal2.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower2.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTower2.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal3.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower3.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTower3.Kill()
			end
		end
	end)

	local gasActive
	Trigger.OnEnteredProximityTrigger(Terminal4.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece and not gasActive then
			Trigger.RemoveProximityTrigger(id)
			gasActive = true

			Media.DisplayMessage(UserInterface.Translate("sarin-dispenser-activated"), UserInterface.Translate("console"))
			local KillCamera = Actor.Create("camera", true, { Owner = Greece, Location = Sarin2.Location })
			local flare1 = Actor.Create("flare", true, { Owner = England, Location = Sarin1.Location })
			local flare2 = Actor.Create("flare", true, { Owner = England, Location = Sarin2.Location })
			local flare3 = Actor.Create("flare", true, { Owner = England, Location = Sarin3.Location })
			local flare4 = Actor.Create("flare", true, { Owner = England, Location = Sarin4.Location })
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				Utils.Do(SarinVictims, function(actor)
					if not actor.IsDead then
						actor.Kill("ExplosionDeath")
					end
				end)
			end)

			Trigger.AfterDelay(DateTime.Seconds(20), function()
				flare1.Destroy()
				flare2.Destroy()
				flare3.Destroy()
				flare4.Destroy()
				KillCamera.Destroy()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal5.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not BadCoil.IsDead then
				Media.DisplayMessage(UserInterface.Translate("tesla-coil-deactivated"), UserInterface.Translate("console"))
				BadCoil.Kill()
			end
		end
	end)

	local teslaActive
	Trigger.OnEnteredProximityTrigger(Terminal6.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece and not teslaActive then
			Trigger.RemoveProximityTrigger(id)
			teslaActive = true

			Media.DisplayMessage(UserInterface.Translate("tesla-coil-activated"), UserInterface.Translate("console"))
			local tesla1 = Actor.Create("tsla", true, { Owner = Turkey, Location = TurkeyCoil1.Location })
			local tesla2 = Actor.Create("tsla", true, { Owner = Turkey, Location = TurkeyCoil2.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				if not tesla1.IsDead then
					tesla1.Kill()
				end
				if not tesla2.IsDead then
					tesla2.Kill()
				end
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal7.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTowerTanya1.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTowerTanya1.Kill()
			end
			if not FlameTowerTanya2.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTowerTanya2.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal8.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTowerExit1.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTowerExit1.Kill()
			end
			if not FlameTowerExit3.IsDead then
				Media.DisplayMessage(UserInterface.Translate("flame-turret-deactivated"), UserInterface.Translate("console"))
				FlameTowerExit3.Kill()
			end
		end
	end)
end

CameraTriggers = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local startCamera = Actor.Create("camera", true, { Owner = Greece, Location = start.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			startCamera.Destroy()
		end)
	end)

	local cam1Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger1.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == Greece and not cam1Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam1Triggered = true

			local camera1 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger1.Location })
			Trigger.OnAllKilled(Camera1Towers, function()
				camera1.Destroy()
			end)
		end
	end)

	local cam2Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger2.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == Greece and not cam2Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam2Triggered = true

			local camera2 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger2.Location })
			Utils.Do(Camera2Team, function(actor)
				actor.AttackMove(CameraTrigger1.Location)
			end)
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				camera2.Destroy()
			end)
		end
	end)

	local cam3Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger3.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == Greece and not cam3Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam3Triggered = true

			local camera3 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger3.Location })
			Actor.Create("apwr", true, { Owner = France, Location = PowerPlantSpawn1.Location })
			Actor.Create("apwr", true, { Owner = Germany, Location = PowerPlantSpawn2.Location })
			if not Mammoth1.IsDead then
				Mammoth1.AttackMove(MammothGo.Location)
			end
			Trigger.OnKilled(Mammoth1, function()
				GoodCoil.Kill()
				camera3.Destroy()
			end)
		end
	end)

	local cam4Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger4.CenterPosition, WDist.FromCells(9), function(actor, id)
		if actor.Owner == Greece and not cam4Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam4Triggered = true

			local camera4 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger4.Location })
			Trigger.OnKilled(Mammoth2, function()
				camera4.Destroy()
			end)
		end
	end)

	local cam5Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger5.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == Greece and not cam5Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam5Triggered = true

			local camera5 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger5.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				camera5.Destroy()
			end)
		end
	end)

	local cam6Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger6.CenterPosition, WDist.FromCells(11), function(actor, id)
		if actor.Owner == Greece and not cam6Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam6Triggered = true

			Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger6.Location })
		end
	end)

	local executionTriggered
	Trigger.OnEnteredFootprint(ExecutionArea, function(actor, id)
		if actor.Owner == Greece and not executionTriggered then
			Trigger.RemoveFootprintTrigger(id)
			executionTriggered = true

			local camera7 = Actor.Create("camera", true, { Owner = Greece, Location = CameraTrigger7.Location })
			Trigger.AfterDelay(DateTime.Seconds(25), function()
				camera7.Destroy()
			end)

			ScientistExecution()
		end
	end)
end

TruckSteal = function()
	Trigger.OnInfiltrated(WarFactory1, function()
		if not TruckStolen and not StealTruck.IsDead then
			TruckStolen = true

			local truckSteal1 = Actor.Create("camera", true, { Owner = Greece, Location = TruckDrive1.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				truckSteal1.Destroy()
			end)
			Utils.Do(TruckExit, function(waypoint)
				StealTruck.Move(waypoint.Location)
			end)
		end
	end)

	local trukDestroyed
	Trigger.OnEnteredFootprint({ TruckDrive2.Location }, function(actor, id)
		if actor.Type == "truk" and not trukDestroyed then
			Trigger.RemoveFootprintTrigger(id)
			trukDestroyed = true

			actor.Destroy()
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				SpyTruckDrive()
			end)
		end
	end)
end

SpyTruckDrive = function()
	StealTruck = Reinforcements.Reinforce(USSR, TruckType, TruckEntry)
	local truckSteal2 = Actor.Create("camera", true, { Owner = Greece, Location = TruckCamera.Location })
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		truckSteal2.Destroy()
	end)

	local spyCreated
	Trigger.OnEnteredFootprint({ TruckDrive5.Location }, function(actor, id)
		if actor.Type == "truk" and not spyCreated then
			Trigger.RemoveFootprintTrigger(id)
			spyCreated = true

			Spy = Actor.Create("spy", true, { Owner = Greece, Location = TruckDrive5.Location })
			Spy.DisguiseAsType("e1", USSR)
			Spy.Move(SpyMove.Location)

			local dogCrewCamera = Actor.Create("camera", true, { Owner = Greece, Location = DoggyCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				dogCrewCamera.Destroy()
			end)

			Utils.Do(DogCrew, function(actor)
				if not actor.IsDead then
					actor.AttackMove(DoggyMove.Location)
				end
			end)
		end
	end)
end

PrisonEscape = function()
	local alarmed
	Trigger.OnEnteredFootprint(PrisonAlarm, function(unit, id)
		if alarmed then
			return
		end

		alarmed = true
		Trigger.RemoveFootprintTrigger(id)

		Media.DisplayMessage(UserInterface.Translate("prisoners-escaping"), UserInterface.Translate("intercom"))
		Media.PlaySoundNotification(Greece, "AlertBuzzer")
		Utils.Do(GuardDogs, IdleHunt)
	end)
end

ScientistExecution = function()
	Media.PlaySoundNotification(Greece, "AlertBleep")
	Media.DisplayMessage(UserInterface.Translate("hurry-base-compromised"), UserInterface.Translate("soviet-officer"))
	Utils.Do(DemoTeam, function(actor)
		actor.AttackMove(DemoDrive2.Location)
	end)

	Trigger.OnAllKilled(FiringSquad, function()
		if not ScientistMan.IsDead then
			ScientistRescued()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(7), function()
		if not Officer2.IsDead then
			Media.DisplayMessage(UserInterface.Translate("prepare-to-fire"), UserInterface.Translate("soviet-officer"))
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		if not Officer2.IsDead then
			Media.DisplayMessage(UserInterface.Translate("fire"), UserInterface.Translate("soviet-officer"))
		end

		Utils.Do(FiringSquad, function(actor)
			if not actor.IsDead then
				actor.Attack(ScientistMan, true, true)
			end
		end)
	end)
end

ScientistRescued = function()
	Media.DisplayMessage(UserInterface.Translate("thanks-for-rescue"), UserInterface.Translate("scientist"))

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if not ScientistMan.IsDead and not DemoTruck.IsDead then
			Media.DisplayMessage(UserInterface.Translate("move-nuclear-outside"), UserInterface.Translate("scientist"))
			DemoTruck.GrantCondition("mission")
			ScientistMan.EnterTransport(DemoTruck)
		end
	end)

	Trigger.OnRemovedFromWorld(ScientistMan, DemoTruckExit)
end

DemoTruckExit = function()
	if ScientistMan.IsDead then
		return
	end

	Media.DisplayMessage(UserInterface.Translate("exit-clear-hopefully"), UserInterface.Translate("scientist"))
	Utils.Do(DemoTruckPath, function(waypoint)
		DemoTruck.Move(waypoint.Location)
	end)

	local trukEscaped
	Trigger.OnEnteredFootprint({ DemoDrive4.Location }, function(actor, id)
		if actor.Type == "dtrk" and not trukEscaped then
			Trigger.RemoveFootprintTrigger(id)
			trukEscaped = true
			actor.Destroy()
		end
	end)
end

AcceptableLosses = 0
Tick = function()
	if Greece.HasNoRequiredUnits() then
		Greece.MarkFailedObjective(ExitBase)
	end

	if Difficulty == "hard" and Greece.UnitsLost > AcceptableLosses then
		Greece.MarkFailedObjective(NoCasualties)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	Turkey = Player.GetPlayer("Turkey")
	USSR = Player.GetPlayer("USSR")
	France = Player.GetPlayer("France")
	Germany = Player.GetPlayer("Germany")

	InitObjectives(Greece)

	USSRobjective = USSR.AddObjective("")
	ExitBase = AddPrimaryObjective(Greece, "reach-eastern-exit")
	FreeTanyaObjective = AddPrimaryObjective(Greece, "free-tanya-keep-alive")
	KillVIPs = AddSecondaryObjective(Greece, "kill-soviet-officers-scientists")
	StealTank = AddSecondaryObjective(Greece, "steal-soviet-mammoth-tank")
	if Difficulty == "hard" then
		NoCasualties = AddPrimaryObjective(Greece, "no-casualties")
	end

	StartSpy.DisguiseAsType("e1", USSR)
	StartAttacker1.AttackMove(start.Location)
	StartAttacker2.AttackMove(start.Location)

	Camera.Position = start.CenterPosition

	ObjectiveTriggers()
	ConsoleTriggers()
	CameraTriggers()
	TruckSteal()
	PrisonEscape()
end
