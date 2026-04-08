--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { AConyard, APower1, APower2, APower3, APower4, APower5, APower6, APower7, ARefinery1, ABarracks1, ALightFactory, AHeavyFactory, AHiTechFactory, AOutpost, AStarport, ATurret1, ATurret2, ATurret3, ATurret4, ATurret5 }

AtreidesSmallBase = { APower8, APower9, ABarracks2, ARefinery2, ATurret6, ATurret7, ATurret8, ATurret9 }

AtreidesSmall2Base = { APower10, APower11, APower12, APower13, APower14, ABarracks3, ATurret10, ATurret11, ATurret12 }

CorrinoBase = { CConYard, CPower1, CPower2, CPower3, CPower4, CPower5, CBarracks, CLightFactory, CHeavyFactory, CPalace, CTurret1, CTurret2, CTurret3, CTurret4 }

MercenaryBase = { MPower1, MPower2, MStarport }
MercenaryLeader = UserInterface.GetFluentMessage("mercenary-leader")

InitialReinforcementsSquads =
{
	{ "combat_tank_a", "quad", "trike", "light_inf", "light_inf" },
	{ "combat_tank_h", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
	{ "trooper", "trooper", "trooper", "light_inf", "light_inf" },
	{ "quad", "quad", "light_inf", "light_inf", "trooper" }
}

InitialSpawnPaths =
{
	{CPos.New(81, 14), AtreidesUnitSpawn.Location},
	{CPos.New(66, 2), CorrinoUnitSpawn.Location},
	{CPos.New(81, 43), AtreidesSmallUnitSpawn.Location},
	{CPos.New(60, 81), HarkonnenUnitSpawn.Location},
}

MercenaryReinforcements = { "combat_tank_o", "trike", "trike", "trooper", "trooper", "trooper" }

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trike" },
		{ "combat_tank_a", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trike" },
		{ "combat_tank_a", "light_inf" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "quad" },
		{ "trooper", "trooper", "trooper", "trike", "trike" },
		{ "combat_tank_a", "combat_tank_a" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "quad" },
		{ "trooper", "trooper", "trooper", "trike", "trike" },
		{ "combat_tank_a", "combat_tank_a" }
	},
	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "quad", "quad" },
		{ "trooper", "trooper", "trooper", "trike", "trike", "trike" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "quad", "quad" },
		{ "trooper", "trooper", "trooper", "trike", "trike", "trike" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a" }
	}
}

AtreidesReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(AReinforcementsPoint1.Location), AReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(AReinforcementsPoint2.Location), AReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(AReinforcementsPoint3.Location), AReinforcementsPoint3.Location }
}

CorrinoReinforcements = { "trike", "trike" }

CorrinoReinforcementsPath = { Map.ClosestEdgeCell(CReinforcementsPoint.Location), CReinforcementsPoint.Location }

AtreidesAttackWaves =
{
	easy = 3,
	normal = 4,
	hard = 6
}

AtreidesReinforcementsInterval =
{
	easy = 3000,
	normal = 2500,
	hard = 2000
}

MercenaryStarportDelivery =
{
	{ "trooper", "trooper", "trooper", "trooper", "quad", "quad", "trike", "trike" },
	{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf" }
}

MercenaryReinforcementsPath = { Map.ClosestEdgeCell(MReinforcementsPoint.Location),MReinforcementsPoint.Location }


SendDelivery = function(starport, timer, squad)
	Trigger.AfterDelay(timer, function()
		if starport.IsDead or starport.Owner ~= Mercenaries then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(Mercenaries, "frigate", squad, { Map.ClosestEdgeCell(starport.Location), starport.Location + CVec.New(1, 1) }, { Map.ClosestEdgeCell(starport.Location) })[2]
		Utils.Do(units, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				if unit.IsDead then return end
				IdlingUnits[Mercenaries][#IdlingUnits[Mercenaries] + 1] = unit
				SelectRoutine(Mercenaries, unit)
		end)
		end)
		SendDelivery(starport, timer, squad)
		Trigger.AfterDelay(500, function()
			if DateTime.GameTime > AttackDelay[Mercenaries] and #IdlingUnits[Mercenaries] > AttackGroupSize["normal"] then
				SendAttack(Mercenaries, Utils.RandomInteger(AttackGroupSize[Difficulty], #IdlingUnits[Mercenaries]))
				AttackDelay[Mercenaries] = DateTime.GameTime + TimeBetweenAttacks[Mercenaries]
			end
		end)
	end)
end

AirStrikeTimer = 7500
AirStrikeChargeTime = 7500
AirstrikeLogic = function(airstrikeProvider)
	if airstrikeProvider.IsDead or airstrikeProvider.Owner ~= Atreides then return end
	if DateTime.GameTime <= AirStrikeTimer then
		Trigger.AfterDelay(AirStrikeTimer - DateTime.GameTime + 1, function()
			AirstrikeLogic(airstrikeProvider)
		end)
		return
	end

	-- randomly choose if wait again or strike. During waiting Airstrike can still be used by DefensiveAirStrike
	if Utils.RandomInteger(1, 100) < 30 then
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
			actor.Type ~= "medium_gun_turret" and
			actor.Type ~= "large_gun_turret" and
			actor.Type ~= "silo" and
			actor.Type ~= "wind_trap"
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
		local ActorsInCircle = Map.ActorsInCircle(possibleTargets[i].CenterPosition, WDist.FromCells(4), function(a)
			return
				not a.Owner.IsAlliedWith(airstrikeProvider.Owner)
				and not a.IsDead
				and Utils.Any(TargetsTypes, function(type) return a.Type == type end)
		end)

		bestValue[i] = 0
		Utils.Do(ActorsInCircle, function(a)
			bestValue[i] = bestValue[i] + Actor.Cost(a.Type)
		end)

		if bestValue[i] > bestValue[bestIndex] then
			bestIndex = i
		end
	end
	airstrikeProvider.TargetAirstrike(possibleTargets[bestIndex].CenterPosition)
	AirStrikeTimer =  DateTime.GameTime + AirStrikeChargeTime
end

EmergencyBehaviour = function(player,target)
	if player == Atreides or player == AtreidesSmall or player == AtreidesSmall2 then
		if AHiTechFactory.IsDead or AHiTechFactory.Owner ~= AtreidesMain then return end

		local enemyunits = Map.ActorsInCircle(Map.CenterOfCell(target), WDist.FromCells(15), function(a)
			return a.Owner.IsAlliedWith(Atreides)
				and not a.IsDead
				and a.HasProperty("Attack")
		end)

		if enemyunits[1] == nil  then return end
		DefensiveAirStrike(AHiTechFactory, enemyunits)
	end

	if player == AtreidesSmall2 and #IdlingUnits[Atreides] > 10 then
		local reinforcements = SetupAttackGroup(Atreides, Utils.RandomInteger(10, #IdlingUnits[Atreides]))
		Utils.Do(reinforcements, function(unit)
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(1, function()
				unit.Stop()
				unit.AttackMove(AReinforcementsPoint2.Location,1)
				unit.AttackMove(target)
				unit.CallFunc(function()
					FindTargetsInArea(player, unit)
				end)
			end)
		end)
	end

	if player == Corrino then
		player.Cash = player.Cash + 2000
	end
end


Tick = function()

	if Ordos.HasNoRequiredUnits() then
		Corrino.MarkCompletedObjective(KillOrdos1)
		AtreidesSmall.MarkCompletedObjective(KillOrdos2)
		Atreides.MarkCompletedObjective(KillOrdos3)
		AtreidesSmall2.MarkCompletedObjective(KillOrdos4)
	end

	if Atreides.HasNoRequiredUnits() and AtreidesSmall.HasNoRequiredUnits() and AtreidesSmall2.HasNoRequiredUnits() and not AtreidesKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("atreides-annihilated"), Mentat)
		AtreidesKilled = true
		Ordos.MarkCompletedObjective(KillAtreides)
	end
	if Corrino.HasNoRequiredUnits() and not CorrinoKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("emperor-annihilated"), Mentat)
		CorrinoKilled = true
		Ordos.MarkCompletedObjective(KillCorrino)
	end

		if DateTime.GameTime % DateTime.Seconds(10) and not MercenariesAreNeutral then
			local lostRatio = UnitLostRatio(Mercenaries, {Atreides, AtreidesSmall, AtreidesSmall2, Corrino}, 10 * DifficultyModifier[Difficulty])
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

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Corrino] then
		local units = Corrino.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Corrino] = false
			ProtectHarvester(units[1], Corrino, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	Ordos = Player.GetPlayer("Ordos")
	Corrino = Player.GetPlayer("Corrino")
	AtreidesSmall2 = Player.GetPlayer("AtreidesSmall2")
	AtreidesSmall = Player.GetPlayer("AtreidesSmall")
	Atreides = Player.GetPlayer("Atreides")
	Mercenaries = Player.GetPlayer("Mercenaries")
	MercenariesNeutral = Player.GetPlayer("MercenariesNeutral")
	Atreides.Cash = 10000
	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Ordos, UserInterface.GetFluentMessage("destroy-atreides"))
	KillCorrino = AddPrimaryObjective(Ordos, UserInterface.GetFluentMessage("destroy-corrino"))

	KillOrdos1 = AddPrimaryObjective(Corrino, "")
	KillOrdos2 = AddPrimaryObjective(AtreidesSmall, "")
	KillOrdos3 = AddPrimaryObjective(Atreides, "")
	KillOrdos4 = AddPrimaryObjective(AtreidesSmall2, "")

	Camera.Position = MReinforcementsPoint.CenterPosition
	AttackLocation = MReinforcementsPoint.Location

	Trigger.OnAllKilledOrCaptured(AtreidesBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)
	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesSmallBase, function()
		Utils.Do(AtreidesSmall.GetGroundAttackers(), function(actor)
			-- if possible retreat to main base
			if AConyard.IsDead then
				IdleHunt()
			else
				OwnerChanged = false
				if actor.IsMobile  then
					actor.Move(AConyard.Location, 5)
					actor.CallFunc(function()
						IdlingUnits[Atreides][#IdlingUnits[Atreides] + 1] = actor
						if OwnerChanged == false then
							ChangeOwner(AtreidesSmall, Atreides)
							OwnerChanged = true
						end
					end)
				end
			end
		end)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesSmall2Base, function()
		Utils.Do(AtreidesSmall2.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnKilled(MStarport, function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-become-neutral"), MercenaryLeader, Mercenaries.Color)
				ChangeOwner(Mercenaries, MercenariesNeutral)
				MercenariesAreNeutral = true
	end)


	local path = function() return Utils.Random(AtreidesReinforcementsPaths) end
	local pathCorrino = function() return CorrinoReinforcementsPath end
	local waveCondition = function() return AtreidesKilled end
	local huntFunction = function(unit)
		unit.AttackMove(AttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Atreides, 0, AtreidesAttackWaves[Difficulty], AtreidesReinforcementsInterval[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)
	SendCarryallReinforcements(Corrino, 0, 1, 13000, pathCorrino, {CorrinoReinforcements}, waveCondition, huntFunction)
	Trigger.AfterDelay(DateTime.Minutes(10), function() AirstrikeLogic(AHiTechFactory) end )
	SendDelivery(MStarport, 5000, MercenaryStarportDelivery[1])
	SendDelivery(MStarport, 12000, MercenaryStarportDelivery[2])

	Actor.Create("upgrade.barracks", true, { Owner = Corrino })
	Actor.Create("upgrade.light", true, { Owner = Corrino })
	Actor.Create("upgrade.heavy", true, { Owner = Corrino })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesSmall })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesSmall2 })
	Actor.Create("upgrade.barracks", true, { Owner = Atreides })
	Actor.Create("upgrade.light", true, { Owner = Atreides })
	Actor.Create("upgrade.heavy", true, { Owner = Atreides })
	Trigger.AfterDelay(0, ActivateAI)
	VisionPoint.GrantCondition("activate", 500)
end

ChangeOwner = function(old_owner, new_owner)
	local units = old_owner.GetActors()
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			unit.Owner = new_owner
		end
	end)
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
