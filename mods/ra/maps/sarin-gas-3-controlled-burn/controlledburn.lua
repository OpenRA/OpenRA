--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

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
			if Map.LobbyOption("difficulty") == "hard" then
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
end

MCVArrived = false
MCVArrivedTick = false
PowerDownTeslas = function()
	if not MCVArrived then
		CaptureSarin = Greece.AddObjective("Capture all Sarin processing plants intact.")
		KillBase = Greece.AddObjective("Destroy the enemy compound.")
		Greece.MarkCompletedObjective(TakeOutPower)
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.Reinforce(Greece, MCVReinforcements[Map.LobbyOption("difficulty")], { AlliesSpawn.Location, AlliesMove.Location })
		local baseFlare = Actor.Create("flare", true, { Owner = Greece, Location = AlliedBase.Location })
		Actor.Create("proc", true, { Owner = USSR, Location = Proc1.Location })
		Actor.Create("proc", true, { Owner = USSR, Location = Proc2.Location })
		SouthMammoth.Patrol(SouthPatrol, true, 20)
		MCVArrived = true

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			MCVArrivedTick = true
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

	SovietObj = USSR.AddObjective("Defeat the Allies.")
	TakeOutPower = Greece.AddObjective("Bring down the power of the base to the east.")

	Trigger.OnObjectiveAdded(Greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(Greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(Greece, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	Trigger.OnPlayerLost(Greece, function()
		Media.PlaySpeechNotification(Greece, "Lose")
	end)
	Trigger.OnPlayerWon(Greece, function()
		Media.PlaySpeechNotification(Greece, "Win")
	end)

	StartSpy.DisguiseAsType("e1", BadGuy)
	Camera.Position = DefaultCameraPosition.CenterPosition
	SetupTriggers()
end
