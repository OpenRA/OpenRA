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
	Atreides = { AConyard, APower1, APower2, APower3, APower4, APower5, APower6, APower7, APower8, APower9, APower10, ABarracks1, ABarracks2,  ARefinery, ALightFactory, AHeavyFactory, ARepair, AHighTech, AOutpost, Aturret1, Aturret2, Aturret3, Aturret4, Aturret5, Aturret6, Aturret7, Aturret8, ARock2, ARock3, ARock4, ASilo1, ASilo2, ASilo3, ASilo4 },
	Harkonnen = { HConyard, HPower1, HPower2, HPower3, HPower4, HPower5, HPower6, HPower7, HBarracks, HRefinery, HOutpost, HHeavyFactory, HLightFactory, HRepair, HTurret1, HTurret2, HTurret3, HTurret4, HTurret5, HSilo1, HSilo2, HSilo3, HSilo4 }
}

AtreidesReinforcements =
{
	easy =
	{
		{  "trike", "quad", "combat_tank_a", "trooper" },
		{ "quad", "trike" , "trike", "trike", "combat_tank_a", "combat_tank_a", "trooper" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "siege_tank", "siege_tank" }
	},

	normal =
	{
		{  "trike", "quad", "combat_tank_a", "trooper", "trooper" },
		{ "quad", "trike" , "trike", "trike", "combat_tank_a", "combat_tank_a", "trooper", "quad", "trike" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "siege_tank", "siege_tank", "combat_tank_a" }
	},

	hard =
	{
		{  "trike", "quad", "combat_tank_a", "trooper", "combat_tank_a", "trooper" },
		{ "quad", "trike" , "trike", "trike", "combat_tank_a", "combat_tank_a", "trooper", "combat_tank_a", "combat_tank_a" },
		{ "combat_tank_a", "combat_tank_a", "combat_tank_a", "siege_tank", "siege_tank", "siege_tank", "siege_tank" }
	}
}

HarkonnenReinforcements =
{
	easy =
	{
		{ "quad", "quad", "trike" },
		{ "trike", "trike" },
		{ "combat_tank_h", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" },
		{ "quad", "trike", "trike" },
		{ "quad", "quad", "quad", "trike", "trike" },
		{ "trike", "trike", "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "light_inf" }
	},

	normal =
	{
		{ "quad", "quad", "trike", "trike" },
		{ "trike", "trike", "trike" },
		{ "combat_tank_h", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper" },
		{ "quad", "trike", "trike", "quad" },
		{ "quad", "quad", "quad", "trike", "trike", "trike" },
		{ "trike", "trike", "trike", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
	},

	hard =
	{
		{ "quad", "quad", "trike", "trike" },
		{ "trike", "trike", "trike" },
		{ "combat_tank_h", "quad", "combat_tank_h", "combat_tank_h" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" },
		{ "quad", "trike", "trike", "quad", "trike", "quad" },
		{ "quad", "quad", "quad", "trike", "trike", "trike", "quad" },
		{ "trike", "trike", "trike", "quad", "quad" },
		{ "light_inf", "light_inf", "light_inf", "light_inf", "light_inf", "trooper", "trooper", "trooper", "trooper" }
	}
}

IxianReinforcements =
{
	easy = { "deviator", "deviator", "missile_tank", "missile_tank", "missile_tank", "siege_tank", "combat_tank_o", "combat_tank_o" },
	normal = { "deviator", "deviator", "missile_tank", "missile_tank", "combat_tank_o", "siege_tank", "combat_tank_o" },
	hard = { "deviator", "deviator", "missile_tank", "missile_tank", "siege_tank", "combat_tank_o" }
}

EnemyAttackDelay =
{
	easy = DateTime.Minutes(5) + DateTime.Seconds(15),
	normal = DateTime.Minutes(3) + DateTime.Seconds(30),
	hard = DateTime.Minutes(2) + DateTime.Seconds(30)
}

AtreidesPaths =
{
	{ Map.ClosestEdgeCell(AtreidesRally1.Location), AtreidesRally1.Location },
	{ Map.ClosestEdgeCell(AtreidesRally2.Location), AtreidesRally2.Location },
	{ Map.ClosestEdgeCell(AtreidesRally3.Location), AtreidesRally3.Location }
}

HarkonnenPaths =
{
	{ Map.ClosestEdgeCell(HarkonnenRally1.Location), HarkonnenRally1.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally2.Location), HarkonnenRally2.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally3.Location), HarkonnenRally3.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally4.Location), HarkonnenRally4.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally5.Location), HarkonnenRally5.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally6.Location), HarkonnenRally6.Location },
	{ Map.ClosestEdgeCell(HarkonnenRally7.Location), HarkonnenRally6.Location }
}

AtreidesAttackWaves =
{
	easy = 1,
	normal = 2,
	hard = 3
}

HarkonnenAttackWaves =
{
	easy = 4,
	normal = 6,
	hard = 8
}

InitialReinforcements =
{
	Atreides = { "combat_tank_a","combat_tank_a", "quad", "quad", "trike", "trike" },
	Harkonnen = { "trooper", "trooper", "trooper", "trooper", "trooper", "combat_tank_h", "combat_tank_h" }
}

InitialReinforcementsPaths =
{
	Atreides = { CPos.New(40, 2), AtreidesInitialSpawn.Location },
	Harkonnen = { CPos.New(76, 89), HarkonnenInitialSpawn.Location }
}

InitialContrabandTimes =
{
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(20)
}

ContrabandTimes =
{
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(6),
	hard = DateTime.Minutes(7)
}

IxianReinforcementsHaveArrived = UserInterface.GetFluentMessage("ixian-reinforcements-arrived")
SendContraband = function()
	Media.PlaySpeechNotification(Ordos, "Reinforce")

	for i = 0, 6 do
		local c = Ordos.Color
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText(IxianReinforcementsHaveArrived, c) end)
	end

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		TimerTicks = ContrabandTimes[Difficulty]
	end)

	local entryPath = { CPos.New(82, OStarport.Location.Y + 1), OStarport.Location + CVec.New(1, 1) }
	local exitPath = { CPos.New(2, OStarport.Location.Y + 1) }
	Reinforcements.ReinforceWithTransport(Ordos, "frigate", IxianReinforcements[Difficulty], entryPath, exitPath)
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

Tick = function()
	if not Ordos.IsObjectiveCompleted(KillAtreides) and Atreides.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.GetFluentMessage("atreides-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillAtreides)
		DestroyCarryalls(Atreides)

		if Ordos.IsObjectiveCompleted(KillHarkonnen) then
			Ordos.MarkCompletedObjective(GuardStarport)
		end
	end

	if not Ordos.IsObjectiveCompleted(KillHarkonnen) and Harkonnen.HasNoRequiredUnits() then
		Media.DisplayMessage(UserInterface.GetFluentMessage("harkonnen-annihilated"), Mentat)
		Ordos.MarkCompletedObjective(KillHarkonnen)
		DestroyCarryalls(Harkonnen)

		if Ordos.IsObjectiveCompleted(KillAtreides) then
			Ordos.MarkCompletedObjective(GuardStarport)
		end
	end

	if TimerTicks and TimerTicks > 0 then
		TimerTicks = TimerTicks - 1

		if TimerTicks == 0 then
			if not FirstIxiansArrived then
				Media.DisplayMessage(UserInterface.GetFluentMessage("deliveries-arriving-massive-reinforcements"), Mentat)
			end

			FirstIxiansArrived = true
			SendContraband()
		elseif (TimerTicks % DateTime.Seconds(1)) == 0 then
			local time = Utils.FormatTime(TimerTicks)
			local reinforcementsText = UserInterface.GetFluentMessage("initial-reinforcements-arrive-in", { ["time"] = time })
			if FirstIxiansArrived then
				reinforcementsText = UserInterface.GetFluentMessage("additional-reinforcements-arrive-in", { ["time"] = time })
			end

			UserInterface.SetMissionText(reinforcementsText, Ordos.Color)
		end
	end

	CheckHarvester(Atreides)
	CheckHarvester(Harkonnen)
end

WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")
	Ordos = Player.GetPlayer("Ordos")
	Atreides.Cash = 5000
	Harkonnen.Cash = 5000

	InitObjectives(Ordos)
	GuardStarport = AddPrimaryObjective(Ordos, "defend-starport")
	KillAtreides = AddPrimaryObjective(Ordos, "destroy-atreides")
	KillHarkonnen = AddPrimaryObjective(Ordos, "destroy-harkonnen")

	Camera.Position = OConyard.CenterPosition
	EnemyAttackLocations = { OConyard.Location, OStarport.Location }

	Trigger.OnRemovedFromWorld(OStarport, function()
		Ordos.MarkFailedObjective(GuardStarport)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		TimerTicks = InitialContrabandTimes[Difficulty]
		local time = Utils.FormatTime(TimerTicks)
		Media.DisplayMessage(UserInterface.GetFluentMessage("ixian-reinforcements-in", { ["time"] = time }), Mentat)
	end)

	Trigger.OnAllKilledOrCaptured(Base[Harkonnen.InternalName], function()
		Utils.Do(Harkonnen.GetGroundAttackers(), IdleHunt)
	end)

	Trigger.OnAllKilledOrCaptured(Base[Atreides.InternalName], function()
		Utils.Do(Atreides.GetGroundAttackers(), IdleHunt)
	end)

	local atreidesPath = function() return Utils.Random(AtreidesPaths) end
	local harkonnenPath = function() return Utils.Random(HarkonnenPaths) end
	local atreidesCondition = function() return Ordos.IsObjectiveCompleted(KillAtreides) end
	local harkonnenCondition = function() return Ordos.IsObjectiveCompleted(KillHarkonnen) end
	local huntFunction = function(unit)
		unit.AttackMove(Utils.Random(EnemyAttackLocations))
		IdleHunt(unit)
	end

	local announcementFunction = function()
		Media.DisplayMessage(UserInterface.GetFluentMessage("enemy-reinforcements-arrived"), Mentat)
	end

	Trigger.AfterDelay(3000, function()
		SendCarryallReinforcements(Atreides, 0, AtreidesAttackWaves[Difficulty], EnemyAttackDelay[Difficulty], atreidesPath, AtreidesReinforcements[Difficulty], atreidesCondition, huntFunction, announcementFunction)
	end)


	Trigger.AfterDelay(Utils.RandomInteger(DateTime.Seconds(45), DateTime.Minutes(1) + DateTime.Seconds(15)), function()
		SendCarryallReinforcements(Harkonnen, 0, HarkonnenAttackWaves[Difficulty], EnemyAttackDelay[Difficulty], harkonnenPath, HarkonnenReinforcements[Difficulty], harkonnenCondition, huntFunction, announcementFunction)
	end)

	Actor.Create("upgrade.barracks", true, { Owner = Atreides })
	Actor.Create("upgrade.light", true, { Owner = Atreides })
	Actor.Create("upgrade.heavy", true, { Owner = Atreides })
	Actor.Create("upgrade.barracks", true, { Owner = Harkonnen })
	Actor.Create("upgrade.light", true, { Owner = Harkonnen })
	Actor.Create("upgrade.heavy", true, { Owner = Harkonnen })
	Trigger.AfterDelay(0, ActivateAI)
end
