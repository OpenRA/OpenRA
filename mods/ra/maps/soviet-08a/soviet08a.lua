--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
alliedScouts = { Actor189, Actor216, Actor217, Actor218, Actor219 }

ussrReinforcements = 
{
	east =
	{
		actors = { "e1", "e1", "e1", "e1", "e1" },
		entryPath = { EastEntry.Location, EastUnload.Location + CVec.New(1, 0) },
		exitPath = { EastEntry.Location },
	},
	south =
	{
		actors = { "e4", "e4", "e1", "e1", "e1" },
		entryPath = { SouthEntry.Location, SouthUnload.Location + CVec.New(0, 1) },
		exitPath = { SouthEntry.Location }
	},
	mammoth =
	{
		actors = { "4tnk" },
		entryPath = { SouthEntry.Location, SouthUnload.Location + CVec.New(0, 1) },
		exitPath = { SouthEntry.Location }
	}
}

Obj2ActorTriggerActivator = { Church, Actor147, Actor148, Actor149, Actor150, Actor151, Actor152, Actor153 }

ActivateAIDelay = DateTime.Seconds(45)

AddEastReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(ussr, "ReinforcementsArrived")
		local reinforcement = ussrReinforcements.east
		Reinforcements.ReinforceWithTransport(ussr, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

AddSouthReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(60), function()
		Media.PlaySpeechNotification(ussr, "ReinforcementsArrived")
		local reinforcement = ussrReinforcements.south
		Reinforcements.ReinforceWithTransport(ussr, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

AddParadropReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(90), function()
		Media.PlaySpeechNotification(ussr, "ReinforcementsArrived")
		scripteddrop.SendParatroopers(ScriptedParadrop.CenterPosition, false, 10)
	end)
end

ChurchAmbushTrigger = function()
	if not AmbushSwitch then
		local hiding = Reinforcements.Reinforce(germany, { 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3' }, { ChurchAmbush.Location, AmbushMove.Location })
		Utils.Do(hiding, function(actor)
			IdleHunt(actor)
		end)
	end
	AmbushSwitch = true
end

Trigger.OnKilled(Church, function()
	Actor.Create("moneycrate", true, { Owner = ussr, Location = ChurchAmbush.Location })
end)

Obj2TriggerFunction = function()
	ussr.MarkCompletedObjective(DestroyVillageObjective)
	Media.PlaySpeechNotification(ussr, "ReinforcementsArrived")
	local reinforcement = ussrReinforcements.mammoth
	Reinforcements.ReinforceWithTransport(ussr, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
end

AddReinforcmentTriggers = function()
	AddEastReinforcementTrigger()
	AddSouthReinforcementTrigger()
	AddParadropReinforcementTrigger()
end

AddRetreatTrigger = function()
	Trigger.OnEnteredProximityTrigger(Actor222.CenterPosition, WDist.FromCells(12), function(actor, id)
		if actor.Owner == ussr and actor.Type == "barr" then
			alliedScouts = Utils.Where(alliedScouts, function(scout) return not scout.IsDead end)
			local removed
			Utils.Do(alliedScouts, function(scout)
				if scout.Type == "e1" and not removed then
					removed = true
				else
					scout.Stop()
					scout.Move(ScoutRetreat.Location, 1)
				end
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)
end

Tick = function()
	greece.Cash = 1000

	if greece.HasNoRequiredUnits() and germany.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(KillAll)
	end

	if ussr.HasNoRequiredUnits() then
		greece.MarkCompletedObjective(BeatUSSR)
	end
end

WorldLoaded = function()
	ussr = Player.GetPlayer("USSR")
	germany = Player.GetPlayer("Germany")
	greece = Player.GetPlayer("Greece")
	
	Trigger.OnObjectiveAdded(ussr, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	
	KillAll = ussr.AddPrimaryObjective("Destroy all Allied units and structures.")
	DestroyVillageObjective = ussr.AddSecondaryObjective("Destroy the village of Allied sympathizers.")
	BeatUSSR = greece.AddPrimaryObjective("Defeat the Soviet forces.")
	
	Trigger.OnObjectiveCompleted(ussr, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(ussr, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(ussr, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(ussr, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(ussr, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(ussr, "MissionAccomplished")
		end)
	end)

	AddReinforcmentTriggers()
	AddRetreatTrigger()
	
	scripteddrop = Actor.Create("scripteddrop", false, { Owner = ussr })
	
	OnAnyDamaged(Obj2ActorTriggerActivator, ChurchAmbushTrigger)
	
	Trigger.OnAllRemovedFromWorld(Obj2ActorTriggerActivator, Obj2TriggerFunction)
	
	Camera.Position = SovietBase.CenterPosition
	
	Trigger.AfterDelay(ActivateAIDelay, ActivateAI)
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end
