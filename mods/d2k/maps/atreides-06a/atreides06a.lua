--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = { OPower1, OPower2, OPower3, OPower4, OPower5, OPower6, OPower7, OPower8, OPower9, OPower10, ORefinery1, ORefinery2, OBarracks1, OLightFactory1, OLightFactory2, OHeavyFactory, OLightFactory3, OBarracks2, OHighTechFactory1, ORepairPad }

OrdosSmallBase = { OStarport }

SmugglersBase = { SPower1, SPower2, SConyard, SBarracks, STurret1 }
OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o"},
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o"},
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" }
	},
	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "light_inf", "trooper" },
		{ "combat_tank_o", "combat_tank_o", "combat_tank_o", "missile_tank" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" }
	}
}

InitialStarportSquad =
{
	easy = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
	normal = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" },
	hard = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "light_inf", "trooper" }
}

OrdosStarportReinforcements =
{
	easy = { "combat_tank_o", "combat_tank_o" },
	normal = { "missile_tank", "combat_tank_o", "combat_tank_o" },
	hard = { "missile_tank", "missile_tank", "combat_tank_o", "combat_tank_o", "combat_tank_o" }
}

OrdosReinforcementsDelay =
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

InitialReinforcementsSquads =
{
	{ "trooper", "trooper", "quad", "quad", "light_inf", "raider" },
	{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
	{"combat_tank_h", "combat_tank_h", "trooper", "trooper"}
}

OrdosReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(OrdosReinforcementsPoint1.Location), OrdosReinforcementsPoint1.Location },
	{ Map.ClosestEdgeCell(OrdosReinforcementsPoint2.Location), OrdosReinforcementsPoint2.Location },
	{ Map.ClosestEdgeCell(OrdosReinforcementsPoint3.Location), OrdosReinforcementsPoint3.Location }
}

InitialPaths =
{
	{ Map.ClosestEdgeCell(OrdosUnitSpawn.Location), OrdosUnitSpawn.Location },
	{ Map.ClosestEdgeCell(SmugglerUnitSpawn.Location), SmugglerUnitSpawn.Location },
	{ Map.ClosestEdgeCell(OrdosSmallUnitSpawn.Location), OrdosSmallUnitSpawn.Location }
}

AtreidesReinforcements =
{
	easy = { "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank", "combat_tank_a", "missile_tank","missile_tank" },
	normal = { "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank", "missile_tank" },
	hard = { "combat_tank_a", "combat_tank_a", "missile_tank" }
}

AtreidesReinforcementsPatch = { Map.ClosestEdgeCell(AtreidesReinforcementsPoint.Location), AtreidesReinforcementsPoint.Location }
SendStarportReinforcements = function()
	Trigger.AfterDelay(OrdosStarportDelay[Difficulty], function()
		if OStarport.IsDead or OStarport.Owner ~= OrdosSmall then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(OrdosSmall, "frigate", OrdosStarportReinforcements[Difficulty], { Map.ClosestEdgeCell(OStarport.Location), OStarport.Location + CVec.New(1, 1) }, { Map.ClosestEdgeCell(OStarport.Location) })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(AttackLocation)
			IdleHunt(unit)
		end)

		SendStarportReinforcements()
	end)
end
InitStarportReinforcements = function (delay, squad)
	Trigger.AfterDelay(delay, function()
		if OStarport.IsDead or OStarport.Owner ~= OrdosSmall then
			return
		end

		local units = Reinforcements.ReinforceWithTransport(OrdosSmall, "frigate", squad, { Map.ClosestEdgeCell(OStarport.Location), OStarport.Location + CVec.New(1, 1) }, { Map.ClosestEdgeCell(OStarport.Location) })[2]
		Utils.Do(units, function(unit)
			unit.AttackMove(AttackLocation)
			IdleHunt(unit)
		end)
	end)
end
SendAtreidesReinforcements = function(delay)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", AtreidesReinforcements[Difficulty], AtreidesReinforcementsPatch, { AtreidesReinforcementsPatch[1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Atreides, "Reinforce")
		end)
	end)
end

Tick = function()

	if Atreides.HasNoRequiredUnits() then
		Ordos.MarkCompletedObjective(KillAtreides1)
		OrdosSmall.MarkCompletedObjective(KillAtreides2)
	end

	if Ordos.HasNoRequiredUnits() and OrdosSmall.HasNoRequiredUnits() and not OrdosKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("ordos-annihilated"), Mentat)
		OrdosKilled = true
		Atreides.MarkCompletedObjective(KillOrdos)
	end

	if Smugglers.HasNoRequiredUnits() and not SmugglersKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("smugglers-annihilated"), Mentat)
		SmugglersKilled = true
		Atreides.MarkCompletedObjective(KillSmugglers)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Ordos] then
		local units = Ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Ordos] = false
			ProtectHarvester(units[1], Ordos, AttackGroupSize[Difficulty])
		end
	end


end

WorldLoaded = function()

	Ordos = Player.GetPlayer("Ordos")
	Smugglers = Player.GetPlayer("Smugglers")
	OrdosSmall = Player.GetPlayer("OrdosSmall")
	Atreides = Player.GetPlayer("Atreides")
	Ordos.Cash = 12000
	OrdosSmall.Cash = 5000
	Smugglers.Cash = 3000
	InitObjectives(Atreides)
	KillOrdos = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-ordos"))
	KillOrdosStarport = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("capture-or-destroy-ordos-starport"))
	KillSmugglers = AddSecondaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-smugglers"))
	KillAtreides1 = AddPrimaryObjective(Ordos, "")
	KillAtreides2 = AddPrimaryObjective(OrdosSmall, "")

	Camera.Position = AMCV.CenterPosition
	AttackLocation = AMCV.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(Ordos.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(SmugglersBase, function()
		Utils.Do(Smugglers.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Atreides.MarkCompletedObjective(KillOrdosStarport)
		Media.DisplayMessage(UserInterface.GetFluentMessage("ordos-starport-destroyed"), Mentat)
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosReinforcementsPaths) end
	local waveCondition = function() return OrdosKilled end
	local huntFunction = function(unit)
		unit.AttackMove(AttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Ordos, 0, OrdosAttackWaves[Difficulty], OrdosReinforcementsDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()
	InitStarportReinforcements(DateTime.Minutes(3), InitialStarportSquad[Difficulty])
	Actor.Create("upgrade.barracks", true, { Owner = Ordos })
	Actor.Create("upgrade.light", true, { Owner = Ordos })
	Actor.Create("upgrade.heavy", true, { Owner = Ordos })
	Actor.Create("upgrade.barracks", true, { Owner = Smugglers })
	Trigger.AfterDelay(0, ActivateAI)

	SendAtreidesReinforcements(DateTime.Minutes(6))

end
