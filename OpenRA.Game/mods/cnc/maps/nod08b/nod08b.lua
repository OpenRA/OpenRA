--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

WaypointGroup1 = { waypoint1, waypoint2, waypoint3, waypoint9, waypoint10 }
WaypointGroup2 = { waypoint5, waypoint6, waypoint7, waypoint8 }
WaypointGroup3 = { waypoint1, waypoint2, waypoint4, waypoint11 }

GDI1 = { units = { ['e2'] = 2, ['e3'] = 2 }, waypoints = WaypointGroup1, delay = 30 }
GDI2 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup2, delay = 40 }
GDI3 = { units = { ['e1'] = 3, ['e2'] = 3 }, waypoints = WaypointGroup3, delay = 40 }
GDI4 = { units = { ['jeep'] = 2 }, waypoints = WaypointGroup2, delay = 20 }
Auto1 = { units = { ['e1'] = 3, ['e2'] = 1 }, waypoints = WaypointGroup2, delay = 30 }
Auto2 = { units = { ['e1'] = 2, ['e2'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto3 = { units = { ['e1'] = 2, ['e2'] = 2 }, waypoints = WaypointGroup1, delay = 30 }
Auto4 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup1, delay = 30 }
Auto5 = { units = { ['mtnk'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto6 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup3, delay = 40 }
Auto7 = { units = { ['jeep'] = 1 }, waypoints = WaypointGroup1, delay = 50 }

AutoAttackWaves = { GDI1, GDI2, GDI3, GDI4, Auto1, Auto2, Auto3, Auto4, Auto5, Auto6, Auto7 }

NodBase = { NodCYard, NodNuke, NodHand }
Outpost = { OutpostCYard, OutpostProc }

IntroGuards = { Actor171, Actor172, Actor173, Actor145, Actor159, Actor160, Actor161 }
OutpostGuards = { Actor177, Actor178, Actor180, Actor187, Actor188, Actor185, Actor186, Actor184, Actor148, Actor179, Actor176, Actor183, Actor182 }
IntroReinforcements = { "e1", "e1", "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3" }

NodBaseTrigger = { CPos.New(52, 2), CPos.New(52, 3), CPos.New(52, 4), CPos.New(52, 5), CPos.New(52, 6), CPos.New(52, 7), CPos.New(52, 8) }

AirstrikeDelay = DateTime.Minutes(2) + DateTime.Seconds(30)

NodBaseCapture = function()
	Nod.MarkCompletedObjective(LocateNodBase)
	Utils.Do(NodBase, function(actor)
		actor.Owner = Nod
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Nod, "NewOptions")
	end)
end

-- Provide Nod with a helicopter until the outpost got captured
SendHelicopter = function()
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if not Nod.IsObjectiveCompleted(CaptureGDIOutpost) then
			Media.PlaySpeechNotification(Nod, "Reinforce")
			local heli = Reinforcements.ReinforceWithTransport(Nod, "tran", nil, { ReinforcementsHelicopterSpawn.Location, waypoint0.Location })[1]
			Trigger.OnKilled(heli, SendHelicopter)
		end
	end)
end

SendGDIAirstrike = function(hq, delay)
	if not hq.IsDead and hq.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)

		if target then
			hq.TargetAirstrike(target, Angle.NorthEast + Angle.New(16))
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
	FlareCamera1 = Actor.Create("camera", true, { Owner = Nod, Location = waypoint25.Location })
	FlareCamera2 = Actor.Create("camera", true, { Owner = Nod, Location = FlareExtraCamera.Location })
	Flare = Actor.Create("flare", true, { Owner = Nod, Location = waypoint25.Location })
	SendHelicopter()
	Nod.MarkCompletedObjective(LocateNodBase)
	NodBaseCapture()
end)

Trigger.OnAllKilledOrCaptured(Outpost, function()
	if not Nod.IsObjectiveCompleted(CaptureGDIOutpost) then
		Nod.MarkCompletedObjective(CaptureGDIOutpost)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			if not GDIHQ.IsDead and (not NodHand.IsDead or not NodNuke.IsDead) then
				local airstrikeproxy = Actor.Create("airstrike.proxy", false, { Owner = GDI })
				airstrikeproxy.TargetAirstrike(AirstrikeTarget.CenterPosition, Angle.NorthEast + Angle.New(16))
				airstrikeproxy.Destroy()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(15), function()
			Utils.Do(OutpostGuards, IdleHunt)
		end)

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			FlareCamera1.Destroy()
			FlareCamera2.Destroy()
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
	AutoGuard(GDI.GetGroundAttackers())

	InitObjectives(Nod)

	LocateNodBase = Nod.AddObjective("Locate the Nod base.")
	CaptureGDIOutpost = Nod.AddObjective("Capture the GDI outpost.")
	NodObjective3 = Nod.AddObjective("Eliminate all GDI forces in the area.")
	GDIObjective = GDI.AddObjective("Eliminate all Nod forces in the area.")
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end

	if DateTime.GameTime > 2 and GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective3)
	end
end
