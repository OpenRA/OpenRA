AutoTrigger = { CPos.New(51, 47), CPos.New(52, 47), CPos.New(53, 47), CPos.New(54, 47) }
GDIHeliTrigger = { CPos.New(27, 55), CPos.New(27, 56), CPos.New(28, 56), CPos.New(28, 57), CPos.New(28, 58), CPos.New(28, 59)}

Nod1Units  = { "e1", "e1", "e3", "e3" }
Auto1Units = { "e1", "e1", "e3" }

KillsUntilReinforcements = 12
HeliDelay = { 83, 137, 211 }

GDIReinforcements = { "e2", "e2", "e2", "e2", "e2" }
GDIReinforcementsWaypoints = { GDIReinforcementsEntry.Location, GDIReinforcementsWP1.Location }

NodHelis = {
	{ delay = DateTime.Seconds(HeliDelay[1]), entry = { NodHeliEntry.Location, NodHeliLZ1.Location }, types = { "e1", "e1", "e3" } },
	{ delay = DateTime.Seconds(HeliDelay[2]), entry = { NodHeliEntry.Location, NodHeliLZ2.Location }, types = { "e1", "e1", "e1", "e1" } },
	{ delay = DateTime.Seconds(HeliDelay[3]), entry = { NodHeliEntry.Location, NodHeliLZ3.Location }, types = { "e1", "e1", "e3" } }
}

SendHeli = function(heli)
	units = Reinforcements.ReinforceWithTransport(enemy, "tran", heli.types, heli.entry, { heli.entry[1] })
	Utils.Do(units[2], function(actor)
		actor.Hunt()
		Trigger.OnIdle(actor, actor.Hunt)
		Trigger.OnKilled(actor, KillCounter)
	end)
	Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.ReinforceWithTransport(player, "apc", GDIReinforcements, GDIReinforcementsWaypoints, nil, function(apc, team)
		table.insert(team, apc)
		Trigger.OnAllKilled(team, function() Trigger.AfterDelay(DateTime.Seconds(5), SendGDIReinforcements) end)
		Utils.Do(team, function(unit) unit.Stance = "Defend" end)
	end)
end

BuildNod1 = function()
	if HandOfNod.IsDead then
		return
	end

	local func = function(team)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, actor.Hunt)
			Trigger.OnKilled(actor, KillCounter)
		end)
		Trigger.OnAllKilled(team, BuildNod1)
	end

	if not HandOfNod.Build(Nod1Units, func) then
		Trigger.AfterDelay(DateTime.Seconds(5), BuildNod1)
	end
end

BuildAuto1 = function()
	if HandOfNod.IsDead then
		return
	end

	local func = function(team)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, actor.Hunt)
			Trigger.OnKilled(actor, KillCounter)
		end)
	end

	if not HandOfNod.IsDead and HandOfNod.Build(Auto1Units, func) then
		Trigger.AfterDelay(DateTime.Seconds(5), BuildAuto1)
	end
end

kills = 0
KillCounter = function() kills = kills + 1 end

ReinforcementsSent = false
Tick = function()
	enemy.Cash = 1000

	if not ReinforcementsSent and kills >= KillsUntilReinforcements then
		ReinforcementsSent = true
		player.MarkCompletedObjective(reinforcementsObjective)
		SendGDIReinforcements()
	end

	if player.HasNoRequiredUnits() then
		Trigger.AfterDelay(DateTime.Seconds(1), function() player.MarkFailedObjective(gdiObjective) end)
	end
end

SetupWorld = function()
	Utils.Do(enemy.GetGroundAttackers(enemy), function(unit)
		Trigger.OnKilled(unit, KillCounter)
	end)

	Utils.Do(player.GetGroundAttackers(), function(unit)
		unit.Stance = "Defend"
	end)

	Hunter1.Hunt()
	Hunter2.Hunt()

	Trigger.OnRemovedFromWorld(crate, function() player.MarkCompletedObjective(gdiObjective) end)
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	SetupWorld()

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
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)


	gdiObjective = player.AddPrimaryObjective("Retrieve the crate with the stolen rods.")
	reinforcementsObjective = player.AddSecondaryObjective("Eliminate " .. KillsUntilReinforcements .. " Nod units for reinforcements.")
	enemy.AddPrimaryObjective("Defend against the GDI forces.")

	BuildNod1()
	Utils.Do(NodHelis, function(heli)
		Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
	end)

	autoTrigger = false
	Trigger.OnEnteredFootprint(AutoTrigger, function(a, id)
		if not autoTrigger and a.Owner == player then
			autoTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			BuildAuto1()
		end
	end)

	gdiHeliTrigger = false
	Trigger.OnEnteredFootprint(GDIHeliTrigger, function(a, id)
		if not gdiHeliTrigger and a.Owner == player then
			gdiHeliTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.ReinforceWithTransport(player, "tran", nil, { GDIHeliEntry.Location, GDIHeliLZ.Location })
		end
	end)

	Camera.Position = Actor56.CenterPosition
end
