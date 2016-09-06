
ConvoyUnits =
{
	{ "ftrk", "ftrk", "truk", "truk", "apc", "ftrk" },
	{ "ftrk", "3tnk", "truk", "truk", "apc" },
	{ "3tnk", "3tnk", "truk", "truk", "ftrk" }
}

ConvoyRallyPoints =
{
	{ SovietEntry1.Location, SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally6.Location },
	{ SovietEntry2.Location, SovietRally10.Location, SovietRally11.Location }
}

ConvoyDelays =
{
	easy = { DateTime.Minutes(4), DateTime.Minutes(5) + DateTime.Seconds(20) },
	normal = { DateTime.Minutes(2) + DateTime.Seconds(30), DateTime.Minutes(4) },
	hard = { DateTime.Minutes(1) + DateTime.Seconds(30), DateTime.Minutes(2) + DateTime.Seconds(30) }
}

Convoys =
{
	easy = 2,
	normal = 3,
	hard = 5
}

ParadropDelays =
{
	easy = { DateTime.Seconds(40), DateTime.Seconds(90) },
	normal = { DateTime.Seconds(30), DateTime.Seconds(70) },
	hard = { DateTime.Seconds(20), DateTime.Seconds(50) }
}

ParadropWaves =
{
	easy = 4,
	normal = 6,
	hard = 10
}

ParadropLZs = { ParadropPoint1.CenterPosition, ParadropPoint2.CenterPosition, ParadropPoint3.CenterPosition }

Paradropped = 0
Paradrop = function()
	Trigger.AfterDelay(Utils.RandomInteger(ParadropDelay[1], ParadropDelay[2]), function()
		local units = PowerProxy.SendParatroopers(Utils.Random(ParadropLZs))
		Utils.Do(units, function(unit)
			Trigger.OnAddedToWorld(unit, IdleHunt)
		end)

		Paradropped = Paradropped + 1
		if Paradropped <= ParadropWaves[Map.LobbyOption("difficulty")] then
			Paradrop()
		end
	end)
end

ConvoysSent = 0
SendConvoys = function()
	Trigger.AfterDelay(Utils.RandomInteger(ConvoyDelay[1], ConvoyDelay[2]), function()
		local path = Utils.Random(ConvoyRallyPoints)
		local units = Reinforcements.Reinforce(ussr, Utils.Random(ConvoyUnits), { path[1] })
		local lastWaypoint = path[#path]

		Utils.Do(units, function(unit)
			Trigger.OnAddedToWorld(unit, function()
				if unit.Type == "truk" then
					Utils.Do(path, function(waypoint)
						unit.Move(waypoint)
					end)

					Trigger.OnIdle(unit, function()
						unit.Move(lastWaypoint)
					end)
				else
					unit.Patrol(path)
					Trigger.OnIdle(unit, function()
						unit.AttackMove(lastWaypoint)
					end)
				end
			end)
		end)

		local id = Trigger.OnEnteredFootprint({ lastWaypoint }, function(a, id)
			if a.Owner == ussr and Utils.Any(units, function(unit) return unit == a end) then

				-- We are at our destination and thus don't care about other queued actions anymore
				a.Stop()
				a.Destroy()

				if a.Type == "truk" then
					player.MarkFailedObjective(DestroyConvoys)
				end
			end
		end)

		Trigger.OnAllRemovedFromWorld(units, function()
			Trigger.RemoveFootprintTrigger(id)

			ConvoysSent = ConvoysSent + 1
			if ConvoysSent <= Convoys[Map.LobbyOption("difficulty")] then
				SendConvoys()
			else
				player.MarkCompletedObjective(DestroyConvoys)
			end
		end)

		Media.PlaySpeechNotification(player, "ConvoyApproaching")
	end)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(KillUSSR)
	end

	if ussr.HasNoRequiredUnits() then
		player.MarkCompletedObjective(KillUSSR)

		-- We don't care about future convoys anymore
		player.MarkCompletedObjective(DestroyConvoys)
	end
end

InitObjectives = function()
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	KillUSSR = player.AddPrimaryObjective("Destroy all Soviet units and buildings in this region.")
	DestroyConvoys = player.AddSecondaryObjective("Eliminate all passing Soviet convoys.")

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionFailed")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "MissionAccomplished")
		end)
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("Greece")
	ussr = Player.GetPlayer("USSR")

	Camera.Position = AlliedConyard.CenterPosition

	InitObjectives()

	local difficulty = Map.LobbyOption("difficulty")
	ConvoyDelay = ConvoyDelays[difficulty]
	ParadropDelay = ParadropDelays[difficulty]
	PowerProxy = Actor.Create("powerproxy.paratroopers", false, { Owner = ussr })
	Paradrop()
	SendConvoys()

	Trigger.AfterDelay(0, ActivateAI)
end
