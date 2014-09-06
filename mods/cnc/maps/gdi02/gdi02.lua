nodInBaseTeam = { RushBuggy, RushRifle1, RushRifle2, RushRifle3 }
MobileConstructionVehicle = { "mcv" }
EngineerReinforcements = { "e6", "e6", "e6" }
VehicleReinforcements = { "jeep" }

AttackerSquadSize = 3

Reinforce = function(passengers)
	Reinforcements.ReinforceWithTransport(player, "oldlst", passengers, { lstStart.Location, lstEnd.Location  }, { lstStart.Location })
	Media.PlaySpeechNotification(player, "Reinforce")
end

BridgeheadSecured = function()
	Reinforce(MobileConstructionVehicle)
	Trigger.AfterDelay(Utils.Seconds(15), NodAttack)
	Trigger.AfterDelay(Utils.Seconds(30), function() Reinforce(EngineerReinforcements) end)
	Trigger.AfterDelay(Utils.Seconds(60), function() Reinforce(VehicleReinforcements) end)
end

NodAttack = function()
	local nodUnits = enemy.GetGroundAttackers()
	if #nodUnits > AttackerSquadSize * 2 then
		local attackers = Utils.Skip(nodUnits, #nodUnits - AttackerSquadSize)
		Utils.Do(attackers, function(unit)
			unit.AttackMove(waypoint2.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
		Trigger.OnAllKilled(attackers, function() Trigger.AfterDelay(Utils.Seconds(15), NodAttack) end)
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	nodObjective = enemy.AddPrimaryObjective("Destroy all GDI troops")
	gdiObjective1 = player.AddPrimaryObjective("Eliminate all Nod forces in the area")
	gdiObjective2 = player.AddSecondaryObjective("Capture the Tiberium Refinery")

	Trigger.OnObjectiveCompleted(player, function() Media.DisplayMessage("Objective completed") end)
	Trigger.OnObjectiveFailed(player, function() Media.DisplayMessage("Objective failed") end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("flag.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	Trigger.OnCapture(NodRefinery, function() player.MarkCompletedObjective(gdiObjective2) end)
	Trigger.OnKilled(NodRefinery, function() player.MarkFailedObjective(gdiObjective2) end)

	Trigger.OnAllKilled(nodInBaseTeam, BridgeheadSecured)

	Media.PlayMovieFullscreen("gdi2.vqa")
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(nodObjective)
	end
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(gdiObjective1)
	end
end
