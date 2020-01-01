--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AllToHuntTrigger =
{
	Silo1, Proc1, Silo2, Silo3, Silo4, Afld1, Hand1, Nuke1, Nuke2, Nuke3, Fact1
}

AtkRoute1 = { waypoint4.Location, waypoint5.Location, waypoint6.Location, waypoint7.Location, waypoint8.Location }
AtkRoute2 = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location }

AutoCreateTeams =
{
	{ types = { e1 = 1, e3 = 3 }, route = AtkRoute2 },
	{ types = { e1 = 3, e3 = 1 }, route = AtkRoute2 },
	{ types = { e3 = 4 }        , route = AtkRoute1 },
	{ types = { e1 = 4 }        , route = AtkRoute1 },
	{ types = { bggy = 1 }      , route = AtkRoute1 },
	{ types = { bggy = 1 }      , route = AtkRoute2 },
	{ types = { ltnk = 1 }      , route = AtkRoute1 },
	{ types = { ltnk = 1 }      , route = AtkRoute2 }
}

RepairThreshold = 0.6

Atk1Delay = DateTime.Seconds(40)
Atk2Delay = DateTime.Seconds(60)
Atk3Delay = DateTime.Seconds(70)
Atk4Delay = DateTime.Seconds(90)
AutoAtkStartDelay = DateTime.Seconds(115)
AutoAtkMinDelay = DateTime.Seconds(45)
AutoAtkMaxDelay = DateTime.Seconds(90)

Atk5CellTriggers =
{
	CPos.New(17,55), CPos.New(16,55), CPos.New(15,55), CPos.New(50,54), CPos.New(49,54),
	CPos.New(48,54), CPos.New(16,54), CPos.New(15,54), CPos.New(14,54), CPos.New(50,53),
	CPos.New(49,53), CPos.New(48,53), CPos.New(50,52), CPos.New(49,52)
}

GDIBase = { GdiNuke1, GdiProc1, GdiWeap1, GdiNuke2, GdiPyle1, GdiSilo1, GdiSilo2, GdiHarv }
GDIUnits = { "e2", "e2", "e2", "e2", "e1", "e1", "e1", "e1", "mtnk", "mtnk", "jeep", "jeep", "apc" }
NodSams = { Sam1, Sam2, Sam3, Sam4 }

MoveThenHunt = function(actors, path)
	Utils.Do(actors, function(actor)
		actor.Patrol(path, false)
		IdleHunt(actor)
	end)
end

AutoCreateTeam = function()
	local team = Utils.Random(AutoCreateTeams)
	for type, count in pairs(team.types) do
		MoveThenHunt(Utils.Take(count, Nod.GetActorsByType(type)), team.route)
	end

	Trigger.AfterDelay(Utils.RandomInteger(AutoAtkMinDelay, AutoAtkMaxDelay), AutoCreateTeam)
end

DiscoverGDIBase = function(actor, discoverer)
	if BaseDiscovered or not discoverer == GDI then
		return
	end

	Utils.Do(GDIBase, function(actor)
		actor.Owner = GDI
	end)

	BaseDiscovered = true

	EliminateNod = GDI.AddObjective("Eliminate all Nod forces in the area.")
	GDI.MarkCompletedObjective(FindBase)
end

Atk1TriggerFunction = function()
	MoveThenHunt(Utils.Take(2, Nod.GetActorsByType('e1')), AtkRoute1)
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute1)
end

Atk2TriggerFunction = function()
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e1')), AtkRoute2)
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute2)
end

Atk3TriggerFunction = function()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute1)
end

Atk4TriggerFunction = function()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute2)
end

Atk5TriggerFunction = function()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('ltnk')), AtkRoute2)
end

InsertGDIUnits = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GDIUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	AbandonedBase = Player.GetPlayer("AbandonedBase")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	RepairNamedActors(Nod, RepairThreshold)

	FindBase = GDI.AddObjective("Find the GDI base.")
	DestroySAMs = GDI.AddObjective("Destroy all SAM sites to receive air support.", "Secondary", false)
	NodObjective = Nod.AddObjective("Destroy all GDI troops.")

	Trigger.AfterDelay(Atk1Delay, Atk1TriggerFunction)
	Trigger.AfterDelay(Atk2Delay, Atk2TriggerFunction)
	Trigger.AfterDelay(Atk3Delay, Atk3TriggerFunction)
	Trigger.AfterDelay(Atk4Delay, Atk4TriggerFunction)
	Trigger.OnEnteredFootprint(Atk5CellTriggers, function(a, id)
		if a.Owner == GDI then
			Atk5TriggerFunction()
			Trigger.RemoveFootprintTrigger(id)
		end
	end)
	Trigger.AfterDelay(AutoAtkStartDelay, AutoCreateTeam)

	Trigger.OnAllRemovedFromWorld(AllToHuntTrigger, function()
		Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(DateTime.Seconds(40), function()
		local delay = function() return DateTime.Seconds(30) end
		local toBuild = function() return { "e1" } end
		ProduceUnits(Nod, Hand1, delay, toBuild)
	end)

	Trigger.OnPlayerDiscovered(AbandonedBase, DiscoverGDIBase)

	Trigger.OnAllKilled(NodSams, function()
		GDI.MarkCompletedObjective(DestroySAMs)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Camera.Position = UnitsRally.CenterPosition

	InsertGDIUnits()
end

Tick = function()
	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective)
	end

	if BaseDiscovered and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(EliminateNod)
	end
end
