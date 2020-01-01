--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

WaypointGroup1 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint10 }
WaypointGroup2 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint7, waypoint8, waypoint9, waypoint10 }
WaypointGroup3 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4, waypoint5 }
WaypointGroup4 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint4 }
WaypointGroup5 = { waypoint0, waypoint1, waypoint2, waypoint3, waypoint6, waypoint7, waypoint8, waypoint9, waypoint11 }

GDI1 = { units = { ['e1'] = 2, ['e2'] = 2 }, waypoints = WaypointGroup3, delay = 80 }
GDI2 = { units = { ['e2'] = 3, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 10 }
GDI3 = { units = { ['e1'] = 2, ['e3'] = 3 }, waypoints = WaypointGroup1, delay = 30 }
GDI4 = { units = { ['jeep'] = 2 }, waypoints = WaypointGroup3, delay = 45 }
GDI5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 10 }
Auto1 = { units = { ['e1'] = 2, ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup4, delay = 25 }
Auto2 = { units = { ['e2'] = 2, ['jeep'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto3 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 30 }
Auto4 = { units = { ['e1'] = 2, ['mtnk'] = 1 }, waypoints = WaypointGroup2, delay = 30 }
Auto5 = { units = { ['e3'] = 2, ['jeep'] = 1 }, waypoints = WaypointGroup1, delay = 30 }

AutoAttackWaves = { GDI1, GDI2, GDI3, GDI4, GDI5, Auto1, Auto2, Auto3, Auto4, Auto5 }

NodBase = { NodCYard, NodNuke, NodHand }
Outpost = { OutpostCYard, OutpostProc }

IntroReinforcements = { "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }

IntroGuards = { Actor89, Actor137, Actor123, Actor124, Actor135, Actor136 }
OutpostGuards = { Actor91, Actor108, Actor109, Actor110, Actor111, Actor112, Actor113, Actor122 }

NodBaseTrigger = { CPos.New(52, 52), CPos.New(52, 53), CPos.New(52, 54), CPos.New(52, 55), CPos.New(52, 56), CPos.New(52, 57), CPos.New(52, 58), CPos.New(52, 59), CPos.New(52, 60), CPos.New(55, 54) }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(20)

NodBaseCapture = function()
	FlareCamera = Actor.Create("camera", true, { Owner = Nod, Location = waypoint25.Location })
	Flare = Actor.Create("flare", true, { Owner = Nod, Location = waypoint25.Location })

	SendHelicopter()

	Nod.MarkCompletedObjective(LocateNodBase)

	Utils.Do(NodBase, function(actor)
		actor.Owner = Nod
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Nod, "NewOptions")
	end)
end

-- Provide the Nod with a helicopter until the outpost got captured
SendHelicopter = function()
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if not Nod.IsObjectiveCompleted(CaptureGDIOutpost) then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			TransportHelicopter = Reinforcements.ReinforceWithTransport(Nod, 'tran', nil, { ReinforcementsHelicopterSpawn.Location, waypoint15.Location })[1]
			Trigger.OnKilled(TransportHelicopter, SendHelicopter)
		end
	end)
end

SendGDIAirstrike = function(hq, delay)
	if not hq.IsDead and hq.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)

		if target then
			hq.SendAirstrike(target, false, Facing.NorthEast + 4)
			Trigger.AfterDelay(delay, function() SendGDIAirstrike(hq, delay) end)
		else
			Trigger.AfterDelay(delay/4, function() SendGDIAirstrike(hq, delay) end)
		end
	end
end

SendWaves = function(counter, Waves)
	if counter <= #Waves then
		local team = Waves[counter]

		for type, amount in pairs(team.units) do
			MoveAndHunt(Utils.Take(amount, GDI.GetActorsByType(type)), team.waypoints)
		end

		Trigger.AfterDelay(DateTime.Seconds(team.delay), function() SendWaves(counter + 1, Waves) end)
	end
end

Trigger.OnAllKilled(IntroGuards, function()
	if not Nod.IsObjectiveCompleted(LocateNodBase) then
		NodBaseCapture()
	end
end)

Trigger.OnAllKilledOrCaptured(Outpost, function()
	if not Nod.IsObjectiveCompleted(CaptureGDIOutpost) then
		Nod.MarkCompletedObjective(CaptureGDIOutpost)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			if not GDIHQ.IsDead and (not NodHand.IsDead or not NodNuke.IsDead) then
				local airstrikeproxy = Actor.Create("airstrike.proxy", false, { Owner = GDI })
				airstrikeproxy.SendAirstrike(AirstrikeTarget.CenterPosition, false, Facing.NorthEast + 4)
				airstrikeproxy.Destroy()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(15), function()
			Utils.Do(OutpostGuards, IdleHunt)
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			FlareCamera.Destroy()
			Flare.Destroy()
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function() SendWaves(1, AutoAttackWaves) end)
		Trigger.AfterDelay(AirstrikeDelay, function() SendGDIAirstrike(GDIHQ, AirstrikeDelay) end)
		Trigger.AfterDelay(DateTime.Minutes(2), function() ProduceInfantry(GDIPyle) end)
		Trigger.AfterDelay(DateTime.Minutes(3), function() ProduceVehicle(GDIWeap) end)
	end
end)

Trigger.OnCapture(OutpostCYard, function()
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(Nod, "NewOptions")
	end)
end)

Trigger.OnAnyKilled(Outpost, function()
	if not Nod.IsObjectiveCompleted(CaptureGDIOutpost) then
		Nod.MarkFailedObjective(CaptureGDIOutpost)
	end
end)

Trigger.OnEnteredFootprint(NodBaseTrigger, function(a, id)
	if not Nod.IsObjectiveCompleted(LocateNodBase) and a.Owner == Nod then
		NodBaseCapture()
		Trigger.RemoveFootprintTrigger(id)
	end
end)

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	Camera.Position = waypoint26.CenterPosition

	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.ReinforceWithTransport(Nod, "tran.in", IntroReinforcements, { ReinforcementsHelicopterSpawn.Location, ReinforcementsHelicopterRally.Location }, { ReinforcementsHelicopterSpawn.Location })

	StartAI()
	AutoGuard(IntroGuards)
	AutoGuard(OutpostGuards)

	InitObjectives(Nod)

	LocateNodBase = Nod.AddObjective("Locate the Nod base.")
	CaptureGDIOutpost = Nod.AddObjective("Capture the GDI outpost.")
	EliminateGDI = Nod.AddObjective("Eliminate all GDI forces in the area.")
	GDIObjective = GDI.AddObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(EliminateGDI)
	end
end
