--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosBase = { OConYard, OOutpost, OPalace, ORefinery1, ORefinery2, OHeavyFactory, OLightFactory, OHiTechFactory, OResearch, ORepair, OStarport, OGunt1, OGunt2, OGunt3, OGunt4, OGunt5, OGunt6, ORock1, ORock2, ORock3, ORock4, OBarracks1, OBarracks2, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10, OPower11, OPower12, OPower13 }
AtreidesBase = { AConYard, AOutpost, ARefinery1, ARefinery2, AHeavyFactory, ALightFactory, AHiTechFactory, ARepair, AStarport, AGunt1, AGunt2, ARock1, ARock2, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9 }
MercenaryBase = { MHeavyFactory, MStarport, MGunt, MPower1, MPower2 }

OrdosReinforcements =
{
	easy =
	{
		{ "trooper", "trooper", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "quad" }
	},

	normal =
	{
		{ "trooper", "trooper", "trooper", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "raider", "raider", "quad", "quad", "deviator" }
	},

	hard =
	{
		{ "trooper", "trooper", "trooper", "trooper", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "quad" },
		{ "raider", "raider", "raider", "quad", "quad", "deviator" },
		{ "siege_tank", "combat_tank_o", "combat_tank_o", "raider" }
	}
}

MercenaryStarportReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "quad", "quad" },
	{ "quad", "combat_tank_o", "trike", "quad", "trooper", "trooper" },
	{ "trooper", "trooper", "trooper", "trooper", "siege_tank", "siege_tank" },
	{ "quad", "quad", "combat_tank_o", "combat_tank_o", "combat_tank_o" }
}

OrdosAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

MercenaryStarportDelay = DateTime.Minutes(1) + DateTime.Seconds(20)

OrdosAttackWaves =
{
	easy = 4,
	normal = 5,
	hard = 6
}

InitialOrdosReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf" },
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "combat_tank_o", "combat_tank_o" }
}

InitialAtreidesReinforcements = { "combat_tank_a", "combat_tank_a", "quad", "quad", "trike" }

InitialMercenaryReinforcements = { "trooper", "trooper", "trooper", "trooper", "quad", "quad" }

OrdosPaths =
{
	{ OrdosEntry1.Location, OrdosRally1.Location },
	{ OrdosEntry2.Location, OrdosRally2.Location },
	{ OrdosEntry3.Location, OrdosRally3.Location },
	{ OrdosEntry4.Location, OrdosRally4.Location }
}

InitialOrdosPaths =
{
	{ OLightFactory.Location, OrdosRally5.Location },
	{ OHiTechFactory.Location, OrdosRally6.Location }
}

SaboteurPaths =
{
	{ SaboteurWaypoint1.Location, SaboteurWaypoint2.Location, SaboteurWaypoint3.Location },
	{ SaboteurWaypoint4.Location, SaboteurWaypoint5.Location, SaboteurWaypoint6.Location }
}

InitialAtreidesPath = { AStarport.Location, AtreidesRally.Location }

InitialMercenaryPath = { MStarport.Location, MercenaryRally.Location }

SendStarportReinforcements = function(faction)
	Trigger.AfterDelay(MercenaryStarportDelay, function()
		if MStarport.IsDead or MStarport.Owner ~= faction then
			return
		end

		local reinforcements = Utils.Random(MercenaryStarportReinforcements)

		local units = Reinforcements.ReinforceWithTransport(faction, "frigate", reinforcements, { MercenaryStarportEntry.Location, MStarport.Location + CVec.New(1, 1) }, { MercenaryStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(MercenaryAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements(faction)
	end)
end

SendAirStrike = function()
	if AHiTechFactory.IsDead or AHiTechFactory.Owner ~= AtreidesEnemy then
		return
	end

	local targets = Utils.Where(Harkonnen.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
			actor.Type ~= "wall" and
			actor.Type ~= "medium_gun_turret" and
			actor.Type ~= "large_gun_turret" and
			actor.Type ~= "silo" and
			actor.Type ~= "wind_trap"
	end)

	if #targets > 0 then
		AHiTechFactory.TargetAirstrike(Utils.Random(targets).CenterPosition)
	end

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)
end

GetSaboteurTargets = function(player)
	return Utils.Where(player.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
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

	local targets = GetSaboteurTargets(Harkonnen)
	if #targets > 0 then
		local saboteur = Actor.Create("saboteur", true, { Owner = Ordos, Location = OPalace.Location + CVec.New(0, 2) })
		saboteur.Move(saboteur.Location + CVec.New(0, 1))
		saboteur.Wait(DateTime.Seconds(5))

		local path = Utils.Random(SaboteurPaths)
		saboteur.Move(path[1])
		saboteur.Move(path[2])
		saboteur.Move(path[3])

		SendSaboteur(saboteur)
	end

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), BuildSaboteur)
end

SendSaboteur = function(saboteur)
	local targets = GetSaboteurTargets(Harkonnen)
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

CheckAttackToAtreides = function()
	AtreidesUnits = AtreidesNeutral.GetActors()

	Utils.Do(AtreidesUnits, function(unit)
		Trigger.OnDamaged(unit, function(self, attacker)
			if attacker.Owner == Harkonnen and not Check then
				ChangeOwner(AtreidesNeutral, AtreidesEnemy)

				-- Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(AtreidesNeutral, AtreidesEnemy)
				end)

				Check = true
				Media.DisplayMessage(UserInterface.Translate("atreides-hostile"), Mentat)
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

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		Ordos.MarkCompletedObjective(KillHarkonnen1)
		AtreidesEnemy.MarkCompletedObjective(KillHarkonnen2)
	end

	if Ordos.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage(UserInterface.Translate("ordos-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if AtreidesEnemy.HasNoRequiredUnits() and AtreidesNeutral.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if MercenaryEnemy.HasNoRequiredUnits() and MercenaryAlly.HasNoRequiredUnits() and not MercenariesDestroyed then
		Media.DisplayMessage(UserInterface.Translate("mercenaries-annihilated"), Mentat)
		MercenariesDestroyed = true
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Ordos] then
		local units = Ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Ordos] = false
			ProtectHarvester(units[1], Ordos, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[AtreidesEnemy] then
		local units = AtreidesEnemy.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[AtreidesEnemy] = false
			ProtectHarvester(units[1], AtreidesEnemy, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	Ordos = Player.GetPlayer("Ordos")
	AtreidesEnemy = Player.GetPlayer("Ordos Aligned Atreides")
	AtreidesNeutral = Player.GetPlayer("Neutral Atreides")
	MercenaryEnemy = Player.GetPlayer("Ordos Aligned Mercenaries")
	MercenaryAlly = Player.GetPlayer("Harkonnen Aligned Mercenaries")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	KillOrdos = AddPrimaryObjective(Harkonnen, "destroy-ordos")
	KillAtreides = AddSecondaryObjective(Harkonnen, "destroy-atreides")
	AllyWithMercenaries = AddSecondaryObjective(Harkonnen, "ally-mercenaries")
	KillHarkonnen1 = AddPrimaryObjective(Ordos, "")
	KillHarkonnen2 = AddPrimaryObjective(AtreidesEnemy, "")

	Camera.Position = HMCV.CenterPosition
	OrdosAttackLocation = HMCV.Location
	MercenaryAttackLocation = HMCV.Location

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), BuildSaboteur)

	Trigger.OnCapture(MHeavyFactory, function()
		Harkonnen.MarkCompletedObjective(AllyWithMercenaries)
		Media.DisplayMessage(UserInterface.Translate("mercenary-leader-captured-allied"), Mentat)
		MercenaryAttackLocation = MercenaryAttackPoint.Location

		ChangeOwner(MercenaryEnemy, MercenaryAlly)
		SendStarportReinforcements(MercenaryAlly)
		DefendAndRepairBase(MercenaryAlly, MercenaryBase, 0.75, AttackGroupSize[Difficulty])
		IdlingUnits[MercenaryAlly] = IdlingUnits[MercenaryEnemy]
	end)

	Trigger.OnKilled(MHeavyFactory, function()
		if not Harkonnen.IsObjectiveCompleted(AllyWithMercenaries) then
			Harkonnen.MarkFailedObjective(AllyWithMercenaries)
		end
	end)

	Trigger.OnKilledOrCaptured(OPalace, function()
		Media.DisplayMessage(UserInterface.Translate("can-not-stand-harkonnen-must-become-neutral"), UserInterface.Translate("atreides-commander"))

		ChangeOwner(AtreidesEnemy, AtreidesNeutral)
		DefendAndRepairBase(AtreidesNeutral, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
		IdlingUnits[AtreidesNeutral] = IdlingUnits[AtreidesEnemy]

		-- Ensure that harvesters that was on a carryall switched sides.
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			ChangeOwner(AtreidesEnemy, AtreidesNeutral)
			CheckAttackToAtreides()
		end)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosBase, function()
		Utils.Do(Ordos.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesBase, function()
		Utils.Do(AtreidesEnemy.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(MercenaryBase, function()
		Utils.Do(MercenaryEnemy.GetGroundAttackers(), IdleHunt)
		Utils.Do(MercenaryAlly.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return Harkonnen.IsObjectiveCompleted(KillOrdos) end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Ordos, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements(MercenaryEnemy)

	Actor.Create("upgrade.barracks", true, { Owner = Ordos })
	Actor.Create("upgrade.light", true, { Owner = Ordos })
	Actor.Create("upgrade.heavy", true, { Owner = Ordos })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesEnemy })
	Actor.Create("upgrade.light", true, { Owner = AtreidesEnemy })
	Actor.Create("upgrade.heavy", true, { Owner = AtreidesEnemy })
	Actor.Create("upgrade.hightech", true, { Owner = AtreidesEnemy })
	Actor.Create("upgrade.heavy", true, { Owner = MercenaryEnemy })
	Trigger.AfterDelay(0, ActivateAI)
end
