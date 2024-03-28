--[[
   Copyright (c) The OpenRA Developers and Contributors
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

AtkRoute1 = { waypoint4, waypoint5, waypoint6, waypoint7, waypoint8, GDIBaseCenter }
AtkRoute2 = { waypoint0, waypoint1, waypoint2, waypoint3, GDIBaseCenter }

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

GDIBase = { GdiNuke1, GdiProc1, GdiWeap1, GdiNuke2, GdiPyle1, GdiSilo1, GdiSilo2 }
GDIUnits = { "e2", "e2", "e2", "e2", "e1", "e1", "e1", "e1", "mtnk", "mtnk", "jeep", "jeep", "apc" }
NodSams = { Sam1, Sam2, Sam3, Sam4 }
NodAttackers = { }
EarlyAttackTimer = 0

SendAttackers = function(actors, path)
	Utils.Do(actors, function(actor)
		local id = #NodAttackers + 1
		NodAttackers[id] = actor

		Trigger.OnKilled(actor, function()
			NodAttackers[id] = nil
		end)
	end)

	MoveAndHunt(actors, path)
end

AutoCreateTeam = function()
	local team = Utils.Random(AutoCreateTeams)
	for type, count in pairs(team.types) do
		SendAttackers(Utils.Take(count, Nod.GetActorsByType(type)), team.route)
	end

	Trigger.AfterDelay(Utils.RandomInteger(AutoAtkMinDelay, AutoAtkMaxDelay), AutoCreateTeam)
end

DiscoverGDIBase = function(_, discoverer)
	if BaseDiscovered or not discoverer == GDI then
		return
	end

	Utils.Do(GDIBase, function(actor)
		actor.Owner = GDI
	end)

	BaseDiscovered = true

	EliminateNod = AddPrimaryObjective(GDI, "eliminate-nod")
	GDI.MarkCompletedObjective(FindBase)

	-- Delay spawn to avoid wasted tiberium and enemy attention.
	if not GdiProc1.IsDead then
		local origin = GdiProc1.Location + CVec.New(2, 3)
		Reinforcements.Reinforce(GDI, { "harv" }, { origin, origin + CVec.New(-2, 0) })
	end
end

LoseGDIBase = function(location)
	if BaseDiscovered then
		return
	end

	GDI.MarkFailedObjective(FindBase)
	Actor.Create("camera", true, { Owner = GDI, Location = location })
	Camera.Position = Map.CenterOfCell(location)
end

Atk1TriggerFunction = function()
	SendAttackers(Utils.Take(2, Nod.GetActorsByType('e1')), AtkRoute1)
	SendAttackers(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute1)
end

Atk2TriggerFunction = function()
	SendAttackers(Utils.Take(3, Nod.GetActorsByType('e1')), AtkRoute2)
	SendAttackers(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute2)
end

Atk3TriggerFunction = function()
	SendAttackers(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute1)
end

Atk4TriggerFunction = function()
	SendAttackers(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute2)
end

Atk5TriggerFunction = function()
	SendAttackers(Utils.Take(1, Nod.GetActorsByType('ltnk')), AtkRoute2)
end

InsertGDIUnits = function()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GDIUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
end

ScheduleNodAttacks = function()
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

	Trigger.AfterDelay(DateTime.Seconds(40), function()
		local delay = function() return DateTime.Seconds(30) end
		local toBuild = function() return { "e1" } end
		ProduceUnits(Nod, Hand1, delay, toBuild)
	end)

	Trigger.OnAllRemovedFromWorld(AllToHuntTrigger, function()
		Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
	end)

	local baseDefenses = Nod.GetActorsByTypes({ "sam", "gun" })
	local pullBuildings = Utils.Concat(AllToHuntTrigger, baseDefenses)

	Utils.Do(pullBuildings, function(building)
		-- Skip the lone SAM that is not part of the Nod base.
		if building == Sam4 then
			return
		end

		Trigger.OnDamaged(building, OnNodBaseDamaged)
	end)
end

OnNodBaseDamaged = function(building, attacker)
	if BaseDiscovered then
		return
	end

	if EarlyAttackTimer > 0 or attacker.Owner ~= GDI then
		return
	end

	EarlyAttackTimer = DateTime.Seconds(10)

	if attacker.IsDead then
		PullAttackers(building.Location)
		return
	end

	PullAttackers(attacker.Location)
end

PullAttackers = function(location)
	Utils.Do(NodAttackers, function(attacker)
		if attacker.IsDead or attacker.Stance == "Defend" then
			return
		end

		-- Ignore structures.
		attacker.Stance = "Defend"
		attacker.Stop()
		attacker.AttackMove(location, 2)
		attacker.CallFunc(function()
			-- No targets nearby. Reset stance for IdleHunt.
			attacker.Stance = "AttackAnything"
		end)
	end)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	AbandonedBase = Player.GetPlayer("AbandonedBase")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	RepairNamedActors(Nod, RepairThreshold)

	FindBase = AddPrimaryObjective(GDI, "find-gdi-base")
	DestroySAMs = AddSecondaryObjective(GDI, "destroy-sams")
	NodObjective = AddPrimaryObjective(Nod, "")

	Trigger.OnPlayerDiscovered(AbandonedBase, DiscoverGDIBase)

	local revealCell = GdiNuke1.Location
	Trigger.OnAllKilled(GDIBase, function()
		LoseGDIBase(revealCell)
	end)

	Trigger.OnAllKilled(NodSams, function()
		GDI.MarkCompletedObjective(DestroySAMs)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Camera.Position = UnitsRally.CenterPosition

	InsertGDIUnits()
	ScheduleNodAttacks()
end

Tick = function()
	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective)
	end

	if not BaseDiscovered then
		EarlyAttackTimer = EarlyAttackTimer - 1
		return
	end

	if Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(EliminateNod)
	end
end
