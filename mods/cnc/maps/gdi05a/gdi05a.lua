--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
GdiBase = { GdiNuke1, GdiNuke2, GdiProc, GdiSilo1, GdiSilo2, GdiPyle, GdiWeap, GdiHarv }
NodSams = { Sam1, Sam2, Sam3, Sam4 }
CoreNodBase = { NodConYard, NodRefinery, HandOfNod, Airfield }

Grd1UnitTypes = { "bggy" }
Grd1Path = { waypoint4.Location, waypoint5.Location, waypoint10.Location }
Grd1Delay = { easy = DateTime.Minutes(2), normal = DateTime.Minutes(1), hard = DateTime.Seconds(30) }
Grd2UnitTypes = { "bggy" }
Grd2Path = { waypoint0.Location, waypoint1.Location, waypoint2.Location }
Grd3Units = { GuardTank1, GuardTank2 }
Grd3Path = { waypoint4.Location, waypoint5.Location, waypoint9.Location }

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


function Build(factory, units, action)
	if factory.IsDead or factory.Owner ~= Nod then
		return
	end

	if not factory.Build(units, action) then
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Build(factory, units, action)
		end)
	end
end


function Attack()
	local production = Utils.Random(AttackUnitTypes[Difficulty])
	local path = Utils.Random(AttackPaths)
	Build(production.factory, production.types, function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= Nod then return end
			unit.Patrol(path, false)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(AttackDelayMin[Difficulty], AttackDelayMax[Difficulty]), Attack)
end


function Grd1Action()
	Build(Airfield, Grd1UnitTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= Nod then return end
			Trigger.OnKilled(unit, function()
				Trigger.AfterDelay(Grd1Delay[Difficulty], Grd1Action)
			end)
			unit.Patrol(Grd1Path, true, DateTime.Seconds(7))
		end)
	end)
end


function Grd2Action()
	Build(Airfield, Grd2UnitTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= Nod then return end
			unit.Patrol(Grd2Path, true, DateTime.Seconds(5))
		end)
	end)
end


function Grd3Action()
	local unit
	for i, u in ipairs(Grd3Units) do
		if not u.IsDead then
			unit = u
			break
		end
	end

	if unit ~= nil then
		Trigger.OnKilled(unit, function()
			Grd3Action()
		end)

		unit.Patrol(Grd3Path, true, DateTime.Seconds(11))
	end
end


function DiscoverGdiBase(actor, discoverer)
	if baseDiscovered or not discoverer == GDI then
		return
	end

	Utils.Do(GdiBase, function(actor)
		actor.Owner = GDI
	end)

	baseDiscovered = true

	gdiObjective3 = GDI.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	GDI.MarkCompletedObjective(gdiObjective1)

	Attack()
end


function SetupWorld()
	Utils.Do(ActorRemovals[Difficulty], function(unit)
		unit.Destroy()
	end)

	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GdiTanks, { GdiTankEntry.Location, GdiTankRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(GDI, GdiApc, { GdiApcEntry.Location, GdiApcRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(GDI, GdiInfantry, { GdiInfantryEntry.Location, GdiInfantryRallyPoint.Location }, 15, function(actor) actor.Stance = "Defend" end)

	Trigger.OnPlayerDiscovered(gdiBase, DiscoverGdiBase)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Nod and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Nod and building.Health < RepairThreshold[Difficulty] * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Trigger.OnAllKilled(NodSams, function()
		GDI.MarkCompletedObjective(gdiObjective2)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	GdiHarv.Stop()
	NodHarv.FindResources()
	if Difficulty ~= "easy" then
		Trigger.OnDamaged(NodHarv, function()
			Utils.Do(Nod.GetGroundAttackers(), function(unit)
				unit.AttackMove(NodHarv.Location)
				if Difficulty == "hard" then
					unit.Hunt()
				end
			end)
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(45), Grd1Action)
	Trigger.AfterDelay(DateTime.Minutes(3), Grd2Action)
	Grd3Action()
end


function WorldLoaded()
	InitObjectives(GDI)
	gdiBase = Player.GetPlayer("AbandonedBase")

	nodObjective = Nod.AddPrimaryObjective("Destroy all GDI troops.")
	gdiObjective1 = GDI.AddPrimaryObjective("Find the GDI base.")
	gdiObjective2 = GDI.AddSecondaryObjective("Destroy all SAM sites to receive air support.")

	SetupWorld()

	Camera.Position = GdiTankRallyPoint.CenterPosition
end


function Tick()
	if GDI.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			Nod.MarkCompletedObjective(nodObjective)
		end
	end
	if baseDiscovered and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(gdiObjective3)
	end
end
