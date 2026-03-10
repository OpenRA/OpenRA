--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosBase = { OConyard, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10, OPower11, OPower12, OPower13, OPower14, OPower15, ORefinery1, ORefinery2, OBarracks1, OBarracks2, OLightFactory1,  OHeavyFactory, OBarracks2, OHighTech, ORepairPad, OResearch, OStarport, OOutpost, OPalace, OSilo1, OSilo2, OSilo3, OSilo4, OSilo5, OSilo6, OSilo7, OSilo8, OSilo9, OTurret1, OTurret2, OTurret3, OTurret4, OTurret5, OTurret6, OTurret7, OTurret8, OTurret9 }

OrdosSmallBase = { OPower16, OPower17, ORefinery3, OBarracks3, OLightFactory2, OTurret10, OTurret11, OTurret12 }

HarkonnenBase = { HConyard, HPower1, HPower2, HPower3, HPower4, HRefinery,  HBarracks, HLightFactory, HHeavyFactory, HOutpost, HSilo1, HSilo2, HSilo3, HTurret1, HTurret2, HTurret3 }

SmugglersBase = { SPower1, SPower2, SSilo1, SSilo2, SSilo3 }

SmugglersActors = { }

EnemyReinforcementsInterval =
{
	easy = DateTime.Minutes(3)+ DateTime.Seconds(30),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}



EnemysAttackWavesTotalCount =
{
	easy = 4,
	normal = 5,
	hard = 6
}

InitialUnitSpawnPaths =
{
	{ CPos.New(14,97), UnitSpawn1.Location },
	{ CPos.New(19,97), UnitSpawn2.Location },
	{ CPos.New(7, 2), UnitSpawn3.Location },
	{ CPos.New(97, 78), UnitSpawn4.Location },
	{ CPos.New(2, 32), UnitSpawn5.Location }
}

InitialUnitSpawn =
{
	{ "trooper", "trooper", "light_inf", "quad", "quad", "raider" },
	{ "combat_tank_o", "siege_tank", "trooper", "trooper", "quad" },
	{ "light_inf", "light_inf", "trooper", "trooper" },
	{ "combat_tank_h", "combat_tank_h", "trooper", "trooper" },
	{ "light_inf", "light_inf", "trooper", "trooper" }
}

OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper" },
		{ "trike", "trike" },
		{ "light_inf", "light_inf", "combat_tank_o" },
		{ "trooper","trooper", "trooper", "light_inf", "combat_tank_o", "missile_tank" },
		{ "missile_tank", "trooper", "light_inf" },
		{ "combat_tank_o", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "quad" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "combat_tank_o" },
		{ "trooper","trooper", "trooper", "trooper", "trooper", "combat_tank_o", "missile_tank" },
		{ "missile_tank", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "quad" }
	},
	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "combat_tank_o", "light_inf", "combat_tank_o" },
		{ "trooper","trooper", "trooper", "trooper", "trooper", "combat_tank_o", "missile_tank", "missile_tank" },
		{ "missile_tank","missile_tank", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "missile_tank", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "quad", "combat_tank_o", "quad" }
	}
}

InitialStarportSquad =
{
	easy = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
	normal = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" },
	hard = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" }
}

OrdosStarportReinforcements =
{
	easy = { "combat_tank_o", "combat_tank_o" },
	normal = { "missile_tank", "combat_tank_o", "combat_tank_o" },
	hard = { "missile_tank", "missile_tank", "combat_tank_o", "combat_tank_o", "combat_tank_o" }
}

OrdosReinforcementsDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

OrdosStarportDelay =
{
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5)
}


OrdosReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint1.Location), EnemyReinforcementPoint1.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint2.Location), EnemyReinforcementPoint2.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint3.Location), EnemyReinforcementPoint3.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint4.Location), EnemyReinforcementPoint4.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint5.Location), EnemyReinforcementPoint5.Location },
	{ Map.ClosestEdgeCell(EnemyReinforcementPoint6.Location), EnemyReinforcementPoint6.Location },
}

SaboteurPaths =
{
	{ HPatrolPoint1.Location, EnemyReinforcementPoint4.Location },
	{ OPatrolPoint3.Location, EnemyReinforcementPoint6.Location },
	{ OPatrolPoint4.Location, EnemyReinforcementPoint1.Location },
	{ OPatrolPoint2.Location, OPatrolPoint5.Location, EnemyReinforcementPoint2.Location}
}

GetSaboteurTargets = function(player)
	return Utils.Where(player.GetActors(), function(actor)
		return actor.HasProperty("Sell") and
			actor.Type ~= "wall" and
			actor.Type ~= "medium_gun_turret" and
			actor.Type ~= "large_gun_turret" and
			actor.Type ~= "silo"
	end)
end

BuildSaboteur = function()
	if OPalace.IsDead or OPalace.Owner ~= Ordos then
		return
	end

	local targets = GetSaboteurTargets(Atreides)
	if #targets > 0 then
		local saboteur = Actor.Create("saboteur", true, { Owner = Ordos, Location = OPalace.Location + CVec.New(0, 2) })
		saboteur.Move(saboteur.Location + CVec.New(0, 1))
		saboteur.Wait(DateTime.Seconds(5))

		local path = Utils.Random(SaboteurPaths)
		Utils.Do(path, function(waypoint)
			saboteur.Move(waypoint)
		end)

		SendSaboteur(saboteur)
		Trigger.AfterDelay(200, function()
			ScanForBetterTargets(saboteur)
		end)
	end

	Trigger.AfterDelay(2250, BuildSaboteur)
end

DemolishType = { "harvester", "mcv",  "siege_tank", "missile_tank", "sonic_tank", "devastator", "deviator", "combat_tank_a", "combat_tank_h", "combat_tank_o"}

ScanForBetterTargets = function(saboteur)
	if saboteur.IsDead or not saboteur.IsInWorld then return end

	local possibleTargets = Map.ActorsInCircle(saboteur.CenterPosition, WDist.FromCells(6), function(a)
		return not saboteur.Owner.IsAlliedWith(a.Owner) and
			Utils.Any(DemolishType, function(d) return d == a.Type end)
	end)

	if possibleTargets[1] == nil then
		Trigger.AfterDelay(100, function()
			ScanForBetterTargets(saboteur)
		end)
		return
	end

	-- filter out targets where infantry is nearby
	for index = #possibleTargets, 1, -1 do
		local infantryunits = Map.ActorsInCircle(possibleTargets[index].CenterPosition, WDist.New(2024), function(u) return u.Type == "light_inf" or u.Type == "trooper" end)
		if infantryunits[1] ~= nil then
			table.remove(possibleTargets, index)
		end
	end

	if possibleTargets[1] ~= nil then
		saboteur.Stop()
		local dfd = Utils.Random(possibleTargets)
		saboteur.Demolish(dfd)
		saboteur.CallFunc(function()
			SendSaboteur(saboteur)
			ScanForBetterTargets(saboteur)
		end)
	else
		Trigger.AfterDelay(100, function()
			ScanForBetterTargets(saboteur)
		end)
	end
end

SendSaboteur = function(saboteur)
	local targets = GetSaboteurTargets(Atreides)
	if #targets < 1 then
		return
	end

	local target = Utils.Random(targets)
	saboteur.Demolish(target)

	-- 'target' got removed from the world in the meantime
	saboteur.CallFunc(function()
		SendSaboteur(saboteur)
	end)
end


Tick = function()

	if Atreides.HasNoRequiredUnits() then
		Ordos.MarkCompletedObjective(KillAtreides1)
		OrdosSmall.MarkCompletedObjective(KillAtreides2)
		Harkonnen.MarkCompletedObjective(KillAtreides3)
	end

	if Ordos.HasNoRequiredUnits() and OrdosSmall.HasNoRequiredUnits() and not OrdosKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("ordos-annihilated"), Mentat)
		OrdosKilled = true
		Atreides.MarkCompletedObjective(KillOrdos)
	end

	if Harkonnen.HasNoRequiredUnits() and not HarkonnenKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-annihilated"), Mentat)
		HarkonnenKilled = true
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Ordos] then
		local units = Ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Ordos] = false
			ProtectHarvester(units[1], Ordos, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Harkonnen] then
		local units = Harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end

end

Trigger.OnEnteredFootprint({ CPos.New(2,2),CPos.New(2,3),CPos.New(2,4),CPos.New(3,2),CPos.New(3,3),CPos.New(3,4) }, function(intruder, id)
	if intruder.Owner == Atreides then
		Utils.Do(IdlingUnits[Smugglers], function(unit)
			if unit.IsDead then return end
			unit.Stop()
			unit.Attack(intruder, true, true)
			Trigger.AfterDelay(400, function()
				if unit.IsDead then return end
				unit.Stop()
				unit.AttackMove(UnitSpawn3.Location)
			end)
		end)
	end
end)


CheckSmugglerEnemies = function()
	Utils.Do(SmugglerActors, function(unit)
		Trigger.OnDamaged(unit, function(self, attacker)
			if Utils.Any(IgnoreDamageFromTypes, function(a) return a == attacker.Type end) then
				return
			end

			if not self.HasProperty("Health") then return end
			if self.MaxHealth * 0.9 < self.Health then return end

			if unit.Owner == Smugglers and attacker.Owner == Atreides then
				ChangeOwner(Smugglers, SmugglersAtreides)
			end

			if unit.Owner == SmugglersAI and attacker.Owner == Atreides then
				ChangeOwner(SmugglersAI, SmugglerBoth)
			end

			if unit.Owner == Smugglers and (attacker.Owner == Ordos or attacker.Owner == OrdosSmall or attacker.Owner == Harkonnen) then
				ChangeOwner(Smugglers, SmugglersAI)
			end

			if unit.Owner == SmugglersAtreides and (attacker.Owner == Ordos or attacker.Owner == OrdosSmall or attacker.Owner == Harkonnen) then
				ChangeOwner(SmugglerHarkonnen, SmugglerBoth)
			end

			if attacker.Owner == Atreides and not MessageCheck then
				MessageCheck = true
				Media.DisplayMessage(UserInterface.GetFluentMessage("smugglers-now-hostile"), Mentat)
			end
		end)
	end)
end

ChangeOwner = function(old_owner, new_owner)
	local units = old_owner.GetActors()
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			unit.Owner = new_owner
		end
	end)
end

EmergencyBehaviour = function(player, target)
	HoldProduction[player] = false
	Attacking[player] = false

	if Difficulty == "hard" then
		player.Cash = player.Cash + 2000
	end

	if player == OrdosSmall and #IdlingUnits[Ordos] > 10 then
		local reinforcements = SetupAttackGroup(Ordos, Utils.RandomInteger(10, #IdlingUnits[Ordos]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				if unit.IsDead then
					return
				end
				unit.Stop()
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end
end

WorldLoaded = function()

	Ordos = Player.GetPlayer("Ordos")
	Smugglers = Player.GetPlayer("Smugglers")
	OrdosSmall = Player.GetPlayer("OrdosSmall")
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")
	SmugglersAtreides = Player.GetPlayer("SmugglersAtreides")
	SmugglersAI = Player.GetPlayer("SmugglersAI")
	SmugglersBoth = Player.GetPlayer("SmugglersBoth")
	Ordos.Cash = 6000
	OrdosSmall.Cash = 3000
	Smugglers.Cash = 7000
	Harkonnen.Cash = 6000
	InitObjectives(Atreides)
	KillOrdos = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-ordos"))
	KillHarkonnen = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-harkonnen"))
	KillAtreides1 = AddPrimaryObjective(Ordos, "")
	KillAtreides2 = AddPrimaryObjective(OrdosSmall, "")
	KillAtreides3 = AddPrimaryObjective(Harkonnen, "")

	Camera.Position = AMCV.CenterPosition
	AttackLocation = AMCV.Location

	Trigger.OnAllKilledOrCaptured(OrdosBase, function()
		Utils.Do(Ordos.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.AfterDelay(5, function()
		SmugglerActors = Smugglers.GetActors()
		CheckSmugglerEnemies()
	end)

	local path = function() return Utils.Random(OrdosReinforcementsPaths) end
	local waveCondition = function() return OrdosKilled end
	local huntFunction = function(unit)
		unit.AttackMove(AttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Ordos, 0, EnemysAttackWavesTotalCount[Difficulty], OrdosReinforcementsDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	Actor.Create("upgrade.barracks", true, { Owner = Ordos })
	Actor.Create("upgrade.light", true, { Owner = Ordos })
	Actor.Create("upgrade.heavy", true, { Owner = Ordos })
	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = OrdosSmall })
	Actor.Create("upgrade.light", true, { Owner = OrdosSmall })
	Trigger.AfterDelay(EarlyGameStage * DifficultyModifier[Difficulty], BuildSaboteur)
	Trigger.AfterDelay(0, ActivateAI)

end
