--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

NodUnitsVehicles1 = { "bggy", "bggy", "bike", "bike" }
NodUnitsVehicles2 = { "ltnk", "ltnk" }
NodUnitsEngineers = { "e6", "e6", "e6", "e6" }
NodUnitsRockets = { "e3", "e3", "e3", "e3" }
NodUnitsGunners = { "e1", "e1", "e1", "e1" }
NodUnitsFlamers = { "e4", "e4", "e4", "e4" }

MainEntranceGuards = { JeepGuard1, JeepGuard2, MediumTank1, SideGuard1, SideGuard2, SideGuard6, SideGuard7, Guard1, Guard2, Guard3, Guard4, Guard5, Guard6, Guard7, Guard8, Guard9, Guard10 }
InfantryPatrol = { InfantryPatrol1, InfantryPatrol2 }

MainEntrancePatrolPath = { waypoint0.Location, waypoint1.Location, waypoint15.Location, waypoint12.Location }
InfantryPatrolPath = { waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint8.Location, waypoint2.Location, waypoint9.Location, waypoint2.Location }
JeepPatrolPath = { waypoint0.Location, waypoint1.Location, waypoint3.Location, waypoint4.Location, waypoint3.Location, waypoint2.Location, waypoint5.Location, waypoint6.Location, waypoint2.Location, waypoint7.Location }
MediumTankPatrolPath = { waypoint14.Location, waypoint5.Location }

TargetsKilled = 0

InsertNodUnits = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, { "ltnk" }, { ReinforcementsTopSpawn.Location, ReinforcementsTankRally.Location }, 1)

	local engineers = Reinforcements.Reinforce(Nod, NodUnitsEngineers, { ReinforcementsTopSpawn.Location, ReinforcementsEngineersRally.Location }, 10)
	Reinforcements.Reinforce(Nod, NodUnitsRockets, { ReinforcementsBottomSpawn.Location, ReinforcementsRocketsRally.Location }, 10)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		Reinforcements.Reinforce(Nod, NodUnitsGunners, { ReinforcementsBottomSpawn.Location, ReinforcementsGunnersRally.Location }, 10)
		Reinforcements.Reinforce(Nod, NodUnitsFlamers, { ReinforcementsTopSpawn.Location, ReinforcementsFlamersRally.Location }, 10)
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Reinforcements.ReinforceWithTransport(Nod, "tran.in", NodUnitsVehicles1, { GunboatRight.Location, ReinforcementsHelicopter1Rally.Location }, { GunboatRight.Location })

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Reinforcements.ReinforceWithTransport(Nod, "tran.in", NodUnitsVehicles2, { GunboatRight.Location, ReinforcementsHelicopter2Rally.Location }, { GunboatRight.Location })
		end)
	end)

	Trigger.OnAllRemovedFromWorld(engineers, function()
		if not Nod.IsObjectiveCompleted(CaptureHelipad) then
			Nod.MarkFailedObjective(CaptureHelipad)
		end
	end)
end

DiscoveredMainEntrance = function()
	if Nod.IsObjectiveCompleted(DistractGuardsObjective) then
		return
	end

	Nod.MarkCompletedObjective(DistractGuardsObjective)

	Utils.Do(MainEntranceGuards, function(guard)
		if not guard.IsDead then
			guard.Patrol(MainEntrancePatrolPath)
		end
	end)
end

Trigger.OnKilled(GDIHpad, function()
	if not Nod.IsObjectiveCompleted(CaptureHelipad) then
		Nod.MarkFailedObjective(CaptureHelipad)
	end
end)

Trigger.OnKilled(GDIOrca, function()
	if not Nod.IsObjectiveCompleted(UseOrcaObjective) then
		Nod.MarkFailedObjective(UseOrcaObjective)
	end
end)

Trigger.OnDamaged(GuardTower3, function()
	if not MediumTank3.IsDead then
		MediumTank3.Patrol(MediumTankPatrolPath)
	end
end)

Utils.Do(Map.ActorsWithTag("Village"), function(actor)
	Trigger.OnKilled(actor, function()
		TargetsKilled = TargetsKilled + 1

		if TargetsKilled >= 15 then
			Nod.MarkCompletedObjective(NoCaptureObjective)
			Nod.MarkCompletedObjective(UseOrcaObjective)
		end
	end)
end)

Utils.Do(Map.ActorsWithTag("GDIBuilding"), function(actor)
	Trigger.OnKilledOrCaptured(actor, function()
		Nod.MarkFailedObjective(NoCaptureObjective)
	end)
end)

Trigger.OnCapture(GDIHpad, function()
	Nod.MarkCompletedObjective(CaptureHelipad)
	if not GDIOrca.IsDead then
		GDIOrca.Owner = Nod
	end

	Actor.Create("camera", true, { Owner = Nod, Location = waypoint25.Location })
	Actor.Create("flare", true, { Owner = Nod, Location = waypoint25.Location })
end)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	Camera.Position = waypoint26.CenterPosition

	InsertNodUnits()

	Jeep.Patrol(JeepPatrolPath)
	Utils.Do(InfantryPatrol, function(unit)
		unit.Patrol(InfantryPatrolPath)
	end)

	Utils.Do(GDI.GetGroundAttackers(), function(guard)
		Trigger.OnDamaged(guard, function()
			if not guard.IsDead then
				IdleHunt(guard)
			end
		end)
	end)

	InitObjectives(Nod)

	Trigger.OnDiscovered(GuardTower1, DiscoveredMainEntrance)
	Trigger.OnDiscovered(GuardTower2, DiscoveredMainEntrance)
	Trigger.OnDiscovered(GuardTower3, function()
		if not Nod.IsObjectiveCompleted(DistractGuardsObjective) then
			Nod.MarkFailedObjective(DistractGuardsObjective)
		end
	end)

	CaptureHelipad = Nod.AddObjective("Capture the GDI helipad.")
	NoCaptureObjective = Nod.AddObjective("Don't capture or destroy any other\nGDI main building.")
	UseOrcaObjective = Nod.AddObjective("Use the GDI orca to wreak havoc at the village.")
	DistractGuardsObjective = Nod.AddObjective("Distract the guards by attacking the\nmain entrance with your vehicles.", "Secondary", false)
	GDIObjective = GDI.AddObjective("Kill all enemies.")
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if GDI.Resources >= GDI.ResourceCapacity * 0.75 then
		GDI.Resources = GDI.ResourceCapacity * 0.25
	end
end
