--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		if not EscapeGoalTrigger and a.Owner == greece then
			EscapeGoalTrigger = true

			greece.MarkCompletedObjective(ExitBase)
			if Difficulty == "hard" then
				greece.MarkCompletedObjective(NoCasualties)
			end

			if not TanyaFreed then
				greece.MarkFailedObjective(FreeTanya)
			else
				greece.MarkCompletedObjective(FreeTanya)
			end
		end
	end)

	Trigger.OnKilled(Tanya, function()
		greece.MarkFailedObjective(FreeTanya)
	end)

	Trigger.OnAllKilled(TanyaTowers, function()
		TanyaFreed = true
		if not Tanya.IsDead then
			Media.PlaySpeechNotification(greece, "TanyaRescued")
			Tanya.Owner = greece
		end
	end)

	Trigger.OnAllKilled(SovietImportantGuys, function()
		greece.MarkCompletedObjective(KillVIPs)
	end)

	Trigger.OnInfiltrated(WarFactory2, function()
		if not StealMammoth.IsDead or StealMammoth.Owner == ussr then
			greece.MarkCompletedObjective(StealTank)
			StealMammoth.Owner = greece
		end
	end)
end

ConsoleTriggers = function()
	Trigger.OnEnteredProximityTrigger(Terminal1.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower1.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTower1.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal2.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower2.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTower2.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal3.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTower3.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTower3.Kill()
			end
		end
	end)

	local gasActive
	Trigger.OnEnteredProximityTrigger(Terminal4.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece and not gasActive then
			Trigger.RemoveProximityTrigger(id)
			gasActive = true

			Media.DisplayMessage("Sarin Nerve Gas dispensers activated", "Console")
			local KillCamera = Actor.Create("camera", true, { Owner = greece, Location = Sarin2.Location })
			local flare1 = Actor.Create("flare", true, { Owner = england, Location = Sarin1.Location })
			local flare2 = Actor.Create("flare", true, { Owner = england, Location = Sarin2.Location })
			local flare3 = Actor.Create("flare", true, { Owner = england, Location = Sarin3.Location })
			local flare4 = Actor.Create("flare", true, { Owner = england, Location = Sarin4.Location })
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
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not BadCoil.IsDead then
				Media.DisplayMessage("Tesla Coil deactivated", "Console")
				BadCoil.Kill()
			end
		end
	end)

	local teslaActive
	Trigger.OnEnteredProximityTrigger(Terminal6.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece and not teslaActive then
			Trigger.RemoveProximityTrigger(id)
			teslaActive = true

			Media.DisplayMessage("Initialising Tesla Coil defence", "Console")
			local tesla1 = Actor.Create("tsla", true, { Owner = turkey, Location = TurkeyCoil1.Location })
			local tesla2 = Actor.Create("tsla", true, { Owner = turkey, Location = TurkeyCoil2.Location })
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
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTowerTanya1.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTowerTanya1.Kill()
			end
			if not FlameTowerTanya2.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTowerTanya2.Kill()
			end
		end
	end)

	Trigger.OnEnteredProximityTrigger(Terminal8.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == greece then
			Trigger.RemoveProximityTrigger(id)
			if not FlameTowerExit1.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTowerExit1.Kill()
			end
			if not FlameTowerExit3.IsDead then
				Media.DisplayMessage("Flame Turret deactivated", "Console")
				FlameTowerExit3.Kill()
			end
		end
	end)
end

CameraTriggers = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local startCamera = Actor.Create("camera", true, { Owner = greece, Location = start.Location })
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			startCamera.Destroy()
		end)
	end)

	local cam1Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger1.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == greece and not cam1Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam1Triggered = true

			local camera1 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger1.Location })
			Trigger.OnAllKilled(Camera1Towers, function()
				camera1.Destroy()
			end)
		end
	end)

	local cam2Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger2.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == greece and not cam2Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam2Triggered = true

			local camera2 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger2.Location })
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
		if actor.Owner == greece and not cam3Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam3Triggered = true

			local camera3 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger3.Location })
			Actor.Create("apwr", true, { Owner = france, Location = PowerPlantSpawn1.Location })
			Actor.Create("apwr", true, { Owner = germany, Location = PowerPlantSpawn2.Location })
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
		if actor.Owner == greece and not cam4Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam4Triggered = true

			local camera4 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger4.Location })
			Trigger.OnKilled(Mammoth2, function()
				camera4.Destroy()
			end)
		end
	end)

	local cam5Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger5.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Owner == greece and not cam5Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam5Triggered = true

			local camera5 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger5.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				camera5.Destroy()
			end)
		end
	end)

	local cam6Triggered
	Trigger.OnEnteredProximityTrigger(CameraTrigger6.CenterPosition, WDist.FromCells(11), function(actor, id)
		if actor.Owner == greece and not cam6Triggered then
			Trigger.RemoveProximityTrigger(id)
			cam6Triggered = true

			Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger6.Location })
		end
	end)

	local executionTriggered
	Trigger.OnEnteredFootprint(ExecutionArea, function(actor, id)
		if actor.Owner == greece and not executionTriggered then
			Trigger.RemoveFootprintTrigger(id)
			executionTriggered = true

			local camera7 = Actor.Create("camera", true, { Owner = greece, Location = CameraTrigger7.Location })
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

			local truckSteal1 = Actor.Create("camera", true, { Owner = greece, Location = TruckDrive1.Location })
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
	StealTruck = Reinforcements.Reinforce(ussr, TruckType, TruckEntry)
	local truckSteal2 = Actor.Create("camera", true, { Owner = greece, Location = TruckCamera.Location })
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		truckSteal2.Destroy()
	end)

	local spyCreated
	Trigger.OnEnteredFootprint({ TruckDrive5.Location }, function(actor, id)
		if actor.Type == "truk" and not spyCreated then
			Trigger.RemoveFootprintTrigger(id)
			spyCreated = true

			Spy = Actor.Create("spy", true, { Owner = greece, Location = TruckDrive5.Location })
			Spy.DisguiseAsType("e1", ussr)
			Spy.Move(SpyMove.Location)

			local dogCrewCamera = Actor.Create("camera", true, { Owner = greece, Location = DoggyCam.Location })
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

		Media.DisplayMessage("Warning, prisoners are attempting to escape!", "Intercom")
		Media.PlaySoundNotification(greece, "AlertBuzzer")
		Utils.Do(GuardDogs, IdleHunt)
	end)
end

ScientistExecution = function()
	Media.PlaySoundNotification(greece, "AlertBleep")
	Media.DisplayMessage("The base is compromised. We have to hurry the execution!", "Soviet Officer")
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
			Media.DisplayMessage("Prepare to Fire!", "Soviet Officer")
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		if not Officer2.IsDead then
			Media.DisplayMessage("Fire!", "Soviet Officer")
		end

		Utils.Do(FiringSquad, function(actor)
			if not actor.IsDead then
				actor.Attack(ScientistMan, true, true)
			end
		end)
	end)
end

ScientistRescued = function()
	Media.DisplayMessage("Thanks for the rescue!", "Scientist")

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if not ScientistMan.IsDead and not DemoTruck.IsDead then
			Media.DisplayMessage("The Soviets have an unstable nuclear device stored here. \n I need to move it out of the facility!", "Scientist")
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

	Media.DisplayMessage("I hope the exit is clear!", "Scientist")
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
	if greece.HasNoRequiredUnits() then
		greece.MarkFailedObjective(ExitBase)
	end

	if Difficulty == "hard" and greece.UnitsLost > AcceptableLosses then
		greece.MarkFailedObjective(NoCasualties)
	end
end

WorldLoaded = function()
	greece = Player.GetPlayer("Greece")
	england = Player.GetPlayer("England")
	turkey = Player.GetPlayer("Turkey")
	ussr = Player.GetPlayer("USSR")
	france = Player.GetPlayer("France")
	germany = Player.GetPlayer("Germany")

	InitObjectives(greece)

	ussrObj = ussr.AddObjective("Defeat the Allies.")
	ExitBase = greece.AddObjective("Reach the eastern exit of the facility.")
	FreeTanya = greece.AddObjective("Free Tanya and keep her alive.")
	KillVIPs = greece.AddObjective("Kill all Soviet officers and scientists.", "Secondary", false)
	StealTank = greece.AddObjective("Steal a Soviet mammoth tank.", "Secondary", false)
	if Difficulty == "hard" then
		NoCasualties = greece.AddPrimaryObjective("Do not lose a single soldier or civilian\nunder your command.")
	end

	StartSpy.DisguiseAsType("e1", ussr)
	StartAttacker1.AttackMove(start.Location)
	StartAttacker2.AttackMove(start.Location)

	Camera.Position = start.CenterPosition

	ObjectiveTriggers()
	ConsoleTriggers()
	CameraTriggers()
	TruckSteal()
	PrisonEscape()
end
