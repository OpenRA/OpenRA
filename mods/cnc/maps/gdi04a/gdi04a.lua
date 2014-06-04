Nod1Template  = { {HandOfNod, {"e1", "e1", "e3", "e3"}} }
Auto1Template = { {HandOfNod, {"e1", "e1", "e3"}} }

KillsUntilReinforcements = 12
HeliDelay = {83, 137, 211}

GDIReinforcements = {"e2", "e2", "e2", "e2"}
GDIReinforcementsWaypoints = {GDIReinforcementsEntry, GDIReinforcementsWP1}

NodHelis = {
		{Utils.Seconds(HeliDelay[1]), {NodHeliEntry, NodHeliLZ1}, {"e1", "e1", "e3"}},
		{Utils.Seconds(HeliDelay[2]), {NodHeliEntry, NodHeliLZ2}, {"e1", "e1", "e1", "e1"}},
		{Utils.Seconds(HeliDelay[3]), {NodHeliEntry, NodHeliLZ3}, {"e1", "e1", "e3"}}
	   }

SendHeli = function(heli, func)
	Reinforcements.ReinforceWithCargo(nod, "tran", heli[2], heli[3], func)
	OpenRA.RunAfterDelay(heli[1], function() SendHeli(heli, func) end)
end

HeliAction = function(heliActor, team)
	Actor.AfterMove(heliActor)
	Actor.UnloadCargo(heliActor, true)
	Actor.Wait(heliActor, Utils.Seconds(2))
	Actor.ScriptedMove(heliActor, NodHeliEntry)
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

BuildNod1 = function()
	Production.BuildTeamFromTemplate(nod, Nod1Template, function(team)
		Team.Do(team, function(actor)
			Actor.OnIdle(actor, Actor.Hunt)
			Actor.OnKilled(actor, KillCounter)
		end)
		Team.AddEventHandler(team.OnAllKilled, BuildNod1)
	end)
end

BuildAuto1 = function()
	Production.BuildTeamFromTemplate(nod, Auto1Template, function(team)
		Team.Do(team, function(actor)
			Actor.OnIdle(actor, Actor.Hunt)
			Actor.OnKilled(actor, KillCounter)
		end)
	end)
end

kills = 0
KillCounter = function() kills = kills + 1 end

Auto1Triggered = false
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

	if not Auto1Triggered then
		-- FIXME: replace with cell trigger when available
		local units = Map.FindUnitsInCircle(player, Auto1Trigger, 2)
		if #units > 0 then
			Auto1Triggered = true
			BuildAuto1()
		end
	elseif not GDIHeliTriggered then
		-- FIXME: replace with cell trigger when available
		local units = Map.FindUnitsInCircle(player, GDIHeliLZ, 2)
		if #units > 0 then
			GDIHeliTriggered = true
			Reinforcements.ReinforceWithCargo(player, "tran", {GDIHeliEntry, GDIHeliLZ}, nil, Actor.AfterMove)
		end
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

	Actor.Hunt(Hunter1)
	Actor.Hunt(Hunter2)

	Actor.OnRemovedFromWorld(crate, MissionAccomplished)
end

WorldLoaded = function()
	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("gdi4b.vqa", function() Media.PlayMovieFullscreen("nitejump.vqa") end) end)

	player	= OpenRA.GetPlayer("GDI")
	nod	= OpenRA.GetPlayer("Nod")

	SetupWorld()

	OpenRA.RunAfterDelay(1, BuildNod1)
	Utils.Do(NodHelis, function(heli)
		OpenRA.RunAfterDelay(heli[1], function() SendHeli(heli, HeliAction) end)
	end)

	OpenRA.SetViewportCenterPosition(Actor56.CenterPosition)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, false)
	Media.PlayMovieFullscreen("burdet1.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, false)
	Media.PlayMovieFullscreen("gameover.vqa")
end
