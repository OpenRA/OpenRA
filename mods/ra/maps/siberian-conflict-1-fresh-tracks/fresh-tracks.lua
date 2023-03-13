--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
TimerTicks = DateTime.Minutes(8)
Squad1 = { FirstSquad1, FirstSquad2, FirstSquad3 }
Squad2 = { SecondSquad1, SecondSquad2, SecondSquad3 }
PatrolMammothPath = { Patrol1.Location, Patrol2.Location, Patrol3.Location, Patrol4.Location, Patrol5.Location, Patrol6.Location, Patrol7.Location }
ConvoyEscape = { CPos.New(113, 42), CPos.New(117, 71) }

ConvoyUnits =
{
	easy = { { "e2", "e2", "e2", "truk", "truk", "truk" }, { "3tnk", "3tnk", "truk", "truk", "truk" } },
	normal = { { "3tnk", "3tnk", "3tnk", "truk", "truk", "truk" }, { "3tnk", "v2rl", "e2", "e1", "e1", "truk", "truk", "truk" } },
	hard = { { "3tnk", "3tnk", "3tnk", "3tnk", "truk", "truk", "truk" }, { "3tnk", "v2rl", "v2rl", "e1", "e1", "e4", "e4", "truk", "truk", "truk", "4tnk" }, { "ttnk", "ttnk", "ttnk", "shok", "shok", "shok", "shok", "truk", "truk", "truk" } }
}

BaseConvoyUnits = { "3tnk", "ttnk", "ttnk", "ttnk", "truk", "truk", "truk"  }
FinalConvoyUnits = { "3tnk", "3tnk", "3tnk", "3tnk", "truk", "truk", "truk", "4tnk" }

AttackPaths =
{
	{ ExitNorth.Location },
	{ ExitEast.Location },
	{ AttackEntry1.Location },
	{ AttackEntry2.Location }
}

AttackWaveUnits = { { "3tnk", "3tnk" }, { "e1", "e1", "e1", "e2", "e2", "e4", "e4" }, { "shok", "shok", "shok", "shok", "shok" }, { "ttnk", "ttnk" }, { "4tnk", "e1", "e1", "e2" }, { "v2rl", "v2rl", "ftrk", "ftrk", "ftrk" } }

ConvoyPaths =
{
	{ NorthwestEntry.Location, NorthwestPath1.Location, NorthwestBridge.Location, Patrol7.Location, Patrol6.Location, NortheastBridge.Location, NorthwestPath3.Location, ExitNorth.Location },
	{ CenterEntry.Location, CenterPath1.Location, CenterPath2.Location, Patrol1.Location, Patrol7.Location, Patrol6.Location, NortheastBridge.Location, NorthwestPath3.Location, ExitNorth.Location },
	{ CenterEntry.Location, CenterPath1.Location, CenterPath2.Location, Patrol1.Location, Patrol2.Location, SouthPath2.Location, SoutheastBridge.Location, SouthPath3.Location, ExitEast.Location },
	{ SouthEntry.Location, SouthPath1.Location, Patrol2.Location, SouthPath2.Location, SoutheastBridge.Location, SouthPath3.Location, ExitEast.Location }
}

BaseConvoyPath = { BaseEntry.Location, BasePath1.Location, CenterPath2.Location, Patrol1.Location, Patrol7.Location, Patrol6.Location, NortheastBridge.Location, NorthwestPath3.Location, ExitNorth.Location }
FinalConvoyPath = { NorthEntry.Location, Patrol7.Location, Patrol6.Location, Patrol5.Location, FinalConvoy1.Location, ExitEast.Location }

OpeningMoves = function()
	Utils.Do(Squad1, function(actor)
		actor.AttackMove(AlliedBase.Location)
	end)

	Utils.Do(Squad2, function(actor)
		actor.AttackMove(AlliedBase.Location)
	end)

	PatrolMammoth.Patrol(PatrolMammothPath, true, 10)
end

AttackWaveDelays =
{
	easy = { DateTime.Seconds(40), DateTime.Seconds(80) },
	normal = { DateTime.Seconds(30), DateTime.Seconds(60) },
	hard = { DateTime.Seconds(20), DateTime.Seconds(40) }
}

AttackWaves = function()
	local attackpath = Utils.Random(AttackPaths)
	local attackers = Reinforcements.Reinforce(USSR, Utils.Random(AttackWaveUnits), { attackpath[1] })
	Utils.Do(attackers, function(unit)
		Trigger.OnAddedToWorld(unit, function()
			unit.AttackMove(AlliedBase.Location)
			IdleHunt(unit)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(AttackWaveDelay[1], AttackWaveDelay[2]), AttackWaves)
end

ConvoyWaves =
{
	easy = 2,
	normal = 3,
	hard = 4
}
ConvoysPassed = 0

SendConvoys = function()
	ConvoysSent = true
	Media.PlaySpeechNotification(Allies, "ConvoyApproaching")
	local path = Utils.Random(ConvoyPaths)
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

	ConvoysPassed = ConvoysPassed + 1
	if ConvoysPassed <= ConvoyWaves[Difficulty] then
		Trigger.AfterDelay(DateTime.Seconds(90), SendConvoys)
	else
		FinalTrucks()
	end
end

FinalTrucks = function()
	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(Allies, "ConvoyApproaching")
		local basePath = BaseConvoyPath
		local baseWaypoint = basePath[#basePath]
		local baseConvoy = Reinforcements.Reinforce(USSR, BaseConvoyUnits, { basePath[1] })
		Utils.Do(baseConvoy, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				Utils.Do(basePath, function(waypoint)
					unit.Move(waypoint)
				end)

				Trigger.OnIdle(unit, function()
					unit.Move(baseWaypoint)
				end)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(2), function()
		Media.PlaySpeechNotification(Allies, "ConvoyApproaching")
		local finalPath = FinalConvoyPath
		local finalWaypoint = finalPath[#finalPath]
		local finalConvoy = Reinforcements.Reinforce(USSR, FinalConvoyUnits, { finalPath[1] })
		Utils.Do(finalConvoy, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				Utils.Do(finalPath, function(waypoint)
					unit.Move(waypoint)
				end)

				Trigger.OnIdle(unit, function()
					unit.Move(finalWaypoint)
				end)
			end)
		end)

		Trigger.OnAllKilled(Utils.Where(finalConvoy, function(a) return a.Type == "truk" end), function()
			Allies.MarkCompletedObjective(StopTrucks)
		end)
	end)
end

ConvoyExit = function()
	Trigger.OnEnteredFootprint(ConvoyEscape, function(actor, id)
		if actor.Owner == USSR and actor.Type == "truk" then
			actor.Stop()
			actor.Destroy()
			Allies.MarkFailedObjective(StopTrucks)
		elseif actor.Owner == USSR and actor.Type ~= "truk" then
			actor.Stop()
			actor.Destroy()
		end
	end)
end

BridgeTriggers = function()
	Trigger.OnKilled(BridgeBarrel1, function()
		local theNorthEastBridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge2" end)[1]
		if not theNorthEastBridge.IsDead then
			theNorthEastBridge.Kill()
		end
	end)

	Trigger.OnKilled(BridgeBarrel2, function()
		local theNorthwestBridge = Map.ActorsInBox(NorthwestPath1.CenterPosition, NorthEntry.CenterPosition, function(self) return self.Type == "bridge1" end)[1]
		if not theNorthwestBridge.IsDead then
			theNorthwestBridge.Kill()
		end
	end)

	Trigger.OnKilled(BridgeBarrel3, function()
		local theSoutheastBridge = Map.ActorsInBox(SoutheastBridge.CenterPosition, SouthPath3.CenterPosition, function(self) return self.Type == "bridge1" end)[1]
		if not theSoutheastBridge.IsDead then
			theSoutheastBridge.Kill()
		end
	end)

	AllThreeBridges = Utils.Where(Map.ActorsInWorld, function(actor)
		return
			actor.Type == "bridge1" or
			actor.Type == "bridge2"
	end)

	Trigger.OnAllKilled(AllThreeBridges, function()
		Allies.MarkCompletedObjective(DestroyBridges)
	end)
end

FirstTrucksEntering = UserInterface.Translate("first-trucks-entering")
FinishTimer = function()
	for i = 0, 5 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(FirstTrucksEntering, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

Ticked = TimerTicks
ConvoysSent = false
Tick = function()
	if Ticked > 0 then
		if (Ticked % DateTime.Seconds(1)) == 0 then
			Timer = UserInterface.Translate("first-trucks-arrive-in", { ["time"] = Utils.FormatTime(Ticked) })
			UserInterface.SetMissionText(Timer, TimerColor)
		end
		Ticked = Ticked - 1
	elseif Ticked == 0 and not ConvoysSent then
		SendConvoys()
		FinishTimer()
	end

	if Allies.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(SovietObj)
	end
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	USSR = Player.GetPlayer("USSR")

	SovietObj = AddPrimaryObjective(USSR, "")
	StopTrucks = AddPrimaryObjective(Allies, "destroy-soviet-convoy")
	DestroyBridges = AddSecondaryObjective(Allies, "destroy-bridges-slow-convoy")

	InitObjectives(Allies)

	Trigger.AfterDelay(DateTime.Minutes(3), function()
		Media.PlaySpeechNotification(Allies, "WarningFiveMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(5), function()
		Media.PlaySpeechNotification(Allies, "WarningThreeMinutesRemaining")
	end)
	Trigger.AfterDelay(DateTime.Minutes(7), function()
		Media.PlaySpeechNotification(Allies, "WarningOneMinuteRemaining")
	end)

	Camera.Position = AlliedBase.CenterPosition
	TimerColor = Allies.Color
	OpeningMoves()
	Trigger.AfterDelay(DateTime.Seconds(30), AttackWaves)
	ConvoyExit()
	BridgeTriggers()

	ConvoyUnits = ConvoyUnits[Difficulty]
	AttackWaveDelay = AttackWaveDelays[Difficulty]
end
