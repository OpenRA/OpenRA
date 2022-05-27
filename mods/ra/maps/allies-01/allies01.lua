--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
InsertionHelicopterType = "tran.insertion"
InsertionPath = { InsertionEntry.Location, InsertionLZ.Location }
ExtractionHelicopterType = "tran.extraction"
ExtractionPath = { SouthReinforcementsPoint.Location, ExtractionLZ.Location }
JeepReinforcements = { "jeep", "jeep" }
TanyaReinforcements = { "e7.noautotarget" }
EinsteinType = "einstein"
FlareType = "flare"
CruisersReinforcements = { "ca", "ca", "ca", "ca" }
OpeningAttack = { Patrol1, Patrol2, Patrol3, Patrol4 }
Responders = { Response1, Response2, Response3, Response4, Response5 }
LabGuardsTeam = { LabGuard1, LabGuard2, LabGuard3 }

SendInsertionHelicopter = function()
	local passengers = Reinforcements.ReinforceWithTransport(Greece, InsertionHelicopterType,
		TanyaReinforcements, InsertionPath, { InsertionEntry.Location })[2]
	local tanya = passengers[1]
	Trigger.OnKilled(tanya, TanyaKilledInAction)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
	end)
end

SendJeeps = function()
	Reinforcements.Reinforce(Greece, JeepReinforcements, InsertionPath, DateTime.Seconds(2))
	Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
end

RunInitialActivities = function()
	SendInsertionHelicopter()

	Utils.Do(OpeningAttack, function(a)
		IdleHunt(a)
	end)

	Trigger.OnKilled(Patrol3, function()
		if not Civilian1.IsDead then
			Civilian1.Move(CivMove.Location)
		end
	end)

	Trigger.OnKilled(BarrelPower, function()
		if not Civilian2.IsDead then
			Civilian2.Move(CivMove.Location)
		end
		Utils.Do(Responders, function(r)
			if not r.IsDead then
				IdleHunt(r)
			end
		end)
	end)
end

LabGuardsKilled = function()
	CreateEinstein()

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Actor.Create(FlareType, true, { Owner = England, Location = ExtractionFlarePoint.Location })
		Media.PlaySpeechNotification(Greece, "SignalFlareNorth")
		SendExtractionHelicopter()
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Media.PlaySpeechNotification(Greece, "AlliedReinforcementsArrived")
		Actor.Create("camera", true, { Owner = Greece, Location = CruiserCameraPoint.Location })
		SendCruisers()
	end)

	Trigger.AfterDelay(DateTime.Seconds(12), function()
		for i = 0, 2 do
			Trigger.AfterDelay(DateTime.Seconds(i), function()
				Media.PlaySoundNotification(Greece, "AlertBuzzer")
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
	heli = Reinforcements.ReinforceWithTransport(Greece, ExtractionHelicopterType, nil, ExtractionPath)[1]
	if not einstein.IsDead then
		Trigger.OnRemovedFromWorld(einstein, EvacuateHelicopter)
	end
	Trigger.OnKilled(heli, RescueFailed)
	Trigger.OnRemovedFromWorld(heli, HelicopterGone)
end

EvacuateHelicopter = function()
	if heli.HasPassengers then
		heli.Move(ExtractionExitPoint.Location)
		heli.Destroy()
	end
end

SendCruisers = function()
	local i = 1
	Utils.Do(CruisersReinforcements, function(cruiser)
		local ca = Actor.Create(cruiser, true, { Owner = England, Location = SouthReinforcementsPoint.Location + CVec.New(2 * i, 0) })
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
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	Greece.MarkFailedObjective(EinsteinSurviveObjective)
end

TanyaKilledInAction = function()
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	Greece.MarkFailedObjective(TanyaSurviveObjective)
end

OilPumpDestroyed = function()
	Trigger.AfterDelay(DateTime.Seconds(5), SendJeeps)
end

CiviliansKilled = function()
	Greece.MarkFailedObjective(CivilProtectionObjective)
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	collateralDamage = true
end

LostMate = function()
	if not Civilian2.IsDead then
		Civilian2.Panic()
	end
end

CreateEinstein = function()
	Greece.MarkCompletedObjective(FindEinsteinObjective)
	Media.PlaySpeechNotification(Greece, "ObjectiveMet")
	einstein = Actor.Create(EinsteinType, true, { Location = EinsteinSpawnPoint.Location, Owner = Greece })
	einstein.Scatter()
	Trigger.OnKilled(einstein, RescueFailed)
	ExtractObjective = Greece.AddObjective("Wait for the helicopter and extract Einstein.")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Greece, "TargetFreed") end)
end

HelicopterGone = function()
	if not heli.IsDead then
		Media.PlaySpeechNotification(Greece, "TargetRescued")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Greece.MarkCompletedObjective(ExtractObjective)
			Greece.MarkCompletedObjective(EinsteinSurviveObjective)
			if not Greece.IsObjectiveFailed(TanyaSurviveObjective) then
				Greece.MarkCompletedObjective(TanyaSurviveObjective)
			end
			if not collateralDamage then
				Greece.MarkCompletedObjective(CivilProtectionObjective)
			end
		end)
	end
end

SetUnitStances = function()
	Utils.Do(Map.NamedActors, function(a)
		if a.Owner == Greece then
			a.Stance = "Defend"
		end
	end)
end

Tick = function()
	USSR.Resources = USSR.Resources - (0.01 * USSR.ResourceCapacity / 25)
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")

	InitObjectives(Greece)

	FindEinsteinObjective = Greece.AddObjective("Find Einstein.")
	TanyaSurviveObjective = Greece.AddObjective("Tanya must survive.")
	EinsteinSurviveObjective = Greece.AddObjective("Einstein must survive.")
	CivilProtectionObjective = Greece.AddObjective("Protect all civilians.", "Secondary", false)

	RunInitialActivities()

	Trigger.OnKilled(Lab, LabDestroyed)
	Trigger.OnKilled(OilPump, OilPumpDestroyed)

	sovietArmy = USSR.GetGroundAttackers()

	Trigger.OnAllKilled(LabGuardsTeam, LabGuardsKilled)

	collateralDamage = false
	civilianTeam = { Civilian1, Civilian2 }
	Trigger.OnAnyKilled(civilianTeam, CiviliansKilled)
	Trigger.OnKilled(Civilian1, LostMate)

	SetUnitStances()

	Trigger.AfterDelay(DateTime.Seconds(5), function() Actor.Create("camera", true, { Owner = Greece, Location = BaseCameraPoint.Location }) end)

	Camera.Position = InsertionLZ.CenterPosition
end
