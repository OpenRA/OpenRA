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

if Map.LobbyOption("difficulty") == "easy" then
	TanyaType = "e7"
else
	TanyaType = "e7.noautotarget"
end

IdleHunt = function(actor)
	Trigger.OnIdle(actor, function(a)
		if a.IsInWorld then
			a.Hunt()
		end
	end)
end

ProduceUnits = function(factory, count)
	if ussr.IsProducing("e1") then
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
		ussr.Build(units, function(soldiers)
			Utils.Do(soldiers, function(unit) IdleHunt(unit) end)
		end)
	end
end

SendAlliedUnits = function()
	Camera.Position = TanyaWaypoint.CenterPosition

	local Artillery = Actor.Create("arty", true, { Owner = player, Location = AlliedUnitsEntry.Location })
	local Tanya = Actor.Create(TanyaType, true, { Owner = player, Location = AlliedUnitsEntry.Location })

	if TanyaType == "e7.noautotarget" then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
		end)
	end
	Artillery.Stance = "HoldFire"

	Tanya.Move(TanyaWaypoint.Location)
	Artillery.Move(ArtilleryWaypoint.Location)

	Trigger.OnKilled(Tanya, function() player.MarkFailedObjective(TanyaSurvive) end)
end

SendUSSRParadrops = function()
	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = ussr })
	local unitsA = powerproxy.SendParatroopers(ParadropLZ.CenterPosition, false, 128 + 32)
	local unitsB = powerproxy.SendParatroopers(ParadropLZ.CenterPosition, false, 128 - 32)

	Utils.Do(unitsA, function(unit)
		IdleHunt(unit)
	end)
	Utils.Do(unitsB, function(unit)
		IdleHunt(unit)
	end)

	powerproxy.Destroy()
end

SendUSSRWaterTransport = function()
	local units = Reinforcements.ReinforceWithTransport(ussr, "lst", TransportReinforcements, { WaterTransportEntry.Location, WaterTransportLoadout.Location }, { WaterTransportExit.Location })[2]
	Utils.Do(units, function(unit) IdleHunt(unit) end)
end

SendUSSRTankReinforcements = function()
	local camera = Actor.Create("camera", true, { Owner = player, Location = USSRReinforcementsCameraWaypoint.Location })
	local ussrTank = Reinforcements.Reinforce(ussr, { "3tnk" }, { USSRReinforcementsEntryWaypoint.Location, USSRReinforcementsRallyWaypoint1.Location, USSRReinforcementsRallyWaypoint2.Location })[1]
	Trigger.OnRemovedFromWorld(ussrTank, function()
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			if not	camera.IsDead then
				camera.Destroy()
			end
		end)
	end)
end

InitPlayers = function()
	player = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")

	ussr.Cash = 10000
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillBridges = player.AddPrimaryObjective("Destroy all bridges.")
	TanyaSurvive = player.AddPrimaryObjective("Tanya must survive.")
	KillUSSR = player.AddSecondaryObjective("Destroy all Soviet oil pumps.")
	FreePrisoners = player.AddSecondaryObjective("Free all Allied soldiers and keep them alive.")
	ussr.AddPrimaryObjective("Bridges must not be destroyed.")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionAccomplished")
		end)
	end)
end

InitTriggers = function()
	Utils.Do(ussr.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Trigger.OnAnyKilled(Prisoners, function() player.MarkFailedObjective(FreePrisoners) end)

	Trigger.OnKilled(USSRTechCenter, function()
		Actor.Create("moneycrate", true, { Owner = ussr, Location = USSRMoneyCrateSpawn.Location })
	end)

	Trigger.OnKilled(ExplosiveBarrel, function()

		-- We need the first bridge which is returned
		local bridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)[1]

		if not bridge.IsDead then
			bridge.Kill()
		end
	end)

	Utils.Do(FirstUSSRBase, function(unit)
		Trigger.OnDamaged(unit, function()
			if not FirstBaseAlert then
				FirstBaseAlert = true
				if not baseCamera then -- TODO: remove the Trigger
					baseCamera = Actor.Create("camera", true, { Owner = player, Location = BaseCameraWaypoint.Location })
				end
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
		end)
	end)
	Trigger.OnAllRemovedFromWorld(FirstUSSRBase, function() -- The camera can remain when one building is captured
		if baseCamera then baseCamera.Destroy() end
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
						Media.PlaySoundNotification(player, "AlertBuzzer")
					end)
				end
				ProduceUnits(ProductionBuildings[2], Utils.RandomInteger(5, 7))
			end
		end)
	end)

	Trigger.OnCapture(USSRRadarDome, function(self)
		largeCamera = Actor.Create("camera.verylarge", true, { Owner = player, Location = LargeCameraWaypoint.Location })
		Trigger.ClearAll(self)
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Trigger.OnRemovedFromWorld(self, function()
				Trigger.ClearAll(self)
				if largeCamera.IsInWorld then largeCamera.Destroy() end
			end)
		end)
	end)

	Trigger.OnEnteredFootprint(CameraTriggerArea, function(a, id)
		if a.Owner == player and not baseCamera then
			Trigger.RemoveFootprintTrigger(id)
			baseCamera = Actor.Create("camera", true, { Owner = player, Location = BaseCameraWaypoint.Location })
		end
	end)
	Trigger.OnEnteredFootprint(WaterTransportTriggerArea, function(a, id)
		if a.Owner == player and not waterTransportTriggered then
			waterTransportTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendUSSRWaterTransport()
		end
	end)
	Trigger.OnEnteredFootprint(ParadropTriggerArea, function(a, id)
		if a.Owner == player and not paradropsTriggered then
			paradropsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendUSSRParadrops()
		end
	end)
	Trigger.OnEnteredFootprint(ReinforcementsTriggerArea, function(a, id)
		if a.Owner == player and not reinforcementsTriggered then
			reinforcementsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			Trigger.AfterDelay(DateTime.Seconds(1), function() SendUSSRTankReinforcements() end)
		end
	end)

	Trigger.AfterDelay(0, function()
		local bridges = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge1" end)

		Trigger.OnAllKilled(bridges, function()
			player.MarkCompletedObjective(KillBridges)
			player.MarkCompletedObjective(TanyaSurvive)
			player.MarkCompletedObjective(FreePrisoners)
		end)

		local oilPumps = ussr.GetActorsByType("v19")

		Trigger.OnAllKilled(oilPumps, function()
			player.MarkCompletedObjective(KillUSSR)
		end)
	end)
end

WorldLoaded = function()

	InitPlayers()

	InitObjectives()
	InitTriggers()
	SendAlliedUnits()
end
