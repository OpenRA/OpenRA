--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
LoseTriggerHouses = { TrigLos2Farm1, TrigLos2Farm2, TrigLos2Farm3, TrigLos2Farm4 }
TownAttackTrigger = { CPos.New(54, 38), CPos.New(55, 38), CPos.New(56, 38), CPos.New(57, 38) }
GDIReinforcementsTrigger = { CPos.New(32, 51), CPos.New(32, 52), CPos.New(33, 52) }

GDIReinforcementsPart1 = { "jeep", "jeep" }
GDIReinforcementsPart2 = { "e2", "e2", "e2", "e2", "e2" }
TownAttackWave1 = { "bggy", "bggy" }
TownAttackWave2 = { "ltnk", "ltnk" }
TownAttackWave3 = { "e1", "e1", "e1", "e3", "e3", "e3" }
TownAttackWpts = { waypoint1, waypoint2 }

Civvie1Wpts = { CivvieWpts1, CivvieWpts2 }
Civvie2Wpts = { CivvieWpts3, CivvieWpts1, CivvieWpts4, CivvieWpts5, CivvieWpts6, CivvieWpts7, CivvieWpts8, CivvieWpts9, CivvieWpts10, CivvieWpts11 }

FollowCivvieWpts = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		actor.Move(wpt.Location, 2)
		actor.Wait(DateTime.Seconds(2))
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
	Reinforcements.Reinforce(enemy, TownAttackWave1, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(0.25), TownAttackAction)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Reinforcements.Reinforce(enemy, TownAttackWave2, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(1), TownAttackAction)
	end)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Reinforcements.Reinforce(enemy, TownAttackWave3, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(1), TownAttackAction)
	end)
end

SendGDIReinforcements = function()
	Reinforcements.Reinforce(player, GDIReinforcementsPart1, { GDIReinfEntry1.Location, GDIReinfRally1.Location }, DateTime.Seconds(1), function(actor)
		Media.PlaySpeechNotification(player, "Reinforce")
		actor.Move(GDIReinfRally3.Location)
		actor.Stance = "Defend"
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Reinforcements.ReinforceWithTransport(player, "apc", GDIReinforcementsPart2, { GDIReinfEntry2.Location, GDIReinfRally2.Location }, nil, function(apc, team)
			Media.PlaySpeechNotification(player, "Reinforce")
			apc.Move(GDIUnloadWpt.Location)
			apc.UnloadPassengers()
			Utils.Do(team, function(unit) unit.Stance = "Defend" end)
		end)
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
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
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	nodObjective = enemy.AddPrimaryObjective("Destroy all GDI troops.")
	gdiObjective1 = player.AddPrimaryObjective("Defend the town of Białystok.")
	gdiObjective2 = player.AddPrimaryObjective("Eliminate all Nod forces in the area.")

	townAttackTrigger = false
	Trigger.OnExitedFootprint(TownAttackTrigger, function(a, id)
		if not townAttackTrigger and a.Owner == player then
			townAttackTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			AttackTown()
		end
	end)

	gdiReinforcementsTrigger = false
	Trigger.OnEnteredFootprint(GDIReinforcementsTrigger, function(a, id)
		if not gdiReinforcementsTrigger and a.Owner == player then
			gdiReinforcementsTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			SendGDIReinforcements()
		end
	end)

	Utils.Do(player.GetGroundAttackers(), function(unit)
		unit.Stance = "Defend"
	end)

	Trigger.AfterDelay(1, function()
		FollowCivvieWpts(civvie1, Civvie1Wpts)
		FollowCivvieWpts(civvie2, Civvie2Wpts)
	end)

	Camera.Position = Actor141.CenterPosition
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(nodObjective)
	end
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(gdiObjective1)
		player.MarkCompletedObjective(gdiObjective2)
	end
end
