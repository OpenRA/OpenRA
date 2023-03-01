--[[
   Copyright (c) The OpenRA Developers and Contributors
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
	Fremen.Build({ "nsfremen" }, function()
		Trigger.AfterDelay(delay, FremenProduction)
	end)
end

AttackNotifier = 0
Tick = function()
	if Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if Harkonnen.HasNoRequiredUnits() and not Atreides.IsObjectiveCompleted(KillHarkonnen) then
		Media.DisplayMessage(UserInterface.Translate("harkonnen-annihilated"), Mentat)
		Atreides.MarkCompletedObjective(KillHarkonnen)
		Atreides.MarkCompletedObjective(ProtectFremen)
		Atreides.MarkCompletedObjective(KeepIntegrity)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Harkonnen] then
		local units = Harkonnen.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Harkonnen] = false
			ProtectHarvester(units[1], Harkonnen, AttackGroupSize[Difficulty])
		end
	end

	if not Sietch.IsDead then
		AttackNotifier = AttackNotifier - 1
		local integrity = math.floor((Sietch.Health * 100) / Sietch.MaxHealth)
		SiegeIntegrity = UserInterface.Translate("sietch-integrity", { ["integrity"] = integrity })
		UserInterface.SetMissionText(SiegeIntegrity, Atreides.Color)

		if integrity < IntegrityLevel[Difficulty] then
			Atreides.MarkFailedObjective(KeepIntegrity)
		end
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Fremen = Player.GetPlayer("Fremen")
	Atreides = Player.GetPlayer("Atreides")

	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Harkonnen, "")
	ProtectFremen = AddPrimaryObjective(Atreides, "protect-fremen-sietch")
	KillHarkonnen = AddPrimaryObjective(Atreides, "destroy-harkonnen")
	local keepSietchIntact = UserInterface.Translate("keep-sietch-intact", { ["integrity"] = IntegrityLevel[Difficulty] })
	KeepIntegrity = AddPrimaryObjective(Atreides, keepSietchIntact)

	Camera.Position = AConyard.CenterPosition
	HarkonnenAttackLocation = AConyard.Location

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Beacon.New(Atreides, Sietch.CenterPosition + WVec.New(0, 1024, 0))
		Media.DisplayMessage(UserInterface.Translate("fremen-sietch-southeast"), Mentat)
	end)

	Trigger.OnAllKilledOrCaptured(HarkonnenBase, function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnKilled(Sietch, function()
		Actor.Create("invisibleBlocker", true, { Owner = Fremen, Location = CPos.New(62, 59) })
		UserInterface.SetMissionText(UserInterface.Translate("sietch-destroyed"), Atreides.Color)
		Atreides.MarkFailedObjective(ProtectFremen)
	end)
	Trigger.OnDamaged(Sietch, function()
		if AttackNotifier <= 0 then
			AttackNotifier = DateTime.Seconds(10)
			Beacon.New(Atreides, Sietch.CenterPosition + WVec.New(0, 1024, 0), DateTime.Seconds(7))
			Media.DisplayMessage(UserInterface.Translate("fremen-sietch-under-attack"), Mentat)

			local defenders = Fremen.GetGroundAttackers()
			if #defenders > 0 then
				Utils.Do(defenders, function(unit)
					unit.Guard(Sietch)
				end)
			end
		end
	end)

	local path = function() return Utils.Random(HarkonnenPaths) end
	local waveCondition = function() return Atreides.IsObjectiveCompleted(KillHarkonnen) end
	local huntFunction = function(unit)
		unit.AttackMove(HarkonnenAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Harkonnen, 0, HarkonnenAttackWaves[Difficulty], HarkonnenAttackDelay[Difficulty], path, HarkonnenReinforcements[Difficulty], waveCondition, huntFunction)

	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.AfterDelay(DateTime.Seconds(50), function()
		Media.PlaySpeechNotification(Atreides, "Reinforce")
		Reinforcements.Reinforce(Atreides, AtreidesReinforcements[1], AtreidesPath)
	end)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), function()
		Media.PlaySpeechNotification(Atreides, "Reinforce")
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", AtreidesReinforcements[2], AtreidesPath, { AtreidesPath[1] })
	end)

	Trigger.OnEnteredProximityTrigger(HarkonnenRally1.CenterPosition, WDist.New(6 * 1024), function(a, id)
		if a.Owner == Atreides then
			Trigger.RemoveProximityTrigger(id)
			local units = Reinforcements.Reinforce(Harkonnen, { "light_inf", "combat_tank_h", "trike" }, HarkonnenPaths[1])
			Utils.Do(units, IdleHunt)
		end
	end)

	Trigger.OnExitedProximityTrigger(Sietch.CenterPosition, WDist.New(10.5 * 1024), function(a, id)
		if a.Owner == Fremen and not a.IsDead then
			a.AttackMove(FremenRally.Location)
			Trigger.OnIdle(a, function()
				if a.Location.X < 54 or a.Location.Y < 54 then
					a.AttackMove(FremenRally.Location)
				end
			end)
		end
	end)
end
