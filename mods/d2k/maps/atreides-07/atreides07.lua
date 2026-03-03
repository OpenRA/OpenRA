--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenMainBase = { HConyard, HWindTrap1, HWindTrap2, HWindTrap3, HWindTrap4, HWindTrap5, HWindTrap6, HWindTrap7, HWindTrap8, HWindTrap9, HWindTrap10, HWindTrap11, HRefinery1, HRefinery2, HBarracks1, HLightFactory1, HHeavyFactory, HHighTech,  HRepairPad,  HOutpost1, HResearch, HTurret1, HTurret2, HTurret3, HTurret4 }

HarkonnenSmallBase = { HWindTrap12, HWindTrap13, HWindTrap14, HWindTrap15, HWindTrap16, HWindTrap17, HWindTrap18, HRefinery3, HBarracks2, HOutpost2, HTurret5, HTurret6, HTurret7, HTurret8 }

HarkonnenSmallBase2 = { HWindTrap19, HWindTrap20, HTurret9, HTurret10 }

EnemyReinforcementsInterval =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

EnemyInitialUnitSpawn =
{
	{ "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h", "combat_tank_h" },
	{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
	{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" }
}

InitialUnitSpawnPaths =
{
	{ CPos.New(6,2), UnitSpawn1.Location },
	{ CPos.New(42,2), UnitSpawn2.Location },
	{ CPos.New(81, 29), UnitSpawn3.Location }
}

EnemyReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(EnemyReinforcementsPoint1.Location), EnemyReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementsPoint2.Location), EnemyReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementsPoint3.Location), EnemyReinforcementsPoint3.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementsPoint4.Location), EnemyReinforcementsPoint4.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementsPoint5.Location), EnemyReinforcementsPoint5.Location }
}

AtreidesReinforcements =
{
	easy = { "combat_tank_a", "combat_tank_a", "siege_tank" },
	normal = { "combat_tank_a", "siege_tank" },
	hard = { "combat_tank_a", "siege_tank"}
}

EnemyReinforcements =
{
	easy = {
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "light_inf", "light_inf" },
		{ "sardaukar", "sardaukar", "sardaukar", "trike" },
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "light_inf", "light_inf" },
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "light_inf", "light_inf" },
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank", "trike", "trike", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "missile_tank"}
		},
	normal = {
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank", "light_inf", "light_inf", "light_inf" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "trike", "trike" },
		{ "combat_tank_h", "combat_tank_h", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "siege_tank", "missile_tank", "missile_tank", "trike", "trike", "quad", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "missile_tank"}
	},
	hard = {
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "siege_tank", "missile_tank","missile_tank", "light_inf", "light_inf", "light_inf" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar",  "sardaukar", "trike", "trike" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "light_inf", "sardaukar", "sardaukar", "light_inf", "light_inf" },
		{ "combat_tank_h", "combat_tank_h", "siege_tank", "siege_tank", "missile_tank", "missile_tank", "trike", "trike", "quad", "quad", "quad", "combat_tank_h" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "missile_tank", "missile_tank" }
	},
}

AtreidesReinforcementsPatch = { Map.ClosestEdgeCell(AtreidesReinforcementsPoint.Location), AtreidesReinforcementsPoint.Location }

EmergencyBehaviour = function(player,target)
	HoldProduction[player] = false
	Attacking[player] = false
	if Difficulty == "hard" then
		player.Cash = player.Cash + 2000
	end
	if player == HarkonnenSmall and #IdlingUnits[Harkonnen] > 10 then
		local reinforcements = SetupAttackGroup(Harkonnen, Utils.RandomInteger(10, #IdlingUnits[Harkonnen]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				if unit.IsDead then
					return
				end
				unit.Stop()
				unit.Move(HPatrolPoint1.Location,1)
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end
end

Tick = function()

	if Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides1)
		HarkonnenSmall.MarkCompletedObjective(KillAtreides2)
	end

	if Harkonnen.HasNoRequiredUnits() and HarkonnenSmall.HasNoRequiredUnits() and not HarkonnenKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-annihilated"), Mentat)
		HarkonnenKilled = true
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end


	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Harkonnen] then
		local units = Harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end


end

WorldLoaded = function()

	Harkonnen = Player.GetPlayer("Harkonnen")
	HarkonnenSmall = Player.GetPlayer("HarkonnenSmall")
	Corrino = Player.GetPlayer("Corrino")
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen.Cash = 10000
	HarkonnenSmall.Cash = 15000
	InitObjectives(Atreides)
	KillHarkonnen = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-harkonnen"))

	KillAtreides1 = AddPrimaryObjective(Harkonnen, "")
	KillAtreides2 = AddPrimaryObjective(HarkonnenSmall, "")

	Camera.Position = AMCV.CenterPosition
	AttackLocation = AMCV.Location

	Trigger.OnAllKilledOrCaptured(HarkonnenMainBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenSmallBase, function()
		Utils.Do(HarkonnenSmall.GetGroundAttackers(), IdleHunt)
	end)
	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(TileReveal2.Location), WDist.FromCells(4), function(actor, id)
		if actor.Owner == Atreides then
			local units  = Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", EnemyReinforcements[Difficulty][5],EnemyReinforcementsPaths[5], {EnemyReinforcementsPaths[5][1]})
			Utils.Do(units[2], function(unit)
				unit.AttackMove(TileReveal2.Location)
				IdleHunt(unit)
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.OnEnteredProximityTrigger(Map.CenterOfCell(TileReveal1.Location), WDist.FromCells(4), function(actor, id)
		if actor.Owner == Atreides then
			if #IdlingUnits[HarkonnenSmall] > 10 then
				local reinforcements = SetupAttackGroup(Harkonnen, Utils.RandomInteger(10, #IdlingUnits[Harkonnen]))
				Utils.Do(reinforcements, function(unit)
					Trigger.ClearAll(unit)
					Trigger.AfterDelay(1, function()
						if unit.IsDead then
							return
						end
						unit.Stop()
						unit.AttackMove(TileReveal1.Location)
						unit.CallFunc(function()
							FindTargetsInArea(HarkonnenSmall, unit)
						end)
					end)
				end)
			end
			Trigger.RemoveProximityTrigger(id)
		end
	end)

	Trigger.AfterDelay(10000, function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("its-a-trap"))
		for i = 1,  3 do
			local units  = Reinforcements.ReinforceWithTransport(Corrino, "carryall.reinforce", EnemyReinforcements[Difficulty][i],EnemyReinforcementsPaths[i], {EnemyReinforcementsPaths[i][1]})
			Utils.Do(units[2], function(unit)
				unit.AttackMove(AttackLocation)
				IdleHunt(unit)
			end)
		end
	end)
	Trigger.AfterDelay(8000, function()
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", AtreidesReinforcements[Difficulty], AtreidesReinforcementsPatch, {AtreidesReinforcementsPatch[1]})
	end)

	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = HarkonnenSmall })
	Trigger.AfterDelay(0, ActivateAI)

end
