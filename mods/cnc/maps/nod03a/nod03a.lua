--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnits = { "bike", "e3", "e1", "bggy", "e1", "e3", "bike", "bggy" }
FirstAttackWave = { "e1", "e1", "e1", "e2", }
SecondThirdAttackWave = { "e1", "e1", "e2", }

SendAttackWave = function(units, spawnPoint)
	Reinforcements.Reinforce(GDI, units, { spawnPoint }, DateTime.Seconds(1), function(actor)
		actor.AttackMove(PlayerBase.Location)
	end)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	CapturePrison = Nod.AddObjective("Capture the prison.")
	DestroyGDI = Nod.AddObjective("Destroy all GDI forces.", "Secondary", false)

	Trigger.OnCapture(TechCenter, function()
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Nod.MarkCompletedObjective(CapturePrison)
		end)
	end)

	Trigger.OnKilled(TechCenter, function()
		Nod.MarkFailedObjective(CapturePrison)
	end)

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnits, { NodEntry.Location, NodRallyPoint.Location })
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Reinforcements.Reinforce(Nod, { "mcv" }, { NodEntry.Location, PlayerBase.Location })
	end)

	Trigger.AfterDelay(DateTime.Seconds(20), function() SendAttackWave(FirstAttackWave, AttackWaveSpawnA.Location) end)
	Trigger.AfterDelay(DateTime.Seconds(50), function() SendAttackWave(SecondThirdAttackWave, AttackWaveSpawnB.Location) end)
	Trigger.AfterDelay(DateTime.Seconds(100), function() SendAttackWave(SecondThirdAttackWave, AttackWaveSpawnC.Location) end)
end

Tick = function()
	if DateTime.GameTime > 2 then
		if Nod.HasNoRequiredUnits() then
			Nod.MarkFailedObjective(CapturePrison)
		end

		if GDI.HasNoRequiredUnits() then
			Nod.MarkCompletedObjective(DestroyGDI)
		end
	end
end
