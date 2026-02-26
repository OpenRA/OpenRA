--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

--[[
APPROACH
First of all I tried to stick to the original level spirit more than copying verbatim triggers/events.

ORIGINAL VS ORA VERSION
The staple element of the mission IMO is the frequent "iron-curtained" mammoth tanks that come from the left (USSR player) 
so I wanted to keep that as close as possible to original intent. The deviation here, to make iron mammoth tanks more of a threat,
was to activate iron curtain only once half hp is reached instead of arrival to x coordinate. Also added a function to allow these to switch 
targets after x seconds to reduce the chance for these to be cheesed.
The village to the left of player units arrival is also another element that I would like to change a little to have more relevance overall.
Since turreted vehicles can attack while moving and most of original triggers are barely noticeable I thought about giving player a neutral
stance towards civilians, so the player will have to "force attack" them. Civilians will have a few instances where they can trigger
attacks made by USSR if the player disturbs the village. There is also the idea of adding and extra secondary objective, something around
the line of: "keep civilian casualties to a bare minimum".
Pointed by JovialFeline, in vanilla ra allies-12, FCom, if captured/destroy, selfs-destruct power plants from BadGuy base. I think this a bit of a strong effect to
keep. Maybe this could happen only if FCom is captured so it requires a bit more thought for the player if he/she wants to take that route.
Last but not least, I created a cruiser variant for England player to be able to destroy sov. beachhead as close as possible to how this
occurs in the og. ra level

BALANCE
As mentioned before, AI will send attacks occasionally but the bulk of the AI efforts will be in protecting the base and other behaviors that make it hard
for the VIP unit to leave the base. Among these changes

OTHER
No secondary objective yet
]]

LstReinforcements = { actors = { "mcv" }, entryPath = { AlliedReinforcementsEntry.Location, Unload1.Location }, exitPath = { AlliedReinforcementsEntry.Location } }

NavalReinforcements = { actors = { "ca", "ca" }, path = { AlliedReinforcementsEntry.Location, CruisersDst.Location } }

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

TopExitBlockers = { TopExitBlocker1, TopExitBlocker2, TopExitBlocker3 }
SideExitBlockers = { SideExitBlocker1, SideExitBlocker2, SideExitBlocker3 }
BotExitBlockers = { BotExitBlocker1, BotExitBlocker2, BotExitBlocker3 }
MMGuardPoints = { MMStop1.Location, MMStop2.Location, MMStop3.Location}

GuardDogs = { GuardDog1, GuardDog2, GuardDog3 }
GuardDogPoint = { BlockExit1.Location, BlockExit2.Location, BlockExit3.Location }

HuntDogTriggers = { BlockExit1.CenterPosition + WVec.New(1024*4,0,0), BlockExit2.CenterPosition  + WVec.New(1024*3,0,0), BlockExit3.CenterPosition }

CameraTriggers = { LowerBaseProximityCam.CenterPosition, UpperBaseProximityCam.CenterPosition }

KosyginType = "gnrl"
KosyginContacted = false

InitialSovietPatrols = function()
	PatrollingDog1.Patrol(DogPatrol1Path, true, DateTime.Seconds(60))
	PatrollingDog2.Patrol(DogPatrol2Path, true, DateTime.Seconds(90))
	for i = 1, 2 do
		TankGroup[i].Patrol(TankGroupPatrolPath, true, DateTime.Seconds(30))
	end
end

DogsGuardGates = function()
	for i = 1, 3 do
		if not GuardDogs[i].IsDead then
			GuardDogs[i].AttackMove(GuardDogPoint[i])
		end
	end
end

MMsGuardMainGate = function()
	for i = 1, 3 do
		if not BotExitBlockers[i].IsDead then
			BotExitBlockers[i].AttackMove(MMGuardPoints[i])
		end
	end
end

USSRLockBase = function()
	DogsGuardGates()
	MMsGuardMainGate()
	BlockGate(TopExitBlockers, BlockExit1.Location)
	BlockGate(SideExitBlockers, BlockExit2.Location)
	DefendAgainstInfiltration(USSR, GuardDog4)
end

BlockGate = function(units, waypoint)
	Utils.Do(units, function(u)
		if not u.IsDead then
			u.Move(waypoint)
		end
	end)
end

InfiltrateForwardCommand = function()
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

CreateKosygin = function()
	Greece.MarkCompletedObjective(UseSpyObjective)
	Media.PlaySpeechNotification(Greece, "ObjectiveMet")
	local kosygin = Reinforcements.Reinforce(Greece, {KosyginType}, {KosyginSpawnPoint.Location, KosyginDst.Location})[1]
	Trigger.OnKilled(kosygin, RescueFailed)

	ExtractObjective = AddPrimaryObjective(Greece, "extract-kosygin")
	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Greece, "TargetFreed") end)

	TriggerHuntKosygin()
end

RescueFailed = function()
	Media.PlaySpeechNotification(Greece, "ObjectiveNotMet")
	Greece.MarkFailedObjective(KosyginSurviveObjective)
	Greece.MarkFailedObjective(ExtractObjective)
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

TriggerHuntKosygin = function()
	Utils.Do(HuntDogTriggers, function(t)
		Trigger.OnEnteredProximityTrigger(t, WDist.FromCells(4), function(actor, id)
			if actor.Type == KosyginType then
				Trigger.RemoveProximityTrigger(id)
				Utils.Do(HuntDogsGroup, function(d)
					if not d.IsDead then
						d.Attack(actor)
					end
				end)
			end
		end)
	end)
end

TriggerCameras = function()
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

InitialAlliedReinforcements = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.ReinforceWithTransport(Greece, "lst.reinforcement", LstReinforcements.actors, LstReinforcements.entryPath, LstReinforcements.exitPath)
	end)
end

SendNavalSupport = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		Reinforcements.Reinforce(Greece, NavalReinforcements.actors, NavalReinforcements.path)
	end)
end

PrepareObjectives = function()
	InitObjectives(Greece)
	
	UseSpyObjective = AddPrimaryObjective(Greece, "infiltrate-soviet-command-center-contact-kosygin")
	KosyginSurviveObjective = AddPrimaryObjective(Greece, "kosygin-must-survive")

	USSRObj = AddPrimaryObjective(USSR, "")
end

InitTriggers = function()
	Greece.Cash = 6000

	TriggerCameras()

	InitialAlliedReinforcements()

	InfiltrateForwardCommand()
	TriggerExtractKosygin()

	InitialSovietPatrols()

	ActivateAI()
end

Tick = function()
	USSR.Resources = USSR.Resources - (0.01 * USSR.ResourceCapacity / 25)
	USSR.Cash = 5000

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRObj)
	end
end

WorldLoaded = function()
	Camera.Position = DefaultCameraPosition.CenterPosition

	Greece = Player.GetPlayer("Greece")
	USSR = Player.GetPlayer("USSR")

	SetDifficulty()
	PrepareObjectives()
	InitTriggers()

	--Actor182.Guard(GuardPoint2)
	--Actor182.Guard(Actor174)
end
