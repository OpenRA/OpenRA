--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

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
GDIReinforcementsLeft = 3

NodHeli = { { HeliEntry.Location, NodHeliLZ.Location }, { "e1", "e1", "e3", "e3" } }

Kills = 0
NodUnitKilled = function()
	Kills = Kills + 1

	if Kills == KillsUntilReinforcements then
		GDI.MarkCompletedObjective(ReinforcementsObjective)
		SendGDIReinforcements()
	end
end

SendHeli = function(heli)
	local units = Reinforcements.ReinforceWithTransport(Nod, "tran", heli[2], heli[1], { heli[1][1] })
	Utils.Do(units[2], function(actor)
		IdleHunt(actor)
		Trigger.OnKilled(actor, NodUnitKilled)
	end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.ReinforceWithTransport(GDI, "apc", GDIReinforcements, GDIReinforcementsWaypoints, nil, function(apc, team)
		table.insert(team, apc)
		Trigger.OnAllKilled(team, function()
			if GDIReinforcementsLeft > 0 then
				GDIReinforcementsLeft = GDIReinforcementsLeft - 1
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Media.DisplayMessage("APC squads in reserve: " .. GDIReinforcementsLeft, "Battlefield Control")
					SendGDIReinforcements()
				end)
			end
		end)
	end)
end

Build = function(unitTypes, repeats, func)
	if HandOfNod.IsDead then
		return
	end

	local after = function(units)
		Utils.Do(units, func)
		if repeats then
			Trigger.OnAllKilled(units, function()
				Build(unitTypes, repeats, func)
			end)
		end
	end

	if not HandOfNod.Build(unitTypes, after) then
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Build(unitTypes, repeats, func)
		end)
	end
end

Tick = function()
	Nod.Cash = 1000

	if (GDIReinforcementsLeft == 0 or not GDI.IsObjectiveCompleted(ReinforcementsObjective)) and GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(GDIObjective)
	end
end

SetupWorld = function()
	Utils.Do(Nod.GetGroundAttackers(), function(unit)
		Trigger.OnKilled(unit, NodUnitKilled)
	end)

	Utils.Do(Hunters, IdleHunt)

	Trigger.OnRemovedFromWorld(crate, function() GDI.MarkCompletedObjective(GDIObjective) end)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	GDIObjective = GDI.AddObjective("Retrieve the crate with the stolen rods.")
	ReinforcementsObjective = GDI.AddObjective("Eliminate " .. KillsUntilReinforcements .. " Nod units for reinforcements.", "Secondary", false)

	SetupWorld()

	Trigger.OnExitedFootprint(BhndTrigger, function(a, id)
		if not bhndTrigger and a.Owner == GDI then
			bhndTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			SendHeli(NodHeli)
		end
	end)

	Trigger.OnExitedFootprint(Atk1Trigger, function(a, id)
		if not atk1Trigger and a.Owner == GDI then
			atk1Trigger = true
			Trigger.RemoveFootprintTrigger(id)

			Build(NodxUnits, false, function(actor)
				Trigger.OnKilled(actor, NodUnitKilled)
				actor.Patrol({ NodPatrol1.Location, NodPatrol2.Location, NodPatrol3.Location, NodPatrol4.Location }, false)
				Trigger.OnIdle(actor, actor.Hunt)
			end)
		end
	end)

	Trigger.OnEnteredFootprint(Atk2Trigger, function(a, id)
		if not atk2Trigger and a.Owner == GDI then
			atk2Trigger = true
			Trigger.RemoveFootprintTrigger(id)

			Build(NodxUnits, false, function(actor)
				Trigger.OnKilled(actor, NodUnitKilled)
				actor.Patrol({ NodPatrol1.Location, NodPatrol2.Location }, false)
				IdleHunt(actor)
			end)
		end
	end)

	Trigger.OnEnteredFootprint(AutoTrigger, function(a, id)
		if not autoTrigger and a.Owner == GDI then
			autoTrigger = true
			Trigger.RemoveFootprintTrigger(id)

			Build(AutoUnits, true, function(actor)
				Trigger.OnKilled(actor, NodUnitKilled)
				IdleHunt(actor)
			end)

			Trigger.AfterDelay(DateTime.Seconds(4), function()
				IdleHunt(tank)
			end)
		end
	end)

	Trigger.OnEnteredFootprint(GDIHeliTrigger, function(a, id)
		if not gdiHeliTrigger and a.Owner == GDI then
			gdiHeliTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.ReinforceWithTransport(GDI, "tran", nil, { HeliEntry.Location, GDIHeliLZ.Location })
		end
	end)

	Camera.Position = GDIReinforcementsWP1.CenterPosition
end
