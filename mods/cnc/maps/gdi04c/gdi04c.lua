LoseTriggerHouses = { TrigLos2Farm1, TrigLos2Farm2, TrigLos2Farm3, TrigLos2Farm4 }

GDIReinforcementsPart1 = { "jeep", "jeep" }
GDIReinforcementsPart2 = { "e2", "e2", "e2", "e2", "e2" }
TownAttackWave1 = { "bggy", "bggy" }
TownAttackWave2 = { "ltnk", "ltnk" }
TownAttackWave3 = { "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }
TownAttackWpts  = { waypoint1, waypoint2 }

Civvie1Wpts	= { waypoint3, waypoint17 }
Civvie2Wpts	= { waypoint26, waypoint3, waypoint9, waypoint4, waypoint5, waypoint6, waypoint8, waypoint7, waypoint1, waypoint2 }

FollowCivvieWpts = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		actor.Move(wpt.Location, 2)
		actor.Wait(Utils.Seconds(2))
	end)
end

FollowWaypoints = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		actor.AttackMove(wpt.Location, 2)
	end)
end

TownAttackersIdleAction = function(actor)
	actor.AttackMove(TownAttackWpt.Location, 2)
	actor.Hunt()
end

TownAttackAction = function(actor)
	Trigger.OnIdle(actor, TownAttackersIdleAction)
	FollowWaypoints(actor, TownAttackWpts)
end

AttackTown = function()
	TownAttackTriggered = true

	Reinforcements.Reinforce(nod, TownAttackWave1, { NodReinfEntry.Location, waypoint0.Location }, Utils.Seconds(0.25), TownAttackAction)
	Trigger.AfterDelay(Utils.Seconds(2), function()
		Reinforcements.Reinforce(nod, TownAttackWave2, { NodReinfEntry.Location, waypoint0.Location }, Utils.Seconds(1), TownAttackAction)
	end)
	Trigger.AfterDelay(Utils.Seconds(4), function()
		Reinforcements.Reinforce(nod, TownAttackWave3, { NodReinfEntry.Location, waypoint0.Location }, Utils.Seconds(1), TownAttackAction)
	end)
end

SendGDIReinforcements = function()
	GDIReinforcementsTriggered = true

	Reinforcements.Reinforce(player, GDIReinforcementsPart1, { GDIReinfEntry1.Location, waypoint12.Location }, Utils.Seconds(1), function(actor)
		Media.PlaySpeechNotification(player, "Reinforce")
		actor.Move(waypoint10.Location)
		actor.Stance = "Defend"
	end)
	Trigger.AfterDelay(Utils.Seconds(5), function()
		Reinforcements.ReinforceWithTransport(player, "apc", GDIReinforcementsPart2, { GDIReinfEntry2.Location, waypoint13.Location }, nil, function(apc, team)
			Media.PlaySpeechNotification(player, "Reinforce")
			apc.Move(GDIUnloadWpt.Location)
			apc.UnloadPassengers()
			Utils.Do(team, function(unit) unit.Stance = "Defend" end)
		end)
	end)
end

-- FIXME: replace with real cell trigger when available
CellTrigger = function(player, trigger, radius, func)
	local units = Map.ActorsInCircle(trigger.CenterPosition, WRange.FromCells(radius), function(actor)
		return actor.Owner == player and actor.HasProperty("Move")
	end)

	if #units > 0 then
		func()
	end
end

WorldLoaded = function()
	player	= Player.GetPlayer("GDI")
	nod	= Player.GetPlayer("Nod")

	nodObjective = nod.AddPrimaryObjective("Destroy all GDI troops")
	gdiObjective1 = player.AddPrimaryObjective("Defend the town of Białystok")
	gdiObjective2 = player.AddPrimaryObjective("Eliminate all Nod forces in the area")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnAllKilled(LoseTriggerHouses, function()
		player.MarkFailedObjective(gdiObjective1)
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("burdet1.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	Utils.Do(player.GetGroundAttackers(), function(unit)
		unit.Stance = "Defend"
	end)

	Trigger.AfterDelay(1, function()
		FollowCivvieWpts(civvie1, Civvie1Wpts)
		FollowCivvieWpts(civvie2, Civvie2Wpts)
	end)

	Camera.Position = Actor141.CenterPosition

	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("gdi4a.vqa", function() Media.PlayMovieFullscreen("nodsweep.vqa") end) end)
end

TownAttackTriggered = false
GDIReinforcementsTriggered = false
Tick = function()
	if player.HasNoRequiredUnits() then
		nod.MarkCompletedObjective(nodObjective)
	end
	if nod.HasNoRequiredUnits() then
		player.MarkCompletedObjective(gdiObjective1)
		player.MarkCompletedObjective(gdiObjective2)
	end

	if not TownAttackTriggered then
		CellTrigger(player, TownAttackTrigger, 2, AttackTown)
	elseif not GDIReinforcementsTriggered then
		CellTrigger(player, GDIReinfTrigger, 2, SendGDIReinforcements)
	end
end
