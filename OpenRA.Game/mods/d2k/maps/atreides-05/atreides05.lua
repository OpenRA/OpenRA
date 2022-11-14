--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenBase = { HarkonnenConstructionYard, HarkonnenWindTrap1, HarkonnenWindTrap2, HarkonnenWindTrap3, HarkonnenWindTrap4, HarkonnenWindTrap5, HarkonnenWindTrap6, HarkonnenWindTrap7, HarkonnenWindTrap8, HarkonnenSilo1, HarkonnenSilo2, HarkonnenSilo3, HarkonnenSilo4, HarkonnenGunTurret1, HarkonnenGunTurret2, HarkonnenGunTurret3, HarkonnenGunTurret4, HarkonnenGunTurret5, HarkonnenGunTurret6, HarkonnenGunTurret7, HarkonnenHeavyFactory, HarkonnenRefinery, HarkonnenOutpost, HarkonnenLightFactory }
SmugglerBase = { SmugglerWindTrap1, SmugglerWindTrap2 }

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

HarkonnenInfantryReinforcements =
{
	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper" }
	},

	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper" }
	}
}
InfantryPath = { HarkonnenEntry3.Location }

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

MercenaryReinforcements =
{
	easy =
	{
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" }
	},

	normal =
	{
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike", "trike", "trike", "trike" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" }
	},

	hard =
	{
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike", "trike", "trike", "trike" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad", "trike", "trike", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "trike", "trike", "quad", "quad", "quad", "trike", "trike", "trike", "trike", "trike", "trike", "quad", "quad" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "combat_tank_o", "trike", "trike", "quad", "quad", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" }
	}
}

MercenaryAttackDelay =
{
	easy = DateTime.Minutes(2) + DateTime.Seconds(40),
	normal = DateTime.Minutes(1) + DateTime.Seconds(50),
	hard = DateTime.Minutes(1) + DateTime.Seconds(10)
}

MercenaryAttackWaves =
{
	easy = 3,
	normal = 6,
	hard = 9
}

MercenarySpawn = { HarkonnenRally4.Location + CVec.New(2, -2) }

-- Ordos tanks because those were intended for the Smugglers not the Atreides
ContrabandReinforcements = { "mcv", "quad", "quad", "combat_tank_o", "combat_tank_o", "combat_tank_o" }
SmugglerReinforcements = { "quad", "quad", "trike", "trike" }
InitialHarkonnenReinforcements = { "trooper", "trooper", "quad", "quad", "trike", "trike", "trike", "light_inf" }

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry1.Location, HarkonnenRally3.Location },
	{ HarkonnenEntry1.Location, HarkonnenRally2.Location },
	{ HarkonnenEntry1.Location, HarkonnenRally4.Location },
	{ HarkonnenEntry2.Location }
}

AtreidesReinforcements = { "trike", "combat_tank_a" }
AtreidesPath = { AtreidesEntry.Location, AtreidesRally.Location }

ContrabandTimes =
{
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(5),
	hard = DateTime.Minutes(2) + DateTime.Seconds(30)
}

wave = 0
SendHarkonnen = function()
	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		if player.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		wave = wave + 1

		if InfantryReinforcements and wave % 4 == 0 then
			local inf = Reinforcements.Reinforce(harkonnen, HarkonnenInfantryReinforcements[Difficulty][wave/4], InfantryPath)
			Utils.Do(inf, function(unit)
				unit.AttackMove(HarkonnenAttackLocation)
				IdleHunt(unit)
			end)
		end

		local entryPath = Utils.Random(HarkonnenPaths)
		local units = Reinforcements.ReinforceWithTransport(harkonnen, "carryall.reinforce", HarkonnenReinforcements[Difficulty][wave], entryPath, { entryPath[1] })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(HarkonnenAttackLocation)
			IdleHunt(unit)
		end)

		if wave < HarkonnenAttackWaves[Difficulty] then
			SendHarkonnen()
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function() LastHarkonnenArrived = true end)
	end)
end

mercWave = 0
SendMercenaries = function()
	Trigger.AfterDelay(MercenaryAttackDelay[Difficulty], function()
		mercWave = mercWave + 1

		Media.DisplayMessage("Incoming hostile Mercenary force detected.", "Mentat")

		local units = Reinforcements.Reinforce(mercenary, MercenaryReinforcements[Difficulty][mercWave], MercenarySpawn)
		Utils.Do(units, function(unit)
			unit.AttackMove(MercenaryAttackLocation1)
			unit.AttackMove(MercenaryAttackLocation2)
			IdleHunt(unit)
		end)

		if mercWave < MercenaryAttackWaves[Difficulty] then
			SendMercenaries()
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function() LastMercenariesArrived = true end)
	end)
end

SendContraband = function(owner)
	ContrabandArrived = true
	UserInterface.SetMissionText("The Contraband has arrived!", player.Color)

	local units = SmugglerReinforcements
	if owner == player then
		units = ContrabandReinforcements
	end

	Reinforcements.ReinforceWithTransport(owner, "frigate", units, { ContrabandEntry.Location, Starport.Location + CVec.New(1, 1) }, { ContrabandExit.Location })

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		if owner == player then
			player.MarkCompletedObjective(CaptureStarport)
			Media.DisplayMessage("Contraband has arrived and been confiscated.", "Mentat")
		else
			player.MarkFailedObjective(CaptureStarport)
			Media.DisplayMessage("Smuggler contraband has arrived. It is too late to confiscate.", "Mentat")
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		UserInterface.SetMissionText("")
	end)
end

SmugglersAttack = function()
	Utils.Do(SmugglerBase, function(building)
		if not building.IsDead and building.Owner == smuggler then
			building.Sell()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(smuggler.GetGroundAttackers(), function(unit)
			IdleHunt(unit)
		end)
	end)
end

AttackNotifier = 0
Tick = function()
	if player.HasNoRequiredUnits() then
		harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if LastHarkonnenArrived and not player.IsObjectiveCompleted(KillHarkonnen) and harkonnen.HasNoRequiredUnits() then
		Media.DisplayMessage("The Harkonnen have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillHarkonnen)
	end

	if LastMercenariesArrived and not player.IsObjectiveCompleted(KillSmuggler) and smuggler.HasNoRequiredUnits() and mercenary.HasNoRequiredUnits() then
		Media.DisplayMessage("The Smugglers have been annihilated!", "Mentat")
		player.MarkCompletedObjective(KillSmuggler)
	end

	if LastHarvesterEaten[harkonnen] and DateTime.GameTime % DateTime.Seconds(10) == 0 then
		local units = harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[harkonnen] = false
			ProtectHarvester(units[1], harkonnen, AttackGroupSize[Difficulty])
		end
	end

	AttackNotifier = AttackNotifier - 1

	if TimerTicks and not ContrabandArrived then
		TimerTicks = TimerTicks - 1
		UserInterface.SetMissionText("The contraband will arrive in " .. Utils.FormatTime(TimerTicks), player.Color)

		if TimerTicks <= 0 then
			SendContraband(smuggler)
		end
	end
end

WorldLoaded = function()
	harkonnen = Player.GetPlayer("Harkonnen")
	smuggler = Player.GetPlayer("Smugglers")
	mercenary = Player.GetPlayer("Mercenaries")
	player = Player.GetPlayer("Atreides")

	InfantryReinforcements = Difficulty ~= "easy"

	InitObjectives(player)
	KillAtreides = harkonnen.AddPrimaryObjective("Kill all Atreides units.")
	CaptureBarracks = player.AddPrimaryObjective("Capture the Barracks at Sietch Tabr.")
	KillHarkonnen = player.AddSecondaryObjective("Annihilate all other Harkonnen units\nand reinforcements.")
	CaptureStarport = player.AddSecondaryObjective("Capture the Smuggler Starport and\nconfiscate the contraband.")

	Camera.Position = ARefinery.CenterPosition
	HarkonnenAttackLocation = AtreidesRally.Location
	MercenaryAttackLocation1 = Starport.Location + CVec.New(-16, 0)
	MercenaryAttackLocation2 = Starport.Location

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		TimerTicks = ContrabandTimes[Difficulty]
		Media.DisplayMessage("The contraband is approaching the Starport to the north in " .. Utils.FormatTime(TimerTicks) .. ".", "Mentat")
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnKilled(Starport, function()
		if not player.IsObjectiveCompleted(CaptureStarport) then
			ContrabandArrived = true
			UserInterface.SetMissionText("Starport destroyed! Contraband can't land.", player.Color)
			player.MarkFailedObjective(CaptureStarport)
			SmugglersAttack()

			Trigger.AfterDelay(DateTime.Seconds(15), function()
				UserInterface.SetMissionText("")
			end)
		end

		if DefendStarport then
			player.MarkFailedObjective(DefendStarport)
		end
	end)
	Trigger.OnDamaged(Starport, function()
		if Starport.Owner ~= smuggler then
			return
		end

		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage("Don't destroy the Starport!", "Mentat")

			local defenders = smuggler.GetGroundAttackers()
			if #defenders > 0 then
				Utils.Do(defenders, function(unit)
					unit.Guard(Starport)
				end)
			end
		end
	end)
	Trigger.OnCapture(Starport, function()
		DefendStarport = player.AddSecondaryObjective("Defend the captured Starport.")

		Trigger.ClearAll(Starport)
		Trigger.AfterDelay(0, function()
			Trigger.OnRemovedFromWorld(Starport, function()
				player.MarkFailedObjective(DefendStarport)
			end)
		end)

		if not ContrabandArrived then
			SendContraband(player)
		end
		SmugglersAttack()
	end)

	Trigger.OnKilled(HarkonnenBarracks, function()
		player.MarkFailedObjective(CaptureBarracks)
	end)
	Trigger.OnDamaged(HarkonnenBarracks, function()
		if AttackNotifier <= 0 and HarkonnenBarracks.Health < HarkonnenBarracks.MaxHealth * 3/4 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage("Don't destroy the Barracks!", "Mentat")
		end
	end)
	Trigger.OnCapture(HarkonnenBarracks, function()
		Media.DisplayMessage("Hostages Released!", "Mentat")

		if DefendStarport then
			player.MarkCompletedObjective(DefendStarport)
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			player.MarkCompletedObjective(CaptureBarracks)
		end)
	end)

	SendHarkonnen()
	Actor.Create("upgrade.barracks", true, { Owner = harkonnen })
	Actor.Create("upgrade.light", true, { Owner = harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = harkonnen })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(player, "Reinforce")
		Reinforcements.ReinforceWithTransport(player, "carryall.reinforce", AtreidesReinforcements, AtreidesPath, { AtreidesPath[1] })
	end)

	local smugglerWaypoint = SmugglerWaypoint1.Location
	Trigger.OnEnteredFootprint({ smugglerWaypoint + CVec.New(-2, 0), smugglerWaypoint + CVec.New(-1, 0), smugglerWaypoint, smugglerWaypoint + CVec.New(1, -1), smugglerWaypoint + CVec.New(2, -1), SmugglerWaypoint3.Location }, function(a, id)
		if not warned and a.Owner == player and a.Type ~= "carryall" then
			warned = true
			Trigger.RemoveFootprintTrigger(id)
			Media.DisplayMessage("Stay away from our Starport.", "Smuggler Leader")
		end
	end)

	Trigger.OnEnteredFootprint({ SmugglerWaypoint2.Location }, function(a, id)
		if not paid and a.Owner == player and a.Type ~= "carryall" then
			paid = true
			Trigger.RemoveFootprintTrigger(id)
			Media.DisplayMessage("You were warned. Now you will pay.", "Smuggler Leader")
			Utils.Do(smuggler.GetGroundAttackers(), function(unit)
				unit.AttackMove(SmugglerWaypoint2.Location)
			end)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				KillSmuggler = player.AddSecondaryObjective("Destroy the Smugglers and their Mercenaries.")
				SendMercenaries()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(HarkonnenBarracks.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == player and a.Type ~= "carryall" then
			Trigger.RemoveProximityTrigger(id)
			Media.DisplayMessage("Capture the Harkonnen barracks to release the hostages.", "Mentat")
			StopInfantryProduction = true
		end
	end)
end
