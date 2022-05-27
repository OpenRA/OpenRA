--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

RunInitialActivities = function()
	Harvester.FindResources()
	Helper.Destroy()
	IdlingUnits()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		BringPatrol1()
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			BringPatrol2()
		end)
		BuildBase()
	end)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == Greece and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < 3/4 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Trigger.OnKilled(Powr, function(building)
		BasePower.exists = false
	end)

	Trigger.OnKilled(Barr, function(building)
		BaseBarracks.exists = false
	end)

	Trigger.OnKilled(Proc, function(building)
		BaseProc.exists = false
	end)

	Trigger.OnKilled(Weap, function(building)
		BaseWeaponsFactory.exists = false
	end)

	Trigger.OnEnteredFootprint(VillageCamArea, function(actor, id)
		if actor.Owner == player then
			Trigger.RemoveFootprintTrigger(id)

			if not AllVillagersDead then
				VillageCamera = Actor.Create("camera", true, { Owner = player, Location = VillagePoint.Location })
			end
		end
	end)

	Trigger.OnAllKilled(Village, function()
		if VillageCamera then
			VillageCamera.Destroy()
		end
		AllVillagersDead = true
	end)

	Trigger.OnAnyKilled(Civs, function()
		Trigger.ClearAll(civ1)
		Trigger.ClearAll(civ2)
		Trigger.ClearAll(civ3)
		Trigger.ClearAll(civ4)
		local units = Reinforcements.Reinforce(Greece, Avengers, { NRoadPoint.Location }, 0)
		Utils.Do(units, function(unit)
			unit.Hunt()
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)

	if Difficulty == "hard" or Difficulty == "normal" then
		Trigger.AfterDelay(DateTime.Seconds(5), ReinfInf)
	end
	Trigger.AfterDelay(DateTime.Minutes(1), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(3), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(2), ReinfArmor)
end

Tick = function()
	if Greece.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillAll)
		player.MarkCompletedObjective(KillRadar)
	end

	if player.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(BeatUSSR)
	end

	if Greece.Resources >= Greece.ResourceCapacity * 0.75 then
		Greece.Cash = Greece.Cash + Greece.Resources - Greece.ResourceCapacity * 0.25
		Greece.Resources = Greece.ResourceCapacity * 0.25
	end

	if RCheck then
		RCheck = false
		if Difficulty == "hard" then
			Trigger.AfterDelay(DateTime.Seconds(150), ReinfArmor)
		elseif Difficulty == "normal" then
			Trigger.AfterDelay(DateTime.Minutes(5), ReinfArmor)
		else
			Trigger.AfterDelay(DateTime.Minutes(8), ReinfArmor)
		end
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	RunInitialActivities()

	InitObjectives(player)

	KillAll = player.AddObjective("Defeat the Allied forces.")
	BeatUSSR = Greece.AddObjective("Defeat the Soviet forces.")
	KillRadar = player.AddObjective("Destroy Allied Radar Dome to stop enemy\nreinforcements.", "Secondary", false)

	Trigger.OnKilled(RadarDome, function()
		player.MarkCompletedObjective(KillRadar)
		Media.PlaySpeechNotification(player, "ObjectiveMet")
	end)

	Trigger.OnDamaged(Harvester, function()
		Utils.Do(Guards, function(unit)
			if not unit.IsDead and not Harvester.IsDead then
				unit.AttackMove(Harvester.Location)
			end
		end)
	end)

	Camera.Position = StartCamPoint.CenterPosition
end
