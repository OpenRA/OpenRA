--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

SamSites = { sam1, sam2, sam3, sam4, sam5 }

NodRetaliateIfDestroyedUnits = { harv1, bggy, ltnk1, ltnk2, ltnk3, arty1, arty2, arty3}

Arty1Delay = { hard = 0, normal = 8, easy = 20 }
Arty2Delay = { hard = 10, normal = 20, easy = 40 }
TerrorTankDelay = { hard = 40, normal = 50, easy = 75 }
TerrorHeliDelay = { hard = 130, normal = 170, easy = 210 }
BaseHeliDelay = { hard = 100, normal = 130, easy = 160 }
NodHelis =
{
	{ delay = DateTime.Seconds(TerrorHeliDelay[Difficulty]), entry = { DefaultChinookTarget.Location, waypoint14.Location }, types = { "e1", "e1", "e4", "e4", "e4" } }, --TERROR, wp14, attack civilians - all 170 timeunits
	{ delay = DateTime.Seconds(0), entry = { DefaultChinookTarget.Location, waypoint13.Location }, types = { "e3", "e3", "e3", "e4", "e4" } }, --Air1, wp13, attack base - triggered on killed units, Harv, some tanks and some buggys...
	{ delay = DateTime.Seconds(BaseHeliDelay[Difficulty]), entry = { DefaultChinookTarget.Location, waypoint0.Location }, types = { "e1", "e3", "e3", "e4", "e4" } } --Air2, wp0, attack base - all 130 timeunits
}

CivilianCasualties = 0
CiviliansKilledThreshold = { hard = 5, normal = 9, easy = 13 } --total 14
Civilians = { civ1, civ2, civ3, civ4, civ5, civ6, civ7, civ8, civ9, civ10, civ11, civ12, civ13, civ14 }

WaypointGroupVillageRight = { waypoint17, waypoint3, waypoint0 }
WaypointGroupVillageLeft = { waypoint17, waypoint14 }
WaypointGroupBaseFrontal = { waypoint7, waypoint11, waypoint31 }
WaypointGroupRightFlankInf = { waypoint7, waypoint8, waypoint10, waypoint8, waypoint9, waypoint31 }
WaypointGroupRightFlank = { waypoint7, waypoint8, waypoint13, waypoint31 }
ArtyWaypoints1 = { waypoint1 }
ArtyWaypoints2 = { waypoint2 }
ArtyWaypoints3 = { waypoint6, waypoint2 }

AutocreateDelay = { hard = 60, normal = 80, easy = 100 }

Auto2 = { units = { ['e4'] = 3, ['e3'] = 4 }, waypoints = WaypointGroupVillageLeft, delay = AutocreateDelay[Difficulty] }
Auto3 = { units = { ['ltnk'] = 1, ['arty'] = 2 }, waypoints = WaypointGroupBaseFrontal, delay = AutocreateDelay[Difficulty] }
Auto4 = { units = { ['arty'] = 2 }, waypoints = ArtyWaypoints1, delay = AutocreateDelay[Difficulty] }
Auto1 = { units = { ['e4'] = 3, ['e3'] = 4 }, waypoints = WaypointGroupBaseFrontal, delay = AutocreateDelay[Difficulty] }
Auto5 = { units = { ['arty'] = 1 }, waypoints = ArtyWaypoints2, delay = AutocreateDelay[Difficulty] }
Auto6 = { units = { ['ltnk'] = 1, ['e4'] = 2 }, waypoints = WaypointGroupBaseFrontal, delay = AutocreateDelay[Difficulty] }
Auto7 = { units = { ['ltnk'] = 1, ['bggy'] = 3 }, waypoints = WaypointGroupBaseFrontal, delay = AutocreateDelay[Difficulty] }
Auto8 = { units = { ['e4'] = 3, ['e3'] = 5 }, waypoints = WaypointGroupRightFlankInf, delay = AutocreateDelay[Difficulty] }

AutoAttackWaves = { Auto2, Auto3, Auto4, Auto1, Auto5, Auto6, Auto7, Auto8 }

StationaryGuardUnits = { Actor237, Actor238, Actor231, ltnk1, ltnk2, Actor233, ltnk3, arty1, arty2, arty3, bggy, Actor240, Actor242, Actor243, Actor227, Actor228, Actor229 }

StartStationaryGuards = function(StationaryGuards)
	Utils.Do(StationaryGuards, function(unit)
		if not unit.IsDead then
			unit.Patrol( { unit.Location } , true, 20)
		end
	end)
end

SendWaves = function(counter, Waves)
	if counter <= #Waves then
		local team = Waves[counter]

		for type, amount in pairs(team.units) do
			MoveAndHunt(Utils.Take(amount, Nod.GetActorsByType(type)), team.waypoints)
		end

		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

SendHeli = function(heli)
	local units = Reinforcements.ReinforceWithTransport(Nod, "tran", heli.types, heli.entry, { heli.entry[1] })
	Utils.Do(units[2], function(actor)
		actor.Hunt()
		Trigger.OnIdle(actor, actor.Hunt)
	end)
	if heli.delay == DateTime.Seconds(0) then
		return
	end
	Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
end

MoveInitialArty = function(arty, waypoints)
	local units = { arty }
	MoveAndIdle(units, waypoints)
end

TankTerror = function(tank)
	local units = { tank }
	MoveAndHunt(units, WaypointGroupVillageLeft)
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Camera.Position = DefaultCameraPosition.CenterPosition

	StartStationaryGuards(StationaryGuardUnits)

	StartAI()

	InitObjectives(GDI)

	ProtectMoebius = AddPrimaryObjective(GDI, "protect-mobius")
	Trigger.OnKilled(DrMoebius, function()
		GDI.MarkFailedObjective(ProtectMoebius)
	end)

	ProtectHospital = AddPrimaryObjective(GDI, "protect-hospital")
	Trigger.OnKilled(Hospital, function()
		GDI.MarkFailedObjective(ProtectHospital)
	end)

	CiviliansKilledThreshold = CiviliansKilledThreshold[Difficulty]
	local civilians = 14 - CiviliansKilledThreshold
	local keepCiviliansAlive = UserInterface.Translate("keep-civilians-alive", { ["civilians"] = civilians })
	ProtectCivilians = AddPrimaryObjective(GDI, keepCiviliansAlive)
	Utils.Do(Civilians, function(civilian)
		Trigger.OnKilled(civilian, function()
			CivilianCasualties = CivilianCasualties + 1
			if CiviliansKilledThreshold < CivilianCasualties then
				GDI.MarkFailedObjective(ProtectCivilians)
			end
		end)
	end)

	SecureArea = AddPrimaryObjective(GDI, "destroy-nod-bases")

	KillGDI = AddPrimaryObjective(Nod, "")

	AirSupport = AddSecondaryObjective(GDI, "destroy-sams")
	Trigger.OnAllKilled(SamSites, function()
		GDI.MarkCompletedObjective(AirSupport)
		Actor.Create("airstrike.proxy", true, { Owner = GDI })
	end)

	Actor.Create("flare", true, { Owner = GDI, Location = DefaultFlareLocation.Location })

	Trigger.AfterDelay(DateTime.Minutes(1), function() SendWaves(1, AutoAttackWaves) end)
	Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(handofnod) end)
	Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(nodairfield) end)

	local InitialArrivingUnits =
	{
		{ units = { Actor252, Actor253, Actor223, Actor225, Actor222, Actor258, Actor259, Actor260, Actor261, Actor254, Actor255, Actor256, Actor257 }, distance = -1 },
		{ units = { Actor218, Actor220, Actor224, Actor226 }, distance = -2 },
		{ units = { gdiAPC1 }, distance = -3 }
	}

	Utils.Do(InitialArrivingUnits, function(group)
		Utils.Do(group.units, function(unit)
			unit.Move(unit.Location + CVec.New(0, group.distance), 0)
		end)
	end)

	Utils.Do(NodHelis, function(heli)
		if heli.delay == DateTime.Seconds(0) then -- heli1 comes only when specific units are killed, see below
			return
		end
		Trigger.AfterDelay(heli.delay, function() SendHeli(heli) end)
	end)

	-- units destroyed, send heli, eg. harv, tnk, bggy,...
	Utils.Do(NodRetaliateIfDestroyedUnits, function(unit)
		Trigger.OnKilled(unit, function()
			SendHeli(NodHelis[2])
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(Arty1Delay[Difficulty]), function() MoveInitialArty(earlyarty1, ArtyWaypoints1) end)
	Trigger.AfterDelay(DateTime.Seconds(Arty2Delay[Difficulty]), function() MoveInitialArty(earlyarty2, ArtyWaypoints2) end)
	Trigger.AfterDelay(DateTime.Seconds(TerrorTankDelay[Difficulty]), function() TankTerror(terrortank) end)
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) then
		if GDI.HasNoRequiredUnits()  then
			Nod.MarkCompletedObjective(KillGDI)
		end
		if Nod.HasNoRequiredUnits() then
			GDI.MarkCompletedObjective(SecureArea)
			GDI.MarkCompletedObjective(ProtectMoebius)
			GDI.MarkCompletedObjective(ProtectHospital)
			GDI.MarkCompletedObjective(ProtectCivilians)
		end
	end
end
