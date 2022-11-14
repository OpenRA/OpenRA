--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { ABarracks, AWindTrap1, AWindTrap2, AWindTrap3, ALightFactory, AOutpost, AConyard, ARefinery, ASilo1, ASilo2, ASilo3, ASilo4 }
AtreidesBaseAreaTriggers =
{
	{ CPos.New(10, 53), CPos.New(11, 53), CPos.New(12, 53), CPos.New(13, 53), CPos.New(14, 53), CPos.New(15, 53), CPos.New(16, 53), CPos.New(17, 53), CPos.New(17, 52), CPos.New(17, 51), CPos.New(17, 50), CPos.New(17, 49), CPos.New(17, 48), CPos.New(17, 47), CPos.New(17, 46), CPos.New(17, 45), CPos.New(17, 44), CPos.New(17, 43), CPos.New(17, 42), CPos.New(17, 41), CPos.New(17, 40), CPos.New(17, 39), CPos.New(17, 38), CPos.New(2, 35), CPos.New(3, 35), CPos.New(4, 35), CPos.New(5, 35), CPos.New(6, 35), CPos.New(7, 35), CPos.New(8, 35), CPos.New(9, 35), CPos.New(10, 35), CPos.New(11, 35), CPos.New(12, 35) },
	{ CPos.New(49, 25), CPos.New(50, 25), CPos.New(51, 25), CPos.New(52, 25), CPos.New(53, 25), CPos.New(54, 25), CPos.New(54, 26), CPos.New(54, 27), CPos.New(54, 28), CPos.New(54, 29) },
	{ CPos.New(19, 2), CPos.New(19, 3), CPos.New(19, 4), CPos.New(41, 2), CPos.New(41, 3), CPos.New(41, 4), CPos.New(41, 5), CPos.New(41, 6), CPos.New(41, 7), CPos.New(41, 8), CPos.New(41, 9), CPos.New(41, 10), CPos.New(41, 11) },
	{ CPos.New(2, 16), CPos.New(3, 16), CPos.New(4, 16), CPos.New(5, 16), CPos.New(19, 2), CPos.New(19, 3), CPos.New(19, 4) }
}

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "trike", "trooper" },
		{ "light_inf", "trike", "quad" },
		{ "light_inf", "light_inf", "trooper", "trike", "trike", "quad" }
	},

	normal =
	{
		{ "light_inf", "trike", "trooper" },
		{ "light_inf", "trike", "trike" },
		{ "light_inf", "light_inf", "trooper", "trike", "trike", "quad" },
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "trike", "quad", "quad" }
	},

	hard =
	{
		{ "trike", "trike", "quad" },
		{ "light_inf", "trike", "trike" },
		{ "trooper", "trooper", "light_inf", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "trike", "trike", "quad", "quad", "quad", "trike" },
		{ "light_inf", "light_inf", "light_inf", "trike", "trike" },
		{ "light_inf", "trike", "light_inf", "trooper", "trooper", "quad" },
		{ "trike", "trike", "quad", "quad", "quad", "trike" }
	}
}

AtreidesAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20)
}

AtreidesAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

AtreidesHunters =
{
	{ "trooper", "trooper", "trooper" },
	{ "trike", "light_inf", "light_inf", "light_inf", "light_inf" },
	{ "trooper", "trooper", "trooper", "trike", "trike" },
	{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper" }
}

AtreidesPaths =
{
	{ AtreidesEntry4.Location, AtreidesRally4.Location },
	{ AtreidesEntry7.Location, AtreidesRally7.Location },
	{ AtreidesEntry8.Location, AtreidesRally8.Location }
}

AtreidesHunterPaths =
{
	{ AtreidesEntry6.Location, AtreidesRally6.Location },
	{ AtreidesEntry5.Location, AtreidesRally5.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location },
	{ AtreidesEntry1.Location, AtreidesRally1.Location }
}

AtreidesInitialPath = { AtreidesEntry2.Location, AtreidesRally2.Location }
AtreidesInitialReinforcements = { "light_inf", "light_inf", "quad", "quad", "trike", "trike", "trooper", "trooper" }

HarkonnenReinforcements = { "quad", "quad" }
HarkonnenPath = { HarkonnenEntry.Location, HarkonnenRally.Location }

HarkonnenBaseBuildings = { "barracks", "light_factory" }
HarkonnenUpgrades = { "upgrade.barracks", "upgrade.light" }

MessageCheck = function(index)
	return #player.GetActorsByType(HarkonnenBaseBuildings[index]) > 0 and not player.HasPrerequisites({ HarkonnenUpgrades[index] })
end

Tick = function()
	if player.HasNoRequiredUnits() then
		atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if atreides.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[atreides] then
		local units = atreides.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[atreides] = false
			ProtectHarvester(units[1], atreides, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage("Upgrade barracks and light factory to produce more advanced units.", "Mentat")
	end
end

WorldLoaded = function()
	atreides = Player.GetPlayer("Atreides")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives(player)
	KillHarkonnen = atreides.AddPrimaryObjective("Kill all Harkonnen units.")
	KillAtreides = player.AddPrimaryObjective("Eliminate all Atreides units and reinforcements\nin the area.")

	Camera.Position = HConyard.CenterPosition

	Trigger.OnAllKilled(AtreidesBase, function()
		Utils.Do(atreides.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(AtreidesPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillAtreides) end
	SendCarryallReinforcements(atreides, 0, AtreidesAttackWaves[Difficulty], AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition)
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", HarkonnenReinforcements, HarkonnenPath, { HarkonnenPath[1] })
	end)

	TriggerCarryallReinforcements(player, atreides, AtreidesBaseAreaTriggers[1], AtreidesHunters[1], AtreidesHunterPaths[1])
	TriggerCarryallReinforcements(player, atreides, AtreidesBaseAreaTriggers[2], AtreidesHunters[2], AtreidesHunterPaths[2])
	TriggerCarryallReinforcements(player, atreides, AtreidesBaseAreaTriggers[3], AtreidesHunters[3], AtreidesHunterPaths[3])
	TriggerCarryallReinforcements(player, atreides, AtreidesBaseAreaTriggers[4], AtreidesHunters[4], AtreidesHunterPaths[4])
end
