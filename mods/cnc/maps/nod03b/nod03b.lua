--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnits = { "e1", "e1", "bggy", "bike", "e1", "e1", "bike", "bggy", "e1", "e1" }
Engineers = { "e6", "e6", "e6" }
FirstAttackWaveUnits = { "e1", "e1", "e2" }
SecondAttackWaveUnits = { "e1", "e1", "e1" }
ThirdAttackWaveUnits = { "e1", "e1", "e1", "e2" }

SendAttackWave = function(units, action)
	Reinforcements.Reinforce(GDI, units, { GDIBarracksSpawn.Location, WP0.Location, WP1.Location }, 15, action)
end

FirstAttackWave = function(soldier)
	soldier.Move(WP2.Location)
	soldier.Move(WP3.Location)
	soldier.Move(WP4.Location)
	soldier.AttackMove(PlayerBase.Location)
end

SecondAttackWave = function(soldier)
	soldier.Move(WP5.Location)
	soldier.Move(WP6.Location)
	soldier.Move(WP7.Location)
	soldier.Move(WP9.Location)
	soldier.AttackMove(PlayerBase.Location)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	InitObjectives(Nod)

	CapturePrison = Nod.AddObjective("Capture the prison.")
	DestroyGDI = Nod.AddObjective("Destroy all GDI forces.", "Secondary", false)

	Trigger.OnKilled(TechCenter, function()
		Nod.MarkFailedObjective(CapturePrison)
	end)

	Trigger.OnCapture(TechCenter, function()
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Nod.MarkCompletedObjective(CapturePrison)
		end)
	end)

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, { "mcv" }, { McvEntry.Location, McvDeploy.Location })
	Reinforcements.Reinforce(Nod, NodUnits, { NodEntry.Location, NodRallypoint.Location })
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Media.PlaySpeechNotification(Nod, "Reinforce")
		Reinforcements.Reinforce(Nod, Engineers, { McvEntry.Location, PlayerBase.Location })
	end)

	Trigger.AfterDelay(DateTime.Seconds(40), function() SendAttackWave(FirstAttackWaveUnits, FirstAttackWave) end)
	Trigger.AfterDelay(DateTime.Seconds(80), function() SendAttackWave(SecondAttackWaveUnits, SecondAttackWave) end)
	Trigger.AfterDelay(DateTime.Seconds(140), function() SendAttackWave(ThirdAttackWaveUnits, FirstAttackWave) end)
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
