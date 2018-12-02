--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
nodInBaseTeam = { RushBuggy, RushRifle1, RushRifle2, RushRifle3 }
MobileConstructionVehicle = { "mcv" }
EngineerReinforcements = { "e6", "e6", "e6" }
VehicleReinforcements = { "jeep" }

AttackerSquadSize = 3


function Reinforce(units)
	ReinforceWithLandingCraft(GDI, units, lstStart.Location, lstEnd.Location)
end


function BridgeheadSecured()
	Reinforce(MobileConstructionVehicle)
	Trigger.AfterDelay(DateTime.Seconds(15), NodAttack)
	Trigger.AfterDelay(DateTime.Seconds(30), function() Reinforce(EngineerReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(120), function() Reinforce(VehicleReinforcements) end)
end


function NodAttack()
	local nodUnits = Nod.GetGroundAttackers()
	if #nodUnits > AttackerSquadSize * 2 then
		local attackers = Utils.Skip(nodUnits, #nodUnits - AttackerSquadSize)
		Utils.Do(attackers, function(unit)
			unit.AttackMove(NodAttackWaypoint.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
		Trigger.OnAllKilled(attackers, function() Trigger.AfterDelay(DateTime.Seconds(15), NodAttack) end)
	end
end


function WorldLoaded()
	InitObjectives(GDI)

	nodObjective = Nod.AddPrimaryObjective("Destroy all GDI troops.")
	gdiObjective1 = GDI.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	gdiObjective2 = GDI.AddSecondaryObjective("Capture the Tiberium refinery.")

	Trigger.OnCapture(NodRefinery, function() GDI.MarkCompletedObjective(gdiObjective2) end)
	Trigger.OnKilled(NodRefinery, function() GDI.MarkFailedObjective(gdiObjective2) end)

	Trigger.OnAllKilled(nodInBaseTeam, BridgeheadSecured)
end


function Tick()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(nodObjective)
	end
	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(gdiObjective1)
	end
end
