--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodInBaseTeam = { RushBuggy, RushRifle1, RushRifle2, RushRifle3 }
MobileConstructionVehicle = { "mcv" }
EngineerReinforcements = { "e6", "e6", "e6" }
VehicleReinforcements = { "jeep" }

AttackerSquadSize = 3

Reinforce = function(units)
	Media.PlaySpeechNotification(GDI, "Reinforce")
	ReinforceWithLandingCraft(GDI, units, lstStart.Location, lstEnd.Location)
end

BridgeheadSecured = function()
	Reinforce(MobileConstructionVehicle)
	Trigger.AfterDelay(DateTime.Seconds(15), NodAttack)
	Trigger.AfterDelay(DateTime.Seconds(30), function() Reinforce(EngineerReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(120), function() Reinforce(VehicleReinforcements) end)
end

NodAttack = function()
	local nodUnits = Nod.GetGroundAttackers()
	if #nodUnits > AttackerSquadSize * 2 then
		local attackers = Utils.Skip(nodUnits, #nodUnits - AttackerSquadSize)
		Utils.Do(attackers, function(unit)
			unit.AttackMove(NodAttackWaypoint.Location)
			IdleHunt(unit)
		end)
		Trigger.OnAllKilled(attackers, function() Trigger.AfterDelay(DateTime.Seconds(15), NodAttack) end)
	end
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	NodObjective = AddPrimaryObjective(Nod, "")
	GDIObjective1 = AddPrimaryObjective(GDI, "eliminate-nod")
	GDIObjective2 = AddSecondaryObjective(GDI, "capture-refinery")

	Trigger.OnCapture(NodRefinery, function() GDI.MarkCompletedObjective(GDIObjective2) end)
	Trigger.OnKilled(NodRefinery, function() GDI.MarkFailedObjective(GDIObjective2) end)

	Trigger.OnAllKilled(NodInBaseTeam, BridgeheadSecured)
end

Tick = function()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective)
	end

	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective1)
	end
end
