--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
StartAnts = { StartAnt1, StartAnt2 }
Hive1Killzone = { CPos.New(80,83), CPos.New(81,83), CPos.New(82,83), CPos.New(83,83), CPos.New(84,83), CPos.New(85,83), CPos.New(85,84), CPos.New(85,85), CPos.New(85,86), CPos.New(85,87), CPos.New(84,87), CPos.New(83,87), CPos.New(82,87), CPos.New(81,87), CPos.New(80,87), CPos.New(80,86), CPos.New(80,85), CPos.New(80,84) }
Hive2Killzone = { CPos.New(84,50), CPos.New(85,50), CPos.New(86,50), CPos.New(87,50), CPos.New(88,50), CPos.New(89,50), CPos.New(89,51), CPos.New(89,52), CPos.New(89,53), CPos.New(89,54), CPos.New(88,54), CPos.New(87,54), CPos.New(86,54), CPos.New(85,54), CPos.New(84,54), CPos.New(84,53), CPos.New(84,52), CPos.New(84,51) }
Hive3Killzone = { CPos.New(73,30), CPos.New(74,30), CPos.New(75,30), CPos.New(76,30), CPos.New(77,30), CPos.New(78,30), CPos.New(78,31), CPos.New(78,32), CPos.New(78,33), CPos.New(78,34), CPos.New(77,34), CPos.New(76,34), CPos.New(75,34), CPos.New(74,34), CPos.New(73,34), CPos.New(73,33), CPos.New(73,32), CPos.New(73,31) }
Hive4Killzone = { CPos.New(51,99), CPos.New(52,99), CPos.New(53,99), CPos.New(54,99), CPos.New(55,99), CPos.New(56,99), CPos.New(56,100), CPos.New(56,101), CPos.New(56,102), CPos.New(56,103), CPos.New(55,103), CPos.New(54,103), CPos.New(53,103), CPos.New(52,103), CPos.New(51,103), CPos.New(51,102), CPos.New(51,101), CPos.New(51,100) }
Hive5Killzone = { CPos.New(55,64), CPos.New(55,65), CPos.New(55,66), CPos.New(56,66), CPos.New(57,66), CPos.New(58,66), CPos.New(59,66), CPos.New(60,66), CPos.New(60,65), CPos.New(60,64), CPos.New(60,63) }
Hive6Killzone = { CPos.New(32,31), CPos.New(33,31), CPos.New(34,31), CPos.New(35,31), CPos.New(36,31), CPos.New(37,31), CPos.New(37,32), CPos.New(37,33), CPos.New(37,34), CPos.New(37,35), CPos.New(36,35), CPos.New(35,35), CPos.New(34,35), CPos.New(33,35), CPos.New(32,34), CPos.New(32,33), CPos.New(32,32) }
Hive7Killzone = { CPos.New(30,76), CPos.New(31,76), CPos.New(32,76), CPos.New(33,76), CPos.New(34,76), CPos.New(35,76), CPos.New(35,77), CPos.New(35,78), CPos.New(35,79), CPos.New(35,80), CPos.New(34,80), CPos.New(33,80), CPos.New(32,80), CPos.New(31,80), CPos.New(30,80), CPos.New(30,79), CPos.New(30,78), CPos.New(30,77) }
Hives = { Hive1, Hive2, Hive3, Hive4, Hive5, Hive6, Hive7 }
HiveFlares = { Hive1Flare, Hive2Flare, Hive3Flare, Hive4Flare, Hive5Flare, Hive6Flare, Hive7Flare }
HiveKillzones = { Hive1Killzone, Hive2Killzone, Hive3Killzone, Hive4Killzone, Hive5Killzone, Hive6Killzone, Hive7Killzone }
HiveGassed = { Hive1Gassed, Hive2Gassed, Hive3Gassed, Hive4Gassed, Hive5Gassed, Hive6Gassed, Hive7Gassed }

Start = function()
	Utils.Do(BadGuy.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Utils.Do(StartAnts, function(ants)
		IdleHunt(ants)
	end)

	Trigger.OnAllKilled(StartAnts, function()
		Media.PlaySpeechNotification(Spain, "ReinforcementsArrived")
		Reinforcements.Reinforce(Spain, { "apc.grens", "apc.rockets" }, { SpainEntry.Location, APCStop.Location })
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(Spain, "ReinforcementsArrived")
		VamonosPest = Reinforcements.Reinforce(Spain, { "chan", "chan", "chan", "chan", "chan", "chan", "chan" }, { SpainEntry.Location, DefaultCameraPosition.Location }, 3)
		Trigger.OnAllKilled(VamonosPest, function()
			if not Spain.IsObjectiveCompleted(GasNests) then
				Spain.MarkFailedObjective(GasNests)
			end
		end)
	end)
end

GasAntNests = function()
	for nestId = 1, 7 do
		Trigger.OnEnteredProximityTrigger(Hives[nestId].CenterPosition, WDist.FromCells(1), function(actor, id)
			if actor.Type == "chan" then
				Trigger.RemoveProximityTrigger(id)
				HiveGassed[nestId] = true
				Actor.Create("flare", true, { Owner = England, Location = Hives[nestId].Location })
				Actor.Create("flare", true, { Owner = England, Location = HiveFlares[nestId].Location })
				Trigger.OnEnteredFootprint(HiveKillzones[nestId], function(ant)
					if ant.Type == "warriorant" then
						ant.Kill("ExplosionDeath")
					end
				end)
			end
		end)
	end
end

SendAnts = function()
	if not Spain.IsObjectiveCompleted(KillAll) and not AntsSent then
		AntsSent = true
		Utils.Do(BadGuy.GetGroundAttackers(), function(ant)
			IdleHunt(ant)
		end)
	end
end

Tick = function()
	if Spain.HasNoRequiredUnits() then
		BadGuy.MarkCompletedObjective(EatSpain)
	end

	if BadGuy.HasNoRequiredUnits() then
		Spain.MarkCompletedObjective(KillAll)
	end

	if HiveGassed[1] and HiveGassed[2] and HiveGassed[3] and HiveGassed[4] and HiveGassed[5] and HiveGassed[6] and HiveGassed[7] then
		Spain.MarkCompletedObjective(GasNests)
		SendAnts()
	end
end

WorldLoaded = function()
	Spain = Player.GetPlayer("Spain")
	BadGuy = Player.GetPlayer("BadGuy")
	USSR = Player.GetPlayer("USSR")
	England = Player.GetPlayer("England")

	InitObjectives(Spain)

	EatSpain = BadGuy.AddObjective("For the Swarm!")
	GasNests = Spain.AddObjective("Gas every ant nest.")
	KillAll = Spain.AddObjective("Kill every ant lurking above ground.")

	Camera.Position = DefaultCameraPosition.CenterPosition
	Start()
	GasAntNests()
	ActivateAntHives()
end
