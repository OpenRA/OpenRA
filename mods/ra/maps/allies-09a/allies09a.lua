--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

lstReinforcements =
{
	actors = { "mcv" },
	entryPath = { AlliedMCVEntry.Location, Unload1.Location },
	exitPath = { AlliedMCVEntry.Location }
}

ExtractionHelicopterType = "tran.extraction"
ExtractionPath = { HeliWP01.Location, HeliWP02.Location, HeliWP03.Location }
Dog5PatrolPath = { WP94.Location, WP93.Location }
Dog6PatrolPath = { WP90.Location, WP91.Location, WP92.Location, WP91.Location }
TankGroup10 = { TankGroup101, TankGroup102 }
TankGroup10PatrolPath = { WP81.Location, WP82.Location, WP83.Location, WP84.Location, WP85.Location, WP84.Location, WP83.Location, WP82.Location }
HuntDogsGroup = { Dog701, Dog702, Dog703, Dog704, Dog705, Dog706 }

KosyginType = "gnrl"
KosyginContacted = false

MissionAccomplished = function()
	Media.PlaySpeechNotification(Greece, "MissionAccomplished")
end

MissionFailed = function()
	Media.PlaySpeechNotification(Greece, "MissionFailed")
end

InitialAlliedReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", lstReinforcements.actors, lstReinforcements.entryPath, lstReinforcements.exitPath)
	end)
end

RescueFailed = function()
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	Greece.MarkFailedObjective(KosyginSurviveObjective)
end

InitialSovietPatrols = function()
	Dog5.Patrol(Dog5PatrolPath, true, DateTime.Seconds(60))
	Dog6.Patrol(Dog6PatrolPath, true, DateTime.Seconds(90))
	for i = 1, 2 do
		TankGroup10[i].Patrol(TankGroup10PatrolPath, true, DateTime.Seconds(30))
	end
end

CreateKosygin = function()
	Greece.MarkCompletedObjective(UseSpyObjective)
	Media.PlaySpeechNotification(Greece, "ObjectiveMet")
	local kosygin = Actor.Create(KosyginType, true, { Location = KosyginSpawnPoint.Location, Owner = Greece })
	Trigger.OnKilled(kosygin, RescueFailed)
	ExtractObjective = Greece.AddObjective("Extract Kosygin and\nget him back to your base.")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Greece, "TargetFreed") end)
end

DogsGuardGates = function()
	if not Dog707.IsDead then
		Dog707.AttackMove(WP89.Location)
	end
	if not Dog708.IsDead then
		Dog708.AttackMove(WP81.Location)
	end
	if not Dog709.IsDead then
		Dog709.AttackMove(WP79.Location)
	end
end

InfiltrateForwardCenter = function()
	Trigger.OnInfiltrated(USSRFC, function()
		if not KosyginContacted then
			KosyginContacted = true
			CreateKosygin()
			DogsGuardGates()
		end
	end)

	Trigger.OnKilledOrCaptured(USSRFC, function()
		if not Greece.IsObjectiveCompleted(UseSpyObjective) then
			Greece.MarkFailedObjective(UseSpyObjective)
		end
	end)
end

Tick = function()
	USSR.Cash = 5000
	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRObj)
	end
end

TriggerHuntKosygin = function()
	Trigger.OnEnteredProximityTrigger(WP79.CenterPosition, WDist.FromCells(4), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Trigger.RemoveProximityTrigger(triggerflee)
			for i = 1, 6 do
				if not HuntDogsGroup[i].IsDead then
					HuntDogsGroup[i].Attack(actor)
				end
			end
		end
	end)
	Trigger.OnEnteredProximityTrigger(WP81.CenterPosition, WDist.FromCells(4), function(actor, triggerflee)
		if actor.Type == KosyginType then
			Trigger.RemoveProximityTrigger(triggerflee)
			for i = 1, 6 do
				if not HuntDogsGroup[i].IsDead then
					HuntDogsGroup[i].Attack(actor)
				end
			end
		end
	end)
	Trigger.OnEnteredProximityTrigger(WP89.CenterPosition, WDist.FromCells(4), function(actor, triggerflee)
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
	Trigger.OnEnteredProximityTrigger(LowerBaseWP.CenterPosition, WDist.FromCells(10), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = Greece, Location = RevealLowerBase.Location })
			Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
		end
	end)
end	

TriggerRevealUSSRFC = function()
	Trigger.OnEnteredProximityTrigger(UpperBaseWP.CenterPosition, WDist.FromCells(10), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			local cam = Actor.Create("Camera", true, { Owner = Greece, Location = KosyginSpawnPoint.Location })
			Trigger.AfterDelay(DateTime.Seconds(15), cam.Destroy)
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

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")
	Camera.Position = DefaultCameraPosition.CenterPosition	
	UseSpyObjective = Greece.AddObjective("Infiltrate the Soviet command center and\ncontact Kosygin.")
	KosyginSurviveObjective = Greece.AddObjective("Kosygin must survive.")
	USSRObj = USSR.AddObjective("Eliminate all Allied forces.")
	Trigger.OnPlayerLost(Greece, MissionFailed)
	Trigger.OnPlayerWon(Greece, MissionAccomplished)
	InitialAlliedReinforcements()
	InfiltrateForwardCenter()
	InitialSovietPatrols()
	TriggerRevealUSSRBase()
	TriggerRevealUSSRFC()
	TriggerExtractKosygin()
	TriggerHuntKosygin()
	ActivateAI()
end
