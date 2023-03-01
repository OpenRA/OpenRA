--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--All timers have been reduced by 40% because time was flying 40% faster than reality in the original game.
--That's why instead of having an hour to complete the mission you only have 36 minutes.

--Unit Groups Setup
USSRDie01 = { USSRGrenadier01, USSRGrenadier02, USSRGrenadier03, USSRFlame01, USSRFlame03 }
USSRDie02 = { USSRGrenadier04, USSRGrenadier05, USSRFlame02, USSRFlame04 }
USSRDie03 = { USSRHTank01, USSRHTank02 }
USSRDie04 = { USSRDog01, USSRDog03 }
USSRDie05 = { USSRDog02, USSRDog04 }
USSRDie06 = { USSRV202, USSRV203 }
AlliedSquad01 = { AlliedSquad01RocketInf01, AlliedSquad01RocketInf02, AlliedSquad01RocketInf03, AlliedSquad01RocketInf04, AlliedSquad01RocketInf05 }
AlliedSquad02 = { AlliedSquad02RifleInf01, AlliedSquad02RifleInf02, AlliedSquad02RifleInf03, AlliedSquad02RifleInf04, AlliedSquad02RifleInf05, AlliedSquad02RifleInf06, AlliedSquad02RifleInf07, AlliedSquad02RifleInf08, AlliedSquad02RifleInf09 }
AlliedSquad03 = { AlliedSquad03LTank01, AlliedSquad03RocketInf01, AlliedSquad03RocketInf02, AlliedSquad03RocketInf03 }
AlliedSquad04 = { AlliedSquad04MGG01, AlliedSquad04MTank01, AlliedSquad04MTank02, AlliedSquad04MTank03, AlliedSquad04MTank04, AlliedSquad04MTank05, AlliedSquad04Arty01, AlliedSquad04Arty02, AlliedSquad04Arty03 }
AlliedTanksReinforcement = { "2tnk", "2tnk" }
if Difficulty == "easy" then
	AlliedHuntingParty = { "1tnk" }
elseif Difficulty == "normal" then
	AlliedHuntingParty = { "1tnk", "1tnk" }
elseif Difficulty == "hard" then
	AlliedHuntingParty = { "1tnk", "1tnk", "1tnk" }
end

--Building Group Setup
AlliedAAGuns = { AAGun01, AAGun02, AAGun03, AAGun04, AAGun05, AAGun06 }

--Area Triggers Setup
WaystationTrigger = { CPos.New(61, 37), CPos.New(62, 37), CPos.New(63, 37), CPos.New(64, 37), CPos.New(65, 37), CPos.New(66, 37), CPos.New(67, 37), CPos.New(68, 37), CPos.New(69, 37), CPos.New(70, 37), CPos.New(71, 37), CPos.New(72, 37), CPos.New(73, 37), CPos.New(74, 37), CPos.New(75, 37), CPos.New(61, 38), CPos.New(62, 38), CPos.New(63, 38), CPos.New(64, 38), CPos.New(65, 38), CPos.New(66, 38), CPos.New(67, 38), CPos.New(68, 38), CPos.New(69, 38), CPos.New(70, 38), CPos.New(71, 38), CPos.New(72, 38), CPos.New(73, 38), CPos.New(74, 38), CPos.New(75, 38), CPos.New(61, 39), CPos.New(62, 39), CPos.New(63, 39), CPos.New(64, 39), CPos.New(65, 39), CPos.New(66, 39), CPos.New(67, 39), CPos.New(68, 39), CPos.New(69, 39), CPos.New(70, 39), CPos.New(71, 39), CPos.New(72, 39), CPos.New(73, 39), CPos.New(74, 39), CPos.New(75, 39) }
Inf01Trigger = { CPos.New(81, 90), CPos.New(81, 91), CPos.New(81, 92), CPos.New(81, 93), CPos.New(81, 94), CPos.New(81, 95), CPos.New(82, 90), CPos.New(82, 91), CPos.New(82, 92), CPos.New(82, 93), CPos.New(82, 94), CPos.New(82, 95) }
Inf02Trigger = { CPos.New(85, 90), CPos.New(85, 91), CPos.New(85, 92), CPos.New(85, 93), CPos.New(85, 94), CPos.New(85, 95), CPos.New(86, 90), CPos.New(86, 91), CPos.New(86, 92), CPos.New(86, 93), CPos.New(86, 94), CPos.New(86, 95) }
RevealBridgeTrigger = { CPos.New(74, 52), CPos.New(75, 52), CPos.New(76, 52), CPos.New(77, 52), CPos.New(78, 52), CPos.New(79, 52), CPos.New(80, 52), CPos.New(81, 52), CPos.New(82, 52), CPos.New(83, 52), CPos.New(84, 52), CPos.New(85, 52), CPos.New(86, 52), CPos.New(87, 52), CPos.New(88, 52), CPos.New(76, 53), CPos.New(77, 53), CPos.New(78, 53), CPos.New(79, 53), CPos.New(80, 53), CPos.New(81, 53), CPos.New(82, 53), CPos.New(83, 53), CPos.New(84, 53), CPos.New(85, 53), CPos.New(86, 53), CPos.New(87, 53) }

--Mission Variables Setup
DateTime.TimeLimit = DateTime.Minutes(36)
BridgeIsIntact = true

--Mission Functions Setup
HuntObjectiveTruck = function(a)
	if a.HasProperty("Hunt") then
		if a.Owner == Greece or a.Owner == GoodGuy then
			Trigger.OnIdle(a, function(a)
				if a.IsInWorld and not ObjectiveTruck01.IsDead then
					a.AttackMove(ObjectiveTruck01.Location, 2)
				elseif a.IsInWorld then
					a.Hunt()
				end
			end)
		end
	end
end

HuntEnemyUnits = function(a)
	if a.HasProperty("Hunt") then
		Trigger.OnIdle(a, function(a)
			if a.IsInWorld then
				a.Hunt()
			end
		end)
	end
end

AlliedGroundPatrols = function(a)
	if a.HasProperty("Hunt") then
		if a.IsInWorld then
			a.Patrol({ AlliedHuntingPartyWP02.Location, AlliedHuntingPartyWP03.Location, AlliedHuntingPartyWP04.Location, AlliedHuntingPartyWP05.Location, AlliedHuntingPartyWP06.Location, AlliedHuntingPartyWP07.Location }, false, 50)
		end
	end
end

SpawnAlliedHuntingParty = function()
	Trigger.AfterDelay(DateTime.Minutes(3), function()
		if BridgeIsIntact then
			local tanks = Reinforcements.Reinforce(Greece, AlliedHuntingParty, { AlliedHuntingPartySpawn.Location, AlliedHuntingPartyWP01.Location,AlliedHuntingPartyWP03.Location, AlliedHuntingPartyWP05.Location }, 0)
			Utils.Do(tanks, function(units)
				HuntObjectiveTruck(units)
			end)
			SpawnAlliedHuntingParty()
		end
	end)
end

WorldLoaded = function()
--Players Setup
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	GoodGuy = Player.GetPlayer("GoodGuy")
	BadGuy = Player.GetPlayer("BadGuy")
	Neutral = Player.GetPlayer("Neutral")
	Creeps = Player.GetPlayer("Creeps")

	Camera.Position	= DefaultCameraPosition.CenterPosition

--Objectives Setup
	InitObjectives(USSR)

	BringSupplyTruck = AddPrimaryObjective(USSR, "supply-truck-waystation")
	ProtectWaystation = AddPrimaryObjective(USSR, "waystation-must-not-be-destroyed")
	DestroyAAGuns = AddSecondaryObjective(USSR, "destory-aa-guns-enable-air-support")
	PreventAlliedIncursions = AddSecondaryObjective(USSR, "find-destroy-bridge-stop-allied-reinforcements")

	Trigger.OnKilled(USSRTechCenter01, function()
		USSR.MarkFailedObjective(ProtectWaystation)
	end)

	Trigger.OnKilled(ObjectiveTruck01, function()
		USSR.MarkFailedObjective(BringSupplyTruck)
	end)

	Trigger.OnEnteredFootprint(WaystationTrigger, function(unit, id)
		if unit == ObjectiveTruck01 then
			Trigger.RemoveFootprintTrigger(id)
			USSR.MarkCompletedObjective(BringSupplyTruck)
			USSR.MarkCompletedObjective(ProtectWaystation)
		end
	end)

	Trigger.OnAllKilled(AlliedAAGuns, function()
		USSR.MarkCompletedObjective(DestroyAAGuns)
		Media.PlaySpeechNotification(USSR, "ObjectiveMet")
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Actor.Create("powerproxy.spyplane", true, { Owner = USSR })
			Actor.Create("powerproxy.parabombs", true, { Owner = USSR })
			Media.DisplayMessage(UserInterface.Translate("air-support-t-minus-3"))
		end)
  end)

--Triggers Setup
	SpawnAlliedHuntingParty()

	Trigger.AfterDelay(0, function()
		local playerrevealcam = Actor.Create("camera", true, { Owner = USSR, Location = PlayerStartLocation.Location })
		Trigger.AfterDelay(1, function()
			if playerrevealcam.IsInWorld then playerrevealcam.Destroy() end
		end)
	end)

	Trigger.OnEnteredFootprint(Inf01Trigger, function(unit, id)
		if unit.Owner == USSR then
			if not AlliedGNRLHouse.IsDead then
				Reinforcements.Reinforce(Greece, { "gnrl" }, { AlliedGNRLSpawn.Location, AlliedGNRLDestination.Location }, 0, function(unit)
					HuntEnemyUnits(unit)
				end)
			end
			Utils.Do(AlliedSquad01, HuntEnemyUnits)
			local alliedgnrlcamera = Actor.Create("scamera", true, { Owner = USSR, Location = AlliedGNRLSpawn.Location })
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				if alliedgnrlcamera.IsInWorld then alliedgnrlcamera.Destroy() end
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Inf02Trigger, function(unit, id)
		if unit.Owner == USSR then
			Utils.Do(AlliedSquad02, HuntEnemyUnits)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Utils.Do(AlliedSquad03, function(actor)
		Trigger.OnDamaged(actor, function(unit, attacker)
			if attacker.Owner == USSR then
				Utils.Do(AlliedSquad03, HuntEnemyUnits)
			end
		end)
	end)

	Trigger.OnEnteredFootprint(RevealBridgeTrigger, function(unit, id)
		if unit.Owner == USSR then
			local bridgecamera01 = Actor.Create("camera", true, { Owner = USSR, Location = AlliedHuntingPartySpawn.Location })
			local bridgecamera02 = Actor.Create("camera", true, { Owner = USSR, Location = AlliedHuntingPartyWP01.Location })
			Trigger.AfterDelay(DateTime.Seconds(6), function()
				if bridgecamera01.IsInWorld then bridgecamera01.Destroy() end
				if bridgecamera02.IsInWorld then bridgecamera02.Destroy() end
			end)
			if Difficulty == "normal" then
				Reinforcements.Reinforce(GoodGuy, { "dd" }, { AlliedDestroyer01Spawn.Location, AlliedDestroyer01WP01.Location, AlliedDestroyer01WP02.Location }, 0, function(unit)
					unit.Stance = "Defend"
				end)
			end
			if Difficulty == "hard" then
				Reinforcements.Reinforce(GoodGuy, { "dd" }, { AlliedDestroyer01Spawn.Location, AlliedDestroyer01WP01.Location, AlliedDestroyer01WP02.Location }, 0, function(unit)
					unit.Stance = "Defend"
				end)
				Reinforcements.Reinforce(GoodGuy, { "dd" }, { AlliedDestroyer02Spawn.Location, AlliedDestroyer02WP01.Location, AlliedDestroyer02WP02.Location }, 0, function(unit)
					unit.Stance = "Defend"
				end)
			end
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(9), function()
		local powerproxy01 = Actor.Create("powerproxy.paratroopers", true, { Owner = Greece })
		local aircraft01 = powerproxy01.TargetParatroopers(AlliedParadropLZ01.CenterPosition, Angle.SouthWest)
		Utils.Do(aircraft01, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				HuntObjectiveTruck(p)
			end)
		end)

		local powerproxy02 = Actor.Create("powerproxy.paratroopers", true, { Owner = GoodGuy })
		local aircraft02 = powerproxy02.TargetParatroopers(AlliedParadropLZ02.CenterPosition, Angle.SouthWest)
		Utils.Do(aircraft02, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				HuntObjectiveTruck(p)
			end)
		end)
	end)

	Trigger.AfterDelay(0, function()
		BridgeEnd = Map.ActorsInBox(AlliedHuntingPartySpawn.CenterPosition, AlliedHuntingPartyWP01.CenterPosition, function(self) return self.Type == "br2" end)[1]
		Trigger.OnKilled(BridgeEnd, function()
			BridgeIsIntact = false
			if not BridgeBarrel01.IsDead then BridgeBarrel01.Kill() end
			if not BridgeBarrel03.IsDead then BridgeBarrel03.Kill() end
			USSR.MarkCompletedObjective(PreventAlliedIncursions)
			Media.PlaySpeechNotification(USSR, "ObjectiveMet")
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Media.DisplayMessage(UserInterface.Translate("allied-ground-reinforcements-stopped"))
			end)
		end)
	end)

	Trigger.OnAnyKilled({ BridgeBarrel01, BridgeBarrel03 }, function()
		if not BridgeEnd.IsDead then
			BridgeEnd.Kill()
		end
	end)

	Trigger.OnAnyKilled(AlliedSquad04, function()
		if BridgeIsIntact then
			local tanks = Reinforcements.Reinforce(Greece, AlliedTanksReinforcement, { AlliedHuntingPartySpawn.Location, AlliedHuntingPartyWP01.Location }, 0, function(units)
				AlliedGroundPatrols(units)
			end)
			Trigger.OnAllKilled(tanks, function()
				if BridgeIsIntact then
					Reinforcements.Reinforce(Greece, AlliedTanksReinforcement, { AlliedHuntingPartySpawn.Location, AlliedHuntingPartyWP01.Location }, 0, function(units)
					AlliedGroundPatrols(units)
					end)
				end
			end)
		end
	end)

	Trigger.OnAllKilled(AlliedSquad04, function()
		if BridgeIsIntact then
			local tanks = Reinforcements.Reinforce(Greece, AlliedTanksReinforcement, { AlliedHuntingPartySpawn.Location, AlliedHuntingPartyWP01.Location }, 0, function(units)
				AlliedGroundPatrols(units)
			end)
			Trigger.OnAllKilled(tanks, function()
				if BridgeIsIntact then
					Reinforcements.Reinforce(Greece, AlliedTanksReinforcement, { AlliedHuntingPartySpawn.Location, AlliedHuntingPartyWP01.Location }, 0, function(units)
					AlliedGroundPatrols(units)
					end)
				end
			end)
		end
	end)

--Units Death Setup
	Trigger.AfterDelay(DateTime.Seconds(660), function()
		Utils.Do(USSRDie01, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(744), function()
		Utils.Do(USSRDie02, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1122), function()
		if not USSRHTank03.IsDead then USSRHTank03.Kill("DefaultDeath") end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1230), function()
		Utils.Do(USSRDie03, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1338), function()
		Utils.Do(USSRDie04, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1416), function()
		Utils.Do(USSRDie05, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1668), function()
		if not USSRV201.IsDead then USSRV201.Kill("DefaultDeath") end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1746), function()
		Utils.Do(USSRDie06, function(actor)
			if not actor.IsDead then actor.Kill("DefaultDeath") end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2034), function()
		if not USSRMTank02.IsDead then USSRMTank02.Kill("DefaultDeath") end
	end)

	Trigger.AfterDelay(DateTime.Seconds(2142), function()
		if not USSRMTank01.IsDead then USSRMTank01.Kill("DefaultDeath") end
	end)

	Trigger.OnTimerExpired(function()
		if not ObjectiveTruck01.IsDead then
			ObjectiveTruck01.Kill("DefaultDeath")

			-- Set the limit to one so that the timer displays 0 and never ends
			-- (which would display the game time instead of 0)
			DateTime.TimeLimit = 1
		end
	end)
end
