--[[
   Copyright (c) The OpenRA Developers and Contributors
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
		if Ordos.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenLightInfantryRushers[Difficulty], path, { path[1] })[2]
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
	if Ordos.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillOrdosH)
		Smuggler.MarkCompletedObjective(KillOrdosS)
		Smuggler.MarkCompletedObjective(DefendOutpost)
	end

	if Harkonnen.HasNoRequiredUnits() and not Ordos.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillHarkonnen)
	end

	CheckHarvester(Harkonnen)
	CheckHarvester(Smuggler)

	AttackNotifier = AttackNotifier - 1
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Smuggler = Player.GetPlayer("Smugglers")
	Ordos = Player.GetPlayer("Ordos")

	InitObjectives(Ordos)
	KillOrdosH = AddPrimaryObjective(Harkonnen, "")
	KillOrdosS = AddSecondaryObjective(Smuggler, "")
	DefendOutpost = AddPrimaryObjective(Smuggler, "outpost-not-captured-destroyed")
	CaptureOutpost = AddPrimaryObjective(Ordos, "capture-smuggler-outpost")
	KillHarkonnen = AddSecondaryObjective(Ordos, "destroy-harkonnen")

	SOutpost.GrantCondition("modified")

	Camera.Position = OConyard.CenterPosition
	HarkonnenAttackLocation = OConyard.Location

	Hunt(Harkonnen)
	Hunt(Smuggler)

	SendHarkonnen(LightInfantryRushersPaths[1])
	SendHarkonnen(LightInfantryRushersPaths[2])
	SendHarkonnen(LightInfantryRushersPaths[3])

	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.barracks", true, { Owner = Smuggler })
	Actor.Create("upgrade.light", true, { Owner = Smuggler })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.OnKilled(SOutpost, function()
		Ordos.MarkFailedObjective(CaptureOutpost)
	end)
	Trigger.OnCapture(SOutpost, function()
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Ordos.MarkCompletedObjective(CaptureOutpost)
			Smuggler.MarkFailedObjective(DefendOutpost)
		end)
	end)
	Trigger.OnDamaged(SOutpost, function()
		if SOutpost.Owner ~= Smuggler then
			return
		end

		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage(UserInterface.Translate("do-not-destroy-outpost"), Mentat)
		end
	end)

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty] - DateTime.Seconds(5), function()
		Media.PlaySpeechNotification(Ordos, "Reinforce")
		Reinforcements.Reinforce(Ordos, OrdosReinforcements, OrdosPath)
	end)

	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		Media.DisplayMessage(UserInterface.Translate("warning-large-force-approaching"), Mentat)
	end)
end
