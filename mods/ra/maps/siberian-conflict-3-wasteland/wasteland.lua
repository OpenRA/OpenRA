--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
OpeningAttack = { StartV2, StartTeam1, StartTeam2, StartTeam3 }

Setup = function()
	Utils.Do(USSR.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(Allies, "ReinforcementsArrived")
		Reinforcements.Reinforce(Allies, { "jeep", "jeep" }, { AlliedReinforcementPoint.Location, DefaultCameraPosition.Location })
		Utils.Do(OpeningAttack, function(a)
			if not a.IsDead then
				a.AttackMove(DefaultCameraPosition.Location)
			end
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Reinforcements.Reinforce(Allies, { "spy", "e1", "e1", "e1", "e3", "e3", "e3" }, { AlliedReinforcementPoint.Location, DefaultCameraPosition.Location })
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Reinforcements.Reinforce(Allies, { "mcv", "2tnk", "2tnk" }, { AlliedReinforcementPoint.Location, DefaultCameraPosition.Location })
	end)

	Trigger.OnKilled(MoneyBarrel, function()
		Actor.Create("moneycrate", true, { Owner = Allies, Location = MoneyBarrel.Location })
	end)
end

Tick = function()
	USSR.Cash = 50000
	BadGuy.Cash = 50000

	if Allies.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(SovietObj)
	end

	if USSR.HasNoRequiredUnits() and BadGuy.HasNoRequiredUnits() then
		Allies.MarkCompletedObjective(DestroyAll)
	end
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	InitObjectives(Allies)

	SovietObj = USSR.AddObjective("Stop the Allies")
	DestroyAll = Allies.AddObjective("Destroy all Soviet units and structures.")

	Camera.Position = DefaultCameraPosition.CenterPosition
	PowerProxy = Actor.Create("paratroopers", false, { Owner = BadGuy })
	Setup()
	ActivateAI()
end
