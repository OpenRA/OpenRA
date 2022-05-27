--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Base =
{
	Harkonnen = { HConyard, HRefinery, HHeavyFactory, HLightFactory, HGunTurret1, HGunTurret2, HGunTurret3, HGunTurret4, HGunTurret5, HBarracks, HPower1, HPower2, HPower3, HPower4 },
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
	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[house] then
		local units = house.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[house] = false
			ProtectHarvester(units[1], house, AttackGroupSize[Difficulty])
		end
	end
end

AttackNotifier = 0
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

	AttackNotifier = AttackNotifier - 1
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	smuggler = Player.GetPlayer("Smugglers")
	player = Player.GetPlayer("Ordos")

	InitObjectives(player)
	KillOrdosH = harkonnen.AddPrimaryObjective("Kill all Ordos units.")
	KillOrdosS = smuggler.AddSecondaryObjective("Kill all Ordos units.")
	DefendOutpost = smuggler.AddPrimaryObjective("Don't let the outpost to be captured or destroyed.")
	CaptureOutpost = player.AddPrimaryObjective("Capture the Smuggler Outpost.")
	KillHarkonnen = player.AddSecondaryObjective("Destroy the Harkonnen.")

	SOutpost.GrantCondition("modified")

	Camera.Position = OConyard.CenterPosition
	HarkonnenAttackLocation = OConyard.Location

	Hunt(harkonnen)
	Hunt(smuggler)

	SendHarkonnen(LightInfantryRushersPaths[1])
	SendHarkonnen(LightInfantryRushersPaths[2])
	SendHarkonnen(LightInfantryRushersPaths[3])

	Actor.Create("upgrade.barracks", true, { Owner = harkonnen })
	Actor.Create("upgrade.light", true, { Owner = harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = smuggler })
	Actor.Create("upgrade.light", true, { Owner = smuggler })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.OnKilled(SOutpost, function()
		player.MarkFailedObjective(CaptureOutpost)
	end)
	Trigger.OnCapture(SOutpost, function()
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			player.MarkCompletedObjective(CaptureOutpost)
			smuggler.MarkFailedObjective(DefendOutpost)
		end)
	end)
	Trigger.OnDamaged(SOutpost, function()
		if SOutpost.Owner ~= smuggler then
			return
		end

		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage("Don't destroy the Outpost!", "Mentat")
		end
	end)

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty] - DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.Reinforce(player, OrdosReinforcements, OrdosPath)
	end)

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		Media.DisplayMessage("WARNING: Large force approaching!", "Mentat")
	end)
end
