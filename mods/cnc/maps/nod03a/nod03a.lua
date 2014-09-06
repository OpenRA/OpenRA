NodUnits = { "bike", "e3", "e1", "bggy", "e1", "e3", "bike", "bggy" }
FirstAttackWave = { "e1", "e1", "e1", "e2", }
SecondThirdAttackWave = { "e1", "e1", "e2", }

SendAttackWave = function(units, spawnPoint)
	Reinforcements.Reinforce(enemy, units, { spawnPoint }, Utils.Seconds(1), function(actor)
		actor.AttackMove(PlayerBase.Location)
	end)
end

InsertNodUnits = function()
	Reinforcements.Reinforce(player, NodUnits, { NodEntry.Location, NodRallyPoint.Location })
	Trigger.AfterDelay(Utils.Seconds(9), function()
		Reinforcements.Reinforce(player, { "mcv" }, { NodEntry.Location, PlayerBase.Location })
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	gdiObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area")
	nodObjective1 = player.AddPrimaryObjective("Capture the prison")
	nodObjective2 = player.AddSecondaryObjective("Destroy all GDI forces")

	InsertNodUnits()
	Trigger.AfterDelay(Utils.Seconds(20), function() SendAttackWave(FirstAttackWave, AttackWaveSpawnA.Location) end)
	Trigger.AfterDelay(Utils.Seconds(50), function() SendAttackWave(SecondThirdAttackWave, AttackWaveSpawnB.Location) end)
	Trigger.AfterDelay(Utils.Seconds(100), function() SendAttackWave(SecondThirdAttackWave, AttackWaveSpawnC.Location) end)

	Trigger.OnObjectiveCompleted(player, function() Media.DisplayMessage("Objective completed") end)
	Trigger.OnObjectiveFailed(player, function() Media.DisplayMessage("Objective failed") end)

	Trigger.OnCapture(TechCenter, function()
		Trigger.AfterDelay(Utils.Seconds(2), function()
			player.MarkCompletedObjective(nodObjective1)
		end)
	end)

	Trigger.OnKilled(TechCenter, function()
		player.MarkFailedObjective(nodObjective1)
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
