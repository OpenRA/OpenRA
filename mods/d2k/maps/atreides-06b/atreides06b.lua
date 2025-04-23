--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

OrdosMainBase = {OTurret1, OTurret2, OTurret3, OTurret4, ORepairPad, ORefinery1, ORefinery2, OConyard, OHeavyFactory, OLightFactory, OHighTech1, OHighTech2, OBarracks, OOutpost, OPower1, OPower2, OPower3, OPower4, OPower5, OPower6}

SmugglersBase = { STurret1, STurret2, STurret3, SPower1, SPower2, SPower3, SPower4, SPower5, SPower5, SBarracks, SLightFactory}

MercenariesBase = {MTurret1, MTurret2, MTurret3, MConyard, MPower1, MPower2, MPower3, MOutpost, MLightFactory, MBarracks, MRefinery}

OrdosSmallBase = { OStarport }

OrdosReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf","raider" },
		{ "combat_tank_o", "trooper", "trooper", "trooper" },
		{ "raider", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf","raider" },
		{ "combat_tank_o", "trooper", "trooper", "trooper" },
		{ "raider","combat_tank_o", "trooper", "trooper", "trooper" },
		{ "raider", "raider", "trooper", "trooper", "trooper" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "trooper", "trooper", "trooper", "trooper" },
		{ "raider", "raider", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "trooper", "trooper", "trooper", "trooper" },
		{ "raider", "raider", "trooper", "trooper", "trooper", "trooper", "combat_tank_o", },
		{ "light_inf", "combat_tank_o", "siege_tank", "light_inf", "raider", "raider" },
		{ "light_inf", "siege_tank", "missile_tank", "light_inf", "raider", "raider" },
	},
	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "trooper", "trooper", "trooper", "trooper", "combat_tank_o" },
		{ "raider", "raider", "trooper", "trooper", "trooper", "trooper","raider", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "trooper", "trooper", "trooper", "trooper", "combat_tank_o", "trooper" },
		{ "raider", "raider", "trooper", "trooper", "trooper", "trooper","raider", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "raider", "raider", "raider" },
		{ "combat_tank_o", "combat_tank_o", "trooper", "trooper", "trooper", "trooper", "combat_tank_o", "siege_tank", "missile_tank" },
		{ "raider", "raider", "trooper", "trooper", "trooper", "trooper","raider", "siege_tank", "missile_tank" }
	}
}

OrdosStarportReinforcements =
{
	easy ={ "combat_tank_o", "raider", "light_inf", "light_inf" },
	normal = { "missile_tank", "combat_tank_o", "raider", "raider", "light_inf", "light_inf" },
	hard = { "missile_tank", "combat_tank_o", "combat_tank_o", "raider", "raider", "light_inf", "light_inf", "light_inf", "light_inf"}
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
	ordos = { "trooper", "trooper", "quad", "light_inf", "raider", "raider" },
	smugglers = { "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
	ordosSmall = {"combat_tank_h", "trooper", "trooper"},
	mercenaries = {"combat_tank_h", "combat_tank_h", "combat_tank_h", "trooper", "trooper"}
}

AtreidesReinforcements1 =
{
	easy = { "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	normal = { "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
	hard = { "light_inf", "light_inf", "light_inf", "light_inf" }
}

AtreidesReinforcements2 =
{
	easy = { "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank", "combat_tank_a", "missile_tank","missile_tank", "combat_tank_a", "missile_tank" },
	normal = { "combat_tank_a", "combat_tank_a", "combat_tank_a", "combat_tank_a", "missile_tank", "missile_tank", "missile_tank" },
	hard = { "combat_tank_a", "combat_tank_a", "missile_tank", "combat_tank_a", "missile_tank" }
}

InitialPaths =
{

	-- fix bug where ClosestEdgeCell return wrong cell for bottom/Right corner
	ordos = { CPos.New( Map.ClosestEdgeCell(OrdosUnitSpawn.Location).X - 1, Map.ClosestEdgeCell(OrdosUnitSpawn.Location).Y - 1), OrdosUnitSpawn.Location },

	-- fix bug where ClosestEdgeCell return wrong cell for bottom/Right corner
	smugglers = { CPos.New( Map.ClosestEdgeCell(SmugglersUnitSpawn.Location).X - 1, Map.ClosestEdgeCell(SmugglersUnitSpawn.Location).Y - 1), SmugglersUnitSpawn.Location },
	-- fix bug where ClosestEdgeCell return wrong cell for bottom/Right corner`
	ordosSmall = { CPos.New( Map.ClosestEdgeCell(OrdosSmallUnitSpawn.Location).X - 1, Map.ClosestEdgeCell(OrdosSmallUnitSpawn.Location).Y), OrdosSmallUnitSpawn.Location },
	mercenaries = { Map.ClosestEdgeCell(MercenariesUnitSpawn.Location), MercenariesUnitSpawn.Location }
}

OrdosReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(OrdosReinforcements1.Location), OrdosReinforcements1.Location },
	{ Map.ClosestEdgeCell(OrdosReinforcements2.Location), OrdosReinforcements2.Location },
	{ Map.ClosestEdgeCell(OrdosReinforcements3.Location), OrdosReinforcements3.Location }
}

AtreidesReinforcementsPaths =
{
	{ Map.ClosestEdgeCell(AtreidesReinforcementsPoint1.Location), AtreidesReinforcementsPoint1.Location},
	{ Map.ClosestEdgeCell(AtreidesReinforcementsPoint2.Location), AtreidesReinforcementsPoint2.Location}

}

SendAtreidesReinforcements = function(delay, squad, path)
	Trigger.AfterDelay(delay, function()
		Reinforcements.ReinforceWithTransport(Atreides, "carryall.reinforce", squad, path, { path[1] })
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(Atreides, "Reinforce")
		end)
	end)
end

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
		Smugglers.Cash = Smugglers.Cash + 500
		SendStarportReinforcements()
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

	if Mercenaries.HasNoRequiredUnits() and not MercenariesKilled then
		Media.DisplayMessage(UserInterface.GetFluentMessage("mercenaries-annihilated"), Mentat)
		MercenariesKilled = true
		Atreides.MarkCompletedObjective(KillMercenaries)
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Ordos] then
		local units = Ordos.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Ordos] = false
			ProtectHarvester(units[1], Ordos, AttackGroupSize[Difficulty])
		end
	end

	if DateTime.GameTime % DateTime.Seconds(10) == 0 and LastHarvesterEaten[Mercenaries] then
		local units = Mercenaries.GetActorsByType("harvester")

		if #units > 0 then
			LastHarvesterEaten[Mercenaries] = false
			ProtectHarvester(units[1], Mercenaries, AttackGroupSize[Difficulty])
		end
	end

end

WorldLoaded = function()

	Ordos = Player.GetPlayer("Ordos")
	Smugglers = Player.GetPlayer("Smugglers")
	OrdosSmall = Player.GetPlayer("OrdosSmall")
	Mercenaries = Player.GetPlayer("Mercenaries")
	Atreides = Player.GetPlayer("Atreides")
	Ordos.Cash = 12000
	OrdosSmall.Cash = 6000
	Smugglers.Cash = 6000
	Mercenaries.Cash = 6000
	InitObjectives(Atreides)
	KillOrdos = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-ordos"))
	KillOrdosStarport = AddPrimaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-ordos-starport"))
	KillSmugglers = AddSecondaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-smugglers"))
	KillMercenaries = AddSecondaryObjective(Atreides, UserInterface.GetFluentMessage("destroy-mercenaries"))
	KillAtreides1 = AddPrimaryObjective(Ordos, "")
	KillAtreides2 = AddPrimaryObjective(OrdosSmall, "")
	Camera.Position = AConyard.CenterPosition
	AttackLocation = AConyard.Location

	Trigger.OnAllKilledOrCaptured(OrdosMainBase, function()
		Utils.Do(Ordos.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(SmugglersBase, function()
		Utils.Do(Smugglers.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(OrdosSmallBase, function()
		Atreides.MarkCompletedObjective(KillOrdosStarport)
		Media.DisplayMessage(UserInterface.GetFluentMessage("starport-destroyed"), Mentat)
		Utils.Do(OrdosSmall.GetGroundAttackers(), IdleHunt)
	end)
	Trigger.OnAllKilledOrCaptured(MercenariesBase, function()
		Utils.Do(Mercenaries.GetGroundAttackers(), IdleHunt)
	end)

	local path = function() return Utils.Random(OrdosReinforcementsPaths) end
	local waveCondition = function() return OrdosKilled end
	local huntFunction = function(unit)
		unit.AttackMove(AttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(Ordos, 0, OrdosAttackWaves[Difficulty], OrdosReinforcementsDelay[Difficulty], path, OrdosReinforcements[Difficulty], waveCondition, huntFunction)

	SendStarportReinforcements()
	SendAtreidesReinforcements(DateTime.Minutes(1), AtreidesReinforcements1[Difficulty], AtreidesReinforcementsPaths[1])
	SendAtreidesReinforcements(DateTime.Minutes(5), AtreidesReinforcements2[Difficulty], AtreidesReinforcementsPaths[2])
	Actor.Create("upgrade.barracks", true, { Owner = Ordos })
	Actor.Create("upgrade.light", true, { Owner = Ordos })
	Actor.Create("upgrade.heavy", true, { Owner = Ordos })
	Actor.Create("upgrade.light", true, { Owner = Mercenaries })
	Actor.Create("upgrade.barracks", true, { Owner = Smugglers })
	Actor.Create("upgrade.barracks", true, { Owner = Mercenaries })
	Trigger.AfterDelay(0, ActivateAI)
end
