NodxTemplate = { {HandOfNod, {"e1", "e1", "e3", "e3"}} }
AutoTemplate = { {HandOfNod, {"e1", "e1", "e1", "e3", "e3"}} }

KillsUntilReinforcements = 12
kills = 0
KillCounter = function() kills = kills + 1 end

GDIReinforcements = {"e2", "e2", "e2", "e2"}
GDIReinforcementsWaypoints = {GDIReinforcementsEntry, GDIReinforcementsWP1}

NodHeli = {{HeliEntry, NodHeliLZ}, {"e1", "e1", "e3", "e3"}}

SendHeli = function(heli, func)
	Reinforcements.ReinforceWithCargo(nod, "tran", heli[1], heli[2], func)
end

HeliAction = function(heliActor, team)
	Actor.AfterMove(heliActor)
	Actor.UnloadCargo(heliActor, true)
	Actor.Wait(heliActor, Utils.Seconds(2))
	Actor.ScriptedMove(heliActor, HeliEntry.Location)
	Actor.RemoveSelf(heliActor)

	Team.Do(team, function(actor)
		Actor.Hunt(actor)
		Actor.OnIdle(actor, Actor.Hunt)
		Actor.OnKilled(actor, KillCounter)
	end)
end

SendGDIReinforcements = function()
	Reinforcements.ReinforceWithCargo(player, "apc", GDIReinforcementsWaypoints, GDIReinforcements, function(apc, team)
		Team.Add(team, apc)
		Actor.OnKilled(apc, SendGDIReinforcements)
		Team.Do(team, function(unit) Actor.SetStance(unit, "Defend") end)
	end)
end

Build = function(template, repeats, func)
	Production.BuildTeamFromTemplate(nod, template, function(team)
		Team.Do(team, func)
		if repeats then
			Team.AddEventHandler(team.OnAllKilled, function()
				Build(template, repeats, func)
			end)
		end
	end)

end

BuildNod1 = function()
	Build(NodxTemplate, false, function(actor)
		Actor.OnKilled(actor, KillCounter)
		Actor.Patrol(actor, {waypoint1, waypoint2, waypoint3, waypoint4}, 0, false)
		Actor.OnIdle(actor, Actor.Hunt)
	end)
end

BuildNod2 = function()
	Build(NodxTemplate, false, function(actor)
		Actor.OnKilled(actor, KillCounter)
		Actor.Patrol(actor, {waypoint1, waypoint2}, 0, false)
		Actor.OnIdle(actor, Actor.Hunt)
	end)
end

BuildAuto = function()
	Build(AutoTemplate, true, function(actor)
		Actor.OnKilled(actor, KillCounter)
		Actor.OnIdle(actor, Actor.Hunt)
	end)
end

-- FIXME: replace with real cell trigger when available
CellTrigger = function(player, trigger, radius, func)
	local units = Map.FindUnitsInCircle(player, trigger, radius)
	if #units > 0 then
		func()
	end
end

BhndTriggered = false
Atk1Triggered = false
Atk2Triggered = false
AutoTriggered = false
GDIHeliTriggered = false
ReinforcementsSent = false

Tick = function()
	if not ReinforcementsSent and kills >= KillsUntilReinforcements then
		ReinforcementsSent = true
		SendGDIReinforcements()
	end

	if Mission.RequiredUnitsAreDestroyed(player) then
		OpenRA.RunAfterDelay(Utils.Seconds(1), MissionFailed)
	end

	if not BhndTriggered then
		CellTrigger(player, BhndTrigger, 2, function()
			BhndTriggered = true
			SendHeli(NodHeli, HeliAction)
		end)
	end

	if not Atk1Triggered then
		CellTrigger(player, Atk1Trigger, 2, function()
			Atk1Triggered = true
			BuildNod1()
		end)
	elseif not Atk2Triggered then
		CellTrigger(player, Atk2Trigger, 2, function()
			Atk2Triggered = true
			BuildNod2()
		end)
	elseif not AutoTriggered then
		CellTrigger(player, AutoTrigger, 2, function()
			AutoTriggered = true
			BuildAuto()
			OpenRA.RunAfterDelay(Utils.Seconds(5), function()
				Actor.Hunt(tank)
			end)
		end)
	elseif not GDIHeliTriggered then
		CellTrigger(player, HeliTrigger, 2, function()
			GDIHeliTriggered = true
			Reinforcements.ReinforceWithCargo(player, "tran", {HeliEntry, GDIHeliLZ}, nil, Actor.AfterMove)
		end)
	end
end

SetupWorld = function()
	OpenRA.GiveCash(nod, 10000)
	Production.EventHandlers.Setup(nod)

	Utils.Do(Mission.GetGroundAttackersOf(nod), function(unit)
		Actor.OnKilled(unit, KillCounter)
	end)

	Utils.Do(Mission.GetGroundAttackersOf(player), function(unit)
		Actor.SetStance(unit, "Defend")
	end)

	hunters1 = Team.New({Hunter1, Hunter2})
	hunters2 = Team.New({Hunter3, Hunter4, Hunter5})

	OpenRA.RunAfterDelay(1, function() Team.Do(hunters1, Actor.Hunt) end)
	OpenRA.RunAfterDelay(1, function() Team.Do(hunters2, Actor.Hunt) end)

	Actor.OnRemovedFromWorld(crate, MissionAccomplished)
end

WorldLoaded = function()
	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("gdi4b.vqa", function() Media.PlayMovieFullscreen("nitejump.vqa") end) end)

	player	= OpenRA.GetPlayer("GDI")
	nod	= OpenRA.GetPlayer("Nod")

	SetupWorld()

	OpenRA.SetViewportCenterPosition(GDIReinforcementsWP1.CenterPosition)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil)
	Media.PlayMovieFullscreen("burdet1.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player })
	Media.PlayMovieFullscreen("gameover.vqa")
end
