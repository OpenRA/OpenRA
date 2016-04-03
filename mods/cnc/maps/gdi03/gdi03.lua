SamSites = { Sam1, Sam2, Sam3, Sam4 }
Sam4Guards = { Sam4Guard0, Sam4Guard1, Sam4Guard2, Sam4Guard3, Sam4Guard4, HiddenBuggy }
NodInfantrySquad = { "e1", "e1", "e1", "e1", "e1" }
NodAttackRoutes = { { AttackWaypoint }, { AttackWaypoint }, { AttackRallypoint1, AttackRallypoint2, AttackWaypoint } }
InfantryReinforcements = { "e1", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e2" }
JeepReinforcements = { "jeep", "jeep", "jeep" }

AttackPlayer = function()
	if NodBarracks.IsDead or NodBarracks.Owner == player then
		return
	end

	local after = function(team)
		local count = 1
		local route = Utils.Random(NodAttackRoutes)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, function()
				if actor.Location == route[count].Location then
					if not count == #route then
						count = count + 1
					else
						Trigger.ClearAll(actor)
						Trigger.AfterDelay(0, function()
							Trigger.OnIdle(actor, actor.Hunt)
						end)
					end
				else
					actor.AttackMove(route[count].Location)
				end
			end)
		end)
		Trigger.OnAllKilled(team, function() Trigger.AfterDelay(DateTime.Seconds(15), AttackPlayer) end)
	end

	NodBarracks.Build(NodInfantrySquad, after)
end

SendReinforcements = function()
	Reinforcements.Reinforce(player, JeepReinforcements, { VehicleStart.Location, VehicleStop.Location })
	Reinforcements.Reinforce(player, InfantryReinforcements, { InfantryStart.Location, InfantryStop.Location }, 5)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(player, { "mcv" }, { VehicleStart.Location, MCVwaypoint.Location })
		InitialUnitsArrived = true
	end)
	Media.PlaySpeechNotification(player, "Reinforce")
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	nodObjective = enemy.AddPrimaryObjective("Destroy all GDI troops.")
	gdiMainObjective = player.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	gdiAirSupportObjective = player.AddSecondaryObjective("Destroy the SAM sites to receive air support.")

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnAllKilled(SamSites, function()
		player.MarkCompletedObjective(gdiAirSupportObjective)
		Actor.Create("airstrike.proxy", true, { Owner = player })
	end)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == enemy and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == enemy and building.Health < 0.25 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Trigger.OnDamaged(Sam4, function()
		Utils.Do(Sam4Guards, function(sam4Guard)
			if not sam4Guard.IsDead then
				Trigger.OnIdle(sam4Guard, sam4Guard.Hunt)
			end
		end)
	end)

	InitialUnitsArrived = false
	SendReinforcements()

	Camera.Position = MCVwaypoint.CenterPosition

	Trigger.AfterDelay(DateTime.Seconds(15), AttackPlayer)

	Actor.Create("nuk2", true, { Owner = player, Location = CPos.New(16, 56) })
	local eye = Actor.Create("eye", true, { Owner = player, Location = CPos.New(20, 56) })

	local targetLoc = CPos.New(20, 30)
	Actor.Create("camera", true, { Owner = player, Location = targetLoc })
	Beacon.New(player, Map.CenterOfCell(targetLoc))
	Trigger.AfterDelay(DateTime.Seconds(6), function() eye.IonCannon(targetLoc) end)
end

Tick = function()
	if InitialUnitsArrived then
		if player.HasNoRequiredUnits() then
			enemy.MarkCompletedObjective(nodObjective)
		end
		if enemy.HasNoRequiredUnits() then
			player.MarkCompletedObjective(gdiMainObjective)
		end
	end
end
