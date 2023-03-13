--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
ProductionUnits = { "e1", "e1", "e2" }
ProductionBuildings = { USSRBarracks1, USSRBarracks2 }
TransportReinforcements = { "e1", "e1", "e1", "e2", "e2" }
FirstUSSRBase = { USSRFlameTower1, USSRBarracks1, USSRPowerPlant1, USSRPowerPlant2, USSRConstructionYard1, USSRTechCenter, USSRBaseGuard1, USSRBaseGuard2, USSRBaseGuard3, USSRBaseGuard4, USSRBaseGuard5, USSRBaseGuard6, USSRBaseGuard7, USSRBaseGuard8 }
SecondUSSRBase = { USSRBarracks2, USSRKennel, USSRRadarDome, USSRBaseGuard10, USSRBaseGuard11, USSRBaseGuard12, USSRBaseGuard13, USSRBaseGuard14 }
Prisoners = { PrisonedMedi1, PrisonedMedi2, PrisonedEngi }
CameraTriggerArea = { CPos.New(43, 64), CPos.New(44, 64), CPos.New(45, 64), CPos.New(46, 64), CPos.New(47, 64) }
WaterTransportTriggerArea = { CPos.New(39, 54), CPos.New(40, 54), CPos.New(41, 54), CPos.New(42, 54), CPos.New(43, 54), CPos.New(44, 54), CPos.New(45, 54) }
ParadropTriggerArea = { CPos.New(81, 60), CPos.New(82, 60), CPos.New(83, 60), CPos.New(63, 63), CPos.New(64, 63), CPos.New(65, 63), CPos.New(66, 63), CPos.New(67, 63), CPos.New(68, 63), CPos.New(69, 63), CPos.New(70, 63), CPos.New(71, 63), CPos.New(72, 63) }
ReinforcementsTriggerArea = { CPos.New(96, 55), CPos.New(97, 55), CPos.New(97, 56), CPos.New(98, 56) }

if Difficulty == "easy" then
	TanyaType = "e7"
else
	TanyaType = "e7.noautotarget"
end

ProduceUnits = function(factory, count)
	if USSR.IsProducing("e1") then
		Trigger.AfterDelay(DateTime.Seconds(5), function() ProduceUnits(factory, count) end)
		return
	end

	local units = { }
	for i = 0, count, 1 do
		local type = Utils.Random(ProductionUnits)
		units[i] = type
	end

	if not factory.IsDead then
		factory.IsPrimaryBuilding = true
		USSR.Build(units, function(soldiers)
			Utils.Do(soldiers, function(unit) IdleHunt(unit) end)
		end)
	end
end

SendAlliedUnits = function()
	Camera.Position = TanyaWaypoint.CenterPosition

	local Artillery = Actor.Create("arty", true, { Owner = Greece, Location = AlliedUnitsEntry.Location })
	local Tanya = Actor.Create(TanyaType, true, { Owner = Greece, Location = AlliedUnitsEntry.Location })

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.DisplayMessage(UserInterface.Translate("tanya-rules-of-engagement"), UserInterface.Translate("tanya"))
		end)
	end
	Artillery.Stance = "HoldFire"

	Tanya.Move(TanyaWaypoint.Location)
	Artillery.Move(ArtilleryWaypoint.Location)

	Trigger.OnKilled(Tanya, function() Greece.MarkFailedObjective(TanyaSurvive) end)
end

SendUSSRParadrops = function()
	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })

	local aircraftA = powerproxy.TargetParatroopers(ParadropLZ.CenterPosition, Angle.SouthEast)
	Utils.Do(aircraftA, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	local aircraftB = powerproxy.TargetParatroopers(ParadropLZ.CenterPosition, Angle.SouthWest)
	Utils.Do(aircraftB, function(a)
		Trigger.OnPassengerExited(a, function(t, p)
			IdleHunt(p)
		end)
	end)

	powerproxy.Destroy()
end

SendUSSRWaterTransport = function()
	local units = Reinforcements.ReinforceWithTransport(USSR, "lst", TransportReinforcements, { WaterTransportEntry.Location, WaterTransportLoadout.Location }, { WaterTransportExit.Location })[2]
	Utils.Do(units, function(unit) IdleHunt(unit) end)
end

SendUSSRTankReinforcements = function()
	local camera = Actor.Create("camera", true, { Owner = Greece, Location = USSRReinforcementsCameraWaypoint.Location })
	local ussrTank = Reinforcements.Reinforce(USSR, { "3tnk" }, { USSRReinforcementsEntryWaypoint.Location, USSRReinforcementsRallyWaypoint1.Location, USSRReinforcementsRallyWaypoint2.Location })[1]
	Trigger.OnRemovedFromWorld(ussrTank, function()
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			if not	camera.IsDead then
				camera.Destroy()
			end
		end)
	end)
end

InitPlayers = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	USSR.Cash = 10000
end

AddObjectives = function()
	KillBridges = AddPrimaryObjective(Greece, "destroy-bridges")
	TanyaSurvive = AddPrimaryObjective(Greece, "tanya-survive")
	KillUSSR = AddSecondaryObjective(Greece, "destroy-oilpumps")
	FreePrisoners = AddSecondaryObjective(Greece, "free-prisoners")
end

InitTriggers = function()
	Utils.Do(USSR.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Trigger.OnAnyKilled(Prisoners, function() Greece.MarkFailedObjective(FreePrisoners) end)

	Trigger.OnKilled(USSRTechCenter, function()
		Actor.Create("moneycrate", true, { Owner = USSR, Location = USSRMoneyCrateSpawn.Location })
	end)

	Trigger.OnKilled(ExplosiveBarrel, function()

		-- We need the first bridge which is returned
		local bridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)[1]

		if not bridge.IsDead then
			bridge.Kill()
		end
	end)

	local baseTrigger = Trigger.OnEnteredFootprint(CameraTriggerArea, function(a, id)
		if a.Owner == Greece and not BaseCamera then
			Trigger.RemoveFootprintTrigger(id)
			BaseCamera = Actor.Create("camera", true, { Owner = Greece, Location = BaseCameraWaypoint.Location })
		end
	end)

	Utils.Do(FirstUSSRBase, function(unit)
		Trigger.OnDamaged(unit, function()
			if not FirstBaseAlert then
				FirstBaseAlert = true
				if not BaseCamera then
					BaseCamera = Actor.Create("camera", true, { Owner = Greece, Location = BaseCameraWaypoint.Location })
					Trigger.RemoveFootprintTrigger(baseTrigger)
				end
				Utils.Do(FirstUSSRBase, function(unit)
					if unit.HasProperty("Move") then
						IdleHunt(unit)
					end
				end)
				for i = 0, 2 do
					Trigger.AfterDelay(DateTime.Seconds(i), function()
						Media.PlaySoundNotification(Greece, "AlertBuzzer")
					end)
				end
				ProduceUnits(ProductionBuildings[1], Utils.RandomInteger(4, 8))
			end
		end)
	end)
	Trigger.OnAllKilledOrCaptured(FirstUSSRBase, function()
		if BaseCamera and BaseCamera.IsInWorld then
			BaseCamera.Destroy()
		end
	end)

	Utils.Do(SecondUSSRBase, function(unit)
		Trigger.OnDamaged(unit, function()
			if not SecondBaseAlert then
				SecondBaseAlert = true
				Utils.Do(SecondUSSRBase, function(unit)
					if unit.HasProperty("Move") then
						IdleHunt(unit)
					end
				end)
				for i = 0, 2 do
					Trigger.AfterDelay(DateTime.Seconds(i), function()
						Media.PlaySoundNotification(Greece, "AlertBuzzer")
					end)
				end
				ProduceUnits(ProductionBuildings[2], Utils.RandomInteger(5, 7))
			end
		end)
	end)

	Trigger.OnCapture(USSRRadarDome, function(self)
		local largeCamera = Actor.Create("camera.verylarge", true, { Owner = Greece, Location = LargeCameraWaypoint.Location })
		Trigger.ClearAll(self)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Trigger.OnRemovedFromWorld(self, function()
				Trigger.ClearAll(self)
				if largeCamera.IsInWorld then largeCamera.Destroy() end
			end)
		end)
	end)

	Trigger.OnEnteredFootprint(WaterTransportTriggerArea, function(a, id)
		if a.Owner == Greece and not WaterTransportTriggered then
			WaterTransportTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendUSSRWaterTransport()
		end
	end)
	Trigger.OnEnteredFootprint(ParadropTriggerArea, function(a, id)
		if a.Owner == Greece and not ParadropsTriggered then
			ParadropsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendUSSRParadrops()
		end
	end)
	Trigger.OnEnteredFootprint(ReinforcementsTriggerArea, function(a, id)
		if a.Owner == Greece and not ReinforcementsTriggered then
			ReinforcementsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			Trigger.AfterDelay(DateTime.Seconds(1), function() SendUSSRTankReinforcements() end)
		end
	end)

	Trigger.AfterDelay(0, function()
		local bridges = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)

		Trigger.OnAllKilled(bridges, function()
			Greece.MarkCompletedObjective(KillBridges)
			Greece.MarkCompletedObjective(TanyaSurvive)

			-- The prisoners are free once their guards are dead
			if PGuard1.IsDead and PGuard2.IsDead then
				Greece.MarkCompletedObjective(FreePrisoners)
			end
		end)

		local oilPumps = USSR.GetActorsByType("v19")

		Trigger.OnAllKilled(oilPumps, function()
			Greece.MarkCompletedObjective(KillUSSR)
		end)
	end)

	Trigger.OnKilled(Jail1Barrel, function()
		Jail1.Destroy()
	end)
	Trigger.OnKilled(Jail2Barrel, function()
		Jail2.Destroy()
	end)
end

WorldLoaded = function()

	InitPlayers()

	InitObjectives(Greece)
	AddObjectives()
	InitTriggers()
	SendAlliedUnits()
end
