Yaks = { "yak", "yak", "yak" }
Airfields = { Airfield1, Airfield2, Airfield3 }

InsertYaks = function()
	local i = 1
	Utils.Do(Yaks, function(yakType)
		local start = YakEntry.CenterPosition + WVec.New(0, (i - 1) * 1536, Actor.CruiseAltitude(yakType))
		local dest = StartJeep.Location + CVec.New(0, 2 * i)
		local yak = Actor.Create(yakType, true, { CenterPosition = start, Owner = player, Facing = (Map.CenterOfCell(dest) - start).Facing })
		yak.Move(dest)
		yak.ReturnToBase(Airfields[i])
		i = i + i
	end)
end

JeepDemolishingBridge = function()
	StartJeep.Move(StartJeepMovePoint.Location)

	Trigger.OnIdle(StartJeep, function()
		if not BridgeBarrel.IsDead then
			BridgeBarrel.Kill()
		end

		local bridge = Map.ActorsInBox(BridgeWaypoint.CenterPosition, Airfield1.CenterPosition,
			function(self) return self.Type == "bridge1" end)[1]

		if not bridge.IsDead then
			bridge.Kill()
		end
	end)
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	france = Player.GetPlayer("France")
	germany = Player.GetPlayer("Germany")
	turkey = Player.GetPlayer("Turkey")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Media.PlayMovieFullscreen("flare.vqa", function()
		CivilProtectionObjective = france.AddPrimaryObjective("Protect the civilians.")
		VillageRaidObjective = player.AddPrimaryObjective("Raze the village.")
		JeepDemolishingBridge()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "StartGame")
		end)
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("snstrafe.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("sfrozen.vqa")
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), InsertYaks)
end

Tick = function()
	if france.HasNoRequiredUnits() and germany.HasNoRequiredUnits() and turkey.HasNoRequiredUnits() then
		player.MarkCompletedObjective(VillageRaidObjective)
	end

	if player.HasNoRequiredUnits() then
		france.MarkCompletedObjective(CivilProtectionObjective)
	end
end