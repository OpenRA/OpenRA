--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
ConstructionVehicleReinforcements = { "mcv" }
ConstructionVehiclePath = { ReinforcementsEntryPoint.Location, DeployPoint.Location }

JeepReinforcements = { "e1", "e1", "e1", "jeep" }
JeepPath = { ReinforcementsEntryPoint.Location, ReinforcementsRallyPoint.Location }

TruckReinforcements = { "truk", "truk", "truk" }
TruckPath = { TruckEntryPoint.Location, TruckRallyPoint.Location }

PathGuards = { PathGuard1, PathGuard2, PathGuard3, PathGuard4, PathGuard5, PathGuard6, PathGuard7, PathGuard8, PathGuard9, PathGuard10, PathGuard11, PathGuard12, PathGuard13, PathGuard14, PathGuard15 }

SovietBase = { SovietConyard, SovietRefinery, SovietPower1, SovietPower2, SovietSilo, SovietKennel, SovietBarracks, SovietWarfactory }

IdlingUnits = { }

if Difficulty == "easy" then
	DateTime.TimeLimit = DateTime.Minutes(10) + DateTime.Seconds(3)

elseif Difficulty == "normal" then
	DateTime.TimeLimit = DateTime.Minutes(5) + DateTime.Seconds(3)
	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	InfantryDelay = DateTime.Seconds(18)
	AttackGroupSize = 5

elseif Difficulty == "hard" then
	DateTime.TimeLimit = DateTime.Minutes(3) + DateTime.Seconds(3)
	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	InfantryDelay = DateTime.Seconds(10)
	VehicleTypes = { "ftrk" }
	VehicleDelay = DateTime.Seconds(30)
	AttackGroupSize = 7

else
	DateTime.TimeLimit = DateTime.Minutes(1) + DateTime.Seconds(3)
	ConstructionVehicleReinforcements = { "jeep" }

	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "dog", "dog" }
	InfantryDelay = DateTime.Seconds(10)
	VehicleTypes = { "ftrk" }
	VehicleDelay = DateTime.Minutes(1) + DateTime.Seconds(10)
	AttackGroupSize = 5
end

SendJeepReinforcements = function()
	Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	Reinforcements.Reinforce(Greece, JeepReinforcements, JeepPath, DateTime.Seconds(1))
end

RunInitialActivities = function()
	Harvester.FindResources()
	Trigger.OnKilled(Harvester, function() HarvesterKilled = true end)

	Trigger.OnAllKilled(PathGuards, function()
		Greece.MarkCompletedObjective(SecureObjective)
		SendTrucks()
	end)

	Trigger.OnAllKilled(SovietBase, function()
		Utils.Do(USSR.GetGroundAttackers(), function(unit)
			if not Utils.Any(PathGuards, function(pg) return pg == unit end) then
				Trigger.OnIdle(unit, unit.Hunt)
			end
		end)
	end)

	if InfantryTypes then
		Trigger.AfterDelay(InfantryDelay, ProduceInfantry)
	end

	if VehicleTypes then
		Trigger.AfterDelay(VehicleDelay, ProduceVehicles)
	end
end

ProduceInfantry = function()
	if SovietBarracks.IsDead then
		return
	end

	local toBuild = { Utils.Random(InfantryTypes) }

	if SovietKennel.IsDead and toBuild == "dog" then
		toBuild = { "e1" }
	end

	USSR.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(InfantryDelay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if SovietWarfactory.IsDead then
		return
	end

	if HarvesterKilled then
		USSR.Build({ "harv" }, function(harv)
			harv[1].FindResources()
			Trigger.OnKilled(harv[1], function() HarvesterKilled = true end)

			HarvesterKilled = false
			ProduceVehicles()
		end)
		return
	end

	local toBuild = { Utils.Random(VehicleTypes) }
	USSR.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(VehicleDelay, ProduceVehicles)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

SendAttack = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	Utils.Do(units, function(unit)
		if Difficulty ~= "tough" then
			unit.AttackMove(DeployPoint.Location)
		end
		Trigger.OnIdle(unit, unit.Hunt)
	end)
end

Tick = function()
	USSR.Resources = USSR.Resources - (0.01 * USSR.ResourceCapacity / 25)

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(ConquestObjective)
	end

	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(USSRobjective)
	end
end

FinishTimer = function()
	DateTime.TimeLimit = 0
	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function()
			UserInterface.SetMissionText(UserInterface.Translate("convoy-arrived"), c)
		end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

ConvoyOnSite = false
SendTrucks = function()
	if not ConvoyOnSite then
		ConvoyOnSite = true

		DateTime.TimeLimit = 0
		UserInterface.SetMissionText("")
		ConvoyObjective = AddPrimaryObjective(Greece, "escort-convoy")

		Media.PlaySpeechNotification(Greece, "ConvoyApproaching")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			ConvoyUnharmed = true
			local trucks = Reinforcements.Reinforce(England, TruckReinforcements, TruckPath, DateTime.Seconds(1),
				function(truck)
					Trigger.OnIdle(truck, function() truck.Move(TruckExitPoint.Location) end)
				end)
			local count = 0
			Trigger.OnEnteredFootprint( { TruckExitPoint.Location }, function(a, id)
				if a.Owner == England then
					count = count + 1
					a.Destroy()
					if count == 3 then
						Greece.MarkCompletedObjective(ConvoyObjective)
						Trigger.RemoveFootprintTrigger(id)
					end
				end
			end)
			Trigger.OnAnyKilled(trucks, ConvoyCasualites)
		end)
	end
end

ConvoyCasualites = function()
	Media.PlaySpeechNotification(Greece, "ConvoyUnitLost")
	if ConvoyUnharmed then
		ConvoyUnharmed = false
		Trigger.AfterDelay(DateTime.Seconds(1), function() Greece.MarkFailedObjective(ConvoyObjective) end)
	end
end

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")

	InitObjectives(Greece)

	USSRobjective = AddPrimaryObjective(USSR, "")

	SecureObjective = AddPrimaryObjective(Greece, "secure-convoy")
	ConquestObjective = AddPrimaryObjective(Greece, "eliminate-soviets")

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(Allies, "MissionTimerInitialised") end)

	RunInitialActivities()

	Reinforcements.Reinforce(Greece, ConstructionVehicleReinforcements, ConstructionVehiclePath)
	Trigger.AfterDelay(DateTime.Seconds(5), SendJeepReinforcements)
	Trigger.AfterDelay(DateTime.Seconds(10), SendJeepReinforcements)

	Trigger.OnTimerExpired(function()
		FinishTimer()
		SendTrucks()
	end)

	Camera.Position = ReinforcementsEntryPoint.CenterPosition
	TimerColor = Greece.Color
end
