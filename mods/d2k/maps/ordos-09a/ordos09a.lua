--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { AConyard, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9, ARefinery1, ARefinery2, ABarracks, ALightFactory, AHeavyFactory, AHiTechFactory, AOutpost, AStarport, ARepairPad, AResearch, APalace, ATurret1, ATurret2, ATurret3, ATurret4, ATurret5, ATurret6, ATurret7, ATurret8, ATurret9 }

HarkonnenBase = { HPower1, HPower2, HPower3, HPower4, HPower5, HPower6, HPower7, HPower8, HPower9, HPower10, HPower11, HConyard, HRefinery1, HRefinery2, HBarracks1, HBarracks2, HLightFactory, HHeavyFactory, HStarport, HHighTech, HResearch, HOutpost, HReapairPad, HTurret1, HTurret2, HTurret3, HTurret4, HTurret5, HTurret6, HTurret7, HTurret8, HTurret9, HPalace }



CorrinoBase = { CPower1, CPower2, CPower3, CPower4, CPower5, CPower6, CPower7, CPower8, CBarracks1, CBarracks2, CStarport, CTurret1, CTurret2, CTurret3, CTurret4, CTurret5, COutpost, CResearch, CRefinery, CPalace }

MercenaryBase = { MPower1, MPower2, MPower3, MPower4, MPower5, MPower6, MPower7, MRefinery, MStarport, MBarracks, MHeavyFactory, MTurret1, MTurret2, MTurret3, MOutpost, MSilo1, MSilo2, MSilo3, MSilo4 }
MercenaryLeader = UserInterface.GetFluentMessage("mercenary-leader")

InitialReinforcementsSquads =
{
	{ "combat_tank_a", "combat_tank_a", "trike", "trike", "quad", "light_inf", "light_inf" },
	{ "combat_tank_h","combat_tank_h", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
	{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "light_inf", "light_inf" },
	{ "quad", "quad", "quad", "light_inf", "light_inf", "trooper", "trooper" },
	{ "trike", "trooper", "trooper", "trooper", "combat_tank_o", "combat_tank_o" }
}

InitialSpawnPaths =
{
	{CPos.New(45, 101), AtreidesUnitSpawn.Location},
	{CPos.New(2, 56), CorrinoUnitSpawn.Location},
	{CPos.New(2, 9), HarkonnenUnitSpawn.Location},
	{CPos.New(2, 22), HarkonnenUnitSpawn2.Location},
	{CPos.New(111, 24), MercenaryUnitSpawn.Location},
}

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf" },
		{ "quad",  "combat_tank_a" },
		{ "combat_tank_a", "combat_tank_a" },
		{ "quad", "quad", "quad", "quad", "combat_tank_a", "combat_tank_a" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad", "combat_tank_a" },
		{ "combat_tank_a", "combat_tank_a", "missile_tank" },
		{ "quad", "quad", "quad", "quad", "combat_tank_a", "combat_tank_a", "combat_tank_a" }
	},
	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad", "combat_tank_a", "combat_tank_a" },
		{ "combat_tank_a", "combat_tank_a","combat_tank_a", "missile_tank" },
		{ "quad", "quad", "quad", "quad", "combat_tank_a", "combat_tank_a", "combat_tank_a", "combat_tank_a" }
	}
}

HarkonnenReinforcements =
{
	easy =
	{
		{ "trike", "trike", "trike", "trike" },
		{ "quad", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "quad" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h" },
		{ "quad", "quad", "quad", "quad", "quad" }
	},

	normal =
	{
		{ "trike", "trike", "trike", "trike", "trike" },
		{ "quad", "quad", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "quad", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h" },
		{ "quad", "quad", "quad", "quad", "quad", "quad" }
	},
	hard =
	{
		{ "trike", "trike", "trike", "trike", "trike", "trike", "trike" },
		{ "quad", "quad", "quad", "quad", "quad"  },
		{ "combat_tank_h", "combat_tank_h","combat_tank_h", "quad", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h"  },
		{ "quad", "quad", "quad", "quad", "quad", "quad", "quad", "quad" }
	}
}

CorrinoReinforcements =
{
	easy =
	{
		{ "sardaukar", "sardaukar"},
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" }
	},

	normal =
	{
		{ "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" }
	},
	hard =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" },
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar" }
	}
}

AtreidesReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(AReinforcementsPoint1.Location), AReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(AReinforcementsPoint2.Location), AReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(AReinforcementsPoint3.Location), AReinforcementsPoint3.Location },
	{ Map.ClosestEdgeCell(AReinforcementsPoint4.Location), AReinforcementsPoint4.Location }
}

HarkonnenReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(HReinforcementsPoint1.Location), HReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(HReinforcementsPoint2.Location), HReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(HReinforcementsPoint3.Location), HReinforcementsPoint3.Location },
	{ Map.ClosestEdgeCell(HReinforcementsPoint4.Location), HReinforcementsPoint4.Location },
	{ Map.ClosestEdgeCell(HReinforcementsPoint5.Location), HReinforcementsPoint5.Location }
}

CorrinoReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(CReinforcementsPoint1.Location), CReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(CReinforcementsPoint2.Location), CReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(CReinforcementsPoint3.Location), CReinforcementsPoint3.Location }
}



AtreidesAttackWaves =
{
	easy = 3,
	normal = 4,
	hard = 6
}

AtreidesReinforcementsInterval =
{
	easy = 4,
	normal = 5,
	hard = 5
}

MercenaryStarportDelivery =
{
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "siege_tank", "siege_tank" },
	{ "missile_tank", "missile_tank", "quad", "quad", "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
}

CorrinoStarportDelivery =
{
	{ "sardaukar", "sardaukar", "sardaukar","sardaukar", "sardaukar", "sardaukar" },
	{ "combat_tank_h", "siege_tank", "trike", "quad", "trike", "quad", "quad" }
}

SendDelivery = function(player, starport, timer, squad)
	Trigger.AfterDelay(timer, function()
		if starport.IsDead or starport.Owner ~= player then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(player, "frigate", squad, { Map.ClosestEdgeCell(starport.Location), starport.Location + CVec.New(1, 1) }, { Map.ClosestEdgeCell(starport.Location) })[2]
		Utils.Do(units, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				if unit.IsDead then return end
				IdlingUnits[player][#IdlingUnits[player] + 1] = unit
				SelectRoutine(player, unit)
			end)
		end)
		SendDelivery(player, starport, timer, squad)
		Trigger.AfterDelay(500, function()
			if #IdlingUnits[player] < AttackGroupSize[Difficulty] then return end
			if DateTime.GameTime > AttackDelay[player] then
				SendAttack(player, Utils.RandomInteger(AttackGroupSize[Difficulty], #IdlingUnits[player]))
				AttackDelay[player] = DateTime.GameTime + TimeBetweenAttacks[player]
			else
				SendPatrol(player, AttackGroupSize[Difficulty])
			end
		end)
	end)
end

AirStrikeTimer = 8000
AirStrikeChargeTime = 8000 * DifficultyModifier[Difficulty]

AirstrikeLogic = function(airstrikeProvider)
	if airstrikeProvider.IsDead or airstrikeProvider.Owner ~= Atreides then return end
	if DateTime.GameTime <= AirStrikeTimer then
		Trigger.AfterDelay(AirStrikeTimer - DateTime.GameTime + 1, function()
			AirstrikeLogic(airstrikeProvider)
		end)
		return
	end

	-- randomly choose if wait again or strike. During waiting Airstrike can still be used by DefensiveAirStrike
	if Utils.RandomInteger(1, 100) < 60 then
		Trigger.AfterDelay(1501, function() AirstrikeLogic(airstrikeProvider)end)
	else
		AirStrikeVSBuilding(airstrikeProvider)
		Trigger.AfterDelay(7500, function() AirstrikeLogic(airstrikeProvider) end)
	end
end

AirStrikeVSBuilding = function(airstrikeProvider)
	if airstrikeProvider.IsDead then return end

	local targets = Utils.Where(Ordos.GetActors(), function(actor)
		return actor.HasProperty("Sell") and
			actor.Type ~= "wall" and
			actor.Type ~= "silo"
	end)

	if #targets > 0 then
		airstrikeProvider.TargetAirstrike(Utils.Random(targets).CenterPosition)
		AirStrikeTimer =  DateTime.GameTime + AirStrikeChargeTime
	end
end

TargetsTypes = { "light_inf", "trooper", "trike", "raider" , "quad", "combat_tank_o", "missile_tank", "siege_tank", "deviator" }

DefensiveAirStrike = function(airstrikeProvider, possibleTargets)
	if airstrikeProvider.IsDead  or DateTime.GameTime <= AirStrikeTimer then return end
	local bestValue = {}
	local bestIndex = 1
	for i = 1, #possibleTargets, 1 do
		local actorsInCircle = Map.ActorsInCircle(possibleTargets[i].CenterPosition, WDist.FromCells(4), function(a)
			return
				not a.Owner.IsAlliedWith(airstrikeProvider.Owner)
				and not a.IsDead
				and Utils.Any(TargetsTypes, function(type) return a.Type == type end)
		end)

		bestValue[i] = 0
		Utils.Do(actorsInCircle, function(a)
			bestValue[i] = bestValue[i] + Actor.Cost(a.Type)
		end)

		if bestValue[i] > bestValue[bestIndex] then
			bestIndex = i
		end
	end

	airstrikeProvider.TargetAirstrike(possibleTargets[bestIndex].CenterPosition)
	AirStrikeTimer =  DateTime.GameTime + AirStrikeChargeTime
end

VehicleTypes = { "trike", "raider" , "quad", "combat_tank_o", "missile_tank", "siege_tank", "deviator" }
NukeTimer = 8000
NukeChargeTime = 9000 * DifficultyModifier[Difficulty]

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
		Trigger.AfterDelay(1300, function() DeathHandLogic(nukeProvider) end)
		return
	else
		if Utils.RandomInteger(1,100) < 50 then
			-- use Nuke Vs buildings
			local targets = Utils.Where(Ordos.GetActors(), function(actor)
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
			local targets = Utils.Where(Ordos.GetActorsByTypes(VehicleTypes), function(a)
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
	local bestValue = { }
	local bestIndex = 1
	for i = 1, #possibleTargets, 1 do
		local actorsInCircle = Map.ActorsInCircle(possibleTargets[i].CenterPosition, WDist.FromCells(5), function(a)
			return a.Owner == Ordos
				and not a.IsDead
				and  Utils.Any(AllPossibleTargets, function(target) return a.Type == target end)
		end)

		bestValue[i] = 0

		Utils.Do(actorsInCircle, function(a)
			bestValue[i] = bestValue[i] + Actor.Cost(a.Type)
		end)

		if bestValue[i] > bestValue[bestIndex] then
			bestIndex = i
		end
	end

	if bestValue[bestIndex] == 0 then
		return
	end

	if possibleTargets[bestIndex] == nil then return end
	nukeProvider.ActivateNukePower(possibleTargets[bestIndex].Location)
	Media.PlaySpeechNotification(Ordos, "MissileLaunchDetected")
	NukeTimer =  DateTime.GameTime + NukeChargeTime
end

FremenGroupSize =
{
	easy = 4,
	normal = 6,
	hard = 10
}

BuildFremen = function(fremenProvider)
	if fremenProvider.IsDead or fremenProvider.Owner ~= Atreides then
		return
	end

	fremenProvider.Produce("fremen")
	fremenProvider.Produce("fremen")
	fremenProvider.Produce("fremen")
	fremenProvider.Produce("fremen")
	fremenProvider.Produce("fremen")

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		IdleFremen = Utils.Where(Atreides.GetActorsByType('fremen'), function(actor) return actor.IsIdle end)

		if #IdleFremen >= FremenGroupSize[Difficulty] then
			SendFremen()
		end
	end)

	Trigger.AfterDelay(2500 ,function()
		BuildFremen(fremenProvider)
	end)
end

SendFremen = function()
	Utils.Do(IdleFremen, function(freman)
		freman.AttackMove(MercenaryUnitSpawn.Location)
		IdleHunt(freman)
	end)
end

EmergencyBehaviour = function(player,target)
	if player == Atreides then
		local airStrikeProvider = Atreides.GetActorsByType("high_tech_factory")
		if airStrikeProvider[1] == nil then return end

		local enemyunits = Map.ActorsInCircle(Map.CenterOfCell(target), WDist.FromCells(15), function(a)
			return not a.Owner.IsAlliedWith(Atreides)
				and not a.IsDead
				and a.HasProperty("Attack")
		end)

		if enemyunits[1] == nil  then return end
		DefensiveAirStrike(airStrikeProvider[1], enemyunits)
	end

	if player == Harkonnen and DateTime.GameTime >= NukeTimer then
		local nukeProvider = Harkonnen.GetActorsByType("palace")
		if nukeProvider[1] == nil then return end
		local enemyunits = Map.ActorsInCircle(Map.CenterOfCell(target), WDist.FromCells(15), function(a)
			return not a.Owner.IsAlliedWith(Harkonnen)
				and not a.IsDead
				and a.HasProperty("Location")
		end)
		ActivateNuke(nukeProvider[1], enemyunits)
	end
	if player == Corrino then
		player.Cash = player.Cash + 2000
	end

	if player == Mercenaries then
		player.Cash = player.Cash + 2000
	end
end

Tick = function()
	if Ordos.HasNoRequiredUnits() then
		Corrino.MarkCompletedObjective(KillOrdos1)
		Harkonnen.MarkCompletedObjective(KillOrdos2)
		Atreides.MarkCompletedObjective(KillOrdos3)
	end

	if Atreides.HasNoRequiredUnits() and not AtreidesKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("atreides-annihilated"), Mentat)
		AtreidesKilled = true
		Ordos.MarkCompletedObjective(KillAtreides)
	end

	if Harkonnen.HasNoRequiredUnits() and not HarkonnenKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-annihilated"), Mentat)
		HarkonnenKilled = true
		Ordos.MarkCompletedObjective(KillHarkonnen)
	end

	if Corrino.HasNoRequiredUnits() and not CorrinoKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("emperor-annihilated"), Mentat)
		CorrinoKilled = true
		Ordos.MarkCompletedObjective(KillCorrino)
	end

	if DateTime.GameTime % DateTime.Seconds(10) and not MercenariesAreNeutral then
		if Mercenaries.UnitsLost < 20 then return end
		local lostRatio = UnitLostRatio(Mercenaries, {Atreides, Harkonnen, Corrino}, 10 * DifficultyModifier[Difficulty])
		if lostRatio > 2 and not TakingLoses then
			Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-loses"), MercenaryLeader, Mercenaries.Color)
			TakingLoses = true
		end
		if lostRatio > 3 and not MercenariesAreNeutral then
			Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-become-neutral"), MercenaryLeader, Mercenaries.Color)
			ChangeOwner(Mercenaries, MercenariesNeutral)
			MercenariesAreNeutral = true
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Harkonnen] then
		local units = Corrino.GetActorsByType("harvester")
		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	Ordos = Player.GetPlayer("Ordos")
	Corrino = Player.GetPlayer("Corrino")
	Harkonnen = Player.GetPlayer("Harkonnen")
	Atreides = Player.GetPlayer("Atreides")
	Mercenaries = Player.GetPlayer("Mercenaries")
	MercenariesNeutral = Player.GetPlayer("MercenariesNeutral")
	Atreides.Cash = 5000
	Harkonnen.Cash = 5000
	Mercenaries.Cash = 5000
	Corrino.Cash = 6000
	InitObjectives(Atreides)

	KillAtreides = AddPrimaryObjective(Ordos, UserInterface.GetFluentMessage("destroy-atreides"))
	KillCorrino = AddPrimaryObjective(Ordos, UserInterface.GetFluentMessage("destroy-corrino"))
	KillHarkonnen = AddPrimaryObjective(Ordos, UserInterface.GetFluentMessage("destroy-harkonnen"))

	KillOrdos1 = AddPrimaryObjective(Corrino, "")
	KillOrdos2 = AddPrimaryObjective(Harkonnen, "")
	KillOrdos3 = AddPrimaryObjective(Atreides, "")

	Camera.Position = AttackLocation.CenterPosition

	Trigger.OnAllKilledOrCaptured(AtreidesBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)
	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), function(actor)
			Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
		end)
	end)

	local pathNumber = 0
	local pathAtreides = function()
		pathNumber = pathNumber + 1
		return AtreidesReinforcementsPaths[pathNumber]
	end

	local pathNumberCorrino = 0
	local pathCorrino = function()
		pathNumberCorrino = pathNumberCorrino + 1
		return CorrinoReinforcementsPaths[pathNumberCorrino]
	end

	local pathNumberHarkonnen = 0
	local pathHarkonnen = function()
		pathNumberHarkonnen = pathNumberHarkonnen + 1
		return HarkonnenReinforcementsPaths[pathNumberHarkonnen]
	end

	local waveCondition = function() return AtreidesKilled end
	local huntFunction = function(unit)
		unit.AttackMove(AttackLocation.Location)
		IdleHunt(unit)
	end

	Trigger.AfterDelay(2500, function()
		SendCarryallReinforcements(Atreides, 0, 2 , AtreidesReinforcementsInterval[Difficulty], pathAtreides, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)
	end)

	SendCarryallReinforcements(Harkonnen, 0, 2, 1250, pathHarkonnen, HarkonnenReinforcements[Difficulty], waveCondition, huntFunction)
	SendCarryallReinforcements(Harkonnen, 2, 5, 9042, pathHarkonnen, HarkonnenReinforcements[Difficulty], waveCondition, huntFunction)
	SendCarryallReinforcements(Atreides, 2, 4, 7000 * DifficultyModifier[Difficulty], pathAtreides, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)
	SendCarryallReinforcements(Corrino, 0, 1, 2500, pathCorrino, CorrinoReinforcements, waveCondition, huntFunction)
	Trigger.AfterDelay(60000 * DifficultyModifier[Difficulty], function()
		SendCarryallReinforcements(Corrino, 1, 3, 5, pathCorrino, {CorrinoReinforcements}, waveCondition, huntFunction)
	end)

	SendDelivery(Mercenaries, MStarport, 5021, MercenaryStarportDelivery[1])
	SendDelivery(Mercenaries, MStarport, 11573, MercenaryStarportDelivery[2])
	SendDelivery(Corrino, CStarport, 5021, CorrinoStarportDelivery[1])
	SendDelivery(Corrino, CStarport, 11573, CorrinoStarportDelivery[2])

	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = Atreides })
	Actor.Create("upgrade.light", true, { Owner = Atreides })
	Actor.Create("upgrade.heavy", true, { Owner = Atreides })
	Actor.Create("upgrade.barracks", true, { Owner = Mercenaries })
	Actor.Create("upgrade.heavy", true, { Owner = Mercenaries })
	Actor.Create("upgrade.barracks", true, { Owner = Corrino })
	Trigger.AfterDelay(0, ActivateAI)


	Trigger.AfterDelay(EarlyGameStage, function() BuildFremen(APalace) end)
	Trigger.AfterDelay(8000, function() DeathHandLogic(HPalace) end)
	Trigger.AfterDelay(7000, function() AirstrikeLogic(AHiTechFactory) end)

end

MercenariesBuildingLoses = 0
Utils.Do(MercenaryBase, function(building)
	Trigger.OnKilledOrCaptured(building, function()
		MercenariesBuildingLoses = MercenariesBuildingLoses + 1
		if MercenariesBuildingLoses >= #MercenaryBase * 0.5 and not MercenariesAreNeutral then
			Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-become-neutral"), MercenaryLeader, Mercenaries.Color)
		ChangeOwner(Mercenaries, MercenariesNeutral)
		MercenariesAreNeutral = true
		end
		if MercenariesBuildingLoses >= #MercenaryBase * 0.3 and not TakingLoses then
			Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-loses"), MercenaryLeader, Mercenaries.Color)
		TakingLoses = true
		end
	end)
end)

CorrinoBuildingLoses = 0
Utils.Do(CorrinoBase,function(building)
	Trigger.OnKilledOrCaptured(building, function()
		CorrinoBuildingLoses = CorrinoBuildingLoses + 1
		if CorrinoBuildingLoses >= #CorrinoBase * 0.5 and not ReleaseSardaukars then
			ReleaseSardaukars = true
			if CPalace.IsDead then return end
			Media.PlaySoundNotification(Ordos, "ReleaseSardaukars")
			local index  = 0
			while index < 100 do
				index = index + 5
				Trigger.AfterDelay(index,function()
					if CPalace.IsDead then return end
					VisionProvider.GrantCondition("activate", 500)
					local actor = Actor.Create("sardaukar", true, {Owner = Corrino, Location = CPalace.Location + CVec.New(1,2)})
					actor.Move(CPos.New(4,43))
					IdlingUnits[Corrino][#IdlingUnits[Corrino] + 1] = actor
				end)
			end
		end
	end)
end)

ChangeOwner = function(old_owner, new_owner)
	local units = old_owner.GetActors()
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			unit.Owner = new_owner
		end
	end)
	if Mercenaries.UnitsLost < 20 then return end
end

UnitLostRatio = function (player, otherPlayers, minThreshold)
	local enemyLoses = 0
	Utils.Do(otherPlayers, function(owner)
		enemyLoses = enemyLoses + owner.UnitsLost
	end)
	if enemyLoses < minThreshold then
		return -1
	else
		return player.UnitsLost / (enemyLoses * DifficultyModifier[Difficulty])
	end
end
