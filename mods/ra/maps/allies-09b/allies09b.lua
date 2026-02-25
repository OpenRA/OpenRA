--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

LstReinforcements =
{
	actors = { "mcv" },
	entryPath = { AlliedReinforcementsEntry.Location, Unload1.Location },
	exitPath = { AlliedReinforcementsEntry.Location }
}

NavalReinforcements =
{
	actors = { "ca", "ca" },
	path = { AlliedReinforcementsEntry.Location, CruisersDst.Location },
}

ExtractionHelicopterType = "tran.extraction"
ExtractionPath = { TranEntry.Location, KosyginExtractPoint.Location }

DogPatrol1Path = { DogPatrol1WP1.Location, DogPatrol1WP2.Location }
DogPatrol2Path = { DogPatrol2WP1.Location, DogPatrol2WP2.Location, DogPatrol2WP3.Location, DogPatrol2WP2.Location }

TankGroup = { PatrollingHTank1, PatrollingHTank2 }
TankGroupPatrolPath =
{
	TanksPatrol1.Location, TanksPatrol2.Location, TanksPatrol3.Location, TanksPatrol4.Location,
	TanksPatrol5.Location, TanksPatrol4.Location, TanksPatrol3.Location, TanksPatrol2.Location
}

HuntDogsGroup = { HuntDog1, HuntDog2, HuntDog3, HuntDog4, HuntDog5, HuntDog6 }

KosyginType = "gnrl"
KosyginContacted = false

InitialAlliedReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", LstReinforcements.actors, LstReinforcements.entryPath, LstReinforcements.exitPath)
	end)
end

InitialSovietPatrols = function()
	PatrollingDog1.Patrol(DogPatrol1Path, true, DateTime.Seconds(60))
	PatrollingDog2.Patrol(DogPatrol2Path, true, DateTime.Seconds(90))
	for i = 1, 2 do
		TankGroup[i].Patrol(TankGroupPatrolPath, true, DateTime.Seconds(30))
	end

end

CreateKosygin = function()
	Greece.MarkCompletedObjective(UseSpyObjective)
	Media.PlaySpeechNotification(Greece, "ObjectiveMet")
	local kosygin = Reinforcements.Reinforce(Greece, {KosyginType}, {KosyginSpawnPoint.Location, KosyginDst.Location})[1]
	Trigger.OnKilled(kosygin, RescueFailed)

	ExtractObjective = AddPrimaryObjective(Greece, "extract-kosygin")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Greece, "TargetFreed") end)
end

RescueFailed = function()
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	Greece.MarkFailedObjective(KosyginSurviveObjective)
end

DogsGuardGates = function()
	if not GuardDog1.IsDead then
		GuardDog1.AttackMove(DogBlockExit1.Location)
	end
	if not GuardDog2.IsDead then
		GuardDog2.AttackMove(DogBlockExit2.Location)
	end
	if not GuardDog3.IsDead then
		GuardDog3.AttackMove(DogBlockExit3.Location)
	end
end

MMsGuardMainGate = function()
	if not MM1.IsDead then
		MM1.AttackMove(MMStop1.Location)
	end
	if not MM2.IsDead then
		MM2.AttackMove(MMStop2.Location)
	end
	if not MM3.IsDead then
		MM3.AttackMove(MMStop3.Location)
	end
end

USSRLockBase = function()
	DogsGuardGates()
	MMsGuardMainGate()

end

InfiltrateForwardCenter = function()
	Trigger.OnInfiltrated(USSRFC, function()
		if not KosyginContacted then
			KosyginContacted = true
			CreateKosygin()
			SendNavalSupport()
			USSRLockBase()
		end
	end)

	Trigger.OnKilledOrCaptured(USSRFC, function()
		if not Greece.IsObjectiveCompleted(UseSpyObjective) then
			Greece.MarkFailedObjective(UseSpyObjective)
		end
	end)
end

SendNavalSupport = function()
	Reinforcements.Reinforce(Greece, NavalReinforcements.actors, NavalReinforcements.path)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	end)
end

TriggerHuntKosygin = function()
	Trigger.OnEnteredProximityTrigger(DogBlockExit1.CenterPosition + WVec.New(1024*4,0,0), WDist.FromCells(4), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Trigger.RemoveProximityTrigger(triggerflee)
			for i = 1, 6 do
				if not HuntDogsGroup[i].IsDead then
					HuntDogsGroup[i].Attack(actor)
				end
			end
		end
	end)
	Trigger.OnEnteredProximityTrigger(DogBlockExit2.CenterPosition + WVec.New(1024*3,0,0), WDist.FromCells(4), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Trigger.RemoveProximityTrigger(triggerflee)
			for i = 1, 6 do
				if not HuntDogsGroup[i].IsDead then
					HuntDogsGroup[i].Attack(actor)
				end
			end
		end
	end)
	Trigger.OnEnteredProximityTrigger(DogBlockExit3.CenterPosition, WDist.FromCells(4), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Trigger.RemoveProximityTrigger(triggerflee)
			for i = 1, 6 do
				if not HuntDogsGroup[i].IsDead then
					HuntDogsGroup[i].Attack(actor)
				end
			end
		end
	end)
end

TriggerRevealUSSRBase = function()
	Trigger.OnEnteredProximityTrigger(LowerBaseProximityCam.CenterPosition, WDist.FromCells(10), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = Greece, Location = LowerBaseProximityCam.Location })
			if DestroyHelperCameras then
				Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
			end
		end
	end)
end

TriggerRevealUSSRFC = function()
	Trigger.OnEnteredProximityTrigger(UpperBaseProximityCam.CenterPosition, WDist.FromCells(10), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = Greece, Location = KosyginSpawnPoint.Location })
			if DestroyHelperCameras then
				Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
			end
		end
	end)
end

TriggerExtractKosygin = function()
	Trigger.OnEnteredProximityTrigger(KosyginExtractPoint.CenterPosition, WDist.FromCells(10), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Reinforcements.ReinforceWithTransport(Greece, ExtractionHelicopterType, nil, ExtractionPath)
			Trigger.RemoveProximityTrigger(triggerflee)
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				Greece.MarkCompletedObjective(KosyginSurviveObjective)
				Greece.MarkCompletedObjective(ExtractObjective)
				Media.PlaySpeechNotification(Greece, "ObjectiveMet")
			end)
		end
	end)
end

PrepareObjectives = function()
	UseSpyObjective = AddPrimaryObjective(Greece, "infiltrate-soviet-command-center-contact-kosygin")
	KosyginSurviveObjective = AddPrimaryObjective(Greece, "kosygin-must-survive")

	USSRObj = AddPrimaryObjective(USSR, "")
end

InitTriggers = function()
	Greece.Cash = 6000

	InitialAlliedReinforcements()

	TriggerRevealUSSRBase()
	TriggerRevealUSSRFC()
	InfiltrateForwardCenter()
	TriggerExtractKosygin()

	InitialSovietPatrols()
	TriggerHuntKosygin()

	--BuildBase()
	ActivateAI()
end

Tick = function()
	USSR.Cash = 5000

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRObj)
	end

end

WorldLoaded = function()
	Camera.Position = DefaultCameraPosition.CenterPosition

	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	PrepareObjectives()
	InitTriggers()
	--InitObjectives()
end
