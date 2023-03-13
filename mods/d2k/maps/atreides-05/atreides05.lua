--[[
   Copyright (c) The OpenRA Developers and Contributors
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

Wave = 0
SendHarkonnen = function()
	Trigger.AfterDelay(HarkonnenAttackDelay[Difficulty], function()
		if Atreides.IsObjectiveCompleted(KillHarkonnen) then
			return
		end

		Wave = Wave + 1

		if InfantryReinforcements and Wave % 4 == 0 then
			local inf = Reinforcements.Reinforce(Harkonnen, HarkonnenInfantryReinforcements[Difficulty][Wave/4], InfantryPath)
			Utils.Do(inf, function(unit)
				unit.AttackMove(HarkonnenAttackLocation)
				IdleHunt(unit)
			end)
		end

		local entryPath = Utils.Random(HarkonnenPaths)
		local units = Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenReinforcements[Difficulty][Wave], entryPath, { entryPath[1] })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(HarkonnenAttackLocation)
			IdleHunt(unit)
		end)

		if Wave < HarkonnenAttackWaves[Difficulty] then
			SendHarkonnen()
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function() LastHarkonnenArrived = true end)
	end)
end

MercWave = 0
SendMercenaries = function()
	Trigger.AfterDelay(MercenaryAttackDelay[Difficulty], function()
		MercWave = MercWave + 1

		Media.DisplayMessage(UserInterface.Translate("incoming-mercenary-force"), Mentat)

		local units = Reinforcements.Reinforce(Mercenary, MercenaryReinforcements[Difficulty][MercWave], MercenarySpawn)
		Utils.Do(units, function(unit)
			unit.AttackMove(MercenaryAttackLocation1)
			unit.AttackMove(MercenaryAttackLocation2)
			IdleHunt(unit)
		end)

		if MercWave < MercenaryAttackWaves[Difficulty] then
			SendMercenaries()
			return
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function() LastMercenariesArrived = true end)
	end)
end

SendContraband = function(owner)
	ContrabandArrived = true
	UserInterface.SetMissionText(UserInterface.Translate("contraband-has-arrived"), Atreides.Color)

	local units = SmugglerReinforcements
	if owner == Atreides then
		units = ContrabandReinforcements
	end

	Reinforcements.ReinforceWithTransport(owner, "frigate", units, { ContrabandEntry.Location, Starport.Location + CVec.New(1, 1) }, { ContrabandExit.Location })

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		if owner == Atreides then
			Atreides.MarkCompletedObjective(CaptureStarport)
			Media.DisplayMessage(UserInterface.Translate("contraband-confiscated"), Mentat)
		else
			Atreides.MarkFailedObjective(CaptureStarport)
			Media.DisplayMessage(UserInterface.Translate("contraband-not-confiscated"), Mentat)
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		UserInterface.SetMissionText("")
	end)
end

SmugglersAttack = function()
	Utils.Do(SmugglerBase, function(building)
		if not building.IsDead and building.Owner == Smuggler then
			building.Sell()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Utils.Do(Smuggler.GetGroundAttackers(), function(unit)
			IdleHunt(unit)
		end)
	end)
end

AttackNotifier = 0
Tick = function()
	if Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if LastHarkonnenArrived and not Atreides.IsObjectiveCompleted(KillHarkonnen) and Harkonnen.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.Translate("atreides-05"), Mentat)
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if LastMercenariesArrived and not Atreides.IsObjectiveCompleted(KillSmuggler) and Smuggler.HasNoRequiredUnits() and Mercenary.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.Translate("smugglers-annihilated"), Mentat)
		Atreides.MarkCompletedObjective(KillSmuggler)
	end

	if LastHarvesterEaten[Harkonnen] and DateTime.GameTime % DateTime.Seconds(10) == 0 then
		local units = Harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end

	AttackNotifier = AttackNotifier - 1

	if TimerTicks and not ContrabandArrived then
		TimerTicks = TimerTicks - 1
		if (TimerTicks % DateTime.Seconds(1)) == 0 then
			local contrabandArrivesIn = UserInterface.Translate("contraband-arrives-in", { ["time"] = Utils.FormatTime(TimerTicks)})
			UserInterface.SetMissionText(contrabandArrivesIn, Atreides.Color)
		end

		if TimerTicks <= 0 then
			SendContraband(Smuggler)
		end
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Smuggler = Player.GetPlayer("Smugglers")
	Mercenary = Player.GetPlayer("Mercenaries")
	Atreides = Player.GetPlayer("Atreides")

	InfantryReinforcements = Difficulty ~= "easy"

	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Harkonnen, "")
	CaptureBarracks = AddPrimaryObjective(Atreides, "capture-barracks-sietch-tabr")
	KillHarkonnen = AddSecondaryObjective(Atreides, "annihilate-harkonnen-units-reinforcements")
	CaptureStarport = AddSecondaryObjective(Atreides, "capture-smuggler-starport-confiscate-contraband")

	Camera.Position = ARefinery.CenterPosition
	HarkonnenAttackLocation = AtreidesRally.Location
	MercenaryAttackLocation1 = Starport.Location + CVec.New(-16, 0)
	MercenaryAttackLocation2 = Starport.Location

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		TimerTicks = ContrabandTimes[Difficulty]
		local time = { ["time"] = Utils.FormatTime(TimerTicks) }
		local contrabandApproaching = UserInterface.Translate("contraband-approaching-starport-north-in", time)
		Media.DisplayMessage(contrabandApproaching, Mentat)
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnKilled(Starport, function()
		if not Atreides.IsObjectiveCompleted(CaptureStarport) then
			ContrabandArrived = true
			UserInterface.SetMissionText(UserInterface.Translate("starport-destroyed-no-contraband"), Atreides.Color)
			Atreides.MarkFailedObjective(CaptureStarport)
			SmugglersAttack()

			Trigger.AfterDelay(DateTime.Seconds(15), function()
				UserInterface.SetMissionText("")
			end)
		end

		if DefendStarport then
			Atreides.MarkFailedObjective(DefendStarport)
		end
	end)
	Trigger.OnDamaged(Starport, function()
		if Starport.Owner ~= Smuggler then
			return
		end

		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage(UserInterface.Translate("do-not-destroy-starport"), Mentat)

			local defenders = Smuggler.GetGroundAttackers()
			if #defenders > 0 then
				Utils.Do(defenders, function(unit)
					unit.Guard(Starport)
				end)
			end
		end
	end)
	Trigger.OnCapture(Starport, function()
		DefendStarport = AddSecondaryObjective(Atreides, "defend-captured-starport")

		Trigger.ClearAll(Starport)
		Trigger.AfterDelay(0, function()
			Trigger.OnRemovedFromWorld(Starport, function()
				Atreides.MarkFailedObjective(DefendStarport)
			end)
		end)

		if not ContrabandArrived then
			SendContraband(Atreides)
		end
		SmugglersAttack()
	end)

	Trigger.OnKilled(HarkonnenBarracks, function()
		Atreides.MarkFailedObjective(CaptureBarracks)
	end)
	Trigger.OnDamaged(HarkonnenBarracks, function()
		if AttackNotifier <= 0 and HarkonnenBarracks.Health < HarkonnenBarracks.MaxHealth * 3/4 then
			AttackNotifier = DateTime.Seconds(10)
			Media.DisplayMessage(UserInterface.Translate("do-not-destroy-barracks"), Mentat)
		end
	end)
	Trigger.OnCapture(HarkonnenBarracks, function()
		Media.DisplayMessage(UserInterface.Translate("hostages-released"), Mentat)

		if DefendStarport then
			Atreides.MarkCompletedObjective(DefendStarport)
		end

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			Atreides.MarkCompletedObjective(CaptureBarracks)
		end)
	end)

	SendHarkonnen()
	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		Media.PlaySpeechNotification(Atreides, "Reinforce")
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", AtreidesReinforcements, AtreidesPath, { AtreidesPath[1] })
	end)

	local smugglerWaypoint = SmugglerWaypoint1.Location
	Trigger.OnEnteredFootprint({ smugglerWaypoint + CVec.New(-2, 0), smugglerWaypoint + CVec.New(-1, 0), smugglerWaypoint, smugglerWaypoint + CVec.New(1, -1), smugglerWaypoint + CVec.New(2, -1), SmugglerWaypoint3.Location }, function(a, id)
		if not Warned and a.Owner == Atreides and a.Type ~= "carryall" then
			Warned = true
			Trigger.RemoveFootprintTrigger(id)
			Media.DisplayMessage(UserInterface.Translate("stay-away-from-starport"), UserInterface.Translate("smuggler-leader"))
		end
	end)

	Trigger.OnEnteredFootprint({ SmugglerWaypoint2.Location }, function(a, id)
		if not Paid and a.Owner == Atreides and a.Type ~= "carryall" then
			Paid = true
			Trigger.RemoveFootprintTrigger(id)
			Media.DisplayMessage(UserInterface.Translate("were-warned-will-pay"), UserInterface.Translate("smuggler-leader"))
			Utils.Do(Smuggler.GetGroundAttackers(), function(unit)
				unit.AttackMove(SmugglerWaypoint2.Location)
			end)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				KillSmuggler = AddSecondaryObjective(Atreides, "destroy-smugglers-mercenaries")
				SendMercenaries()
			end)
		end
	end)

	Trigger.OnEnteredProximityTrigger(HarkonnenBarracks.CenterPosition, WDist.New(5 * 1024), function(a, id)
		if a.Owner == Atreides and a.Type ~= "carryall" then
			Trigger.RemoveProximityTrigger(id)
			Media.DisplayMessage(UserInterface.Translate("capture-harkonnen-barracks-release-hostages"), Mentat)
			StopInfantryProduction = true
		end
	end)
end
