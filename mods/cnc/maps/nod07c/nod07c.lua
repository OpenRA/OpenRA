--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
NodUnitsVehicles1 = { 'bggy', 'bggy', 'bike', 'bike' }
NodUnitsVehicles2 = { 'ltnk', 'ltnk' }
NodUnitsEngineers = { 'e6', 'e6', 'e6', 'e6' }
NodUnitsRockets = { 'e3', 'e3', 'e3', 'e3' }
NodUnitsGunners = { 'e1', 'e1', 'e1', 'e1' }
NodUnitsFlamers = { 'e4', 'e4', 'e4', 'e4' }

GDI1 = { units = { ['e1'] = 2 }, waypoints = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint8.Location, waypoint2.Location, waypoint9.Location, waypoint2.Location } }
GDI2 = { units = { ['e1'] = 10, ['e2'] = 8, ['mtnk'] = 1, ['jeep'] = 1 }, waypoints = { waypoint12.Location, waypoint15.Location, waypoint0.Location } }
GDI3 = { units = { ['jeep'] = 1 }, waypoints = { waypoint0.Location, waypoint1.Location, waypoint3.Location, waypoint4.Location, waypoint3.Location, waypoint2.Location, waypoint5.Location, waypoint6.Location, waypoint2.Location, waypoint7.Location } }
MTANK = { units = { ['mtnk'] = 1 }, waypoints = { waypoint14.Location, waypoint5.Location } }

targetsKilled = 0

AutoGuard = function(guards)
	Utils.Do(guards, function(guard)
		Trigger.OnDamaged(guard, function(guard, attacker)
			if not guard.IsDead then
				guard.Hunt()
			end
		end)
	end)
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, { 'ltnk' }, { ReinforcementsTopSpawn.Location, ReinforcementsTankRally.Location }, 1)
	local Engineers = Reinforcements.Reinforce(player, NodUnitsEngineers, { ReinforcementsTopSpawn.Location, ReinforcementsEngineersRally.Location }, 10)
	Reinforcements.Reinforce(player, NodUnitsRockets, { ReinforcementsBottomSpawn.Location, ReinforcementsRocketsRally.Location }, 10)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(player, NodUnitsGunners, { ReinforcementsBottomSpawn.Location, ReinforcementsGunnersRally.Location }, 10)
		Reinforcements.Reinforce(player, NodUnitsFlamers, { ReinforcementsTopSpawn.Location, ReinforcementsFlamersRally.Location }, 10)
	end)
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local unitsA = Reinforcements.ReinforceWithTransport(player, 'tran.in', NodUnitsVehicles1, { GunboatRight.Location, ReinforcementsHelicopter1Rally.Location }, { GunboatRight.Location })[2]

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			local unitsB = Reinforcements.ReinforceWithTransport(player, 'tran.in', NodUnitsVehicles2, { GunboatRight.Location, ReinforcementsHelicopter2Rally.Location }, { GunboatRight.Location })[2]

			Utils.Do(unitsB, function(unit)
				unitsA[#unitsA + 1] = unit
			end)

			Trigger.OnAllKilled(unitsA, function()
				if not defendersActive then
					defendersActive = true
					player.MarkFailedObjective(NodObjective4)
				end
			end)
		end)
	end)

	Trigger.OnAllRemovedFromWorld(Engineers, function()
		if not player.IsObjectiveCompleted(NodObjective1) then
			player.MarkFailedObjective(NodObjective1)
		end
	end)
end

DiscoveredSideEntrance = function(_,discoverer)
	if not defendersActive then
		defendersActive = true
		player.MarkFailedObjective(NodObjective4)
	end
end

DiscoveredMainEntrance = function(_,discoverer)
	if not defendersActive then
		SendDefenders(GDI2)
	end
end

SendDefenders = function(team)
	defendersActive = true
	player.MarkCompletedObjective(NodObjective4)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		for type, amount in pairs(team.units) do
			local actors = Utils.Take(amount, enemy.GetActorsByType(type))
			Utils.Do(actors, function(actor)
				if actor.IsIdle then
					actor.AttackMove(waypoint0.Location)
				end
			end)
		end
	end)
end

SendGuards = function(team)
	for type, amount in pairs(team.units) do
		local actors = Utils.Take(amount, enemy.GetActorsByType(type))
		Utils.Do(actors, function(actor)
			if actor.IsIdle then
				actor.Patrol(team.waypoints, true, DateTime.Seconds(25))
			end
		end)
	end
end

Trigger.OnKilled(GDIHpad, function()
	if not player.IsObjectiveCompleted(NodObjective1) then
		player.MarkFailedObjective(NodObjective1)
	end
end)

Trigger.OnKilled(GDIOrca, function()
	if not player.IsObjectiveCompleted(NodObjective3) then
		player.MarkFailedObjective(NodObjective3)
	end
end)

Trigger.OnDamaged(GDIBuilding11, function()
	SendGuards(MTANK)
end)

Utils.Do(Map.ActorsWithTag("Village"), function(actor)
	Trigger.OnKilled(actor, function()
		targetsKilled = targetsKilled + 1
	end)
end)

Utils.Do(Map.ActorsWithTag("GDIBuilding"), function(actor)
	Trigger.OnKilledOrCaptured(actor, function()
		player.MarkFailedObjective(NodObjective2)
	end)
end)

Trigger.OnCapture(GDIHpad, function()
	hpadCaptured = true
	player.MarkCompletedObjective(NodObjective1)
	if not GDIOrca.IsDead then
		GDIOrca.Owner = player
	end
	Actor.Create("camera", true, { Owner = player, Location = waypoint25.Location })
	Actor.Create("flare", true, { Owner = player, Location = waypoint25.Location })
end)

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()

	SendGuards(GDI1)
	SendGuards(GDI3)
	AutoGuard(enemy.GetGroundAttackers())

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

	Trigger.OnDiscovered(GDIBuilding9, DiscoveredMainEntrance)
	Trigger.OnDiscovered(GDIBuilding10, DiscoveredMainEntrance)
	Trigger.OnDiscovered(GDIBuilding11, DiscoveredSideEntrance)

	NodObjective1 = player.AddPrimaryObjective("Capture the GDI helipad.")
	NodObjective2 = player.AddPrimaryObjective("Don't capture or destroy any other\nGDI main building.")
	NodObjective3 = player.AddPrimaryObjective("Use the GDI orca to wreak havoc at the village.")
	NodObjective4 = player.AddSecondaryObjective("Distract the guards by attacking the\nmain entrance with your vehicles.")
	GDIObjective = enemy.AddPrimaryObjective("Kill all enemies.")
end

Tick = function()
	if DateTime.GameTime > 2 and player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(GDIObjective)
	end

	if targetsKilled >= 15 then
		player.MarkCompletedObjective(NodObjective2)
		player.MarkCompletedObjective(NodObjective3)
	end

	if enemy.Resources >= enemy.ResourceCapacity * 0.75 then
		enemy.Resources = enemy.ResourceCapacity * 0.25
	end
end
