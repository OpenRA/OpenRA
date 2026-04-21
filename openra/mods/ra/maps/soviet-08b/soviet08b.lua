--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedScouts = { Jeep1, Jeep2, Rifle1, Rifle2, Rifle3, Rifle4 }

SovReinforcements =
{
	one =
	{
		actors = { "e1", "e1", "e1", "e1", "e1" },
		entryPath = { SouthEntry.Location, Unload1.Location + CVec.New(1, 0) },
		exitPath = { SouthEntry.Location },
	},
	two =
	{
		actors = { "e4", "e4", "e1", "e1", "e1" },
		entryPath = { SouthEntry.Location, Unload2.Location + CVec.New(0, 1) },
		exitPath = { SouthEntry.Location }
	},
	mammoth =
	{
		actors = { "4tnk" },
		entryPath = { SouthEntry.Location, Unload1.Location + CVec.New(0, 1) },
		exitPath = { SouthEntry.Location }
	}
}

Village = { Church, Civ1, Civ2, Civ3, Civ4, Civ5, Civ6, Civ7, Civ8 }

ActivateAIDelay = DateTime.Seconds(45)

FirstReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local reinforcement = SovReinforcements.one
		Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

SecondReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(60), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local reinforcement = SovReinforcements.two
		Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
	end)
end

AddParadropReinforcementTrigger = function()
	Trigger.AfterDelay(DateTime.Seconds(90), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ScriptedDrop.TargetParatroopers(ScriptedParadrop.CenterPosition, Angle.New(0))
	end)
end

ChurchAmbushTrigger = function()
	if not AmbushSwitch then
		local hiding = Reinforcements.Reinforce(Germany, { 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e1', 'e3', 'e3', 'e3' }, { ChurchAmbush.Location, AmbushMove.Location }, 0)
		Utils.Do(hiding, IdleHunt)
	end
	AmbushSwitch = true
end

Trigger.OnKilled(Church, function()
	Actor.Create("moneycrate", true, { Owner = USSR, Location = ChurchAmbush.Location })
end)

DestroyVillage = function()
	Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
	USSR.MarkCompletedObjective(DestroyVillageObjective)
	local reinforcement = SovReinforcements.mammoth
	Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
end

AddRetreatTrigger = function()
	Trigger.OnEnteredProximityTrigger(Jeep2.CenterPosition, WDist.FromCells(12), function(actor, id)
		if actor.Owner == USSR and actor.Type == "barr" then
			AlliedScouts = Utils.Where(AlliedScouts, function(scout) return not scout.IsDead end)
			local removed
			Utils.Do(AlliedScouts, function(scout)
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

BoatAttack = function(boat)
	if boat.IsDead then
		return
	else
		boat.AttackMove(BoatRally.Location)
	end
end

Tick = function()
	Greece.Cash = 1000

	if Greece.HasNoRequiredUnits() and Germany.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(KillAll)
	end

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(BeatUSSR)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Germany = Player.GetPlayer("Germany")
	Greece = Player.GetPlayer("Greece")

	InitObjectives(USSR)

	KillAll = AddPrimaryObjective(USSR, "destroy-allied-units-structures")
	DestroyVillageObjective = AddSecondaryObjective(USSR, "destroy-allied-sympathizers-village")
	BeatUSSR = AddPrimaryObjective(Greece, "")

	FirstReinforcementTrigger()
	SecondReinforcementTrigger()
	AddParadropReinforcementTrigger()
	AddRetreatTrigger()

	ScriptedDrop = Actor.Create("scripteddrop", false, { Owner = USSR })

	OnAnyDamaged(Village, ChurchAmbushTrigger)

	Trigger.OnAllRemovedFromWorld(Village, DestroyVillage)

	Camera.Position = SovietBase.CenterPosition

	Trigger.AfterDelay(ActivateAIDelay, ActivateAI)
	Trigger.AfterDelay(DateTime.Minutes(2), function() BoatAttack(Gunboat1) end)
	Trigger.AfterDelay(DateTime.Minutes(5), function() BoatAttack(Gunboat2) end)
	Trigger.AfterDelay(DateTime.Minutes(7), function() BoatAttack(Gunboat3) end)
	Trigger.AfterDelay(DateTime.Minutes(10), function() BoatAttack(Gunboat4) end)
	Trigger.AfterDelay(DateTime.Minutes(12), function() BoatAttack(Gunboat5) end)
	Trigger.AfterDelay(DateTime.Minutes(14), function() BoatAttack(Gunboat6) end)
end
