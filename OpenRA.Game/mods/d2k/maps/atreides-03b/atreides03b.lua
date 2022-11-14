--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosBase = { OBarracks, OWindTrap1, OWindTrap2, OOutpost, OConyard, ORefinery, OSilo }

OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "raider", "trooper" },
		{ "light_inf", "raider", "quad" },
		{ "light_inf", "light_inf", "trooper", "raider", "raider", "quad" }
	},

	normal =
	{
		{ "light_inf", "raider", "trooper" },
		{ "light_inf", "raider", "raider" },
		{ "light_inf", "light_inf", "trooper", "raider", "raider", "quad" },
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "raider", "quad", "quad" }
	},

	hard =
	{
		{ "raider", "raider", "quad" },
		{ "light_inf", "raider", "raider" },
		{ "trooper", "trooper", "light_inf", "raider" },
		{ "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "raider", "raider", "quad", "quad", "quad", "raider" },
		{ "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "light_inf", "raider", "light_inf", "trooper", "trooper", "quad" },
		{ "raider", "raider", "quad", "quad", "quad", "raider" }
	}
}

OrdosAttackDelay =
{
	easy = DateTime.Minutes(5),
	normal = DateTime.Minutes(2) + DateTime.Seconds(40),
	hard = DateTime.Minutes(1) + DateTime.Seconds(20)
}

OrdosAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

ToHarvest =
{
	easy = 5000,
	normal = 6000,
	hard = 7000
}

InitialOrdosReinforcements = { "light_inf", "light_inf", "quad", "quad", "raider", "raider", "trooper", "trooper" }

OrdosPaths =
{
	{ OrdosEntry1.Location, OrdosRally1.Location },
	{ OrdosEntry2.Location, OrdosRally2.Location }
}

AtreidesReinforcements = { "quad", "quad", "trike", "trike" }
AtreidesPath = { AtreidesEntry.Location, AtreidesRally.Location }

AtreidesBaseBuildings = { "barracks", "light_factory" }
AtreidesUpgrades = { "upgrade.barracks", "upgrade.light" }

MessageCheck = function(index)
	return #player.GetActorsByType(AtreidesBaseBuildings[index]) > 0 and not player.HasPrerequisites({ AtreidesUpgrades[index] })
end

Tick = function()
	if player.HasNoRequiredUnits() then
		ordos.MarkCompletedObjective(KillAtreides)
	end

	if ordos.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage("The Ordos have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillOrdos)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[ordos] then
		local units = ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[ordos] = false
			ProtectHarvester(units[1], ordos, AttackGroupSize[Difficulty])
		end
	end

	if player.Resources > SpiceToHarvest - 1 then
		player.MarkCompletedObjective(GatherSpice)
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage("Upgrade barracks and light factory to produce more advanced units.", "Mentat")
	end

	UserInterface.SetMissionText("Harvested resources: " .. player.Resources .. "/" .. SpiceToHarvest, player.Color)
end

WorldLoaded = function()
	ordos = Player.GetPlayer("Ordos")
	player = Player.GetPlayer("Atreides")

	SpiceToHarvest = ToHarvest[Difficulty]

	InitObjectives(player)
	KillAtreides = ordos.AddPrimaryObjective("Kill all Atreides units.")
	GatherSpice = player.AddPrimaryObjective("Harvest " .. tostring(SpiceToHarvest) .. " Solaris worth of Spice.")
	KillOrdos = player.AddSecondaryObjective("Eliminate all Ordos units and reinforcements\nin the area.")

	Camera.Position = AConyard.CenterPosition

	local checkResourceCapacity = function()
		Trigger.AfterDelay(0, function()
			if player.ResourceCapacity < SpiceToHarvest then
				Media.DisplayMessage("We don't have enough silo space to store the required amount of Spice!", "Mentat")
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					ordos.MarkCompletedObjective(KillAtreides)
				end)

				return true
			end
		end)
	end

	Trigger.OnRemovedFromWorld(AConyard, function()

		-- Mission already failed, no need to check the other conditions as well
		if checkResourceCapacity() then
			return
		end

		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" and actor.Owner == player end)
		if #refs == 0 then
			ordos.MarkCompletedObjective(KillAtreides)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				ordos.MarkCompletedObjective(KillAtreides)
			end)

			local silos = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "silo" and actor.Owner == player end)
			Utils.Do(refs, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
			Utils.Do(silos, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
		end
	end)

	Trigger.OnAllKilled(OrdosBase, function()
		Utils.Do(ordos.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return OrdosPaths[1] end
	local waveCondition = function() return player.IsObjectiveCompleted(KillOrdos) end
	SendCarryallReinforcements(ordos, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition)
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", AtreidesReinforcements, AtreidesPath, { AtreidesPath[1] })
	end)
end
