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
	IdlingUnits()
	Trigger.AfterDelay(10, function()
		BringPatrol1()
		BringPatrol2()
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

	Reinforcements.Reinforce(player, SovietMCV, SovietStartToBasePath, 0, function(mcv)
		mcv.Move(StartCamPoint.Location)
	end)
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")

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
		local units = Reinforcements.Reinforce(Greece, Avengers, { SWRoadPoint.Location }, 0)
		Utils.Do(units, function(unit)
			unit.Hunt()
		end)
	end)

	Runner1.Move(CrossroadsEastPoint.Location)
	Runner2.Move(InVillagePoint.Location)
	Tank5.Move(V2MovePoint.Location)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Tank1.Stop()
		Tank2.Stop()
		Tank3.Stop()
		Tank4.Stop()
		Tank5.Stop()
		Trigger.AfterDelay(1, function()
			Tank1.Move(SovietBaseEntryPointNE.Location)
			Tank2.Move(SovietBaseEntryPointW.Location)
			Tank3.Move(SovietBaseEntryPointNE.Location)
			Tank4.Move(SovietBaseEntryPointW.Location)
			Tank5.Move(V2MovePoint.Location)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceArmor)

	if Difficulty == "hard" or Difficulty == "normal" then
		Trigger.AfterDelay(DateTime.Seconds(15), ReinfInf)
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

	Camera.Position = StartCamPoint.CenterPosition
end
