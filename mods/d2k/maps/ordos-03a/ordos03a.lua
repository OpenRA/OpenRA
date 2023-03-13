--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	return #Ordos.GetActorsByType(OrdosBaseBuildings[index]) > 0 and not Ordos.HasPrerequisites({ OrdosUpgrades[index] })
end

Tick = function()
	if Ordos.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if Harkonnen.HasNoRequiredUnits() and not Ordos.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillHarkonnen)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Harkonnen] then
		local units = Harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage(UserInterface.Translate("upgrade-barracks-light-factory"), Mentat)
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Ordos = Player.GetPlayer("Ordos")

	InitObjectives(Ordos)
	KillOrdos = AddPrimaryObjective(Harkonnen, "")
	KillHarkonnen = AddPrimaryObjective(Ordos, "eliminate-harkonnen-units-reinforcements")

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(HarkonnenPaths) end
	local waveCondition = function() return Ordos.IsObjectiveCompleted(KillHarkonnen) end
	SendCarryallReinforcements(Harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition)
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(Ordos, "carryall.reinforce", OrdosReinforcements, OrdosPath, { OrdosPath[1] })
	end)

	TriggerCarryallReinforcements(Ordos, Harkonnen, HarkonnenBaseAreaTrigger, HarkonnenHunters, HarkonnenHunterPath)
end
