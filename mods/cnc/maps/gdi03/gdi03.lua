--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SamSites = { Sam1, Sam2, Sam3, Sam4 }
Sam4Guards = { Sam4Guard0, Sam4Guard1, Sam4Guard2, Sam4Guard3, Sam4Guard4, HiddenBuggy }
NodInfantrySquad = { "e1", "e1", "e1", "e1", "e1" }
NodAttackRoutes = { { AttackWaypoint }, { AttackWaypoint }, { AttackRallypoint1, AttackRallypoint2, AttackWaypoint } }
InfantryReinforcements = { "e1", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e2" }
JeepReinforcements = { "jeep", "jeep", "jeep" }

AttackPlayer = function()
	if NodBarracks.IsDead or NodBarracks.Owner == GDI then
		return
	end

	local after = function(team)
		local count = 1
		local route = Utils.Random(NodAttackRoutes)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, function()
				if actor.Location == route[count].Location then
					if not count == #route then
						count = count + 1
					else
						Trigger.ClearAll(actor)
						Trigger.AfterDelay(0, function()
							Trigger.OnIdle(actor, actor.Hunt)
						end)
					end
				else
					actor.AttackMove(route[count].Location)
				end
			end)
		end)
		Trigger.OnAllKilled(team, function() Trigger.AfterDelay(DateTime.Seconds(15), AttackPlayer) end)
	end

	NodBarracks.Build(NodInfantrySquad, after)
end

SendReinforcements = function()
	Reinforcements.Reinforce(GDI, JeepReinforcements, { VehicleStart.Location, VehicleStop.Location })
	Reinforcements.Reinforce(GDI, InfantryReinforcements, { InfantryStart.Location, InfantryStop.Location }, 5)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(GDI, { "mcv" }, { VehicleStart.Location, MCVwaypoint.Location })
		InitialUnitsArrived = true
	end)
	Media.PlaySpeechNotification(GDI, "Reinforce")
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	DestroyNod = GDI.AddObjective("Eliminate all Nod forces in the area.")
	local airSupportObjective = GDI.AddObjective("Destroy the SAM sites to receive air support.", "Secondary", false)

	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(airSupportObjective)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	RepairNamedActors(Nod, 0.25)

	Trigger.OnDamaged(Sam4, function()
		Utils.Do(Sam4Guards, IdleHunt)
	end)

	SendReinforcements()

	Camera.Position = MCVwaypoint.CenterPosition

	Trigger.AfterDelay(DateTime.Seconds(15), AttackPlayer)
end

Tick = function()
	if InitialUnitsArrived then
		if GDI.HasNoRequiredUnits() then
			GDI.MarkFailedObjective(DestroyNod)
		end

		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(DestroyNod)
		end
	end
end
