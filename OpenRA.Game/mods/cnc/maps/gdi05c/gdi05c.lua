--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

RepairThreshold = { easy = 0.3, normal = 0.6, hard = 0.9 }

ActorRemovals =
{
	easy = { Actor197, Actor198, Actor199, Actor162, Actor178, Actor181, Actor171, Actor163 },
	normal = { Actor197, Actor162 },
	hard = { },
}

AllToHuntTrigger = { Silo1, Proc1, Silo2, Radar1, Afld1, Hand1, Nuke1, Nuke2, Nuke3, Fact1 }

AtkRoute1 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4 }
AtkRoute2 = { waypoint0, waypoint8, waypoint4 }
AtkRoute3 = { waypoint0, waypoint1, waypoint2, waypoint5, waypoint6, waypoint7 }
AtkRoute4 = { waypoint0, waypoint8, waypoint9, waypoint10, waypoint11 }

AutoCreateTeams =
{
	{ types = { e1 = 2, e3 = 2 }, route = AtkRoute1 },
	{ types = { e1 = 1, e3 = 2 }, route = AtkRoute2 },
	{ types = { ltnk = 1 }      , route = AtkRoute2 },
	{ types = { bggy = 1 }      , route = AtkRoute2 },
	{ types = { bggy = 1 }      , route = AtkRoute1 },
	{ types = { ltnk = 1 }      , route = AtkRoute3 },
	{ types = { bggy = 1 }      , route = AtkRoute4 }
}

Atk1Delay = { easy = DateTime.Seconds(60), normal = DateTime.Seconds(50), hard = DateTime.Seconds(40) }
Atk2Delay = { easy = DateTime.Seconds(90), normal = DateTime.Seconds(70), hard = DateTime.Seconds(50) }
Atk3Delay = { easy = DateTime.Seconds(120), normal = DateTime.Seconds(90), hard = DateTime.Seconds(70) }
Atk4Delay = { easy = DateTime.Seconds(150), normal = DateTime.Seconds(110), hard = DateTime.Seconds(90) }
AutoAtkStartDelay = { easy = DateTime.Seconds(150), normal = DateTime.Seconds(120), hard = DateTime.Seconds(90) }
AutoAtkMinDelay = { easy = DateTime.Minutes(1), normal = DateTime.Seconds(45), hard = DateTime.Seconds(30) }
AutoAtkMaxDelay = { easy = DateTime.Minutes(2), normal = DateTime.Seconds(90), hard = DateTime.Minutes(1) }

GDIBase = { GdiNuke1, GdiProc1, GdiWeap1, GdiNuke2, GdiNuke3, GdiPyle1, GdiSilo1, GdiRadar1 }
GDIUnits = { "e2", "e2", "e2", "e2", "e1", "e1", "e1", "e1", "mtnk", "mtnk", "jeep", "apc", "apc" }
GDIHarvester = { "harv" }
NodSams = { Sam1, Sam2, Sam3 }

AutoCreateTeam = function()
	local team = Utils.Random(AutoCreateTeams)
	for type, count in pairs(team.types) do
		MoveAndHunt(Utils.Take(count, Nod.GetActorsByType(type)), team.route)
	end

	Trigger.AfterDelay(Utils.RandomInteger(AutoAtkMinDelay[Difficulty], AutoAtkMaxDelay[Difficulty]), AutoCreateTeam)
end

DiscoverGDIBase = function(actor, discoverer)
	if BaseDiscovered or not discoverer == GDI then
		return
	end

	--Spawn harv only when base is discovered, otherwise it will waste tiberium and might get killed before the player arrives
	if not GdiProc1.IsDead then
		Reinforcements.Reinforce(GDI, GDIHarvester, { GdiProc1.Location + CVec.New(0, 2), GdiProc1.Location + CVec.New(-1, 3) }, 1)
	end

	Utils.Do(GDIBase, function(actor)
		actor.Owner = GDI
	end)

	BaseDiscovered = true

	EliminateNod = GDI.AddObjective("Eliminate all Nod forces in the area.")
	GDI.MarkCompletedObjective(FindBase)
end

Atk1TriggerFunction = function()
	MoveAndHunt(Utils.Take(2, Nod.GetActorsByType('e1')), AtkRoute1)
	MoveAndHunt(Utils.Take(2, Nod.GetActorsByType('e3')), AtkRoute1)
end

Atk2TriggerFunction = function()
	MoveAndHunt(Utils.Take(4, Nod.GetActorsByType('e3')), AtkRoute2)
end

Atk3TriggerFunction = function()
	MoveAndHunt(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute2)
end

Atk4TriggerFunction = function()
	MoveAndHunt(Utils.Take(2, Nod.GetActorsByType('e1')), AtkRoute1)
	MoveAndHunt(Utils.Take(1, Nod.GetActorsByType('ltnk')), AtkRoute1)
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

	RepairNamedActors(Nod, RepairThreshold[Difficulty])

	FindBase = GDI.AddObjective("Find the GDI base.")
	DestroySAMs = GDI.AddObjective("Destroy all SAM sites to receive air support.", "Secondary", false)
	NodObjective = Nod.AddObjective("Destroy all GDI troops.")

	Utils.Do(ActorRemovals[Difficulty], function(unit)
		unit.Destroy()
	end)

	Trigger.AfterDelay(Atk1Delay[Difficulty], Atk1TriggerFunction)
	Trigger.AfterDelay(Atk2Delay[Difficulty], Atk2TriggerFunction)
	Trigger.AfterDelay(Atk3Delay[Difficulty], Atk3TriggerFunction)
	Trigger.AfterDelay(Atk4Delay[Difficulty], Atk4TriggerFunction)

	Trigger.AfterDelay(AutoAtkStartDelay[Difficulty], AutoCreateTeam)

	Trigger.OnAllKilledOrCaptured(AllToHuntTrigger, function()
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
