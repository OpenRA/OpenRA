--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
MCVReinforcements = { "mcv" }
InfantryReinforcements = { "e1", "e1", "e1" }
VehicleReinforcements = { "jeep" }
NodPatrol = { "e1", "e1" }
GDIBaseBuildings = { "pyle", "fact", "nuke" }

SendNodPatrol = function()
	Reinforcements.Reinforce(enemy, NodPatrol, { nod0.Location, nod1.Location }, 15, function(soldier)
		soldier.AttackMove(nod2.Location)
		soldier.Move(nod3.Location)
		soldier.Hunt()
	end)
end

ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = player, Facing = 0, Location = transportStart })
	local subcell = 0
	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint ~= nil then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()
end

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	ReinforceWithLandingCraft(units, lstStart.Location, lstEnd.Location, reinforcementsTarget.Location)
end

CheckForBase = function(player)
	local buildings = 0

	Utils.Do(GDIBaseBuildings, function(name)
		if #player.GetActorsByType(name) > 0 then
			buildings = buildings + 1
		end
	end)

	return buildings == #GDIBaseBuildings
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

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	secureAreaObjective = player.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	beachheadObjective = player.AddSecondaryObjective("Establish a beachhead.")

	ReinforceWithLandingCraft(MCVReinforcements, lstStart.Location + CVec.New(2, 0), lstEnd.Location + CVec.New(2, 0), mcvTarget.Location)
	Reinforce(InfantryReinforcements)

	SendNodPatrol()

	Trigger.AfterDelay(DateTime.Seconds(10), function() Reinforce(InfantryReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(60), function() Reinforce(VehicleReinforcements) end)
end

Tick = function()
	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(secureAreaObjective)
	end

	if DateTime.GameTime > DateTime.Seconds(5) and player.HasNoRequiredUnits() then
		player.MarkFailedObjective(beachheadObjective)
		player.MarkFailedObjective(secureAreaObjective)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not player.IsObjectiveCompleted(beachheadObjective) and CheckForBase(player) then
		player.MarkCompletedObjective(beachheadObjective)
	end
end
