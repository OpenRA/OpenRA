--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

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

FremenProduction = function()
	if Sietch.IsDead then
		return
	end

	local delay = Utils.RandomInteger(FremenInterval[Difficulty][1], FremenInterval[Difficulty][2] + 1)
	fremen.Build({ "nsfremen" }, function()
		Trigger.AfterDelay(delay, FremenProduction)
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

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[harkonnen] then
		local units = harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[harkonnen] = false
			ProtectHarvester(units[1], harkonnen, AttackGroupSize[Difficulty])
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

	InitObjectives(player)
	KillAtreides = harkonnen.AddPrimaryObjective("Kill all Atreides units.")
	ProtectFremen = player.AddPrimaryObjective("Protect the Fremen Sietch.")
	KillHarkonnen = player.AddPrimaryObjective("Destroy the Harkonnen.")
	KeepIntegrity = player.AddSecondaryObjective("Keep the Sietch " .. IntegrityLevel[Difficulty] .. "% intact!")

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
		Actor.Create("invisibleBlocker", true, { Owner = fremen, Location = CPos.New(62, 59) })
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

	local path = function() return Utils.Random(HarkonnenPaths) end
	local waveCondition = function() return player.IsObjectiveCompleted(KillHarkonnen) end
	local huntFunction = function(unit)
		unit.AttackMove(HarkonnenAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition, huntFunction)

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

	Trigger.OnExitedProximityTrigger(Sietch.CenterPosition, WDist.New(10.5 * 1024), function(a, id)
		if a.Owner == fremen and not a.IsDead then
			a.AttackMove(FremenRally.Location)
			Trigger.OnIdle(a, function()
				if a.Location.X < 54 or a.Location.Y < 54 then
					a.AttackMove(FremenRally.Location)
				end
			end)
		end
	end)
end
