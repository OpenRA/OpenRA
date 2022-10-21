--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Base =
{
	Atreides = { AConyard, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9, APower10, APower11, APower12, ABarracks, ARefinery, ALightFactory, AHeavyFactory, ARepair, AResearch, AGunt1, AGunt2, ARock1, ARock2, ARock3, ARock4 },
	Harkonnen = { HConyard, HPower1, HPower2, HPower3, HPower4, HPower5, HPower6, HPower7, HPower8, HPower9, HPower10, HBarracks, HRefinery, HOutpost, HHeavyFactory, HGunt1, HGunt2, HGunt3, HGunt4, HRock, HSilo1, HSilo2, HSilo3 }
}

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "quad", "quad", "combat_tank_a" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad" },
		{ "quad", "quad", "combat_tank_a", "combat_tank_a" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad", "quad" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a" }
	},

	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "quad", "quad" },
		{ "quad", "quad", "quad", "combat_tank_a", "combat_tank_a" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad", "quad", "quad" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "quad" },
		{ "combat_tank_a", "combat_tank_a", "missile_tank", "siege_tank" }
	}
}

HarkonnenReinforcements =
{
	easy =
	{
		{ "quad", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" }
	},

	normal =
	{
		{ "combat_tank_h", "combat_tank_h", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "quad", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "trike", "trike", "quad", "siege_tank" }
	},

	hard =
	{
		{ "combat_tank_h", "combat_tank_h", "trike", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "combat_tank_h", "quad", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "trike", "trike", "quad", "quad", "siege_tank" },
		{ "missile_tank", "missile_tank", "missile_tank", "missile_tank" }
	}
}

IxianReinforcements =
{
	easy = { "deviator", "deviator", "missile_tank", "missile_tank", "missile_tank", "siege_tank", "siege_tank", "combat_tank_o", "combat_tank_o" },
	normal = { "deviator", "deviator", "missile_tank", "missile_tank", "missile_tank", "siege_tank", "siege_tank", "combat_tank_o" },
	hard = { "deviator", "deviator", "missile_tank", "missile_tank", "siege_tank", "siege_tank", "combat_tank_o" }
}

EnemyAttackDelay =
{
	easy = DateTime.Minutes(5) + DateTime.Seconds(15),
	normal = DateTime.Minutes(3) + DateTime.Seconds(15),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

AtreidesPaths =
{
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location },
	{ AtreidesEntry4.Location, AtreidesRally4.Location }
}

HarkonnenPaths =
{
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry3.Location, HarkonnenRally3.Location },
	{ HarkonnenEntry4.Location, HarkonnenRally4.Location },
	{ HarkonnenEntry5.Location, HarkonnenRally5.Location },
	{ HarkonnenEntry6.Location, HarkonnenRally6.Location },
	{ HarkonnenEntry7.Location, HarkonnenRally7.Location }
}

AtreidesAttackWaves =
{
	easy = 3,
	normal = 4,
	hard = 5
}

HarkonnenAttackWaves =
{
	easy = 4,
	normal = 5,
	hard = 6
}

InitialReinforcements =
{
	Atreides = { "combat_tank_a", "quad", "quad", "trike", "trike" },
	Harkonnen = { "trooper", "trooper", "trooper", "trooper", "trooper", "combat_tank_h" }
}

InitialReinforcementsPaths =
{
	Atreides = { AtreidesEntry1.Location, AtreidesRally1.Location },
	Harkonnen = { HarkonnenEntry1.Location, HarkonnenRally1.Location }
}

InitialContrabandTimes =
{
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(20)
}

ContrabandTimes =
{
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(7)
}

IxianReinforcementsHaveArrived = UserInterface.Translate("ixian-reinforcements-arrived")
SendContraband = function()
	Media.PlaySpeechNotification(player, "Reinforce")

	for i = 0, 6 do
		local c = player.Color
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(IxianReinforcementsHaveArrived, c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		TimerTicks = ContrabandTimes[Difficulty]
	end)

	local entryPath = { CPos.New(82, OStarport.Location.Y + 1), OStarport.Location + CVec.New(1, 1) }
	local exitPath = { CPos.New(2, OStarport.Location.Y + 1) }
	Reinforcements.ReinforceWithTransport(player, "frigate", IxianReinforcements[Difficulty], entryPath, exitPath)
end

Hunt = function(house)
	Trigger.OnAllKilledOrCaptured(Base[house.InternalName], function()
		Utils.Do(house.GetGroundAttackers(), IdleHunt)
	end)
end

CheckHarvester = function(house)
	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[house] then
		local units = house.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[house] = false
			ProtectHarvester(units[1], house, AttackGroupSize[Difficulty])
		end
	end
end

Tick = function()
	if not player.IsObjectiveCompleted(KillAtreides) and atreides.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		player.MarkCompletedObjective(KillAtreides)
		DestroyCarryalls(atreides)

		if player.IsObjectiveCompleted(KillHarkonnen) then
			player.MarkCompletedObjective(GuardStarport)
		end
	end

	if not player.IsObjectiveCompleted(KillHarkonnen) and harkonnen.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		player.MarkCompletedObjective(KillHarkonnen)
		DestroyCarryalls(harkonnen)

		if player.IsObjectiveCompleted(KillAtreides) then
			player.MarkCompletedObjective(GuardStarport)
		end
	end

	if TimerTicks and TimerTicks > 0 then
		TimerTicks = TimerTicks - 1

		if TimerTicks == 0 then
			if not FirstIxiansArrived then
				Media.DisplayMessage(UserInterface.Translate("deliveries-arriving-massive-reinforcements"), Mentat)
			end

			FirstIxiansArrived = true
			SendContraband()
		elseif (TimerTicks % DateTime.Seconds(1)) == 0 then
			local time = { ["time"] = Utils.FormatTime(TimerTicks) }
			local reinforcementsText = UserInterface.Translate("initial-reinforcements-arrive-in", time)
			if FirstIxiansArrived then
				reinforcementsText = UserInterface.Translate("additional-reinforcements-arrive-in", time)
			end

			UserInterface.SetMissionText(reinforcementsText, player.Color)
		end
	end

	CheckHarvester(atreides)
	CheckHarvester(harkonnen)
end

WorldLoaded = function()
	atreides = Player.GetPlayer("Atreides")
	harkonnen = Player.GetPlayer("Harkonnen")
	player = Player.GetPlayer("Ordos")

	InitObjectives(player)
	GuardStarport = AddPrimaryObjective(player, "defend-starport")
	KillAtreides = AddPrimaryObjective(player, "destroy-atreides")
	KillHarkonnen = AddPrimaryObjective(player, "destroy-harkonnen")

	Camera.Position = OConyard.CenterPosition
	EnemyAttackLocations = { OConyard.Location, OStarport.Location }

	Trigger.OnRemovedFromWorld(OStarport, function()
		player.MarkFailedObjective(GuardStarport)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		TimerTicks = InitialContrabandTimes[Difficulty]
		local time = { ["time"] = Utils.FormatTime(TimerTicks) }
		Media.DisplayMessage(UserInterface.Translate("ixian-reinforcements-in", time), Mentat)
	end)

	Hunt(atreides)
	Hunt(harkonnen)

	local atreidesPath = function() return Utils.Random(AtreidesPaths) end
	local harkonnenPath = function() return Utils.Random(HarkonnenPaths) end
	local atreidesCondition = function() return player.IsObjectiveCompleted(KillAtreides) end
	local harkonnenCondition = function() return player.IsObjectiveCompleted(KillHarkonnen) end
	local huntFunction = function(unit)
		unit.AttackMove(Utils.Random(EnemyAttackLocations))
		IdleHunt(unit)
	end
	local announcementFunction = function()
		Media.DisplayMessage(UserInterface.Translate("enemy-reinforcements-arrived"), Mentat)
	end

	SendCarryallReinforcements(atreides, 0, AtreidesAttackWaves[Difficulty], EnemyAttackDelay[Difficulty], atreidesPath, AtreidesReinforcements[Difficulty], atreidesCondition, huntFunction, announcementFunction)

	Trigger.AfterDelay(Utils.RandomInteger(DateTime.Seconds(45), DateTime.Minutes(1) + DateTime.Seconds(15)), function()
		SendCarryallReinforcements(harkonnen, 0, HarkonnenAttackWaves[Difficulty], EnemyAttackDelay[Difficulty], harkonnenPath, HarkonnenReinforcements[Difficulty], harkonnenCondition, huntFunction, announcementFunction)
	end)

	Actor.Create("upgrade.barracks", true, { Owner = atreides })
	Actor.Create("upgrade.light", true, { Owner = atreides })
	Actor.Create("upgrade.heavy", true, { Owner = atreides })
	Actor.Create("upgrade.barracks", true, { Owner = harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = harkonnen })
	Trigger.AfterDelay(0, ActivateAI)
end
