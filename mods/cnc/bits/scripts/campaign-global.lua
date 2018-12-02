--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Nod = Player.GetPlayer("Nod")
GDI = Player.GetPlayer("GDI")

Difficulty = Map.LobbyOption("difficulty")

function InitObjectives(player)
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
end


function CheckForBase(player, buildingTypes)
	local buildings = 0

	Utils.Do(buildingTypes, function(name)
		if #player.GetActorsByType(name) > 0 then
			buildings = buildings + 1
		end
	end)

	return buildings == #buildingTypes
end


function ReinforceWithLandingCraft(player, units, transportStart, transportUnload, rallypoint)
	Media.PlaySpeechNotification(player, "Reinforce")
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


kills = 0


function KillCounter() kills = kills + 1 end
