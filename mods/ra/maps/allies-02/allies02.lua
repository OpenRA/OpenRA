ConstructionVehicleReinforcements = { "mcv" }
ConstructionVehiclePath = { ReinforcementsEntryPoint.Location, DeployPoint.Location }

JeepReinforcements = { "e1", "e1", "e1", "jeep" }
JeepPath = { ReinforcementsEntryPoint.Location, ReinforcementsRallyPoint.Location }

TruckReinforcements = { "truk", "truk", "truk" }
TruckPath = { TruckEntryPoint.Location, TruckRallyPoint.Location }

PathGuards = { PathGuard1, PathGuard2, PathGuard3, PathGuard4, PathGuard5, PathGuard6, PathGuard7, PathGuard8, PathGuard9, PathGuard10, PathGuard11, PathGuard12, PathGuard13, PathGuard14, PathGuard15 }

IdlingUnits = { }

if Map.LobbyOption("difficulty") == "easy" then
	TimerTicks = DateTime.Minutes(10)
	Announcements =
	{
		{ speech = "TenMinutesRemaining", delay = DateTime.Seconds(3) },
		{ speech = "WarningFiveMinutesRemaining", delay = DateTime.Minutes(5) },
		{ speech = "WarningFourMinutesRemaining", delay = DateTime.Minutes(6) },
		{ speech = "WarningThreeMinutesRemaining", delay = DateTime.Minutes(7) },
		{ speech = "WarningTwoMinutesRemaining", delay = DateTime.Minutes(8) },
		{ speech = "WarningOneMinuteRemaining", delay = DateTime.Minutes(9) }
	}

elseif Map.LobbyOption("difficulty") == "normal" then
	TimerTicks = DateTime.Minutes(5)
	Announcements =
	{
		{ speech = "WarningFiveMinutesRemaining", delay = DateTime.Seconds(3) },
		{ speech = "WarningFourMinutesRemaining", delay = DateTime.Minutes(1) },
		{ speech = "WarningThreeMinutesRemaining", delay = DateTime.Minutes(2) },
		{ speech = "WarningTwoMinutesRemaining", delay = DateTime.Minutes(3) },
		{ speech = "WarningOneMinuteRemaining", delay = DateTime.Minutes(4) }
	}

	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	InfantryDelay = DateTime.Seconds(18)
	AttackGroupSize = 5

elseif Map.LobbyOption("difficulty") == "hard" then
	TimerTicks = DateTime.Minutes(3)
	Announcements =
	{
		{ speech = "WarningThreeMinutesRemaining", delay = DateTime.Seconds(3) },
		{ speech = "WarningTwoMinutesRemaining", delay = DateTime.Minutes(1) },
		{ speech = "WarningOneMinuteRemaining", delay = DateTime.Minutes(2) },
	}

	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "e1" }
	InfantryDelay = DateTime.Seconds(10)
	VehicleTypes = { "ftrk" }
	VehicleDelay = DateTime.Seconds(30)
	AttackGroupSize = 7

else
	TimerTicks = DateTime.Minutes(1)
	Announcements = { { speech = "WarningOneMinuteRemaining", delay = DateTime.Seconds(3) } }
	ConstructionVehicleReinforcements = { "jeep" }

	InfantryTypes = { "e1", "e1", "e1", "e2", "e2", "dog", "dog" }
	InfantryDelay = DateTime.Seconds(10)
	VehicleTypes = { "ftrk" }
	VehicleDelay = DateTime.Minutes(1) + DateTime.Seconds(10)
	AttackGroupSize = 5
end

SendJeepReinforcements = function()
	Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	Reinforcements.Reinforce(player, JeepReinforcements, JeepPath, DateTime.Seconds(1))
end

RunInitialActivities = function()
	Harvester.FindResources()
	Trigger.OnKilled(Harvester, function() HarvesterKilled = true end)

	Trigger.OnAllKilled(PathGuards, SendTrucks)

	if InfantryTypes then
		Trigger.AfterDelay(InfantryDelay, InfantryProduction)
	end

	if VehicleTypes then
		Trigger.AfterDelay(VehicleDelay, VehicleProduction)
	end
end

InfantryProduction = function()
	if SovietBarracks.IsDead then
		return
	end

	local toBuild = { Utils.Random(InfantryTypes) }

	if SovietKennel.IsDead and toBuild == "dog" then
		toBuild = "e1"
	end

	ussr.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(InfantryDelay, InfantryProduction)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

VehicleProduction = function()
	if SovietWarfactory.IsDead then
		return
	end

	if HarvesterKilled then
		ussr.Build({ "harv" }, function(harv)
			harv[1].FindResources()
			Trigger.OnKilled(harv[1], function() HarvesterKilled = true end)

			HarvesterKilled = false
			VehicleProduction()
		end)
		return
	end

	local toBuild = { Utils.Random(VehicleTypes) }
	ussr.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(VehicleDelay, VehicleProduction)

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
		if Map.LobbyOption("difficulty") ~= "tough" then
			unit.AttackMove(DeployPoint.Location)
		end
		Trigger.OnIdle(unit, unit.Hunt)
	end)
end

ticked = TimerTicks
Tick = function()
	ussr.Resources = ussr.Resources - (0.01 * ussr.ResourceCapacity / 25)

	if ussr.HasNoRequiredUnits() then
		player.MarkCompletedObjective(ConquestObjective)
	end

	if player.HasNoRequiredUnits() then
		ussr.MarkCompletedObjective(ussrObj)
	end

	if ticked > 0 then
		UserInterface.SetMissionText("The convoy arrives in " .. Utils.FormatTime(ticked), TimerColor)
		ticked = ticked - 1
	elseif ticked == 0 then
		FinishTimer()
		SendTrucks()
		ticked = ticked - 1
	end
end

FinishTimer = function()
	for i = 0, 5, 1 do
		local c = TimerColor
		if i % 2 == 0 then
			c = HSLColor.White
		end

		Trigger.AfterDelay(DateTime.Seconds(i), function() UserInterface.SetMissionText("The convoy arrived!", c) end)
	end
	Trigger.AfterDelay(DateTime.Seconds(6), function() UserInterface.SetMissionText("") end)
end

ConvoyOnSite = false
SendTrucks = function()
	if not ConvoyOnSite then
		ConvoyOnSite = true

		ticked = 0
		ConvoyObjective = player.AddPrimaryObjective("Escort the convoy.")
		player.MarkCompletedObjective(SecureObjective)

		Media.PlaySpeechNotification(player, "ConvoyApproaching")
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			ConvoyUnharmed = true
			local trucks = Reinforcements.Reinforce(france, TruckReinforcements, TruckPath, DateTime.Seconds(1),
				function(truck)
					Trigger.OnIdle(truck, function() truck.Move(TruckExitPoint.Location) end)
				end)
			count = 0
			Trigger.OnEnteredFootprint( { TruckExitPoint.Location }, function(a, id)
				if a.Owner == france then
					count = count + 1
					a.Destroy()
					if count == 3 then
						player.MarkCompletedObjective(ConvoyObjective)
						Trigger.RemoveFootprintTrigger(id)
					end
				end
			end)
			Trigger.OnAnyKilled(trucks, ConvoyCasualites)
		end)
	end
end

ConvoyCasualites = function()
	Media.PlaySpeechNotification(player, "ConvoyUnitLost")
	if ConvoyUnharmed then
		ConvoyUnharmed = false
		Trigger.AfterDelay(DateTime.Seconds(1), function() player.MarkFailedObjective(ConvoyObjective) end)
	end
end

ConvoyTimerAnnouncements = function()
	for i = #Announcements, 1, -1 do
		Trigger.AfterDelay(Announcements[i].delay, function()
			if not ConvoyOnSite then
				Media.PlaySpeechNotification(player, Announcements[i].speech)
			end
		end)
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("Greece")
	france = Player.GetPlayer("France")
	ussr = Player.GetPlayer("USSR")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "MissionFailed")
	end)
	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "MissionAccomplished")
	end)

	ussrObj = ussr.AddPrimaryObjective("Deny the allies!")

	SecureObjective = player.AddPrimaryObjective("Secure the convoy's path.")
	ConquestObjective = player.AddPrimaryObjective("Eliminate the entire soviet presence in this area.")

	Trigger.AfterDelay(DateTime.Seconds(1), function() Media.PlaySpeechNotification(allies, "MissionTimerInitialised") end)

	RunInitialActivities()

	Reinforcements.Reinforce(player, ConstructionVehicleReinforcements, ConstructionVehiclePath)
	Trigger.AfterDelay(DateTime.Seconds(5), SendJeepReinforcements)
	Trigger.AfterDelay(DateTime.Seconds(10), SendJeepReinforcements)

	Camera.Position = ReinforcementsEntryPoint.CenterPosition
	TimerColor = player.Color

	ConvoyTimerAnnouncements()
end
