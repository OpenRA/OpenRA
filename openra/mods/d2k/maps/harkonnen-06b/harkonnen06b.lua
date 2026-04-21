--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = { OConYard1, OOutpost, ORefinery1, ORefinery2, OHeavyFactory, OLightFactory1, OHiTechFactory, ORepair, OStarport, OGunt1, OGunt2, OGunt3, OGunt4, OGunt5, OGunt6, ORocket1, ORocket2, OBarracks1, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10, OSilo1, OSilo2, OSilo3, OSilo4, OSilo5, OSilo6 }

OrdosSmallBase = { OConYard2, ORefinery3, OBarracks2, OLightFactory2, OGunt6, OGunt7, ORocket3, ORocket4, OPower11, OPower12, OPower13, OPower14, OSilo7, OSilo8, OSilo9 }

ProductionBuildingsOrdosSmallBase = { OConyard2, ORefinery3, OBarracks2, OLightFactory2 }

SmugglerTriggerZone = Utils.Concat(
	GetCellsInRectangle(CPos.New(48,24), CPos.New(53,29)),
	GetCellsInRectangle(CPos.New(43,30), CPos.New(53,35))
)

SmugglerPatrol = {Smuggler1, Smuggler2, Smuggler3, Smuggler4, Smuggler5, Smuggler6 }

OrdosReinforcements =
{
	easy =
	{
		{ "combat_tank_o", "light_inf", "raider" },
		{ "raider", "trooper" },
		{ "quad", "trooper", "trooper", "combat_tank_o"},
		{ "siege_tank", "quad" },
		{ "combat_tank_o", "combat_tank_o", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" }
	},

	normal =
	{
		{ "combat_tank_o", "raider", "raider" },
		{ "raider", "raider" },
		{ "quad", "trooper", "trooper", "trooper", "combat_tank_o"},
		{ "raider", "raider" },
		{ "siege_tank", "combat_tank_o" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "missile_tank" },
		{ "combat_tank_o", "combat_tank_o", "siege_tank" }
	},

	hard =
	{
		{ "combat_tank_o", "combat_tank_o", "raider" },
		{ "raider", "raider", "trooper" },
		{ "quad", "trooper", "trooper", "trooper", "trooper", "combat_tank_o"},
		{ "raider", "raider", "light_inf" },
		{ "siege_tank", "combat_tank_o", "quad" },
		{ "trooper", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" },
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
	{ OrdosEntry6.Location, OrdosRally6.Location }
}

InitialOrdosPaths =
{
	{ OrdosEntry7.Location, OrdosRally7.Location },
	{ OrdosEntry8.Location, OrdosRally8.Location },
	{ OrdosEntry9.Location, OrdosRally9.Location }
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

		Media.DisplayMessage(UserInterface.GetFluentMessage("ixian-transports-detected"), Mentat)

		SendStarportReinforcements()
	end)
end

Message = {}
StopFollowing = {}
Trigger.OnEnteredFootprint(SmugglerTriggerZone, function(a, id)
	if SmugglerNeutral.HasNoRequiredUnits() then
		Trigger.RemoveFootprintTrigger(id)
	end

	if a.Owner == Harkonnen and not Message[Harkonnen] then
		Media.DisplayMessage(UserInterface.GetFluentMessage("smuggler-warning-harkonnen"), "Smugglers", HSLColor.FromHex("7B2910"))
		Message[Harkonnen] = true
	end

	if a.Owner == OrdosMain or a.Owner == OrdosSmall and not Message[OrdosMain] then
		Media.DisplayMessage(UserInterface.GetFluentMessage("smuggler-warning-ordos"), "Smugglers", HSLColor.FromHex("7B2910"))
		Message[OrdosMain] = true
	end

	if a.Owner ~= SmugglerNeutral then
		Utils.Do(SmugglerPatrol, function(u)
			if not u.IsDead then
				StopFollowing[u] = false
				FollowIntruder(u,a)
			end
		end)
	end
end)

Trigger.OnExitedFootprint(SmugglerTriggerZone, function(a, id)
	if SmugglerNeutral.HasNoRequiredUnits() then
		Trigger.RemoveFootprintTrigger(id)
	end

	if a.Owner ~= SmugglerNeutral then
		Utils.Do(SmugglerPatrol, function(u)
			if not u.IsDead then StopFollowing[u] = true end
		end)
	end
end)

FollowIntruder = function(unit, intruder)
	if intruder.IsDead or unit.IsDead then return end
	if StopFollowing[unit] == true then unit.Stop() return end
	unit.Stop()
	unit.AttackMove(intruder.Location,1)
	Trigger.AfterDelay(100, function() FollowIntruder(unit, intruder) end)
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
			if Utils.Any(IgnoreDamageFromTypes, function(a) return a == attacker.Type end) then
				return
			end

			if not self.HasProperty("Health") then return end
			if self.MaxHealth * 0.9 < self.Health then return end

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
				Media.DisplayMessage(UserInterface.GetFluentMessage("smugglers-now-hostile"), Mentat)
			end
		end)
	end)
end

EmergencyBehaviour = function(player, target)
	if player ~= OrdosSmall then return end
	if #IdlingUnits[OrdosMain] > 10 and Utils.Any(OrdosSmallBase, function(a) return not a.IsDead end) then
		CheckArea(OrdosMain, target, 10)
	end
end

Tick = function()
	if Harkonnen.HasNoRequiredUnits() then
		OrdosMain.MarkCompletedObjective(KillHarkonnen1)
		OrdosSmall.MarkCompletedObjective(KillHarkonnen2)
	end

	if OrdosMain.HasNoRequiredUnits() and OrdosSmall.HasNoRequiredUnits() and not OrdosKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("ordos-annihilated"), Mentat)
		OrdosKilled = true
	end

	if SmugglerNeutral.HasNoRequiredUnits() and SmugglerHarkonnen.HasNoRequiredUnits() and SmugglerOrdos.HasNoRequiredUnits() and SmugglerBoth.HasNoRequiredUnits() and not SmugglersKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("smugglers-annihilated"), Mentat)
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
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		SmugglerUnits = SmugglerNeutral.GetActors()
		CheckSmugglerEnemies()
	end)

	Camera.Position = HConYard.CenterPosition
	OrdosAttackLocation = HConYard.Location

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
end
