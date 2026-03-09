--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenMainBase = { HConyard, HWindTrap1, HWindTrap2, HWindTrap3, HWindTrap4, HWindTrap5, HWindTrap6, HWindTrap7, HWindTrap8, HWindTrap9, HRefinery1, HRefinery2, HBarracks1, HBarracks2, HLightFactory1, HHeavyFactory1, HStarport, HHighTech,  HRepairPad,  HOutpost1, HResearch, HPalace, HSilo1, HSilo2, HSilo3, HSilo4, HSilo5, HSilo6, HSilo7, HSilo8, HSilo9, HSilo10, HSilo11, HSilo12, HSilo13, HSilo14, HSilo15,  HTurret1, HTurret2, HTurret3, HTurret4, HTurret5,HTurret6, HTurret7, HTurret8, HWindTrap22, HWindTrap23 }

HarkonnenSmallBase = { HWindTrap10, HWindTrap11, HWindTrap12, HWindTrap13, HWindTrap14, HWindTrap15, HRefinery3, HBarracks3, HLightFactory2, HHeavyFactory2, HSilo16, HSilo17, HSilo18, HSilo19, HSilo20, HSilo21, HSilo22, HSilo23, HTurret9, HTurret10, HTurret11, HTurret12 }

HarkonnenSmall2Base = { HWindTrap16, HWindTrap17, HWindTrap18, HWindTrap19, HWindTrap20, HWindTrap21, HRefinery4, HBarracks4, HLightFactory3, HTurret13, HTurret14, HTurret15, HTurret16, HTurret17, HTurret18, HSilo24, HSilo25, HSilo26, HSilo27, HSilo28, HSilo29 }

CorrinoBase = { CConyard, CWindTrap1, CWindTrap2, CWindTrap3, CWindTrap4, CWindTrap5, CWindTrap6, CWindTrap7, CWindTrap8, CWindTrap9, CWindTrap10, CWindTrap11, CRefinery, CBarracks1, CBarracks2, CBarracks3, CLightFactory, CHeavyFactory, CStarport, CHighTech, CRepairPad,  CResearch, CPalace, CTurret1, CTurret2, CTurret3, CTurret4, CTurret5, CTurret6, CTurret7, CTurret8, CTurret9, CSilo1, CSilo2, CSilo3, CSilo4, CSilo5, CSilo6, CSilo7, CSilo8, CSilo9 }


EnemyReinforcementsInterval =
{
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

EnemysAttackWavesTotalCount =
{
	easy = 8,
	normal = 9,
	hard = 10
}

EnemyInitialUnitSpawn =
{
	{ "trooper", "trooper", "quad", "quad", "combat_tank_h", "combat_tank_h",  "light_inf" },
	{ "quad", "quad","quad", "quad", "light_inf", "light_inf", "trooper" },
	{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "trooper", "trooper" },
	{ "trike", "trike", "quad", "quad", "trooper", "trooper" },
	{ "quad", "quad", "quad", "quad", "trooper", "trooper", "light_inf", "light_inf" }
}

InitialUnitSpawnPaths =
{
	{ CPos.New(70, 2), UnitSpawn1.Location },
	{ CPos.New(2, 77), UnitSpawn2.Location },
	{ CPos.New(17, 2), UnitSpawn3.Location },
	{ CPos.New(21, 2), UnitSpawn4.Location },
	{ CPos.New(101, 76), UnitSpawn5.Location }
}

HarkonnenReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint1.Location), HarkonnenReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint2.Location), HarkonnenReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint3.Location), HarkonnenReinforcementsPoint3.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint4.Location), HarkonnenReinforcementsPoint4.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint5.Location), HarkonnenReinforcementsPoint5.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint6.Location), HarkonnenReinforcementsPoint6.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint7.Location), HarkonnenReinforcementsPoint7.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint8.Location), HarkonnenReinforcementsPoint8.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint9.Location), HarkonnenReinforcementsPoint9.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint10.Location), HarkonnenReinforcementsPoint10.Location },
}

CorrinoReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(CorrinoReinforcementsPoint1.Location), CorrinoReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(CorrinoReinforcementsPoint2.Location), CorrinoReinforcementsPoint2.Location }
}

HarkonnenReinforcements =
{
	easy = {
		{ "combat_tank_h", "missile_tank", "trike"},
		{ "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "missile_tank" },
		{ "trike", "trike" },
		{ "combat_tank_h", "missile_tank" },
		{ "combat_tank_h", "missile_tank", "trooper", "trooper" },
		{ "combat_tank_h", "missile_tank", "trike" }
	},
	normal = {
		{ "combat_tank_h", "missile_tank", "trike", "trike"},
		{ "trooper", "trooper", "trooper", "trooper" , "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" , "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "missile_tank" , "missile_tank" },
		{ "trike", "trike" },
		{ "combat_tank_h", "missile_tank", "missile_tank" },
		{ "combat_tank_h", "combat_tank_h", "missile_tank", "trooper", "trooper" },
		{ "combat_tank_h", "missile_tank", "trike" , "trike" }
	},
	hard = {
		{ "combat_tank_h", "missile_tank", "trike", "combat_tank_h", "missile_tank"},
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf"  },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "missile_tank", "trooper", "missile_tank" },
		{ "trike", "trike", "trike", "trike"  },
		{ "combat_tank_h", "missile_tank", "combat_tank_h", "missile_tank" },
		{ "combat_tank_h", "missile_tank", "trooper", "trooper", "trooper", "trooper"  },
		{ "combat_tank_h", "missile_tank", "trike", "missile_tank", "trike" }
	},
}

CorrinoReinforcements =
{
	easy = {
		{ "sardaukar", "sardaukar", "sardaukar"},
		{ "sardaukar", "sardaukar", "quad" },
	},
	normal = {
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar"},
		{ "sardaukar", "sardaukar", "quad" , "quad" },
	},
	hard = {
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "quad", "quad", "quad", "quad" },
	},
}

EmergencyBehaviour = function(player, target)
	HoldProduction[player] = false
	Attacking[player] = false

	if Difficulty == "hard" then
		player.Cash = player.Cash + 2000
	end

	if player == HarkonnenSmall and #IdlingUnits[Corrino] > 10 then
		local reinforcements = SetupAttackGroup(Corrino, Utils.RandomInteger(10, #IdlingUnits[Corrino]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				if unit.IsDead then
					return
				end
				unit.Stop()
				unit.AttackMove(HarkonnenReinforcementsPoint1.Location, 1)
				unit.AttackMove(HarkonnenReinforcementsPoint2.Location, 1)
				unit.AttackMove(UnitSpawn5.Location, 1)
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end

	if player == Harkonnen or player == HarkonnenSmall or player == HarkonnenSmall2 and DateTime.GameTime >= NukeTimer then
		local enemyunits = Map.ActorsInCircle(Map.CenterOfCell(target), WDist.FromCells(15), function(a)
			return a.Owner == Atreides
				and not a.IsDead
				and a.HasProperty("Location")
		end)
		ActivateNuke(HPalace, enemyunits)
	end

	if player == HarkonnenSmall2 and #IdlingUnits[Harkonnen] > 10 then
		local reinforcements = SetupAttackGroup(Harkonnen, Utils.RandomInteger(10, #IdlingUnits[Harkonnen]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				if unit.IsDead then
					return
				end
				unit.Stop()
				unit.AttackMove(HarkonnenPatrolPoint2.Location, 1)
				unit.AttackMove(UnitSpawn2.Location, 1)
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end
end

ReleaseSardaukars = true

Trigger.OnDamaged(CPalace, function(self)
	if self.Health < self.MaxHealth * 0.8 and ReleaseSardaukars then
		local index  = 0
		while index < 100 do
			index = index + 5
			Trigger.AfterDelay(index,function()
				if self.IsDead then return end
				local actor = Actor.Create("sardaukar", true, { Owner = Corrino, Location = CPalace.Location + CVec.New(1,2) })
				actor.Move(CPos.New(73,21), 1)
				IdlingUnits[Corrino][#IdlingUnits[Corrino] + 1] = actor
			end)
		end

		ReleaseSardaukars = false
	end
end)

VehicleTypes = { "trike", "raider" , "quad", "combat_tank_o", "missile_tank", "siege_tank", "deviator" }
NukeTimer = 10000
NukeChargeTime = 8000 * DifficultyModifier[Difficulty]

DeathHandLogic = function(nukeProvider)
	if nukeProvider.IsDead then
		return
	end

	if DateTime.GameTime <= NukeTimer then
		Trigger.AfterDelay(NukeTimer - DateTime.GameTime + 1, function()
			DeathHandLogic(nukeProvider)
		end)
		return
	end

	if Utils.RandomInteger(1, 100) < 60 * DifficultyModifier[Difficulty] then
		Trigger.AfterDelay(1000, function() DeathHandLogic(nukeProvider)end)
		return
	else
		if Utils.RandomInteger(1,100) < 50 then
			-- use Nuke Vs buildings
			local targets = Utils.Where(Atreides.GetActors(), function(actor)
				return actor.HasProperty("Sell") and
					actor.Type ~= "wall" and
					actor.Type ~= "construction_yard" and
					actor.Type ~= "silo"
			end)
			if #targets > 0  then
				ActivateNuke(nukeProvider, targets)
			end
		else
			-- use nuke vs units
			local targets = Utils.Where(Atreides.GetActorsByTypes(VehicleTypes), function(a)
				return not a.IsDead and a.IsInWorld
			end)
			if #targets > 0  then
				ActivateNuke(nukeProvider, targets)
			end
		end
		Trigger.AfterDelay(NukeChargeTime, function() DeathHandLogic(nukeProvider) end)
	end
end

AllPossibleTargets = { "light_inf", "trooper", "trike", "raider" , "quad", "combat_tank_o", "missile_tank", "siege_tank", "deviator", "wind_trap", "barracks", "refinery", "heavy_factory", "light_factory", "repair_pad", "outpost", "research_centre", "palace" , "silo", "medium_gun_turret", "large_gun_turret" }

ActivateNuke = function(nukeProvider, possibleTargets)
	if nukeProvider.IsDead then return end
	local bestValue = {}
	local bestIndex = 1

	for i = 1, #possibleTargets, 1 do
		local ActorsInCircle = Map.ActorsInCircle(possibleTargets[i].CenterPosition, WDist.FromCells(5), function(a)
			return a.Owner == Atreides
				and not a.IsDead
				and  Utils.Any(AllPossibleTargets, function(target) return a.Type == target end)
		end)

		bestValue[i] = 0
		Utils.Do(ActorsInCircle, function(a)
			if a.Type == "refinery" then
				bestValue[i] = bestValue[i] + 500
			else
				bestValue[i] = bestValue[i] + Actor.Cost(a.Type)
			end
		end)

		if bestValue[i] > bestValue[bestIndex] then
			bestIndex = i
		end
	end

	if bestValue[bestIndex] == 0 then
		return
	end
	Media.PlaySpeechNotification(Atreides, "MissileLaunchDetected")
	nukeProvider.ActivateNukePower(possibleTargets[bestIndex].Location)
	NukeTimer =  DateTime.GameTime + NukeChargeTime
end

Tick = function()

	if Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides1)
		HarkonnenSmall.MarkCompletedObjective(KillAtreides2)
		HarkonnenSmall2.MarkCompletedObjective(KillAtreides4)
		Corrino.MarkCompletedObjective(KillAtreides3)
	end

	if Harkonnen.HasNoRequiredUnits() and HarkonnenSmall.HasNoRequiredUnits() and HarkonnenSmall2.HasNoRequiredUnits() and not HarkonnenKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-annihilated"), Mentat)
		HarkonnenKilled = true
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if Corrino.HasNoRequiredUnits() and not Atreides.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage(UserInterface.GetFluentMessage("emperor-annihilated"), Mentat)
		Atreides.MarkCompletedObjective(KillCorrino)
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
	HarkonnenSmall2 = Player.GetPlayer("HarkonnenSmall2")
	Corrino = Player.GetPlayer("Corrino")
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen.Cash = 7000
	HarkonnenSmall.Cash = 7000
	HarkonnenSmall2.Cash = 7000
	Corrino.Cash = 7000

	InitObjectives(Atreides)
	KillHarkonnen = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-harkonnen"))
	KillCorrino = AddPrimaryObjective(Atreides, "destroy-imperial-forces")

	KillAtreides1 = AddPrimaryObjective(Harkonnen, "")
	KillAtreides2 = AddPrimaryObjective(HarkonnenSmall, "")
	KillAtreides3 = AddPrimaryObjective(Corrino, "")
	KillAtreides4 = AddPrimaryObjective(HarkonnenSmall2, "")

	Camera.Position = AMCV.CenterPosition
	AttackLocation = AMCV.Location

	Trigger.OnAllKilledOrCaptured(HarkonnenMainBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)


	Trigger.OnAllKilledOrCaptured(HarkonnenSmallBase, function()
		Utils.Do(HarkonnenSmall.GetGroundAttackers(), IdleHunt)
	end)
	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(Corrino.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenReinforcementsPaths) end
	local waveCondition = function() return Atreides.IsObjectiveCompleted(KillHarkonnen) end
	Trigger.AfterDelay(3500, function()
		SendCarryallReinforcements(Harkonnen, 0, EnemysAttackWavesTotalCount[Difficulty], EnemyReinforcementsInterval[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition)
	end)

	Trigger.AfterDelay(20500, function()
		local units = Reinforcements.ReinforceWithTransport(Corrino, "carryall.reinforce", CorrinoReinforcements[Difficulty][1], CorrinoReinforcementsPaths[1], {CorrinoReinforcementsPaths[1][1]})
		Utils.Do(units[2], function(unit)
			unit.AttackMove(AttackLocation)
			IdleHunt(unit)
		end)
	end)
	Trigger.AfterDelay(28500, function()
		local units = Reinforcements.ReinforceWithTransport(Corrino, "carryall.reinforce", CorrinoReinforcements[Difficulty][2], CorrinoReinforcementsPaths[2], {CorrinoReinforcementsPaths[2][1]})
		Utils.Do(units[2], function(unit)
			unit.AttackMove(AttackLocation)
			IdleHunt(unit)
		end)
	end)

	DeathHandLogic(HPalace)
	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = HarkonnenSmall })
	Actor.Create("upgrade.heavy", true, { Owner = HarkonnenSmall })
	Actor.Create("upgrade.barracks", true, { Owner = HarkonnenSmall })
	Actor.Create("upgrade.light", true, { Owner = HarkonnenSmall2 })
	Actor.Create("upgrade.barracks", true, { Owner = HarkonnenSmall2 })
	Actor.Create("upgrade.light", true, { Owner = Corrino })
	Actor.Create("upgrade.heavy", true, { Owner = Corrino })
	Actor.Create("upgrade.barracks", true, { Owner = Corrino })
	Trigger.AfterDelay(0, ActivateAI)


end
