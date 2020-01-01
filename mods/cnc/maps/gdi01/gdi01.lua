--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	Reinforcements.Reinforce(Nod, NodPatrol, { nod0.Location, nod1.Location }, 15, function(soldier)
		soldier.AttackMove(nod2.Location)
		soldier.AttackMove(nod3.Location)
		IdleHunt(soldier)
	end)
end

Reinforce = function(units)
	Media.PlaySpeechNotification(GDI, "Reinforce")
	ReinforceWithLandingCraft(GDI, units, lstStart.Location, lstEnd.Location, reinforcementsTarget.Location)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	secureAreaObjective = GDI.AddObjective("Eliminate all Nod forces in the area.")
	beachheadObjective = GDI.AddObjective("Establish a beachhead.", "Secondary", false)

	ReinforceWithLandingCraft(GDI, MCVReinforcements, lstStart.Location + CVec.New(2, 0), lstEnd.Location + CVec.New(2, 0), mcvTarget.Location)
	Reinforce(InfantryReinforcements)

	SendNodPatrol()

	Trigger.AfterDelay(DateTime.Seconds(10), function() Reinforce(InfantryReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(60), function() Reinforce(VehicleReinforcements) end)
end

Tick = function()
	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(secureAreaObjective)
	end

	if DateTime.GameTime > DateTime.Seconds(5) and GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(beachheadObjective)
		GDI.MarkFailedObjective(secureAreaObjective)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not GDI.IsObjectiveCompleted(beachheadObjective) and CheckForBase(GDI, GDIBaseBuildings) then
		GDI.MarkCompletedObjective(beachheadObjective)
	end
end
