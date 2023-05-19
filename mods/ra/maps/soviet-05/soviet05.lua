--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

CheckForBase = function()
	local baseBuildings = Map.ActorsInBox(BaseRectTL.CenterPosition, BaseRectBR.CenterPosition, function(actor)
		return (actor.Type == "fact" or actor.Type == "powr") and actor.Owner == player
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
	if Difficulty == "hard" then
		Expand()
		ExpansionCheck = true
	else
		ExpansionCheck = false
	end

	Trigger.AfterDelay(1, function()
		Harvester.FindResources()
		IdlingUnits()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")

		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == Greece and building.Health < building.MaxHealth * 3/4 then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)

	Reinforcements.Reinforce(USSR, SovietStartReinf, SovietStartToBasePath, 0, function(soldier)
		soldier.AttackMove(SovietBasePoint.Location)
	end)

	Actor.Create("camera", true, { Owner = USSR, Location = GreeceBasePoint.Location })
	Actor.Create("camera", true, { Owner = USSR, Location = SovietBasePoint.Location })

	startmcv.Move(MCVStartMovePoint.Location)
	Runner1.Move(RunnerPoint.Location)
	Runner2.Move(RunnerPoint.Location)
	Runner3.Move(RunnerPoint.Location)

	ProduceInfantry()
	Trigger.AfterDelay(DateTime.Minutes(2), ProduceShips)

	if Difficulty == "hard" or Difficulty == "normal" then
		Trigger.AfterDelay(DateTime.Seconds(25), ReinfInf)
	end
	Trigger.AfterDelay(DateTime.Minutes(2), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(3), BringDDPatrol2)
	Trigger.AfterDelay(DateTime.Minutes(5), ReinfInf)
	Trigger.AfterDelay(DateTime.Minutes(6), BringDDPatrol1)
end

Expand = function()
	if ExpansionCheck or mcvtransport.IsDead or mcvGG.IsDead then
		return
	end

	ExpansionCheck = true
	Trigger.ClearAll(mcvGG)
	Trigger.ClearAll(mcvtransport)
	Media.DisplayMessage(UserInterface.Translate("allied-mcv-island"))

	Reinforcements.Reinforce(GoodGuy, { "dd", "dd" }, ShipArrivePath, 0, function(ddsquad)
		ddsquad.AttackMove(NearExpPoint.Location) end)


	mcvtransport.Move(lstBeachPoint.Location)

	mcvGG.Move(mcvGGLoadPoint.Location)
	mcvGG.EnterTransport(mcvtransport)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		if mcvtransport.IsDead or mcvGG.IsDead then
			return
		end

		mcvtransport.Move(GGUnloadPoint.Location)
		mcvtransport.UnloadPassengers()
		mcvtransport.CallFunc(function()
			if mcvGG.IsDead then
				return
			end

			mcvGG.Move(MCVDeploy.Location)
			mcvGG.CallFunc(function()

				-- Avoid crashing through modifying the actor list from mcvGG's tick
				Trigger.AfterDelay(0, function()
					mcvGG.Owner = GoodGuy

					IslandTroops1()
					Trigger.AfterDelay(DateTime.Minutes(3), IslandTroops2)
					Trigger.AfterDelay(DateTime.Minutes(6), IslandTroops3)

					if not mcvtransport.IsDead then
						mcvtransport.Move(ReinfNorthPoint.Location)
						mcvtransport.Destroy()
					end
				end)

				Trigger.AfterDelay(DateTime.Seconds(1), function()
					GoodGuy.GrantCondition("ai-active")
				end)
			end)
		end)
	end)
end

Tick = function()
	if Greece.HasNoRequiredUnits() and GoodGuy.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(KillAll)

		if HoldObjective then
			USSR.MarkCompletedObjective(HoldObjective)
		end
	end

	if USSR.HasNoRequiredUnits() then
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

	if not BaseEstablished and CheckForBase() then
		BaseEstablished = true
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
		if Difficulty == "easy" then
			Trigger.AfterDelay(DateTime.Minutes(6), ReinfArmor)
		elseif Difficulty == "normal" then
			Trigger.AfterDelay(DateTime.Minutes(4), ReinfArmor)
		else
			Trigger.AfterDelay(DateTime.Minutes(3), ReinfArmor)
		end
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	GoodGuy = Player.GetPlayer("GoodGuy")
	Greece = Player.GetPlayer("Greece")

	InitObjectives(USSR)

	CaptureObjective = AddPrimaryObjective(USSR, "capture-radar-dome")
	KillAll = AddPrimaryObjective(USSR, "defeat-allied-forces")
	BeatUSSR = AddPrimaryObjective(Greece, "")

	RunInitialActivities()

	Trigger.OnDamaged(mcvGG, Expand)
	Trigger.OnDamaged(mcvtransport, Expand)

	Trigger.OnKilled(RadarDome, function()
		if not USSR.IsObjectiveCompleted(CaptureObjective) then
			USSR.MarkFailedObjective(CaptureObjective)
		end

		if HoldObjective then
			USSR.MarkFailedObjective(HoldObjective)
		end
	end)

	RadarDome.GrantCondition("french")
	Trigger.OnCapture(RadarDome, function()
		if USSR.IsObjectiveCompleted(KillAll) then
			USSR.MarkCompletedObjective(CaptureObjective)
			return
		end

		HoldObjective = AddPrimaryObjective(USSR, "defend-radar-dome")
		USSR.MarkCompletedObjective(CaptureObjective)
		Beacon.New(USSR, MCVDeploy.CenterPosition)
		if Difficulty == "easy" then
			Actor.Create("camera", true, { Owner = USSR, Location = MCVDeploy.Location })
			Media.DisplayMessage(UserInterface.Translate("allied-expansion-movement-detected"))
		else
			Actor.Create("MCV.CAM", true, { Owner = USSR, Location = MCVDeploy.Location })
			Media.DisplayMessage(UserInterface.Translate("coordinates-allied-expansion-discovered"))
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
				USSR.MarkFailedObjective(HoldObjective)
			end)
		end)
	end)

	Trigger.OnEnteredProximityTrigger(USSRExpansionPoint.CenterPosition, WDist.New(4 * 1024), function(unit, id)
		if unit.Owner == USSR and RadarDome.Owner == USSR then
			Trigger.RemoveProximityTrigger(id)

			Para2()

			local units = Reinforcements.ReinforceWithTransport(USSR, "lst", SovietMCVReinf, { ReinfSouthPoint.Location, USSRlstPoint.Location }, { ReinfSouthPoint.Location })[2]
			Utils.Do(units, function(unit)
				Trigger.OnAddedToWorld(unit, function()
					if unit.Type == "mcv" then
						unit.Move(USSRExpansionPoint.Location)
					else
						unit.AttackMove(USSRExpansionPoint.Location)
					end
				end)
			end)

			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		end
	end)

	Camera.Position = StartCamPoint.CenterPosition
end
