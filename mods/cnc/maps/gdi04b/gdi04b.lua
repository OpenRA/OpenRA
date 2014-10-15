BhndTrigger = { CPos.New(39, 21), CPos.New(40, 21), CPos.New(41, 21) }
Atk1Trigger = { CPos.New(35, 37) }
Atk2Trigger = { CPos.New(9, 44), CPos.New(10, 44), CPos.New(11, 44), CPos.New(12, 44), CPos.New(13, 44) }
AutoTrigger = { CPos.New(5, 30), CPos.New(6, 30), CPos.New(7, 30), CPos.New(8, 30), CPos.New(9, 30), CPos.New(10, 30), CPos.New(11, 30), CPos.New(12, 30), CPos.New(13, 30) }
GDIHeliTrigger = { CPos.New(11, 11), CPos.New(11, 12), CPos.New(11, 13), CPos.New(11, 14), CPos.New(11, 15), CPos.New(12, 15), CPos.New(13, 15), CPos.New(14, 15), CPos.New(15, 15), CPos.New(16, 15) }

Hunters = { Hunter1, Hunter2, Hunter3, Hunter4, Hunter5 }
NodxUnits = { "e1", "e1", "e3", "e3" }
AutoUnits = { "e1", "e1", "e1", "e3", "e3" }

KillsUntilReinforcements = 12

GDIReinforcements = { "e2", "e2", "e2", "e2", "e2" }
GDIReinforcementsWaypoints = { GDIReinforcementsEntry.Location, GDIReinforcementsWP1.Location }

NodHeli = { { HeliEntry.Location, NodHeliLZ.Location }, { "e1", "e1", "e3", "e3" } }

SendHeli = function(heli)
	units = Reinforcements.ReinforceWithTransport(nod, "tran", heli[2], heli[1], { heli[1][1] })
	Utils.Do(units[2], function(actor)
		actor.Hunt()
		Trigger.OnIdle(actor, actor.Hunt)
		Trigger.OnKilled(actor, KillCounter)
	end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(gdi, "Reinforce")
	Reinforcements.ReinforceWithTransport(gdi, "apc", GDIReinforcements, GDIReinforcementsWaypoints, nil, function(apc, team)
		table.insert(team, apc)
		Trigger.OnAllKilled(team, function() Trigger.AfterDelay(Utils.Seconds(5), SendGDIReinforcements) end)
		Utils.Do(team, function(unit) unit.Stance = "Defend" end)
	end)
end

Build = function(unitTypes, repeats, func)
	if HandOfNod.IsDead then
		return
	end

	local innerFunc = function(units)
		Utils.Do(units, func)
		if repeats then
			Trigger.OnAllKilled(units, function()
				Build(unitTypes, repeats, func)
			end)
		end
	end

	if not HandOfNod.Build(unitTypes, innerFunc) then
		Trigger.AfterDelay(Utils.Seconds(5), function()
			Build(unitTypes, repeats, func)
		end)
	end
end

BuildNod1 = function()
	Build(NodxUnits, false, function(actor)
		Trigger.OnKilled(actor, KillCounter)
		actor.Patrol({ waypoint1.Location, waypoint2.Location, waypoint3.Location, waypoint4.Location }, false)
		Trigger.OnIdle(actor, actor.Hunt)
	end)
end

BuildNod2 = function()
	Build(NodxUnits, false, function(actor)
		Trigger.OnKilled(actor, KillCounter)
		actor.Patrol({ waypoint1.Location, waypoint2.Location }, false)
		Trigger.OnIdle(actor, actor.Hunt)
	end)
end

BuildAuto = function()
	Build(AutoUnits, true, function(actor)
		Trigger.OnKilled(actor, KillCounter)
		Trigger.OnIdle(actor, actor.Hunt)
	end)
end

ReinforcementsSent = false
kills = 0
KillCounter = function() kills = kills + 1 end
Tick = function()
	nod.Cash = 1000

	if not ReinforcementsSent and kills >= KillsUntilReinforcements then
		ReinforcementsSent = true
		gdi.MarkCompletedObjective(reinforcementsObjective)
		SendGDIReinforcements()
	end

	if gdi.HasNoRequiredUnits() then
		Trigger.AfterDelay(Utils.Seconds(1), function()
			gdi.MarkFailedObjective(gdiObjective)
		end)
	end
end

SetupWorld = function()
	Utils.Do(nod.GetGroundAttackers(), function(unit)
		Trigger.OnKilled(unit, KillCounter)
	end)

	Utils.Do(gdi.GetGroundAttackers(), function(unit)
		unit.Stance = "Defend"
	end)

	Utils.Do(Hunters, function(actor) actor.Hunt() end)

	Trigger.OnRemovedFromWorld(crate, function() gdi.MarkCompletedObjective(gdiObjective) end)
end

WorldLoaded = function()
	gdi = Player.GetPlayer("GDI")
	nod = Player.GetPlayer("Nod")

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
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("burdet1.vqa")
		end)
	end)

	Trigger.OnPlayerLost(gdi, function()
		Media.PlaySpeechNotification(gdi, "Lose")
		Trigger.AfterDelay(Utils.Seconds(1), function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	gdiObjective = gdi.AddPrimaryObjective("Retrieve the crate with the stolen rods.")
	reinforcementsObjective = gdi.AddSecondaryObjective("Eliminate " .. KillsUntilReinforcements .. " Nod units for reinforcements.")
	nod.AddPrimaryObjective("Defend against the GDI forces.")

	SetupWorld()

	bhndTrigger = false
	Trigger.OnExitedFootprint(BhndTrigger, function(a, id)
		if not bhndTrigger and a.Owner == gdi then
			bhndTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			SendHeli(NodHeli)
		end
	end)

	atk1Trigger = false
	Trigger.OnExitedFootprint(Atk1Trigger, function(a, id)
		if not atk1Trigger and a.Owner == gdi then
			atk1Trigger = true
			Trigger.RemoveFootprintTrigger(id)
			BuildNod1()
		end
	end)

	atk2Trigger = false
	Trigger.OnEnteredFootprint(Atk2Trigger, function(a, id)
		if not atk2Trigger and a.Owner == gdi then
			atk2Trigger = true
			Trigger.RemoveFootprintTrigger(id)
			BuildNod2()
		end
	end)

	autoTrigger = false
	Trigger.OnEnteredFootprint(AutoTrigger, function(a, id)
		if not autoTrigger and a.Owner == gdi then
			autoTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			BuildAuto()
			Trigger.AfterDelay(Utils.Seconds(4), function()
				tank.Hunt()
			end)
		end
	end)

	gdiHeliTrigger = false
	Trigger.OnEnteredFootprint(GDIHeliTrigger, function(a, id)
		if not gdiHeliTrigger and a.Owner == gdi then
			gdiHeliTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.ReinforceWithTransport(gdi, "tran", nil, { HeliEntry.Location, GDIHeliLZ.Location })
		end
	end)

	Camera.Position = GDIReinforcementsWP1.CenterPosition

	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("nitejump.vqa") end)
end
