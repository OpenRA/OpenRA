Base = 
{
	Harkonnen = { HRefinery, SHeavyFactory, SLightFactory, HGunTurret1, HGunTurret2, HGunTurret3, HGunTurret4, HGunTurret5, SBarracks, HPower1, HPower2, HPower3, HPower4 },
	Smugglers = { SOutpost, SHeavyFactory, SLightFactory, SGunTurret1, SGunTurret2, SGunTurret3, SGunTurret4, SBarracks, SPower1, SPower2, SPower3 }
}

HarkonnenLightInfantryRushers =
{
	easy = { "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	normal = { "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	hard = { "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" }
}

HarkonnenAttackDelay =
{
	easy = DateTime.Minutes(3) + DateTime.Seconds(30),
	normal = DateTime.Minutes(2) + DateTime.Seconds(30),
	hard = DateTime.Minutes(1) + DateTime.Seconds(30)
}

InitialReinforcements = 
{
	Harkonnen = { "combat_tank_h", "combat_tank_h", "trike", "quad" },
	Smugglers = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" }
}

LightInfantryRushersPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry3.Location, HarkonnenRally3.Location }
}

InitialReinforcementsPaths =
{
	Harkonnen = { HarkonnenEntry4.Location, HarkonnenRally4.Location },
	Smugglers = { SmugglerEntry.Location, SmugglerRally.Location }
}

OrdosReinforcements = { "light_inf", "light_inf", "light_inf", "light_inf" }

OrdosPath = { OrdosEntry.Location, OrdosRally.Location }

SendHarkonnen = function(path)
	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		if player.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", HarkonnenLightInfantryRushers[Difficulty], path, { path[1] })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(HarkonnenAttackLocation)
			IdleHunt(unit)
		end)
	end)
end

Hunt = function(house)
	Trigger.OnAllKilledOrCaptured(Base[house.Name], function()
		Utils.Do(house.GetGroundAttackers(), IdleHunt)
	end)
end

CheckHarvester = function(house)
	if DateTime.GameTime % DateTime.Seconds(30) and HarvesterKilled[house.Name] then
		local units = house.GetActorsByType("harvester")

		if #units > 0 then
			HarvesterKilled[house.Name] = false
			ProtectHarvester(units[1], house)
		end
	end
end

Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillOrdosH)
		smuggler.MarkCompletedObjective(KillOrdosS)
		smuggler.MarkCompletedObjective(DefendOutpost)
	end

	if harkonnen.HasNoRequiredUnits() and not player.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end

	CheckHarvester(harkonnen)
	CheckHarvester(smuggler)

	if SOutpost.IsDead then
		player.MarkFailedObjective(CaptureOutpost)
	end

	if SOutpost.Owner == player then
		player.MarkCompletedObjective(CaptureOutpost)
		smuggler.MarkFailedObjective(DefendOutpost)
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	smuggler = Player.GetPlayer("Smugglers")
	player = Player.GetPlayer("Ordos")

	Difficulty = Map.LobbyOption("difficulty")

	InitObjectives()

	Camera.Position = OConyard.CenterPosition
	HarkonnenAttackLocation = OConyard.Location

	Hunt(harkonnen)
	Hunt(smuggler)

	SendHarkonnen(LightInfantryRushersPaths[1])
	SendHarkonnen(LightInfantryRushersPaths[2])
	SendHarkonnen(LightInfantryRushersPaths[3])
	ActivateAI()

	Actor.Create("upgrade.barracks", true, { Owner = harkonnen })
	Actor.Create("upgrade.light", true, { Owner = harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = smuggler })
	Actor.Create("upgrade.light", true, { Owner = smuggler })

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty] - DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, OrdosReinforcements, OrdosPath)
	end)

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
			Media.DisplayMessage("WARNING: Large force approaching!", "Mentat")
	end)
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillOrdosH = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillOrdosS = smuggler.AddSecondaryObjective("Kill all Ordos units.")
	DefendOutpost = smuggler.AddPrimaryObjective("Don't let the outpost to be captured or destroyed.")
	CaptureOutpost = player.AddPrimaryObjective("Capture the Smuggler Outpost.")
	KillHarkonnen = player.AddSecondaryObjective("Destroy the Harkonnen.")

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
