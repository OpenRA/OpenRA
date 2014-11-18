InsertionHelicopterType = "tran.insertion"
InsertionPath = { InsertionEntry.Location, InsertionLZ.Location }
ExtractionHelicopterType = "tran.extraction"
ExtractionPath = { SouthReinforcementsPoint.Location, ExtractionLZ.Location }
JeepReinforcements = { "jeep", "jeep" }
TanyaReinforcements = { "e7" }
EinsteinType = "einstein"
FlareType = "flare"
CruisersReinforcements = { "ca", "ca", "ca", "ca" }

SendInsertionHelicopter = function()
	local passengers = Reinforcements.ReinforceWithTransport(player, InsertionHelicopterType,
		TanyaReinforcements, InsertionPath, { InsertionEntry.Location })[2]
	local tanya = passengers[1]
	Trigger.OnKilled(tanya, RescueFailed)
	tanya.Stance = "HoldFire"
end

SendJeeps = function()
	Reinforcements.Reinforce(player, JeepReinforcements, InsertionPath, DateTime.Seconds(2))
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
end

RunInitialActivities = function()
	SendInsertionHelicopter()
	Patrol1.Hunt()
	Patrol2.Hunt()
	Patrol3.Hunt()
	Patrol4.Hunt()
	Harvester.FindResources()
	Civilian1.Wait(DateTime.Seconds(6))
	Civilian2.Wait(DateTime.Seconds(6))
	Civilian1.Hunt()
	Civilian2.Hunt()
end

LabGuardsKilled = function()
	CreateEinstein()

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Actor.Create(FlareType, true, { Owner = england, Location = ExtractionFlarePoint.Location })
		Media.PlaySpeechNotification(player, "SignalFlareNorth")
		SendExtractionHelicopter()
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.PlaySpeechNotification(player, "AlliedReinforcementsArrived")
		Actor.Create("camera", true, { Owner = player, Location = CruiserCameraPoint.Location })
		SendCruisers()
	end)

	Trigger.AfterDelay(DateTime.Seconds(12), function()
		for i = 0, 2 do
			Trigger.AfterDelay(DateTime.Seconds(i), function()
				Media.PlaySoundNotification(player, "AlertBuzzer")
			end)
		end
		Utils.Do(sovietArmy, function(a)
			if not a.IsDead and a.HasProperty("Hunt") then
				Trigger.OnIdle(a, a.Hunt)
			end
		end)
	end)
end

SendExtractionHelicopter = function()
	heli = Reinforcements.ReinforceWithTransport(player, ExtractionHelicopterType, nil, ExtractionPath)[1]
	if not einstein.IsDead then
		Trigger.OnRemovedFromWorld(einstein, EvacuateHelicopter)
	end
	Trigger.OnKilled(heli, RescueFailed)
	Trigger.OnRemovedFromWorld(heli, HelicopterGone)
end

EvacuateHelicopter = function()
	if heli.HasPassengers then
		heli.Move(ExtractionExitPoint.Location)
		Trigger.OnIdle(heli, heli.Destroy)
	end
end

SendCruisers = function()
	local i = 1
	Utils.Do(CruisersReinforcements, function(cruiser)
		local ca = Actor.Create(cruiser, true, { Owner = england, Location = SouthReinforcementsPoint.Location + CVec.New(2 * i, 0) })
		ca.Move(Map.NamedActor("CruiserPoint" .. i).Location)
		i = i + 1
	end)
end

LabDestroyed = function()
	if not einstein then
		RescueFailed()
	end
end

RescueFailed = function()
	player.MarkFailedObjective(SurviveObjective)
	ussr.MarkCompletedObjective(DefendObjective)
end

OilPumpDestroyed = function()
	Trigger.AfterDelay(DateTime.Seconds(5), SendJeeps)
end

CiviliansKilled = function()
	player.MarkFailedObjective(CivilProtectionObjective)
	Media.PlaySpeechNotification(player, "ObjectiveNotMet")
	collateralDamage = true
end

CreateEinstein = function()
	player.MarkCompletedObjective(FindEinsteinObjective)
	Media.PlaySpeechNotification(player, "ObjectiveMet")
	einstein = Actor.Create(EinsteinType, true, { Location = EinsteinSpawnPoint.Location, Owner = player })
	einstein.Scatter()
	Trigger.OnKilled(einstein, RescueFailed)
	ExtractObjective = player.AddPrimaryObjective("Wait for the helicopter and extract Einstein.")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(player, "TargetFreed") end)
end

HelicopterGone = function()
	if not heli.IsDead then
		Media.PlaySpeechNotification(player, "TargetRescued")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			player.MarkCompletedObjective(ExtractObjective)
			player.MarkCompletedObjective(SurviveObjective)
			ussr.MarkFailedObjective(DefendObjective)
			if not collateralDamage then
				player.MarkCompletedObjective(CivilProtectionObjective)
			end
		end)
	end
end

MissionAccomplished = function()
	Media.PlaySpeechNotification(player, "Win")
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlayMovieFullscreen("snowbomb.vqa")
	end)
end

MissionFailed = function()
	Media.PlaySpeechNotification(player, "Lose")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlayMovieFullscreen("bmap.vqa") end)
end

SetUnitStances = function()
	Utils.Do(Map.NamedActors, function(a)
		if a.Owner == player then
			a.Stance = "Defend"
		end
	end)
end

Tick = function()
	ussr.Resources = ussr.Resources - (0.01 * ussr.ResourceCapacity / 25)
end

WorldLoaded = function()
	player = Player.GetPlayer("Greece")
	england = Player.GetPlayer("England")
	ussr = Player.GetPlayer("USSR")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, MissionFailed)
	Trigger.OnPlayerWon(player, MissionAccomplished)

	Media.PlayMovieFullscreen("landing.vqa", function()
		FindEinsteinObjective = player.AddPrimaryObjective("Find Einstein.")
		SurviveObjective = player.AddPrimaryObjective("Tanya and Einstein must survive.")
		england.AddPrimaryObjective("Destroy the soviet base after a successful rescue.")
		CivilProtectionObjective = player.AddSecondaryObjective("Protect all civilians.")
		DefendObjective = ussr.AddPrimaryObjective("Kill Tanya and keep Einstein hostage.")

		RunInitialActivities()
	end)

	Trigger.OnKilled(Lab, LabDestroyed)
	Trigger.OnKilled(OilPump, OilPumpDestroyed)

	sovietArmy = ussr.GetGroundAttackers()

	labGuardsTeam = { LabGuard1, LabGuard2, LabGuard3 }
	Trigger.OnAllKilled(labGuardsTeam, LabGuardsKilled)

	collateralDamage = false
	civilianTeam = { Civilian1, Civilian2 }
	Trigger.OnAnyKilled(civilianTeam, CiviliansKilled)

	SetUnitStances()

	Trigger.AfterDelay(DateTime.Seconds(5), function() Actor.Create("camera", true, { Owner = player, Location = BaseCameraPoint.Location }) end)

	Camera.Position = InsertionLZ.CenterPosition
end
