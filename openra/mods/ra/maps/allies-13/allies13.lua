--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Engineers = { Engi1, Engi2, Engi3, Engi4 }
HeavyTankCamFootprint = { CPos.New(11, 46), CPos.New(12, 47), CPos.New(12, 48), CPos.New(12, 49) }
TankRifles = { TankRifle1, TankRifle2, TankRifle3 }
MRJCamFootprint = { CPos.New(16, 53), CPos.New(16, 54), CPos.New(16, 55) }
DoomRoomFootprint = { CPos.New(78,70), CPos.New(79,70), CPos.New(80,70) }
V2RoomFootprint = { CPos.New(64,71), CPos.New(65,71), CPos.New(66,71) }
CoreRoomFootprint = { CPos.New(55,60), CPos.New(55,61), CPos.New(55,62), CPos.New(65,52), CPos.New(66,52), CPos.New(67,52), CPos.New(75,58), CPos.New(75,59), CPos.New(75,60) }
CoreRoomPath = { CorePathA.Location, CorePathB.Location, CorePathC.Location, CorePathD.Location }
CoreRoomPatrol = { CorePatrol1, CorePatrol2, CorePatrol3 }
DogPatrolAPath = { PatrolPath1.Location, MRJCamera.Location, PatrolPath2.Location, PatrolPath3.Location }
DogPatrolA = { DogPatrolA1, DogPatrolA2, DogPatrolA3 }
DogPatrolBPath = { PatrolPath4.Location, DoomRoomCam.Location, PatrolPath5.Location, DoomRoomCam.Location, PatrolPath4.Location, CorePathA.Location }
DogPatrolB = { DogPatrolB1, DogPatrolB2, DogPatrolB3 }
RiflePatrolAPath = { PatrolPath6.Location, PatrolPath7.Location, PatrolPath2.Location, MRJCamera.Location, PatrolPath1.Location, PatrolPath3.Location, PatrolPath7.Location, CorePathC.Location }
RiflePatrolA = { RiflePatrolA1, RiflePatrolA2 }
RiflePatrolBPath = { PatrolPath8.Location, Generator4.Location, PatrolPath8.Location, CorePathB.Location }
RiflePatrolB = { RiflePatrolB1, RiflePatrolB2 }
RiflePatrolCPath = { PatrolPath9.Location, PatrolPath10.Location, CorePathD.Location, PatrolPath11.Location }
RiflePatrolC = { RiflePatrolC1, RiflePatrolC2 }
Generators = { Generator1, Generator2, Generator3, Generator4, Generator5, Generator6, Generator7, Generator8 }
ChargePlaced = { Charge1Placed, Charge2Placed, Charge3Placed, Charge4Placed, Charge5Placed, Charge6Placed, Charge7Placed, Charge8Placed }
FlameConsoles = { FlameConsole1, FlameConsole2, FlameConsole3, FlameConsole4, FlameConsole5, FlameConsole6, FlameConsole7, FlameConsole8 }
FlameTowers = { FlameTower1, FlameTower2, FlameTower3, FlameTower4, FlameTower5, FlameTower6, FlameTower7, FlameTower8 }
DualTowerActors = { DualTower1, DualTower2, DoomBarrel }
DoomFootprint = { CPos.New(75,77), CPos.New(76,77), CPos.New(77,77), CPos.New(78,77), CPos.New(79,77), CPos.New(80,77), CPos.New(81,77) }
DoomPatrol = { DoomGren1, DoomGren2, DoomGren3, DoomGren4, DoomGren5, DoomRifle1, DoomRifle2, DoomRifle3, DoomRifle4, DoomRifle5, DoomFlamer1, DoomFlamer2, DoomFlamer3, DoomFlamer4, DoomFlamer5 }
V2s = { V21, V22, V23, V24, V25, V26, V27, V28 }

TimerLength =
{
	easy = DateTime.Minutes(0),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(10)
}

MissionTriggers = function()
	Trigger.OnAllKilled(Engineers, function()
		USSR.MarkCompletedObjective(StopAllies)
	end)

	Trigger.OnAllKilled(V2s, function()
		Reinforcements.Reinforce(Greece, { "e1", "e1", "e1", "medi" }, { ReinforcementsSouth.Location, SouthTeamStop.Location }, 0)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	end)

	local doomTriggered
	Trigger.OnEnteredFootprint(DoomFootprint, function(actor, id)
		if actor.Owner == Greece and not doomTriggered then
			Trigger.RemoveFootprintTrigger(id)
			doomTriggered = true

			Utils.Do(DoomPatrol, IdleHunt)
		end
	end)

	Trigger.OnEnteredProximityTrigger(MoneyCrates.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			Greece.MarkCompletedObjective(TakeMoney)
		end
	end)

	local flamers = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.Type == "ftur" end)
	Utils.Do(flamers, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Health < building.MaxHealth * 9/10 then
				building.StartBuildingRepairs()
			end
		end)
	end)

	Trigger.OnTimerExpired(function()
		DateTime.TimeLimit = 0
		Trigger.AfterDelay(1, function() UserInterface.SetMissionText("We're too late!", USSR.Color) end)
		USSR.MarkCompletedObjective(StopAllies)
	end)
end

PlaceCharges = function()
	for generatorID = 1, 8 do
		Trigger.OnEnteredProximityTrigger(Generators[generatorID].CenterPosition, WDist.FromCells(1), function(actor, id)
			if actor.Type == "e6" then
				Trigger.RemoveProximityTrigger(id)
				ChargePlaced[generatorID] = true
				Actor.Create("flare", true, { Owner = Greece, Location = Generators[generatorID].Location + CVec.New(0,-1) })
				Media.PlaySpeechNotification(Greece, "ExplosiveChargePlaced")
				Media.DisplayMessage(UserInterface.GetFluentMessage("explosive-charge-placed"), UserInterface.GetFluentMessage("engineer"))
			end
		end)
	end
end

FlameTowerTriggers = function()
	for flameID = 1, 8 do
		Trigger.OnEnteredProximityTrigger(FlameConsoles[flameID].CenterPosition, WDist.FromCells(1), function(actor, id)
			if actor.Type == "e6" then
				Trigger.RemoveProximityTrigger(id)
				if not FlameTowers[flameID].IsDead then
					Media.DisplayMessage(UserInterface.GetFluentMessage("flame-turret-deactivated"), UserInterface.GetFluentMessage("console"))
					FlameTowers[flameID].Kill()
					Media.PlaySoundNotification(Greece, "AngryBleep")
				end
			end
		end)
	end

	Trigger.OnEnteredProximityTrigger(TurncoatConsole.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Type == "e6" then
			Trigger.RemoveProximityTrigger(id)
			Actor.Create("ftur", true, { Owner = Turkey, Location = TurncoatFlameTurret.Location })
		end
	end)

	Trigger.OnEnteredProximityTrigger(TwoTowerConsole.CenterPosition, WDist.FromCells(1), function(actor, id)
		if actor.Type == "e6" then
			Trigger.RemoveProximityTrigger(id)

			Media.DisplayMessage(UserInterface.GetFluentMessage("flame-turret-deactivated"), UserInterface.GetFluentMessage("console"))
			Utils.Do(DualTowerActors, function(actor)
				if not actor.IsDead then
					actor.Kill()
				end
			end)
		end
	end)

	local towers = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == USSR and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(towers, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == USSR and building.Health < building.MaxHealth * 99/100 then
				building.StartBuildingRepairs()
			end
		end)
	end)
end

CameraTriggers = function()
	local heavyCamTriggered
	Trigger.OnEnteredFootprint(HeavyTankCamFootprint, function(actor, id)
		if actor.Owner == Greece and not heavyCamTriggered then
			Trigger.RemoveFootprintTrigger(id)
			heavyCamTriggered = true

			local heavyCam = Actor.Create("camera", true, { Owner = Greece, Location = HeavyTankCam.Location })
			Media.DisplayMessage(UserInterface.GetFluentMessage("old-flametowers"), UserInterface.GetFluentMessage("engineer"))
			Utils.Do(TankRifles, IdleHunt)
			Trigger.AfterDelay(DateTime.Minutes(1), function()
				heavyCam.Destroy()
			end)
		end
	end)

	local mrjCamTriggered
	Trigger.OnEnteredFootprint(MRJCamFootprint, function(actor, id)
		if actor.Owner == Greece and not mrjCamTriggered then
			Trigger.RemoveFootprintTrigger(id)
			mrjCamTriggered = true

			local mrjCam = Actor.Create("camera", true, { Owner = Greece, Location = MRJCamera.Location })
			--The original had the Mobile Radar Jammers attempt to escape. Excluding that for now as MRJs in OpenRA can take much more damage than the original.
			Trigger.AfterDelay(DateTime.Minutes(1), function()
				mrjCam.Destroy()
			end)
		end
	end)

	local v2CamTriggered
	Trigger.OnEnteredFootprint(V2RoomFootprint, function(actor, id)
		if actor.Owner == Greece and not v2CamTriggered then
			Trigger.RemoveFootprintTrigger(id)
			v2CamTriggered = true

			local v2Cam = Actor.Create("camera", true, { Owner = Greece, Location = TurncoatFlameTurret.Location })
			Trigger.AfterDelay(DateTime.Minutes(1), function()
				v2Cam.Destroy()
			end)
		end
	end)

	local coreCamTriggered
	Trigger.OnEnteredFootprint(CoreRoomFootprint, function(actor, id)
		if actor.Owner == Greece and not coreCamTriggered then
			Trigger.RemoveFootprintTrigger(id)
			coreCamTriggered = true

			Actor.Create("camera", true, { Owner = Greece, Location = GasSpawn.Location + CVec.New(1,0) })
		end
	end)

	local doomCamTriggered
	Trigger.OnEnteredFootprint(DoomRoomFootprint, function(actor, id)
		if actor.Owner == Greece and not doomCamTriggered then
			Trigger.RemoveFootprintTrigger(id)
			doomCamTriggered = true

			local doomCam = Actor.Create("camera", true, { Owner = Greece, Location = DoomRoomCam.Location })
			Media.PlaySoundNotification(Greece, "AlertBleep")
			Media.DisplayMessage(UserInterface.GetFluentMessage("be-sneaky"), UserInterface.GetFluentMessage("soldier"))
			Trigger.AfterDelay(DateTime.Minutes(1), function()
				doomCam.Destroy()
			end)
		end
	end)
end

SendRifles = function()
	RiflesSent = true
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Reinforcements.Reinforce(Greece, { "e1", "e1", "e1" }, { ReinforcementsWest.Location, MRJCamera.Location }, 0)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	end)
end

GroupPatrol = function(units, waypoints, delay)
	local i = 1
	local stop = false

	Utils.Do(units, function(unit)
		Trigger.OnIdle(unit, function()
			if stop then
				return
			end

			if unit.Location == waypoints[i] then
				local bool = Utils.All(units, function(actor) return actor.IsIdle end)

				if bool then
					stop = true

					i = i + 1
					if i > #waypoints then
						i = 1
					end

					Trigger.AfterDelay(delay, function() stop = false end)
				end
			else
				unit.AttackMove(waypoints[i])
			end
		end)
	end)
end

Tick = function()
	USSR.Cash = 5000

	if ChargePlaced[1] and ChargePlaced[2] and ChargePlaced[3] and ChargePlaced[4] and ChargePlaced[5] and ChargePlaced[6] and ChargePlaced[7] and ChargePlaced[8] then
		Greece.MarkCompletedObjective(PlaceExplosives)
		if not Greece.IsObjectiveCompleted(TakeMoney) then
			Greece.MarkFailedObjective(TakeMoney)
		end
	end

	if ChargePlaced[1] and ChargePlaced[2] and not RiflesSent then
		SendRifles()
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Turkey = Player.GetPlayer("Turkey")
	England = Player.GetPlayer("England")

	InitObjectives(Greece)

	PlaceExplosives = AddPrimaryObjective(Greece, "place-explosive-charges")
	TakeMoney = AddSecondaryObjective(Greece, "steal-supplies")
	StopAllies = AddPrimaryObjective(USSR, "")

	Trigger.AfterDelay(DateTime.Minutes(7), function()
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location + CVec.New(1,0) })
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location + CVec.New(0,-1) })
	end)
	Trigger.AfterDelay(DateTime.Minutes(17), function()
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location + CVec.New(2,0) })
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location + CVec.New(2,-1) })
	end)
	Trigger.AfterDelay(DateTime.Minutes(22), function()
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location + CVec.New(1,-1) })
		Actor.Create("flare", true, { Owner = England, Location = GasSpawn.Location})
	end)

	Camera.Position = DefaultCameraPosition.CenterPosition
	MissionTriggers()
	PlaceCharges()
	FlameTowerTriggers()
	CameraTriggers()
	GroupPatrol(CoreRoomPatrol, CoreRoomPath, DateTime.Seconds(5))
	GroupPatrol(DogPatrolA, DogPatrolAPath, DateTime.Seconds(5))
	GroupPatrol(DogPatrolB, DogPatrolBPath, DateTime.Seconds(5))
	GroupPatrol(RiflePatrolA, RiflePatrolAPath, DateTime.Seconds(8))
	GroupPatrol(RiflePatrolB, RiflePatrolBPath, DateTime.Seconds(8))
	GroupPatrol(RiflePatrolC, RiflePatrolCPath, DateTime.Seconds(8))

	DateTime.TimeLimit = (DateTime.Minutes(32) - TimerLength[Difficulty])
end
