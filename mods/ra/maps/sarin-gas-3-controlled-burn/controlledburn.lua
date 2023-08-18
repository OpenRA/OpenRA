--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
StartUnits = { APC, StartSpy, Rifle1, Rifle2, Rifle3, Rifle4, Rocket1, Rocket2, Rocket3, Rocket4, Rocket5 }
SarinPlants = { SarinLab1, SarinLab2, SarinLab3, SarinLab4, SarinLab5 }
MammothStart = { CPos.New(37, 46), CPos.New(37, 47), CPos.New(37, 48), CPos.New(37, 49), CPos.New(37,50) }
NorthPatrol = { NorthPatrol1.Location, NorthPatrol2.Location, NorthPatrol3.Location, NorthPatrol4.Location, NorthPatrol5.Location }
BarrerlInvestigators = { Alert1, Alert2, Alert3, Alert4, Alert5 }
RaxTeam = { "e1", "e2", "e2", "e4", "e4", "shok" }
SouthPatrol = { SouthPatrol1.Location, SouthPatrol2.Location, SouthPatrol3.Location }
MCVReinforcements =
{
	easy = { "1tnk", "1tnk", "2tnk", "2tnk", "2tnk", "2tnk", "arty", "mcv" },
	normal = { "1tnk", "1tnk", "2tnk", "2tnk", "mcv" },
	hard = { "1tnk", "1tnk", "mcv" }
}

SetupTriggers = function()
	Trigger.OnEnteredFootprint(MammothStart, function(actor, mammothcam)
		if actor.Owner == Greece then
			Trigger.RemoveFootprintTrigger(mammothcam)
			NorthMammoth.Patrol(NorthPatrol, true, 20)
			local mammothCamera = Actor.Create("camera", true, { Owner = Greece, Location = NorthPatrol1.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				mammothCamera.Destroy()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(NorthPatrol3.CenterPosition, WDist.FromCells(8), function(actor, trigger1)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(trigger1)
			local baseCamera = Actor.Create("camera", true, { Owner = Greece, Location = BaseCam.Location })
			if Difficulty == "hard" then
				Reinforcements.Reinforce(BadGuy, RaxTeam, { BadGuyRaxSpawn.Location, BaseCam.Location }, 0)
			end
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				baseCamera.Destroy()
			end)
		end
	end)

	Trigger.OnAllRemovedFromWorld(StartUnits, function()
		if not MCVArrived then
			USSR.MarkCompletedObjective(SovietObj)
		end
	end)

	Trigger.OnKilled(VeryImportantBarrel, function()
		Utils.Do(BarrerlInvestigators, function(actor)
			if not actor.IsDead then
				actor.AttackMove(AlertGo.Location)
			end
		end)
	end)

	Trigger.OnAnyKilled(SarinPlants, function()
		Greece.MarkFailedObjective(CaptureSarin)
	end)

	Trigger.OnAllKilledOrCaptured(SarinPlants, function()
		Greece.MarkCompletedObjective(CaptureSarin)
	end)

	Trigger.OnEnteredProximityTrigger(AlliesMove.CenterPosition, WDist.FromCells(3), function(actor, id)
		if actor.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			Media.PlaySpeechNotification(Greece, "SignalFlareSouth")
		end
	end)
end

MCVArrived = false
MCVArrivedTick = false
PowerDownTeslas = function()
	if not MCVArrived then
		CaptureSarin = AddPrimaryObjective(Greece, "capture-sarin-plants-intact")
		KillBase = AddPrimaryObjective(Greece, "destroy-enemy-compound")
		Greece.MarkCompletedObjective(TakeOutPower)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.Reinforce(Greece, MCVReinforcements[Difficulty], { AlliesSpawn.Location, AlliesMove.Location })
		local baseFlare = Actor.Create("flare", true, { Owner = Greece, Location = AlliedBase.Location })
		Actor.Create("proc", true, { Owner = USSR, Location = Proc1.Location })
		Actor.Create("proc", true, { Owner = USSR, Location = Proc2.Location })
		SouthMammoth.Patrol(SouthPatrol, true, 20)
		MCVArrived = true

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			MCVArrivedTick = true
			PrepareFinishingHunt(USSR)
		end)

		Trigger.AfterDelay(DateTime.Seconds(60), function()
			local attackers = Reinforcements.Reinforce(USSR, { "e1", "e1", "e1", "e2", "e4" }, { SovietGroundEntry3.Location }, 5)
			Utils.Do(attackers, IdleHunt)
		end)

		Trigger.AfterDelay(DateTime.Seconds(100), function()
			baseFlare.Destroy()
			ActivateAI()
		end)
	end
end

PrepareFinishingHunt = function(player)
	local buildings = GetBaseBuildings(player)

	Trigger.OnAllKilledOrCaptured(buildings, function()
		Utils.Do(player.GetGroundAttackers(), function(actor)
			actor.Stop()
			IdleHunt(actor)
		end)
	end)
end

GetBaseBuildings = function(player)
	-- Excludes the unrepairable sarin plants, which is desired anyway.
	local buildings = Utils.Where(player.GetActors(), function(actor)
		return actor.HasProperty("StartBuildingRepairs")
	end)
	return buildings
end

Tick = function()
	USSR.Cash = 10000
	BadGuy.Cash = 10000

	if BadGuy.PowerState ~= "Normal" then
		PowerDownTeslas()
	end

	if Greece.HasNoRequiredUnits() and MCVArrivedTick then
		USSR.MarkCompletedObjective(SovietObj)
	end

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(KillBase)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")

	SovietObj = AddPrimaryObjective(USSR, "")
	TakeOutPower = AddPrimaryObjective(Greece, "cut-power-east")

	InitObjectives(Greece)

	StartSpy.DisguiseAsType("e1", BadGuy)
	Camera.Position = DefaultCameraPosition.CenterPosition
	SetupTriggers()
end
