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

wave = 0
SendOrdos = function()
	Trigger.AfterDelay(OrdosAttackDelay[Map.LobbyOption("difficulty")], function()
		if player.IsObjectiveCompleted(KillOrdos) then
			return
		end

		wave = wave + 1
		if wave > OrdosAttackWaves[Map.LobbyOption("difficulty")] then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(ordos, "carryall.reinforce", OrdosReinforcements[Map.LobbyOption("difficulty")][wave], OrdosPaths[1], { OrdosPaths[1][1] })[2]
		Utils.Do(units, IdleHunt)

		SendOrdos()
	end)
end

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

	if DateTime.GameTime % DateTime.Seconds(30) and HarvesterKilled then
		local units = ordos.GetActorsByType("harvester")

		if #units > 0 then
			HarvesterKilled = false
			ProtectHarvester(units[1])
		end
	end

	if player.Resources > ToHarvest[Map.LobbyOption("difficulty")] - 1 then
		player.MarkCompletedObjective(GatherSpice)
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage("Upgrade barracks and light factory to produce more advanced units.", "Mentat")
	end

	UserInterface.SetMissionText("Harvested resources: " .. player.Resources .. "/" .. ToHarvest[Map.LobbyOption("difficulty")], player.Color)
end

WorldLoaded = function()
	ordos = Player.GetPlayer("Ordos")
	player = Player.GetPlayer("Atreides")

	InitObjectives()

	Camera.Position = AConyard.CenterPosition

	Trigger.OnAllKilled(OrdosBase, function()
		Utils.Do(ordos.GetGroundAttackers(), IdleHunt)
	end)

	SendOrdos()
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", AtreidesReinforcements, AtreidesPath, { AtreidesPath[1] })
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillAtreides = ordos.AddPrimaryObjective("Kill all Atreides units.")
	GatherSpice = player.AddPrimaryObjective("Harvest " .. tostring(ToHarvest[Map.LobbyOption("difficulty")]) .. " Solaris worth of Spice.")
	KillOrdos = player.AddSecondaryObjective("Eliminate all Ordos units and reinforcements\nin the area.")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end
