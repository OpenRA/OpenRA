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

wave = 0
SendHarkonnen = function()
	Trigger.AfterDelay(HarkonnenAttackDelay[Map.LobbyOption("difficulty")], function()
		if player.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		wave = wave + 1
		if wave > HarkonnenAttackWaves[Map.LobbyOption("difficulty")] then
			return
		end

		local path = Utils.Random(HarkonnenPaths)
		local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", HarkonnenReinforcements[Map.LobbyOption("difficulty")][wave], path, { path[1] })[2]
		Utils.Do(units, IdleHunt)

		SendHarkonnen()
	end)
end

MessageCheck = function(index)
	return #player.GetActorsByType(OrdosBaseBuildings[index]) > 0 and not player.HasPrerequisites({ OrdosUpgrades[index] })
end

SendHunters = function(areaTrigger, unit, path, check)
	Trigger.OnEnteredFootprint(areaTrigger, function(a, id)
		if not check and a.Owner == player then
			local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", unit, path, { path[1] })[2]
			Utils.Do(units, IdleHunt)
			check = true
		end
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillOrdos)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end

	if DateTime.GameTime % DateTime.Seconds(30) and HarvesterKilled then
		local units = harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			HarvesterKilled = false
			ProtectHarvester(units[1])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(32) == 0 and (MessageCheck(1) or MessageCheck(2)) then
		Media.DisplayMessage("Upgrade barracks and light factory to produce more advanced units.", "Mentat")
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	player = Player.GetPlayer("Ordos")

	InitObjectives()

	Camera.Position = OConyard.CenterPosition

	Trigger.OnAllKilled(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	SendHarkonnen()
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", OrdosReinforcements, OrdosPath, { OrdosPath[1] })
	end)

	SendHunters(HarkonnenBaseAreaTrigger, HarkonnenHunters, HarkonnenHunterPath, HuntersSent)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillOrdos = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillHarkonnen = player.AddPrimaryObjective("Eliminate all Harkonnen units and reinforcements\nin the area.")

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
