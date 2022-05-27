--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

InfantryReinforcements = { "e1", "e1", "e2" }
JeepReinforcements = { "jeep" }
TankReinforcements = { "mtnk" }
BaseReinforcements = { "mcv" }
GDIBaseBuildings = { "pyle", "fact", "nuke", "hq", "weap" }

SamSites = { sam1, sam2, sam3, sam4 }
NodBase = { handofnod, nodpower1, nodpower2, nodpower3, nodairfield, nodrefinery, nodconyard }
HiddenNodUnits = { sleeper1, sleeper2, sleeper3, sleeper4 }

SendReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(20), function()
		ReinforceWithLandingCraft(GDI, BaseReinforcements, spawnpoint3.Location - CVec.New(0, -4), spawnpoint3.Location - CVec.New(0, -1), waypoint26.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		ReinforceWithLandingCraft(GDI, TankReinforcements, spawnpoint2.Location - CVec.New(0, -4), spawnpoint2.Location - CVec.New(0, -1), waypoint10.Location)
		ReinforceWithLandingCraft(GDI, TankReinforcements, spawnpoint3.Location - CVec.New(0, -4), spawnpoint3.Location - CVec.New(0, -1), waypoint10.Location)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		ReinforceWithLandingCraft(GDI, JeepReinforcements, spawnpoint1.Location - CVec.New(0, -4), spawnpoint1.Location - CVec.New(0, -1), waypoint10.Location)
	end)

	ReinforceWithLandingCraft(GDI, InfantryReinforcements, spawnpoint2.Location - CVec.New(0, -4), spawnpoint2.Location - CVec.New(0, -1), waypoint10.Location)
	ReinforceWithLandingCraft(GDI, InfantryReinforcements, spawnpoint3.Location - CVec.New(0, -4), spawnpoint3.Location - CVec.New(0, -1), waypoint10.Location)
end

AttackPlayer = function()
	Trigger.AfterDelay(DateTime.Seconds(40), function()
		for type, count in pairs({ ['e3'] = 3, ['e4'] = 2 }) do
			atk1Actors = Utils.Take(count, Nod.GetActorsByType(type))
			Utils.Do(atk1Actors, function(unit)
				unit.Move(waypoint6.Location)
				unit.Move(waypoint7.Location)
				unit.Move(waypoint8.Location)
				unit.Move(waypoint9.Location)
				unit.Move(waypoint10.Location)
				unit.AttackMove(waypoint11.Location)
			end)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(40), function()
		for type, count in pairs({ ['e1'] = 3, ['e3'] = 2 }) do
			atk2Actors = Utils.Take(count, Nod.GetActorsByType(type))
			Utils.Do(atk2Actors, function(unit)
				unit.Move(waypoint11.Location)
				unit.Move(waypoint12.Location)
				unit.Move(waypoint15.Location)
				unit.Move(waypoint16.Location)
				unit.Hunt()
			end)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(80), function()
		for type, count in pairs({ ['e3'] = 3, ['e4'] = 2 }) do
			atk3Actors = Utils.Take(count, Nod.GetActorsByType(type))
			Utils.Do(atk3Actors, function(unit)
				unit.Move(waypoint6.Location)
				unit.Move(waypoint7.Location)
				unit.Move(waypoint8.Location)
				unit.Move(waypoint9.Location)
				unit.Move(waypoint10.Location)
				unit.AttackMove(waypoint11.Location)
			end)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(80), function()
		for type, count in pairs({ ['e1'] = 3, ['e3'] = 2 }) do
			atk4Actors = Utils.Take(count, Nod.GetActorsByType(type))
			Utils.Do(atk4Actors, function(unit)
				unit.Move(waypoint11.Location)
				unit.Move(waypoint12.Location)
				unit.Move(waypoint15.Location)
				unit.Move(waypoint16.Location)
				unit.AttackMove(waypoint11.Location)
			end)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(80), function()
		atk5Actors = Utils.Take(2, Nod.GetActorsByType('bggy'))
		Utils.Do(atk5Actors, function(unit)
			unit.Move(waypoint11.Location)
			unit.Move(waypoint12.Location)
			unit.Move(waypoint15.Location)
			unit.Move(waypoint16.Location)
			unit.Hunt()
		end)
	end)

	Utils.Do(NodBase, function(actor)
		Trigger.OnRemovedFromWorld(actor, function()
			Utils.Do(Nod.GetGroundAttackers(Nod), IdleHunt)
		end)
	end)

	Utils.Do(HiddenNodUnits, IdleHunt)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Camera.Position = spawnpoint2.CenterPosition

	InitObjectives(GDI)

	DestroyNod = GDI.AddObjective("Destroy remaining Nod structures and units.")
	ConstructBase = GDI.AddObjective("Construct all available buildings.", "Secondary", false)

	SendReinforcements()

	local destroySAMs = GDI.AddSecondaryObjective("Destroy the SAM sites to receive air support.")
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(destroySAMs)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	AttackPlayer()
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) then
		if GDI.HasNoRequiredUnits()  then
			GDI.MarkFailedObjective(DestroyNod)
		end

		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(DestroyNod)
		end

		if not GDI.IsObjectiveCompleted(ConstructBase) and DateTime.GameTime % DateTime.Seconds(1) == 0 and CheckForBase(GDI, GDIBaseBuildings) then
			GDI.MarkCompletedObjective(ConstructBase)
		end
	end
end
