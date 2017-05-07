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

wave = 0
SendAtreides = function()
	Trigger.AfterDelay(AtreidesAttackDelay[Map.LobbyOption("difficulty")], function()
		if player.IsObjectiveCompleted(KillAtreides) then
			return
		end

		wave = wave + 1
		if wave > AtreidesAttackWaves[Map.LobbyOption("difficulty")] then
			return
		end

		local path = Utils.Random(AtreidesPaths)
		local units = Reinforcements.ReinforceWithTransport(atreides, "carryall.reinforce", AtreidesReinforcements[Map.LobbyOption("difficulty")][wave], path, { path[1] })[2]
		Utils.Do(units, IdleHunt)

		SendAtreides()
	end)
end

MessageCheck = function(index)
	return #player.GetActorsByType(HarkonnenBaseBuildings[index]) > 0 and not player.HasPrerequisites({ HarkonnenUpgrades[index] })
end

SendHunters = function(areaTrigger, unit, path, check)
	Trigger.OnEnteredFootprint(areaTrigger, function(a, id)
		if not check and a.Owner == player then
			local units = Reinforcements.ReinforceWithTransport(atreides, "carryall.reinforce", unit, path, { path[1] })[2]
			Utils.Do(units, IdleHunt)
			check = true
		end
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if atreides.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage("The Atreides have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillAtreides)
	end

	if DateTime.GameTime % DateTime.Seconds(30) and HarvesterKilled then
		local units = atreides.GetActorsByType("harvester")

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
	atreides = Player.GetPlayer("Atreides")
	player = Player.GetPlayer("Harkonnen")

	InitObjectives()

	Camera.Position = HConyard.CenterPosition

	Trigger.OnAllKilled(AtreidesBase, function()
		Utils.Do(atreides.GetGroundAttackers(), IdleHunt)
	end)

	SendAtreides()
	ActivateAI()

	Trigger.AfterDelay(DateTime.Minutes(2) + DateTime.Seconds(30), function()
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", HarkonnenReinforcements, HarkonnenPath, { HarkonnenPath[1] })
	end)

	SendHunters(AtreidesBaseAreaTriggers[1], AtreidesHunters[1], AtreidesHunterPaths[1], HuntersSent1)
	SendHunters(AtreidesBaseAreaTriggers[2], AtreidesHunters[2], AtreidesHunterPaths[2], HuntersSent2)
	SendHunters(AtreidesBaseAreaTriggers[3], AtreidesHunters[3], AtreidesHunterPaths[3], HuntersSent3)
	SendHunters(AtreidesBaseAreaTriggers[4], AtreidesHunters[4], AtreidesHunterPaths[4], HuntersSent4)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillHarkonnen = atreides.AddPrimaryObjective("Kill all Harkonnen units.")
	KillAtreides = player.AddPrimaryObjective("Eliminate all Atreides units and reinforcements\nin the area.")

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
