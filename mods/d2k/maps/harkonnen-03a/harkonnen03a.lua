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
	SendHunters(AtreidesBaseAreaTriggers[1], AtreidesHunters[2], AtreidesHunterPaths[2], HuntersSent2)
	SendHunters(AtreidesBaseAreaTriggers[2], AtreidesHunters[3], AtreidesHunterPaths[3], HuntersSent3)
	SendHunters(AtreidesBaseAreaTriggers[2], AtreidesHunters[4], AtreidesHunterPaths[4], HuntersSent4)
	SendHunters(AtreidesBaseAreaTriggers[3], AtreidesHunters[5], AtreidesHunterPaths[5], HuntersSent5)
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
