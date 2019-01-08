--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HBarracks, HWindTrap1, HWindTrap2, HLightFactory, HOutpost, HConyard, HRefinery, HSilo1, HSilo2, HSilo3, HSilo4 }
HarkonnenBaseAreaTrigger = { CPos.New(2, 58), CPos.New(3, 58), CPos.New(4, 58), CPos.New(5, 58), CPos.New(6, 58), CPos.New(7, 58), CPos.New(8, 58), CPos.New(9, 58), CPos.New(10, 58), CPos.New(11, 58), CPos.New(12, 58), CPos.New(13, 58), CPos.New(14, 58), CPos.New(15, 58), CPos.New(16, 58), CPos.New(16, 59), CPos.New(16, 60) }

HarkonnenReinforcements =
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

HarkonnenAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20)
}

HarkonnenAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry3.Location, HarkonnenRally3.Location }
}

HarkonnenHunters = { "light_inf", "light_inf", "trike", "quad" }
HarkonnenInitialReinforcements = { "light_inf", "light_inf", "quad", "quad", "trike", "trike", "trooper", "trooper" }

HarkonnenHunterPath = { HarkonnenEntry5.Location, HarkonnenRally5.Location }
HarkonnenInitialPath = { HarkonnenEntry4.Location, HarkonnenRally4.Location }

OrdosReinforcements = { "quad", "raider" }
OrdosPath = { OrdosEntry.Location, OrdosRally.Location }

OrdosBaseBuildings = { "barracks", "light_factory" }
OrdosUpgrades = { "upgrade.barracks", "upgrade.light" }

MessageCheck = function(index)
	return #player.GetActorsByType(OrdosBaseBuildings[index]) > 0 and not player.HasPrerequisites({ OrdosUpgrades[index] })
end

Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[harkonnen] then
		local units = harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[harkonnen] = false
			ProtectHarvester(units[1], harkonnen, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage("Upgrade barracks and light factory to produce more advanced units.", "Mentat")
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	player = Player.GetPlayer("Ordos")

	InitObjectives(player)
	KillOrdos = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillHarkonnen = player.AddPrimaryObjective("Eliminate all Harkonnen units and reinforcements\nin the area.")

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillHarkonnen) end
	SendCarryallReinforcements(harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition)
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", OrdosReinforcements, OrdosPath, { OrdosPath[1] })
	end)

	TriggerCarryallReinforcements(player, harkonnen, HarkonnenBaseAreaTrigger, HarkonnenHunters, HarkonnenHunterPath)
end
