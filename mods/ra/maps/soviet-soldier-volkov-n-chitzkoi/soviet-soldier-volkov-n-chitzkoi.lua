--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
-- Unit Groups Setup
SuperTeam = { "zkoi", "volk" }
PlayerTankDivision = { PlyrHvyTnk01, PlyrHvyTnk02, PlyrHvyTnk03, PlyrHvyTnk04, PlyrHvyTnk05, PlyrMthTnk01, PlyrMthTnk02, PlyrV2RL01, PlyrV2RL02, PlyrV2RL03, PlyrV2RL04 }
InitialHuntTeam = { InitialHuntUnit01, InitialHuntUnit02, InitialHuntUnit03, InitialHuntUnit04, InitialHuntUnit05, InitialHuntUnit06, InitialHuntUnit07, InitialHuntUnit08, InitialHuntUnit09 }
BarrelsShooter = { InitialRifleman01, InitialRifleman02 }
TownPeoples = { TownDude01, TownDude02, TownDude03, TownDude04, TownMedic01, TownMedic02, TownMedic03 }
CivTeam01 = { "c1", "c3", "c4" }
CivTeam02 = { "c4", "c5", "c6" }
InfGuardSquad01 = { InfGuardSquad01Unit01, InfGuardSquad01Unit02, InfGuardSquad01Unit03, InfGuardSquad01Unit04, InfGuardSquad01Unit05, MediumTankGuard01 }
InfGuardSquad02 = { InfGuardSquad02Unit01, InfGuardSquad02Unit02, InfGuardSquad02Unit03, RangerGuard02 }
InfGuardSquad03 = { InfGuardSquad03Unit01, InfGuardSquad03Unit02, InfGuardSquad03Unit03, InfGuardSquad03Unit04, InfGuardSquad03Unit05, RangerGuard04 }
TanyaSquad = { TanyaSquadUnit01, TanyaSquadUnit02, TanyaSquadUnit03, TanyaSquadUnit04, TanyaSquadUnit05, TanyaSquadUnit06, TanyaSquadTanya }

-- Building Group Setup
AlliedOreRef = { OreRefinery01, OreRefinery02 }
AlliedWarFact = { AlliedWarFact01, AlliedWarFact02 }
HeavyTurrets = { HTurret01, HTurret02, HTurret03 }

-- Area Triggers Setup
SuperTeamLandCell = { CPos.New(21, 82), CPos.New(20, 81), CPos.New(21, 81), CPos.New(22, 81), CPos.New(20, 82), CPos.New(22, 82), CPos.New(21, 83), CPos.New(20, 83), CPos.New(22, 83) }
CivTeam01Trigger = { CPos.New(21, 58), CPos.New(21, 59), CPos.New(21, 60), CPos.New(22, 60), CPos.New(23, 60), CPos.New(22, 61), CPos.New(23, 61), CPos.New(24, 60), CPos.New(25, 60), CPos.New(24, 59), CPos.New(24, 58), CPos.New(23, 58), CPos.New(22, 58) }
CivTeam02Trigger = { CPos.New(33, 62), CPos.New(33, 63), CPos.New(32, 62), CPos.New(32, 63), CPos.New(31, 62), CPos.New(31, 63), CPos.New(31, 61), CPos.New(31, 60), CPos.New(30, 62), CPos.New(30, 61), CPos.New(30, 60), CPos.New(32, 60), CPos.New(33, 60) }
MineSoldierTrigger = { CPos.New(32, 58), CPos.New(32, 59), CPos.New(33, 58), CPos.New(33, 59), CPos.New(31, 59), CPos.New(30, 59), CPos.New(29, 59), CPos.New(29, 58), CPos.New(28, 59), CPos.New(27, 59), CPos.New(26, 59), CPos.New(25, 59), CPos.New(27, 58), CPos.New(26, 58), CPos.New(25, 58), CPos.New(24, 58), CPos.New(23, 58), CPos.New(26, 57), CPos.New(24, 57), CPos.New(24, 56), CPos.New(24, 55), CPos.New(24, 54),  CPos.New(23, 57), CPos.New(23, 56), CPos.New(23, 55), CPos.New(23, 54), CPos.New(22, 57) }
MineRevealTrigger = { CPos.New(30, 46), CPos.New(31, 46), CPos.New(32, 46), CPos.New(33, 46), CPos.New(34, 46), CPos.New(35, 46), CPos.New(36, 46), CPos.New(37, 46) }
ParaTrigger = { CPos.New(18, 34), CPos.New(19, 34), CPos.New(20, 34), CPos.New(21, 34), CPos.New(22, 34), CPos.New(23, 34), CPos.New(24, 34), CPos.New(25, 34), CPos.New(18, 35), CPos.New(19, 35), CPos.New(20, 35), CPos.New(21, 35), CPos.New(22, 35), CPos.New(23, 35), CPos.New(24, 35), CPos.New(25, 35) }
TanyaTrigger = { CPos.New(59, 43), CPos.New(60, 43), CPos.New(61, 43), CPos.New(62, 43), CPos.New(63, 43), CPos.New(64, 43), CPos.New(65, 43), CPos.New(66, 43), CPos.New(67, 43), CPos.New(68, 43), CPos.New(69, 43), CPos.New(59, 44), CPos.New(60, 44), CPos.New(61, 44), CPos.New(62, 44), CPos.New(63, 44), CPos.New(64, 44), CPos.New(65, 44), CPos.New(66, 44), CPos.New(67, 44), CPos.New(68, 44), CPos.New(69, 44) }

-- Mission Variables Setup
GreeceHarvestersAreDead = false
AlloyFacilityDestroyed = false

WorldLoaded = function()

--Players Setup
	player = Player.GetPlayer("USSR")
	greece = Player.GetPlayer("Greece")
	goodguy = Player.GetPlayer("GoodGuy")
	spain = Player.GetPlayer("Spain")
	france = Player.GetPlayer("France")

	greece.Cash = 20000

	Camera.Position	= DefaultCameraPosition.CenterPosition

--AI Production Setup
	ProduceArmor()

	if Difficulty == "easy" then
		Trigger.AfterDelay(DateTime.Minutes(10), ProduceNavyGuard)
	elseif Difficulty == "normal" then
		Trigger.AfterDelay(DateTime.Minutes(5), ProduceNavyGuard)
	elseif Difficulty == "hard" then
		ProduceNavyGuard()
	end

--Objectives Setup
	InitObjectives(player)

	DestroyControlCenter = player.AddObjective("Destroy the Control Center.")
	KeepTanksAlive = player.AddObjective("Your tank division must not be destroyed before\n the alloy facility is dealt with.")
	KeepVolkovAlive = player.AddObjective("Keep Volkov Alive.")
	KeepChitzkoiAlive = player.AddObjective("Keep Chitzkoi Alive.", "Secondary", false)

	Trigger.OnKilled(ControlCenter, function()
		Utils.Do(HeavyTurrets, function(struc)
			if not struc.IsDead then struc.Kill() end
		end)
		player.MarkCompletedObjective(DestroyControlCenter)
		DestroyAlloyFacility = player.AddObjective("Destroy the Alloy Facility.")
		Media.PlaySpeechNotification(player, "FirstObjectiveMet")
		Media.DisplayMessage("Excellent! The heavy turret control center is destroyed\n and now we can deal with the alloy facility.")
	end)

	Trigger.OnKilled(AlloyFacility, function()
		if not player.IsObjectiveCompleted(DestroyControlCenter) then --Prevent a crash if the player somehow manage to cheese the mission and destroy
			player.MarkCompletedObjective(DestroyControlCenter) --the Alloy Facility without destroying the Control Center.
			DestroyAlloyFacility = player.AddObjective("Destroy the Alloy Facility.")
		end
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			player.MarkCompletedObjective(DestroyAlloyFacility)
			player.MarkCompletedObjective(KeepTanksAlive)
			player.MarkCompletedObjective(KeepVolkovAlive)
			player.MarkCompletedObjective(KeepChitzkoiAlive)
		end)
		AlloyFacilityDestroyed = true
		Media.PlaySpeechNotification(player, "SecondObjectiveMet")
	end)

	Trigger.OnAllKilled(PlayerTankDivision, function()
		if not AlloyFacilityDestroyed then player.MarkFailedObjective(KeepTanksAlive) end
	end)

	Trigger.AfterDelay(0, function()
		local AlliedBaseCamera = Actor.Create("camera", true, { Owner = player, Location = waypoint12.Location })
		local SuperTeamCamera = Actor.Create("camera", true, { Owner = player, Location = DefaultCameraPosition.Location })
		Trigger.AfterDelay(1, function()
			if AlliedBaseCamera.IsInWorld then AlliedBaseCamera.Destroy() end
		end)
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			if SuperTeamCamera.IsInWorld then SuperTeamCamera.Destroy() end
		end)
	end)

--Super Team Setup
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		local spawn = superteamspawn.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("badr"))
		local transport = Actor.Create("badr", true, { CenterPosition = spawn, Owner = player, Facing = (superteamdrop.CenterPosition - spawn).Facing, Health = 3 })
		Utils.Do(SuperTeam, function(type)
			local a = Actor.Create(type, false, { Owner = player })
			transport.LoadPassenger(a)
			if a.Type == "volk" then
				VolkovIsDead(a)
			end
			if a.Type == "zkoi" then
				ChitzkoiIsDead(a)
			end
		end)
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
		transport.Paradrop(CPos.New(21, 82))
	end)

	Trigger.OnEnteredFootprint(SuperTeamLandCell, function(unit, id)
		if unit.Owner == player then
			Trigger.RemoveFootprintTrigger(id)
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if not BarrelsShooter[1].IsDead then
					BarrelsShooter[1].Attack(Barrel, true, true)
				elseif not BarrelsShooter[2].IsDead then
					BarrelsShooter[2].Attack(Barrel, true, true)
				end
				Utils.Do(InitialHuntTeam, function(actor)
					if not actor.IsDead then
						Trigger.OnIdle(actor, actor.Hunt)
					end
				end)
			end)
		end
	end)

--Guards Squads Setup -- I used proximity triggers to make them hunt you down in order to mimic their behavior from the original mission
	Trigger.OnEnteredProximityTrigger(RangerGuard01.CenterPosition, WDist.New(70 * 70), function(unit, id)
		if not RangerGuard01.IsDead and unit.Owner == player then
			Trigger.OnIdle(RangerGuard01, RangerGuard01.Hunt)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredProximityTrigger(waypoint7.CenterPosition, WDist.FromCells(6), function(unit, id)
		if unit.Owner == player then
			Utils.Do(InfGuardSquad01, function(actor)
				if not actor.IsDead then
					Trigger.OnIdle(actor, actor.Hunt)
				end
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredProximityTrigger(InfGuardSquad02Unit01.CenterPosition, WDist.FromCells(6), function(unit, id)
		if unit.Owner == player and (unit.Type == "volk" or unit.Type == "zkoi") then
			Utils.Do(InfGuardSquad02, function(actor)
				if not actor.IsDead then
					Trigger.OnIdle(actor, actor.Hunt)
				end
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredProximityTrigger(InfGuardSquad03Unit05.CenterPosition, WDist.FromCells(8), function(unit, id)
		if unit.Owner == player then
			local HospitalCamera = Actor.Create("camera", true, { Owner = player, Location = waypoint13.Location })
			Utils.Do(InfGuardSquad03, function(actor)
				if not actor.IsDead then
					Trigger.OnIdle(actor, actor.Hunt)
				end
			end)
			if not SupplyTruck01.IsDead then
				SupplyTruck01.Move(waypoint14.Location)
				Trigger.AfterDelay(DateTime.Seconds(8), function()
					if not SupplyTruck01.IsDead then
						SupplyTruck01.Move(waypoint15.Location)
					end
				end)
			end
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				if HospitalCamera.IsInWorld then HospitalCamera.Destroy() end
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredProximityTrigger(LightTankGuard02.CenterPosition, WDist.FromCells(8), function(unit, id)
		if not LightTankGuard02.IsDead and unit.Owner == player and (unit.Type == "volk" or unit.Type == "zkoi") then
			Trigger.OnIdle(LightTankGuard02, LightTankGuard02.Hunt)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

--Tanya Squad Setup
	Trigger.OnEnteredFootprint(TanyaTrigger, function(unit, id)
		if unit.Owner == player then
			if not TanyaSquadTanya.IsDead then
				local TanyaSquadCamera = Actor.Create("camera", true, { Owner = player, Location = waypoint85.Location })
				Media.PlaySoundNotification(player, "rokroll")
				Utils.Do(TanyaSquad, function(actor)
					if not actor.IsDead then
						Trigger.OnIdle(actor, actor.Hunt)
					end
				end)
				Trigger.OnKilled(TanyaSquadTanya, function()
					if TanyaSquadCamera.IsInWorld then
						TanyaSquadCamera.Destroy()
					end
				end)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

--Town Setup
	Utils.Do(TownPeoples, function(actor)
		Trigger.OnDamaged(actor, function()
			if not TownMedic01.IsDead then
				TownMedic01.Patrol({ waypoint5.Location, waypoint6.Location, waypoint7.Location }, true, 0)
			end
			if not TownMedic02.IsDead then
				TownMedic02.Patrol({ waypoint8.Location, waypoint7.Location, waypoint5.Location, waypoint6.Location }, true, 0)
			end
		end)
	end)

	Trigger.OnEnteredFootprint(CivTeam01Trigger, function(unit, id)
		if unit.Owner == player then
			if not TownHouse03.IsDead then
				local civ01 = Reinforcements.Reinforce(spain, CivTeam01, { civteam01spawn.Location }, 0)
				Utils.Do(civ01, function(actor)
					if not actor.IsDead then
						Trigger.OnIdle(actor, actor.Hunt)
					end
				end)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(CivTeam02Trigger, function(unit, id)
		if unit.Owner == player then
			if not TownHouse04.IsDead then
				local civ02 = Reinforcements.Reinforce(spain, CivTeam02, { civteam02spawn.Location }, 0)
				Utils.Do(civ02, function(actor)
					if not actor.IsDead then
						Trigger.OnIdle(actor, actor.Hunt)
					end
				end)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

--Minefield Setup
	Trigger.OnEnteredFootprint(MineSoldierTrigger, function(unit, id)
		if unit.Owner == player then
			local MineSoldierCamera1 = Actor.Create("camera", true, { Owner = player, Location = waypoint96.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				if MineSoldierCamera1.IsInWorld then MineSoldierCamera1.Destroy() end
			end)
			if not MineSoldier01.IsDead then
				MineSoldier01.Patrol({ waypoint91.Location, waypoint95.Location, waypoint76.Location, waypoint93.Location }, false, 0)
			end
			if not MineSoldier02.IsDead then
				MineSoldier02.Patrol({ waypoint92.Location, waypoint91.Location, waypoint76.Location, waypoint93.Location }, false, 0)
			end
			if not MineSoldier03.IsDead then
				MineSoldier03.Patrol({ waypoint91.Location, waypoint95.Location, waypoint76.Location, waypoint93.Location }, false, 0)
			end
			if not MineSoldier04.IsDead then
				MineSoldier04.Patrol({ waypoint92.Location, waypoint95.Location, waypoint76.Location, waypoint93.Location }, false, 0)
			end
			if not MineSoldier05.IsDead then
				MineSoldier05.Patrol({ waypoint90.Location, waypoint91.Location, waypoint95.Location, waypoint76.Location, waypoint93.Location }, false, 0)
			end
			if not MineSoldier06.IsDead then
				MineSoldier06.Patrol({ waypoint92.Location, waypoint91.Location, waypoint93.Location }, false, 0)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(MineRevealTrigger, function(unit, id)
		if unit.Owner == goodguy then
			local MineSoldierCamera2 = Actor.Create("camera", true, { Owner = player, Location = waypoint76.Location })
			Trigger.AfterDelay(DateTime.Seconds(12), function()
				if MineSoldierCamera2.IsInWorld then MineSoldierCamera2.Destroy() end
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

--Paradrop Rifle Team Setup
	Trigger.OnEnteredFootprint(ParaTrigger, function(unit, id)
		if unit.Owner == player then
			local powerproxy = Actor.Create("powerproxy.pararifles", true, { Owner = greece })
			local aircraft = powerproxy.TargetParatroopers(waypoint89.CenterPosition, Angle.South)
			local prtcamera = Actor.Create("camera", true, { Owner = player, Location = waypoint89.Location })
			Utils.Do(aircraft, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				if prtcamera.IsInWorld then prtcamera.Destroy() end
			end)
			if Difficulty == "hard" and not RiflemanGuard01.IsDead then
				Trigger.ClearAll(RiflemanGuard01)
				ProduceInfantry() --Greece will start infantry production right away if the difficulty is set to hard
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnKilled(RiflemanGuard01, function()
		ProduceInfantry() --Greece will start infantry production once this unit is dead just like in the original mission
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		local GreeceHarvesters = greece.GetActorsByType("harv")
		Trigger.OnAllKilled(GreeceHarvesters, function()
			GreeceHarvestersAreDead = true
		end)
	end)

end

VolkovIsDead = function(a)
	Trigger.OnKilled(a, function()
		player.MarkFailedObjective(KeepVolkovAlive)
	end)
end

ChitzkoiIsDead = function(a)
	Trigger.OnKilled(a, function()
		player.MarkFailedObjective(KeepChitzkoiAlive)
		Media.DisplayMessage("We can rebuild Chitzkoi. We have the technology.")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "ObjectiveNotMet")
		end)
	end)
end
