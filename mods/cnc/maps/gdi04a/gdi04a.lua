--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AutoTrigger = { CPos.New(51, 47), CPos.New(52, 47), CPos.New(53, 47), CPos.New(54, 47) }
GDIHeliTrigger = { CPos.New(27, 55), CPos.New(27, 56), CPos.New(28, 56), CPos.New(28, 57), CPos.New(28, 58), CPos.New(28, 59)}

NodUnits = { "e1", "e1", "e3", "e3" }
AutoUnits = { "e1", "e1", "e3" }

KillsUntilReinforcements = 12
HeliDelay = { 83, 137, 211 }

GDIReinforcements = { "e2", "e2", "e2", "e2", "e2" }
GDIReinforcementsWaypoints = { GDIReinforcementsEntry.Location, GDIReinforcementsWP1.Location }
GDIReinforcementsLeft = 3

NodHelis =
{
	{ delay = DateTime.Seconds(HeliDelay[1]), entry = { NodHeliEntry.Location, NodHeliLZ1.Location }, types = { "e1", "e1", "e3" } },
	{ delay = DateTime.Seconds(HeliDelay[2]), entry = { NodHeliEntry.Location, NodHeliLZ2.Location }, types = { "e1", "e1", "e1", "e1" } },
	{ delay = DateTime.Seconds(HeliDelay[3]), entry = { NodHeliEntry.Location, NodHeliLZ3.Location }, types = { "e1", "e1", "e3" } }
}

Kills = 0
NodUnitKilled = function()
	Kills = Kills + 1

	if Kills == KillsUntilReinforcements then
		GDI.MarkCompletedObjective(ReinforcementsObjective)
		SendGDIReinforcements()
	end
end

SendHeli = function(heli)
	local units = Reinforcements.ReinforceWithTransport(Nod, "tran", heli.types, heli.entry, { heli.entry[1] })
	Utils.Do(units[2], function(actor)
		IdleHunt(actor)
		Trigger.OnKilled(actor, NodUnitKilled)
	end)
	Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
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

BuildNod = function()
	local after = function(team)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, actor.Hunt)
			Trigger.OnKilled(actor, NodUnitKilled)
		end)
		Trigger.OnAllKilled(team, BuildNod)
	end

	ProduceUnits(Nod, HandOfNod, nil, function() return NodUnits end, after)
end

BuildAuto = function()
	local after = function(team)
		Utils.Do(team, function(actor)
			Trigger.OnIdle(actor, actor.Hunt)
			Trigger.OnKilled(actor, NodUnitKilled)
		end)
	end

	local delay = function() return DateTime.Seconds(5) end
	ProduceUnits(Nod, HandOfNod, delay, function() return AutoUnits end, after)
end

Tick = function()
	Nod.Cash = 1000

	if (GDIReinforcementsLeft == 0 or not GDI.IsObjectiveCompleted(ReinforcementsObjective)) and GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(GDIObjective)
	end
end

SetupWorld = function()
	Utils.Do(Nod.GetGroundAttackers(Nod), function(unit)
		Trigger.OnKilled(unit, NodUnitKilled)
	end)

	Hunter1.Hunt()
	Hunter2.Hunt()

	Trigger.OnRemovedFromWorld(crate, function() GDI.MarkCompletedObjective(GDIObjective) end)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	SetupWorld()

	InitObjectives(GDI)

	GDIObjective = GDI.AddObjective("Retrieve the crate with the stolen rods.")
	ReinforcementsObjective = GDI.AddObjective("Eliminate " .. KillsUntilReinforcements .. " Nod units for reinforcements.", "Secondary", false)

	BuildNod()
	Utils.Do(NodHelis, function(heli)
		Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
	end)

	Trigger.OnEnteredFootprint(AutoTrigger, function(a, id)
		if not AutoTriggered and a.Owner == GDI then
			AutoTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			BuildAuto()
		end
	end)

	Trigger.OnEnteredFootprint(GDIHeliTrigger, function(a, id)
		if not GDIHeliTriggered and a.Owner == GDI then
			GDIHeliTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			Reinforcements.ReinforceWithTransport(GDI, "tran", nil, { GDIHeliEntry.Location, GDIHeliLZ.Location })
		end
	end)

	Camera.Position = Actor56.CenterPosition
end
