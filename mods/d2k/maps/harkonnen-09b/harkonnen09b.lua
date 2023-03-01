--[[
   Copyright (c) The OpenRA Developers and Contributors
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
		if CStarport.IsDead or CStarport.Owner ~= CorrinoSmall then
			return
		end

		local reinforcements = Utils.Random(CorrinoStarportReinforcements[Difficulty])

		local units = Reinforcements.ReinforceWithTransport(CorrinoSmall, "frigate", reinforcements, { CorrinoStarportEntry.Location, CStarport.Location + CVec.New(1, 1) }, { CorrinoStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(AtreidesAttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements()
	end)
end

SendHarkonnenReinforcements = function(delay)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenReinforcements, HarkonnenPath, { HarkonnenPath[1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Harkonnen, "Reinforce")
		end)
	end)
end

SendAirStrike = function()
	if AHiTechFactory.IsDead or AHiTechFactory.Owner ~= AtreidesMain then
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

BuildFremen = function()
	if APalace.IsDead or APalace.Owner ~= AtreidesMain then
		return
	end

	APalace.Produce("fremen")
	APalace.Produce("fremen")

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		IdleFremen = Utils.Where(AtreidesMain.GetActorsByType('fremen'), function(actor) return actor.IsIdle end)

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
			if unit.Owner == SmugglerNeutral and attacker.Owner == Harkonnen then
				ChangeOwner(SmugglerNeutral, SmugglerHarkonnen)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerNeutral, SmugglerHarkonnen)
				end)
			end

			if unit.Owner == SmugglerAI and attacker.Owner == Harkonnen then
				ChangeOwner(SmugglerAI, SmugglerBoth)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerAI, SmugglerBoth)
				end)
			end

			if unit.Owner == SmugglerNeutral and (attacker.Owner == AtreidesMain or attacker.Owner == AtreidesSmall or attacker.Owner == CorrinoMain or attacker.Owner == CorrinoSmall) then
				ChangeOwner(SmugglerNeutral, SmugglerAI)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerNeutral, SmugglerAI)
				end)
			end

			if unit.Owner == SmugglerHarkonnen and (attacker.Owner == AtreidesMain or attacker.Owner == AtreidesSmall or attacker.Owner == CorrinoMain or attacker.Owner == CorrinoSmall) then
				ChangeOwner(SmugglerHarkonnen, SmugglerBoth)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerHarkonnen, SmugglerBoth)
				end)
			end

			if attacker.Owner == Harkonnen and not MessageCheck then
				MessageCheck = true
				Media.DisplayMessage(UserInterface.Translate("smugglers-now-hostile"), Mentat)
			end
		end)
	end)
end

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		AtreidesMain.MarkCompletedObjective(KillHarkonnen1)
		AtreidesSmall.MarkCompletedObjective(KillHarkonnen2)
		CorrinoMain.MarkCompletedObjective(KillHarkonnen3)
		CorrinoSmall.MarkCompletedObjective(KillHarkonnen4)
	end

	if AtreidesMain.HasNoRequiredUnits() and AtreidesSmall.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if CorrinoMain.HasNoRequiredUnits() and CorrinoSmall.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillCorrino) then
		Media.DisplayMessage(UserInterface.Translate("emperor-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillCorrino)
	end

	if SmugglerNeutral.HasNoRequiredUnits() and SmugglerHarkonnen.HasNoRequiredUnits() and SmugglerAI.HasNoRequiredUnits() and SmugglerBoth.HasNoRequiredUnits() and not SmugglersKilled then
		Media.DisplayMessage(UserInterface.Translate("smugglers-annihilated"), Mentat)
		SmugglersKilled = true
	end

	local playerConYards = Harkonnen.GetActorsByType("construction_yard")
	if #playerConYards > 0 and not Harkonnen.IsObjectiveCompleted(DeployMCV) then
		Harkonnen.MarkCompletedObjective(DeployMCV)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[AtreidesMain] then
		local units = AtreidesMain.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[AtreidesMain] = false
			ProtectHarvester(units[1], AtreidesMain, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[AtreidesSmall] then
		local units = AtreidesSmall.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[AtreidesSmall] = false
			ProtectHarvester(units[1], AtreidesSmall, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[CorrinoMain] then
		local units = CorrinoMain.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[CorrinoMain] = false
			ProtectHarvester(units[1], CorrinoMain, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[CorrinoSmall] then
		local units = CorrinoSmall.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[CorrinoSmall] = false
			ProtectHarvester(units[1], CorrinoSmall, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	AtreidesMain = Player.GetPlayer("Atreides Main Base")
	AtreidesSmall = Player.GetPlayer("Atreides Small Base")
	CorrinoMain = Player.GetPlayer("Corrino Main Base")
	CorrinoSmall = Player.GetPlayer("Corrino Small Base")
	SmugglerNeutral = Player.GetPlayer("Smugglers - Neutral")
	SmugglerHarkonnen = Player.GetPlayer("Smugglers - Enemy to Harkonnen")
	SmugglerAI = Player.GetPlayer("Smugglers - Enemy to AI")
	SmugglerBoth = Player.GetPlayer("Smugglers - Enemy to Both")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	DeployMCV = AddSecondaryObjective(Harkonnen, "build-deploy-mcv")
	KillAtreides = AddPrimaryObjective(Harkonnen, "destroy-atreides")
	KillCorrino = AddPrimaryObjective(Harkonnen, "destroy-imperial-forces")
	KillHarkonnen1 = AddPrimaryObjective(AtreidesMain, "")
	KillHarkonnen2 = AddPrimaryObjective(AtreidesSmall, "")
	KillHarkonnen3 = AddPrimaryObjective(CorrinoMain, "")
	KillHarkonnen4 = AddPrimaryObjective(CorrinoSmall, "")

	-- Wait for carryall drop
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		SmugglerUnits = SmugglerNeutral.GetActors()
		CheckSmugglerEnemies()
	end)

	Camera.Position = HBarracks.CenterPosition
	AtreidesAttackLocation = HBarracks.Location

	Trigger.AfterDelay(DateTime.Minutes(5), SendAirStrike)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds (30), BuildFremen)

	Trigger.OnAllKilledOrCaptured(AtreidesMainBase, function()
		Utils.Do(AtreidesMain.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(AtreidesSmallBase, function()
		Utils.Do(AtreidesSmall.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoMainBase, function()
		Utils.Do(CorrinoMain.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(CorrinoSmallBase, function()
		Utils.Do(CorrinoSmall.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(AtreidesPaths) end
	local waveCondition = function() return Harkonnen.IsObjectiveCompleted(KillAtreides) end
	local huntFunction = function(unit)
		unit.AttackMove(AtreidesAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(AtreidesMain, 0, AtreidesAttackWaves[Difficulty], AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.light", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.heavy", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.hightech", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesSmall })
	Actor.Create("upgrade.light", true, { Owner = AtreidesSmall })
	Actor.Create("upgrade.heavy", true, { Owner = AtreidesSmall })
	Actor.Create("upgrade.barracks", true, { Owner = CorrinoMain })
	Actor.Create("upgrade.heavy", true, { Owner = CorrinoMain })
	Actor.Create("upgrade.barracks", true, { Owner = CorrinoSmall })
	Actor.Create("upgrade.light", true, { Owner = CorrinoSmall })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Minutes(2))
end
