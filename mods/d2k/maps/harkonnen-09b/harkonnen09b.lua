--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesMainBase = { AConYard1, AOutpost1, APalace, ARefinery1, ARefinery2, AHeavyFactory1, ALightFactory1, ARepair1, AStarport1, AHiTechFactory, AResearch, ARock1, ARock2, ARock3, ARock4, ARock5, ARock6, ARock7, ARock8, ARock9, ARock10, ABarracks1, ABarracks2, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9, APower10, APower11, ASilo1, ASilo2, ASilo3 }
AtreidesSmallBase = { AConYard2, ARefinery3, ABarracks3, AHeavyFactory2, ALightFactory2, ARepair2, AGunt1, AGunt2, ARock11, APower12, APower13, APower14, APower15, APower16, APower17, APower18, APower19, APower20 }
CorrinoMainBase = { CConYard, COutpost, CPalace, CRefinery1, CHeavyFactory, CResearch, CGunt1, CGunt2, CGunt3, CGunt4, CGunt5, CGunt6, CRock1, CRock2, CRock3, CRock4, CBarracks1, CBarracks2, CPower1, CPower2, CPower3, CPower4, CPower5, CPower6, CPower7, CPower8 }
CorrinoSmallBase = { CRefinery2, CLightFactory, CStarport, CGunt7, CGunt8, CBarracks3, CPower9, CPower10, CPower11, CPower12, CSilo1, CSilo2, CSilo3, CSilo4 }

AtreidesReinforcements =
{
	easy =
	{
		{ "combat_tank_a", "quad", "light_inf", "light_inf" },
		{ "fremen", "trike", "combat_tank_a"},
		{ "combat_tank_a", "quad", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "sonic_tank" },
		{ "light_inf", "light_inf", "light_inf", "quad" }
	},

	normal =
	{
		{ "combat_tank_a", "quad", "quad", "light_inf", "light_inf" },
		{ "fremen", "fremen", "trike", "combat_tank_a"},
		{ "combat_tank_a", "quad", "quad", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "sonic_tank" },
		{ "light_inf", "light_inf", "light_inf", "quad", "quad" },
		{ "combat_tank_a", "combat_tank_a", "missile_tank" }
	},

	hard =
	{
		{ "combat_tank_a", "quad", "quad", "quad", "light_inf", "light_inf" },
		{ "fremen", "fremen", "fremen", "trike", "combat_tank_a"},
		{ "combat_tank_a", "quad", "quad", "quad", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "sonic_tank" },
		{ "light_inf", "light_inf", "light_inf", "quad", "quad", "quad" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank" },
		{ "fremen", "fremen", "fremen", "fremen", "fremen", "fremen", "fremen", "fremen" }
	}
}

CorrinoStarportReinforcements =
{
	easy =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "siege_tank", "siege_tank" },
		{ "trooper", "trooper", "combat_tank_h", "combat_tank_h" },
		{ "trike", "trike", "trike", "trooper", "trooper", "light_inf", "light_inf" }
	},

	normal =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "siege_tank", "siege_tank" },
		{ "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h" },
		{ "trike", "trike", "trike", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf" }
	},

	hard =
	{
		{ "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "sardaukar", "siege_tank", "siege_tank" },
		{ "trooper", "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h" },
		{ "trike", "trike", "trike", "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf" }
	}
}

AtreidesAttackDelay =
{
	easy = DateTime.Minutes(3) + DateTime.Seconds(30),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

CorrinoStarportDelay =
{
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(6)
}

AtreidesAttackWaves =
{
	easy = 6,
	normal = 7,
	hard = 8
}

FremenGroupSize =
{
	easy = 2,
	normal = 4,
	hard = 6
}

InitialAtreidesReinforcements =
{
	{ "combat_tank_a", "combat_tank_a", "quad", "trike" },
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "combat_tank_a" },
	{ "combat_tank_a", "combat_tank_a", "quad", "quad", "trike" }
}

InitialCorrinoReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "quad", "quad" },
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h" },
	{ "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h", "combat_tank_h" }
}

AtreidesPaths =
{
	{ AtreidesEntry1.Location, AtreidesRally1.Location },
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location },
	{ AtreidesEntry4.Location, AtreidesRally4.Location },
	{ AtreidesEntry5.Location, AtreidesRally5.Location }
}

InitialAtreidesPaths =
{
	{ AConYard1.Location, AtreidesRally6.Location },
	{ ARefinery2.Location, AtreidesRally7.Location },
	{ AtreidesEntry6.Location, AtreidesRally8.Location }
}

InitialCorrinoPaths =
{
	{ CConYard.Location, CorrinoRally1.Location },
	{ CHeavyFactory.Location, CorrinoRally2.Location },
	{ CRefinery2.Location, CorrinoRally3.Location }
}

HarkonnenReinforcements = { "combat_tank_h", "combat_tank_h" }

HarkonnenPath = { HarkonnenEntry.Location, HarkonnenRally.Location }

SendStarportReinforcements = function()
	Trigger.AfterDelay(CorrinoStarportDelay[Difficulty], function()
		if CStarport.IsDead or CStarport.Owner ~= corrino_small then
			return
		end

		reinforcements = Utils.Random(CorrinoStarportReinforcements[Difficulty])

		local units = Reinforcements.ReinforceWithTransport(corrino_small, "frigate", reinforcements, { CorrinoStarportEntry.Location, CStarport.Location + CVec.New(1, 1) }, { CorrinoStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(AtreidesAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements()
	end)
end

SendHarkonnenReinforcements = function(delay)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", HarkonnenReinforcements, HarkonnenPath, { HarkonnenPath[1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(player, "Reinforce")
		end)
	end)
end

SendAirStrike = function()
	if AHiTechFactory.IsDead or AHiTechFactory.Owner ~= atreides_main then
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

BuildFremen = function()
	if APalace.IsDead or APalace.Owner ~= atreides_main then
		return
	end

	APalace.Produce("fremen")
	APalace.Produce("fremen")

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		IdleFremen = Utils.Where(atreides_main.GetActorsByType('fremen'), function(actor) return actor.IsIdle end)

		if #IdleFremen >= FremenGroupSize[Difficulty] then
			SendFremen()
		end
	end)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds (30), BuildFremen)
end

SendFremen = function()
	Utils.Do(IdleFremen, function(freman)
		freman.AttackMove(AtreidesAttackLocation)
		IdleHunt(freman)
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

CheckSmugglerEnemies = function()
	Utils.Do(SmugglerUnits, function(unit)
		Trigger.OnDamaged(unit, function(self, attacker)
			if unit.Owner == smuggler_neutral and attacker.Owner == player then
				ChangeOwner(smuggler_neutral, smuggler_harkonnen)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(smuggler_neutral, smuggler_harkonnen)
				end)
			end

			if unit.Owner == smuggler_ai and attacker.Owner == player then
				ChangeOwner(smuggler_ai, smuggler_both)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(smuggler_ai, smuggler_both)
				end)
			end

			if unit.Owner == smuggler_neutral and (attacker.Owner == atreides_main or attacker.Owner == atreides_small or attacker.Owner == corrino_main or attacker.Owner == corrino_small) then
				ChangeOwner(smuggler_neutral, smuggler_ai)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(smuggler_neutral, smuggler_ai)
				end)
			end

			if unit.Owner == smuggler_harkonnen and (attacker.Owner == atreides_main or attacker.Owner == atreides_small or attacker.Owner == corrino_main or attacker.Owner == corrino_small) then
				ChangeOwner(smuggler_harkonnen, smuggler_both)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(smuggler_harkonnen, smuggler_both)
				end)
			end

			if attacker.Owner == player and not message_check then

				message_check = true
				Media.DisplayMessage("The Smugglers are now hostile!", "Mentat")
			end
		end)
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		atreides_main.MarkCompletedObjective(KillHarkonnen1)
		atreides_small.MarkCompletedObjective(KillHarkonnen2)
		corrino_main.MarkCompletedObjective(KillHarkonnen3)
		corrino_small.MarkCompletedObjective(KillHarkonnen4)
	end

	if atreides_main.HasNoRequiredUnits() and atreides_small.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end

	if corrino_main.HasNoRequiredUnits() and corrino_small.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage("The Emperor has been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillCorrino)
	end

	if smuggler_neutral.HasNoRequiredUnits() and smuggler_harkonnen.HasNoRequiredUnits() and smuggler_ai.HasNoRequiredUnits() and smuggler_both.HasNoRequiredUnits() and not SmugglersKilled then
		Media.DisplayMessage("The Smugglers have been annihilated!", "Mentat")
		SmugglersKilled = true
	end

	local playerConYards = player.GetActorsByType("construction_yard")
	if #playerConYards > 0 and not player.IsObjectiveCompleted(DeployMCV) then
		player.MarkCompletedObjective(DeployMCV)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[atreides_main] then
		local units = atreides_main.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[atreides_main] = false
			ProtectHarvester(units[1], atreides_main, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[atreides_small] then
		local units = atreides_small.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[atreides_small] = false
			ProtectHarvester(units[1], atreides_small, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[corrino_main] then
		local units = corrino_main.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[corrino_main] = false
			ProtectHarvester(units[1], corrino_main, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[corrino_small] then
		local units = corrino_small.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[corrino_small] = false
			ProtectHarvester(units[1], corrino_small, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	atreides_main = Player.GetPlayer("Atreides Main Base")
	atreides_small = Player.GetPlayer("Atreides Small Base")
	corrino_main = Player.GetPlayer("Corrino Main Base")
	corrino_small = Player.GetPlayer("Corrino Small Base")
	smuggler_neutral = Player.GetPlayer("Smugglers - Neutral")
	smuggler_harkonnen = Player.GetPlayer("Smugglers - Enemy to Harkonnen")
	smuggler_ai = Player.GetPlayer("Smugglers - Enemy to AI")
	smuggler_both = Player.GetPlayer("Smugglers - Enemy to Both")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	DeployMCV = player.AddSecondaryObjective("Build an MCV and deploy it into a Construction Yard.")
	KillAtreides = player.AddPrimaryObjective("Destroy the Atreides.")
	KillCorrino = player.AddPrimaryObjective("Destroy the Imperial Forces.")
	KillHarkonnen1 = atreides_main.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen2 = atreides_small.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen3 = corrino_main.AddPrimaryObjective("Kill all Harkonnen units.")
	KillHarkonnen4 = corrino_small.AddPrimaryObjective("Kill all Harkonnen units.")

	-- Wait for carryall drop
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		SmugglerUnits = smuggler_neutral.GetActors()
		CheckSmugglerEnemies()
	end)

	Camera.Position = HBarracks.CenterPosition
	AtreidesAttackLocation = HBarracks.Location

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds (30), BuildFremen)

	Trigger.OnAllKilledOrCaptured(AtreidesMainBase, function()
		Utils.Do(atreides_main.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesSmallBase, function()
		Utils.Do(atreides_small.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoMainBase, function()
		Utils.Do(corrino_main.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoSmallBase, function()
		Utils.Do(corrino_small.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(AtreidesPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillAtreides) end
	local huntFunction = function(unit)
		unit.AttackMove(AtreidesAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(atreides_main, 0, AtreidesAttackWaves[Difficulty], AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = atreides_main })
	Actor.Create("upgrade.light", true, { Owner = atreides_main })
	Actor.Create("upgrade.heavy", true, { Owner = atreides_main })
	Actor.Create("upgrade.hightech", true, { Owner = atreides_main })
	Actor.Create("upgrade.barracks", true, { Owner = atreides_small })
	Actor.Create("upgrade.light", true, { Owner = atreides_small })
	Actor.Create("upgrade.heavy", true, { Owner = atreides_small })
	Actor.Create("upgrade.barracks", true, { Owner = corrino_main })
	Actor.Create("upgrade.heavy", true, { Owner = corrino_main })
	Actor.Create("upgrade.barracks", true, { Owner = corrino_small })
	Actor.Create("upgrade.light", true, { Owner = corrino_small })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Minutes(2))
end
