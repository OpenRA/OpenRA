--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosBase = { OBarracks, OWindTrap1, OWindTrap2, OWindTrap3, OWindTrap4, OLightFactory, OOutpost, OConyard, ORefinery, OSilo1, OSilo2, OSilo3, OSilo4 }

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
	return #Atreides.GetActorsByType(AtreidesBaseBuildings[index]) > 0 and not Atreides.HasPrerequisites({ AtreidesUpgrades[index] })
end

CachedResources = -1
Tick = function()
	if Atreides.HasNoRequiredUnits() then
		Ordos.MarkCompletedObjective(KillAtreides)
	end

	if Ordos.HasNoRequiredUnits() and not Atreides.IsObjectiveCompleted(KillOrdos) then
		Media.DisplayMessage(UserInterface.Translate("ordos-annihilated"), Mentat)
		Atreides.MarkCompletedObjective(KillOrdos)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Ordos] then
		local units = Ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Ordos] = false
			ProtectHarvester(units[1], Ordos, AttackGroupSize[Difficulty])
		end
	end

	if Atreides.Resources > SpiceToHarvest - 1 then
		Atreides.MarkCompletedObjective(GatherSpice)
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage(UserInterface.Translate("upgrade-barracks-light-factory"), Mentat)
	end

	if Atreides.Resources ~= CachedResources then
		local parameters = { ["harvested"] = Atreides.Resources, ["goal"] = SpiceToHarvest }
		local harvestedResources = UserInterface.Translate("harvested-resources", parameters)
		UserInterface.SetMissionText(harvestedResources)
		CachedResources = Atreides.Resources
	end
end

WorldLoaded = function()
	Ordos = Player.GetPlayer("Ordos")
	Atreides = Player.GetPlayer("Atreides")

	SpiceToHarvest = ToHarvest[Difficulty]

	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Ordos, "")
	local harvestSpice = UserInterface.Translate("harvest-spice", { ["spice"] = SpiceToHarvest })
	GatherSpice = AddPrimaryObjective(Atreides, harvestSpice)
	KillOrdos = AddSecondaryObjective(Atreides, "eliminate-ordos-units-reinforcements")

	Camera.Position = AConyard.CenterPosition

	local checkResourceCapacity = function()
		Trigger.AfterDelay(0, function()
			if Atreides.ResourceCapacity < SpiceToHarvest then
				Media.DisplayMessage(UserInterface.Translate("not-enough-silos"), Mentat)
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Ordos.MarkCompletedObjective(KillAtreides)
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

		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" and actor.Owner == Atreides end)
		if #refs == 0 then
			Ordos.MarkCompletedObjective(KillAtreides)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				Ordos.MarkCompletedObjective(KillAtreides)
			end)

			local silos = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "silo" and actor.Owner == Atreides end)
			Utils.Do(refs, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
			Utils.Do(silos, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
		end
	end)

	Trigger.OnAllKilled(OrdosBase, function()
		Utils.Do(Ordos.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return OrdosPaths[1] end
	local waveCondition = function() return Atreides.IsObjectiveCompleted(KillOrdos) end
	SendCarryallReinforcements(Ordos, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition)
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Media.PlaySpeechNotification(Atreides, "Reinforce")
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", AtreidesReinforcements, AtreidesPath, { AtreidesPath[1] })
	end)
end
