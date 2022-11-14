--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesMainBase = { AConYard1, AOutpost1, ARefinery1, ARefinery2, AHeavyFactory1, ALightFactory1, ALightFactory2, AHiTechFactory, AResearch, ARepair, AStarport, AGunt1, AGunt2, AGunt3, AGunt4, AGunt5, AGunt6, ARock1, ARock2, ARock3, ARock4, ARock5, ARock6, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9, APower10, APower11, APower12, APower13, APower14, APower15, APower16, APower17, APower18, APower19, APower20, APower21, APower22, ASilo1, ASilo2, ASilo3 }
AtreidesSmallBase = { AOutpost2, ARefinery3, ABarracks, AHeavyFactory2, AGunt7, AGunt8, APower23, APower24, APower25, APower26, ASilo4, ASilo5, ASilo6, ASilo7 }
CorrinoBase = { CBarracks, COutpost, CPower1, CPower2, CPower3, CRock1, CRock2 }

AtreidesReinforcements =
{
	easy = { "sonic_tank" },
	normal = { "sonic_tank", "trooper" },
	hard = { "sonic_tank", "quad" }
}

CorrinoReinforcements =
{
	easy =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "quad", "quad", "trike" },
		{ "sardaukar", "sardaukar", "combat_tank_h", "trike" }
	},

	normal =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "combat_tank_h", "quad", "trike" },
		{ "sardaukar", "sardaukar", "combat_tank_h", "siege_tank" }
	},

	hard =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "combat_tank_h", "quad", "quad", "trike" },
		{ "sardaukar", "sardaukar", "sardaukar", "combat_tank_h", "siege_tank" }
	}
}

EnemyAttackDelay =
{
	easy = DateTime.Minutes(3) + DateTime.Seconds(30),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

CorrinoInitialAttackDelay =
{
	easy = DateTime.Seconds(45),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(15)
}

InitialAtreidesReinforcements =
{
	easy =
	{
		{ "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_a", "quad", "quad", "trike", "trike" },
		{ "trooper", "trooper", "quad", "quad" }
	},

	normal =
	{
		{ "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_a", "combat_tank_a", "quad", "trike", "trike" },
		{ "trooper", "trooper", "trooper", "quad", "quad" }
	},

	hard =
	{
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_a", "combat_tank_a", "quad", "quad", "trike", "trike" },
		{ "trooper", "trooper", "trooper", "trooper", "quad", "quad" }
	}
}

InitialCorrinoReinforcements = { "trooper", "trooper", "trooper", "trooper", "trooper" }

AtreidesPath = { AtreidesEntry1.Location, AtreidesRally1.Location }

CorrinoPaths =
{
	{ CorrinoEntry1.Location, CorrinoRally1.Location },
	{ CorrinoEntry1.Location, CorrinoRally2.Location }
}

InitialAtreidesPaths =
{
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location },
	{ AtreidesEntry4.Location, AtreidesRally4.Location }
}

InitialCorrinoPath = { CorrinoEntry2.Location, CorrinoRally3.Location }

HarkonnenReinforcements =
{
	easy =
	{
		{ "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "trike", "siege_tank", "combat_tank_h", "combat_tank_h" }
	},

	normal =
	{
		{ "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "siege_tank", "combat_tank_h", "combat_tank_h" }
	},

	hard =
	{
		{ "trooper", "trooper", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "trike", "combat_tank_h", "combat_tank_h" }
	},
}

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location }
}

SendHarkonnenReinforcements = function(number)
	Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", HarkonnenReinforcements[Difficulty][number], HarkonnenPaths[number], { HarkonnenPaths[number][1] })
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		Media.PlaySpeechNotification(player, "Reinforce")
	end)
end

SendEnemyReinforcements = function(player, delay, path, unitTypes, customCondition, customHuntFunction)
	Trigger.AfterDelay(delay, function()
		if customCondition and customCondition() then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", unitTypes, path, { path[1] })[2]

		if not customHuntFunction then
			customHuntFunction = IdleHunt
		end
		Utils.Do(units, customHuntFunction)

		SendEnemyReinforcements(player, delay, path, unitTypes, customCondition, customHuntFunction)
	end)
end

SendAirStrike = function()
	if HiTechIsDead then
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

Tick = function()
	if player.HasNoRequiredUnits() then
		atreides_main.MarkCompletedObjective(KillHarkonnen1)
		atreides_small.MarkCompletedObjective(KillHarkonnen2)
		corrino.MarkCompletedObjective(KillHarkonnen3)
	end

	if atreides_main.HasNoRequiredUnits() and atreides_small.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end

	if corrino.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage("The Emperor have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillCorrino)
	end

	if (HEngineer.IsDead or AConYard2.IsDead) and not player.IsObjectiveCompleted(CaptureAtreidesConYard) then
		player.MarkFailedObjective(CaptureAtreidesConYard)
	end

	if (AHiTechFactory.IsDead or AHiTechFactory.Owner ~= atreides_main) and not HiTechIsDead then
		Media.DisplayMessage("High Tech Factory neutralized! Atreides cut off from Imperial reinforcement!", "Mentat")
		HiTechIsDead = true
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos_main] then
		local units = ordos_main.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos_main] = false
			ProtectHarvester(units[1], ordos_main, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos_small] then
		local units = ordos_small.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos_small] = false
			ProtectHarvester(units[1], ordos_small, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	atreides_main = Player.GetPlayer("Atreides Main Base")
	atreides_small = Player.GetPlayer("Atreides Small Base")
	corrino = Player.GetPlayer("Corrino")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	CaptureAtreidesConYard = player.AddPrimaryObjective("Capture the Atreides Construction Yard at the South.")
	KillAtreides = player.AddPrimaryObjective("Destroy the Atreides.")
	KillCorrino = player.AddPrimaryObjective("Destroy the Corrino.")
	KillHarkonnen1 = atreides_main.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen2 = atreides_small.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen3 = corrino.AddPrimaryObjective("Kill all Harkonnen units.")

	Media.DisplayMessage("Destroy Atreides High Tech Factory to cut off Atreides from Imperial reinforcements.", "Mentat")

	Camera.Position = HEngineer.CenterPosition
	AtreidesAttackLocation = AConYard2.Location

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)

	Trigger.OnCapture(AConYard2, function()
		player.MarkCompletedObjective(CaptureAtreidesConYard)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesMainBase, function()
		Utils.Do(atreides_main.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesSmallBase, function()
		Utils.Do(atreides_small.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoBase, function()
		Utils.Do(corrino.GetGroundAttackers(), IdleHunt)
	end)

	local atreidesWaveCondition = function() return player.IsObjectiveCompleted(KillAtreides) or HiTechIsDead end
	local corrinoWaveCondition = function() return player.IsObjectiveCompleted(KillCorrino) or HiTechIsDead end
	local huntFunction = function(unit)
		unit.AttackMove(AtreidesAttackLocation)
		IdleHunt(unit)
	end
	SendEnemyReinforcements(atreides_main, EnemyAttackDelay[Difficulty], AtreidesPath, AtreidesReinforcements[Difficulty], atreidesWaveCondition, huntFunction)
	Trigger.AfterDelay(CorrinoInitialAttackDelay[Difficulty], function()
		SendEnemyReinforcements(corrino, EnemyAttackDelay[Difficulty], CorrinoPaths[1], CorrinoReinforcements[Difficulty][1], corrinoWaveCondition, huntFunction)
		SendEnemyReinforcements(corrino, EnemyAttackDelay[Difficulty], CorrinoPaths[2], CorrinoReinforcements[Difficulty][2], corrinoWaveCondition, huntFunction)
	end)

	Actor.Create("upgrade.light", true, { Owner = atreides_main })
	Actor.Create("upgrade.heavy", true, { Owner = atreides_main })
	Actor.Create("upgrade.hightech", true, { Owner = atreides_main })
	Actor.Create("upgrade.barracks", true, { Owner = atreides_small })
	Actor.Create("upgrade.heavy", true, { Owner = atreides_small })
	Actor.Create("upgrade.barracks", true, { Owner = corrino })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(1)
	SendHarkonnenReinforcements(2)
end
