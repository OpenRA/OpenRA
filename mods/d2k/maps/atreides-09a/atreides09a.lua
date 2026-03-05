--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenMainBase = { HConyard, HWindTrap1, HWindTrap2, HWindTrap3, HWindTrap4, HWindTrap5, HWindTrap6, HWindTrap7, HWindTrap8, HWindTrap9, HWindTrap10, HWindTrap11, HWindTrap12, HWindTrap13, HWindTrap14, HWindTrap15, HWindTrap16, HWindTrap17, HWindTrap18, HWindTrap19, HRefinery1, HRefinery2, HBarracks1, HLightFactory1, HHeavyFactory1, HHighTech,  HRepairPad,  HOutpost1, HResearch, HPalace, HSilo1, HSilo2, HSilo3, HSilo4, HSilo5, HSilo6, HSilo7, HSilo8, HSilo9, HSilo10, HSilo11, HSilo12, HTurret1, HTurret2, HTurret3, HTurret4, HTurret5,HTurret6, HTurret7, HTurret8, HTurret9, HTurret10, HTurret11, HTurret12, HTurret13, HTurret14, HTurret15, HTurret16, HTurret17 }

HarkonnenSmallBase = { HWindTrap20, HWindTrap21, HWindTrap22, HWindTrap23, HWindTrap24, HWindTrap25, HWindTrap26, HWindTrap27, HWindTrap28, HRefinery3, HBarracks2, HLightFactory2, HHeavyFactory2, HSilo13, HSilo14, HSilo15, HSilo16, HSilo17, HTurret18, HTurret19, HTurret20, HTurret21 }

CorrinoBase = { CConyard, CWindTrap1, CWindTrap2, CWindTrap3, CWindTrap4, CWindTrap5, CWindTrap6, CWindTrap7, CWindTrap8, CRefinery, CBarracks1, CBarracks2, CBarracks3, CLightFactory, CHeavyFactory, CStarport, CPalace, CTurret1, CTurret2, CTurret3, CTurret4, CTurret5, CTurret6 }


EnemyReinforcementsInterval =
{
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

EnemysAttackWavesTotalCount =
{
	easy = 6,
	normal = 7,
	hard = 8
}

EnemyInitialUnitSpawn =
{
	{ "trooper", "trooper", "quad", "quad", "combat_tank_h", "combat_tank_h", "combat_tank_h", "light_inf" },
	{ "quad", "quad","quad", "quad", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
	{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "trooper", "trooper" },
	{ "trike", "trike", "quad", "quad", "trooper", "trooper" }
}

InitialUnitSpawnPaths =
{
	{ CPos.New(72,2), UnitSpawn1.Location },
	{ CPos.New(2,30), UnitSpawn2.Location },
	{ CPos.New(99, 38), UnitSpawn3.Location },
	{ CPos.New(99, 36), UnitSpawn4.Location }
}

HarkonnenReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint1.Location), HarkonnenReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint2.Location), HarkonnenReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint3.Location), HarkonnenReinforcementsPoint3.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint4.Location), HarkonnenReinforcementsPoint4.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint5.Location), HarkonnenReinforcementsPoint5.Location },
	{ Map.ClosestEdgeCell(HarkonnenReinforcementsPoint6.Location), HarkonnenReinforcementsPoint6.Location }
}

CorrinoReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(CorrinoReinforcementsPoint1.Location), CorrinoReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(CorrinoReinforcementsPoint2.Location), CorrinoReinforcementsPoint2.Location }
}

HarkonnenReinforcements =
{
	easy = {
		{ "combat_tank_h", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad" },
		{ "trooper", "trooper", "trooper", "light_inf" },
		{ "trooper", "trooper", "trooper", "light_inf" },
		{ "trike", "trooper", "trooper", },
		{ "combat_tank_h", "light_inf" },
		{ "trike", "trooper", "trooper", },
		{ "combat_tank_h", "light_inf" }
	},
	normal = {
		{ "combat_tank_h", "combat_tank_h", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad", "quad" },
		{ "missile_tank", "trooper", "trooper", "trooper", "light_inf" },
		{ "missile_tank", "trooper", "trooper", "trooper", "light_inf" },
		{ "trike", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "combat_tank_h" },
		{ "trike", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "combat_tank_h" }
	},
	hard = {
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad", "quad" },
		{ "missile_tank", "trooper", "trooper", "trooper", "light_inf", "trooper", "light_inf" },
		{ "missile_tank", "missile_tank", "trooper", "trooper", "trooper", "light_inf" },
		{ "trike", "trike", "trike", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h" },
		{ "trike", "trike", "trike", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h" }
	},
}

CorrinoReinforcements =
{
	easy = {
		{ "sardaukar", "sardaukar", "sardaukar"},
		{ "trike", "trike" },
	},
	normal = {
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar"},
		{ "trike", "trike", "missile_tank" },
	},
	hard = {
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "trike", "trike", "missile_tank", "missile_tank" },
	},
}

EmergencyBehaviour = function(player, target)
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
				unit.Move(EnemyReinforcementsPoint1.Location, 1)
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end

	if player == Harkonnen and DateTime.GameTime >= NukeTimer then
		local enemyunits = Map.ActorsInCircle(Map.CenterOfCell(target), WDist.FromCells(15), function(a)
			return a.Owner == Atreides
				and not a.IsDead
				and a.HasProperty("Attack")
		end)
		ActivateNuke(HPalace, enemyunits)
	end

	if player == Harkonnen and #IdlingUnits[Corrino] > 10 then
		local reinforcements = SetupAttackGroup(Corrino, Utils.RandomInteger(10, #IdlingUnits[Corrino]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				if unit.IsDead then
					return
				end
				unit.Stop()
				unit.AttackMove(UnitSpawn4.Location, 1)
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
				actor.Move(CPos.New(98,6))
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
			bestValue[i] = bestValue[i] + Actor.Cost(a.Type)
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
		Corrino.MarkCompletedObjective(KillAtreides3)
	end

	if Harkonnen.HasNoRequiredUnits() and HarkonnenSmall.HasNoRequiredUnits() and not HarkonnenKilled then
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
	Corrino = Player.GetPlayer("Corrino")
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen.Cash = 10000
	HarkonnenSmall.Cash = 5000
	Corrino.Cash = 10000

	InitObjectives(Atreides)
	KillHarkonnen = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-harkonnen"))
	KillCorrino = AddPrimaryObjective(Atreides, "destroy-imperial-forces")

	KillAtreides1 = AddPrimaryObjective(Harkonnen, "")
	KillAtreides2 = AddPrimaryObjective(HarkonnenSmall, "")
	KillAtreides3 = AddPrimaryObjective(Corrino, "")

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
	local huntFunction = function(unit)
		unit.AttackMove(AMCV.Location)
		IdleHunt(unit)
	end
	Trigger.AfterDelay(2000, function()
		SendCarryallReinforcements(Harkonnen, 0, EnemysAttackWavesTotalCount[Difficulty], EnemyReinforcementsInterval[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition, huntFunction)
	end)

	Trigger.AfterDelay(22000, function()
		local units = Reinforcements.ReinforceWithTransport(Corrino, "carryall.reinforce", CorrinoReinforcements[Difficulty][1], CorrinoReinforcementsPaths[1], {CorrinoReinforcementsPaths[1][1]})
		Utils.Do(units[2], function(unit)
			unit.AttackMove(AttackLocation)
			IdleHunt(unit)
		end)
	end)
	Trigger.AfterDelay(26500, function()
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
	Actor.Create("upgrade.light", true, { Owner = Corrino })
	Actor.Create("upgrade.heavy", true, { Owner = Corrino })
	Actor.Create("upgrade.barracks", true, { Owner = Corrino })
	Trigger.AfterDelay(0, ActivateAI)


end
