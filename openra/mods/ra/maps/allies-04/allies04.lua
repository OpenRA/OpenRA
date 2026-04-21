--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

ConvoyUnits =
{
	{ "ftrk", "ftrk", "truk", "truk", "apc", "ftrk" },
	{ "ftrk", "3tnk", "truk", "truk", "apc" },
	{ "3tnk", "3tnk", "truk", "truk", "ftrk" }
}

ConvoyRallyPoints =
{
	{ SovietEntry1.Location, SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally6.Location },
	{ SovietEntry2.Location, SovietRally10.Location, SovietRally11.Location }
}

ConvoyDelays =
{
	easy = { DateTime.Minutes(4), DateTime.Minutes(5) + DateTime.Seconds(20) },
	normal = { DateTime.Minutes(2) + DateTime.Seconds(30), DateTime.Minutes(4) },
	hard = { DateTime.Minutes(1) + DateTime.Seconds(30), DateTime.Minutes(2) + DateTime.Seconds(30) },
	tough = { DateTime.Minutes(1), DateTime.Minutes(1) + DateTime.Seconds(15) }
}

Convoys =
{
	easy = 2,
	normal = 3,
	hard = 5,
	tough = 10
}

ParadropDelays =
{
	easy = { DateTime.Seconds(40), DateTime.Seconds(90) },
	normal = { DateTime.Seconds(30), DateTime.Seconds(70) },
	hard = { DateTime.Seconds(20), DateTime.Seconds(50) },
	tough = { DateTime.Seconds(10), DateTime.Seconds(25) }
}

ParadropWaves =
{
	easy = 4,
	normal = 6,
	hard = 10,
	tough = 25
}

ParadropLZs = { ParadropPoint1.CenterPosition, ParadropPoint2.CenterPosition, ParadropPoint3.CenterPosition }

Paradropped = 0
Paradrop = function()
	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelay[1], ParadropDelay[2]), function()
		local aircraft = PowerProxy.TargetParatroopers(Utils.Random(ParadropLZs))
		Utils.Do(aircraft, function(a)
			Trigger.OnPassengerExited(a, function(_, p)
				IdleHunt(p)
			end)
		end)

		Paradropped = Paradropped + 1
		if Paradropped <= ParadropWaves[Difficulty] then
			Paradrop()
		end
	end)
end

ConvoysSent = 0
SendConvoys = function()
	Trigger.AfterDelay(Utils.RandomInteger(ConvoyDelay[1], ConvoyDelay[2]), function()
		local path = Utils.Random(ConvoyRallyPoints)
		local units = Reinforcements.Reinforce(USSR, Utils.Random(ConvoyUnits), { path[1] })
		local lastWaypoint = path[#path]

		Utils.Do(units, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				if unit.Type == "truk" then
					Utils.Do(path, function(waypoint)
						unit.Move(waypoint)
					end)

					Trigger.OnIdle(unit, function()
						unit.Move(lastWaypoint)
					end)
				else
					unit.Patrol(path)
					Trigger.OnIdle(unit, function()
						unit.AttackMove(lastWaypoint)
					end)
				end
			end)
		end)

		local id = Trigger.OnEnteredFootprint({ lastWaypoint }, function(a, id)
			if a.Owner == USSR and Utils.Any(units, function(unit) return unit == a end) then

				-- We are at our destination and thus don't care about other queued actions anymore
				a.Stop()
				a.Destroy()

				if a.Type == "truk" then
					Greece.MarkFailedObjective(DestroyConvoys)
				end
			end
		end)

		Trigger.OnAllRemovedFromWorld(units, function()
			Trigger.RemoveFootprintTrigger(id)

			ConvoysSent = ConvoysSent + 1
			if ConvoysSent <= Convoys[Difficulty] then
				SendConvoys()
			else
				Greece.MarkCompletedObjective(DestroyConvoys)
			end
		end)

		Media.PlaySpeechNotification(Greece, "ConvoyApproaching")
	end)
end

Tick = function()
	if Greece.HasNoRequiredUnits() then
		Greece.MarkFailedObjective(KillUSSR)
	end

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(KillUSSR)

		-- We don't care about future convoys anymore
		Greece.MarkCompletedObjective(DestroyConvoys)
	end
end

AddObjectives = function()
	KillUSSR = AddPrimaryObjective(Greece, "destroy-soviet-units-buildings")
	DestroyConvoys = AddSecondaryObjective(Greece, "destroy-convoys")
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	Camera.Position = AlliedConyard.CenterPosition

	InitObjectives(Greece)
	AddObjectives()

	ConvoyDelay = ConvoyDelays[Difficulty]
	ParadropDelay = ParadropDelays[Difficulty]
	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = USSR })
	Paradrop()
	SendConvoys()

	Trigger.AfterDelay(0, ActivateAI)
end
