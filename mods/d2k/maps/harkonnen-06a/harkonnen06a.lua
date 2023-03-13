--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = { OOutpost, ORefinery1, ORefinery2, OHeavyFactory1, OHeavyFactory2, OLightFactory1, OHiTechFactory, OResearch, ORepair, OStarport, OGunt1, OGunt2, OGunt3, OGunt4, OGunt5, OGunt6, OGunt7, OGunt8, OGunt9, OGunt10, OGunt11, OGunt12, OBarracks1, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10 }
OrdosSmallBase = { OConyard, ORefinery3, OBarracks2, OLightFactory2, OGunt13, OGunt14, OGunt15, OGunt16, OPower11, OPower12, OPower13, OPower14 }

OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "quad", "raider", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "quad", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o" },
		{ "quad", "quad", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "siege_tank" }
	},

	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "combat_tank_o", "combat_tank_o", "raider" },
		{ "quad", "quad", "raider", "raider" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "siege_tank", "siege_tank" },
		{ "missile_tank", "quad", "quad", "raider", "raider" }
	}
}

OrdosStarportReinforcements =
{
	easy = { "raider", "missile_tank", "combat_tank_o", "quad", "deviator", "deviator" },
	normal = { "raider", "missile_tank", "missile_tank", "quad", "deviator", "deviator" },
	hard = { "raider", "raider", "missile_tank", "missile_tank", "quad", "quad", "deviator", "deviator" }
}

OrdosAttackDelay =
{
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2) + DateTime.Seconds(20),
	hard = DateTime.Minutes(1)
}

OrdosStarportDelay =
{
	easy = DateTime.Minutes(7),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(5)
}

OrdosAttackWaves =
{
	easy = 7,
	normal = 8,
	hard = 9
}

InitialOrdosReinforcements =
{
	{ "trooper", "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	{ "combat_tank_o", "combat_tank_o", "quad", "quad", "raider", "raider" }
}

OrdosPaths =
{
	{ OrdosEntry1.Location, OrdosRally1.Location },
	{ OrdosEntry2.Location, OrdosRally2.Location },
	{ OrdosEntry3.Location, OrdosRally3.Location },
	{ OrdosEntry4.Location, OrdosRally4.Location },
	{ OrdosEntry5.Location, OrdosRally5.Location },
	{ OrdosEntry6.Location, OrdosRally6.Location },
	{ OrdosEntry7.Location, OrdosRally7.Location }
}

InitialOrdosPaths =
{
	{ OrdosEntry8.Location, OrdosRally8.Location },
	{ OrdosEntry9.Location, OrdosRally9.Location },
	{ OrdosEntry10.Location, OrdosRally10.Location }
}

HarkonnenReinforcements =
{
	{ "combat_tank_h", "combat_tank_h" },
	{ "missile_tank", "missile_tank" }
}

HarkonnenPaths =
{
	{ HarkonnenEntry1.Location, HarkonnenRally1.Location },
	{ HarkonnenEntry2.Location, HarkonnenRally2.Location }
}

SendStarportReinforcements = function()
	Trigger.AfterDelay(OrdosStarportDelay[Difficulty], function()
		if OStarport.IsDead or OStarport.Owner ~= OrdosMain then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(OrdosMain, "frigate", OrdosStarportReinforcements[Difficulty], { OrdosStarportEntry.Location, OStarport.Location + CVec.New(1, 1) }, { OrdosStarportExit.Location })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(OrdosAttackLocation)
			IdleHunt(unit)
		end)

		Media.DisplayMessage(UserInterface.Translate("ixian-transports-detected"), Mentat)

		SendStarportReinforcements()
	end)
end

SendHarkonnenReinforcements = function(delay, number)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(Harkonnen, "carryall.reinforce", HarkonnenReinforcements[number], HarkonnenPaths[number], { HarkonnenPaths[number][1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Harkonnen, "Reinforce")
		end)
	end)
end

ChangeOwner = function(old_owner, new_owner)
	local units = old_owner.GetActors()
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			unit.Owner = new_owner
		end
	end)
end

CheckSmugglerEnemies = function()
	Utils.Do(SmugglerUnits, function(unit)
		Trigger.OnDamaged(unit, function(self, attacker)
			if unit.Owner == SmugglerNeutral and attacker.Owner == Harkonnen then
				ChangeOwner(SmugglerNeutral, SmugglerHarkonnen)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerNeutral, SmugglerHarkonnen)
				end)
			end

			if unit.Owner == SmugglerOrdos and attacker.Owner == Harkonnen then
				ChangeOwner(SmugglerOrdos, SmugglerBoth)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerOrdos, SmugglerBoth)
				end)
			end

			if unit.Owner == SmugglerNeutral and (attacker.Owner == OrdosMain or attacker.Owner == OrdosSmall) then
				ChangeOwner(SmugglerNeutral, SmugglerOrdos)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerNeutral, SmugglerOrdos)
				end)
			end

			if unit.Owner == SmugglerHarkonnen and (attacker.Owner == OrdosMain or attacker.Owner == OrdosSmall) then
				ChangeOwner(SmugglerHarkonnen, SmugglerBoth)

				--	Ensure that harvesters that was on a carryall switched sides.
				Trigger.AfterDelay(DateTime.Seconds(15), function()
					ChangeOwner(SmugglerHarkonnen, SmugglerBoth)
				end)
			end

			if attacker.Owner == Harkonnen and not MessageCheck then

				MessageCheck = true
				Media.DisplayMessage(UserInterface.Translate("smugglers-now-hostile"), Mentat)
			end
		end)
	end)
end

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		OrdosMain.MarkCompletedObjective(KillHarkonnen1)
		OrdosSmall.MarkCompletedObjective(KillHarkonnen2)
	end

	if OrdosMain.HasNoRequiredUnits() and OrdosSmall.HasNoRequiredUnits() and not OrdosKilled then
		Media.DisplayMessage(UserInterface.Translate("ordos-annihilated"), Mentat)
		OrdosKilled = true
	end

	if SmugglerNeutral.HasNoRequiredUnits() and SmugglerHarkonnen.HasNoRequiredUnits() and SmugglerOrdos.HasNoRequiredUnits() and SmugglerBoth.HasNoRequiredUnits() and not SmugglersKilled then
		Media.DisplayMessage(UserInterface.Translate("smugglers-annihilated"), Mentat)
		SmugglersKilled = true
	end

	if (OStarport.IsDead or OStarport.Owner == Harkonnen) and not Harkonnen.IsObjectiveCompleted(DestroyStarport) then
		Harkonnen.MarkCompletedObjective(DestroyStarport)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[OrdosMain] then
		local units = OrdosMain.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[OrdosMain] = false
			ProtectHarvester(units[1], OrdosMain, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[OrdosSmall] then
		local units = OrdosSmall.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[OrdosSmall] = false
			ProtectHarvester(units[1], OrdosSmall, AttackGroupSize[Difficulty])
		end
	end
end

WorldLoaded = function()
	OrdosMain = Player.GetPlayer("Ordos Main Base")
	OrdosSmall = Player.GetPlayer("Ordos Small Base")
	SmugglerNeutral = Player.GetPlayer("Smugglers - Neutral")
	SmugglerHarkonnen = Player.GetPlayer("Smugglers - Enemy to Harkonnen")
	SmugglerOrdos = Player.GetPlayer("Smugglers - Enemy to Ordos")
	SmugglerBoth = Player.GetPlayer("Smugglers - Enemy to Both")
	Harkonnen = Player.GetPlayer("Harkonnen")

	InitObjectives(Harkonnen)
	DestroyStarport = AddPrimaryObjective(Harkonnen, "capture-destroy-ordos-starport")
	KillHarkonnen1 = AddPrimaryObjective(OrdosMain, "")
	KillHarkonnen2 = AddPrimaryObjective(OrdosSmall, "")

	-- Wait for carryall drop
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		SmugglerUnits = SmugglerNeutral.GetActors()
		CheckSmugglerEnemies()
	end)

	Camera.Position = HMCV.CenterPosition
	OrdosAttackLocation = HMCV.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(OrdosMain.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosPaths) end
	local waveCondition = function() return OrdosKilled end
	local huntFunction = function(unit)
		unit.AttackMove(OrdosAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(OrdosMain, 0, OrdosAttackWaves[Difficulty], OrdosAttackDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()

	Actor.Create("upgrade.barracks", true, { Owner = OrdosMain })
	Actor.Create("upgrade.light", true, { Owner = OrdosMain })
	Actor.Create("upgrade.heavy", true, { Owner = OrdosMain })
	Actor.Create("upgrade.barracks", true, { Owner = OrdosSmall })
	Actor.Create("upgrade.light", true, { Owner = OrdosSmall })
	Trigger.AfterDelay(0, ActivateAI)

	SendHarkonnenReinforcements(DateTime.Minutes(2) + DateTime.Seconds(15), 2)
	SendHarkonnenReinforcements(DateTime.Minutes(2) + DateTime.Seconds(45), 1)
	SendHarkonnenReinforcements(DateTime.Minutes(4) + DateTime.Seconds(30), 2)
end
