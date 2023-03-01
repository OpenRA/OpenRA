--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { ABarracks, AWindTrap1, AWindTrap2, ALightFactory, AOutpost, AConyard, ARefinery, ASilo }
AtreidesBaseAreaTriggers =
{
	{ CPos.New(34, 50), CPos.New(35, 50), CPos.New(36, 50), CPos.New(37, 50), CPos.New(38, 50), CPos.New(39, 50), CPos.New(40, 50), CPos.New(41, 50), CPos.New(14, 57), CPos.New(14, 58), CPos.New(14, 59), CPos.New(14, 60), CPos.New(14, 61), CPos.New(14, 62), CPos.New(14, 63), CPos.New(14, 64), CPos.New(14, 65)},
	{ CPos.New(29, 51), CPos.New(29, 52), CPos.New(29, 53), CPos.New(29, 54), CPos.New(44, 50), CPos.New(44, 51), CPos.New(44, 52), CPos.New(44, 53), CPos.New(44, 54), CPos.New(43, 54), CPos.New(42, 54), CPos.New(41, 54), CPos.New(40, 54), CPos.New(39, 54), CPos.New(38, 54), CPos.New(37, 54), CPos.New(36, 54), CPos.New(35, 54), CPos.New(34, 54), CPos.New(33, 54), CPos.New(32, 54), CPos.New(31, 54), CPos.New(30, 54) },
	{ CPos.New(46, 18), CPos.New(46, 19), CPos.New(46, 20), CPos.New(46, 21), CPos.New(46, 22), CPos.New(46, 23) }
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
	{ "trooper", "trooper", "trooper", "trooper" },
	{ "trike", "trike", "trike" },
	{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trike", "trike" },
	{ "trooper", "trooper", "trooper", "trooper", "trooper", "quad", "quad" },
	{ "quad", "trooper", "trooper", "trooper" }
}

AtreidesPaths =
{
	{ AtreidesEntry1.Location, AtreidesRally1.Location },
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location }
}

AtreidesHunterPaths =
{
	{ AtreidesEntry4.Location, AtreidesRally4.Location },
	{ AtreidesEntry5.Location, AtreidesRally5.Location },
	{ AtreidesEntry6.Location, AtreidesRally6.Location },
	{ AtreidesEntry7.Location, AtreidesRally7.Location },
	{ AtreidesEntry8.Location, AtreidesRally8.Location }
}

HarkonnenReinforcements = { "trike", "trike", "quad" }
HarkonnenPath = { HarkonnenEntry.Location, HarkonnenRally.Location }

HarkonnenBaseBuildings = { "barracks", "light_factory" }
HarkonnenUpgrades = { "upgrade.barracks", "upgrade.light" }

MessageCheck = function(index)
	return #Harkonnen.GetActorsByType(HarkonnenBaseBuildings[index]) > 0 and not Harkonnen.HasPrerequisites({ HarkonnenUpgrades[index] })
end

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if Atreides.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Atreides] then
		local units = Atreides.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Atreides] = false
			ProtectHarvester(units[1], Atreides, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage(UserInterface.Translate("upgrade-barracks-light-factory"), Mentat)
	end
end

WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	KillHarkonnen = AddPrimaryObjective(Atreides, "")
	KillAtreides = AddPrimaryObjective(Harkonnen, "eliminate-atreides-units-reinforcements")

	Camera.Position = HConyard.CenterPosition

	Trigger.OnAllKilled(AtreidesBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(AtreidesPaths) end
	local waveCondition = function() return Harkonnen.IsObjectiveCompleted(KillAtreides) end
	SendCarryallReinforcements(Atreides, 0, AtreidesAttackWaves[Difficulty], AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition)
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenReinforcements, HarkonnenPath, { HarkonnenPath[1] })
	end)

	TriggerCarryallReinforcements(Harkonnen, Atreides, AtreidesBaseAreaTriggers[1], AtreidesHunters[1], AtreidesHunterPaths[1])
	TriggerCarryallReinforcements(Harkonnen, Atreides, AtreidesBaseAreaTriggers[1], AtreidesHunters[2], AtreidesHunterPaths[2])
	TriggerCarryallReinforcements(Harkonnen, Atreides, AtreidesBaseAreaTriggers[2], AtreidesHunters[3], AtreidesHunterPaths[3])
	TriggerCarryallReinforcements(Harkonnen, Atreides, AtreidesBaseAreaTriggers[2], AtreidesHunters[4], AtreidesHunterPaths[4])
	TriggerCarryallReinforcements(Harkonnen, Atreides, AtreidesBaseAreaTriggers[3], AtreidesHunters[5], AtreidesHunterPaths[5])
end
