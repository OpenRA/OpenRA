--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

		reinforcements = Utils.Random(MercenaryStarportReinforcements)

		local units = Reinforcements.ReinforceWithTransport(faction, "frigate", reinforcements, { MercenaryStarportEntry.Location, MStarport.Location + CVec.New(1, 1) }, { MercenaryStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(MercenaryAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements(faction)
	end)
end

SendAirStrike = function()
	if AHiTechFactory.IsDead or AHiTechFactory.Owner ~= atreides_enemy then
		return
	end

	local targets = Utils.Where(player.GetActors(), function(actor)
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
	if OPalace.IsDead or OPalace.Owner ~= ordos then
		return
	end

	local targets = GetSaboteurTargets(player)
	if #targets > 0 then
		local saboteur = Actor.Create("saboteur", true, { Owner = ordos, Location = OPalace.Location + CVec.New(0, 2) })
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
	local targets = GetSaboteurTargets(player)
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
	AtreidesUnits = atreides_neutral.GetActors()

	Utils.Do(AtreidesUnits, function(unit)
		Trigger.OnDamaged(unit, function(self, attacker)
			if attacker.Owner == player and not check then
				ChangeOwner(atreides_neutral, atreides_enemy)

				-- Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(atreides_neutral, atreides_enemy)
				end)

				check = true
				Media.DisplayMessage("The Atreides are now hostile!", "Mentat")
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
	if player.HasNoRequiredUnits() then
		ordos.MarkCompletedObjective(KillHarkonnen1)
		atreides_enemy.MarkCompletedObjective(KillHarkonnen2)
	end

	if ordos.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage("The Ordos have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillOrdos)
	end

	if atreides_enemy.HasNoRequiredUnits() and atreides_neutral.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end

	if mercenary_enemy.HasNoRequiredUnits() and mercenary_ally.HasNoRequiredUnits() and not MercenariesDestroyed then
		Media.DisplayMessage("The Mercenaries have been annihilated!", "Mentat")
		MercenariesDestroyed = true
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos] then
		local units = ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos] = false
			ProtectHarvester(units[1], ordos, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[atreides_enemy] then
		local units = atreides_enemy.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[atreides_enemy] = false
			ProtectHarvester(units[1], atreides_enemy, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	ordos = Player.GetPlayer("Ordos")
	atreides_enemy = Player.GetPlayer("Ordos Aligned Atreides")
	atreides_neutral = Player.GetPlayer("Neutral Atreides")
	mercenary_enemy = Player.GetPlayer("Ordos Aligned Mercenaries")
	mercenary_ally = Player.GetPlayer("Harkonnen Aligned Mercenaries")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	KillOrdos = player.AddPrimaryObjective("Destroy the Ordos.")
	KillAtreides = player.AddSecondaryObjective("Destroy the Atreides.")
	AllyWithMercenaries = player.AddSecondaryObjective("Persuade the Mercenaries to fight with\nHouse Harkonnen.")
	KillHarkonnen1 = ordos.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen2 = atreides_enemy.AddPrimaryObjective("Kill all Harkonnen units.")

	Camera.Position = HMCV.CenterPosition
	OrdosAttackLocation = HMCV.Location
	MercenaryAttackLocation = HMCV.Location

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(30), BuildSaboteur)

	Trigger.OnCapture(MHeavyFactory, function()
		player.MarkCompletedObjective(AllyWithMercenaries)
		Media.DisplayMessage("Leader Captured. Mercenaries have been persuaded to fight with House Harkonnen.", "Mentat")
		MercenaryAttackLocation = MercenaryAttackPoint.Location

		ChangeOwner(mercenary_enemy, mercenary_ally)
		SendStarportReinforcements(mercenary_ally)
		DefendAndRepairBase(mercenary_ally, MercenaryBase, 0.75, AttackGroupSize[Difficulty])
		IdlingUnits[mercenary_ally] = IdlingUnits[mercenary_enemy]
	end)

	Trigger.OnKilled(MHeavyFactory, function()
		if not player.IsObjectiveCompleted(AllyWithMercenaries) then
			player.MarkFailedObjective(AllyWithMercenaries)
		end
	end)

	Trigger.OnKilledOrCaptured(OPalace, function()
		Media.DisplayMessage("We cannot stand against the Harkonnen. We must become neutral.", "Atreides Commander")

		ChangeOwner(atreides_enemy, atreides_neutral)
		DefendAndRepairBase(atreides_neutral, AtreidesBase, 0.75, AttackGroupSize[Difficulty])
		IdlingUnits[atreides_neutral] = IdlingUnits[atreides_enemy]

		-- Ensure that harvesters that was on a carryall switched sides.
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			ChangeOwner(atreides_enemy, atreides_neutral)
			CheckAttackToAtreides()
		end)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosBase, function()
		Utils.Do(ordos.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesBase, function()
		Utils.Do(atreides_enemy.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(MercenaryBase, function()
		Utils.Do(mercenary_enemy.GetGroundAttackers(), IdleHunt)
		Utils.Do(mercenary_ally.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillOrdos) end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(ordos, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements(mercenary_enemy)

	Actor.Create("upgrade.barracks", true, { Owner = ordos })
	Actor.Create("upgrade.light", true, { Owner = ordos })
	Actor.Create("upgrade.heavy", true, { Owner = ordos })
	Actor.Create("upgrade.barracks", true, { Owner = atreides_enemy })
	Actor.Create("upgrade.light", true, { Owner = atreides_enemy })
	Actor.Create("upgrade.heavy", true, { Owner = atreides_enemy })
	Actor.Create("upgrade.hightech", true, { Owner = atreides_enemy })
	Actor.Create("upgrade.heavy", true, { Owner = mercenary_enemy })
	Trigger.AfterDelay(0, ActivateAI)
end
