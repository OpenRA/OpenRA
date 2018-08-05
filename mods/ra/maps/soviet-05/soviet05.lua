--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
CheckForBase = function()
	baseBuildings = Map.ActorsInBox(Map.TopLeft, CFBPoint.CenterPosition, function(actor)
		return actor.Type == "fact" or actor.Type == "powr"
	end)

	return #baseBuildings >= 2
end

CheckForCYard = function()
	ConYard = Map.ActorsInBox(mcvGGLoadPoint.CenterPosition, ReinfEastPoint.CenterPosition, function(actor)
		return actor.Type == "fact" and actor.Owner == GoodGuy
	end)

	return #ConYard >= 1
end

CheckForSPen = function()
	return Utils.Any(Map.ActorsInWorld, function(actor) return actor.Type == "spen" end)
end

RunInitialActivities = function()
	if Map.LobbyOption("difficulty") == "hard" then
		Expand()
		ExpansionCheck = true
	else
		ExpansionCheck = false
	end

	Trigger.AfterDelay(1, function()
		Harvester.FindResources()
		Helper.Destroy()
		IdlingUnits()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")

		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < building.MaxHealth * 3/4 then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)

	Reinforcements.Reinforce(player, SovietStartReinf, SovietStartToBasePath, 0, function(soldier)
		soldier.AttackMove(SovietBasePoint.Location)
	end)

	Actor.Create("camera", true, { Owner = player, Location = GreeceBasePoint.Location })
	Actor.Create("camera", true, { Owner = player, Location = SovietBasePoint.Location })

	startmcv.Move(MCVStartMovePoint.Location)
	Runner1.Move(RunnerPoint.Location)
	Runner2.Move(RunnerPoint.Location)
	Runner3.Move(RunnerPoint.Location)

	ProduceInfantry()
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceShips)

	if Map.LobbyOption("difficulty") == "hard" or Map.LobbyOption("difficulty") == "normal" then
		Trigger.AfterDelay(DateTime.Seconds(25), ReinfInf)
	end
	Trigger.AfterDelay(DateTime.Minutes(2), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(3), BringDDPatrol2)
	Trigger.AfterDelay(DateTime.Minutes(5), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(6), BringDDPatrol1)
end

Expand = function()
	if ExpansionCheck then
		return
	elseif mcvtransport.IsDead then
		return
	elseif mcvGG.IsDead then
		return
	end

	mcvGG.Move(mcvGGLoadPoint.Location)
	mcvtransport.Move(lstBeachPoint.Location)
	Media.DisplayMessage("Allied MCV detected moving to the island.")

	Reinforcements.Reinforce(GoodGuy, { "dd", "dd" }, ShipArrivePath, 0, function(ddsquad)
		ddsquad.AttackMove(NearExpPoint.Location) end)

	ExpansionCheck = true
	Trigger.ClearAll(mcvGG)
	Trigger.ClearAll(mcvtransport)
	Trigger.AfterDelay(DateTime.Seconds(3), function()
		if mcvtransport.IsDead then
			return
		elseif mcvGG.IsDead then
			return
		end

		mcvGG.EnterTransport(mcvtransport)
		mcvtransport.Move(GGUnloadPoint.Location)
		mcvtransport.UnloadPassengers()
		Trigger.AfterDelay(DateTime.Seconds(12), function()
			if mcvGG.IsDead then
				return
			end

			mcvGG.Move(MCVDeploy.Location)
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				if not mcvGG.IsDead then
					mcvGG.Deploy()
					Trigger.AfterDelay(DateTime.Seconds(4), function()
						local fact = Map.ActorsInBox(mcvGGLoadPoint.CenterPosition, ReinfEastPoint.CenterPosition, function(actor)
							return actor.Type == "fact" and actor.Owner == GoodGuy end)
						if #fact == 0 then
							return
						else
							Trigger.OnDamaged(fact[1], function()
								if fact[1].Owner == GoodGuy and fact[1].Health < fact[1].MaxHealth * 3/4 then
									fact[1].StartBuildingRepairs()
								end
							end)
						end
					end)

					IslandTroops1()
					Trigger.AfterDelay(DateTime.Minutes(3), IslandTroops2)
					Trigger.AfterDelay(DateTime.Minutes(6), IslandTroops3)
					Trigger.AfterDelay(DateTime.Seconds(7), BuildBase)
				end

				if not mcvtransport.IsDead then
					mcvtransport.Move(ReinfNorthPoint.Location)
					mcvtransport.Destroy()
				end
			end)
		end)
	end)
end

Tick = function()
	if Greece.HasNoRequiredUnits() and GoodGuy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillAll)
		player.MarkCompletedObjective(HoldObjective)
	end

	if player.HasNoRequiredUnits() then
		GoodGuy.MarkCompletedObjective(BeatUSSR)
	end

	if Greece.Resources >= Greece.ResourceCapacity * 0.75 then
		Greece.Cash = Greece.Cash + Greece.Resources - Greece.ResourceCapacity * 0.25
		Greece.Resources = Greece.ResourceCapacity * 0.25
	end

	if GoodGuy.Resources >= GoodGuy.ResourceCapacity * 0.75 then
		GoodGuy.Cash = GoodGuy.Cash + GoodGuy.Resources - GoodGuy.ResourceCapacity * 0.25
		GoodGuy.Resources = GoodGuy.ResourceCapacity * 0.25
	end

	if not baseEstablished and CheckForBase() then
		baseEstablished = true
		Para()
	end

	if not SPenEstablished and CheckForSPen() then
		SPenEstablished = true

		local units = Reinforcements.ReinforceWithTransport(Greece, "lst", ArtyReinf, SouthReinfPath, { ReinfEastPoint.Location })[2]
		Utils.Do(units, function(unit) IdleHunt(unit) end)
		if not ExpansionCheck then
			Expand()
			ExpansionCheck = true
		end
	end

	if not RCheck then
		RCheck = true
		if Map.LobbyOption("difficulty") == "easy" and ReinfCheck then
			Trigger.AfterDelay(DateTime.Minutes(6), ReinfArmor)
		elseif Map.LobbyOption("difficulty") == "normal" then
			Trigger.AfterDelay(DateTime.Minutes(4), ReinfArmor)
		else
			Trigger.AfterDelay(DateTime.Minutes(3), ReinfArmor)
		end
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	GoodGuy = Player.GetPlayer("GoodGuy")
	Greece = Player.GetPlayer("Greece")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
		Media.PlaySpeechNotification(player, "ObjectiveMet")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	CaptureObjective = player.AddPrimaryObjective("Capture the Radar Dome.")
	KillAll = player.AddPrimaryObjective("Defeat the Allied forces.")
	BeatUSSR = GoodGuy.AddPrimaryObjective("Defeat the Soviet forces.")

	RunInitialActivities()

	Trigger.OnDamaged(mcvGG, Expand)
	Trigger.OnDamaged(mcvtransport, Expand)

	Trigger.OnKilled(RadarDome, function()
		if not player.IsObjectiveCompleted(CaptureObjective) then
			player.MarkFailedObjective(CaptureObjective)
		end

		if HoldObjective then
			player.MarkFailedObjective(HoldObjective)
		end
	end)

	RadarDome.GrantCondition("french")
	Trigger.OnCapture(RadarDome, function()
		HoldObjective = player.AddPrimaryObjective("Defend the Radar Dome.")
		player.MarkCompletedObjective(CaptureObjective)
		Beacon.New(player, MCVDeploy.CenterPosition)
		if Map.LobbyOption("difficulty") == "easy" then
			Actor.Create("camera", true, { Owner = player, Location = MCVDeploy.Location })
			Media.DisplayMessage("Movement of an Allied expansion base discovered.")
		else
			Actor.Create("MCV.CAM", true, { Owner = player, Location = MCVDeploy.Location })
			Media.DisplayMessage("Coordinates of an Allied expansion base discovered.")
		end

		if not ExpansionCheck then
			Expand()
			ExpansionCheck = true
		end

		Reinforcements.Reinforce(Greece, ArmorReinfGreece, AlliedCrossroadsToRadarPath , 0, IdleHunt)

		RadarDome.RevokeCondition(1)
		Trigger.ClearAll(RadarDome)
		Trigger.AfterDelay(0, function()
			Trigger.OnRemovedFromWorld(RadarDome, function()
				player.MarkFailedObjective(HoldObjective)
			end)
		end)
	end)

	Trigger.OnEnteredProximityTrigger(USSRExpansionPoint.CenterPosition, WDist.New(4 * 1024), function(unit, id)
		if unit.Owner == player and RadarDome.Owner == player then
			Trigger.RemoveProximityTrigger(id)

			Para2()
			ProduceInfantryGG()
			ProduceTanksGG()

			local units = Reinforcements.ReinforceWithTransport(player, "lst", SovietMCVReinf, { ReinfSouthPoint.Location, USSRlstPoint.Location }, { ReinfSouthPoint.Location })[2]
			Utils.Do(units, function(unit)
				Trigger.OnAddedToWorld(unit, function()
					if unit.Type == "mcv" then
						unit.Move(USSRExpansionPoint.Location)
					else
						unit.AttackMove(USSRExpansionPoint.Location)
					end
				end)
			end)

			Media.PlaySpeechNotification(player, "ReinforcementsArrived")
		end
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	Camera.Position = StartCamPoint.CenterPosition
end
