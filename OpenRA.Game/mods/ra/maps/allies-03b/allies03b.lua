--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
ProductionUnits = { "e1", "e1", "e2" }
ProductionBuildings = { USSRBarracks1, USSRBarracks2, USSRBarracks3 }
FirstUSSRBase = { USSRFlameTower1, USSRFlameTower2, USSRFlameTower3, USSRBarracks1, PGuard1, PGuard2, PGuard3, PGuard4, PGuard5 }
SecondUSSRBase = { USSRFlameTower4, USSRFlameTower5, USSRFlameTower6, USSRRadarDome, USSRBarracks2, USSRPowerPlant, USSRSubPen, USSRBaseGuard1, USSRBaseGuard2, uSSRBaseGuard3, MediGuard }
Prisoners = { PrisonedEngi1, PrisonedEngi2, PrisonedEngi3, PrisonedEngi4 }
PGuards = { PGuard1, PGuard2, PGuard3, PGuard4, PGuard5 }
AlliedIslandReinforcements = { "1tnk", "1tnk" }
USSRTankReinforcements = { "3tnk", "3tnk", "3tnk" }
USSRTankReinforcementsWaypoints = { USSRReinforcementsEntryWaypoint.Location, USSRReinforcementsCameraWaypoint.Location + CVec.New(1, -1), USSRReinforcementsRallyWaypoint.Location }
TrukTriggerArea = { CPos.New(51, 89), CPos.New(52, 89), CPos.New(53, 89), CPos.New(54, 89), CPos.New(55, 89), CPos.New(56, 89), CPos.New(57, 89) }
FreeMediTriggerArea = { CPos.New(56, 93), CPos.New(56, 94), CPos.New(57, 94), CPos.New(57, 95), CPos.New(57, 96), CPos.New(57, 97), CPos.New(57, 98), CPos.New(57, 99), CPos.New(57, 100), CPos.New(57, 101), CPos.New(57, 102) }
CameraTriggerArea = { CPos.New(73, 88), CPos.New(73, 87), CPos.New(76, 92), CPos.New(76, 93), CPos.New(76, 94) }
BeachTriggerArea = { CPos.New(111, 36), CPos.New(112, 36), CPos.New(112, 37), CPos.New(113, 37), CPos.New(113, 38), CPos.New(114, 38), CPos.New(114, 39), CPos.New(115, 39), CPos.New(116, 39), CPos.New(116, 40), CPos.New(117, 40), CPos.New(118, 40), CPos.New(119, 40), CPos.New(119, 41) }
ParadropTriggerArea = { CPos.New(81, 66), CPos.New(82, 66), CPos.New(83, 66), CPos.New(84, 66), CPos.New(85, 66), CPos.New(86, 66), CPos.New(87, 66), CPos.New(93, 64), CPos.New(94, 64), CPos.New(94, 63), CPos.New(95, 63), CPos.New(95, 62), CPos.New(96, 62), CPos.New(96, 61), CPos.New(97, 61), CPos.New(97, 60), CPos.New(98, 60), CPos.New(99, 60), CPos.New(100, 60), CPos.New(101, 60), CPos.New(102, 60), CPos.New(103, 60) }
ReinforcementsTriggerArea = { CPos.New(57, 46), CPos.New(58, 46), CPos.New(66, 35), CPos.New(65, 35), CPos.New(65, 36), CPos.New(64, 36), CPos.New(64, 37), CPos.New(64, 38), CPos.New(64, 39), CPos.New(64, 40), CPos.New(64, 41), CPos.New(63, 41), CPos.New(63, 42), CPos.New(63, 43), CPos.New(62, 43), CPos.New(62, 44) }
Barracks3TriggerArea = { CPos.New(69, 50), CPos.New(69, 51), CPos.New(69, 52), CPos.New(69, 53), CPos.New(69, 54), CPos.New(61, 45), CPos.New(62, 45), CPos.New(62, 46), CPos.New(62, 47), CPos.New(62, 48), CPos.New(63, 48), CPos.New(57, 46), CPos.New(58, 46) }
JeepTriggerArea = { CPos.New(75, 76), CPos.New(76, 76), CPos.New(77, 76), CPos.New(78, 76), CPos.New(79, 76), CPos.New(80, 76), CPos.New(81, 76), CPos.New(82, 76), CPos.New(91, 78), CPos.New(92, 78), CPos.New(93, 78), CPos.New(95, 84), CPos.New(96, 84), CPos.New(97, 84), CPos.New(98, 84), CPos.New(99, 84), CPos.New(100, 84) }
JeepBarrels = { JeepBarrel1, JeepBarrel2, JeepBarrel3, JeepBarrel4 }
GuardTanks = { Heavy1, Heavy2, Heavy3 }
CheckpointGuards = { USSRCheckpointGuard1, USSRCheckpointGuard2 }
CheckpointGuardWaypoints = { CheckpointGuardWaypoint1, CheckpointGuardWaypoint2 }

if Difficulty == "easy" then
	TanyaType = "e7"
else
	TanyaType = "e7.noautotarget"
end

Tick = function()
	if TeleportJeepCamera and Jeep.IsInWorld then
		JeepCamera.Teleport(Jeep.Location)
	end
end

ProduceUnits = function(factory, count)
	if ussr.IsProducing("e1") then
		Trigger.AfterDelay(DateTime.Seconds(5), function() ProduceUnits(factory, count) end)
		return
	end

	local units = { }
	for i = 0, count, 1 do
		units[i] = Utils.Random(ProductionUnits)
	end

	if not factory.IsDead then
		factory.IsPrimaryBuilding = true
		ussr.Build(units, function(soldiers)
			Utils.Do(soldiers, function(unit) IdleHunt(unit) end)
		end)
	end
end

SetupAlliedUnits = function()
	Tanya = Actor.Create(TanyaType, true, { Owner = player, Location = TanyaWaypoint.Location, Facing = Angle.South })

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end

	Camera.Position = Tanya.CenterPosition

	InsertionHeli.Wait(DateTime.Seconds(2))
	InsertionHeli.Move(InsertionHeliExit.Location)
	InsertionHeli.Destroy()

	Trigger.OnKilled(Tanya, function() player.MarkFailedObjective(TanyaSurvive) end)
end

SetupTopRightIsland = function()
	player.MarkCompletedObjective(FindAllies)
	Media.PlaySpeechNotification(player, "AlliedReinforcementsArrived")
	Reinforcements.Reinforce(player, AlliedIslandReinforcements, { AlliedIslandReinforcementsEntry.Location, IslandParadropReinforcementsDropzone.Location })
	SendUSSRParadrops(Angle.New(720), IslandParadropReinforcementsDropzone)
end

SendUSSRParadrops = function(facing, dropzone)
	local paraproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = ussr })

	local aircraft = paraproxy.TargetParatroopers(dropzone.CenterPosition, facing)
	Utils.Do(aircraft, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	paraproxy.Destroy()
end

SendUSSRTankReinforcements = function()
	local camera = Actor.Create("camera", true, { Owner = player, Location = USSRReinforcementsCameraWaypoint.Location })
	local ussrTanks = Reinforcements.Reinforce(ussr, USSRTankReinforcements, USSRTankReinforcementsWaypoints)
	Trigger.OnAllRemovedFromWorld(ussrTanks, function()
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			if not	camera.IsDead then
				camera.Destroy()
			end
		end)
	end)
end

JeepCheckpointMove = function()
	JeepCamera = Actor.Create("camera.jeep", true, { Owner = player })
	TeleportJeepCamera = true

	Trigger.OnIdle(Jeep, function()
		if Jeep.Location == JeepCheckpoint.Location then
			Trigger.ClearAll(Jeep)
			for i = 1, 2, 1 do
				if not CheckpointGuards[i].IsDead then
					CheckpointGuards[i].Move(CheckpointGuardWaypoints[i].Location)
				end
			end
		else
			Jeep.Move(JeepCheckpoint.Location)
		end
	end)
end

JeepSuicideMove = function()
	if not JeepCamera then
		JeepCamera = Actor.Create("camera.jeep", true, { Owner = player })
		TeleportJeepCamera = true
	end

	Trigger.OnIdle(Jeep, function()
		if Jeep.Location == JeepSuicideWaypoint.Location then
			Trigger.ClearAll(Jeep)
			TeleportJeepCamera = false
			Jeep.Kill()
			if not USSRFlameTower4.IsDead then USSRFlameTower4.Kill() end
			Trigger.AfterDelay(DateTime.Seconds(1), JeepCamera.Destroy)
		else
			Jeep.Move(JeepSuicideWaypoint.Location)
		end
	end)
end

AlertFirstBase = function()
	if not FirstBaseAlert then
		FirstBaseAlert = true
		Utils.Do(FirstUSSRBase, function(unit)
			if unit.HasProperty("Move") then
				IdleHunt(unit)
			end
		end)
		for i = 0, 2 do
			Trigger.AfterDelay(DateTime.Seconds(i), function()
				Media.PlaySoundNotification(player, "AlertBuzzer")
			end)
		end
		ProduceUnits(ProductionBuildings[1], Utils.RandomInteger(4, 8))
	end
end

InitPlayers = function()
	player = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")

	ussr.Cash = 10000
end

AddObjectives = function()
	InitObjectives(player)

	KillBridges = player.AddObjective("Destroy all bridges.")
	TanyaSurvive = player.AddObjective("Tanya must survive.")
	FindAllies = player.AddObjective("Find our lost tanks.", "Secondary", false)
	FreePrisoners = player.AddObjective("Free all Allied soldiers and keep them alive.", "Secondary", false)
	ussr.AddObjective("Bridges must not be destroyed.")
end

InitTriggers = function()
	Utils.Do(ussr.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Trigger.OnAnyKilled(Prisoners, function() player.MarkFailedObjective(FreePrisoners) end)
	Trigger.OnKilled(PrisonedMedi, function() player.MarkFailedObjective(FreePrisoners) end)
	Trigger.OnKilled(MediHideaway, function()
		if not MediFreed then
			MediFreed = true
			player.MarkFailedObjective(FreePrisoners)
		end
	end)

	Trigger.OnKilled(ExplosiveBarrel, function()
		if reinforcementsTriggered then
			return
		end

		if not ExplodingBridge.IsDead then ExplodingBridge.Kill() end
		reinforcementsTriggered = true
		Trigger.AfterDelay(DateTime.Seconds(1), SendUSSRTankReinforcements)
	end)

	Trigger.OnKilled(ExplosiveBarrel2, function()
		if not USSRFlameTower3.IsDead then USSRFlameTower3.Kill() end
	end)

	Trigger.OnAnyKilled(JeepBarrels, function()
		Utils.Do(JeepBarrels, function(barrel)
			if not barrel.IsDead then barrel.Kill() end
		end)
		Utils.Do(GuardTanks, function(tank)
			if not tank.IsDead then tank.Kill() end
		end)

		jeepTriggered = true
		JeepSuicideMove()
	end)

	Utils.Do(FirstUSSRBase, function(unit)
		Trigger.OnDamaged(unit, function()
			if not baseCamera then baseCamera = Actor.Create("camera", true, { Owner = player, Location = BaseCameraWaypoint.Location }) end
			AlertFirstBase()
		end)
	end)
	Trigger.OnAllKilledOrCaptured(FirstUSSRBase, function()
		if baseCamera and baseCamera.IsInWorld then
			baseCamera.Destroy()
		end
	end)

	Trigger.OnDamaged(USSRBarracks3, function()
		if not Barracks3Producing then
			Barracks3Producing = true
			ProduceUnits(ProductionBuildings[3], Utils.RandomInteger(2, 5))
		end
	end)

	Trigger.OnCapture(USSRRadarDome, function()
		largeCameraA = Actor.Create("camera.verylarge", true, { Owner = player, Location = LargeCameraWaypoint1.Location })
		largeCameraB = Actor.Create("camera.verylarge", true, { Owner = player, Location = LargeCameraWaypoint2.Location })
		largeCameraC = Actor.Create("camera.verylarge", true, { Owner = player, Location = LargeCameraWaypoint3.Location })
	end)
	Trigger.OnRemovedFromWorld(USSRRadarDome, function()
		if largeCameraA and largeCameraA.IsInWorld then largeCameraA.Destroy() end
		if largeCameraB and largeCameraB.IsInWorld then largeCameraB.Destroy() end
		if largeCameraC and largeCameraC.IsInWorld then largeCameraC.Destroy() end
	end)

	Trigger.OnEnteredFootprint(TrukTriggerArea, function(a, id)
		if a.Owner == player and not trukTriggered then
			trukTriggered = true
			Trigger.RemoveFootprintTrigger(id)

			if USSRTruk.IsDead then
				return
			end

			Trigger.OnIdle(USSRTruk, function()
				if USSRTruk.Location == BaseCameraWaypoint.Location then
					Trigger.ClearAll(USSRTruk)

					local driver = Actor.Create("e1", true, { Owner = ussr, Location = USSRTruk.Location })
					if not PGuard5.IsDead then
						driver.AttackMove(PGuard5.Location)
					else
						driver.Scatter()
					end

					FirstUSSRBase[#FirstUSSRBase + 1] = driver
					Trigger.AfterDelay(DateTime.Seconds(3), AlertFirstBase)
				else
					USSRTruk.Move(BaseCameraWaypoint.Location)
				end
			end)
			Trigger.OnEnteredProximityTrigger(BaseCameraWaypoint.CenterPosition, WDist.New(7 * 1024), function(a, id)
				if a.Type == "truk" and not baseCamera then
					Trigger.RemoveProximityTrigger(id)
					baseCamera = Actor.Create("camera", true, { Owner = player, Location = BaseCameraWaypoint.Location })
				end
			end)
		end
	end)
	Trigger.OnEnteredFootprint(FreeMediTriggerArea, function(a, id)
		if a.Owner == player and not MediFreed then
			MediFreed = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.Reinforce(player, { "medi" }, { MediSpawnpoint.Location, MediRallypoint.Location })
		end
	end)
	Trigger.OnEnteredFootprint(CameraTriggerArea, function(a, id)
		if a.Owner == player and not baseCamera then
			Trigger.RemoveFootprintTrigger(id)
			baseCamera = Actor.Create("camera", true, { Owner = player, Location = BaseCameraWaypoint.Location })
		end
	end)
	Trigger.OnEnteredFootprint(BeachTriggerArea, function(a, id)
		if a.Owner == player and not beachTransportTriggered then
			beachTransportTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SetupTopRightIsland()
		end
	end)
	Trigger.OnEnteredFootprint(ParadropTriggerArea, function(a, id)
		if a.Owner == player and a.Type ~= "jeep.mission" and not paradropsTriggered then
			paradropsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendUSSRParadrops(Angle.New(216), ParadropReinforcementsDropzone)
		end
	end)
	Trigger.OnEnteredFootprint(ReinforcementsTriggerArea, function(a, id)
		if a.Owner == player and not reinforcementsTriggered then
			reinforcementsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			Trigger.AfterDelay(DateTime.Seconds(1), SendUSSRTankReinforcements)
		end
	end)
	Trigger.OnEnteredFootprint(Barracks3TriggerArea, function(a, id)
		if a.Owner == player and not Barracks3Producing then
			Barracks3Producing = true
			Trigger.RemoveFootprintTrigger(id)
			ProduceUnits(ProductionBuildings[3], Utils.RandomInteger(2, 5))
		end
	end)
	Trigger.OnEnteredFootprint(JeepTriggerArea, function(a, id)
		if a.Owner == player and not jeepTriggered then
			jeepTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			JeepCheckpointMove()
		end
	end)

	-- The engineers need to leave the enemy base to count as 'freed'
	Trigger.OnExitedProximityTrigger(BaseCameraWaypoint.CenterPosition, WDist.New(7 * 1024), function(a, id)
		if a.Type == "e6" and not EngisFreed then
			EngisFreed = true
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.AfterDelay(0, function()
		local bridges = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" or actor.Type == "bridge2" end)
		ExplodingBridge = bridges[1]

		Trigger.OnAllKilled(bridges, function()
			player.MarkCompletedObjective(KillBridges)
			player.MarkCompletedObjective(TanyaSurvive)

			-- The medic is freed once his guard is dead
			if MediFreed and MediGuard.IsDead and EngisFreed then
				player.MarkCompletedObjective(FreePrisoners)
			end
		end)
	end)
end

WorldLoaded = function()

	InitPlayers()

	AddObjectives()
	InitTriggers()
	SetupAlliedUnits()
end
