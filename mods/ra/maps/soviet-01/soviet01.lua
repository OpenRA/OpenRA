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
		i = i + 1
	end)
end

JeepDemolishingBridge = function()
	StartJeep.Move(StartJeepMovePoint.Location)

	Trigger.OnIdle(StartJeep, function()
		Trigger.ClearAll(StartJeep)
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

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	VillageRaidObjective = player.AddPrimaryObjective("Raze the village.")

	Trigger.OnAllRemovedFromWorld(Airfields, function()
		player.MarkFailedObjective(VillageRaidObjective)
	end)

	JeepDemolishingBridge()

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "MissionAccomplished")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "MissionFailed")
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), InsertYaks)
end

Tick = function()
	if france.HasNoRequiredUnits() and germany.HasNoRequiredUnits() then
		player.MarkCompletedObjective(VillageRaidObjective)
	end
end
