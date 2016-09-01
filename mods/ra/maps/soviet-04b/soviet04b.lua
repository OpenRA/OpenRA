
RunInitialActivities = function()
	Harvester.FindResources()
	Helper.Destroy()
	IdlingUnits()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		BringPatrol1()
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			BringPatrol2()
		end)
		BuildBase()
	end)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Trigger.OnKilled(Powr, function(building)
		BasePower.exists = false
	end)

	Trigger.OnKilled(Barr, function(building)
		BaseBarracks.exists = false
	end)

	Trigger.OnKilled(Proc, function(building)
		BaseProc.exists = false
	end)

	Trigger.OnKilled(Weap, function(building)
		BaseWeaponsFactory.exists = false
	end)

	Trigger.OnEnteredFootprint(VillageCamArea, function(actor, id)
		if actor.Owner == player then
			Trigger.RemoveFootprintTrigger(id)

			if not AllVillagersDead then
				VillageCamera = Actor.Create("camera", true, { Owner = player, Location = VillagePoint.Location })
			end
		end
	end)

	Trigger.OnAllKilled(Village, function()
		if VillageCamera then
			VillageCamera.Destroy()
		end
		AllVillagersDead = true
	end)

	Trigger.OnAnyKilled(Civs, function()
		Trigger.ClearAll(civ1)
		Trigger.ClearAll(civ2)
		Trigger.ClearAll(civ3)
		Trigger.ClearAll(civ4)
		local units = Reinforcements.Reinforce(Greece, Avengers, { NRoadPoint.Location }, 0)
		Utils.Do(units, function(unit)
			unit.Hunt()
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)

	if Map.LobbyOption("difficulty") == "hard" or Map.LobbyOption("difficulty") == "normal" then
		Trigger.AfterDelay(DateTime.Seconds(5), ReinfInf)
	end
	Trigger.AfterDelay(DateTime.Minutes(1), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(3), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(2), ReinfArmor)
end

Tick = function()
	if Greece.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillAll)
		player.MarkCompletedObjective(KillRadar)
	end

	if player.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(BeatUSSR)
	end

	if Greece.Resources >= Greece.ResourceCapacity * 0.75 then
		Greece.Cash = Greece.Cash + Greece.Resources - Greece.ResourceCapacity * 0.25
		Greece.Resources = Greece.ResourceCapacity * 0.25
	end

	if RCheck then
		RCheck = false
		if Map.LobbyOption("difficulty") == "hard" then
			Trigger.AfterDelay(DateTime.Seconds(150), ReinfArmor)
		elseif Map.LobbyOption("difficulty") == "normal" then
			Trigger.AfterDelay(DateTime.Minutes(5), ReinfArmor)
		else
			Trigger.AfterDelay(DateTime.Minutes(8), ReinfArmor)
		end
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	RunInitialActivities()

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	KillAll = player.AddPrimaryObjective("Defeat the Allied forces.")
	BeatUSSR = Greece.AddPrimaryObjective("Defeat the Soviet forces.")
	KillRadar = player.AddSecondaryObjective("Destroy Allied Radar Dome to stop enemy\nreinforcements.")

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnKilled(Radar, function()
		player.MarkCompletedObjective(KillRadar)
		Media.PlaySpeechNotification(player, "ObjectiveMet")
	end)

	Trigger.OnDamaged(Harvester, function()
		Utils.Do(Guards, function(unit)
			if not unit.IsDead and not Harvester.IsDead then
				unit.AttackMove(Harvester.Location)
			end
		end)
	end)

	Camera.Position = StartCamPoint.CenterPosition
end
