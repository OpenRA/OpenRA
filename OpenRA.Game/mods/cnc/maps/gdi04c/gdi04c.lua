--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

LoseTriggerHouses = { TrigLos2Farm1, TrigLos2Farm2, TrigLos2Farm3, TrigLos2Farm4 }
TownAttackTrigger = { CPos.New(54, 38), CPos.New(55, 38), CPos.New(56, 38), CPos.New(57, 38) }
GDIReinforcementsTrigger = { CPos.New(32, 51), CPos.New(32, 52), CPos.New(33, 52) }

GDIReinforcementsPart1 = { "jeep", "jeep" }
GDIReinforcementsPart2 = { "e2", "e2", "e2", "e2", "e2" }
TownAttackWave1 = { "bggy", "bggy" }
TownAttackWave2 = { "ltnk", "ltnk" }
TownAttackWave3 = { "e1", "e1", "e1", "e3", "e3", "e3" }

Civvie1Wpts = { CivvieWpts1, CivvieWpts2 }
Civvie2Wpts = { CivvieWpts3, CivvieWpts1, CivvieWpts4, CivvieWpts5, CivvieWpts6, CivvieWpts7, CivvieWpts8, CivvieWpts9, CivvieWpts10, CivvieWpts11 }

FollowCivvieWpts = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		actor.Move(wpt.Location, 2)
		actor.Wait(DateTime.Seconds(2))
	end)
end

FollowWaypoints = function(actor, wpts)
	Utils.Do(wpts, function(wpt)
		actor.AttackMove(wpt.Location, 2)
	end)
end

TownAttackersIdleAction = function(actor)
	actor.AttackMove(TownAttackWpt.Location, 2)
	actor.Hunt()
end

TownAttackAction = function(actor)
	Trigger.OnIdle(actor, TownAttackersIdleAction)
end

AttackTown = function()
	Reinforcements.Reinforce(Nod, TownAttackWave1, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(0.25), TownAttackAction)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Reinforcements.Reinforce(Nod, TownAttackWave2, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(1), TownAttackAction)
	end)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Reinforcements.Reinforce(Nod, TownAttackWave3, { NodReinfEntry.Location, NodReinfRally.Location }, DateTime.Seconds(1), TownAttackAction)
	end)
end

SendGDIReinforcements = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GDIReinforcementsPart1, { GDIReinfEntry1.Location, GDIReinfRally1.Location, GDIReinfRally3.Location }, DateTime.Seconds(1))

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(GDI, "Reinforce")
		local apc = Reinforcements.ReinforceWithTransport(GDI, "apc", GDIReinforcementsPart2, { GDIReinfEntry2.Location, GDIReinfRally2.Location, GDIUnloadWpt.Location })[1]
		apc.UnloadPassengers()
	end)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	Trigger.OnAllKilled(LoseTriggerHouses, function()
		GDI.MarkFailedObjective(DefendTown)
	end)

	NodObjective = Nod.AddPrimaryObjective("Destroy all GDI troops.")
	DefendTown = GDI.AddPrimaryObjective("Defend the town of Białystok.")
	EliminateNod = GDI.AddPrimaryObjective("Eliminate all Nod forces in the area.")

	Trigger.OnExitedFootprint(TownAttackTrigger, function(a, id)
		if not TownAttackTriggered and a.Owner == GDI then
			TownAttackTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			AttackTown()
		end
	end)

	Trigger.OnEnteredFootprint(GDIReinforcementsTrigger, function(a, id)
		if not GDIReinforcementsTriggered and a.Owner == GDI then
			GDIReinforcementsTriggered = true
			Trigger.RemoveFootprintTrigger(id)
			SendGDIReinforcements()
		end
	end)

	Trigger.AfterDelay(1, function()
		FollowCivvieWpts(civvie1, Civvie1Wpts)
		FollowCivvieWpts(civvie2, Civvie2Wpts)
	end)

	Camera.Position = Actor141.CenterPosition
end

Tick = function()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective)
	end

	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(DefendTown)
		GDI.MarkCompletedObjective(EliminateNod)
	end
end
