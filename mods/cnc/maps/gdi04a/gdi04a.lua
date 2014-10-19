AutoTrigger = { CPos.New(51, 47), CPos.New(52, 47), CPos.New(53, 47), CPos.New(54, 47) }
GDIHeliTrigger = { CPos.New(27, 55), CPos.New(27, 56), CPos.New(28, 56), CPos.New(28, 57), CPos.New(28, 58), CPos.New(28, 59)}

Nod1Units  = { "e1", "e1", "e3", "e3" }
Auto1Units = { "e1", "e1", "e3" }

KillsUntilReinforcements = 12
HeliDelay = { 83, 137, 211 }

GDIReinforcements = { "e2", "e2", "e2", "e2", "e2" }
GDIReinforcementsWaypoints = { GDIReinforcementsEntry.Location, GDIReinforcementsWP1.Location }

NodHelis = {
		{ DateTime.Seconds(HeliDelay[1]), { NodHeliEntry.Location, NodHeliLZ1.Location }, { "e1", "e1", "e3" } },
		{ DateTime.Seconds(HeliDelay[2]), { NodHeliEntry.Location, NodHeliLZ2.Location }, { "e1", "e1", "e1", "e1" } },
		{ DateTime.Seconds(HeliDelay[3]), { NodHeliEntry.Location, NodHeliLZ3.Location }, { "e1", "e1", "e3" } }
	   }

SendHeli = function(heli)
	units = Reinforcements.ReinforceWithTransport(nod, "tran", heli[3], heli[2], { heli[2][1] })
	Utils.Do(units[2], function(actor)
		actor.Hunt()
		Trigger.OnIdle(actor, actor.Hunt)
		Trigger.OnKilled(actor, KillCounter)
	end)
	Trigger.AfterDelay(heli[1], function() SendHeli(heli) end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(gdi, "Reinforce")
	Reinforcements.ReinforceWithTransport(gdi, "apc", GDIReinforcements, GDIReinforcementsWaypoints, nil, function(apc, team)
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
	nod.Cash = 1000

	if not ReinforcementsSent and kills >= KillsUntilReinforcements then
		ReinforcementsSent = true
		gdi.MarkCompletedObjective(reinforcementsObjective)
		SendGDIReinforcements()
	end

	if gdi.HasNoRequiredUnits() then
		Trigger.AfterDelay(DateTime.Seconds(1), function() gdi.MarkFailedObjective(gdiObjective) end)
	end
end

SetupWorld = function()
	Utils.Do(nod.GetGroundAttackers(nod), function(unit)
		Trigger.OnKilled(unit, KillCounter)
	end)

	Utils.Do(gdi.GetGroundAttackers(), function(unit)
		unit.Stance = "Defend"
	end)

	Hunter1.Hunt()
	Hunter2.Hunt()

	Trigger.OnRemovedFromWorld(crate, function() gdi.MarkCompletedObjective(gdiObjective) end)
end

WorldLoaded = function()
	gdi = Player.GetPlayer("GDI")
	nod = Player.GetPlayer("Nod")

	SetupWorld()

	Trigger.OnObjectiveAdded(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(gdi, function()
		Media.PlaySpeechNotification(gdi, "Win")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("burdet1.vqa")
		end)
	end)

	Trigger.OnPlayerLost(gdi, function()
		Media.PlaySpeechNotification(gdi, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	gdiObjective = gdi.AddPrimaryObjective("Retrieve the crate with the stolen rods.")
	reinforcementsObjective = gdi.AddSecondaryObjective("Eliminate " .. KillsUntilReinforcements .. " Nod units for reinforcements.")
	nod.AddPrimaryObjective("Defend against the GDI forces.")

	BuildNod1()
	Utils.Do(NodHelis, function(heli)
		Trigger.AfterDelay(heli[1], function() SendHeli(heli) end)
	end)

	autoTrigger = false
	Trigger.OnEnteredFootprint(AutoTrigger, function(a, id)
		if not autoTrigger and a.Owner == gdi then
			autoTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			BuildAuto1()
		end
	end)

	gdiHeliTrigger = false
	Trigger.OnEnteredFootprint(GDIHeliTrigger, function(a, id)
		if not gdiHeliTrigger and a.Owner == gdi then
			gdiHeliTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.ReinforceWithTransport(gdi, "tran", nil, { GDIHeliEntry.Location, GDIHeliLZ.Location })
		end
	end)

	Camera.Position = Actor56.CenterPosition

	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("nitejump.vqa") end)
end
