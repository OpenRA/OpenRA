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
	AtreidesMainBase = { AConyard, APower1, APower2, APower3, ABarracks1, ARefinery1, ALightFactory, AHeavyFactory, AGunt1, AGunt2, AGunt3, AGunt4, AGunt5 },
	AtreidesSmallBase1 = { APower4, APower5, ABarracks2, ARefinery2, AGunt6, AGunt7 },
	AtreidesSmallBase2 = { APower6, APower7, ABarracks3, AGunt8, AGunt9, AGunt10 },
	AtreidesSmallBase3 = { APower8, APower9, AStarport, AGunt11, AGunt12 }
}

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "combat_tank_a"},
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "quad"  },
		{ "quad", "quad", "trooper", "quad", "quad", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "quad", "siege_tank", "missile_tank" },
		{ "quad", "quad", "quad", "trooper", "trooper", "trooper", "trooper" }
	},

	normal =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "trike", "combat_tank_a"},
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a" },
		{ "quad", "quad", "quad", "quad", "quad", "quad" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "combat_tank_a", "quad", "siege_tank", "missile_tank" },
		{ "quad", "quad", "quad", "trooper", "trooper", "trooper", "trooper", "trooper" }
	},

	hard =
	{
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "quad", "trike", "trike", "combat_tank_a"},
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "quad" },
		{ "quad", "quad", "quad", "trooper", "quad", "quad", "quad", "trooper"  },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "combat_tank_a", "quad", "siege_tank", "siege_tank", "missile_tank" },
		{ "quad", "quad", "quad", "trooper", "trooper", "trooper", "trooper", "trooper", "trooper" }
	}
}

AtreidesAttackDelay =
{
	easy = DateTime.Minutes(3) + DateTime.Seconds(15),
	normal = DateTime.Minutes(2) + DateTime.Seconds(15),
	hard = DateTime.Minutes(1) + DateTime.Seconds(15)
}

AtreidesPaths =
{
	{ AtreidesEntry4.Location, AtreidesRally4.Location },
	{ AtreidesEntry5.Location, AtreidesRally5.Location },
	{ AtreidesEntry6.Location, AtreidesRally6.Location },
	{ AtreidesEntry7.Location, AtreidesRally7.Location },
	{ AtreidesEntry8.Location, AtreidesRally8.Location }
}

InitialReinforcements =
{
	AtreidesMainBase = { "combat_tank_a", "combat_tank_a", "quad", "quad", "trike", "light_inf", "light_inf", "light_inf", "light_inf" },
	AtreidesSmallBase1 = { "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf" },
	AtreidesSmallBase2 = { "trooper", "trooper", "trooper", "light_inf", "light_inf", "light_inf", "light_inf" }
}

InitialReinforcementsPaths =
{
	AtreidesMainBase = { AtreidesEntry1.Location, AtreidesRally1.Location },
	AtreidesSmallBase1 = { ABarracks2.Location, AtreidesRally2.Location },
	AtreidesSmallBase2 = { ABarracks3.Location, AtreidesRally3.Location }
}

ToHarvest =
{
	easy = 12500,
	normal = 15000,
	hard = 20000
}

Hunt = function(house)
	Trigger.OnAllKilledOrCaptured(Base[house.InternalName], function()
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

CachedResources = -1
Tick = function()
	if Ordos.Resources > SpiceToHarvest - 1 then
		Ordos.MarkCompletedObjective(GatherSpice)
	end

	if Ordos.HasNoRequiredUnits() then
		AtreidesMain.MarkCompletedObjective(KillOrdos1)
		AtreidesSmall1.MarkCompletedObjective(KillOrdos2)
		AtreidesSmall2.MarkCompletedObjective(KillOrdos3)
		AtreidesSmall3.MarkCompletedObjective(KillOrdos4)
	end

	if AtreidesMain.HasNoRequiredUnits() and AtreidesSmall1.HasNoRequiredUnits() and AtreidesSmall2.HasNoRequiredUnits() and AtreidesSmall3.HasNoRequiredUnits() and not Ordos.IsObjectiveCompleted(KillAtreides) then
		Media.DisplayMessage(UserInterface.Translate("atreides-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillAtreides)
	end

	if #Ordos.GetActorsByType("engineer") == 0 and not Ordos.IsObjectiveCompleted(CaptureStarport) then
		Ordos.MarkFailedObjective(CaptureStarport)
	end

	if Ordos.IsObjectiveCompleted(CaptureStarport) then
		if Ordos.Resources ~= CachedResources then
			local parameters = { ["harvested"] = Ordos.Resources, ["goal"] = SpiceToHarvest }
			local harvestedResources = UserInterface.Translate("harvested-resources", parameters)
			UserInterface.SetMissionText(harvestedResources)
			CachedResources = Ordos.Resources
		end
	end

	CheckHarvester(AtreidesMain)
	CheckHarvester(AtreidesSmall1)
end

WorldLoaded = function()
	AtreidesMain = Player.GetPlayer("AtreidesMainBase")
	AtreidesSmall1 = Player.GetPlayer("AtreidesSmallBase1")
	AtreidesSmall2 = Player.GetPlayer("AtreidesSmallBase2")
	AtreidesSmall3 = Player.GetPlayer("AtreidesSmallBase3")
	Ordos = Player.GetPlayer("Ordos")

	SpiceToHarvest = ToHarvest[Difficulty]

	InitObjectives(Ordos)
	KillOrdos1 = AddPrimaryObjective(AtreidesMain, "")
	KillOrdos2 = AddPrimaryObjective(AtreidesSmall1, "")
	KillOrdos3 = AddPrimaryObjective(AtreidesSmall2, "")
	KillOrdos4 = AddPrimaryObjective(AtreidesSmall3, "")
	CaptureStarport = AddPrimaryObjective(Ordos, "capture-atreides-starport-establish-base")
	local harvestSpice = UserInterface.Translate("harvest-spice", { ["spice"] = SpiceToHarvest })
	GatherSpice = AddPrimaryObjective(Ordos, harvestSpice)
	KillAtreides = AddSecondaryObjective(Ordos, "destroy-atreides")

	Camera.Position = OEngi1.CenterPosition
	AtreidesAttackLocation = OEngi1.Location

	if Difficulty ~= "easy" then
		OTrooper3.Destroy()
	end
	if Difficulty == "hard" then
		OTrooper4.Destroy()
		OCombat2.Destroy()
	end

	Hunt(AtreidesMain)
	Hunt(AtreidesSmall1)
	Hunt(AtreidesSmall2)
	Hunt(AtreidesSmall3)

	local path = function() return Utils.Random(AtreidesPaths) end
	local waveCondition = function() return Ordos.IsObjectiveCompleted(KillAtreides) end
	local huntFunction = function(unit)
		unit.AttackMove(AtreidesAttackLocation)
		IdleHunt(unit)
	end
	SendCarryallReinforcements(AtreidesMain, 0, 8, AtreidesAttackDelay[Difficulty], path, AtreidesReinforcements[Difficulty], waveCondition, huntFunction)

	Actor.Create("upgrade.barracks", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.light", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.heavy", true, { Owner = AtreidesMain })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesSmall1 })
	Actor.Create("upgrade.barracks", true, { Owner = AtreidesSmall2 })
	Trigger.AfterDelay(0, ActivateAI)

	Trigger.OnKilled(AStarport, function()
		if not Ordos.IsObjectiveCompleted(CaptureStarport) then
			Ordos.MarkFailedObjective(CaptureStarport)
		end
	end)

	Trigger.OnCapture(AStarport, function()
		Ordos.MarkCompletedObjective(CaptureStarport)

		if not AIProductionActivated then
			ActivateAIProduction()
		end

		Reinforcements.ReinforceWithTransport(Ordos, "frigate", { "mcv" }, { OrdosStarportEntry.Location, AStarport.Location + CVec.New(1, 1) }, { OrdosStarportExit.Location })

		if APower8.Owner ~= Ordos and not APower8.IsDead then
			APower8.Sell()
		end
		if APower9.Owner ~= Ordos and not APower9.IsDead then
			APower9.Sell()
		end
	end)
	Trigger.OnKilledOrCaptured(APower8, function()
		if not AIProductionActivated then
			ActivateAIProduction()
		end
	end)
	Trigger.OnKilledOrCaptured(APower9, function()
		if not AIProductionActivated then
			ActivateAIProduction()
		end
	end)
end
