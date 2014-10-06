NodUnits = { "e1", "e1", "bggy", "bike", "e1", "e1", "bike", "bggy", "e1", "e1" }
Engineers = { "e6", "e6", "e6" }
FirstAttackWaveUnits  = { "e1", "e1", "e2" }
SecondAttackWaveUnits = { "e1", "e1", "e1" }
ThirdAttackWaveUnits = { "e1", "e1", "e1", "e2" }

SendAttackWave = function(units, action)
	Reinforcements.Reinforce(enemy, units, { GDIBarracksSpawn.Location, WP0.Location, WP1.Location }, 15, action)
end

FirstAttackWave = function(soldier)
	soldier.Move(WP2.Location)
	soldier.Move(WP3.Location)
	soldier.Move(WP4.Location)
	soldier.AttackMove(PlayerBase.Location)
end

SecondAttackWave = function(soldier)
	soldier.Move(WP5.Location)
	soldier.Move(WP6.Location)
	soldier.Move(WP7.Location)
	soldier.Move(WP9.Location)
	soldier.AttackMove(PlayerBase.Location)
end

InsertNodUnits = function()
	Reinforcements.Reinforce(player, { "mcv" }, { McvEntry.Location, McvDeploy.Location })
	Reinforcements.Reinforce(player, NodUnits, { NodEntry.Location, NodRallypoint.Location })
	Trigger.AfterDelay(Utils.Seconds(15), function()
		Reinforcements.Reinforce(player, Engineers, { McvEntry.Location, PlayerBase.Location })
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("desflees.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("flag.vqa")
		end)
	end)

	gdiObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area")
	nodObjective1 = player.AddPrimaryObjective("Capture the prison")
	nodObjective2 = player.AddSecondaryObjective("Destroy all GDI forces")

	Trigger.OnKilled(TechCenter, function() player.MarkFailedObjective(nodObjective1) end)
	Trigger.OnCapture(TechCenter, function()
		Trigger.AfterDelay(Utils.Seconds(2), function()
			player.MarkCompletedObjective(nodObjective1)
		end)
	end)

	InsertNodUnits()
	Trigger.AfterDelay(Utils.Seconds(40), function() SendAttackWave(FirstAttackWaveUnits, FirstAttackWave) end)
	Trigger.AfterDelay(Utils.Seconds(80), function() SendAttackWave(SecondAttackWaveUnits, SecondAttackWave) end)
	Trigger.AfterDelay(Utils.Seconds(140), function() SendAttackWave(ThirdAttackWaveUnits, FirstAttackWave) end)

	Media.PlayMovieFullscreen("nod3.vqa")
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(gdiObjective)
	end

	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(nodObjective2)
	end
end
