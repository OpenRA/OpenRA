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
		Actor.MoveNear(actor, wpt.Location, 2)
		Actor.Wait(actor, Utils.Seconds(2))
	end)
end

FollowWaypoints = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		Actor.AttackMove(actor, wpt.Location, 2)
	end)
end

TownAttackersIdleAction = function(actor)
	Actor.AttackMove(actor, TownAttackWpt.Location, 2)
	Actor.Hunt(actor)
end

TownAttackAction = function(actor)
	Actor.OnIdle(actor, TownAttackersIdleAction)
	FollowWaypoints(actor, TownAttackWpts)
end

AttackTown = function()
	TownAttackTriggered = true

	Reinforcements.Reinforce(nod, TownAttackWave1, NodReinfEntry.Location, waypoint0.Location, Utils.Seconds(0.25), TownAttackAction)
	OpenRA.RunAfterDelay(Utils.Seconds(2), function()
		Reinforcements.Reinforce(nod, TownAttackWave2, NodReinfEntry.Location, waypoint0.Location, Utils.Seconds(1), TownAttackAction)
	end)
	OpenRA.RunAfterDelay(Utils.Seconds(4), function()
		Reinforcements.Reinforce(nod, TownAttackWave3, NodReinfEntry.Location, waypoint0.Location, Utils.Seconds(1), TownAttackAction)
	end)
end

SendGDIReinforcements = function()
	GDIReinforcementsTriggered = true

	Reinforcements.Reinforce(player, GDIReinforcementsPart1, GDIReinfEntry1.Location, waypoint12.Location, Utils.Seconds(1), function(actor)
		Media.PlaySpeechNotification("Reinforce")
		Actor.Move(actor, waypoint10.Location)
		Actor.SetStance(actor, "Defend")
	end)
	OpenRA.RunAfterDelay(Utils.Seconds(5), function()
		Reinforcements.ReinforceWithCargo(player, "apc", { GDIReinfEntry2, waypoint13 }, GDIReinforcementsPart2, function(apc, team)
			Media.PlaySpeechNotification("Reinforce")
			Actor.Move(apc, GDIUnloadWpt.Location)
			Actor.UnloadCargo(apc, true)
			Team.Do(team, function(unit) Actor.SetStance(unit, "Defend") end)
		end)
	end)
end

-- FIXME: replace with real cell trigger when available
CellTrigger = function(player, trigger, radius, func)
	local units = Map.FindUnitsInCircle(player, trigger, radius)
	if #units > 0 then
		func()
	end
end

TownAttackTriggered = false
GDIReinforcementsTriggered = false
Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		OpenRA.RunAfterDelay(Utils.Seconds(1), MissionFailed)
	end
	if Mission.RequiredUnitsAreDestroyed(nod) then
		OpenRA.RunAfterDelay(Utils.Seconds(1), MissionAccomplished)
	end

	if not TownAttackTriggered then
		CellTrigger(player, TownAttackTrigger, 2, AttackTown)
	elseif not GDIReinforcementsTriggered then
		CellTrigger(player, GDIReinfTrigger, 2, SendGDIReinforcements)
	end
end

WorldLoaded = function()
	Media.PlayMovieFullscreen("bkground.vqa", function() Media.PlayMovieFullscreen("gdi4a.vqa", function() Media.PlayMovieFullscreen("nodsweep.vqa") end) end)
	player	= OpenRA.GetPlayer("GDI")
	nod	= OpenRA.GetPlayer("Nod")

	LoseTriggerTeam = Team.New(LoseTriggerHouses)
	Team.AddEventHandler(LoseTriggerTeam.OnAllKilled, MissionFailed)

	Utils.Do(Mission.GetGroundAttackersOf(player), function(unit)
		Actor.SetStance(unit, "Defend")
	end)

	OpenRA.RunAfterDelay(1, function()
		FollowCivvieWpts(civvie1, Civvie1Wpts)
		FollowCivvieWpts(civvie2, Civvie2Wpts)
	end)

	OpenRA.SetViewportCenterPosition(Actor141.CenterPosition)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil)
	Media.PlayMovieFullscreen("burdet1.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player })
	Media.PlayMovieFullscreen("gameover.vqa")
end
