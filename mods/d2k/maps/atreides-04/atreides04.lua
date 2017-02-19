HarkonnenBase = { HarkonnenOutpost, HarkonnenRefinery, HarkonnenHeavyFact, HarkonnenTurret1, HarkonnenTurret2, HarkonnenBarracks, HarkonnenSilo1, HarkonnenSilo2, HarkonnenWindTrap1, HarkonnenWindTrap2, HarkonnenWindTrap3, HarkonnenWindTrap4, HarkonnenWindTrap5 }

HarkonnenReinforcements =
{
	easy =
	{
		{ "combat_tank_h", "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper" }
	},

	normal =
	{
		{ "combat_tank_h", "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "trike", "combat_tank_h", "light_inf", "trooper", "trooper", "quad" },
		{ "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad" }
	},

	hard =
	{
		{ "combat_tank_h", "trooper", "trooper", "trooper", "trooper" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_h", "trike", "combat_tank_h", "light_inf", "trooper", "trooper", "quad" },
		{ "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "trike", "light_inf", "light_inf", "trooper", "trooper", "quad", "quad" },
		{ "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h", "combat_tank_h" }
	}
}

HarkonnenAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

HarkonnenAttackWaves =
{
	easy = 5,
	normal = 7,
	hard = 9
}

InitialHarkonnenReinforcements = { "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" }

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally3.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry3.Location, HarkonnenRally4.Location },
	{ HarkonnenEntry4.Location, HarkonnenRally4.Location }
}

AtreidesReinforcements =
{
	{ "trike", "combat_tank_a", "combat_tank_a" },
	{ "quad", "combat_tank_a", "combat_tank_a" }
}
AtreidesPath = { AtreidesEntry.Location, AtreidesRally.Location }

FremenInterval =
{
	easy = { DateTime.Minutes(1) + DateTime.Seconds(30), DateTime.Minutes(2) },
	normal = { DateTime.Minutes(2) + DateTime.Seconds(20), DateTime.Minutes(2) + DateTime.Seconds(40) },
	hard = { DateTime.Minutes(3) + DateTime.Seconds(40), DateTime.Minutes(4) }
}

IntegrityLevel =
{
	easy = 50,
	normal = 75,
	hard = 100
}

wave = 0
SendHarkonnen = function()
	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		if player.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		wave = wave + 1
		if wave > HarkonnenAttackWaves[Difficulty] then
			return
		end

		local entryPath = Utils.Random(HarkonnenPaths)
		local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", HarkonnenReinforcements[Difficulty][wave], entryPath, { entryPath[1] })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(HarkonnenAttackLocation)
			IdleHunt(unit)
		end)

		SendHarkonnen()
	end)
end

FremenProduction = function()
	if Sietch.IsDead then
		return
	end

	local delay = Utils.RandomInteger(FremenInterval[Difficulty][1], FremenInterval[Difficulty][2] + 1)
	fremen.Build({ "nsfremen" }, function()
		Trigger.AfterDelay(delay, ProduceInfantry)
	end)
end

AttackNotifier = 0
Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
		player.MarkCompletedObjective(ProtectFremen)
		player.MarkCompletedObjective(KeepIntegrity)
	end

	if DateTime.GameTime % DateTime.Seconds(30) and HarvesterKilled then
		local units = harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			HarvesterKilled = false
			ProtectHarvester(units[1])
		end
	end

	if not Sietch.IsDead then
		AttackNotifier = AttackNotifier - 1
		local integrity = math.floor((Sietch.Health * 100) / Sietch.MaxHealth)
		UserInterface.SetMissionText("Sietch structural integrity: " .. integrity .. "%", player.Color)

		if integrity < IntegrityLevel[Difficulty] then
			player.MarkFailedObjective(KeepIntegrity)
		end
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	fremen = Player.GetPlayer("Fremen")
	player = Player.GetPlayer("Atreides")

	Difficulty = Map.LobbyOption("difficulty")

	InitObjectives()

	Camera.Position = AConyard.CenterPosition
	HarkonnenAttackLocation = AConyard.Location

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Beacon.New(player, Sietch.CenterPosition + WVec.New(0, 1024, 0))
		Media.DisplayMessage("Fremen Sietch detected to the southeast.", "Mentat")
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnKilled(Sietch, function()
		UserInterface.SetMissionText("Sietch destroyed!", player.Color)
		player.MarkFailedObjective(ProtectFremen)
	end)
	Trigger.OnDamaged(Sietch, function()
		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Beacon.New(player, Sietch.CenterPosition + WVec.New(0, 1024, 0), DateTime.Seconds(7))
			Media.DisplayMessage("The Fremen Sietch is under attack!", "Mentat")

			local defenders = fremen.GetGroundAttackers()
			if #defenders > 0 then
				Utils.Do(defenders, function(unit)
					unit.Guard(Sietch)
				end)
			end
		end
	end)

	SendHarkonnen()
	Actor.Create("upgrade.barracks", true, { Owner = harkonnen })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Seconds(50), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, AtreidesReinforcements[1], AtreidesPath)
	end)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", AtreidesReinforcements[2], AtreidesPath, { AtreidesPath[1] })
	end)

	Trigger.OnEnteredProximityTrigger(HarkonnenRally1.CenterPosition, WDist.New(6 * 1024), function(a, id)
		if a.Owner == player then
			Trigger.RemoveProximityTrigger(id)
			local units = Reinforcements.Reinforce(harkonnen, { "light_inf", "combat_tank_h", "trike" }, HarkonnenPaths[1])
			Utils.Do(units, IdleHunt)
		end
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillAtreides = harkonnen.AddPrimaryObjective("Kill all Atreides units.")
	ProtectFremen = player.AddPrimaryObjective("Protect the Fremen Sietch.")
	KillHarkonnen = player.AddPrimaryObjective("Destroy the Harkonnen.")
	KeepIntegrity = player.AddSecondaryObjective("Keep the Sietch " .. IntegrityLevel[Difficulty] .. "% intact!")

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
