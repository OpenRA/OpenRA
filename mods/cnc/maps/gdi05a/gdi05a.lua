--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

RepairThreshold = { easy = 0.3, normal = 0.6, hard = 0.9 }

ActorRemovals =
{
	easy = { Actor167, Actor168, Actor190, Actor191, Actor193, Actor194, Actor196, Actor198, Actor200 },
	normal = { Actor167, Actor194, Actor196, Actor197 },
	hard = { },
}

GdiTanks = { "mtnk", "mtnk" }
GdiApc = { "apc" }
GdiInfantry = { "e1", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e2" }
GDIBase = { GdiNuke1, GdiNuke2, GdiProc, GdiSilo1, GdiSilo2, GdiPyle, GdiWeap, GdiHarv }
NodSams = { Sam1, Sam2, Sam3, Sam4 }
CoreNodBase = { NodConYard, NodRefinery, HandOfNod, Airfield }

Guard1UnitTypes = { "bggy" }
Guard1Path = { waypoint4.Location, waypoint5.Location, waypoint10.Location }
Guard1Delay = { easy = DateTime.Minutes(2), normal = DateTime.Minutes(1), hard = DateTime.Seconds(30) }
Guard2UnitTypes = { "bggy" }
Guard2Path = { waypoint0.Location, waypoint1.Location, waypoint2.Location }
Guard3Path = { waypoint4.Location, waypoint5.Location, waypoint9.Location }

AttackDelayMin = { easy = DateTime.Minutes(1), normal = DateTime.Seconds(45), hard = DateTime.Seconds(30) }
AttackDelayMax = { easy = DateTime.Minutes(2), normal = DateTime.Seconds(90), hard = DateTime.Minutes(1) }
AttackUnitTypes =
{
	easy =
	{
		{ factory = HandOfNod, types = { "e1", "e1" } },
		{ factory = HandOfNod, types = { "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e3", "e3" } },
	},
	normal =
	{
		{ factory = HandOfNod, types = { "e1", "e1", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3" } },
		{ factory = Airfield, types = { "bggy" } },
	},
	hard =
	{
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e1", "e3", "e3" } },
		{ factory = HandOfNod, types = { "e1", "e1", "e3", "e3", "e3" } },
		{ factory = Airfield, types = { "bggy" } },
		{ factory = Airfield, types = { "ltnk" } },
	}
}

AttackPaths =
{
	{ waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location },
	{ waypoint4.Location, waypoint9.Location, waypoint7.Location, waypoint8.Location },
}

Attack = function()
	local production = Utils.Random(AttackUnitTypes[Difficulty])
	local path = Utils.Random(AttackPaths)
	local toBuild = function() return production.types end
	ProduceUnits(Nod, production.factory, nil, toBuild, function(units)
		Utils.Do(units, function(unit)
			unit.Patrol(path, false)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(AttackDelayMin[Difficulty], AttackDelayMax[Difficulty]), Attack)
end

Guard1Action = function()
	ProduceUnits(Nod, Airfield, nil, function() return Guard1UnitTypes end, function(units)
		Trigger.OnAllKilled(units, function()
			Trigger.AfterDelay(Guard1Delay[Difficulty], Guard1Action)
		end)

		Utils.Do(units, function(unit)
			unit.Patrol(Guard1Path, true, DateTime.Seconds(7))
		end)
	end)
end

Guard2Action = function()
	ProduceUnits(Nod, Airfield, nil, function() return Guard2UnitTypes end, function(units)
		Utils.Do(units, function(unit)
			unit.Patrol(Guard2Path, true, DateTime.Seconds(5))
		end)
	end)
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

	Attack()
end

SetupWorld = function()
	Utils.Do(ActorRemovals[Difficulty], function(unit)
		unit.Destroy()
	end)

	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GdiTanks, { GdiTankEntry.Location, GdiTankRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(GDI, GdiApc, { GdiApcEntry.Location, GdiApcRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(GDI, GdiInfantry, { GdiInfantryEntry.Location, GdiInfantryRallyPoint.Location }, 15, function(actor) actor.Stance = "Defend" end)

	Trigger.OnPlayerDiscovered(AbandonedBase, DiscoverGDIBase)

	RepairNamedActors(Nod, RepairThreshold[Difficulty])

	Trigger.OnAllKilled(NodSams, function()
		GDI.MarkCompletedObjective(DestroySAMs)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Trigger.AfterDelay(0, function()
		GdiHarv.Stop()
	end)

	if Difficulty ~= "easy" then
		Trigger.OnDamaged(NodHarv, function()
			Utils.Do(Nod.GetGroundAttackers(), function(unit)
				unit.AttackMove(NodHarv.Location)
				if Difficulty == "hard" then
					IdleHunt(unit)
				end
			end)
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(45), Guard1Action)
	Trigger.AfterDelay(DateTime.Minutes(3), Guard2Action)

	Trigger.OnKilled(GuardTank1, function()
		if not GuardTank2.IsDead then
			GuardTank2.Patrol(Guard3Path, true, DateTime.Seconds(11))
		end
	end)

	GuardTank1.Patrol(Guard3Path, true, DateTime.Seconds(11))
end

WorldLoaded = function()
	AbandonedBase = Player.GetPlayer("AbandonedBase")
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	NodObjective = Nod.AddObjective("Destroy all GDI troops.")
	FindBase = GDI.AddObjective("Find the GDI base.")
	DestroySAMs = GDI.AddObjective("Destroy all SAM sites to receive air support.", "Secondary", false)

	SetupWorld()

	Camera.Position = GdiTankRallyPoint.CenterPosition
end

Tick = function()
	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective)
	end

	if BaseDiscovered and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(EliminateNod)
	end
end
