--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesBase = { AConyard, AOutpost, ARefinery, AHeavyFactory, ALightFactory, AGunt1, AGunt2, ABarracks, ASilo, APower1, APower2, APower3, APower4, APower5, APower6 }
FremenBase = { FGunt1, FGunt2 }

BaseAreaTriggers =
{
	{ CPos.New(27, 38), CPos.New(26, 38), CPos.New(26, 39), CPos.New(25, 39), CPos.New(25, 40), CPos.New(25, 41), CPos.New(24, 41), CPos.New(24, 42) },
	{ CPos.New(19, 81), CPos.New(19, 82), CPos.New(19, 83), CPos.New(19, 84), CPos.New(19, 85), CPos.New(19, 86), CPos.New(19, 87), CPos.New(19, 88), CPos.New(19, 89), CPos.New(19, 90), CPos.New(19, 91) },
	{ CPos.New(10, 78), CPos.New(11, 78), CPos.New(12, 78), CPos.New(13, 78), CPos.New(14, 78), CPos.New(15, 78) }
}

Sietches = { FSietch1, FSietch2 }

FremenReinforcements =
{
	easy =
	{
		{ "combat_tank_a", "combat_tank_a" },
		{ "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a" },
		{ "combat_tank_a", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_a", "trike", "trike", "quad", "trooper", "nsfremen" }
	},

	normal =
	{
		{ "combat_tank_a", "combat_tank_a" },
		{ "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a" },
		{ "combat_tank_a", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_a", "trike", "trike", "quad", "trooper", "nsfremen" },
		{ "combat_tank_a", "trike", "combat_tank_a", "quad", "nsfremen", "nsfremen" },
		{ "fremen", "fremen", "fremen", "fremen", "trooper", "trooper", "trooper", "trooper" }
	},

	hard =
	{
		{ "combat_tank_a", "combat_tank_a" },
		{ "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a" },
		{ "combat_tank_a", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_a", "trike", "trike", "quad", "trooper", "nsfremen" },
		{ "combat_tank_a", "trike", "combat_tank_a", "quad", "nsfremen", "nsfremen" },
		{ "fremen", "fremen", "fremen", "fremen", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank" },
		{ "combat_tank_a", "combat_tank_a", "quad", "quad", "trike", "trike", "trooper", "trooper", "light_inf", "light_inf" }
	}
}

FremenAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

FremenAttackWaves =
{
	easy = 5,
	normal = 7,
	hard = 9
}

AtreidesHunters = { "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" }

FremenHunters =
{
	{ "fremen", "fremen", "fremen" },
	{ "combat_tank_a", "combat_tank_a", "combat_tank_a" },
	{ "missile_tank", "missile_tank", "missile_tank" }
}

InitialAtreidesReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf" },
	{ "combat_tank_a", "combat_tank_a", "quad", "trike" }
}

AtreidesPaths =
{
	{ AtreidesEntry1.Location, AtreidesRally1.Location },
	{ AtreidesEntry2.Location, AtreidesRally2.Location },
	{ AtreidesEntry3.Location, AtreidesRally3.Location }
}

FremenPaths =
{
	{ FremenEntry4.Location, FremenRally4.Location },
	{ FremenEntry5.Location, FremenRally5.Location },
	{ FremenEntry6.Location, FremenRally6.Location }
}

FremenHunterPaths =
{
	{ FremenEntry1.Location, FremenRally1.Location },
	{ FremenEntry2.Location, FremenRally2.Location },
	{ FremenEntry3.Location, FremenRally3.Location }
}

HarkonnenReinforcements = { "combat_tank_h", "combat_tank_h" }

HarkonnenPath = { HarkonnenEntry.Location, HarkonnenRally.Location }

FremenInterval =
{
	easy = { DateTime.Minutes(1) + DateTime.Seconds(30), DateTime.Minutes(2) },
	normal = { DateTime.Minutes(2) + DateTime.Seconds(20), DateTime.Minutes(2) + DateTime.Seconds(40) },
	hard = { DateTime.Minutes(3) + DateTime.Seconds(40), DateTime.Minutes(4) }
}

FremenProduction = function()
	if SietchesAreDestroyed then
		return
	end

	local delay = Utils.RandomInteger(FremenInterval[Difficulty][1], FremenInterval[Difficulty][2] + 1)
	Fremen.Build({ "nsfremen" }, function()
		Trigger.AfterDelay(delay, FremenProduction)
	end)
end

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if Atreides.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if Fremen.HasNoRequiredUnits() and not Harkonnen.IsObjectiveCompleted(KillFremen) then
		Media.DisplayMessage(UserInterface.Translate("fremen-annihilated"), Mentat)
		Harkonnen.MarkCompletedObjective(KillFremen)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Atreides] then
		local units = Atreides.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Atreides] = false
			ProtectHarvester(units[1], Atreides, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Fremen = Player.GetPlayer("Fremen")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	KillAtreides = AddPrimaryObjective(Harkonnen, "destroy-atreides")
	KillFremen = AddPrimaryObjective(Harkonnen, "destroy-fremen")
	KillHarkonnen = AddPrimaryObjective(Atreides, "")

	Camera.Position = HConyard.CenterPosition
	FremenAttackLocation = HConyard.Location

	Trigger.OnAllKilledOrCaptured(AtreidesBase, function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilled(Sietches, function()
		SietchesAreDestroyed = true
	end)

	local path = function() return Utils.Random(FremenPaths) end
	local waveCondition = function() return Harkonnen.IsObjectiveCompleted(KillFremen) end
	local huntFunction = function(unit)
		unit.AttackMove(FremenAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Fremen, 0, FremenAttackWaves[Difficulty], FremenAttackDelay[Difficulty], path, FremenReinforcements[Difficulty], waveCondition, huntFunction)

	Actor.Create("upgrade.barracks", true, { Owner = Atreides })
	Actor.Create("upgrade.light", true, { Owner = Atreides })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(15), function()
		Media.PlaySpeechNotification(Harkonnen, "Reinforce")
		Reinforcements.Reinforce(Harkonnen, HarkonnenReinforcements, HarkonnenPath)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		Media.DisplayMessage(UserInterface.Translate("fremen-spotted-north-southeast"), Mentat)
	end)

	local atreidesCondition = function() return Harkonnen.IsObjectiveCompleted(KillAtreides) end
	TriggerCarryallReinforcements(Harkonnen, Atreides, BaseAreaTriggers[1], AtreidesHunters,  AtreidesPaths[1], atreidesCondition)

	local fremenCondition = function() return Harkonnen.IsObjectiveCompleted(KillFremen) end
	TriggerCarryallReinforcements(Harkonnen, Fremen, BaseAreaTriggers[1], FremenHunters[1],  FremenHunterPaths[3], fremenCondition)
	TriggerCarryallReinforcements(Harkonnen, Fremen, BaseAreaTriggers[2], FremenHunters[2],  FremenHunterPaths[2], fremenCondition)
	TriggerCarryallReinforcements(Harkonnen, Fremen, BaseAreaTriggers[3], FremenHunters[3],  FremenHunterPaths[1], fremenCondition)
end
