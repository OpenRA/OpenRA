--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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

GdiBase = { GdiNuke1, GdiProc1, GdiWeap1, GdiNuke2, GdiPyle1, GdiSilo1, GdiSilo2, GdiHarv }
GdiUnits = { "e2", "e2", "e2", "e2", "e1", "e1", "e1", "e1", "mtnk", "mtnk", "jeep", "jeep", "apc" }
NodSams = { Sam1, Sam2, Sam3, Sam4 }


function AllToHunt()
	local list = Nod.GetGroundAttackers()
	Utils.Do(list, function(unit)
		unit.Hunt()
	end)
end


function MoveThenHunt(actors, path)
	Utils.Do(actors, function(actor)
		actor.Patrol(path, false)
		IdleHunt(actor)
	end)
end


function AutoCreateTeam()
	local team = Utils.Random(AutoCreateTeams)
	for type, count in pairs(team.types) do
		MoveThenHunt(Utils.Take(count, Nod.GetActorsByType(type)), team.route)
	end

	Trigger.AfterDelay(Utils.RandomInteger(AutoAtkMinDelay, AutoAtkMaxDelay), AutoCreateTeam)
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
end


function Atk1TriggerFunction()
	MoveThenHunt(Utils.Take(2, Nod.GetActorsByType('e1')), AtkRoute1)
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute1)
end


function Atk2TriggerFunction()
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e1')), AtkRoute2)
	MoveThenHunt(Utils.Take(3, Nod.GetActorsByType('e3')), AtkRoute2)
end


function Atk3TriggerFunction()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute1)
end


function Atk4TriggerFunction()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('bggy')), AtkRoute2)
end


function Atk5TriggerFunction()
	MoveThenHunt(Utils.Take(1, Nod.GetActorsByType('ltnk')), AtkRoute2)
end


function StartProduction(type)
	if Hand1.IsInWorld and Hand1.Owner == Nod then
		Hand1.Build(type)
		Trigger.AfterDelay(DateTime.Seconds(30), function() StartProduction(type) end)
	end
end


function InsertGdiUnits()
	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, GdiUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
end


function IdleHunt(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end


function WorldLoaded()
	InitObjectives(GDI)

	gdiBase = Player.GetPlayer("AbandonedBase")

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Nod and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Nod and building.Health < RepairThreshold * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	gdiObjective1 = GDI.AddPrimaryObjective("Find the GDI base.")
	gdiObjective2 = GDI.AddSecondaryObjective("Destroy all SAM sites to receive air support.")
	nodObjective = Nod.AddPrimaryObjective("Destroy all GDI troops.")

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

	Trigger.OnAllRemovedFromWorld(AllToHuntTrigger, AllToHunt)

	Trigger.AfterDelay(DateTime.Seconds(40), function() StartProduction({ "e1" }) end)

	Trigger.OnPlayerDiscovered(gdiBase, DiscoverGdiBase)

	Trigger.OnAllKilled(NodSams, function()
		GDI.MarkCompletedObjective(gdiObjective2)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Camera.Position = UnitsRally.CenterPosition

	InsertGdiUnits()
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
